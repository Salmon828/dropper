namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Provides extension methods to configure session networking options.
    /// </summary>
    public static partial class SessionOptionsExtensions
    {
        /// <summary>
        /// Configures a session to use Relay networking.
        /// </summary>
        /// <remarks>
        /// <para>The region is optional; the default behavior is to perform quality of service (QoS) measurements and
        /// pick the lowest latency region. The list of regions can be obtained from the Relay Allocations Service via
        /// <see cref="Unity.Services.Relay.IRelayService.ListRegionsAsync"/>.</para>
        /// <para>When using Netcode for Entities, the default Multiplayer Services Network handler requires that your
        /// Client and Server Worlds are created before you create or join a session. Using the default
        /// <c>ClientServerBootstrap</c>, automatically creates the client and server worlds at startup when the Netcode
        /// for Entities package is first added to your project. For more advanced use cases, use <see
        /// cref="WithNetworkHandler{T}"/> to disable the default integration with Netcode for Entities.</para>
        /// </remarks>
        /// <param name="options">The SessionOptions this extension method applies to.</param>
        /// <param name="region">Force a specific Relay region to be used and skip auto-selection from QoS.</param>
        /// <typeparam name="T">The options' type.</typeparam>
        /// <returns>The session options</returns>
        public static T WithRelayNetwork<T>(this T options, string region = null) where T : SessionOptions
        {
            return options.WithOption(new ConnectionOption(ConnectionInfo.BuildRelay(region)));
        }

        /// <summary>
        /// Configures a session to use direct networking and accept connections at the specified address.
        /// </summary>
        /// <remarks>
        /// <para>Using direct networking in client-hosted games reveals the IP address of players to the host. For
        /// client-hosted games, using Relay or Distributed Authority is recommended to handle NAT, firewalls and
        /// protect player privacy.</para>
        /// <para>The default values allow local connections only and use 127.0.0.1 as the <paramref name="listenIp"/>
        /// and <paramref name="publishIp"/>. To listen on all interfaces, use 0.0.0.0 as the listenIp and specify the
        /// external/public IP address that clients should use as the publishIp.</para>
        /// <para>The port number defaults to 0 which selects a randomly available port on the machine and uses the
        /// chosen value as the publish port. If a non-zero value is used, the port number applies to both listen and
        /// publish addresses.</para>
        /// <para>When using Netcode for Entities, the default Multiplayer Services Network handler requires that your
        /// Client and Server Worlds are created before you create or join a session. Using the default
        /// <c>ClientServerBootstrap</c>, automatically creates the client and server worlds at startup when the Netcode
        /// for Entities package is first added to your project. For more advanced use cases, use <see
        /// cref="WithNetworkHandler{T}"/> to disable the default integration with Netcode for Entities.</para>
        /// </remarks>
        /// <param name="options">The SessionOptions this extension method applies to.</param>
        /// <param name="listenIp">Listen for incoming connection at this address ("0.0.0.0" for all
        /// interfaces).</param>
        /// <param name="publishIp">Address that clients should use when connecting</param>
        /// <param name="port">Port to listen for incoming connections and also the one to use by clients</param>
        /// <typeparam name="T">The options' type.</typeparam>
        /// <returns>The session options</returns>
        public static T WithDirectNetwork<T>(this T options, string listenIp = "127.0.0.1",
            string publishIp = "127.0.0.1", int port = 0) where T : SessionOptions
        {
            return options.WithOption(new ConnectionOption(ConnectionInfo.BuildDirect(publishIp, port, listenIp)));
        }

        /// <summary>
        /// Configures a session to use a custom network handler.
        /// </summary>
        /// <remarks>
        /// <para>When a network handler is provided, it disables the default integration with Netcode for Game Objects
        /// and Netcode for Entities.</para>
        /// <para>Combine this option with another networking option (<see cref="WithDirectNetwork{T}"/>, <see
        /// cref="WithRelayNetwork{T}"/>, <see cref="WithDistributedAuthorityNetwork{T}"/>) to obtain the appropriate
        /// data to implement a custom management of the netcode library and/or transport library.</para>
        /// <para>This option applies to all session flows and is normally set for all roles (host, server,
        /// client).</para>
        /// </remarks>
        /// <param name="options">The SessionOptions this extension method applies to.</param>
        /// <param name="networkHandler">The <see cref="INetworkHandler"/> to use </param>
        /// <typeparam name="T">The options' type.</typeparam>
        /// <returns>The session options</returns>
        public static T WithNetworkHandler<T>(this T options, INetworkHandler networkHandler)
            where T : BaseSessionOptions
        {
            return options.WithOption(new NetworkOption(networkHandler));
        }

#if GAMEOBJECTS_NETCODE_2_AVAILABLE || PACKAGE_DOCS_GENERATION
        /// <summary>
        /// Configures a session to use the distributed authority networking.
        /// </summary>
        /// <remarks>
        /// The region is optional and defaults to "us-central1". The list of regions can be obtained from the Relay
        /// Allocations Service via <see cref="Unity.Services.Relay.IRelayService.ListRegionsAsync"/>. To determine the
        /// lowest latency region, use <see cref="Unity.Services.Qos.IQosService.GetSortedRelayQosResultsAsync"/>.
        /// </remarks>
        /// <param name="options">The <see cref="SessionOptions"/> this extension method applies to.</param>
        /// <param name="region">The Relay region where the Relay allocation used by Distributed Authority will
        /// happen.</param>
        /// <typeparam name="T">The options' type.</typeparam>
        /// <returns>The <see cref="SessionOptions"/>.</returns>
        public static T WithDistributedAuthorityNetwork<T>(this T options, string region = null)
            where T : SessionOptions
        {
            return options.WithOption(new ConnectionOption(ConnectionInfo.BuildDistributedConnection(region)));
        }

#endif
    }
}
