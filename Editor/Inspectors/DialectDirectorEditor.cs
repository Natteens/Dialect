using Dialect.Core;
using UnityEditor;
using UnityEngine;

namespace Dialect.Editor.Inspectors
{
    [CustomEditor(typeof(DialectDirector))]
    public sealed class DialectDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var director = (DialectDirector)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            { EditorGUILayout.HelpBox("Runtime controls are available in Play Mode.", MessageType.Info); return; }
            using (new EditorGUI.DisabledScope(director.IsRunning))
                if (GUILayout.Button("Play Default Graph")) director.TryPlay(director.DefaultGraph);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(director.Session?.State != DialectPlaybackState.WaitingForAdvance))
                    if (GUILayout.Button("Advance")) director.Advance();
                using (new EditorGUI.DisabledScope(!director.IsRunning))
                    if (GUILayout.Button("Stop")) director.Stop();
            }
            if (director.Session != null)
            {
                EditorGUILayout.LabelField("State", director.Session.State.ToString());
                EditorGUILayout.LabelField("Graph", director.Session.Graph != null ? director.Session.Graph.name : "None");
                EditorGUILayout.LabelField("Current Node", director.Session.CurrentNodeId ?? "None");
            }
            Repaint();
        }
    }
}
