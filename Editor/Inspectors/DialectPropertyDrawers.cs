using System;
using System.Linq;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Editor.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialect.Editor.Inspectors
{
    [CustomPropertyDrawer(typeof(DialectVariableDefinition))]
    public sealed class DialectVariableDefinitionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = DrawerUI.Root("dialect-variable-definition");
            var name = property.FindPropertyRelative("name");
            var value = property.FindPropertyRelative("defaultValue");
            var nameField = new PropertyField(name, "Name")
            {
                tooltip = "Name used to find this variable while authoring. Renaming does not break references."
            };
            root.Add(nameField);
            var typeField = new EnumField("Type", GetType(value.managedReferenceValue));
            root.Add(typeField);
            var valueHost = new VisualElement();
            root.Add(valueHost);
            void RebuildValue()
            {
                valueHost.Clear();
                var serializedValue = value.FindPropertyRelative("value");
                if (serializedValue != null)
                {
                    var valueField = new PropertyField(serializedValue, "Default Value")
                    {
                        tooltip = "Initial value copied into a dialogue session."
                    };
                    valueHost.Add(valueField);
                }
            }
            typeField.RegisterValueChangedCallback(evt =>
            {
                var selected = (DialectValueType)evt.newValue;
                if (selected == GetType(value.managedReferenceValue)) return;
                value.managedReferenceValue = CreateValue(selected);
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                RebuildValue();
            });
            RebuildValue();
            return root;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("defaultValue");
            var rawValue = value.FindPropertyRelative("value");
            var valueHeight = rawValue != null ? EditorGUI.GetPropertyHeight(rawValue, true) : EditorGUIUtility.singleLineHeight;
            return EditorGUIUtility.singleLineHeight * 2 + valueHeight + 10;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = new Rect(position.x, position.y + 2, position.width, EditorGUIUtility.singleLineHeight);
            var name = property.FindPropertyRelative("name");
            var value = property.FindPropertyRelative("defaultValue");
            name.stringValue = EditorGUI.TextField(line, new GUIContent("Name", "Name used to find this variable while authoring. Renaming does not break references."), name.stringValue);
            line.y += line.height + 3;
            var type = GetType(value.managedReferenceValue);
            var selected = (DialectValueType)EditorGUI.EnumPopup(line, "Type", type);
            if (selected != type) value.managedReferenceValue = CreateValue(selected);
            line.y += line.height + 3;
            var rawValue = value.FindPropertyRelative("value");
            if (rawValue != null)
            {
                line.height = EditorGUI.GetPropertyHeight(rawValue, true);
                EditorGUI.PropertyField(line, rawValue, new GUIContent("Default Value", "Initial value copied into a dialogue session."), true);
            }
            else EditorGUI.LabelField(line, "Default Value", "Unavailable");
        }

        static DialectValueType GetType(object value) => value is DialectValue dialect ? dialect.Type : DialectValueType.String;
        static DialectValue CreateValue(DialectValueType type) => type switch
        {
            DialectValueType.LocalizedString => new DialectLocalizedStringValue(),
            DialectValueType.Boolean => new DialectBoolValue(),
            DialectValueType.Integer => new DialectIntValue(),
            DialectValueType.Float => new DialectFloatValue(),
            DialectValueType.Object => new DialectObjectValue(),
            _ => new DialectStringValue()
        };
    }

    [CustomPropertyDrawer(typeof(DialectVariableReference))]
    public sealed class DialectVariableReferenceDrawer : PropertyDrawer
    {
        readonly DialectGraph graphOverride;

        public DialectVariableReferenceDrawer() { }
        public DialectVariableReferenceDrawer(DialectGraph graph) => graphOverride = graph;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = DrawerUI.Root("dialect-variable-reference");
            var boardProperty = property.FindPropertyRelative("blackboard");
            var idProperty = property.FindPropertyRelative("variableId");
            var graph = graphOverride ?? DrawerUI.CurrentGraph();
            if (graph == null)
            {
                root.Add(new PropertyField(boardProperty, "Blackboard"));
                var fallback = new HelpBox("Open this field in a Dialect graph to choose from its linked Shared Boards.", HelpBoxMessageType.Info);
                root.Add(fallback);
                return root;
            }

            var items = new List<ReferenceChoice> { new("None", null, string.Empty) };
            var multipleBoards = graph.Blackboards.Count > 1;
            foreach (var board in graph.Blackboards)
            {
                if (board == null) continue;
                foreach (var variable in board.Variables)
                {
                    if (variable == null) continue;
                    var name = multipleBoards ? $"{board.name} / {variable.Name}" : variable.Name;
                    items.Add(new ReferenceChoice(name, board, variable.Id));
                }
            }
            var current = items.FindIndex(item => item.Board == boardProperty.objectReferenceValue && item.Id == idProperty.stringValue);
            if (current < 0 && (boardProperty.objectReferenceValue != null || !string.IsNullOrEmpty(idProperty.stringValue)))
            {
                var oldBoard = boardProperty.objectReferenceValue as DialectBlackboard;
                items.Add(new ReferenceChoice($"Missing / {(oldBoard != null ? oldBoard.name : "Blackboard")}", oldBoard, idProperty.stringValue));
                current = items.Count - 1;
            }
            current = Mathf.Max(0, current);
            var labels = items.Select(item => item.Name).ToList();
            var popup = new DropdownField("Variable", labels, current)
            {
                tooltip = "Shared variable linked to this graph. The board and stable ID are stored automatically."
            };
            popup.RegisterValueChangedCallback(evt =>
            {
                var selected = items[Mathf.Max(0, labels.IndexOf(evt.newValue))];
                ApplySelection(property, selected.Board, selected.Id);
            });
            root.Add(popup);
            if (items.Count == 1) root.Add(new HelpBox("Link a Dialect Blackboard with Shared Boards, then choose a variable here.", HelpBoxMessageType.Info));
            return root;
        }

        public static void ApplySelection(SerializedProperty property, DialectBlackboard board, string variableId)
        {
            property.FindPropertyRelative("blackboard").objectReferenceValue = board;
            property.FindPropertyRelative("variableId").stringValue = variableId ?? string.Empty;
            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2 + 3;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var boardProperty = property.FindPropertyRelative("blackboard");
            var idProperty = property.FindPropertyRelative("variableId");
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(line, boardProperty, label);
            line.y += line.height + 3;
            var board = boardProperty.objectReferenceValue as DialectBlackboard;
            if (board == null) { EditorGUI.LabelField(line, "Variable", "Select a blackboard first"); return; }
            var variables = board.Variables.ToArray();
            var names = new string[variables.Length + 1];
            names[0] = "None";
            for (var i = 0; i < variables.Length; i++) names[i + 1] = variables[i]?.Name ?? "Missing";
            var current = Array.FindIndex(variables, variable => variable?.Id == idProperty.stringValue) + 1;
            using (new EditorGUI.DisabledScope(names.Length == 0))
            {
                var selected = EditorGUI.Popup(line, "Variable", current, names);
                if (selected != current) idProperty.stringValue = selected == 0 ? string.Empty : variables[selected - 1].Id;
            }
        }

        readonly struct ReferenceChoice
        {
            public ReferenceChoice(string name, DialectBlackboard board, string id) { Name = name; Board = board; Id = id; }
            public string Name { get; }
            public DialectBlackboard Board { get; }
            public string Id { get; }
        }
    }

    [CustomPropertyDrawer(typeof(DialectText))]
    public sealed class DialectTextDrawer : PropertyDrawer
    {
        static readonly List<string> Modes = new() { "Inline", "Localized", "Variable" };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = DrawerUI.Root("dialect-text");
            var source = property.FindPropertyRelative("source");
            var inline = property.FindPropertyRelative("inlineText");
            var localized = property.FindPropertyRelative("localizedText");
            var variable = property.FindPropertyRelative("variable");
            var mode = new PopupField<string>(Modes, Mathf.Clamp(source.enumValueIndex, 0, Modes.Count - 1));
            mode.AddToClassList("dialect-text__mode");
            mode.tooltip = "Inline writes directly here. Localized selects a String Table entry. Variable is for serialized references; graph ports are preferred for dynamic text.";
            var inlineField = new TextField { multiline = true, tooltip = "Text written directly in this node." };
            inlineField.BindProperty(inline);
            inlineField.AddToClassList("dialect-text__inline");
            var localizedField = new PropertyField(localized, string.Empty);
            localizedField.AddToClassList("dialect-text__localized");
            var variableField = new PropertyField(variable, string.Empty);
            variableField.AddToClassList("dialect-text__variable");
            root.Add(mode);
            root.Add(inlineField);
            root.Add(localizedField);
            root.Add(variableField);
            void Refresh(int index)
            {
                inlineField.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                localizedField.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
                variableField.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            mode.RegisterValueChangedCallback(evt =>
            {
                source.enumValueIndex = Modes.IndexOf(evt.newValue);
                property.serializedObject.ApplyModifiedProperties();
                Refresh(source.enumValueIndex);
            });
            Refresh(source.enumValueIndex);
            return root;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var source = property.FindPropertyRelative("source").enumValueIndex;
            var value = property.FindPropertyRelative(source == 1 ? "localizedText" : source == 2 ? "variable" : "inlineText");
            return EditorGUIUtility.singleLineHeight + 3 + EditorGUI.GetPropertyHeight(value, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var source = property.FindPropertyRelative("source");
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            line = EditorGUI.PrefixLabel(line, label);
            source.enumValueIndex = EditorGUI.Popup(line, Mathf.Clamp(source.enumValueIndex, 0, Modes.Count - 1), Modes.ToArray());
            line.y += line.height + 3;
            var value = property.FindPropertyRelative(source.enumValueIndex == 1 ? "localizedText" : source.enumValueIndex == 2 ? "variable" : "inlineText");
            line.height = EditorGUI.GetPropertyHeight(value, true);
            EditorGUI.PropertyField(line, value, GUIContent.none, true);
        }
    }

    [CustomPropertyDrawer(typeof(DialectPortCount))]
    public sealed class DialectPortCountDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = DrawerUI.Root("dialect-count");
            var count = property.FindPropertyRelative("count");
            if (count.intValue < 1) count.intValue = 1;
            var number = new Label(count.intValue.ToString()) { tooltip = "Current item count." };
            number.AddToClassList("dialect-count__number");
            Button remove = null;
            remove = new Button(() => Change(-1)) { text = "− Remove Last", tooltip = "Removes only the last item so existing connections keep their meaning." };
            var add = new Button(() => Change(1)) { text = "+ Add", tooltip = "Adds a new item after existing items." };
            row.Add(number);
            row.Add(remove);
            row.Add(add);
            void Change(int delta)
            {
                count.intValue = Mathf.Clamp(count.intValue + delta, 1, 8);
                property.serializedObject.ApplyModifiedProperties();
                number.text = count.intValue.ToString();
                remove.SetEnabled(count.intValue > 1);
            }
            remove.SetEnabled(count.intValue > 1);
            return row;
        }
    }

    [CustomPropertyDrawer(typeof(DialectVariableTarget))]
    public sealed class DialectVariableTargetDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = DrawerUI.Root("dialect-variable-target");
            var graph = DrawerUI.CurrentGraph();
            if (graph == null)
            {
                root.Add(new HelpBox("Open this node in its Dialect graph to select a session variable.", HelpBoxMessageType.Info));
                return root;
            }
            var scope = property.FindPropertyRelative("scope");
            var localId = property.FindPropertyRelative("localVariableId");
            var boardProperty = property.FindPropertyRelative("blackboard");
            var sharedId = property.FindPropertyRelative("sharedVariableId");
            var choices = new List<TargetChoice> { new("None", DialectVariableScope.Local, string.Empty, null) };
            foreach (var variable in graph.GetVariables())
                if (variable != null) choices.Add(new TargetChoice($"Local / {variable.Name}", DialectVariableScope.Local, variable.ID.ToString(), null));
            foreach (var board in graph.Blackboards)
                if (board != null)
                    foreach (var variable in board.Variables)
                        if (variable != null) choices.Add(new TargetChoice($"{board.name} / {variable.Name}", DialectVariableScope.Shared, variable.Id, board));
            var current = choices.FindIndex(item => item.Scope == (DialectVariableScope)scope.enumValueIndex &&
                (item.Scope == DialectVariableScope.Local ? item.Id == localId.stringValue : item.Id == sharedId.stringValue && item.Board == boardProperty.objectReferenceValue));
            if (current < 0 && (!string.IsNullOrEmpty(localId.stringValue) || !string.IsNullOrEmpty(sharedId.stringValue) || boardProperty.objectReferenceValue != null))
            {
                var missingScope = (DialectVariableScope)scope.enumValueIndex;
                var missingBoard = boardProperty.objectReferenceValue as DialectBlackboard;
                var missingId = missingScope == DialectVariableScope.Local ? localId.stringValue : sharedId.stringValue;
                var missingLabel = missingScope == DialectVariableScope.Local ? "Missing / Local variable" : $"Missing / {(missingBoard != null ? missingBoard.name : "Blackboard")}";
                choices.Add(new TargetChoice(missingLabel, missingScope, missingId, missingBoard));
                current = choices.Count - 1;
            }
            if (current < 0) current = 0;
            var popup = new PopupField<TargetChoice>("Variable", choices, current)
            {
                formatListItemCallback = item => item.Name,
                formatSelectedValueCallback = item => item.Name,
                tooltip = "Local variables live in this .dlg. Shared variables come from linked Dialect Blackboards."
            };
            popup.RegisterValueChangedCallback(evt =>
            {
                scope.enumValueIndex = (int)evt.newValue.Scope;
                if (evt.newValue.Scope == DialectVariableScope.Local) localId.stringValue = evt.newValue.Id;
                else { boardProperty.objectReferenceValue = evt.newValue.Board; sharedId.stringValue = evt.newValue.Id; }
                property.serializedObject.ApplyModifiedProperties();
            });
            root.Add(popup);
            return root;
        }

        readonly struct TargetChoice
        {
            public TargetChoice(string name, DialectVariableScope scope, string id, DialectBlackboard board)
            { Name = name; Scope = scope; Id = id; Board = board; }
            public string Name { get; }
            public DialectVariableScope Scope { get; }
            public string Id { get; }
            public DialectBlackboard Board { get; }
        }
    }

    static class DrawerUI
    {
        const string StylesheetPath = "Packages/com.natteens.dialect/Editor/Styles/DialectAuthoring.uss";
        public static VisualElement Root(string className)
        {
            var root = new VisualElement();
            root.AddToClassList(className);
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            return root;
        }

        public static DialectGraph CurrentGraph()
        {
            if (EditorWindow.focusedWindow is IGraphWindow focused && focused.Graph is DialectGraph focusedGraph) return focusedGraph;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (window is IGraphWindow graphWindow && graphWindow.Graph is DialectGraph graph) return graph;
            return null;
        }
    }
}
