using System;
using System.IO;

namespace Unity.Services.Multiplay.Authoring.Core
{
    internal class MultiplayBuildAuthoring : IMultiplayBuildAuthoring
    {
        internal const string k_MultiplayServerBuildDirectory = "Builds/Multiplay";
        internal const string k_MultiplayServerExecutableName = "Server";

        readonly IBinaryBuilder m_BinaryBuilder;

        public MultiplayBuildAuthoring(IBinaryBuilder binaryBuilder)
        {
            m_BinaryBuilder = binaryBuilder;
        }

        public string BuildMultiplayServer(string directory, string executableName)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Directory cannot be null or whitespace when building multiplay server!", nameof(directory));
            }
            if (string.IsNullOrWhiteSpace(executableName))
            {
                throw new ArgumentException("Executable name cannot be null or whitespace when building multiplay server!", nameof(executableName));
            }
            var serverBuild = m_BinaryBuilder.BuildLinuxServer(directory, executableName);
            return serverBuild.Path;
        }

        public void WarnBuildTargetChanged()
        {
            m_BinaryBuilder.WarnBuildTargetChanged();
        }

        public void CleanBuilds()
        {
            if (Directory.Exists(k_MultiplayServerBuildDirectory))
            {
                Directory.Delete(k_MultiplayServerBuildDirectory, true);
            }
        }
    }
}
