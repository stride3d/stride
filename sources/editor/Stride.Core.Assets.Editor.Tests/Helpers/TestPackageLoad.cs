// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// Loads the package that a test wrote next to its project without the NuGet restore and the MSBuild evaluation of the project,
/// which have nothing to give for it (<see cref="PackageSession.LoadWithoutMSBuild"/>).
/// </summary>
public static class TestPackageLoad
{
    /// <summary>
    /// Loads the package of a library, or of the executable of the given platform, into the given result.
    /// </summary>
    /// <remarks>
    /// The files of the project are the files of its folder, without <c>bin</c> and <c>obj</c>, as the .NET SDK lists them by
    /// default: the scripts that the test wrote are the project assets of the session.
    /// </remarks>
    public static void WithoutMSBuild(UFile packagePath, PlatformType? executablePlatform, PackageSessionResult result, PackageLoadParameters loadParameters)
    {
        var projectDirectory = packagePath.GetFullDirectory().ToOSPath();
        string[] outputs = [Path.Combine(projectDirectory, "bin") + Path.DirectorySeparatorChar, Path.Combine(projectDirectory, "obj") + Path.DirectorySeparatorChar];
        var projectFiles = Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
            .Where(x => !outputs.Any(output => x.StartsWith(output, StringComparison.OrdinalIgnoreCase)))
            .Select(x => new UFile(x))
            .ToList();
        PackageSession.LoadWithoutMSBuild(packagePath, executablePlatform is null ? ProjectType.Library : ProjectType.Executable, result, loadParameters,
            executablePlatform ?? PlatformType.Shared, projectFiles);
    }
}
