using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Multiplay.Authoring.Core
{
    internal interface IBinaryBuilder
    {
        ServerBuild BuildLinuxServer(string outDir, string executable);

        void WarnBuildTargetChanged();
    }
}
