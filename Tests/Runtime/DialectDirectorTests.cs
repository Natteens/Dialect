using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Actions;
using Dialect.Conditions;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Nodes;
using NUnit.Framework;
using UnityEngine;

namespace Dialect.Tests
{
    public sealed class DialectDirectorTests
    {
        GameObject owner;
        DialectDirector director;
        readonly List<Object> assets = new();

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Dialect Director Test");
            director = owner.AddComponent<DialectDirector>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(owner);
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

        DialectRuntimeGraph Graph(params RuntimeNode[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(System.Guid.NewGuid().ToString("N"), 0, new List<RuntimeNode>(nodes), null, new List<string>());
            assets.Add(graph);
            return graph;
        }
    }
}
