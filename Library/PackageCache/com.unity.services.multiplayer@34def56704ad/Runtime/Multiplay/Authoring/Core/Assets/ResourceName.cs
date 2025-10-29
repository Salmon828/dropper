namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Represents a resource's name
    /// </summary>
    public interface IResourceName
    {
        /// <summary>
        /// The name of the resource.
        /// </summary>
        string Name { get; init; }
    }
}
