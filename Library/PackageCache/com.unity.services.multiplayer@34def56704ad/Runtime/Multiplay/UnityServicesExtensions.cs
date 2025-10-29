using Unity.Services.Multiplay;

namespace Unity.Services.Core
{
    /// <summary>
    /// Unity services extensions
    /// </summary>
    public static class UnityServicesExtensions
    {
        /// <summary>
        /// Retrieve the multiplay service from the core service registry
        /// </summary>
        /// <param name="unityServices">The core services instance</param>
        /// <returns>The multiplay service instance</returns>
        public static IMultiplayService GetMultiplayService(this IUnityServices unityServices)
        {
            return unityServices.GetService<IMultiplayService>();
        }
    }
}
