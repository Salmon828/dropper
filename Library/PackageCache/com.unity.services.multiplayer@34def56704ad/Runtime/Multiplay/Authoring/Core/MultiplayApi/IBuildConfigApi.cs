using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    interface IBuildConfigApi : IInitializable
    {
        Task<BuildConfigurationId?> FindByName(string name, CancellationToken cancellationToken = default);
        Task<BuildConfigurationId> Create(string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default);
        Task Update(BuildConfigurationId id, string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BuildConfigInfo>> ListBuildConfigs(CancellationToken cancellationToken = default);
        Task Delete(BuildConfigurationId buildConfigurationId, CancellationToken cancellationToken = default);
    }
}
