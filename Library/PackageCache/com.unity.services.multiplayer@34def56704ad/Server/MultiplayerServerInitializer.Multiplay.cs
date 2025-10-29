using Unity.Services.Authentication.Server.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Multiplay;
using Unity.Services.Multiplay.Http;
using Unity.Services.Multiplay.Internal;
using Unity.Services.Wire.Internal;

namespace Unity.Services.Multiplayer
{
    partial class MultiplayerServerInitializer : IInitializablePackageV2
    {
        internal static WrappedMultiplayService InitializeMultiplayService(
            IWireDirect wireDirect,
            IServerAccessToken serverAccessToken)
        {
            var serverConfigReader = new ServerConfigReader();

            if (serverConfigReader.Exists)
            {
                var httpClient = new HttpClient();
                var config = serverConfigReader.LoadServerConfig();
                var internalService = new InternalMultiplayService(httpClient, wireDirect, serverAccessToken);
                var service = new WrappedMultiplayService(internalService, config);
                return service;
            }

            return null;
        }
    }
}
