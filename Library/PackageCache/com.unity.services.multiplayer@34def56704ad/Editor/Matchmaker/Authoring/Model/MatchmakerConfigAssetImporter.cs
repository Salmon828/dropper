using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Parser;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Model
{
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.services.multiplayer@1.0/manual/Matchmaker/Authoring/index.html")]
    [ScriptedImporter(1, new[]
    {
        IMatchmakerConfigParser.QueueConfigExtension,
        IMatchmakerConfigParser.EnvironmentConfigExtension
    })]
    class MatchmakerConfigAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = MatchmakerAuthoringServices.Instance
                .GetService<ObservableMatchmakerQueueAssets>()
                .GetOrCreateInstance(ctx.assetPath);

            ctx.AddObjectToAsset("MainAsset", asset);
            UnityEngine.Debug.Log($"[{nameof(MatchmakerConfigAssetImporter)}][{GetHashCode()}] Asset Imported");
            ctx.SetMainObject(asset);
        }
    }
}
