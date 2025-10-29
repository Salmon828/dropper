using System;

namespace Unity.Services.Multiplay
{
    /// <summary>
    /// Here is the first point and call for accessing Multiplay package's
    /// features!
    /// Use the <see cref="Instance" /> method to get a singleton of the <see
    /// cref="IMultiplayService" /> and from there you can make various requests
    /// to the Multiplay service API.
    /// </summary>
    /// <remarks>The <see cref="MultiplayService"/> is not available in the
    /// editor, tyring to access it will result in an <see
    /// cref="InvalidOperationException"/>.</remarks>
    public static class MultiplayService
    {
        private static IMultiplayService m_Service;

        /// <summary>
        /// Provides the Multiplay Service interface for making service API
        /// requests.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown in the following
        /// scenarios:
        /// <list type="number">
        /// <item>Trying to access the Multiplay API before initializing the
        /// Unity services.</item>
        /// <item>Trying to access the Multiplay API while not deployed in
        /// Multiplay Hosting (i.e. in editor).</item>
        /// </list>
        /// </exception>
        public static IMultiplayService Instance
        {
            get
            {
                if (m_Service == null)
                {
                    throw new InvalidOperationException(
                        $"Unable to get {nameof(IMultiplayService)} because " +
                        "Multiplay API is not initialized. Make sure you are " +
                        "deployed in Multiplay Hosting and you call " +
                        "UnityServices.InitializeAsync() before attempting " +
                        "to access the Multiplay API.");
                }

                return m_Service;
            }
            internal set
            {
                m_Service = value;
            }
        }
    }
}
