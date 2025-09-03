using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Services.Multiplay.Authoring.YamlDotNet.RepresentationModel;
using UnityEditor;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    class MultiplayConfigYamlChecker
    {
        static readonly List<CheckItem> k_CheckConfig = new()
        {
            new CheckItem(new Regex("\\/buildConfigurations\\/.*\\/speedMhz"), "speedMhz"),
            new CheckItem(new Regex("\\/buildConfigurations\\/.*\\/cores"), "cores"),
            new CheckItem(new Regex("\\/buildConfigurations\\/.*\\/memoryMiB"), "memoryMiB"),
        };

        const string k_Separator = "/";

        const string k_DeprecatedFieldWarningMessage =
            "The game server hosting configuration file at `{0}({1},{2})` has a deprecated field ({3}). It is now advised to use the usage settings within the fleet configuration section.";

        readonly string k_Path;

        public MultiplayConfigYamlChecker(string path)
        {
            k_Path = path;
        }

        public IEnumerable<string> CheckForDeprecatedFields()
        {
            var warnings = new HashSet<string>();
            var yamlStream = new YamlStream();
            var text = File.ReadAllText(k_Path);
            using var reader = new StringReader(text);
            yamlStream.Load(reader);
            var mapping = yamlStream.Documents[0].RootNode;
            return CheckNode("", mapping);
        }

        HashSet<string> CheckNode(string prefix, YamlNode node)
        {
            var warnings = new HashSet<string>();
            if (node is YamlScalarNode scalarNode)
            {
                var match = k_CheckConfig.FirstOrDefault(item => item.pattern.IsMatch(prefix));
                if (match is not null)
                {
                    warnings.Add(
                        string.Format(k_DeprecatedFieldWarningMessage, k_Path, scalarNode.Start.Line, scalarNode.Start.Column, match.field));
                }
            }
            else if (node is YamlMappingNode mapping)
            {
                foreach (var kv in mapping)
                {
                    warnings.UnionWith(CheckNode($"{prefix}{k_Separator}{kv.Key.ToString()}", kv.Value));
                }
            }

            return warnings;
        }
    }

    record CheckItem(Regex pattern, string field);
}
