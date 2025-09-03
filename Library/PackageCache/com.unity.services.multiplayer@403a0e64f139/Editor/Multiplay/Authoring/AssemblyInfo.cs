using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring.IntegrationTests.EditMode")]
[assembly: InternalsVisibleTo("InternalsVisible.DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Tests.Editor.Matchmaker.Authoring")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplay.Hosting.E2ETests")]
#endif

[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Editor.Matchmaker.Authoring")]

// Needed to enable record types
namespace System.Runtime.CompilerServices
{
    static class IsExternalInit {}
}
