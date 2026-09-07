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
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Variables — Name, Type, Default Value"),
                elementHeightCallback = index => Matches(variables.GetArrayElementAtIndex(index))
                    ? EditorGUI.GetPropertyHeight(variables.GetArrayElementAtIndex(index), true) + 6 : 0,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = variables.GetArrayElementAtIndex(index);
                    if (!Matches(element)) return;
                    rect.y += 2;
                    EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            search = EditorGUILayout.TextField(EditorGUIUtility.IconContent("Search Icon"), search);
            list.draggable = string.IsNullOrWhiteSpace(search);
            list.DoLayoutList();
            if (GUILayout.Button("Add Variable")) ShowAddMenu();
            if (list.index >= 0 && GUILayout.Button("Duplicate Selected")) Duplicate(list.index);
            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var item in targets)
                    if (item is DialectBlackboard board) board.RepairVariableIds();
            }
            DrawValidation();
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
            var diagnostics = new System.Collections.Generic.List<string>();
            board.GetDiagnostics(diagnostics);
            foreach (var diagnostic in diagnostics) EditorGUILayout.HelpBox(diagnostic, MessageType.Warning);
        }

        bool Matches(SerializedProperty element)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            var name = element.FindPropertyRelative("name").stringValue;
            var value = element.FindPropertyRelative("defaultValue").managedReferenceValue as DialectValue;
            return name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (value?.Type.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
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
