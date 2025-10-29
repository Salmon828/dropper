using System;
using System.Collections.Generic;
using Unity.Networking.Transport.Relay;

namespace Unity.Services.Relay.Models
{
    /// <summary>
    /// Utility methods for relay allocations
    /// </summary>
    public static class AllocationUtils
    {
        /// <summary>
        /// Convert an allocation to Transport's RelayServerData model
        /// </summary>
        /// <param name="allocation">Allocation from which to create the server data.</param>
        /// <param name="connectionType">Type of connection to use ("udp", "dtls", "ws", or "wss").</param>
        /// <returns>Relay server data model for Transport</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if allocation is null, if the connection type is invalid or if no endpoint match the connection type
        /// </exception>
        public static RelayServerData ToRelayServerData(this Allocation allocation, string connectionType)
        {
            if (allocation == null)
            {
                throw new ArgumentException($"Invalid allocation.");
            }

            ValidateRelayConnectionType(connectionType);

            var isWebSocket = connectionType == "ws" || connectionType == "wss";
            var endpoint = GetEndpoint(allocation.ServerEndpoints, connectionType);

            return new RelayServerData(
                host: endpoint.Host,
                port: (ushort)endpoint.Port,
                allocationId: allocation.AllocationIdBytes,
                connectionData: allocation.ConnectionData,
                hostConnectionData: allocation.ConnectionData,
                key: allocation.Key,
                isSecure: endpoint.Secure,
                isWebSocket: isWebSocket);
        }

        /// <summary>
        /// Convert an allocation to Transport's RelayServerData model
        /// </summary>
        /// <param name="allocation">Allocation from which to create the server data.</param>
        /// <param name="connectionType">Type of connection to use ("udp", "dtls", "ws", or "wss").</param>
        /// <returns>Relay server data model for Transport</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if allocation is null, if the connection type is invalid or if no endpoint match the connection type
        /// </exception>
        public static RelayServerData ToRelayServerData(this JoinAllocation allocation, string connectionType)
        {
            if (allocation == null)
            {
                throw new ArgumentException($"Invalid allocation.");
            }

            ValidateRelayConnectionType(connectionType);

            var isWebSocket = connectionType == "ws" || connectionType == "wss";
            var endpoint = GetEndpoint(allocation.ServerEndpoints, connectionType);

            return new RelayServerData(
                host: endpoint.Host,
                port: (ushort)endpoint.Port,
                allocationId: allocation.AllocationIdBytes,
                connectionData: allocation.ConnectionData,
                hostConnectionData: allocation.HostConnectionData,
                key: allocation.Key,
                isSecure: endpoint.Secure,
                isWebSocket: isWebSocket);
        }

        static RelayServerEndpoint GetEndpoint(List<RelayServerEndpoint> endpoints, string connectionType)
        {
            if (endpoints != null)
            {
                foreach (var serverEndpoint in endpoints)
                {
                    if (serverEndpoint.ConnectionType == connectionType)
                    {
                        return serverEndpoint;
                    }
                }
            }

            throw new ArgumentException($"No endpoint for connection type '{connectionType}' in allocation.");
        }

        static void ValidateRelayConnectionType(string connectionType)
        {
            // We check against a hardcoded list of strings instead of just trying to find the
            // connection type in the endpoints since it may contains things we don't support
            // (e.g. they provide a "tcp" endpoint which we don't support).
            if (connectionType != "udp" && connectionType != "dtls" && connectionType != "ws" && connectionType != "wss")
            {
                throw new ArgumentException($"Invalid connection type: {connectionType}. Must be udp, dtls, ws or wss.");
            }

#if UNITY_WEBGL
            if (connectionType == "udp" || connectionType == "dtls")
            {
                Multiplayer.Logger.LogWarning($"Relay connection type is set to \"{connectionType}\" which is not valid on WebGL. Use \"wss\" instead.");
            }
#endif
        }
    }
}
