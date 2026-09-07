using System;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Nodes;
using Dialect.Values;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;

namespace Dialect.Tests
{
    public sealed class DialectBuiltInValueTests
    {
        GameObject owner;
        DialectDirector director;
        readonly List<UnityEngine.Object> assets = new();

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("Dialect Built-in Value Test");
            director = owner.AddComponent<DialectDirector>();
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
            foreach (var asset in assets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [TestCase(DialectComparisonOperator.Equal, 2, 2, true)]
        [TestCase(DialectComparisonOperator.NotEqual, 2, 3, true)]
        [TestCase(DialectComparisonOperator.Less, 2, 3, true)]
        [TestCase(DialectComparisonOperator.LessOrEqual, 2, 2, true)]
        [TestCase(DialectComparisonOperator.Greater, 3, 2, true)]
        [TestCase(DialectComparisonOperator.GreaterOrEqual, 3, 3, true)]
        public void CompareIntegerSupportsEveryOperator(DialectComparisonOperator operation, int a, int b, bool expected) =>
            AssertCompare(DialectCompareType.Integer, operation, new DialectIntValue(a), new DialectIntValue(b), expected);

        [TestCase(DialectComparisonOperator.Equal, 2f, 2f, true)]
        [TestCase(DialectComparisonOperator.NotEqual, 2f, 3f, true)]
        [TestCase(DialectComparisonOperator.Less, 2f, 3f, true)]
        [TestCase(DialectComparisonOperator.LessOrEqual, 2f, 2f, true)]
        [TestCase(DialectComparisonOperator.Greater, 3f, 2f, true)]
        [TestCase(DialectComparisonOperator.GreaterOrEqual, 3f, 3f, true)]
        public void CompareFloatSupportsEveryExactOperator(DialectComparisonOperator operation, float a, float b, bool expected) =>
            AssertCompare(DialectCompareType.Float, operation, new DialectFloatValue(a), new DialectFloatValue(b), expected);

        [Test]
        public void CompareStringBooleanAndObjectSupportEqualityOperators()
        {
            AssertCompare(DialectCompareType.String, DialectComparisonOperator.Equal,
                new DialectStringValue("A"), new DialectStringValue("A"), true);
            AssertCompare(DialectCompareType.String, DialectComparisonOperator.NotEqual,
                new DialectStringValue("A"), new DialectStringValue("B"), true);
            AssertCompare(DialectCompareType.Boolean, DialectComparisonOperator.Equal,
                new DialectBoolValue(true), new DialectBoolValue(true), true);
            AssertCompare(DialectCompareType.Boolean, DialectComparisonOperator.NotEqual,
                new DialectBoolValue(true), new DialectBoolValue(false), true);
            var asset = ScriptableObject.CreateInstance<TestAsset>();
            assets.Add(asset);
            AssertCompare(DialectCompareType.Object, DialectComparisonOperator.Equal,
                new DialectObjectValue(asset), new DialectObjectValue(asset), true);
            AssertCompare(DialectCompareType.Object, DialectComparisonOperator.NotEqual,
                new DialectObjectValue(asset), new DialectObjectValue(null), true);
            AssertCompare(DialectCompareType.Object, DialectComparisonOperator.Equal,
                new DialectObjectValue(null), new DialectObjectValue(null), true);
        }

        [Test]
        public void CompareAcceptsConnectedResolversAndRejectsInvalidOperator()
        {
            var left = new DialectValueExpression(new FixedResolver(typeof(int), 4), "left");
            var right = new DialectValueExpression(new FixedResolver(typeof(int), 5), "right");
            Assert.That(Resolve(new CompareValueResolver(DialectCompareType.Integer,
                DialectComparisonOperator.Less, left, right)), Is.EqualTo(true));
            Resolve(new CompareValueResolver(DialectCompareType.String,
                DialectComparisonOperator.Greater, String("A"), String("B")));
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));
        }

