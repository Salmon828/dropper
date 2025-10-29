#if GAMEOBJECTS_NETCODE_AVAILABLE
using System;
using System.Threading.Tasks;
using Unity.Services.DistributedAuthority;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;

namespace Unity.Services.Multiplayer
{
    internal class GameObjectsNetcodeNetworkHandler : INetworkHandler
    {
        NetworkConfiguration _connectionData;

        NetworkManager NetworkManager => NetworkManager.Singleton;

        public Task StartAsync(NetworkConfiguration configuration)
        {
            switch (configuration.Type)
            {
                case NetworkType.Direct:
                    SetupDirect(configuration);
                    break;
                case NetworkType.Relay:
                    SetupRelay(configuration);
                    break;
                case NetworkType.DistributedAuthority:
                    SetupDistributedAuthority(configuration);
                    break;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Logger.LogVerbose($"Netcode for GameObjects Network Handler: StopAsync");

            if (NetworkManager != null)
            {
                NetworkManager.Shutdown();
            }

            return Task.CompletedTask;
        }

        void SetupDirect(NetworkConfiguration configuration)
        {
            if (NetworkManager == null)
            {
                throw new SessionException("Failed to start NetworkManager component",
                    SessionError.NetworkManagerNotInitialized);
            }

            var transport = NetworkManager.GetComponent<UnityTransport>();

            if (transport == null)
            {
                throw new SessionException("NetworkManager must have a UnityTransport component",
                    SessionError.TransportComponentMissing);
            }
            Logger.LogVerbose(
                $"[SetupDirect] Publish Address: {configuration.DirectNetworkPublishAddress} - Listen Address: {configuration.DirectNetworkListenAddress}");
            transport.SetConnectionData(configuration.DirectNetworkPublishAddress, configuration.DirectNetworkListenAddress);
            if (configuration.Role == NetworkRole.Server)
            {
                StartAsServer();
            }
            else if (configuration.Role == NetworkRole.Host)
            {
                StartAsHost();
            }
            else
            {
                StartAsClient();
            }

            if (configuration.Role != NetworkRole.Client && configuration.DirectNetworkListenAddress.Port == 0)
            {
#if GAMEOBJECTS_NETCODE_2_AVAILABLE
                var localEndpoint = transport.GetLocalEndpoint();
                Logger.LogVerbose(
                    $"[SetupDirect] LocalEndpoint {localEndpoint}");
                configuration.UpdatePublishPort(localEndpoint.Port);
#else
                throw new SessionException("Listening port 0 requires Netcode for GameObjects 2.0.0; " +
                    "change the port to a non-zero value or upgrade netcode package to 2.0.0 or newer.", SessionError.Unknown);
#endif
            }
        }

        void SetupRelay(NetworkConfiguration configuration)
        {
            if (NetworkManager == null)
            {
                throw new SessionException("Failed to start NetworkManager component",
                    SessionError.NetworkManagerNotInitialized);
            }

            var transport = NetworkManager.GetComponent<UnityTransport>();

            if (transport == null)
            {
                throw new SessionException("NetworkManager must have a UnityTransport component",
                    SessionError.TransportComponentMissing);
            }

            transport.SetRelayServerData(configuration.RelayServerData);

            if (configuration.Role == NetworkRole.Host || configuration.Role == NetworkRole.Server)
            {
                StartAsHost();
            }
            else
            {
                StartAsClient();
            }
        }

        void SetupDistributedAuthority(NetworkConfiguration configuration)
        {
#if GAMEOBJECTS_NETCODE_2_AVAILABLE
            if (NetworkManager == null)
            {
                throw new SessionException("Failed to start NetworkManager component", SessionError.NetworkManagerNotInitialized);
            }

            // Since you can have more than one NetworkTransport component attached to a NetworkManager,
            // we want to get the NetworkTransport currently assigned to the NetworkManager.
            var transport = NetworkManager.NetworkConfig.NetworkTransport as DistributedAuthorityTransport;

            // If the currently assigned transport is not the DaTransport
            if (!transport)
            {
                // Check and see if the NetworkManager's GameObject has the DistributedAuthorityTransport component
                transport = NetworkManager.GetComponent<DistributedAuthorityTransport>();
                if (!transport)
                {
                    // If it does not, then add the DistributedAuthorityTransport component to the NetworkTransport
                    transport = NetworkManager.gameObject.AddComponent<DistributedAuthorityTransport>();
                }

                NetworkManager.NetworkConfig.NetworkTransport = transport;
            }

            transport.ConnectPayload = configuration.DistributedAuthorityConnectionPayload;
            transport.SetRelayServerData(configuration.RelayServerData);

            NetworkManager.NetworkConfig.UseCMBService = true;
            NetworkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.DistributedAuthority;

            StartAsClient();
#else
            throw new SessionException("Distributed Authority requires Netcode For GameObjects 2.0", SessionError.Unknown);
#endif
        }

        void StartAsServer()
        {
            Logger.LogVerbose($"Starting session connection as server");

            if (!NetworkManager.StartServer())
            {
                throw new SessionException("Failed to start NetworkManager component as server",
                    SessionError.NetworkManagerStartFailed);
            }
        }

        void StartAsHost()
        {
            Logger.LogVerbose($"Starting session connection as host");

            if (!NetworkManager.StartHost())
            {
                throw new SessionException("Failed to start NetworkManager component as host",
                    SessionError.NetworkManagerStartFailed);
            }
        }

        void StartAsClient()
        {
            Logger.LogVerbose($"Starting session connection as client");

            if (!NetworkManager.StartClient())
            {
                throw new SessionException("Failed to start NetworkManager component as client",
                    SessionError.NetworkManagerStartFailed);
            }
        }
    }
}

#endif
