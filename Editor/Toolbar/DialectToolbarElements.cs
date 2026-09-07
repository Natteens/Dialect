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
            graph.RequestStrictValidation();
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
            text = "Runtime Debug";
            tooltip = "Visualize a currently running Dialect session in this graph.";
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
            EditorGUILayout.HelpBox("Local variables live in this .dlg. Shared Boards are reusable Dialect Blackboard assets.", MessageType.Info);
            EditorGUILayout.Space();
            for (var i = 0; i < blackboards.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var previous = blackboards[i];
                    var selected = (DialectBlackboard)EditorGUILayout.ObjectField(previous, typeof(DialectBlackboard), false);
                    if (selected != previous && selected != null && blackboards.Contains(selected))
                        EditorGUILayout.HelpBox("Already linked", MessageType.Warning);
                    else blackboards[i] = selected;
                    using (new EditorGUI.DisabledScope(blackboards[i] == null))
                        if (GUILayout.Button("Open", GUILayout.Width(44))) Selection.activeObject = blackboards[i];
                    using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("▲", GUILayout.Width(28))) Move(i, i - 1);
                    using (new EditorGUI.DisabledScope(i == blackboards.Count - 1)) if (GUILayout.Button("▼", GUILayout.Width(28))) Move(i, i + 1);
                    if (GUILayout.Button("−", GUILayout.Width(28))) { blackboards.RemoveAt(i); Apply(); break; }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add")) { blackboards.Add(null); Repaint(); }
                if (GUILayout.Button("Create Blackboard")) CreateBlackboard();
            }
            if (GUI.changed) Apply();
        }

        void CreateBlackboard()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Dialect Blackboard", "DialectBlackboard", "asset",
                "Choose where to create the reusable shared blackboard.");
            if (string.IsNullOrEmpty(path)) return;
            var board = CreateInstance<DialectBlackboard>();
            AssetDatabase.CreateAsset(board, path);
            AssetDatabase.SaveAssets();
            blackboards.Add(board);
            Apply();
            Selection.activeObject = board;
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
