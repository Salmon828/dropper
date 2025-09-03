using UnityEditor.AssetImporters;
using UnityEngine;
using Logger = Unity.Services.Multiplayer.Editor.Shared.Logging.Logger;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    [ScriptedImporter(k_Version, MultiplayConfigResource.FileExtension)]
    class MultiplayConfigImporter : ScriptedImporter
    {
        const int k_Version = 1;

        public void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = MultiplayAuthoringServices.Provider
                .GetService<MultiplayConfigObservableAssets>()
                .GetOrCreateInstance(ctx.assetPath);

            CheckForDeprecatedFields(asset.Path);

            ctx.AddObjectToAsset("MainAsset", asset, MultiplayConfigResource.Icon);
            ctx.SetMainObject(asset);
        }

        static void CheckForDeprecatedFields(string path)
        {
            var checker = new MultiplayConfigYamlChecker(path);
            var warnings = checker.CheckForDeprecatedFields();
            foreach (var warning in warnings)
            {
                Logger.LogWarning(warning);
            }
        }
    }
}
