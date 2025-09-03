using System.IO;

namespace Unity.Services.Multiplay.Authoring.Core.Builds
{
    record BuildEntry(string Path, Stream Content, bool Skipped = false);
}
