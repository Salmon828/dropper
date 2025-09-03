using System.Collections.Generic;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Multiplay.Authoring.Core
{
    interface IBuildFileManagement
    {
        string DefaultBuildPath(string name);
        List<BuildEntry> GetBuildEntriesAtPath(string buildPath, IReadOnlyList<string> excludePaths);
    }
}
