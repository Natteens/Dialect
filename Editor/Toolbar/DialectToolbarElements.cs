using System.Collections.Generic;
using Dialect.Blackboards;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialect.Editor
{
    [GraphToolbarElement("Dialect/Validate", typeof(DialectGraph), 100)]
    sealed class DialectValidateToolbarButton : EditorToolbarButton
    {
        public DialectValidateToolbarButton() : base(Validate)
        {
            text = "Validate";
            tooltip = "Save, import, and refresh Dialect diagnostics.";
        }

        static void Validate()
        {
            if (EditorWindow.focusedWindow is not IGraphWindow window || window.Graph is not DialectGraph graph) return;
            GraphDatabase.SaveGraph(graph);
            var path = AssetDatabase.GUIDToAssetPath(graph.AssetGuid.ToString());
            if (!string.IsNullOrEmpty(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    [GraphToolbarElement("Dialect/SharedBlackboards", typeof(DialectGraph), 110)]
    sealed class DialectBlackboardsToolbarButton : EditorToolbarButton
    {
        public DialectBlackboardsToolbarButton() : base(Open)
        {
            text = "Shared Boards";
            tooltip = "Edit the shared blackboards linked to this graph.";
        }

        static void Open()
        {
            if (EditorWindow.focusedWindow is IGraphWindow window && window.Graph is DialectGraph graph)
                DialectGraphBlackboardsWindow.Open(graph);
        }
    }

    [GraphToolbarElement("Dialect/Visualization", typeof(DialectGraph), 120)]
    sealed class DialectVisualizationToolbarToggle : EditorToolbarToggle
    {
        public DialectVisualizationToolbarToggle()
        {
            text = "Debug";
            tooltip = "Enable runtime node, wire, and value previews.";
            SetValueWithoutNotify(DialectProjectSettings.instance.VisualizationEnabled);
            INotifyValueChangedExtensions.RegisterValueChangedCallback<bool>(this,
                evt => DialectProjectSettings.instance.VisualizationEnabled = evt.newValue);
        }
    }

    sealed class DialectGraphBlackboardsWindow : EditorWindow
    {
        DialectGraph graph;
        readonly List<DialectBlackboard> blackboards = new();

        public static void Open(DialectGraph graph)
        {
            var window = GetWindow<DialectGraphBlackboardsWindow>(true, "Dialect Shared Blackboards");
            window.graph = graph;
            window.blackboards.Clear();
            window.blackboards.AddRange(graph.Blackboards);
            window.minSize = new Vector2(360, 180);
            window.Show();
        }

        void OnGUI()
        {
            if (graph == null) { EditorGUILayout.HelpBox("Open this window from a Dialect graph.", MessageType.Info); return; }
            EditorGUILayout.LabelField(graph.Name, EditorStyles.boldLabel);
            EditorGUILayout.Space();
            for (var i = 0; i < blackboards.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    blackboards[i] = (DialectBlackboard)EditorGUILayout.ObjectField(blackboards[i], typeof(DialectBlackboard), false);
                    using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("▲", GUILayout.Width(28))) Move(i, i - 1);
                    using (new EditorGUI.DisabledScope(i == blackboards.Count - 1)) if (GUILayout.Button("▼", GUILayout.Width(28))) Move(i, i + 1);
                    if (GUILayout.Button("−", GUILayout.Width(28))) { blackboards.RemoveAt(i); Apply(); break; }
                }
            }
            if (GUILayout.Button("Add Shared Blackboard")) { blackboards.Add(null); Repaint(); }
            if (GUI.changed) Apply();
        }

        void Move(int from, int to)
        {
            var value = blackboards[from];
            blackboards.RemoveAt(from);
            blackboards.Insert(to, value);
            Apply();
        }

        void Apply()
        {
            graph.UndoBeginRecordGraph("Edit shared blackboards");
            graph.SetBlackboards(blackboards);
            graph.UndoEndRecordGraph();
            GraphDatabase.SaveGraph(graph);
        }
    }
}
