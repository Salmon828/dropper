using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Serialization;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Serialization.NamingConventions;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Serialization.NodeDeserializers;
using Unity.Services.Multiplayer.Editor.Shared.Assets;
using Unity.Services.Multiplayer.Editor.Shared.Threading;
using UnityEngine;
using Logger = Unity.Services.Multiplayer.Editor.Shared.Logging.Logger;
using FileIO = System.IO.File;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.services.multiplayer@1.0/manual/Multiplay/Authoring/index.html")]
    class MultiplayConfigAsset : ScriptableObject, IPath, INotifyPropertyChanged
    {
        public MultiplayConfig Config
        {
            get => m_Config;
            set
            {
                m_Config = value;
                NormalizeBuildPaths();
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get => m_Path;
            set
            {
                m_Path = value;
                NormalizeBuildPaths();
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NonSerialized]
        MultiplayConfig m_Config;
        [NonSerialized]
        string m_Path;

        public void FromYamlFile(string yamlFilePath)
        {
            try
            {
                Logger.LogVerbose($"Deserializing {yamlFilePath}");
                var content = FileIO.ReadAllText(yamlFilePath);
                FromYaml(content);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to deserialize '{yamlFilePath}'. Reason: {e.Message}.");
                Logger.LogException(e);
                Config = null;
            }
        }

        internal void FromYaml(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new ResourceNameTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            Config = deserializer.Deserialize<MultiplayConfig>(yaml);
        }

        void NormalizeBuildPaths()
        {
            if (Config?.Builds == null || Path == null)
            {
                return;
            }

            var basePath = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(Path) !);
            foreach (var(_, definition) in Config.Builds)
            {
                if (definition.BuildPath != null && definition.BuildPath.StartsWith("."))
                {
                    definition.BuildPath = System.IO.Path.GetFullPath(definition.BuildPath, basePath);
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Sync.RunNextUpdateOnMain(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public void TransferListeners(MultiplayConfigAsset other)
        {
            if (PropertyChanged is null) return;

            foreach (var listener in PropertyChanged.GetInvocationList())
            {
                other.PropertyChanged += (PropertyChangedEventHandler)listener;
            }

            // unregister the listeners from this object
            PropertyChanged = null;
        }
    }
}
