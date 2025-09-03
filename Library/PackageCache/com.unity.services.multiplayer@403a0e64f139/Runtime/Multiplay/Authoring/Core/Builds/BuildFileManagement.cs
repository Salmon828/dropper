using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unity.Services.Multiplay.Authoring.Core.Builds
{
    class BuildFileManagement : IBuildFileManagement
    {
        const string k_BuildDirectory = "Builds/Multiplay/";

        IFileReader m_FileReader;

        public BuildFileManagement(IFileReader fileReader)
        {
            m_FileReader = fileReader;
        }

        public string DefaultBuildPath(string buildItemName)
        {
            return $"{k_BuildDirectory}{buildItemName}";
        }

        public List<BuildEntry> GetBuildEntriesAtPath(string buildPath, IReadOnlyList<string> excludePaths)
        {
            var buildEntries = new List<BuildEntry>();
            var excludedFiles = excludePaths.SelectMany(ep => m_FileReader.EnumerateFilesWithPattern(ep)).ToList();
            var excludedFilesSet = excludedFiles.Select(Path.GetFullPath).ToHashSet();
            foreach (var filePath in m_FileReader.EnumerateFiles(buildPath))
            {
                bool skip = excludedFilesSet.Contains(Path.GetFullPath(filePath));
                var fileStream = skip ? null : m_FileReader.OpenReadFile(filePath);
                var uploadPath = filePath.Substring(buildPath.Length).Replace("\\", "/");
                var buildEntry = new BuildEntry(uploadPath, fileStream, skip);
                buildEntries.Add(buildEntry);
            }
            return buildEntries;
        }
    }
}
