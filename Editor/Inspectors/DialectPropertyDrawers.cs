using System;
using System.Linq;
using Dialect.Blackboards;
using UnityEditor;
using UnityEngine;

namespace Dialect.Editor.Inspectors
{
    [CustomPropertyDrawer(typeof(DialectVariableDefinition))]
    public sealed class DialectVariableDefinitionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("defaultValue");
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUI.GetPropertyHeight(value, true) + 8;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var name = property.FindPropertyRelative("name");
            var value = property.FindPropertyRelative("defaultValue");
            name.stringValue = EditorGUI.TextField(new Rect(line.x, line.y, line.width * .62f, line.height), name.stringValue);
            var type = GetType(value.managedReferenceValue);
            var selected = (DialectValueType)EditorGUI.EnumPopup(new Rect(line.x + line.width * .64f, line.y, line.width * .36f, line.height), type);
            if (selected != type) value.managedReferenceValue = CreateValue(selected);
            line.y += line.height + 3;
            EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, EditorGUI.GetPropertyHeight(value, true)), value, new GUIContent("Default"), true);
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
            var names = board.Variables.Select(variable => variable?.Name ?? "Missing").ToArray();
            var current = Math.Max(0, Array.FindIndex(board.Variables.ToArray(), variable => variable?.Id == idProperty.stringValue));
            using (new EditorGUI.DisabledScope(names.Length == 0))
            {
                var selected = EditorGUI.Popup(line, "Variable", current, names.Length == 0 ? new[] { "No variables" } : names);
                if (names.Length > 0) idProperty.stringValue = board.Variables[selected].Id;
            }
        }
    }
}
