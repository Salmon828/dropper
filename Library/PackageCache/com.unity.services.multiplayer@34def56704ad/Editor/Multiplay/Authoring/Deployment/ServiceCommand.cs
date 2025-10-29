using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;

namespace Unity.Services.Multiplay.Authoring.Editor.Deployment
{
    abstract class ServiceCommand : Command<DeploymentItem>
    {
        // Using service locator pattern (anti-pattern) to make the DeployCommand a service entrypoint
        // Can be set in order to override for testing
        internal IScopedServiceProvider Provider { get; set; } = MultiplayAuthoringServices.Provider;
    }
}
