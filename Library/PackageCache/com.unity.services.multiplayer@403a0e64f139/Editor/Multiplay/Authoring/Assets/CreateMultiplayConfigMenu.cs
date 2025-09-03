using System.IO;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Editor.Analytics;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    class CreateMultiplayConfigMenu : EndNameEditAction
    {
        const string k_DefaultName = "new_multiplay_config";
        const int k_CreateItemPriority = 82;

        [MenuItem("Assets/Create/Services/Multiplay Config", false, k_CreateItemPriority)]
        public static void CreateMultiplayConfigFile()
        {
            var filePath = k_DefaultName + MultiplayConfigResource.FileExtension;
            var icon = MultiplayConfigResource.Icon;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                CreateInstance<CreateMultiplayConfigMenu>(),
                filePath,
                icon,
                null);
        }

        [InitializeOnLoadMethod]
        static void SetMonoDefinitionIcon()
        {
            var monoImporter = (MonoImporter)AssetImporter.GetAtPath(MultiplayConfigResource.MonoDefinitionPath);
            var monoScript = monoImporter.GetScript();
            EditorGUIUtility.SetIconForObject(monoScript,  MultiplayConfigResource.Icon);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            File.WriteAllText(pathName, MultiplayConfigTemplate.Yaml);
            AssetDatabase.Refresh();
            MultiplayAuthoringServices.Provider.GetService<IAssetAnalytics>().AddAsset();
        }
    }
}
