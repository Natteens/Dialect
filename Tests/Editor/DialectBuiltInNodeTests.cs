using System.Linq;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Editor;
using Dialect.Editor.Inspectors;
using Dialect.Editor.Nodes;
using Dialect.Nodes;
using Dialect.Values;
using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialect.Tests.Editor
{
    public sealed class DialectBuiltInNodeTests
    {
        const string GraphPath = "Assets/__DialectBuiltInNodeTest.dlg";

        sealed class SettingsHolder : ScriptableObject
        {
            public DialectCompareSettings settings = new(DialectCompareType.String, DialectComparisonOperator.Equal);
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(GraphPath);

        [Test]
        public void WaitNodesExposeTypedInlinePortsAndCompileAsFocusedFlowNodes()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(GraphPath);
            var start = new StartNode();
            var wait = new WaitNode();
            var waitUntil = new WaitUntilNode();
            var waitForResume = new WaitForResumeNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(wait);
            graph.AddNode(waitUntil);
            graph.AddNode(waitForResume);
            graph.AddNode(end);
            Assert.That(wait.GetInputPortByName(WaitNode.DurationPort).DataType, Is.EqualTo(typeof(float)));
            Assert.That(waitUntil.GetInputPortByName(WaitUntilNode.ConditionPort).DataType, Is.EqualTo(typeof(bool)));
            Assert.That(wait.GetInputPortByName(WaitNode.DurationPort).TrySetValue(0f), Is.True);
            Assert.That(waitUntil.GetInputPortByName(WaitUntilNode.ConditionPort).TrySetValue(true), Is.True);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), wait.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(wait.GetOutputPortByName(DialectNode.FlowOutput), waitUntil.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(waitUntil.GetOutputPortByName(DialectNode.FlowOutput), waitForResume.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(waitForResume.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));

            var runtime = SaveAndImport(graph);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            Assert.That(runtime.Nodes, Has.Some.InstanceOf<WaitRuntimeNode>());
            Assert.That(runtime.Nodes, Has.Some.InstanceOf<WaitUntilRuntimeNode>());
            Assert.That(runtime.Nodes, Has.Some.InstanceOf<WaitForResumeRuntimeNode>());
        }

        [Test]
        public void CompareChangesBothInputPortsAndNormalizesInvalidOperator()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(GraphPath);
            var compare = new CompareNode();
            graph.AddNode(compare);
            Assert.That(compare.GetNodeOptionByName(CompareNode.SettingsOption).TrySetValue(
                new DialectCompareSettings(DialectCompareType.Integer, DialectComparisonOperator.Greater)), Is.True);
            Assert.That(compare.GetInputPortByName(CompareNode.LeftPort).DataType, Is.EqualTo(typeof(int)));
            Assert.That(compare.GetInputPortByName(CompareNode.RightPort).DataType, Is.EqualTo(typeof(int)));
            Assert.That(compare.ValueType, Is.EqualTo(typeof(bool)));

            Assert.That(compare.GetNodeOptionByName(CompareNode.SettingsOption).TrySetValue(
                new DialectCompareSettings(DialectCompareType.String, DialectComparisonOperator.Greater)), Is.True);
            Assert.That(compare.GetInputPortByName(CompareNode.LeftPort).DataType, Is.EqualTo(typeof(string)));
            Assert.That(compare.GetNodeOptionByName(CompareNode.SettingsOption)
                .TryGetValue(out DialectCompareSettings settings), Is.True);
            Assert.That(settings.Operator, Is.EqualTo(DialectComparisonOperator.Equal));
        }

        [Test]
        public void CompareDrawerOffersOnlyOperatorsValidForSelectedType()
        {
            var holder = ScriptableObject.CreateInstance<SettingsHolder>();
            try
            {
                var serialized = new SerializedObject(holder);
                var root = new DialectCompareSettingsDrawer().CreatePropertyGUI(serialized.FindProperty("settings"));
                var fields = root.Query<DropdownField>().ToList();
                Assert.That(fields, Has.Count.EqualTo(1));
                Assert.That(fields[0].choices, Is.EquivalentTo(new[] { "Equal", "Not Equal" }));
            }
            finally { Object.DestroyImmediate(holder); }
        }

        [Test]
        public void SelectKeepsPortsTypedAndFeedsDialogueTextWithoutDuplicateFlow()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(GraphPath);
            var start = new StartNode();
            var select = new SelectNode();
            var dialogue = new DialogueNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(select);
            graph.AddNode(dialogue);
            graph.AddNode(end);
            select.GetInputPortByName(SelectNode.ConditionPort).TrySetValue(true);
            select.GetInputPortByName(SelectNode.TruePort).TrySetValue("Powered");
            select.GetInputPortByName(SelectNode.FalsePort).TrySetValue("Dark");
            dialogue.GetInputPortByName(DialogueNode.SpeakerPort).TrySetValue(DialectText.Inline("Guide"));
            Assert.That(graph.Connect(select.GetOutputPortByName(DialectValueNode.ValueOutput),
                dialogue.GetInputPortByName(DialogueNode.TextPort)), Is.True);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), dialogue.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(dialogue.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));

            var runtime = SaveAndImport(graph);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            var owner = new GameObject("Dialect Select Authoring Test");
            try
            {
                var line = default(DialectLine);
                var director = owner.AddComponent<DialectDirector>();
                director.LinePresented += value => line = value;
                director.Play(runtime);
                Assert.That(line.Text, Is.EqualTo("Powered"));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(DialectValueType.String, typeof(string))]
        [TestCase(DialectValueType.LocalizedString, typeof(UnityEngine.Localization.LocalizedString))]
        [TestCase(DialectValueType.Boolean, typeof(bool))]
        [TestCase(DialectValueType.Integer, typeof(int))]
        [TestCase(DialectValueType.Float, typeof(float))]
        [TestCase(DialectValueType.Object, typeof(Object))]
        public void SelectChangesAllValuePortsToSelectedType(DialectValueType valueType, System.Type expected)
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(GraphPath);
            var select = new SelectNode();
            graph.AddNode(select);
            select.GetNodeOptionByName(SelectNode.TypeOption).TrySetValue(valueType);
            Assert.That(select.GetInputPortByName(SelectNode.TruePort).DataType, Is.EqualTo(expected));
            Assert.That(select.GetInputPortByName(SelectNode.FalsePort).DataType, Is.EqualTo(expected));
            Assert.That(select.GetOutputPortByName(DialectValueNode.ValueOutput).DataType, Is.EqualTo(expected));
        }

        [Test]
        public void ModifyVariableShowsOnlyValidTypedControlsAndCompiles()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(GraphPath);
            var score = graph.CreateVariable("Score", 2, VariableKind.Local);
            var enabled = graph.CreateVariable("Enabled", false, VariableKind.Local);
            var start = new StartNode();
            var modify = new ModifyVariableNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(modify);
            graph.AddNode(end);
            modify.GetNodeOptionByName(ModifyVariableNode.SettingsOption)
                .TrySetValue(new DialectModifySettings(DialectVariableTarget.Local(score.ID.ToString())));
            Assert.That(modify.GetInputPortByName(ModifyVariableNode.OperandPort).DataType, Is.EqualTo(typeof(int)));
            modify.GetInputPortByName(ModifyVariableNode.OperandPort).TrySetValue(3);

            modify.GetNodeOptionByName(ModifyVariableNode.SettingsOption)
                .TrySetValue(new DialectModifySettings(DialectVariableTarget.Local(enabled.ID.ToString())));
            Assert.That(modify.GetInputPortByName(ModifyVariableNode.OperandPort), Is.Null);

            modify.GetNodeOptionByName(ModifyVariableNode.SettingsOption)
                .TrySetValue(new DialectModifySettings(DialectVariableTarget.Local(score.ID.ToString())));
            modify.GetInputPortByName(ModifyVariableNode.OperandPort).TrySetValue(3);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), modify.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(modify.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            var runtime = SaveAndImport(graph);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
        }

        static DialectRuntimeGraph SaveAndImport(DialectGraph graph)
        {
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(GraphPath);
        }
    }
}
