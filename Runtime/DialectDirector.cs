using System;
using System.Collections.Generic;
using System.Threading;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Nodes;
using Dialect.Values;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Dialect
{
    [AddComponentMenu("Dialect/Dialect Director")]
    public sealed class DialectDirector : MonoBehaviour
    {
        [SerializeField] DialectRuntimeGraph defaultGraph;
        [SerializeField, Min(1)] int maxAutomaticSteps = 1024;
        [SerializeField] int randomSeed = 1979;

        bool isPumping;
        bool dispatchingLine;
        bool dispatchingChoices;
        PendingCommand pendingCommand;
        int pendingChoice = -1;
        CancellationTokenSource sessionCancellation;
        int sessionGeneration;

        enum PendingCommand { None, Advance, Choice, Stop }

        public DialectRuntimeGraph DefaultGraph { get => defaultGraph; set => defaultGraph = value; }
        public int RandomSeed { get => randomSeed; set => randomSeed = value; }
        public DialectSession Session { get; private set; }
        public bool IsRunning => Session != null && Session.State is DialectPlaybackState.Running
            or DialectPlaybackState.WaitingForAdvance or DialectPlaybackState.WaitingForChoice
            or DialectPlaybackState.Suspended;

        public event Action<DialectLine> LinePresented;
        public event Action<DialectChoiceSet> ChoicesPresented;
        public event Action<DialectSession> SessionStarted;
        public event Action<DialectSession, DialectTerminationReason> SessionEnded;
        public event Action<DialectSession, string> SessionFaulted;
        public event Action<DialectSession, RuntimeNode> NodeEntered;
        public event Action<DialectSession, DialectTransition> Transitioned;
        public event Action<DialectSession, DialectValuePreview> ValueResolved;

#if UNITY_EDITOR
        internal static event Action<DialectDirector> EditorDirectorEnabled;
        internal static event Action<DialectDirector> EditorDirectorDisabled;
#endif

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
#if UNITY_EDITOR
            EditorDirectorEnabled?.Invoke(this);
#endif
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (IsRunning) Terminate(DialectTerminationReason.DirectorDisabled, DialectPlaybackState.Ended);
#if UNITY_EDITOR
            EditorDirectorDisabled?.Invoke(this);
#endif
        }

        void OnDestroy() => CancelSessionOperations();

        public void Play() => Play(defaultGraph, null);
        public void Play(DialectRuntimeGraph graph) => Play(graph, null);

        public void Play(DialectRuntimeGraph graph, object userData)
        {
            if (TryPlay(graph, userData, null)) return;
            var details = graph == null ? "The graph is null." : string.Join(" ", graph.Diagnostics);
            throw new InvalidOperationException($"Dialect could not start the requested graph. {details}".TrimEnd());
        }

        public bool TryPlay(DialectRuntimeGraph graph) => TryPlay(graph, null);

        public bool TryPlay(DialectRuntimeGraph graph, object userData) => TryPlay(graph, userData, null);

        public void Play(DialectRuntimeGraph graph, object userData,
            IReadOnlyDictionary<string, DialectValue> overrides)
        {
            if (TryPlay(graph, userData, overrides)) return;
            var details = graph == null ? "The graph is null." : string.Join(" ", graph.Diagnostics);
            throw new InvalidOperationException($"Dialect could not start the requested graph. {details}".TrimEnd());
        }

        public bool TryPlay(DialectRuntimeGraph graph, object userData,
            IReadOnlyDictionary<string, DialectValue> overrides)
        {
            if (graph == null || !graph.IsValid || !graph.TryGetNode(graph.EntryNodeIndex, out _)) return false;
            DialectVariableStore variables;
            try { variables = new DialectVariableStore(graph.LocalVariables, graph.Blackboards); }
            catch (InvalidOperationException) { return false; }
            if (overrides != null)
                foreach (var pair in overrides)
                    if (!variables.TrySet(pair.Key, pair.Value)) return false;
            if (IsRunning) Terminate(DialectTerminationReason.Interrupted, DialectPlaybackState.Ended);
            pendingCommand = PendingCommand.None;
            pendingChoice = -1;
            BeginSessionOperations();
            Session = new DialectSession(graph, variables, userData, randomSeed);
            SessionStarted?.Invoke(Session);
            Pump();
            return true;
        }

        public bool Advance()
        {
            if (isPumping)
            {
                if (!dispatchingLine) return false;
                pendingCommand = PendingCommand.Advance;
                return true;
            }
            if (Session?.State != DialectPlaybackState.WaitingForAdvance) return false;
            MoveTo(Session.PendingTarget);
            Session.State = DialectPlaybackState.Running;
            Pump();
            return true;
        }

        public bool Choose(int index)
        {
            if (isPumping && dispatchingChoices && Session?.CurrentChoices != null && index >= 0 && index < Session.CurrentChoices.Count)
            { pendingCommand = PendingCommand.Choice; pendingChoice = index; return true; }
            if (Session?.State != DialectPlaybackState.WaitingForChoice || Session.CurrentChoices == null || index < 0 || index >= Session.CurrentChoices.Count) return false;
            MoveTo(Session.CurrentChoices.Choices[index].TargetNodeIndex);
            Session.CurrentChoices = null;
            Session.State = DialectPlaybackState.Running;
            Pump();
            return true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            if (isPumping) { pendingCommand = PendingCommand.Stop; return; }
            Terminate(DialectTerminationReason.Stopped, DialectPlaybackState.Ended);
        }

        public bool Resume()
        {
            if (Session?.State != DialectPlaybackState.Suspended ||
                Session.SuspensionKind == DialectSuspensionKind.Automatic) return false;
            if (Session.PendingTarget >= 0) MoveTo(Session.PendingTarget);
            Session.PendingTarget = -1;
            Session.SuspensionKind = DialectSuspensionKind.None;
            Session.State = DialectPlaybackState.Running;
            Pump();
            return true;
        }

        internal void PresentLine(DialectLine line)
        {
            Session.CurrentLine = line;
            Session.CurrentChoices = null;
            dispatchingLine = true;
            try { LinePresented?.Invoke(line); }
            finally { dispatchingLine = false; }
        }

        internal void PresentChoices(DialectChoiceSet choices)
        {
            Session.CurrentChoices = choices;
            Session.CurrentLine = default;
            dispatchingChoices = true;
            try { ChoicesPresented?.Invoke(choices); }
            finally { dispatchingChoices = false; }
        }

        internal void ReportResolvedValue(string portId, string value)
        {
            if (Session?.SetValuePreview(portId, value) == true)
                ValueResolved?.Invoke(Session, new DialectValuePreview(portId, value));
        }

        internal void BeginWait(DialectSession session, float duration, WaitTimeMode timeMode, int target)
        {
            var generation = sessionGeneration;
            var token = sessionCancellation.Token;
            RunWait(session, generation, duration, timeMode, target, token);
        }

        internal void BeginWaitUntil(DialectSession session, DialectValueExpression condition,
            DialectExecutionContext context, int target)
        {
            var generation = sessionGeneration;
            var token = sessionCancellation.Token;
            RunWaitUntil(session, generation, condition, context, target, token);
        }

        async void RunWait(DialectSession session, int generation, float duration, WaitTimeMode timeMode,
            int target, CancellationToken token)
        {
            try
            {
                if (timeMode == WaitTimeMode.Scaled)
                    await Awaitable.WaitForSecondsAsync(duration, token);
                else
                {
                    var elapsed = 0f;
                    while (elapsed < duration)
                    {
                        await Awaitable.NextFrameAsync(token);
                        elapsed += Time.unscaledDeltaTime;
                    }
                }
                ContinueAutomaticSuspension(session, generation, target);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { FaultAutomaticSuspension(session, generation, exception.Message); }
        }

        async void RunWaitUntil(DialectSession session, int generation, DialectValueExpression condition,
            DialectExecutionContext context, int target, CancellationToken token)
        {
            try
            {
                while (true)
                {
                    await Awaitable.NextFrameAsync(token);
                    if (condition.Resolve(context) is not bool ready)
                        throw new InvalidOperationException("Wait Until Condition must resolve to Boolean.");
                    if (ready) break;
                }
                ContinueAutomaticSuspension(session, generation, target);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { FaultAutomaticSuspension(session, generation, exception.Message); }
        }

        void ContinueAutomaticSuspension(DialectSession session, int generation, int target)
        {
            if (!IsCurrentAutomaticSuspension(session, generation, target)) return;
            MoveTo(target);
            session.PendingTarget = -1;
            session.SuspensionKind = DialectSuspensionKind.None;
            session.State = DialectPlaybackState.Running;
            Pump();
        }

        void FaultAutomaticSuspension(DialectSession session, int generation, string message)
        {
            if (!IsCurrentAutomaticSuspension(session, generation, session.PendingTarget)) return;
            Fault(message, DialectTerminationReason.ExecutionError);
        }

        bool IsCurrentAutomaticSuspension(DialectSession session, int generation, int target) =>
            ReferenceEquals(Session, session) && sessionGeneration == generation &&
            session.State == DialectPlaybackState.Suspended &&
            session.SuspensionKind == DialectSuspensionKind.Automatic && session.PendingTarget == target;

        void BeginSessionOperations()
        {
            CancelSessionOperations();
            sessionGeneration++;
            sessionCancellation = new CancellationTokenSource();
        }

        void CancelSessionOperations()
        {
            if (sessionCancellation == null) return;
            sessionCancellation.Cancel();
            sessionCancellation.Dispose();
            sessionCancellation = null;
        }

        void Pump()
        {
            if (isPumping || Session == null) return;
            isPumping = true;
            try
            {
                var steps = 0;
                while (Session.State == DialectPlaybackState.Running)
                {
                    if (++steps > maxAutomaticSteps)
                    { Fault("Automatic execution exceeded the configured step limit.", DialectTerminationReason.RunawayExecution); break; }
                    if (!Session.Graph.TryGetNode(Session.CurrentNodeIndex, out var node))
                    { Fault($"Node index {Session.CurrentNodeIndex} is invalid.", DialectTerminationReason.ExecutionError); break; }

                    Session.CurrentNodeId = node.AuthoringId;
                    NodeEntered?.Invoke(Session, node);
                    DialectExecutionResult result;
                    try
                    {
                        result = node.Execute(new DialectExecutionContext(this, Session.Graph, Session, Session.Variables, Session.UserData));
                    }
                    catch (Exception exception)
                    { Fault(exception.Message, DialectTerminationReason.ExecutionError); break; }

                    Apply(result);
                    ApplyPendingCommand();
                }
            }
            finally { isPumping = false; }
            ApplyPendingCommand();
        }

        void Apply(DialectExecutionResult result)
        {
            switch (result.Kind)
            {
                case DialectExecutionKind.Continue: MoveTo(result.TargetNodeIndex); break;
                case DialectExecutionKind.WaitForAdvance:
                    Session.PendingTarget = result.TargetNodeIndex;
                    Session.State = DialectPlaybackState.WaitingForAdvance;
                    break;
                case DialectExecutionKind.AwaitChoice: Session.State = DialectPlaybackState.WaitingForChoice; break;
                case DialectExecutionKind.Suspended:
                    Session.PendingTarget = result.TargetNodeIndex;
                    Session.SuspensionKind = result.SuspensionKind;
                    Session.State = DialectPlaybackState.Suspended;
                    break;
                case DialectExecutionKind.End: Terminate(DialectTerminationReason.Completed, DialectPlaybackState.Ended); break;
            }
        }

        void ApplyPendingCommand()
        {
            var command = pendingCommand;
            var choice = pendingChoice;
            pendingCommand = PendingCommand.None;
            pendingChoice = -1;
            if (command == PendingCommand.Stop) Terminate(DialectTerminationReason.Stopped, DialectPlaybackState.Ended);
            else if (command == PendingCommand.Advance && Session?.State == DialectPlaybackState.WaitingForAdvance)
            { MoveTo(Session.PendingTarget); Session.State = DialectPlaybackState.Running; }
            else if (command == PendingCommand.Choice && Session?.State == DialectPlaybackState.WaitingForChoice &&
                     choice >= 0 && choice < Session.CurrentChoices.Count)
            { MoveTo(Session.CurrentChoices.Choices[choice].TargetNodeIndex); Session.State = DialectPlaybackState.Running; }
        }

        void OnLocaleChanged(Locale locale)
        {
            if (!IsRunning || Session == null || !Session.Graph.TryGetNode(Session.CurrentNodeIndex, out var node)) return;
            node.RefreshPresentation(new DialectExecutionContext(this, Session.Graph, Session, Session.Variables, Session.UserData));
        }

        void MoveTo(int target)
        {
            if (Session == null) return;
            var from = Session.CurrentNodeIndex;
            Session.CurrentNodeIndex = target;
            if (Session.Graph.TryGetTransition(from, target, out var transition))
            {
                Session.LastTransition = transition;
                Transitioned?.Invoke(Session, transition);
            }
        }

        void Fault(string message, DialectTerminationReason reason)
        {
            CancelSessionOperations();
            Session.State = DialectPlaybackState.Faulted;
            Session.TerminationReason = reason;
            SessionFaulted?.Invoke(Session, message);
            SessionEnded?.Invoke(Session, reason);
        }

        void Terminate(DialectTerminationReason reason, DialectPlaybackState state)
        {
            if (Session == null) return;
            CancelSessionOperations();
            Session.State = state;
            Session.TerminationReason = reason;
            Session.CurrentLine = default;
            Session.CurrentChoices = null;
            Session.PendingTarget = -1;
            Session.SuspensionKind = DialectSuspensionKind.None;
            SessionEnded?.Invoke(Session, reason);
        }
    }
}