        [TestCase(DialectValueType.String, true)]
        [TestCase(DialectValueType.String, false)]
        [TestCase(DialectValueType.Boolean, true)]
        [TestCase(DialectValueType.Boolean, false)]
        [TestCase(DialectValueType.Integer, true)]
        [TestCase(DialectValueType.Integer, false)]
        [TestCase(DialectValueType.Float, true)]
        [TestCase(DialectValueType.Float, false)]
        [TestCase(DialectValueType.Object, true)]
        [TestCase(DialectValueType.Object, false)]
        [TestCase(DialectValueType.LocalizedString, true)]
        [TestCase(DialectValueType.LocalizedString, false)]
        public void SelectReturnsOnlyChosenTypedPath(DialectValueType type, bool condition)
        {
            var first = Value(type, true);
            var second = Value(type, false);
            var selected = Resolve(new SelectValueResolver(type, Bool(condition),
                new DialectValueExpression(new FixedResolver(DialectValueUtility.GetSystemType(type), first), "true"),
                new DialectValueExpression(new FixedResolver(DialectValueUtility.GetSystemType(type), second), "false")));
            Assert.That(selected, Is.SameAs(condition ? first : second).Or.EqualTo(condition ? first : second));
        }

        [TestCase(DialectNumericOperation.Add, 5, 2, 7)]
        [TestCase(DialectNumericOperation.Subtract, 5, 2, 3)]
        [TestCase(DialectNumericOperation.Multiply, 5, 2, 10)]
        public void ModifyLocalIntegerSupportsEveryOperation(DialectNumericOperation operation,
            int initial, int operand, int expected)
        {
            var definition = new DialectVariableDefinition("score", "Score", new DialectIntValue(initial));
            PlayWithVariables(new[] { definition }, null,
                new ModifyVariableRuntimeNode(definition.Id, DialectValueType.Integer, operation,
                    new DialectValueExpression(new DialectIntValue(operand)), 1), new EndRuntimeNode());
            Assert.That(director.Session.Variables.TryGet(definition.Id, out int value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
            Assert.That(((DialectIntValue)definition.DefaultValue).Value, Is.EqualTo(initial));
        }

        [TestCase(DialectNumericOperation.Add, 2f, .5f, 2.5f)]
        [TestCase(DialectNumericOperation.Subtract, 2f, .5f, 1.5f)]
        [TestCase(DialectNumericOperation.Multiply, 2f, .5f, 1f)]
        public void ModifyFloatSupportsEveryOperation(DialectNumericOperation operation,
            float initial, float operand, float expected)
        {
            var definition = new DialectVariableDefinition("speed", "Speed", new DialectFloatValue(initial));
            PlayWithVariables(new[] { definition }, null,
                new ModifyVariableRuntimeNode(definition.Id, DialectValueType.Float, operation,
                    new DialectValueExpression(new DialectFloatValue(operand)), 1), new EndRuntimeNode());
            Assert.That(director.Session.Variables.TryGet(definition.Id, out float value), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }

        [Test]
        public void ModifyBooleanToggleAndStringAppendNeedOnlyValidOperands()
        {
            var enabled = new DialectVariableDefinition("enabled", "Enabled", new DialectBoolValue(false));
            var name = new DialectVariableDefinition("name", "Name", new DialectStringValue("Dia"));
            PlayWithVariables(new[] { enabled, name }, null,
                new ModifyVariableRuntimeNode(enabled.Id, DialectValueType.Boolean, default, default, 1),
                new ModifyVariableRuntimeNode(name.Id, DialectValueType.String, default, String("lect"), 2),
                new EndRuntimeNode());
            Assert.That(director.Session.Variables.TryGet(enabled.Id, out bool flag), Is.True);
            Assert.That(flag, Is.True);
            Assert.That(director.Session.Variables.TryGet(name.Id, out string text), Is.True);
            Assert.That(text, Is.EqualTo("Dialect"));
        }

        [Test]
        public void ModifySharedValueKeepsAssetImmutableAndSnapshotCoherent()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var shared = board.AddVariable("Score", new DialectIntValue(3));
            assets.Add(board);
            PlayWithVariables(null, new[] { board },
                new ModifyVariableRuntimeNode(shared.Id, DialectValueType.Integer, DialectNumericOperation.Add,
                    new DialectValueExpression(new DialectIntValue(4)), 1), new EndRuntimeNode());
            var snapshot = director.Session.Variables.CreateSnapshot();
            Assert.That((int)snapshot[shared.Id].BoxedValue, Is.EqualTo(7));
            Assert.That(((DialectIntValue)shared.DefaultValue).Value, Is.EqualTo(3));
        }

        [Test]
        public void ModifyRejectsMissingTargetAndTypeMismatch()
        {
            var definition = new DialectVariableDefinition("score", "Score", new DialectIntValue(3));
            PlayWithVariables(new[] { definition }, null,
                new ModifyVariableRuntimeNode("missing", DialectValueType.Integer, DialectNumericOperation.Add,
                    new DialectValueExpression(new DialectIntValue(1)), 1), new EndRuntimeNode());
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));

            PlayWithVariables(new[] { definition }, null,
                new ModifyVariableRuntimeNode(definition.Id, DialectValueType.Float, DialectNumericOperation.Add,
                    new DialectValueExpression(new DialectFloatValue(1)), 1), new EndRuntimeNode());
            Assert.That(director.Session.State, Is.EqualTo(DialectPlaybackState.Faulted));
        }

