using System.Collections.Generic;
using Dialect.Blackboards;
using UnityEditor;
using UnityEngine;

namespace Dialect.Editor
{
    [FilePath("ProjectSettings/DialectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class DialectProjectSettings : ScriptableSingleton<DialectProjectSettings>
    {
        public static event System.Action<bool> VisualizationChanged;
        [SerializeField] List<DialectBlackboard> defaultBlackboards = new();
        [SerializeField] bool visualizationEnabled = true;

        public IReadOnlyList<DialectBlackboard> DefaultBlackboards => defaultBlackboards;
        public bool VisualizationEnabled
        {
            get => visualizationEnabled;
            set
            {
                if (visualizationEnabled == value) return;
                visualizationEnabled = value;
                Save(true);
                VisualizationChanged?.Invoke(value);
            }
        }

        internal void SaveSettings()
        {
            Save(true);
            VisualizationChanged?.Invoke(visualizationEnabled);
        }
    }

    static class DialectSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create() => new("Project/Dialect", SettingsScope.Project)
        {
            label = "Dialect",
            guiHandler = _ =>
            {
                var settings = DialectProjectSettings.instance;
                var serialized = new SerializedObject(settings);
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("defaultBlackboards"), new GUIContent("Default Shared Blackboards"), true);
                EditorGUILayout.PropertyField(serialized.FindProperty("visualizationEnabled"), new GUIContent("Runtime Visualization"));
                if (serialized.ApplyModifiedProperties()) settings.SaveSettings();
                EditorGUILayout.HelpBox("Defaults are copied only into newly created .dlg graphs.", MessageType.Info);
            },
            keywords = new HashSet<string> { "Dialect", "Blackboard", "Dialogue", "Visualization" }
        };
    }
}
