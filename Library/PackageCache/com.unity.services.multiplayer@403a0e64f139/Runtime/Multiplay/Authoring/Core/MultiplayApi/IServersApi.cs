using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Provides access to server information for the project.
    /// See https://services.docs.unity.com/multiplay-config/v1/#tag/Servers for details
    /// </summary>
    public interface IServersApi : IInitializable
    {
        /// <summary>
        /// Trigger an action against the server with the given ID.
        /// </summary>
        /// <param name="serverId">The ID of the server on which to trigger the action.</param>
        /// <param name="action">The action to trigger.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>True if the action was successfully triggered, false otherwise.</returns>
        Task<bool> TriggerServerActionAsync(long serverId, ServerAction action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the details of a single server with the given ID.
        /// </summary>
        /// <param name="serverId">The ID of the server.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>The details of the server with the given ID.</returns>
        Task<ServerInfo> GetServerAsync(long serverId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists the Action Logs for a server.
        /// </summary>
        /// <param name="serverId">The ID of the server.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A list of Action Logs for the server with the given ID.</returns>
        Task<List<ActionLog>> GetServerActionLogsAsync(long serverId, CancellationToken cancellationToken = default);
    }
}