        void AssertCompare(DialectCompareType type, DialectComparisonOperator operation,
            DialectValue a, DialectValue b, bool expected) =>
            Assert.That(Resolve(new CompareValueResolver(type, operation,
                new DialectValueExpression(a), new DialectValueExpression(b))), Is.EqualTo(expected));

        object Resolve(DialectValueResolver resolver)
        {
            var capture = new CaptureValueRuntimeNode(resolver, 1);
            director.Play(Graph(null, null, capture, new EndRuntimeNode()));
            return capture.Value;
        }

        void PlayWithVariables(IEnumerable<DialectVariableDefinition> variables,
            IEnumerable<DialectBlackboard> boards, params RuntimeNode[] nodes) => director.Play(Graph(variables, boards, nodes));

        DialectRuntimeGraph Graph(IEnumerable<DialectVariableDefinition> variables,
            IEnumerable<DialectBlackboard> boards, params RuntimeNode[] nodes)
        {
            var graph = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            graph.Configure(Guid.NewGuid().ToString("N"), 0, new List<RuntimeNode>(nodes),
                variables == null ? null : new List<DialectVariableDefinition>(variables),
                boards == null ? null : new List<DialectBlackboard>(boards), null, new List<string>());
            assets.Add(graph);
            return graph;
        }

        static DialectValueExpression Bool(bool value) => new(new DialectBoolValue(value));
        static DialectValueExpression String(string value) => new(new DialectStringValue(value));

        object Value(DialectValueType type, bool first) => type switch
        {
            DialectValueType.LocalizedString => new LocalizedString("Table", first ? "First" : "Second"),
            DialectValueType.Boolean => first,
            DialectValueType.Integer => first ? 1 : 2,
            DialectValueType.Float => first ? 1f : 2f,
            DialectValueType.Object => CreateAsset(),
            _ => first ? "First" : "Second"
        };

        TestAsset CreateAsset()
        {
            var asset = ScriptableObject.CreateInstance<TestAsset>();
            assets.Add(asset);
            return asset;
        }

        sealed class TestAsset : ScriptableObject { }

        [Serializable]
        sealed class FixedResolver : DialectValueResolver
        {
            readonly Type type;
            readonly object value;
            public FixedResolver(Type type, object value) { this.type = type; this.value = value; }
            public override Type ValueType => type;
            public override object Resolve(DialectExecutionContext context) => value;
        }

        [Serializable]
        sealed class CaptureValueRuntimeNode : RuntimeNode
        {
            [SerializeReference] DialectValueResolver resolver;
            [SerializeField] int next;
            public CaptureValueRuntimeNode(DialectValueResolver resolver, int next)
            { this.resolver = resolver; this.next = next; }
            public object Value { get; private set; }
            public override DialectExecutionResult Execute(DialectExecutionContext context)
            {
                Value = resolver.Resolve(context);
                return DialectExecutionResult.ContinueTo(next);
            }
        }
    }
}
