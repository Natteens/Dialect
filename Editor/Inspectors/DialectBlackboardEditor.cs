using System;
using Dialect.Blackboards;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Dialect.Editor.Inspectors
{
    [CustomEditor(typeof(DialectBlackboard))]
    public sealed class DialectBlackboardEditor : UnityEditor.Editor
    {
        ReorderableList list;
        string search = string.Empty;

        void OnEnable()
        {
            var variables = serializedObject.FindProperty("variables");
            list = new ReorderableList(serializedObject, variables, true, true, false, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Variables"),
                elementHeightCallback = index => EditorGUI.GetPropertyHeight(variables.GetArrayElementAtIndex(index), true) + 6,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = variables.GetArrayElementAtIndex(index);
                    var name = element.FindPropertyRelative("name").stringValue;
                    if (!string.IsNullOrWhiteSpace(search) && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    { EditorGUI.LabelField(rect, $"{name} (filtered)", EditorStyles.miniLabel); return; }
                    rect.y += 2;
                    EditorGUI.PropertyField(rect, element, new GUIContent(string.IsNullOrWhiteSpace(name) ? "Unnamed" : name), true);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            search = EditorGUILayout.TextField(EditorGUIUtility.IconContent("Search Icon"), search);
            list.DoLayoutList();
            if (GUILayout.Button("Add Variable")) ShowAddMenu();
            if (list.index >= 0 && GUILayout.Button("Duplicate Selected")) Duplicate(list.index);
            DrawValidation();
            serializedObject.ApplyModifiedProperties();
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (DialectValueType type in Enum.GetValues(typeof(DialectValueType)))
                menu.AddItem(new GUIContent(type.ToString()), false, () => Add(type));
            menu.ShowAsContext();
        }

        void Add(DialectValueType type)
        {
            serializedObject.Update();
            var variables = serializedObject.FindProperty("variables");
            var index = variables.arraySize++;
            var element = variables.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("id").stringValue = Guid.NewGuid().ToString("N");
            element.FindPropertyRelative("name").stringValue = type.ToString();
            element.FindPropertyRelative("defaultValue").managedReferenceValue = CreateValue(type);
            serializedObject.ApplyModifiedProperties();
            list.index = index;
        }

        void Duplicate(int index)
        {
            var variables = serializedObject.FindProperty("variables");
            variables.InsertArrayElementAtIndex(index);
            var copy = variables.GetArrayElementAtIndex(index + 1);
            copy.FindPropertyRelative("id").stringValue = Guid.NewGuid().ToString("N");
            copy.FindPropertyRelative("name").stringValue += " Copy";
            serializedObject.ApplyModifiedProperties();
        }

        void DrawValidation()
        {
            var board = (DialectBlackboard)target;
            var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in board.Variables)
                if (variable == null || string.IsNullOrWhiteSpace(variable.Name) || !names.Add(variable.Name))
                { EditorGUILayout.HelpBox("Variables need unique, non-empty names.", MessageType.Warning); return; }
        }

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
}
