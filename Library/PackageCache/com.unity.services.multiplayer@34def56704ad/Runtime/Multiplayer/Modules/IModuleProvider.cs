using System;

namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// Component providing the multiplayer session module
    /// </summary>
    interface IModuleProvider
    {
        public Type Type { get; }
        /// <summary>
        /// Higher gets executed first
        /// </summary>
        public int Priority { get; }
        public IModule Build(ISession session);
    }
}
