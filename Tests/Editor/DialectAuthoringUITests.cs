using Dialect.Blackboards;
using Dialect.Editor;
using Dialect.Editor.Inspectors;
using Dialect.Editor.Nodes;
using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialect.Tests.Editor
{
    public sealed class DialectAuthoringUITests
    {
        sealed class AuthoringHolder : ScriptableObject
        {
            public DialectText text = DialectText.Inline("Where am I?");
            public DialectPortCount count = new(2);
            public DialectVariableReference reference;
        }

        const string GraphPath = "Assets/__DialectAuthoringUITest.dlg";
        const string BoardPath = "Assets/__DialectAuthoringUITestBoard.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(GraphPath);
            AssetDatabase.DeleteAsset(BoardPath);
        }

        [Test]
        public void DialectTextDrawerBuildsVisualTreeAndPreservesInlineValueWhenSwitchingMode()
        {
            var holder = ScriptableObject.CreateInstance<AuthoringHolder>();
            try
            {
                var serialized = new SerializedObject(holder);
                var property = serialized.FindProperty("text");
                var root = new DialectTextDrawer().CreatePropertyGUI(property);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.Q<PopupField<string>>(), Is.Not.Null);
                Assert.That(root.Query<TextField>().ToList(), Has.Some.Matches<TextField>(field => field.multiline));
                property.FindPropertyRelative("source").enumValueIndex = 1;
                serialized.ApplyModifiedProperties();
                serialized.Update();
                Assert.That(property.FindPropertyRelative("source").enumValueIndex, Is.EqualTo(1));
                Assert.That(property.FindPropertyRelative("inlineText").stringValue, Is.EqualTo("Where am I?"));
            }
            finally { Object.DestroyImmediate(holder); }
        }

        [Test]
        public void PortCountDrawerBuildsAddRemoveControls()
        {
            var holder = ScriptableObject.CreateInstance<AuthoringHolder>();
            try
            {
                var serialized = new SerializedObject(holder);
                var root = new DialectPortCountDrawer().CreatePropertyGUI(serialized.FindProperty("count"));
                var buttons = root.Query<Button>().ToList();
                Assert.That(buttons, Has.Count.EqualTo(2));
                Assert.That(buttons, Has.Some.Matches<Button>(button => button.text == "+ Add"));
                Assert.That(buttons, Has.Some.Matches<Button>(button => button.text.Contains("Remove Last")));
            }
            finally { Object.DestroyImmediate(holder); }
        }

        [Test]
        public void BlackboardVariableDrawerUsesExplicitAuthoringLabels()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            board.AddVariable("Player Name", new DialectStringValue("Player"));
            try
            {
                var serialized = new SerializedObject(board);
                var property = serialized.FindProperty("variables").GetArrayElementAtIndex(0);
                var root = new DialectVariableDefinitionDrawer().CreatePropertyGUI(property);
                var propertyLabels = root.Query<UnityEditor.UIElements.PropertyField>().ToList();
                Assert.That(propertyLabels, Has.Some.Matches<UnityEditor.UIElements.PropertyField>(field => field.label == "Name"));
                Assert.That(propertyLabels, Has.Some.Matches<UnityEditor.UIElements.PropertyField>(field => field.label == "Default Value"));
                Assert.That(root.Query<EnumField>().ToList(), Has.Some.Matches<EnumField>(field => field.label == "Type"));
            }
            finally { Object.DestroyImmediate(board); }
        }

        [Test]
        public void SharedVariablePickerUsesLinkedBoardAndPersistsStableReference()
        {
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var definition = board.AddVariable("Player Name", new DialectStringValue("Player"));
            AssetDatabase.CreateAsset(board, BoardPath);
            var graph = GraphDatabase.CreateGraph<DialectGraph>(GraphPath);
            graph.LinkBlackboard(board);
            GraphDatabase.SaveGraph(graph);
            var holder = ScriptableObject.CreateInstance<AuthoringHolder>();
            try
            {
                var serialized = new SerializedObject(holder);
                var property = serialized.FindProperty("reference");
                var root = new DialectVariableReferenceDrawer(graph).CreatePropertyGUI(property);
                var picker = root.Q<DropdownField>();
                Assert.That(picker, Is.Not.Null);
                Assert.That(picker.choices, Does.Contain("Player Name"));
                Assert.That(picker.choices, Has.None.Contains(board.name + " /"),
                    "A single linked board must not be selected twice in the node UI.");

                DialectVariableReferenceDrawer.ApplySelection(property, board, definition.Id);
                serialized.Update();
                Assert.That(property.FindPropertyRelative("blackboard").objectReferenceValue, Is.SameAs(board));
                Assert.That(property.FindPropertyRelative("variableId").stringValue, Is.EqualTo(definition.Id));
            }
            finally { Object.DestroyImmediate(holder); }
        }
    }
}
