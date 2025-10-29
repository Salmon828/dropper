namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Represents the state of a Game Server Hosting server.
    /// </summary>
    public enum MultiplaySessionManagerState
    {
        /// <summary>
        /// Mulitplay session has not yet been initialized.
        /// </summary>
        Uninitialized,
        /// <summary>
        /// Multiplay session is awaiting allocation.
        /// </summary>
        AwaitingAllocation,
        /// <summary>
        /// Multipaly session has been allocated.
        /// </summary>
        Allocated
    }
}
