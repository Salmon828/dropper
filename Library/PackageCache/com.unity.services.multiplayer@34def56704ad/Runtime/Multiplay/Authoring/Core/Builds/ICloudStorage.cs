using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Multiplay.Authoring.Core.Builds
{
    interface ICloudStorage : IInitializable
    {
        Task<int> UploadBuildEntries(CloudBucketId bucket, IList<BuildEntry> localEntries, Action<BuildEntry> onUpdated, CancellationToken cancellationToken = default);
    }
}
