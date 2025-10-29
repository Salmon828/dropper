using System.Collections.Generic;
using Unity.Services.Core.Internal;

namespace Unity.Services.Multiplayer
{
    interface IModuleRegistry
    {
        List<IModuleProvider> ModuleProviders { get; }
        void RegisterModuleProvider<T>(T moduleProvider) where T : IModuleProvider;
    }
}
