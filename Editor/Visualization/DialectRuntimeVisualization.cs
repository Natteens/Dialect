using System;
using System.Collections.Generic;
using Dialect.Core;
using Unity.GraphToolkit.Editor.GraphVisualization;
using UnityEditor;
using UnityEngine;

namespace Dialect.Editor.Visualization
{
    [InitializeOnLoad]
    static class DialectRuntimeVisualization
    {
        const double RefreshInterval = .2d;
        static readonly HashSet<DialectDirector> Directors = new();
        static readonly Dictionary<DialectSession, VisualizationState> States = new();
        static double nextRefresh;
        static bool updateSubscribed;

        sealed class VisualizationState : IDisposable
        {
            public DialectSession Session;
            public Context Context;
            public Hash128 ObservedActiveContextId;
            public bool HasObservedActiveContext;
            public bool WasObservedAsActive;
            public double ContextCreatedAt;
            public string RenderedNodeId;
            public DialectTransition? RenderedTransition;
            public int RenderedPreviewHash;

            public void Dispose()
            {
                if (Context != null && Context.IsValid)
                {
                    try { Context.ClearAllVisualization(); }
                    catch (ArgumentException) { }
                    finally { Context.Dispose(); }
                }
                Context = null;
                HasObservedActiveContext = false;
                WasObservedAsActive = false;
            }
        }

