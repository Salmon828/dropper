using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Fetch
{
    interface IMatchmakerFetchHandler
    {
        Task<FetchResult> FetchAsync(string rootDir, IReadOnlyList<string> filePaths, bool reconcile, bool dryRun, CancellationToken ct = default);
    }
}
