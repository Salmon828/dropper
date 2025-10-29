using Unity.Services.Matchmaker.Models;

namespace Unity.Services.Multiplayer
{
    class ConnectionInfo
    {
        internal NetworkType Network { get; private set; }
        internal string Ip { get; private set; }
        internal int Port { get; private set; }
        internal string ListenAddress { get; private set; }
        internal string Region { get; private set; }

        ConnectionInfo()
        {
        }

        public static ConnectionInfo BuildDirect(string ip, int port, string listenAddress)
        {
            return new ConnectionInfo()
            {
                Network = NetworkType.Direct,
                Ip = ip,
                Port = port,
                ListenAddress = listenAddress
            };
        }

        public static ConnectionInfo BuildRelay(string region = null)
        {
            return new ConnectionInfo()
            {
                Network = NetworkType.Relay,
                Region = region
            };
        }

        public static ConnectionInfo BuildDistributedConnection(string region = null)
        {
            return new ConnectionInfo()
            {
                Network = NetworkType.DistributedAuthority,
                Region = region
            };
        }

        public void OverrideDirectConnectionInfo(string ip, int port, string listenAddress)
        {
            Ip = ip;
            Port = port;
            ListenAddress = listenAddress;
        }
    }
}
