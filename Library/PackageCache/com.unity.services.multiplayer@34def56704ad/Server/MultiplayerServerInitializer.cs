using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Authentication.Server.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Device.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Scheduler.Internal;
using Unity.Services.Core.Telemetry.Internal;
using Unity.Services.Multiplay;
using Unity.Services.Qos.Internal;
using Unity.Services.Vivox.Internal;
using Unity.Services.Wire.Internal;
using UnityEngine;

namespace Unity.Services.Multiplayer
{
    partial class MultiplayerServerInitializer : IInitializablePackageV2
    {
        const string k_CloudEnvironmentKey = "com.unity.services.core.cloud-environment";
        const string k_PackageName = "com.unity.services.multiplayer";
        const string k_StagingEnvironment = "staging";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            var package = new MultiplayerServerInitializer();
            package.Register(CorePackageRegistry.Instance);
        }

        public void Register(CorePackageRegistry registry)
        {
            registry.Register(this)
                .DependsOn<IActionScheduler>()
                .DependsOn<IWireDirect>()
                .DependsOn<IServerAccessToken>()
                .DependsOn<IServerEnvironmentId>()
                .DependsOn<IMetricsFactory>()
                .DependsOn<IPlayerId>()
                .DependsOn<IProjectConfiguration>()
                .DependsOn<IInstallationId>()
                .OptionallyDependsOn<IVivox>()
                .OptionallyDependsOn<IWire>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            InitializeServices(registry, true);
            return Task.CompletedTask;
        }

        public Task InitializeInstanceAsync(CoreRegistry registry)
        {
            InitializeServices(registry, false);
            return Task.CompletedTask;
        }

        void InitializeServices(CoreRegistry registry, bool globalRegistry)
        {
            var actionScheduler = registry.GetServiceComponent<IActionScheduler>();
            var wireDirect = registry.GetServiceComponent<IWireDirect>();
            var serverAccessToken = registry.GetServiceComponent<IServerAccessToken>();
            var environmentId = registry.GetServiceComponent<IServerEnvironmentId>();
            var metricsFactory = registry.GetServiceComponent<IMetricsFactory>();
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();
            var installationId = registry.GetServiceComponent<IInstallationId>();
            var qosResults = registry.GetServiceComponent<IQosResults>();
            registry.TryGetServiceComponent<IWire>(out var wire);
            registry.TryGetServiceComponent<IVivox>(out var vivox);
            var cloudEnvironment = projectConfiguration.GetString(k_CloudEnvironmentKey);
            var serviceId = new InternalServiceID(serverAccessToken);
            var lobbyService = MultiplayerInitializer.InitializeLobbyService(serverAccessToken, environmentId, metricsFactory, null, serviceId, vivox, wire, cloudEnvironment);
            var lobbyBuilder = new LobbyBuilder(actionScheduler, lobbyService, null, serviceId, serverAccessToken, serverAccessToken, true);
            var relayService = MultiplayerInitializer.InitializeRelayService(serverAccessToken, projectConfiguration, qosResults);
            relayService.EnableQos = false;
            var relayBuilder = new RelayBuilder(relayService);
            var matchmakerService = InitializeMatchmakerService(serverAccessToken, environmentId, installationId, projectConfiguration, cloudEnvironment);
            var networkBuilder = new NetworkBuilder(actionScheduler);
            var connectionModule = new ConnectionProvider(networkBuilder, null, relayBuilder);
            var matchmakerModule = new MatchmakerProvider(actionScheduler, matchmakerService);

            var moduleRegistry = new ModuleRegistry();
            moduleRegistry.RegisterModuleProvider(connectionModule);
            moduleRegistry.RegisterModuleProvider(matchmakerModule);

            var sessionManager = new SessionManager(actionScheduler, moduleRegistry, lobbyBuilder, null, serverAccessToken);

            var multiplayService = InitializeMultiplayService(wireDirect, serverAccessToken);

            MultiplaySessionManager multiplaySessionManager = null;

            if (multiplayService != null)
            {
                multiplaySessionManager = new MultiplaySessionManager(sessionManager, multiplayService, actionScheduler, matchmakerService);
                registry.RegisterService<IMultiplayService>(multiplayService);
            }

            var multiplayerServerService = new MultiplayerServerServiceImpl(sessionManager, multiplaySessionManager);
            registry.RegisterService<IMultiplayerServerService>(multiplayerServerService);

            if (globalRegistry)
            {
                MultiplayerServerService.Instance = multiplayerServerService;

                if (multiplayService != null)
                {
                    MultiplayService.Instance = multiplayService;
                }
            }
        }
    }
}
