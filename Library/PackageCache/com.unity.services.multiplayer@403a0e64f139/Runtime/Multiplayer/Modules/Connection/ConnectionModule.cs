using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Networking.Transport.Relay;
using Unity.Services.DistributedAuthority;

namespace Unity.Services.Multiplayer
{
    class ConnectionModule : IModule
    {
        public const string PropertyKey = "_session_network";

        public bool IsInProgress { get; private set; }
        public bool IsSetup { get; private set; }

        public string SessionPropertyKey => PropertyKey;

        SessionProperty Property;
        ConnectionMetadata ConnectionMetadata;
        INetworkHandler NetworkHandler;
        IDaHandler DaHandler;
        IRelayHandler RelayHandler;
        internal ConnectionInfo ConnectionInfo;

        readonly ISession m_Session;
        readonly INetworkBuilder m_NetworkBuilder;
        readonly IDaBuilder m_DaBuilder;
        readonly IRelayBuilder m_RelayBuilder;

        internal ConnectionModule(ISession session, INetworkBuilder networkBuilder, IDaBuilder daBuilder, IRelayBuilder relayBuilder)
        {
            m_Session = session;
            m_NetworkBuilder = networkBuilder;
            m_DaBuilder = daBuilder;
            m_RelayBuilder = relayBuilder;
            m_Session.Changed += OnSessionChanged;
            m_Session.SessionPropertiesChanged += OnSessionChanged;
        }

        async void OnSessionChanged()
        {
            await ValidatePropertyAsync();
        }

        async Task IModule.InitializeAsync()
        {
            Logger.LogVerbose($"ConnectionModule InitializeAsync");

            if (ConnectionInfo != null && m_Session.IsHost)
            {
                await CreateConnectionAsync();
            }

            if (!IsSetup)
            {
                await ValidatePropertyAsync();
            }
        }

        public void SetConnectionOption(ConnectionInfo options)
        {
            ConnectionInfo = options;
        }

        public void SetNetworkHandler(INetworkHandler networkHandler)
        {
            NetworkHandler = networkHandler;
        }

