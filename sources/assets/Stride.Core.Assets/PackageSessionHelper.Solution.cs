// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using NuGet.ProjectModel;
using Stride.Core.Extensions;
using Stride.Core.VisualStudio;

namespace Stride.Core.Assets;

internal partial class PackageSessionHelper
{
    // Not used since Xenko 3.1 anymore, so doesn't apply to Stride
    private static readonly string[] SolutionPackageIdentifier = ["XenkoPackage", "SiliconStudioPackage"];

    public static async Task<PackageVersion?> GetPackageVersion(string fullPath)
    {
        try
        {
            if (Path.GetExtension(fullPath).Equals(Package.PackageFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                var packageVersion = await TryGetPackageVersion(fullPath);
                if (packageVersion is not null)
                {
                    return packageVersion;
                }
            }
            else if (Regex.IsMatch(Path.GetExtension(fullPath), @"\.slnf?$", RegexOptions.IgnoreCase))
            {
                // Solution file: extract projects
                var solution = Path.GetExtension(fullPath).Equals(".sln", StringComparison.InvariantCultureIgnoreCase)
                    ? Solution.FromFile(fullPath)
                    : Solution.FromSolutionFilter(fullPath);
                foreach (var project in solution.Projects)
                {
                    if (project.TypeGuid == KnownProjectTypeGuid.CSharp || project.TypeGuid == KnownProjectTypeGuid.CSharpNewSystem)
                    {
                        var projectPath = project.FullPath;
                        var projectVersion = await TryGetPackageVersion(projectPath);
                        if (projectVersion is not null)
                        {
                            return projectVersion;
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
        return IsPackage(project, out _);
    }

    internal static bool IsPackage(Project project, [MaybeNullWhen(false)] out string packagePathRelative)
    {
        packagePathRelative = null;
        if (project.IsSolutionFolder)
        {
            foreach (var solutionPackageIdentifier in SolutionPackageIdentifier)
            {
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

    private static async Task<PackageVersion?> TryGetPackageVersion(string projectPath)
    {
        var projectAssetsJsonPath = Path.Combine(Path.GetDirectoryName(projectPath), "obj", LockFileFormat.AssetsFileName);
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
                    return new PackageVersion(library.Version.ToString());
                }
            }
        }
        return null;
    }
}
