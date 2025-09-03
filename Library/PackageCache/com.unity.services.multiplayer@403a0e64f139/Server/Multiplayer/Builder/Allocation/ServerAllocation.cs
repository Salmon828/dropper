using Unity.Services.Multiplay;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Provides information about a Game Server Hosting allocation.
    /// </summary>
    public class ServerAllocation : IMultiplayAllocation
    {
        /// <summary>
        /// The Id of the allocation
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// The Id of the event that created the allocation.
        /// </summary>
        public string EventId { get; }

        /// <summary>
        /// The Id of the server that is hosting the allocation of the game server.
        /// </summary>
        public long ServerId { get; }

        internal bool IsMatchmakerAllocation { get; set; }

        internal ServerAllocation(IMultiplayAllocationInfo allocation)
        {
            ID = allocation.AllocationId;
            EventId = allocation.EventId;
            ServerId = allocation.ServerId;
        }
    }
}
