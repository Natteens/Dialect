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
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultGraph"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAutomaticSteps"));
            serializedObject.ApplyModifiedProperties();

            var director = (DialectDirector)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            { EditorGUILayout.HelpBox("Runtime controls are available in Play Mode.", MessageType.Info); return; }

            var session = director.Session;
            EditorGUILayout.LabelField("State", session?.State.ToString() ?? "Idle");
            EditorGUILayout.LabelField("Graph", session?.Graph != null ? session.Graph.name : "None");
            EditorGUILayout.LabelField("Current Node", string.IsNullOrEmpty(session?.CurrentNodeId) ? "None" : session.CurrentNodeId);
            EditorGUILayout.LabelField("Termination", session?.TerminationReason?.ToString() ?? "None");

            using (new EditorGUI.DisabledScope(director.IsRunning || director.DefaultGraph == null))
                if (GUILayout.Button("Play Default Graph")) director.TryPlay(director.DefaultGraph);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(session?.State != DialectPlaybackState.WaitingForAdvance))
                    if (GUILayout.Button("Advance")) director.Advance();
                using (new EditorGUI.DisabledScope(session?.State != DialectPlaybackState.Suspended))
                    if (GUILayout.Button("Resume")) director.Resume();
                using (new EditorGUI.DisabledScope(!director.IsRunning))
                    if (GUILayout.Button("Stop")) director.Stop();
            }

            if (session?.State == DialectPlaybackState.WaitingForChoice && session.CurrentChoices != null)
                for (var i = 0; i < session.CurrentChoices.Count; i++)
                    if (GUILayout.Button($"Choose {i + 1}: {session.CurrentChoices.Choices[i].Text}")) director.Choose(i);

            using (new EditorGUI.DisabledScope(session?.Graph == null))
                if (GUILayout.Button("Open Active Graph")) AssetDatabase.OpenAsset(session.Graph);
            Repaint();
        }
    }
}
