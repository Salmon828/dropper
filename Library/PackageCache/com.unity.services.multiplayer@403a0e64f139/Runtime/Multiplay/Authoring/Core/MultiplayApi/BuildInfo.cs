using System;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Information about a build
    /// </summary>
    /// <param name="Name">Name of the build</param>
    /// <param name="BuildId">Build ID</param>
    /// <param name="Updated">Last updated time</param>
    /// <param name="SyncStatus">Sync status</param>
    /// <param name="CloudBucketId">CCD bucket ID if any</param>
    public record BuildInfo(
        string Name,
        BuildId BuildId,
        DateTime Updated,
        string SyncStatus,
        CloudBucketId CloudBucketId);
}
