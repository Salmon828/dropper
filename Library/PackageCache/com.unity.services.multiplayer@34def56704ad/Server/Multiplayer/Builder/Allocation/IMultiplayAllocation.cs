namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Represents an allocation for a Game Server Hosting server.
    /// </summary>
    public interface IMultiplayAllocation
    {
        /// <summary>
        /// The ID for the allocation.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// The event ID for the allocation.
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// The server ID for the allocation.
        /// </summary>
        long ServerId { get; }
    }
}
