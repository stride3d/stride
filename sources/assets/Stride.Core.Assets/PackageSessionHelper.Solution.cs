// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using NuGet.ProjectModel;
using Stride.Core.Extensions;
using Stride.Core.Solutions;

namespace Stride.Core.Assets;

internal partial class PackageSessionHelper
{
    // Not used since Xenko 3.1 anymore, so doesn't apply to Stride
    private static readonly string[] SolutionPackageIdentifier = ["XenkoPackage", "SiliconStudioPackage"];

    public static async Task<PackageVersion?> GetPackageVersion(string fullPath)
    {
        try
        {
            // Solution file: extract projects
            var solutionDirectory = Path.GetDirectoryName(fullPath) ?? "";
            var solution = Solution.FromFile(fullPath);

            foreach (var project in solution.Projects)
            {
                if (project.TypeGuid == KnownProjectTypeGuid.CSharp || project.TypeGuid == KnownProjectTypeGuid.CSharpLegacy)
                {
                    var projectPath = project.FullPath;
#if !STRIDE_LAUNCHER && !STRIDE_VSPACKAGE
                    var projectAssetsJsonPath = Path.Combine(Path.GetDirectoryName(projectPath), "obj", LockFileFormat.AssetsFileName);
                    if (!File.Exists(projectAssetsJsonPath))
                    {
                        var log = new Stride.Core.Diagnostics.LoggerResult();
                        await VSProjectHelper.RestoreNugetPackages(log, projectPath);
                    }
#endif
                    // The restored version, or the one written in the project when edited since its restore
                    if (ProjectVersionReader.ReadVersion(projectPath, "Stride.Engine", "Xenko.Engine") is { } version
                        && PackageVersion.TryParse(version, out var packageVersion))
                    {
                        return packageVersion;
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
}
