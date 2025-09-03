using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Parser;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.IO;
using Unity.Services.Multiplayer.Editor.Shared.Assets;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Model
{
    /// <summary>
    /// This class serves to track creation and deletion of assets of the
    /// associated service type
    /// </summary>
    sealed class ObservableMatchmakerQueueAssets : ObservableCollection<MatchmakerAsset>, IDisposable
    {
        readonly IMatchmakerConfigParser m_ResourceLoader;
        readonly ObservableAssets<MatchmakerAsset> m_MyServiceAssets;

        public ObservableCollection<IDeploymentItem> DeploymentItems { get; } = new ObservableCollection<IDeploymentItem>();

        public ObservableMatchmakerQueueAssets(IMatchmakerConfigParser resourceLoader)
        {
            m_ResourceLoader = resourceLoader;
            m_MyServiceAssets = new ObservableAssets<MatchmakerAsset>();

            foreach (var asset in m_MyServiceAssets)
            {
                OnNewAsset(asset);
                DeploymentItems.Add(asset.ResourceDeploymentItem);
            }

            m_MyServiceAssets.CollectionChanged += MyServiceAssetsOnCollectionChanged;
        }

        public void Dispose()
        {
            m_MyServiceAssets.CollectionChanged -= MyServiceAssetsOnCollectionChanged;
        }

        void OnNewAsset(MatchmakerAsset asset)
        {
            PopulateModel(asset);
            Add(asset);
        }

        void PopulateModel(MatchmakerAsset asset)
        {
            try
            {
                asset.ClearOwnedStates();
                asset.ResourceDeploymentItem.Content = m_ResourceLoader.Parse(asset.Path);
            }
            catch (MyServiceDeserializationException e)
            {
                asset.ResourceDeploymentItem.States.Add(
                    new AssetState(e.ErrorMessage, e.Details, SeverityLevel.Error));
            }
        }

        void MyServiceAssetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems.Cast<MatchmakerAsset>())
                {
                    DeploymentItems.Remove(oldItem.ResourceDeploymentItem);
                    Remove(oldItem);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.Cast<MatchmakerAsset>())
                {
                    DeploymentItems.Add(newItem.ResourceDeploymentItem);
                    OnNewAsset(newItem);
                }
            }
        }

        MatchmakerAsset RegenAsset(MatchmakerAsset asset)
        {
            var newAsset = ScriptableObject.CreateInstance<MatchmakerAsset>();
            newAsset.Path = asset.Path;
            //Keep the DI reference
            newAsset.ResourceDeploymentItem = asset.ResourceDeploymentItem;
            asset = newAsset;
            PopulateModel(asset);

            return asset;
        }

        public MatchmakerAsset GetOrCreateInstance(string assetPath)
        {
            foreach (var a in m_MyServiceAssets)
            {
                if (assetPath == a.Path)
                {
                    return a == null ? RegenAsset(a) : a;
                }
            }

            var asset = ScriptableObject.CreateInstance<MatchmakerAsset>();
            asset.Path = assetPath;
            return asset;
        }
    }
}
