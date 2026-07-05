// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Samples.Generator;

namespace Stride.Packaging.Tests;

/// <summary>
/// Shared paths + headless generation/MSBuild bootstrap for the end-user packaging tests.
/// </summary>
internal static class TestEnvironment
{
    /// <summary>Repo root: walk up from the test bin output to the directory holding nuget.config.</summary>
    public static string WorktreeRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NuGet.config")) || File.Exists(Path.Combine(dir.FullName, "nuget.config")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate worktree root from " + AppContext.BaseDirectory);
    }

    /// <summary>The locally-built package feed; the engine build/pack must have populated it.</summary>
    public static string BinPackages()
    {
        var binPackages = Path.Combine(WorktreeRoot(), "bin", "packages");
        if (!Directory.Exists(binPackages))
            throw new DirectoryNotFoundException($"bin/packages not found at {binPackages}; build the engine first.");
        return binPackages;
    }

    /// <summary>The platform variant matching the host OS, as used by the dotnet-new templates.</summary>
    public static string HostPlatform => OperatingSystem.IsLinux() ? "linux"
                                       : OperatingSystem.IsMacOS() ? "macos"
                                       : "windows";

    /// <summary>Committed plugin/consumer fixtures (packed/built as subprocesses).</summary>
    public static string FixturesDir() => Path.Combine(WorktreeRoot(), "tests", "enduser", "Stride.Packaging.Tests", "Fixtures");

    /// <summary>
    /// The locally-built Stride NuGet version to pin fixtures to. Prefers a prerelease (the dev
    /// auto-deploy stamp, e.g. 4.4.0.2-dev1); CI's clean feed has exactly one.
    /// </summary>
    public static string ResolveStrideVersion()
    {
        const string prefix = "Stride.Engine.";
        var versions = Directory.EnumerateFiles(BinPackages(), "Stride.Engine.*.nupkg")
            .Select(f => Path.GetFileNameWithoutExtension(f)[prefix.Length..])
            .ToList();
        if (versions.Count == 0)
            throw new InvalidOperationException($"No Stride.Engine package found in {BinPackages()}.");
        var prerelease = versions.Where(v => v.Contains('-')).ToList();
        return (prerelease.Count > 0 ? prerelease : versions).OrderByDescending(v => v, StringComparer.Ordinal).First();
    }

    /// <summary>Recursively copy a directory, skipping bin/obj.</summary>
    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj")
                continue;
            Directory.CreateDirectory(dir.Replace(source, dest));
        }
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;
            File.Copy(file, file.Replace(source, dest), overwrite: true);
        }
    }

    private static bool msbuildReady;

    /// <summary>Generate a sample from its template GUID into a fresh temp directory; returns the .slnx path.</summary>
    public static string GenerateSample(Guid templateGuid, string sampleName)
    {
        if (!msbuildReady)
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            DotNetNewTemplateBridge.RegisterProjectTemplates();
            msbuildReady = true;
        }

        var outputDir = Path.Combine(Path.GetTempPath(), "stride-packaging-tests", sampleName);
        var logger = new LoggerResult();
        var session = SampleGenerator.Generate(new UDirectory(outputDir), templateGuid, sampleName, logger);
        return session.SolutionPath.ToOSPath();
    }
}
