using System;
using System.Collections.ObjectModel;
using Unity.Services.Core.Editor;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.AdminApi;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.AdminApi.Matchmaker.Api;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.AdminApi.Network;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.AdminApi.Shared;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Fetch;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.IO;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Parser;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Deployment;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.IO;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Model;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using UnityEditor;
using ILogger = Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Logging.ILogger;
using Logger = Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Logging.Logger;
using static Unity.Services.Multiplayer.Editor.Shared.DependencyInversion.Factories;
using FileSystem = Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.IO.FileSystem;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using Unity.Services.Multiplay.Authoring.Editor;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring
{
    class MatchmakerAuthoringServices : AbstractRuntimeServices<MatchmakerAuthoringServices>
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Instance.Initialize(new ServiceCollection());
            var deploymentItemProvider = Instance.GetService<DeploymentProvider>();
            Deployments.Instance.DeploymentProviders.Add(deploymentItemProvider);
        }

        internal override void Register(ServiceCollection collection)
        {
            // This is the Dependency Inversion container for the assembly
            collection.RegisterSingleton(Default<ObservableCollection<MatchmakerAsset>, ObservableMatchmakerQueueAssets>);
            collection.RegisterSingleton(c => (ObservableMatchmakerQueueAssets)c.GetService(typeof(ObservableCollection<MatchmakerAsset>)));
            collection.Register(Default<MatchmakerDeployCommand>);
            collection.Register(Default<IEditorMatchmakerDeploymentHandler, EditorMatchmakerDeploymentHandler>);
            collection.Register(Default<IConfigApiClient, MatchmakerAdminClient>);
            collection.Register(Default<IAccessTokens, AccessTokens>);
            collection.RegisterStartupSingleton(Default<DeploymentProvider, MatchmakerDeploymentProvider>);
            collection.Register(Default<IMatchmakerConfigParser, MatchmakerConfigLoader>);
            collection.Register(Default<IDeepEqualityComparer, MatchmakerConfigLoader>);
            collection.Register(Default<ILogger, Logger>);
            collection.Register(Default<IFileSystem, FileSystem>);
            collection.Register(Default<IMatchmakerAdminApi, MatchmakerAdminApi>);
            collection.Register(Default<IApiClient, ApiClient>);
            collection.Register(_ => new Func<IEnvironmentProvider>(() => Deployments.Instance.EnvironmentProvider));
            collection.Register(Default<IProjectIdProvider, ProjectIdProvider>);
            collection.Register(Default<IRetryPolicy, RetryPolicy>);
            var multiplayRegistry = MultiplayAuthoringServices.Provider;

            collection.Register(_ => multiplayRegistry.GetService<IFleetApi>());
        }
    }
}
