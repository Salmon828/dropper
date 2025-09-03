using System;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Represents the options for to start the Game Server Hosting server and create the session the player will connect to.
    /// </summary>
    public class MultiplaySessionManagerOptions
    {
        /// <summary>
        /// Options for the session creation.
        /// </summary>
        public SessionOptions SessionOptions { get; set; }

        /// <summary>
        /// Callbacks for the Game Server Hosting server events.
        /// </summary>
        public MultiplaySessionManagerEventCallbacks Callbacks;

        /// <summary>
        /// Options for the Game Server Hosting server.
        /// </summary>
        public MultiplayServerOptions MultiplayServerOptions { get; set; }
    }
}
