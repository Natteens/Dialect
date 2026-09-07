using System.Linq;
using Dialect.Core;
using Dialect.Blackboards;
using Dialect.Editor;
using Dialect.Editor.Nodes;
using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Dialect.Tests.Editor
{
    public sealed class DialectGraphTests
    {
        const string TestPath = "Assets/__DialectGraphTest.dlg";
        const string TestBoardPath = "Assets/__DialectGraphTestBoard.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestPath);
            AssetDatabase.DeleteAsset(TestBoardPath);
        }

        [Test]
        public void EmptyGraphCreatesConnectedStartAndEnd()
        {
            var graph = DialectGraph.CreateInitialized(TestPath);
            var start = graph.GetNodes().OfType<StartNode>().Single();
            Assert.That(graph.GetNodes().OfType<EndNode>().Count(), Is.EqualTo(1));
            Assert.That(start.GetOutputPortByName(DialectNode.FlowOutput).IsConnected, Is.True);

            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            Assert.That(runtime.GraphId, Is.EqualTo(graph.ID.ToString()),
                "Runtime visualization must use Graph.ID, not the .dlg asset GUID.");
            Assert.That(runtime.GraphId, Is.Not.EqualTo(graph.AssetGuid.ToString()));
        }

        [Test]
        public void MissingStartImportsStableInvalidRuntimeAsset()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            graph.AddNode(new EndNode());
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.False);
            Assert.That(runtime.Diagnostics, Has.Some.Contains("Start"));
        }

        [Test]
        public void LocalStringVariableConnectsToDialogueAndCompilesDefault()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var dialogue = new DialogueNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(dialogue);
            graph.AddNode(end);
            var variable = graph.CreateVariable("SpeakerName", "Mara", VariableKind.Local);
            var variableNode = (INode)graph.AddVariableNode(variable, new Vector2(120, 360));
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), dialogue.GetInputPortByName(DialectNode.FlowInput));
            Assert.That(graph.Connect(variableNode.GetOutputPort(0), dialogue.GetInputPortByName(DialogueNode.SpeakerPort)), Is.True);
            dialogue.GetInputPortByName(DialogueNode.TextPort).TrySetValue(DialectText.Inline("Hello"));
            graph.Connect(dialogue.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);

            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            Assert.That(runtime.LocalVariables, Has.Count.EqualTo(1));
            var owner = new GameObject("Dialect Local Variable Test");
            try
            {
                DialectLine line = default;
                var director = owner.AddComponent<DialectDirector>();
                director.LinePresented += value => line = value;
                director.Play(runtime);
                Assert.That(line.Speaker, Is.EqualTo("Mara"));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void LocalVariablesCompileAllBuiltInTypes()
        {
            var graph = DialectGraph.CreateInitialized(TestPath);
            graph.CreateVariable("String", "value", VariableKind.Local);
            graph.CreateVariable("Localized", new UnityEngine.Localization.LocalizedString(), VariableKind.Local);
            graph.CreateVariable("Bool", true, VariableKind.Local);
            graph.CreateVariable("Int", 4, VariableKind.Local);
            graph.CreateVariable("Float", 2.5f, VariableKind.Local);
            graph.CreateVariable("Object", (UnityEngine.Object)null, VariableKind.Local);
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            Assert.That(runtime.LocalVariables, Has.Count.EqualTo(6));
        }

        [Test]
        public void NewGraphCopiesProjectDefaultBoardsOnce()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var settings = DialectProjectSettings.instance;
            var serialized = new SerializedObject(settings);
            var defaults = serialized.FindProperty("defaultBlackboards");
            serialized.Update();
            var previous = new UnityEngine.Object[defaults.arraySize];
            for (var i = 0; i < defaults.arraySize; i++) previous[i] = defaults.GetArrayElementAtIndex(i).objectReferenceValue;
            try
            {
                defaults.ClearArray();
                defaults.InsertArrayElementAtIndex(0);
                defaults.GetArrayElementAtIndex(0).objectReferenceValue = board;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var graph = DialectGraph.CreateInitialized(TestPath);
                Assert.That(graph.Blackboards, Has.Count.EqualTo(1));
                Assert.That(graph.Blackboards[0], Is.SameAs(board));
            }
            finally
            {
                serialized.Update();
                defaults.ClearArray();
                for (var i = 0; i < previous.Length; i++)
                {
                    defaults.InsertArrayElementAtIndex(i);
                    defaults.GetArrayElementAtIndex(i).objectReferenceValue = previous[i];
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Object.DestroyImmediate(board);
            }
        }

        [Test]
        public void MissingEndImportsStableInvalidRuntimeAsset()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            graph.AddNode(new StartNode());
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.False);
            Assert.That(runtime.Diagnostics, Has.Some.Contains("End"));
        }

        [Test]
        public void MultipleStartsImportStableInvalidRuntimeAsset()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            graph.AddNode(new StartNode());
            graph.AddNode(new StartNode());
            graph.AddNode(new EndNode());
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.False);
            Assert.That(runtime.Diagnostics, Has.Some.Contains("exactly one Start"));
        }

        [Test]
        public void DialogueCanAuthorInlineLineWithOptionalSpeaker()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var dialogue = new DialogueNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(dialogue);
            graph.AddNode(end);
            dialogue.GetInputPortByName(DialogueNode.SpeakerPort).TrySetValue(DialectText.Inline(string.Empty));
            dialogue.GetInputPortByName(DialogueNode.TextPort).TrySetValue(DialectText.Inline("Where am I?"));
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), dialogue.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(dialogue.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
        }

        [Test]
        public void StrictImportRejectsDialogueWithoutLineButNotEmptySpeaker()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var dialogue = new DialogueNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(dialogue);
            graph.AddNode(end);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), dialogue.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(dialogue.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.False);
            Assert.That(runtime.Diagnostics, Has.Some.Contains("Line is empty"));
            Assert.That(runtime.Diagnostics, Has.None.Contains("Speaker is empty"));
        }

        [Test]
        public void BranchCompilesConnectedTrueAndFalseOutputs()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var branch = new BranchNode();
            var trueEnd = new EndNode();
            var falseEnd = new EndNode();
            graph.AddNode(start);
            graph.AddNode(branch);
            graph.AddNode(trueEnd);
            graph.AddNode(falseEnd);
            branch.GetInputPortByName(BranchNode.ConditionPort).TrySetValue(true);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), branch.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(branch.GetOutputPortByName(BranchNode.TruePort), trueEnd.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(branch.GetOutputPortByName(BranchNode.FalsePort), falseEnd.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
        }

        [Test]
        public void IncompleteChoiceIsQuietInLiveValidationAndRejectedByStrictImport()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var choice = new ChoiceNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(choice);
            graph.AddNode(end);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), choice.GetInputPortByName(DialectNode.FlowInput));

            Assert.DoesNotThrow(() => graph.Validate(new GraphLogger(), DialectValidationMode.Live));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);

            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.False);
            Assert.That(runtime.Diagnostics, Has.Some.Contains("Choice 1 is empty"));
            Assert.That(runtime.Diagnostics, Has.Some.Contains("choice0Target"));
        }

        [Test]
        public void ChoiceAddAndRemovePreserveExistingSemanticPortsAfterReload()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var choice = new ChoiceNode();
            var firstEnd = new EndNode();
            var secondEnd = new EndNode();
            graph.AddNode(start);
            graph.AddNode(choice);
            graph.AddNode(firstEnd);
            graph.AddNode(secondEnd);
            choice.GetInputPortByName(ChoiceNode.TextName(0)).TrySetValue(DialectText.Inline("Open the door"));
            choice.GetInputPortByName(ChoiceNode.TextName(1)).TrySetValue(DialectText.Inline("Leave"));
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), choice.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(choice.GetOutputPortByName(ChoiceNode.TargetName(0)), firstEnd.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(choice.GetOutputPortByName(ChoiceNode.TargetName(1)), secondEnd.GetInputPortByName(DialectNode.FlowInput));
            var firstId = choice.GetOutputPortByName(ChoiceNode.TargetName(0)).ID;
            var secondId = choice.GetOutputPortByName(ChoiceNode.TargetName(1)).ID;

            Assert.That(choice.GetNodeOptionByName(ChoiceNode.CountOption).TrySetValue(new DialectPortCount(3)), Is.True);
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(2)), Is.Not.Null);
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(0)).ID, Is.EqualTo(firstId));
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(1)).ID, Is.EqualTo(secondId));
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(0)).IsConnected, Is.True);
            Assert.That(choice.GetNodeOptionByName(ChoiceNode.CountOption).TrySetValue(new DialectPortCount(2)), Is.True);
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(2)), Is.Null);
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(0)).IsConnected, Is.True);
            Assert.That(choice.GetOutputPortByName(ChoiceNode.TargetName(1)).IsConnected, Is.True);

            GraphDatabase.SaveGraph(graph);
            var reloaded = GraphDatabase.LoadGraph<DialectGraph>(TestPath);
            var reloadedChoice = reloaded.GetNodes().OfType<ChoiceNode>().Single();
            Assert.That(reloadedChoice.GetInputPortByName(ChoiceNode.TextName(0)).TryGetValue(out DialectText firstText), Is.True);
            Assert.That(firstText.InlineValue, Is.EqualTo("Open the door"));
            Assert.That(reloadedChoice.GetOutputPortByName(ChoiceNode.TargetName(0)).IsConnected, Is.True);
            Assert.That(reloadedChoice.GetOutputPortByName(ChoiceNode.TargetName(1)).IsConnected, Is.True);
        }

        [Test]
        public void BranchAcceptsNativeBooleanConstant()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var branch = new BranchNode();
            var trueEnd = new EndNode();
            var falseEnd = new EndNode();
            graph.AddNode(start);
            graph.AddNode(branch);
            graph.AddNode(trueEnd);
            graph.AddNode(falseEnd);
            var constant = graph.CreateConstantNode(new Vector2(100, 320), true);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), branch.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(constant.GetOutputPort(0), branch.GetInputPortByName(BranchNode.ConditionPort));
            graph.Connect(branch.GetOutputPortByName(BranchNode.TruePort), trueEnd.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(branch.GetOutputPortByName(BranchNode.FalsePort), falseEnd.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
        }

        [Test]
        public void SetVariableCompilesLocalTargetAndUpdatesOnlySessionValue()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var variable = graph.CreateVariable("Score", 2, VariableKind.Local);
            var start = new StartNode();
            var set = new SetVariableNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(set);
            graph.AddNode(end);
            Assert.That(set.GetNodeOptionByName(SetVariableNode.TargetOption)
                .TrySetValue(DialectVariableTarget.Local(variable.ID.ToString())), Is.True);
            Assert.That(set.GetInputPortByName(SetVariableNode.ValuePort).DataType, Is.EqualTo(typeof(int)));
            set.GetInputPortByName(SetVariableNode.ValuePort).TrySetValue(9);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), set.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(set.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));

            var owner = new GameObject("Dialect Set Variable Editor Test");
            try
            {
                var director = owner.AddComponent<DialectDirector>();
                director.Play(runtime);
                Assert.That(director.Session.Variables.TryGet(variable.ID.ToString(), out int value), Is.True);
                Assert.That(value, Is.EqualTo(9));
                Assert.That(runtime.LocalVariables.Single().DefaultValue.BoxedValue, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void SharedBooleanVariableConnectsDirectlyToBranch()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var enabled = board.AddVariable("Door Is Open", new DialectBoolValue(true));
            AssetDatabase.CreateAsset(board, TestBoardPath);
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            graph.LinkBlackboard(board);
            var start = new StartNode();
            var shared = new SharedVariableNode();
            var branch = new BranchNode();
            var trueEnd = new EndNode();
            var falseEnd = new EndNode();
            graph.AddNode(start);
            graph.AddNode(shared);
            graph.AddNode(branch);
            graph.AddNode(trueEnd);
            graph.AddNode(falseEnd);
            shared.GetInputPortByName(SharedVariableNode.ReferencePort)
                .TrySetValue(new DialectVariableReference(board, enabled.Id));
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), branch.GetInputPortByName(DialectNode.FlowInput));
            Assert.That(graph.Connect(shared.GetOutputPortByName(DialectValueNode.ValueOutput),
                branch.GetInputPortByName(BranchNode.ConditionPort)), Is.True);
            graph.Connect(branch.GetOutputPortByName(BranchNode.TruePort), trueEnd.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(branch.GetOutputPortByName(BranchNode.FalsePort), falseEnd.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
        }
    }
}