        public async Task CreateConnectionAsync()
        {
            Logger.LogVerbose($"ConnectionModule.CreateConnectionAsync");

            if (!m_Session.IsHost)
                throw new SessionException("Trying to setup the network but the player isn't the host", SessionError.NetworkSetupFailed);

            if (IsInProgress)
                throw new SessionException("Trying to setup the network when already in progress", SessionError.NetworkSetupFailed);

            if (IsSetup)
                throw new SessionException("Trying to setup the network when already done", SessionError.NetworkSetupFailed);

            try
            {
                IsInProgress = true;

                string allocationId = null;

                var networkRole = m_Session.CurrentPlayer != null ? NetworkRole.Host : NetworkRole.Server;

                switch (ConnectionInfo.Network)
                {
                    case NetworkType.DistributedAuthority:
                    {
                        Logger.LogVerbose($"Setting up DA Network");
                        DaHandler = m_DaBuilder.Build();
                        await DaHandler.CreateAndJoinSessionAsync(m_Session.Id, ConnectionInfo.Region);
                        allocationId = DaHandler.AllocationId.ToString();

                        var connectPayload = DaHandler.GetConnectPayload();
                        await SetDistributedAuthorityConnectHash(connectPayload);

                        var networkConfiguration = new NetworkConfiguration(
                            networkRole,
                            NetworkType.DistributedAuthority,
                            DaHandler.GetRelayServerData(SecurePlatformConnectionType()),
                            connectPayload.SerializeToNativeArray());
                        await SetupConnectionAsync(networkConfiguration);
                        ConnectionMetadata = new ConnectionMetadata()
                        {
                            Network = NetworkType.DistributedAuthority,
                            RelayJoinCode = DaHandler.RelayJoinCode,
                            HostId = null // Must be set to null for DA.
                        };
                        break;
                    }
                    case NetworkType.Relay:
                    {
                        Logger.LogVerbose($"Setting up Relay Network");
                        RelayHandler = m_RelayBuilder.Build();
                        await RelayHandler.CreateAllocationAsync(m_Session.MaxPlayers, ConnectionInfo.Region);
                        await RelayHandler.FetchJoinCodeAsync();

                        var relayServerData = RelayHandler.GetRelayServerData(SecurePlatformConnectionType());
                        RelayServerData? relayClientData = null;
#if ENTITIES_NETCODE_AVAILABLE
                        await RelayHandler.JoinAllocationAsync(RelayHandler.RelayJoinCode);
                        var clientRelayData = RelayHandler.GetRelayServerData(SecurePlatformConnectionType());
                        relayClientData = clientRelayData;
#endif

                        var networkConfiguration = new NetworkConfiguration(
                            networkRole,
                            NetworkType.Relay,
                            relayServerData,
                            relayClientData);

                        await SetupConnectionAsync(networkConfiguration);

                        ConnectionMetadata = new ConnectionMetadata()
                        {
                            Network = NetworkType.Relay,
                            RelayJoinCode = RelayHandler.RelayJoinCode,
                            HostId = m_Session.Host
                        };

                        allocationId = RelayHandler.AllocationId.ToString();
                        break;
                    }
                    case NetworkType.Direct:
                    {
                        Logger.LogVerbose($"Setting up Direct Network");
                        var networkConfiguration = new NetworkConfiguration(
                            networkRole,
                            ConnectionInfo.Ip,
                            ConnectionInfo.Port,
                            ConnectionInfo.ListenAddress);

                        await SetupConnectionAsync(networkConfiguration);
                        if (networkConfiguration.Role != NetworkRole.Client &&
                            networkConfiguration.DirectNetworkPublishAddress.Port == 0)
                        {
                            Logger.LogWarning($"Port 0 on publish address {networkConfiguration.DirectNetworkPublishAddress}" +
                                $" was not updated by network handler (hint: call NetworkConfiguration.UpdatePublishPort())");
                        }

                        ConnectionMetadata = new ConnectionMetadata()
                        {
                            Network = NetworkType.Direct,
                            Ip = ConnectionInfo.Ip,
                            Port = networkConfiguration.DirectNetworkPublishAddress.Port,
                            HostId = m_Session.Host
                        };
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(allocationId) && m_Session.CurrentPlayer != null)
                {
                    m_Session.CurrentPlayer.SetAllocationId(allocationId);
                }

                Logger.LogVerbose($"Adding connection session property");
                var sessionProperty = new SessionProperty(SerializeConnectionMetadata(ConnectionMetadata), VisibilityPropertyOptions.Member);
                m_Session.AsHost().SetProperty(PropertyKey, sessionProperty);
            }
            catch (Exception)
            {
                IsInProgress = false;
                throw;
            }
        }

        async Task JoinConnectionAsync()
        {
            if (IsInProgress)
                throw new SessionException("Trying to connect when already connecting", SessionError.NetworkSetupFailed);

            if (IsSetup)
                throw new SessionException("Trying to connect when already connected", SessionError.NetworkSetupFailed);

            Logger.LogVerbose($"ConnectionModule.JoinConnectionAsync");
            IsInProgress = true;
            string allocationId = null;

            NetworkConfiguration networkConfiguration;
            RelayHandler = m_RelayBuilder.Build();

            switch (ConnectionMetadata.Network)
            {
                case NetworkType.Direct:
                {
                    Logger.LogVerbose($"ConnectionModule.JoinConnectionAsync:Direct");
                    networkConfiguration = new NetworkConfiguration(
                        NetworkRole.Client,
                        ConnectionMetadata.Ip,
                        ConnectionMetadata.Port,
                        null);
                    break;
                }
                case NetworkType.Relay:
                {
                    Logger.LogVerbose($"ConnectionModule.JoinConnectionAsync:Relay");
                    await RelayHandler.JoinAllocationAsync(ConnectionMetadata.RelayJoinCode);
                    var relayServerData = RelayHandler.GetRelayServerData(SecurePlatformConnectionType());
                    RelayServerData? relayClientData = null;
#if ENTITIES_NETCODE_AVAILABLE
                    relayClientData = relayServerData;
#endif
                    networkConfiguration = new NetworkConfiguration(
                        NetworkRole.Client,
                        NetworkType.Relay,
                        relayServerData,
                        relayClientData);
                    allocationId = RelayHandler.AllocationId.ToString();
                    break;
                }
                case NetworkType.DistributedAuthority:
                {
                    Logger.LogVerbose($"ConnectionModule.JoinConnectionAsync:Distributed");
                    DaHandler = m_DaBuilder.Build();
                    await DaHandler.JoinSessionAsync(ConnectionMetadata.RelayJoinCode);
                    allocationId = DaHandler.AllocationId.ToString();

                    var connectPayload = DaHandler.GetConnectPayload();
                    await SetDistributedAuthorityConnectHash(connectPayload);

                    networkConfiguration = new NetworkConfiguration(
                        NetworkRole.Client,
                        NetworkType.DistributedAuthority,
                        DaHandler.GetRelayServerData(SecurePlatformConnectionType()),
                        connectPayload.SerializeToNativeArray());
                    break;
                }
                default:
                    throw new ArgumentException($"Invalid transport type {ConnectionMetadata.Network}");
            }

            await SetupConnectionAsync(networkConfiguration);

            if (!string.IsNullOrEmpty(allocationId))
            {
                m_Session.CurrentPlayer.SetAllocationId(allocationId);
            }
        }

        public async Task LeaveAsync()
        {
            Logger.LogVerbose($"ConnectionModule LeaveAsync");
            if (IsSetup)
            {
                RelayHandler?.Disconnect();
                await NetworkHandler.StopAsync();
                IsSetup = false;
            }
        }

        async Task ValidatePropertyAsync()
        {
            if (m_Session.Properties == null || m_Session.Properties.Count == 0)
            {
                Logger.LogVerbose($"ConnectionModule ValidatePropertyAsync: No properties on session");
            }

            if (!m_Session.IsHost && m_Session.Properties.TryGetValue(PropertyKey, out var newProperty))
            {
                if (!IsSetup && !IsInProgress && Property?.Value != newProperty.Value)
                {
                    Property = newProperty;
                    await ProcessAsync(Property.Value);
                }
            }
        }

        public async Task ProcessAsync(string metadata)
        {
            Logger.LogVerbose($"ConnectionModule.Process: {metadata}");

            if (!IsInProgress && !IsSetup && !m_Session.IsHost)
            {
                try
                {
                    DeserializeConnectionMetadata(metadata);
                    if (ConnectionMetadata.HostId == null || ConnectionMetadata.HostId.Equals(m_Session.Host))
                    {
                        await JoinConnectionAsync();
                    }
                }
                catch (Exception)
                {
                    IsInProgress = false;
                    throw;
                }
            }
        }

        async Task SetupConnectionAsync(NetworkConfiguration networkConfiguration)
        {
            NetworkHandler ??= m_NetworkBuilder.Build();
            await NetworkHandler.StartAsync(networkConfiguration);
            IsSetup = true;
        }

        string SerializeConnectionMetadata(ConnectionMetadata connectionMetadata)
        {
            try
            {
                var metadata = JsonConvert.SerializeObject(connectionMetadata);
                Logger.LogVerbose($"SerializeConnectionMetadata: {metadata}");
                return metadata;
            }
            catch (Exception)
            {
                throw new SessionException("Failed to serialize connection metadata",
                    SessionError.InvalidSessionMetadata);
            }
        }

        void DeserializeConnectionMetadata(string metadata)
        {
            try
            {
                Logger.LogVerbose($"DeserializeConnectionMetadata: {metadata}");
                ConnectionMetadata = JsonConvert.DeserializeObject<ConnectionMetadata>(metadata);

                if (ConnectionMetadata == null)
                {
                    throw new SessionException("Invalid connection metadata",
                        SessionError.InvalidSessionMetadata);
                }
            }
            catch (Exception)
            {
                throw new SessionException("Missing connection metadata",
                    SessionError.InvalidSessionMetadata);
            }
        }

        async Task SetDistributedAuthorityConnectHash(ConnectPayload connectPayload)
        {
            const string daHashKey = "_distributed_authority_connect_hash";
            m_Session.CurrentPlayer.SetProperty(daHashKey, new PlayerProperty(connectPayload.Base64SecretHash(), VisibilityPropertyOptions.Private));
            await m_Session.SaveCurrentPlayerDataAsync();
        }

        static string SecurePlatformConnectionType()
        {
#if UNITY_WEBGL
            return MPConstants.WSS;
#else
            return MPConstants.DTLS;
#endif
        }
    }
}
