using System;
using Unity.Services.Multiplayer.Editor.Shared.Analytics;
using Unity.Services.Multiplayer.Editor.Shared.UI.DeploymentConfigInspectorFooter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    [CustomEditor(typeof(MultiplayConfigAsset))]
    [CanEditMultipleObjects]
    class MultiplayConfigAssetInspector : UnityEditor.Editor
    {
        const string k_Uxml = "Packages/com.unity.services.multiplayer/Editor/Multiplay/Authoring/Assets/Layouts/MultiplayConfigAssetInspector.uxml";

        public override VisualElement CreateInspectorGUI()
        {
            DisableReadonlyFlags();
            var myInspector = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml);
            visualTree.CloneTree(myInspector);

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
                "game_server_hosting");
#endif
        }

        void DisableReadonlyFlags()
        {
            serializedObject.targetObject.hideFlags = HideFlags.None;
        }
    }
}
