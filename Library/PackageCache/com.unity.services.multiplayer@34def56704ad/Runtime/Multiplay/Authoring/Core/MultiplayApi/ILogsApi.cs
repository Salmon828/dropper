using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Allows to find logs happening on the remote server
    /// </summary>
    public interface ILogsApi : IInitializable
    {
        /// <summary>
        /// Returns logs found according to the parameters.
        /// </summary>
        /// <param name="searchParams">The parameters to look for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<LogSearchResult> SearchLogsAsync(LogSearchParams searchParams, CancellationToken cancellationToken = default);
    }
}
