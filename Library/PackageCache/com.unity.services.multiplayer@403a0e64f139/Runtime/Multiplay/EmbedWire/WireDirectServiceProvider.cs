using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Scheduler.Internal;
using Unity.Services.Core.Telemetry.Internal;
using Unity.Services.Core.Threading.Internal;
using UnityEngine;

namespace Unity.Services.Wire.Internal
{
    class WireDirectServiceProvider : IInitializablePackageV2
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            var package = new WireDirectServiceProvider();
            package.Register(CorePackageRegistry.Instance);
        }

        public void Register(CorePackageRegistry registry)
        {
            registry.Register(this)
                .DependsOn<IActionScheduler>()
                .DependsOn<IUnityThreadUtils>()
                .DependsOn<IMetricsFactory>()
                .OptionallyDependsOn<IAccessToken>()
                .ProvidesComponent<IWireDirect>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            InitializeComponent(registry);
            return Task.CompletedTask;
        }

        public Task InitializeInstanceAsync(CoreRegistry registry)
        {
            InitializeComponent(registry);
            return Task.CompletedTask;
        }

        void InitializeComponent(CoreRegistry registry)
        {
            var actionScheduler = registry.GetServiceComponent<IActionScheduler>();
            var threadUtils = registry.GetServiceComponent<IUnityThreadUtils>();
            registry.RegisterServiceComponent<IWireDirect>(new WireDirect(actionScheduler, null, threadUtils, null));
        }
    }
}
