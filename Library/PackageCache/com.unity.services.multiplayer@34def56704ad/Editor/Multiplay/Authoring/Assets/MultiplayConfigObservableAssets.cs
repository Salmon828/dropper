using Unity.Services.Multiplay.Authoring.Editor.Analytics;
using Unity.Services.Multiplayer.Editor.Shared.Assets;
using UnityEngine;
using Logger = Unity.Services.Multiplayer.Editor.Shared.Logging.Logger;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    class MultiplayConfigObservableAssets : ObservableAssets<MultiplayConfigAsset>
    {
        readonly IAssetAnalytics m_AssetAnalytics;

        public MultiplayConfigObservableAssets(IAssetAnalytics assetAnalytics)
            : base(new AssetPostprocessorProxy(), false)
        {
            m_AssetAnalytics = assetAnalytics;
            LoadAllAssets();
        }

        protected override void AddForPath(string path, MultiplayConfigAsset asset)
        {
            Logger.LogVerbose($"[{nameof(MultiplayConfigObservableAssets)}] Asset '{path}' Added");
            base.AddForPath(path, asset);
            asset.FromYamlFile(path);
            m_AssetAnalytics?.UpdateAsset(asset);
        }

        protected override void UpdateForPath(string path, MultiplayConfigAsset asset)
        {
            Logger.LogVerbose($"[{nameof(MultiplayConfigObservableAssets)}] Asset '{path}' Updated");
            base.UpdateForPath(path, asset);
            m_AssetAnalytics?.UpdateAsset(asset);
        }

        protected override void MovePath(string toPath, string fromPath)
        {
            Logger.LogVerbose($"[{nameof(MultiplayConfigObservableAssets)}] Asset '{toPath}' moved '{fromPath}'");
            base.MovePath(toPath, fromPath);
            m_AssetAnalytics?.UpdateAsset(m_AssetPaths[toPath]);
        }

        protected override void RemoveForPath(string path, MultiplayConfigAsset asset)
        {
            Logger.LogVerbose($"[{nameof(MultiplayConfigObservableAssets)}] Asset '{path}' removed");
            base.RemoveForPath(path, asset);
            m_AssetAnalytics?.DeleteAsset(asset);
        }

        /// <summary>
        /// Checks whether the <see cref="Object"/> is C# <c>null</c>.
        /// </summary>
        /// <param name="o">The <see cref="Object"/> to check.</param>
        /// <returns><c>true</c> if the <see cref="Object"/> is C# <c>null</c>, <c>false</c> otherwise.</returns>
        /// <remarks>An object is considered C# <c>null</c> if the expression <c>(object)o == null</c> evaluates to
        /// <c>true</c>.</remarks>
        static bool IsManagedObjectAlive(Object o) => o is not null;

        /// <summary>
        /// Checks whether the <see cref="Object"/> is Unity <c>null</c>.
        /// </summary>
        /// <param name="o">The <see cref="Object"/> to check.</param>
        /// <returns><c>true</c> if the <see cref="Object"/> is Unity <c>null</c>, <c>false</c> otherwise.</returns>
        /// <remarks>An object is considered Unity <c>null</c> if:
        /// <list type="bullet">
        ///   <item>
        ///     The native part of the object is no longer alive, <see cref="Object.IsNativeObjectAlive"/> evaluates to
        ///     <c>false</c>.
        ///   </item>
        ///   <item>
        ///     The expression <c>(object)o == null</c> evaluates to <c>true</c>.
        ///   </item>
        /// </list>
        /// If the method evaluates to <c>true</c> because the native part of the object is no longer alive, you might
        /// still be able to use the managed part of the object.</remarks>
        static bool IsNativeObjectAlive(Object o) => o != null;

        public MultiplayConfigAsset GetOrCreateInstance(string assetPath)
        {
            foreach (var a in this)
            {
                // Should be safe to access 'a.Path' as the managed part of the
                // instance 'a' should still be in a valid state.
                if (IsManagedObjectAlive(a) && assetPath == a.Path)
                {
                    // If the native part of the instance 'a' is no longer alive
                    // we regenerate the instance at the same location and
                    // return it, otherwise we just return it.
                    return !IsNativeObjectAlive(a) ? RegenAsset(a) : a;
                }
            }

            var asset = ScriptableObject.CreateInstance<MultiplayConfigAsset>();
            Logger.LogVerbose($"[{nameof(MultiplayConfigObservableAssets)}] Asset '{asset.Path}' created.");
            asset.Path = assetPath;
            asset.FromYamlFile(asset.Path);
            return asset;
        }

        static MultiplayConfigAsset RegenAsset(MultiplayConfigAsset asset)
        {
            var newAsset = ScriptableObject.CreateInstance<MultiplayConfigAsset>();
            Logger.LogVerbose($"[{nameof(MultiplayConfigObservableAssets)}] Asset '{asset.Path}' regenerated.");
            newAsset.Path = asset.Path;
            asset.TransferListeners(newAsset);  // transfer PropertyChanged event listeners before parsing yaml to trigger the event as expected
            newAsset.FromYamlFile(asset.Path);
            return newAsset;
        }
    }
}
