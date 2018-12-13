// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.VisualStudio;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Version = System.Version;

namespace Xenko.Core.Assets
{
    internal partial class PackageSessionHelper
    {
        private const string XenkoPackage = "XenkoPackage";
        private static readonly string[] SolutionPackageIdentifier = new[] { XenkoPackage, "SiliconStudioPackage" };

        public static async Task<PackageVersion> GetPackageVersion(string fullPath)
        {
            try
            {
                // Solution file: extract projects
                var solutionDirectory = Path.GetDirectoryName(fullPath) ?? "";
                var solution = Xenko.Core.VisualStudio.Solution.FromFile(fullPath);

                foreach (var project in solution.Projects)
                {
                    // Xenko up to 3.0
                    try
                    {
                        string packagePath;
                        if (IsPackage(project, out packagePath))
                        {
                            var packageFullPath = Path.Combine(solutionDirectory, packagePath);

                            // Load the package as a Yaml dynamic node, so that we can check Xenko version from dependencies
                            var input = new StringReader(File.ReadAllText(packageFullPath));
                            var yamlStream = new YamlStream();
                            yamlStream.Load(input);
                            dynamic yamlRootNode = new DynamicYamlMapping((YamlMappingNode)yamlStream.Documents[0].RootNode);

                            if (yamlRootNode.Meta.Dependencies is DynamicYamlArray)
                            {
                                foreach (var dependency in yamlRootNode.Meta.Dependencies)
                                {
                                    if ((string)dependency.Name == "Xenko")
                                    {
                                        return new PackageVersion((string)dependency.Version);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        e.Ignore();
                    }

                    // Xenko 3.1+
                    if (project.TypeGuid == VisualStudio.KnownProjectTypeGuid.CSharp || project.TypeGuid == VisualStudio.KnownProjectTypeGuid.CSharpNewSystem)
                    {
                        var projectPath = project.FullPath;
                        var packagePath = Path.ChangeExtension(projectPath, Package.PackageFileExtension);
                        if (File.Exists(packagePath))
                        {
                            var projectAssetsJsonPath = Path.Combine(Path.GetDirectoryName(projectPath), @"obj", LockFileFormat.AssetsFileName);
#if !XENKO_LAUNCHER
                            if (!File.Exists(projectAssetsJsonPath))
                            {
                                var log = new LoggerResult();
                                await VSProjectHelper.RestoreNugetPackages(log, projectPath);
                            }
#endif
                            if (File.Exists(projectAssetsJsonPath))
                            {
                                if (File.Exists(projectAssetsJsonPath))
                                {
                                    var format = new LockFileFormat();
                                    var projectAssets = format.Read(projectAssetsJsonPath);
                                    foreach (var library in projectAssets.Libraries)
                                    {
                                        if (library.Type == "package" && library.Name == "Xenko.Engine")
                                        {
                                            return new PackageVersion((string)library.Version.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            return null;
        }

        internal static bool IsPackage(Project project)
        {
            string packagePath;
            return IsPackage(project, out packagePath);
        }

        internal static bool IsPackage(Project project, out string packagePathRelative)
        {
            packagePathRelative = null;
            if (project.IsSolutionFolder)
            {
                foreach (var solutionPackageIdentifier in SolutionPackageIdentifier)
                if (project.Sections.Contains(solutionPackageIdentifier))
                {
                    var propertyItem = project.Sections[solutionPackageIdentifier].Properties.FirstOrDefault();
                    if (propertyItem != null)
                    {
                        packagePathRelative = propertyItem.Name;
                        return true;
                    }
                }
            }
            return false;
        }

        internal static void RemovePackageSections(Project project)
        {
            if (project.IsSolutionFolder)
            {
                foreach (var solutionPackageIdentifier in SolutionPackageIdentifier)
                    project.Sections.Remove(solutionPackageIdentifier);
            }
        }
    }
}
