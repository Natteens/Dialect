using System;
using System.Collections;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Nodes;
using Dialect.Values;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dialect.Tests
{
    public sealed class DialectWaitTests
    {
        GameObject owner;
        DialectDirector director;
        readonly List<UnityEngine.Object> assets = new();

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Dialect Wait Test");
            director = owner.AddComponent<DialectDirector>();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
            foreach (var asset in assets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [Test]
        public void WaitZeroAndNegativeContinueImmediately()
        {
            foreach (var duration in new[] { 0f, -1f })
            {
                director.Play(Graph(new WaitRuntimeNode(Float(duration), WaitTimeMode.Scaled, 1), new EndRuntimeNode()));
                Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            }
        }

        [UnityTest]
        public IEnumerator PositiveScaledWaitSuspendsThenTransitionsExactlyOnce()
        {
            var transitions = 0;
            director.Transitioned += (_, _) => transitions++;
            director.Play(GraphWithTransitions(
                new RuntimeNode[] { new WaitRuntimeNode(Float(.001f), WaitTimeMode.Scaled, 1), new EndRuntimeNode() },
                new DialectTransition(0, 1, "wait-out", "end-in")));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Suspended));
            Assert.That(director.Session.CurrentNodeIndex, Is.EqualTo(0));
            Assert.That(director.Resume(), Is.False);
            yield return CompleteWithinFrames(120);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            Assert.That(transitions, Is.EqualTo(1));
            Assert.That(director.Session.LastTransition.Value.OutputPortId, Is.EqualTo("wait-out"));
        }

        [UnityTest]
        public IEnumerator ScaledWaitPausesAtZeroTimeScale()
        {
            Time.timeScale = 0f;
            director.Play(Graph(new WaitRuntimeNode(Float(.0001f), WaitTimeMode.Scaled, 1), new EndRuntimeNode()));
            for (var i = 0; i < 4; i++) yield return null;
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Suspended));
            Time.timeScale = 1f;
            yield return CompleteWithinFrames(120);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [UnityTest]
        public IEnumerator UnscaledWaitCompletesAtZeroTimeScale()
        {
            Time.timeScale = 0f;
            director.Play(Graph(new WaitRuntimeNode(Float(.0001f), WaitTimeMode.Unscaled, 1), new EndRuntimeNode()));
            yield return CompleteWithinFrames(120);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [UnityTest]
        public IEnumerator StopCancelsWaitWithoutFault()
        {
            var faults = 0;
            director.SessionFaulted += (_, _) => faults++;
            director.Play(Graph(new WaitRuntimeNode(Float(.02f), WaitTimeMode.Unscaled, 1), new EndRuntimeNode()));
            director.Stop();
            for (var i = 0; i < 4; i++) yield return null;
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Stopped));
            Assert.That(faults, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReplacementPlayCancelsOldWaitAndGenerationGuardProtectsNewSession()
        {
            director.Play(Graph(new WaitRuntimeNode(Float(.02f), WaitTimeMode.Unscaled, 1), new EndRuntimeNode()));
            var oldSession = director.Session;
            director.Play(Graph(new DialogueRuntimeNode(default, DialectText.Inline("New"), 1), new EndRuntimeNode()));
            var newSession = director.Session;
            for (var i = 0; i < 8; i++) yield return null;
            Assert.That(oldSession.TerminationReason, Is.EqualTo(DialectTerminationReason.Interrupted));
            Assert.That(director.Session, Is.SameAs(newSession));
            Assert.That(newSession.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
        }

        [UnityTest]
        public IEnumerator DisableAndDestroyCancelWaitsWithoutFault()
        {
            var faults = 0;
            director.SessionFaulted += (_, _) => faults++;
            director.Play(Graph(new WaitRuntimeNode(Float(.02f), WaitTimeMode.Unscaled, 1), new EndRuntimeNode()));
            director.enabled = false;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.DirectorDisabled));
            Assert.That(faults, Is.Zero);

            director.enabled = true;
            director.Play(Graph(new WaitRuntimeNode(Float(.02f), WaitTimeMode.Unscaled, 1), new EndRuntimeNode()));
            UnityEngine.Object.Destroy(owner);
            owner = null;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(faults, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SessionFaultCancelsPendingOperationAndDoesNotContinue()
        {
            var resolver = new ThrowAfterFalseResolver();
            director.Play(Graph(new WaitUntilRuntimeNode(new DialectValueExpression(resolver, "condition"), 1),
                new EndRuntimeNode()));
            yield return null;
            yield return null;
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.ExecutionError));
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));
        }

        [Test]
        public void WaitUntilTrueContinuesImmediately()
        {
            director.Play(Graph(new WaitUntilRuntimeNode(Bool(true), 1), new EndRuntimeNode()));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [UnityTest]
        public IEnumerator WaitUntilLocalVariableReevaluatesAndContinuesOnce()
        {
            var definition = new DialectVariableDefinition("ready", "Ready", new DialectBoolValue(false));
            var transitions = 0;
            var previews = 0;
            director.Transitioned += (_, _) => transitions++;
            director.ValueResolved += (_, value) => { if (value.PortId == "ready-port") previews++; };
            var graph = GraphWithVariables(new[] { definition }, null,
                new WaitUntilRuntimeNode(new DialectValueExpression(
                    new DialectVariableValueResolver(definition.Id, DialectValueType.Boolean), "ready-port"), 1),
                new EndRuntimeNode());
            director.Play(graph);
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Suspended));
            Assert.That(director.Session.CurrentNodeIndex, Is.Zero);
            Assert.That(director.Session.Variables.TrySet(definition.Id, new DialectBoolValue(true)), Is.True);
            yield return null;
            yield return null;
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            Assert.That(transitions, Is.Zero);
            Assert.That(previews, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator WaitUntilSharedAndCustomBooleanValuesContinue()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var shared = board.AddVariable("Ready", new DialectBoolValue(false));
            assets.Add(board);
            var graph = GraphWithVariables(null, new[] { board },
                new WaitUntilRuntimeNode(new DialectValueExpression(
                    new DialectVariableValueResolver(shared.Id, DialectValueType.Boolean), "shared-ready"), 1),
                new EndRuntimeNode());
            director.Play(graph);
            director.Session.Variables.TrySet(shared.Id, new DialectBoolValue(true));
            yield return null;
            yield return null;
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            Assert.That(((DialectBoolValue)shared.DefaultValue).Value, Is.False);

            var custom = new MutableBoolResolver();
            director.Play(Graph(new WaitUntilRuntimeNode(new DialectValueExpression(custom, "custom-ready"), 1),
                new EndRuntimeNode()));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Suspended));
            custom.Value = true;
            yield return null;
            yield return null;
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        [UnityTest]
        public IEnumerator WaitUntilCancellationAndReplacementNeverDuplicateContinuation()
        {
            var mutable = new MutableBoolResolver();
            var transitions = 0;
            director.Transitioned += (_, _) => transitions++;
            director.Play(Graph(new WaitUntilRuntimeNode(new DialectValueExpression(mutable, "condition"), 1),
                new EndRuntimeNode()));
            director.Stop();
            mutable.Value = true;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(transitions, Is.Zero);

            mutable.Value = false;
            director.Play(Graph(new WaitUntilRuntimeNode(new DialectValueExpression(mutable, "condition"), 1),
                new EndRuntimeNode()));
            director.Play(Graph(new DialogueRuntimeNode(default, DialectText.Inline("Replacement"), 1),
                new EndRuntimeNode()));
            mutable.Value = true;
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.WaitingForAdvance));
            Assert.That(transitions, Is.Zero);
        }

        [Test]
        public void WaitForResumeAdvancesOnceWithoutReexecutingItself()
        {
            var entered = 0;
            director.NodeEntered += (_, node) => { if (node is WaitForResumeRuntimeNode) entered++; };
            director.Play(Graph(new WaitForResumeRuntimeNode(1), new EndRuntimeNode()));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Suspended));
            Assert.That(director.Resume(), Is.True);
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            Assert.That(director.Resume(), Is.False);
            Assert.That(entered, Is.EqualTo(1));
        }

        [Test]
        public void StopAndReplacementWhileWaitingForResumeAreSafe()
        {
            director.Play(Graph(new WaitForResumeRuntimeNode(1), new EndRuntimeNode()));
            director.Stop();
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Stopped));
            director.Play(Graph(new WaitForResumeRuntimeNode(1), new EndRuntimeNode()));
            var old = director.Session;
            director.Play(Graph(new EndRuntimeNode()));
            Assert.That(old.TerminationReason, Is.EqualTo(DialectTerminationReason.Interrupted));
            Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
        }

        IEnumerator CompleteWithinFrames(int count)
        {
            for (var i = 0; i < count && director.Session.State == DialectPlaybackState.Suspended; i++)
                yield return null;
        }

        DialectRuntimeGraph Graph(params RuntimeNode[] nodes) => GraphWithVariables(null, null, nodes);

        DialectRuntimeGraph GraphWithVariables(IEnumerable<DialectVariableDefinition> variables,
            IEnumerable<DialectBlackboard> boards, params RuntimeNode[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(Guid.NewGuid().ToString("N"), 0, new List<RuntimeNode>(nodes),
                variables == null ? null : new List<DialectVariableDefinition>(variables),
                boards == null ? null : new List<DialectBlackboard>(boards), null, new List<string>());
            assets.Add(graph);
            return graph;
        }

        DialectRuntimeGraph GraphWithTransitions(RuntimeNode[] nodes, params DialectTransition[] transitions)
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(Guid.NewGuid().ToString("N"), 0, new List<RuntimeNode>(nodes), null, null,
                new List<DialectTransition>(transitions), new List<string>());
            assets.Add(graph);
            return graph;
        }

        static DialectValueExpression Float(float value) => new(new DialectFloatValue(value));
        static DialectValueExpression Bool(bool value) => new(new DialectBoolValue(value));

        [Serializable]
        sealed class MutableBoolResolver : DialectValueResolver
        {
            public bool Value { get; set; }
            public override Type ValueType => typeof(bool);
            public override object Resolve(DialectExecutionContext context) => Value;
        }

        [Serializable]
        sealed class ThrowAfterFalseResolver : DialectValueResolver
        {
            int calls;
            public override Type ValueType => typeof(bool);
            public override object Resolve(DialectExecutionContext context)
            {
                if (calls++ == 0) return false;
                throw new InvalidOperationException("Condition failed.");
            }
        }
    }
}
