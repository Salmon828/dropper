using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Model;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Deployment
{
    class MatchmakerDeploymentProvider : DeploymentProvider
    {
        public override string Service => L10n.Tr("Matchmaker");

        public override Command DeployCommand { get; }

        public MatchmakerDeploymentProvider(
            MatchmakerDeployCommand matchmakerDeployCommand,
            ObservableMatchmakerQueueAssets observableMatchmakerQueueAssets)
            : base(observableMatchmakerQueueAssets.DeploymentItems)
        {
            DeployCommand = matchmakerDeployCommand;
        }
    }
}
