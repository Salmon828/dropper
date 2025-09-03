using System.Threading.Tasks;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Represents an object that requires asynchronous initialization before use
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// The initialization that the object requires.
        /// </summary>
        /// <returns>A task for the operation.</returns>
        public Task InitAsync();
    }
}
