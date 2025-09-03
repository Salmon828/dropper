using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Facade for session browsing, session management and matchmaking.
    /// </summary>
    public interface IMultiplayerServerService
    {
        /// <summary>
        /// Creates a server session.
        /// </summary>
        /// <param name="sessionOptions">The options for the resulting session</param>
        /// <returns>The created server session</returns>
        /// <exception cref="SessionException">Provides a specific session error type and error message.</exception>
        public Task<IServerSession> CreateSessionAsync(SessionOptions sessionOptions);

        /// <summary>
        /// Starts the Multiplay server session manager.
        /// The session manager should be started on server startup. It handles the session management within the server
        /// lifecycle.
        /// </summary>
        /// <param name="options">The options to start the session manager.</param>
        /// <returns>The session manager.</returns>
        /// <exception cref="SessionException">
        /// <para>Thrown when called from a non Multiplay Game Server Hosting server. The <see
        /// cref="SessionException.Error"/> property will be set to <see
        /// cref="SessionError.InvalidPlatformOperation"/>.</para>
        /// </exception>
        public Task<IMultiplaySessionManager> StartMultiplaySessionManagerAsync(MultiplaySessionManagerOptions options);
    }

    /// <summary>
    /// The entry class of the Multiplayer SDK and session system.
    /// </summary>
    public static class MultiplayerServerService
    {
        /// <summary>
        /// A static instance of the Multiplayer service and session system.
        /// </summary>
        public static IMultiplayerServerService Instance { get; internal set; }
    }
}
