using System;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Represents a type-safe Build Configuration ID
    /// </summary>
    public readonly struct BuildConfigurationId : IEquatable<BuildConfigurationId>
    {
        /// <summary>
        /// The numerical value of the Build Config
        /// </summary>
        public long Id { get; init; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">BuildConfigurationId to compare against.</param>
        /// <returns><c>True</c> if the two objects are equatable, <c>false</c> otherwise.</returns>
        public bool Equals(BuildConfigurationId other)
        {
            return Id == other.Id;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="obj">Another object to compare agaianst.</param>
        /// <returns><c>True</c> if the two objects are equatable, <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is BuildConfigurationId other && Equals(other);
        }

        /// <summary>
        /// Calculates a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
