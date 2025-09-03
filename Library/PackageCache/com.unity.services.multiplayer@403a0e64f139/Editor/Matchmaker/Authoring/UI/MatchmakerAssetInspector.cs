using System;
using System.IO;
using System.Linq;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Model;
using Unity.Services.Multiplayer.Editor.Shared.Analytics;
using Unity.Services.Multiplayer.Editor.Shared.UI.DeploymentConfigInspectorFooter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.UI
{
    [CustomEditor(typeof(MatchmakerAsset))]
    class MatchmakerAssetInspector : UnityEditor.Editor
    {
        const int k_MaxLines = 75;
        const string k_Uxml = "Packages/com.unity.services.multiplayer/Editor/Matchmaker/Authoring/UI/Assets/MatchmakerAssetInspector.uxml";

        public override VisualElement CreateInspectorGUI()
        {
            var myInspector = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml);

            visualTree.CloneTree(myInspector);
            ShowResourceBody(myInspector);

            SetupConfigFooter(myInspector);

            return myInspector;
        }

        void SetupConfigFooter(VisualElement myInspector)
        {
#if DEPLOYMENT_API_AVAILABLE_V1_1
            var deploymentConfigInspectorFooter = myInspector.Q<DeploymentConfigInspectorFooter>();
            deploymentConfigInspectorFooter.BindGUI(
                AssetDatabase.GetAssetPath(target),
                MultiplayAuthoringServices.GetService<ICommonAnalytics>(MultiplayAuthoringServices.Provider),
                "matchmaker");
#endif
        }

        void ShowResourceBody(VisualElement myInspector)
        {
            var body = myInspector.Q<TextField>();
            if (targets.Length == 1)
            {
                body.visible = true;
                body.value = ReadResourceBody(targets[0]);
            }
            else
            {
                body.visible = false;
            }
        }

        static string ReadResourceBody(Object resource)
        {
            var path = AssetDatabase.GetAssetPath(resource);
            var lines = File.ReadLines(path).Take(k_MaxLines).ToList();
            if (lines.Count == k_MaxLines)
            {
                lines.Add("...");
            }
            return string.Join(Environment.NewLine, lines);
        }
    }
}
