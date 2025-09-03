using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unity.Services.Multiplay.Authoring.Core.Builds
{
    class FileReader : IFileReader
    {
        string k_AllFilesSearchPattern = "*.*";

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public IReadOnlyList<string> EnumerateFiles(string path, string pattern = null)
        {
            pattern ??= k_AllFilesSearchPattern;
            return Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories).ToList();
        }

        public IReadOnlyList<string> EnumerateFilesWithPattern(string pathWithPattern)
        {
            var fileName = Path.GetFileName(pathWithPattern);
            var dirPath = Path.GetDirectoryName(pathWithPattern);
            var isPattern = fileName.Contains("*") || fileName.Contains("?");
            //Pattern _in_ string
            if (isPattern)
            {
                if (DirectoryExists(dirPath))
                    return EnumerateFiles(dirPath, fileName).ToList();
                return Array.Empty<string>();
            }

            // Directory directly
            if (DirectoryExists(pathWithPattern))
                return EnumerateFiles(pathWithPattern).ToList();

            //Just a file
            return new[] { pathWithPattern };
        }

        public Stream OpenReadFile(string path)
        {
            return File.OpenRead(path);
        }
    }
}
