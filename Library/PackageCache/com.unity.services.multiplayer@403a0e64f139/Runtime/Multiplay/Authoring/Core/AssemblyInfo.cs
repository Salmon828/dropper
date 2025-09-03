using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring.Editor")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.Multiplay")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.Multiplay.UnitTest")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.GameServerHosting")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.GameServerHosting.UnitTest")]
[assembly: InternalsVisibleTo("InternalsVisible.DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.Matchmaker")]
[assembly: InternalsVisibleTo("Unity.Services.Cli.Matchmaker.UnitTest")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Editor.Matchmaker.Authoring")]

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring.IntegrationTests.EditMode")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Multiplay.Authoring.Tests")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplayer.Tests.Editor.Matchmaker.Authoring")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplay.Hosting.E2ETests")]
#endif


// Needed to enable record types
namespace System.Runtime.CompilerServices
{
    static class IsExternalInit {}
}
