namespace Unity.Services.Multiplayer
{
    internal class ConnectionMetadata
    {
        public NetworkType Network { get; set; }

        public string Ip { get; set; }
        public int Port { get; set; }
        public string RelayJoinCode { get; set; }
        public string HostId { get; set; }
    }
}
