using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Services.Multiplayer
{
    class ModuleRegistry : IModuleRegistry
    {
        List<IModuleProvider> IModuleRegistry.ModuleProviders
        {
            get
            {
                var providers = ModuleProviders.Values.ToList();
                providers.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                return providers;
            }
        }

        internal readonly Dictionary<Type, IModuleProvider> ModuleProviders = new Dictionary<Type, IModuleProvider>();

        internal ModuleRegistry()
        {
        }

        public IModuleProvider GetModuleProvider<T>()
        {
            var type = typeof(T);
            return ModuleProviders.ContainsKey(type) ? ModuleProviders[type] : null;
        }

        public void RegisterModuleProvider<T>(T moduleProvider) where T : IModuleProvider
        {
            ModuleProviders.TryAdd(typeof(T), moduleProvider);
        }
    }
}
