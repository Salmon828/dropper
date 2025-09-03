
using System;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// A build configuration for configuring game builds to run on servers.
    /// </summary>
    /// <param name="BuildConfigurationId">ID of the build configuration.</param>
    /// <param name="Name">Name of the build configuration.</param>
    /// <param name="BuildID">ID of the build associated with the build configuration.</param>
    /// <param name="BuildName">Name of the build associated with the build configuration.</param>
    /// <param name="Version">Version of the build configuration</param>
    public record BuildConfigInfo(
        BuildConfigurationId BuildConfigurationId,
        string Name,
        BuildId BuildID,
        string BuildName,
        long Version);
}
