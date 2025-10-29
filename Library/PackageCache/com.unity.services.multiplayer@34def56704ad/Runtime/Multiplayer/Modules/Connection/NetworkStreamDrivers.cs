#if ENTITIES_NETCODE_AVAILABLE
using Unity.Entities;
using Unity.NetCode;

namespace Unity.Services.Multiplayer
{
    internal class NetworkStreamDrivers
    {
        public NetworkStreamDriver? ServerNetworkStreamDriver  { get; set; }
        public NetworkStreamDriver? ClientNetworkStreamDriver  { get; set; }
        public EntityManager ClientEntityManager { get; set; }
    }
}
#endif
