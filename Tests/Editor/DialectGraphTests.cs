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

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TestPath);

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
    }
}
