using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Model;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    /// <summary>
    /// Responsible to make available deployment functionality
    /// </summary>
    public interface IMultiplayDeployer
    {
        /// <summary>
        /// Initialize the MultiplayDeployer with an authenticated client
        /// </summary>
        /// <returns>A task for the operation.</returns>
        Task InitAsync();

        /// <summary>
        /// Deploy the associated Multiplay Config items.
        /// Builds will be built and uploaded, Build Configurations and fleets will
        /// be created or updated according to the item.
        /// The item status and progress will be updated along the way.
        /// </summary>
        /// <param name="items">A read-only list of Multiplay Config items.</param>
        /// <param name="token">Cancellation Token to cancel the request.</param>
        /// <returns>A task for the operation.</returns>
        Task Deploy(IReadOnlyList<DeploymentItem> items, CancellationToken token = default);

        /// <summary>
        /// Build the binaries associated with the build items
        /// </summary>
        /// <param name="buildItems">A read-only list of builds.</param>
        /// <param name="token">Cancellation Token to cancel the request.</param>
        /// <returns>A tuple containing a list of successful builds and a list of failed builds.</returns>
        Task<(List<BuildItem>, List<BuildItem>)> BuildBinaries(
            IReadOnlyList<BuildItem> buildItems,
            CancellationToken token = default);

        /// <summary>
        /// Uploads the associated builds, and waits for them to be available.
        /// </summary>
        /// <param name="successfulBuilds">A list of successful builds.</param>
        /// <param name="token">Cancellation Token to cancel the request.</param>
        /// <returns>The result of the upload.</returns>
        Task<UploadResult> UploadAndSyncBuilds(
            List<BuildItem> successfulBuilds,
            CancellationToken token = default);

        /// <summary>
        /// Creates or Updates the associated build configurations
        /// </summary>
        /// <param name="items">A read-only list of build configurations.</param>
        /// <param name="successfulUploads">A dictionary of successful uploads.</param>
        /// <param name="token">Cancellation Token to cancel the request.</param>
        /// <returns>A tuple containing a dictionary of successful build configuration deployments and a list of failed build configuration deployments.</returns>
        Task<(Dictionary<BuildConfigurationName, BuildConfigurationId>, List<BuildConfigurationItem>)> DeployBuildConfigs(
            IReadOnlyList<BuildConfigurationItem> items,
            Dictionary<BuildName, BuildId> successfulUploads,
            CancellationToken token);

        /// <summary>
        /// Creates or Updates the associated fleets
        /// </summary>
        /// <param name="items">A read-only list of fleets.</param>
        /// <param name="buildConfigIds">If the config IDs are known, they will not be searched remotely</param>
        /// <param name="token">Cancellation Token to cancel the request.</param>
        /// <returns>A task for the operation.</returns>
        Task DeployFleets(
            IReadOnlyList<FleetItem> items,
            Dictionary<BuildConfigurationName, BuildConfigurationId> buildConfigIds = null,
            CancellationToken token = default);


        /// <summary>
        /// Creates a test allocation for the associated Fleet
        /// </summary>
        /// <param name="fleetName">The name of the fleet.</param>
        /// <param name="buildConfigurationName">The name of the build configuration.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>The test allocation information.</returns>
        Task<AllocationInformation> CreateAndSyncTestAllocationAsync(
            FleetName fleetName,
            BuildConfigurationName buildConfigurationName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists existing test allocations for the associated fleet
        /// </summary>
        /// <param name="fleetId">The ID of the fleet.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A list of test allocation information.</returns>
        Task<List<AllocationInformation>> ListTestAllocations(FleetId fleetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a test allocation
        /// </summary>
        /// <param name="fleetId">The ID of the fleet.</param>
        /// <param name="allocationId">The ID of the allocation.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A task for the operation.</returns>
        Task RemoveTestAllocation(FleetId fleetId, Guid allocationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets regions that are available for fleet scaling options
        /// </summary>
        /// <returns>A dictionary of region names and their corresponding region IDs.</returns>
        Task<Dictionary<string, Guid>> GetAvailableRegions();

        /// <summary>
        /// Gets the information of the fleets
        /// </summary>
        /// <returns>A read-only list of fleets.</returns>
        public Task<IReadOnlyList<FleetInfo>> GetFleets();

        /// <summary>
        /// Deletes the associated Fleet
        /// </summary>
        /// <param name="fleetName">The name of the fleet.</param>
        /// <returns>A task for the operation.</returns>
        Task DeleteFleet(FleetId fleetName);

        /// <summary>
        /// Gets the information of the builds for the current environment
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A read-only list of builds.</returns>
        public Task<IReadOnlyList<BuildInfo>> GetBuilds(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the specified build
        /// </summary>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A task for the operation.</returns>
        Task DeleteBuild(BuildId buildId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the information of the build configurations for the current environment
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A read-only list of build configurations.</returns>
        public Task<IReadOnlyList<BuildConfigInfo>> GetBuildConfigs(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the Build Configuration
        /// </summary>
        /// <param name="buildConfigurationId">The ID of the build configuration.</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>A task for the operation.</returns>
        Task DeleteBuildConfig(BuildConfigurationId buildConfigurationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Represents the result of a build upload operation
        /// </summary>
        /// <param name="UploadResults">The individual upload result for the build item</param>
        /// <param name="FailedUploads">The Builds that failed to upload</param>
        /// <param name="SuccessfulSyncs">The builds that were successfully synced</param>
        /// <param name="FailedSyncs">The builds that failed to sync</param>
        public record UploadResult(
            Dictionary<BuildItem, BuildUploadResult> UploadResults,
            List<BuildItem> FailedUploads,
            Dictionary<BuildName, BuildId> SuccessfulSyncs,
            List<BuildItem> FailedSyncs);
    }
}
