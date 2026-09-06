using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Dialect.Editor
{
    public sealed class CreateDialectGraphAction : AssetCreationEndAction
    {
        [MenuItem("Assets/Create/Dialect/Dialogue Graph")]
        static void CreateDialogueGraph()
        {
            var icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None,
                CreateInstance<CreateDialectGraphAction>(), $"New Dialogue Graph.{DialectGraph.AssetExtension}", icon, null);
        }

        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(pathName);
            DialectGraph.CreateInitialized(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadMainAssetAtPath(path));
        }
    }
}