        static DialectRuntimeVisualization()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            DialectDirector.EditorDirectorEnabled += OnDirectorEnabled;
            DialectDirector.EditorDirectorDisabled += OnDirectorDisabled;
            DialectProjectSettings.VisualizationChanged += OnVisualizationChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                foreach (var director in UnityEngine.Object.FindObjectsByType<DialectDirector>(FindObjectsInactive.Include)) Attach(director);
                ScheduleReplay();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode) ClearAll();
        }

        static void OnDirectorEnabled(DialectDirector director)
        {
            if (!Application.isPlaying) return;
            Attach(director);
            Capture(director.Session);
        }

        static void OnDirectorDisabled(DialectDirector director)
        {
            if (director == null) return;
            Remove(director.Session);
            Detach(director);
        }

        static void Attach(DialectDirector director)
        {
            if (director == null || !Directors.Add(director)) return;
            director.SessionStarted += OnSessionStarted;
            director.NodeEntered += OnNodeEntered;
            director.Transitioned += OnTransitioned;
            director.ValueResolved += OnValueResolved;
            director.SessionEnded += OnSessionEnded;
            Capture(director.Session);
        }

        static void Detach(DialectDirector director)
        {
            if (director == null || !Directors.Remove(director)) return;
            director.SessionStarted -= OnSessionStarted;
            director.NodeEntered -= OnNodeEntered;
            director.Transitioned -= OnTransitioned;
            director.ValueResolved -= OnValueResolved;
            director.SessionEnded -= OnSessionEnded;
        }

        static void OnSessionStarted(DialectSession session) => Capture(session, true);
        static void OnNodeEntered(DialectSession session, RuntimeNode node) { Capture(session); Replay(session); }
        static void OnTransitioned(DialectSession session, DialectTransition transition) { Capture(session); Replay(session); }
        static void OnValueResolved(DialectSession session, DialectValuePreview preview) { Capture(session); Replay(session); }
        static void OnSessionEnded(DialectSession session, DialectTerminationReason reason) => Remove(session);

        static void OnVisualizationChanged(bool enabled)
        {
            if (!enabled)
            {
                foreach (var state in States.Values)
                    if (state.Context != null && state.Context.IsValid) state.Context.ClearAllVisualization();
                return;
            }
            InvalidateRenderedState();
            ScheduleReplay();
        }

        static void Capture(DialectSession session, bool replaceGraphSession = false)
        {
            if (session?.Graph == null) return;
            if (replaceGraphSession)
            {
                var stale = new List<DialectSession>();
                foreach (var pair in States)
                    if (pair.Key != session && pair.Key.Graph == session.Graph) stale.Add(pair.Key);
                foreach (var oldSession in stale) Remove(oldSession);
            }
            if (!States.ContainsKey(session)) States.Add(session, new VisualizationState { Session = session });
            EnsureUpdateSubscription();
            ScheduleReplay();
        }

        static void ScheduleReplay()
        {
            EditorApplication.delayCall -= ReplayAll;
            EditorApplication.delayCall += ReplayAll;
        }

        static void ReplayAll()
        {
            foreach (var session in new List<DialectSession>(States.Keys)) Replay(session);
        }

        static void Replay(DialectSession session)
        {
            if (!DialectProjectSettings.instance.VisualizationEnabled || session?.Graph == null || !States.TryGetValue(session, out var state)) return;
            if (!EnsureContext(state) || !state.Context.IsGraphLoaded) return;

            var nodeId = session.CurrentNodeId ?? string.Empty;
            var transition = session.LastTransition;
            var previewHash = PreviewHash(session.ValuePreviews);
            if (state.RenderedNodeId == nodeId && Nullable.Equals(state.RenderedTransition, transition) && state.RenderedPreviewHash == previewHash) return;

            try
            {
                var context = state.Context;
                context.NodeCustomizationEnabled = true;
                context.WireCustomizationEnabled = true;
                context.PortPreviewEnabled = true;
                context.ClearAllVisualization();

                foreach (var candidate in session.Graph.Transitions)
                {
                    if (!TryParse(candidate.OutputPortId, out var candidateOutput) ||
                        !TryParse(candidate.InputPortId, out var candidateInput)) continue;
                    var inactiveWire = context.GetWireReference(candidateOutput, candidateInput);
                    inactiveWire.Opacity = .42f;
                    inactiveWire.IsDashed = true;
                }

                if (TryParse(nodeId, out var parsedNode))
                {
                    var node = context.GetNodeReference(parsedNode);
                    node.FillAmount = 100f;
                    context.Motion.Play(node, 1.2f);
                }

                if (transition.HasValue && TryParse(transition.Value.OutputPortId, out var output) &&
                    TryParse(transition.Value.InputPortId, out var input))
                {
                    var wire = context.GetWireReference(output, input);
                    wire.ClearCustomization();
                    wire.Opacity = 1f;
                    wire.WidthOverride = 8f;
                    wire.IsDashed = false;
                    context.Motion.Play(wire, 1.4f);
                }

                foreach (var preview in session.ValuePreviews)
                    if (TryParse(preview.Key, out var portId)) context.GetPortReference(portId).SetPreview(Truncate(preview.Value));

                state.RenderedNodeId = nodeId;
                state.RenderedTransition = transition;
                state.RenderedPreviewHash = previewHash;
            }
            catch (ArgumentException)
            {
                // IsGraphLoaded can become true one update before all node views exist.
                // Recreate the public context and replay the retained state on the next tick.
                ResetContext(state);
            }
            catch (ObjectDisposedException)
            {
                ResetContext(state);
            }
        }

        static bool EnsureContext(VisualizationState state)
        {
            if (!TryParse(state.Session.Graph.GraphId, out var graphId)) return false;
            Context active;
            try { active = Registry.GetActiveContext(graphId); }
            catch (ArgumentException) { return false; }
            var activeId = active?.VisualizationContextID ?? default;

            if (state.Context != null && state.Context.IsValid)
            {
                if (ReferenceEquals(active, state.Context))
                {
                    state.WasObservedAsActive = true;
                    state.ObservedActiveContextId = activeId;
                }
                if (state.HasObservedActiveContext && activeId != state.ObservedActiveContextId)
                {
                    // Graph Toolkit detaches or unregisters contexts while entering Play Mode
                    // and while graph windows rebuild. Registry unregister does not mark the
                    // public Context invalid, so IsValid alone can leave us holding an orphan.
                    if (!ReferenceEquals(active, state.Context))
                    {
                        ResetContext(state);
                    }
                    else
                    {
                        state.ObservedActiveContextId = activeId;
                    }
                }

                if (state.Context != null && state.Context.IsValid && active == null &&
                    !state.WasObservedAsActive && state.Context.IsGraphLoaded &&
                    EditorApplication.timeSinceStartup - state.ContextCreatedAt >= 1d)
                {
                    // Covers a context created just before Registry's Play Mode cleanup:
                    // it can be unregistered without ever producing an active-ID change.
                    ResetContext(state);
                }

                if (state.Context != null && state.Context.IsValid) return true;
            }

            try { state.Context = Registry.CreateVisualizationContext(graphId); }
            catch (ArgumentException) { return false; }
            state.ObservedActiveContextId = activeId;
            state.HasObservedActiveContext = true;
            state.WasObservedAsActive = false;
            state.ContextCreatedAt = EditorApplication.timeSinceStartup;
            state.RenderedNodeId = null;
            state.RenderedTransition = null;
            state.RenderedPreviewHash = 0;
            return state.Context != null && state.Context.IsValid;
        }

        static void ResetContext(VisualizationState state)
        {
            if (state.Context != null && state.Context.IsValid) state.Context.Dispose();
            state.Context = null;
            state.HasObservedActiveContext = false;
            state.WasObservedAsActive = false;
            state.RenderedNodeId = null;
            state.RenderedTransition = null;
            state.RenderedPreviewHash = 0;
        }

        static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
            if (States.Count == 0)
            {
                EditorApplication.update -= OnEditorUpdate;
                updateSubscribed = false;
                return;
            }
            ReplayAll();
        }

        static void EnsureUpdateSubscription()
        {
            if (updateSubscribed) return;
            updateSubscribed = true;
            EditorApplication.update += OnEditorUpdate;
        }

        static void InvalidateRenderedState()
        {
            foreach (var state in States.Values)
            {
                state.RenderedNodeId = null;
                state.RenderedTransition = null;
                state.RenderedPreviewHash = 0;
            }
        }

        static int PreviewHash(IReadOnlyDictionary<string, string> previews)
        {
            unchecked
            {
                var hash = 17;
                foreach (var pair in previews) hash = hash * 31 + pair.Key.GetHashCode() * 7 + (pair.Value?.GetHashCode() ?? 0);
                return hash;
            }
        }

        static string Truncate(string value)
        {
            value ??= string.Empty;
            value = value.Replace('\r', ' ').Replace('\n', ' ');
            return value.Length <= 48 ? value : value.Substring(0, 45) + "...";
        }

        static bool TryParse(string value, out Hash128 result)
        {
            try { result = Hash128.Parse(value); return result.isValid; }
            catch { result = default; return false; }
        }

        static void Remove(DialectSession session)
        {
            if (session == null || !States.Remove(session, out var state)) return;
            state.Dispose();
        }

        static void ClearAll()
        {
            EditorApplication.delayCall -= ReplayAll;
            EditorApplication.update -= OnEditorUpdate;
            updateSubscribed = false;
            foreach (var state in States.Values) state.Dispose();
            States.Clear();
            foreach (var director in new List<DialectDirector>(Directors)) Detach(director);
        }
    }
}
