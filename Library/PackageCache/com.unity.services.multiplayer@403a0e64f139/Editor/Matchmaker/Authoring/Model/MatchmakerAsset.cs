using Newtonsoft.Json;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Parser;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.IO;
using Unity.Services.Multiplayer.Editor.Shared.Assets;
using UnityEditor;
using UnityEngine;
using PathIO = System.IO.Path;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Model
{
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.services.multiplayer@1.0/manual/Matchmaker/Authoring/index.html")]
    class MatchmakerAsset : ScriptableObject, IPath, ISerializationCallbackReceiver
    {
        const string k_DefaultFileName = "Matchmaker";
        string m_Path;

        public string Name { get; set; }

        public string Path { get => m_Path; set => SetPath(value); }

        public MatchmakerConfigResource ResourceDeploymentItem { get; set; }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { /* Not needed */ }

        void ISerializationCallbackReceiver.OnAfterDeserialize() { Name = System.IO.Path.GetFileName(Path); }

        void SetPath(string path)
        {
            if (ResourceDeploymentItem == null)
            {
                string type = "Queue Config";
                if (PathIO.GetExtension(path) == IMatchmakerConfigParser.EnvironmentConfigExtension)
                    type = "Environment Config";
                ResourceDeploymentItem = new MatchmakerConfigResource { Type = type };
            }

            var fileName = PathIO.GetFileName(path);

            Name = fileName;
            ResourceDeploymentItem.Path = path;
            ResourceDeploymentItem.Name = fileName;
            m_Path = path;
        }

        [MenuItem("Assets/Create/Services/Matchmaker Environment Config", false, 81)]
        public static void CreateConfigEnv()
        {
            var fileName = $"{k_DefaultFileName}Environment{IMatchmakerConfigParser.EnvironmentConfigExtension}";
            var content = JsonConvert.SerializeObject(EnvironmentConfig.GetDefault(), MatchmakerConfigLoader.GetSerializationSettings());
            ProjectWindowUtil.CreateAssetWithContent(fileName, content);
        }

        [MenuItem("Assets/Create/Services/Matchmaker Queue Config", false, 81)]
        public static void CreateConfig()
        {
            var fileName = $"{k_DefaultFileName}Queue{IMatchmakerConfigParser.QueueConfigExtension}";
            var content = JsonConvert.SerializeObject(QueueConfig.GetDefault(), MatchmakerConfigLoader.GetSerializationSettings());
            ProjectWindowUtil.CreateAssetWithContent(fileName, content);
        }

        public void ClearOwnedStates()
        {
            var states = ResourceDeploymentItem.States;
            var i = 0;
            while (i < states.Count)
            {
                if (MatchmakerConfigLoader.IsDeserializationError(states[i].Description))
                {
                    states.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }
}
