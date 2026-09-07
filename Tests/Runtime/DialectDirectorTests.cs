using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Actions;
using Dialect.Conditions;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Nodes;
using Dialect.Values;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Dialect.Tests
{
    public sealed class DialectDirectorTests
    {
        sealed class TestLocalesProvider : ILocalesProvider
        {
            public List<Locale> Locales { get; } = new();

            public Locale GetLocale(LocaleIdentifier id)
            {
                foreach (var locale in Locales)
                    if (locale != null && locale.Identifier == id) return locale;
                return null;
            }

            public void AddLocale(Locale locale)
            {
                if (locale != null && !Locales.Contains(locale)) Locales.Add(locale);
            }

            public bool RemoveLocale(Locale locale) => Locales.Remove(locale);
        }

        GameObject owner;
        DialectDirector director;
        LocalizationSettings originalLocalizationSettings;
        readonly List<Object> assets = new();

        [SetUp]
        public void SetUp()
        {
            originalLocalizationSettings = LocalizationSettings.Instance;
            var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            var baselineLocale = Locale.CreateLocale("en");
            assets.Add(settings);
            assets.Add(baselineLocale);
            LocalizationSettings.Instance = settings;
            LocalizationSettings.AvailableLocales = new TestLocalesProvider();
            LocalizationSettings.AvailableLocales.AddLocale(baselineLocale);
            LocalizationSettings.StartupLocaleSelectors.Clear();
            LocalizationSettings.StartupLocaleSelectors.Add(new SpecificLocaleSelector { LocaleId = baselineLocale.Identifier });
            LocalizationSettings.SelectedLocale = baselineLocale;
            owner = new GameObject("Dialect Director Test");
            director = owner.AddComponent<DialectDirector>();
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null) Object.DestroyImmediate(owner);
            LocalizationSettings.Instance = originalLocalizationSettings;
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test]
        public void PlayAdvanceAndEndUsesExplicitSessionState()
        {
            var graph = Graph(new StartRuntimeNode(1),
                new DialogueRuntimeNode(DialectText.Inline("Guide"), DialectText.Inline("Hello"), 2),
                new EndRuntimeNode());
            DialectLine received = default;
            director.LinePresented += line => received = line;
            Assert.That(director.TryPlay(graph), Is.True);
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
            Assert.That(received.Text, Is.EqualTo("Hello"));
            Assert.That(director.Advance(), Is.True);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [Test]
        public void InvalidChoiceDoesNotEndOrMoveSession()
        {
            var choices = new List<DialectChoiceDefinition>
            { new(DialectText.Inline("Continue"), 2) };
            var graph = Graph(new StartRuntimeNode(1), new ChoiceRuntimeNode(choices), new EndRuntimeNode());
            director.TryPlay(graph);
            Assert.That(director.Choose(4), Is.False);
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForChoice));
            Assert.That(director.Choose(0), Is.True);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [Test]
        public void StartingAnotherGraphInterruptsCurrentSession()
        {
            var first = Graph(new StartRuntimeNode(1),
                new DialogueRuntimeNode(default, DialectText.Inline("First"), 2), new EndRuntimeNode());
            var second = Graph(new StartRuntimeNode(1), new EndRuntimeNode());
            var reasons = new List<DialectTerminationReason>();
            director.SessionEnded += (_, value) => reasons.Add(value);
            director.TryPlay(first);
            director.TryPlay(second);
            Assert.That(reasons, Does.Contain(DialectTerminationReason.Interrupted));
        }

        [Test]
        public void AutomaticCycleStopsAtRunawayGuard()
        {
            var graph = Graph(new StartRuntimeNode(0));
            director.TryPlay(graph);
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.RunawayExecution));
        }

        [Test]
        public void InvalidGraphIsRejectedWithoutCreatingSession()
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure("invalid", -1, new List<RuntimeNode>(), null, new List<string> { "Missing Start" });
            assets.Add(graph);
            Assert.That(director.TryPlay(graph), Is.False);
            Assert.That(director.Session, Is.Null);
            var exception = Assert.Throws<System.InvalidOperationException>(() => director.Play(graph));
            Assert.That(exception.Message, Does.Contain("Missing Start"));
        }

        [Test]
        public void StopEndsWaitingSessionExplicitly()
        {
            var graph = Graph(new StartRuntimeNode(1),
                new DialogueRuntimeNode(default, DialectText.Inline("Wait"), 2), new EndRuntimeNode());
            director.Play(graph);
            director.Stop();
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Ended));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Stopped));
        }

        [TestCase(true, "True")]
        [TestCase(false, "False")]
        public void ConditionUsesExplicitSemanticTarget(bool result, string expected)
        {
            var condition = ScriptableObject.CreateInstance<FixedCondition>();
            condition.Result = result;
            assets.Add(condition);
            var graph = Graph(new StartRuntimeNode(1), new ConditionRuntimeNode(condition, 2, 4),
                new DialogueRuntimeNode(default, DialectText.Inline("True"), 3), new EndRuntimeNode(),
                new DialogueRuntimeNode(default, DialectText.Inline("False"), 3));
            DialectLine line = default;
            director.LinePresented += value => line = value;
            director.Play(graph);
            Assert.That(line.Text, Is.EqualTo(expected));
        }

        [Test]
        public void ActionReceivesTypedContextAndUserData()
        {
            var action = ScriptableObject.CreateInstance<CaptureAction>();
            assets.Add(action);
            var marker = new object();
            director.Play(Graph(new StartRuntimeNode(1), new ActionRuntimeNode(action, 2), new EndRuntimeNode()), marker);
            Assert.That(action.Director, Is.SameAs(director));
            Assert.That(action.UserData, Is.SameAs(marker));
        }

        [Test]
        public void LongAutomaticChainExecutesIteratively()
        {
            const int length = 700;
            var nodes = new RuntimeNode[length + 1];
            for (var i = 0; i < length; i++) nodes[i] = new StartRuntimeNode(i + 1);
            nodes[length] = new EndRuntimeNode();
            director.Play(Graph(nodes));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [Test]
        public void AdvanceRequestedFromPresentationCallbackIsQueued()
        {
            var graph = Graph(new StartRuntimeNode(1),
                new DialogueRuntimeNode(default, DialectText.Inline("Continue"), 2), new EndRuntimeNode());
            director.LinePresented += _ => director.Advance();
            director.Play(graph);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [Test]
        public void ChoiceRequestedFromPresentationCallbackIsQueued()
        {
            var graph = Graph(new StartRuntimeNode(1),
                new ChoiceRuntimeNode(new List<DialectChoiceDefinition> { new(DialectText.Inline("Go"), 2) }),
                new EndRuntimeNode());
            director.ChoicesPresented += _ => director.Choose(0);
            director.Play(graph);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [Test]
        public void InvalidReentrantAdvanceIsRejected()
        {
            var action = ScriptableObject.CreateInstance<AdvanceAction>();
            assets.Add(action);
            action.Director = director;
            director.Play(Graph(new StartRuntimeNode(1), new ActionRuntimeNode(action, 2), new EndRuntimeNode()));
            Assert.That(action.Result, Is.False);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [Test]
        public void StopRequestedFromPresentationCallbackEndsSession()
        {
            var graph = Graph(new StartRuntimeNode(1),
                new DialogueRuntimeNode(default, DialectText.Inline("Stop"), 2), new EndRuntimeNode());
            director.LinePresented += _ => director.Stop();
            director.Play(graph);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Stopped));
        }

        [Test]
        public void InvalidTargetFaultsInsteadOfThrowing()
        {
            director.Play(Graph(new StartRuntimeNode(99)));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.ExecutionError));
        }

        [Test]
        public void ResumeOnlyRunsSuspendedSession()
        {
            var marker = new SuspensionMarker();
            director.Play(Graph(new SuspendOnceRuntimeNode(1), new EndRuntimeNode()), marker);
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Suspended));
            Assert.That(director.Resume(), Is.True);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            Assert.That(director.Resume(), Is.False);
        }

        [Test]
        public void DisablingDirectorTerminatesActiveSession()
        {
            director.Play(Graph(new DialogueRuntimeNode(default, DialectText.Inline("Wait"), 1), new EndRuntimeNode()));
            director.enabled = false;
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.DirectorDisabled));
        }

        [Test]
        public void InvalidReplacementDoesNotInterruptCurrentGraph()
        {
            var waiting = Graph(new DialogueRuntimeNode(default, DialectText.Inline("Wait"), 1), new EndRuntimeNode());
            var invalid = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            invalid.Configure("invalid", -1, new List<RuntimeNode>(), null, new List<string> { "Invalid" });
            assets.Add(invalid);
            director.Play(waiting);
            Assert.That(director.TryPlay(invalid), Is.False);
            Assert.That(director.Session.Graph, Is.SameAs(waiting));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
        }

        [Test]
        public void InvalidOverrideDoesNotInterruptCurrentGraph()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            assets.Add(board);
            var variable = board.AddVariable("Flag", new DialectBoolValue(true));
            var waiting = Graph(new DialogueRuntimeNode(default, DialectText.Inline("Wait"), 1), new EndRuntimeNode());
            var replacement = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            replacement.Configure("replacement", 0, new List<RuntimeNode> { new EndRuntimeNode() },
                new List<DialectBlackboard> { board }, new List<string>());
            assets.Add(replacement);
            director.Play(waiting);
            var overrides = new Dictionary<string, DialectValue>
            { [variable.Id] = new DialectIntValue(4) };
            Assert.That(director.TryPlay(replacement, null, overrides), Is.False);
            Assert.That(director.Session.Graph, Is.SameAs(waiting));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
        }

        [Test]
        public void DestroyingDirectorTerminatesActiveSession()
        {
            DialectTerminationReason? reason = null;
            director.SessionEnded += (_, value) => reason = value;
            director.Play(Graph(new DialogueRuntimeNode(default, DialectText.Inline("Wait"), 1), new EndRuntimeNode()));
            Object.DestroyImmediate(owner);
            owner = null;
            Assert.That(reason, Is.EqualTo(DialectTerminationReason.DirectorDisabled));
        }

        [Test]
        public void TransitionEventReportsCompiledWireMapping()
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure("graph", 0, new List<RuntimeNode> { new StartRuntimeNode(1), new EndRuntimeNode() },
                null, null, new List<DialectTransition> { new(0, 1, "output", "input") }, new List<string>());
            assets.Add(graph);
            DialectTransition observed = default;
            director.Transitioned += (_, transition) => observed = transition;
            director.Play(graph);
            Assert.That(observed.OutputPortId, Is.EqualTo("output"));
            Assert.That(observed.InputPortId, Is.EqualTo("input"));
        }

        [Test]
        public void SharedStringFeedsDialogueWithoutMutatingAsset()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            assets.Add(board);
            var variable = board.AddVariable("Speaker", new DialectStringValue("Mara"));
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure("graph", 0, new List<RuntimeNode>
            {
                new DialogueRuntimeNode(DialectText.Blackboard(new DialectVariableReference(board, variable.Id)),
                    DialectText.Inline("Hello"), 1), new EndRuntimeNode()
            }, new List<DialectBlackboard> { board }, new List<string>());
            assets.Add(graph);
            DialectLine line = default;
            director.LinePresented += value => line = value;
            director.Play(graph);
            Assert.That(line.Speaker, Is.EqualTo("Mara"));
            Assert.That(director.Session.Variables.TrySet(variable.Id, new DialectStringValue("Alex")), Is.True);
            Assert.That(((DialectStringValue)variable.DefaultValue).Value, Is.EqualTo("Mara"));
        }

        [Test]
        public void EmptyLocalizedStringResolvesAsEmptyText()
        {
            DialectLine line = default;
            director.LinePresented += value => line = value;
            director.Play(Graph(new DialogueRuntimeNode(DialectText.Localized(new LocalizedString()),
                DialectText.Localized(new LocalizedString()), 1), new EndRuntimeNode()));
            Assert.That(line.Speaker, Is.Empty);
            Assert.That(line.Text, Is.Empty);
        }

        [Test]
        public void LocaleChangesRefreshVisibleLineWithoutMovingSession()
        {
            var original = LocalizationSettings.SelectedLocale;
            var first = Locale.CreateLocale("x-dialect-a");
            var second = Locale.CreateLocale("x-dialect-b");
            assets.Add(first);
            assets.Add(second);
            LocalizationSettings.AvailableLocales.AddLocale(first);
            LocalizationSettings.AvailableLocales.AddLocale(second);
            var callbacks = 0;
            director.LinePresented += _ => callbacks++;
            try
            {
                director.Play(Graph(new DialogueRuntimeNode(DialectText.Inline("Guide"), DialectText.Inline("Line"), 1),
                    new EndRuntimeNode()));
                var nodeIndex = director.Session.CurrentNodeIndex;
                LocalizationSettings.SelectedLocale = first;
                LocalizationSettings.SelectedLocale = second;
                Assert.That(callbacks, Is.EqualTo(3));
                Assert.That(director.Session.CurrentNodeIndex, Is.EqualTo(nodeIndex));
                Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
            }
            finally { LocalizationSettings.SelectedLocale = original; }
        }

        [Test]
        public void LocaleChangeRefreshesChoicesWithoutLosingSelectionState()
        {
            var original = LocalizationSettings.SelectedLocale;
            var locale = Locale.CreateLocale("x-dialect-choice");
            assets.Add(locale);
            LocalizationSettings.AvailableLocales.AddLocale(locale);
            var callbacks = 0;
            director.ChoicesPresented += _ => callbacks++;
            try
            {
                director.Play(Graph(new ChoiceRuntimeNode(new List<DialectChoiceDefinition>
                    { new(DialectText.Inline("Continue"), 1) }), new EndRuntimeNode()));
                LocalizationSettings.SelectedLocale = locale;
                Assert.That(callbacks, Is.EqualTo(2));
                Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForChoice));
                Assert.That(director.Session.CurrentChoices.Count, Is.EqualTo(1));
            }
            finally { LocalizationSettings.SelectedLocale = original; }
        }

        [Test]
        public void LocaleChangeAfterStopDoesNotRefreshPresentation()
        {
            var original = LocalizationSettings.SelectedLocale;
            var locale = Locale.CreateLocale("x-dialect-stopped");
            assets.Add(locale);
            LocalizationSettings.AvailableLocales.AddLocale(locale);
            var callbacks = 0;
            director.LinePresented += _ => callbacks++;
            try
            {
                director.Play(Graph(new DialogueRuntimeNode(default, DialectText.Inline("Line"), 1), new EndRuntimeNode()));
                director.Stop();
                LocalizationSettings.SelectedLocale = locale;
                Assert.That(callbacks, Is.EqualTo(1));
            }
            finally { LocalizationSettings.SelectedLocale = original; }
        }

        [Test]
        public void BranchUsesBooleanExpressionAndTakesOnlyMatchingPath()
        {
            DialectLine line = default;
            director.LinePresented += value => line = value;
            director.Play(Graph(
                new BranchRuntimeNode(new DialectValueExpression(new DialectBoolValue(true)), 1, 2),
                new DialogueRuntimeNode(default, DialectText.Inline("True"), 3),
                new DialogueRuntimeNode(default, DialectText.Inline("False"), 3),
                new EndRuntimeNode()));
            Assert.That(line.Text, Is.EqualTo("True"));
            Assert.That(director.Session.CurrentNodeIndex, Is.EqualTo(1));
        }

        [Test]
        public void SetVariableChangesSessionWithoutMutatingDefinition()
        {
            var definition = new DialectVariableDefinition("score-id", "Score", new DialectIntValue(2));
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(System.Guid.NewGuid().ToString("N"), 0,
                new List<RuntimeNode>
                {
                    new SetVariableRuntimeNode(definition.Id, DialectValueType.Integer,
                        new DialectValueExpression(new DialectIntValue(9)), 1),
                    new EndRuntimeNode()
                }, new List<DialectVariableDefinition> { definition }, null, null, new List<string>());
            assets.Add(graph);
            director.Play(graph);
            Assert.That(director.Session.Variables.TryGet(definition.Id, out int value), Is.True);
            Assert.That(value, Is.EqualTo(9));
            Assert.That(((DialectIntValue)definition.DefaultValue).Value, Is.EqualTo(2));
        }

        [Test]
        public void RandomBranchIsRepeatableForDirectorSeed()
        {
            string first = null;
            string second = null;
            director.RandomSeed = 42;
            director.LinePresented += line =>
            {
                if (first == null) first = line.Text;
                else second = line.Text;
            };
            RuntimeNode[] Nodes() => new RuntimeNode[]
            {
                new RandomBranchRuntimeNode(new List<int> { 1, 2, 3 }),
                new DialogueRuntimeNode(default, DialectText.Inline("A"), 4),
                new DialogueRuntimeNode(default, DialectText.Inline("B"), 4),
                new DialogueRuntimeNode(default, DialectText.Inline("C"), 4),
                new EndRuntimeNode()
            };
            director.Play(Graph(Nodes()));
            director.Stop();
            director.Play(Graph(Nodes()));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void SessionRetainsCurrentVisualizationStateForLateReplay()
        {
            var start = new StartRuntimeNode(1);
            var dialogue = new DialogueRuntimeNode(default,
                new DialectTextExpression(new DialectConstantValueResolver(new DialectStringValue("Stateful line")), "line-port"), 2);
            var end = new EndRuntimeNode();
            start.SetAuthoringId("start-node");
            dialogue.SetAuthoringId("dialogue-node");
            end.SetAuthoringId("end-node");
            var transition = new DialectTransition(0, 1, "start-output", "dialogue-input");
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(System.Guid.NewGuid().ToString("N"), 0,
                new List<RuntimeNode> { start, dialogue, end }, null, null,
                new List<DialectTransition> { transition, new(1, 2, "dialogue-output", "end-input") }, new List<string>());
            assets.Add(graph);

            director.Play(graph);

            Assert.That(director.Session.CurrentNodeId, Is.EqualTo("dialogue-node"));
            Assert.That(director.Session.LastTransition, Is.EqualTo(transition));
            Assert.That(director.Session.ValuePreviews["line-port"], Is.EqualTo("Stateful line"));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
        }

        public sealed class FixedCondition : DialectCondition
        {
            public bool Result { get; set; }
            public override bool Evaluate(DialectExecutionContext context) => Result;
        }

        public sealed class CaptureAction : DialectAction
        {
            public DialectDirector Director { get; private set; }
            public object UserData { get; private set; }
            public override void Execute(DialectExecutionContext context)
            {
                Director = context.Director;
                UserData = context.UserData;
            }
        }

        public sealed class AdvanceAction : DialectAction
        {
            public DialectDirector Director { get; set; }
            public bool Result { get; private set; }
            public override void Execute(DialectExecutionContext context) => Result = Director.Advance();
        }

        sealed class SuspensionMarker { public bool Resumed; }

        [System.Serializable]
        sealed class SuspendOnceRuntimeNode : RuntimeNode
        {
            [SerializeField] int next;
            public SuspendOnceRuntimeNode(int next) => this.next = next;
            public override DialectExecutionResult Execute(DialectExecutionContext context)
            {
                var marker = (SuspensionMarker)context.UserData;
                if (!marker.Resumed) { marker.Resumed = true; return DialectExecutionResult.Suspended(); }
                return DialectExecutionResult.ContinueTo(next);
            }
        }

        DialectRuntimeGraph Graph(params RuntimeNode[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(System.Guid.NewGuid().ToString("N"), 0, new List<RuntimeNode>(nodes), null, new List<string>());
            assets.Add(graph);
            return graph;
        }
    }
}
