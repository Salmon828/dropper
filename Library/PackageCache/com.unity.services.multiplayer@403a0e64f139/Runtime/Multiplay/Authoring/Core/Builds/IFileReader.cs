using System.Collections.Generic;
using System.IO;

namespace Unity.Services.Multiplay.Authoring.Core.Builds
{
    interface IFileReader
    {
        IReadOnlyList<string> EnumerateFiles(string path, string pattern = null);
        IReadOnlyList<string> EnumerateFilesWithPattern(string pathWithPattern);
        Stream OpenReadFile(string path);

        bool DirectoryExists(string path);
    }
}
