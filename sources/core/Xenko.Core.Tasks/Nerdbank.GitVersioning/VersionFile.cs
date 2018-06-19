// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Xenko.Core;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;

namespace Nerdbank.GitVersioning
{
    /// <summary>
    /// Read version from .xkpkg, implemented for <see cref="GitExtensions"/>.
    /// </summary>
    class VersionFile
    {
        /// <summary>
        /// Reads version from the given .xkpkg file.
        /// </summary>
        /// <param name="packagePath"></param>
        /// <returns></returns>
        public static VersionOptions GetVersion(string packagePath)
        {
            try
            {
                using (var fileStream = File.OpenRead(packagePath))
                {
                    return GetVersionFromStream(fileStream);
                }
            }
            catch
            {
                return null;
            }
        }

        public static VersionOptions GetVersion(LibGit2Sharp.Commit commit, string packagePath)
        {
            if (commit == null)
            {
                return null;
            }

            try
            {
                var packageData = commit.Tree[packagePath]?.Target as LibGit2Sharp.Blob;
                if (packageData == null)
                    return null;

                return GetVersionFromStream(packageData.GetContentStream());
            }
            catch
            {
                return null;
            }
        }

        private static VersionOptions GetVersionFromStream(Stream stream)
        {
            // Load the asset as a YamlNode object
            var input = new StreamReader(stream);
            var yamlStream = new YamlStream();
            yamlStream.Load(input);

            // Version is stored in Meta.Version
            var rootNode = (YamlMappingNode)yamlStream.Documents[0].RootNode;
            var metaNode = rootNode?.Children[new YamlScalarNode("Meta")] as YamlMappingNode;
            if (metaNode == null)
                return null;

            var versionNode = (YamlScalarNode)metaNode.Children[new YamlScalarNode("Version")];

            return new VersionOptions { Version = new PackageVersion((string)versionNode) };
        }
    }
}
