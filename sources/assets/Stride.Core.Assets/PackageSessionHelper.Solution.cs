// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.VisualStudio;

namespace Stride.Core.Assets
{
    internal partial class PackageSessionHelper
    {
        // Not used since Xenko 3.1 anymore, so doesn't apply to Stride
        private static readonly string[] SolutionPackageIdentifier = new[] { "XenkoPackage", "SiliconStudioPackage" };

        public static async Task<PackageVersion> GetPackageVersion(string fullPath)
        {
            try
            {
                // Solution file: extract projects
                var solutionDirectory = Path.GetDirectoryName(fullPath) ?? "";
                var solution = Stride.Core.VisualStudio.Solution.FromFile(fullPath);

                foreach (var project in solution.Projects)
                {
                    if (project.TypeGuid == VisualStudio.KnownProjectTypeGuid.CSharp || project.TypeGuid == VisualStudio.KnownProjectTypeGuid.CSharpNewSystem)
                    {
                        var projectPath = project.FullPath;
                        var projectAssetsJsonPath = Path.Combine(Path.GetDirectoryName(projectPath), @"obj", LockFileFormat.AssetsFileName);
#if !STRIDE_LAUNCHER && !STRIDE_VSPACKAGE
                        if (!File.Exists(projectAssetsJsonPath))
                        {
                            var log = new Stride.Core.Diagnostics.LoggerResult();
                            await VSProjectHelper.RestoreNugetPackages(log, projectPath);
                        }
#endif
                        if (File.Exists(projectAssetsJsonPath))
                        {
                            var format = new LockFileFormat();
                            var projectAssets = format.Read(projectAssetsJsonPath);
                            foreach (var library in projectAssets.Libraries)
                            {
                                if ((library.Type == "package" || library.Type == "project") && (library.Name == "Stride.Engine" || library.Name == "Xenko.Engine"))
                                {
                                    return new PackageVersion((string)library.Version.ToString());
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
