using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Configuration data for network handlers.
    /// </summary>
    /// <remarks>
    /// Used in <see cref="INetworkHandler.StartAsync"/>.
    /// </remarks>
    public class NetworkConfiguration
    {
        /// <summary>
        /// Networking role for this session.
        /// </summary>
        /// <remarks>
        /// Some flows, such as peer-to-peer matchmaking or <see cref="SessionManager.CreateOrJoinAsync"/>, will
        /// randomly assign roles to players as being either a <see cref="NetworkRole.Client"/> or a
        /// <see cref="NetworkRole.Host"/>.
        /// </remarks>
        public NetworkRole Role { get; }

        /// <summary>
        /// The type of network to use for the session.
        /// </summary>
        public NetworkType Type { get; }

        /// <summary>
        /// Endpoint (IP address and port) to be used by connecting clients.
        /// </summary>
        /// <remarks>
        /// May differ from <see cref="DirectNetworkListenAddress"/> but the ports must match.
        /// Only available when <see cref="NetworkType"/> is <see cref="NetworkType.Direct"/>.
        /// </remarks>
        public NetworkEndpoint DirectNetworkPublishAddress { get; private set; }

        /// <summary>
        /// Endpoint (IP address and port) to listen on.
        /// </summary>
        /// <remarks>
        /// May differ from <see cref="DirectNetworkPublishAddress"/> but the ports must match.
        /// Only available when <see cref="NetworkType"/> is <see cref="NetworkType.Direct"/>.
        /// </remarks>
        public NetworkEndpoint DirectNetworkListenAddress { get; }

        /// <summary>
        /// Relay configuration data for a host or server.
        /// </summary>
        /// <remarks>
        /// Not available when <see cref="NetworkType"/> is <see cref="NetworkType.Direct"/>.
        /// Not available when <see cref="Role"/> is <see cref="NetworkRole.Client"/>.
        /// </remarks>
        public RelayServerData RelayServerData { get; }

        /// <summary>
        /// Relay configuration data for a client.
        /// </summary>
        /// <remarks>
        /// Not available when <see cref="NetworkType"/> is <see cref="NetworkType.Direct"/>.
        /// </remarks>
        public RelayServerData RelayClientData { get; }

        internal NativeArray<byte> DistributedAuthorityConnectionPayload { get; } = new(0, Allocator.Temp);

        internal NetworkConfiguration(NetworkRole networkRole, string directNetworkPublishIp, int? port, string directNetworkListenIp)
        {
            Role = networkRole;
            Type = NetworkType.Direct;
            Logger.LogVerbose($"DirectNetworkPublishIp: {directNetworkPublishIp}");
            Logger.LogVerbose($"DirectNetworkListenIp: {directNetworkListenIp}");

#if !(UNITY_SWITCH || UNITY_PS4 || UNITY_PS5)

            DirectNetworkPublishAddress = NetworkEndpoint.Parse(directNetworkPublishIp, (ushort)port);
            DirectNetworkListenAddress = NetworkEndpoint.Parse(directNetworkListenIp, (ushort)port);
#else
            Logger.LogVerbose($"IPv6 is not supported on Switch, PS4 and PS5. Enforcing the provided IP address(es) to be parsed as IPV4 address(es).");

            if (NetworkEndpoint.TryParse(directNetworkPublishIp, (ushort)port, out var publishEndpoint, NetworkFamily.Ipv4))
            {
                DirectNetworkPublishAddress = publishEndpoint;
            }

            if (NetworkEndpoint.TryParse(directNetworkListenIp, (ushort)port, out var listenEndpoint, NetworkFamily.Ipv4))
            {
                DirectNetworkListenAddress = listenEndpoint;
            }
#endif
        }

        internal NetworkConfiguration(NetworkRole networkRole, NetworkType typeType, RelayServerData relayServerData, RelayServerData? relayClientData)
        {
            Role = networkRole;
            Type = typeType;
            RelayServerData = relayServerData;
            if (relayClientData != null)
            {
                RelayClientData = relayClientData.Value;
            }
        }

        internal NetworkConfiguration(
            NetworkRole networkRole,
            NetworkType typeType,
            RelayServerData relayServerData,
            NativeArray<byte> distributedAuthorityConnectionPayload,
            RelayServerData? relayClientData = null)
        {
            Role = networkRole;
            Type = typeType;
            RelayServerData = relayServerData;
            DistributedAuthorityConnectionPayload = distributedAuthorityConnectionPayload;
            if (relayClientData != null)
            {
                RelayClientData = relayClientData.Value;
            }
        }

        internal NetworkConfiguration(ConnectionMetadata metadata)
        {
            Type = metadata.Network;
            DirectNetworkPublishAddress = NetworkEndpoint.Parse(metadata.Ip, (ushort)metadata.Port);
        }

        /// <summary>
        /// Update the port for clients to connect to that will be shared through the session.
        /// </summary>
        /// <param name="port">The port for clients to connect to</param>
        public void UpdatePublishPort(ushort port)
        {
            DirectNetworkPublishAddress = DirectNetworkPublishAddress.WithPort(port);
        }
    }
}
