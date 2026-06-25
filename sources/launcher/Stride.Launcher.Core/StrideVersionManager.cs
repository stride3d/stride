// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text.Json;
using System.Text.RegularExpressions;
using Stride.Core;
using Stride.Core.Packages;

namespace Stride.Launcher.Core;

/// <summary>
///   Manages locally installed Stride versions through the shared NuGet store, so the CLI and the
///   launcher operate on the same set of installed versions.
/// </summary>
public sealed class StrideVersionManager
{
    private const string MainPackageId = "Stride.GameStudio";

    private readonly NugetStore store = new(oldRootDirectory: null);
    private readonly ManagedVersionStore managedVersions = new();

    /// <summary>Lists the installed Stride versions, newest first.</summary>
    public IReadOnlyList<StrideVersion> GetInstalled()
    {
        return store.GetPackagesInstalled(store.MainPackageIds)
            .Where(package => package.Id == MainPackageId)
            .OrderByDescending(package => package.Version)
            .Select(ToStrideVersion)
            .ToList();
    }

    /// <summary>
    ///   Installs a Stride version and returns it. <paramref name="versionSpec"/> may be a full version,
    ///   a major.minor line (newest patch in that line), or null for the newest available. Existing
    ///   versions are left untouched.
    /// </summary>
    public async Task<StrideVersion> Install(string? versionSpec, CancellationToken cancellationToken)
    {
        var available = (await store.FindSourcePackagesById(MainPackageId, cancellationToken))
            .OrderByDescending(package => package.Version)
            .ToList();

        var target = Resolve(available, versionSpec)
            ?? throw new InvalidOperationException(string.IsNullOrEmpty(versionSpec)
                ? "No Stride version is available from the package source."
                : $"No Stride version matching '{versionSpec}' is available.");

        var installed = await store.InstallPackage(target.Id, target.Version, target.TargetFrameworks, progress: null)
            ?? throw new InvalidOperationException($"Failed to install Stride {target.Version}.");

        return ToStrideVersion(installed);
    }

    /// <summary>Uninstalls the given Stride version. Returns false if it was not installed.</summary>
    public async Task<bool> Uninstall(string versionSpec)
    {
        var local = store.FindLocalPackage(MainPackageId, new PackageVersion(versionSpec));
        if (local is null)
            return false;

        await store.UninstallPackage(local, progress: null);
        return true;
    }

    /// <summary>
    ///   Updates installed lines to their newest patch: installs the newest patch, records it as the line's
    ///   managed version, and uninstalls the previously managed one (manual installs are kept). With no line,
    ///   every installed line is updated. Returns the versions that were updated.
    /// </summary>
    public async Task<IReadOnlyList<StrideVersion>> Update(string? line, CancellationToken cancellationToken)
    {
        var managed = managedVersions.Load();
        var lines = line is not null
            ? new[] { line }
            : GetInstalled().Select(version => version.Line).Distinct().ToArray();

        var available = (await store.FindSourcePackagesById(MainPackageId, cancellationToken))
            .OrderByDescending(package => package.Version)
            .ToList();

        var updated = new List<StrideVersion>();
        foreach (var targetLine in lines)
        {
            var newest = available.FirstOrDefault(package => LineOf(package.Version) == targetLine);
            if (newest is null)
                continue;

            var installedPackage = await store.InstallPackage(newest.Id, newest.Version, newest.TargetFrameworks, progress: null);
            if (installedPackage is null)
                continue;

            // Retire the version this line previously managed, unless it is the one just installed.
            if (managed.TryGetValue(targetLine, out var previous) && previous != newest.Version.ToString())
            {
                var previousPackage = store.FindLocalPackage(MainPackageId, new PackageVersion(previous));
                if (previousPackage is not null)
                    await store.UninstallPackage(previousPackage, progress: null);
            }

            managed[targetLine] = newest.Version.ToString();
            updated.Add(ToStrideVersion(installedPackage));
        }

        managedVersions.Save(managed);
        return updated;
    }

    private static string LineOf(PackageVersion version) => $"{version.Version.Major}.{version.Version.Minor}";

    /// <summary>The newest installed version, or null if none is installed.</summary>
    public StrideVersion? GetDefault() => GetInstalled().FirstOrDefault();

    /// <summary>
    ///   Resolves the Stride version to use: <paramref name="explicitVersion"/> if given, otherwise the
    ///   version pinned by a project under <paramref name="workingDirectory"/>, otherwise the newest installed.
    /// </summary>
    public PackageVersion? ResolveVersion(string? explicitVersion, string workingDirectory)
    {
        if (!string.IsNullOrEmpty(explicitVersion))
            return new PackageVersion(explicitVersion);

        return FindProjectVersion(workingDirectory) ?? GetDefault()?.Version;
    }

    /// <summary>Locates the Game Studio executable for the given version, or null if not found.</summary>
    public string? LocateGameStudio(PackageVersion version)
        => LocateExecutable("Stride.GameStudio", version, "Stride.GameStudio.Avalonia.Desktop.exe", "Stride.GameStudio.exe");

    /// <summary>
    ///   Locates the asset compiler executable for the given version, or null if not found. The package is
    ///   Stride.AssetCompiler from 4.4.0 onwards and Stride.Core.Assets.CompilerApp before that.
    /// </summary>
    public string? LocateAssetCompiler(PackageVersion version)
    {
        var packageId = version.Version >= new Version(4, 4, 0) ? "Stride.AssetCompiler" : "Stride.Core.Assets.CompilerApp";
        return LocateExecutable(packageId, version, packageId + ".exe");
    }

    // Resolves the Stride version pinned by the project or solution in the current directory, mirroring how
    // `dotnet build` finds its target there: a .csproj, or the projects a .sln references. Subdirectories are
    // not scanned. Returns null if nothing is found or the project has not been restored.
    private static PackageVersion? FindProjectVersion(string workingDirectory)
    {
        if (!Directory.Exists(workingDirectory))
            return null;

        var projectFiles = Directory.EnumerateFiles(workingDirectory, "*.csproj")
            .Concat(Directory.EnumerateFiles(workingDirectory, "*.sln").SelectMany(ReferencedProjects));

        return projectFiles.Select(ReadRestoredStrideVersion).FirstOrDefault(version => version is not null);
    }

    // The project paths referenced by a solution file, resolved relative to it.
    private static IEnumerable<string> ReferencedProjects(string solutionFile)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionFile)!;
        foreach (Match match in Regex.Matches(File.ReadAllText(solutionFile), @"=\s*""[^""]*"",\s*""([^""]+\.csproj)"""))
            yield return Path.Combine(solutionDirectory, match.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar));
    }

    // The resolved Stride version from the project's restored obj/project.assets.json. This avoids a full
    // MSBuild evaluation: NuGet already resolved central package management, version ranges and props into the
    // flat "libraries" list. Assumes the default obj/ location; if it is redirected or not yet restored,
    // returns null so the caller can restore and retry.
    private static PackageVersion? ReadRestoredStrideVersion(string projectFile)
    {
        var assetsFile = Path.Combine(Path.GetDirectoryName(projectFile)!, "obj", "project.assets.json");
        if (!File.Exists(assetsFile))
            return null;

        using var document = JsonDocument.Parse(File.ReadAllText(assetsFile));
        if (!document.RootElement.TryGetProperty("libraries", out var libraries))
            return null;

        foreach (var library in libraries.EnumerateObject())
        {
            // Keys look like "Stride.Engine/4.4.0.1".
            var separator = library.Name.IndexOf('/');
            if (separator <= 0)
                continue;

            var packageId = library.Name[..separator];
            if (packageId == "Stride" || packageId.StartsWith("Stride.", StringComparison.Ordinal))
                return new PackageVersion(library.Name[(separator + 1)..]);
        }

        return null;
    }

    // Searches the package's tools/ and lib/ folders for the first matching executable.
    private string? LocateExecutable(string packageId, PackageVersion version, params string[] executableNames)
    {
        var installPath = store.GetInstalledPath(packageId, version);
        if (string.IsNullOrEmpty(installPath))
            return null;

        foreach (var topLevelFolder in new[] { "tools", "lib" })
        {
            var directory = Path.Combine(installPath, topLevelFolder);
            if (!Directory.Exists(directory))
                continue;

            foreach (var executableName in executableNames)
            {
                var match = Directory.EnumerateFiles(directory, executableName, SearchOption.AllDirectories).FirstOrDefault();
                if (match is not null)
                    return match;
            }
        }

        return null;
    }

    private StrideVersion ToStrideVersion(NugetLocalPackage package)
        => new(package.Id, package.Version, store.GetInstalledPath(package.Id, package.Version));

    // available is newest-first. A "major.minor" spec resolves to the newest patch in that line.
    private static NugetServerPackage? Resolve(IReadOnlyList<NugetServerPackage> available, string? versionSpec)
    {
        if (string.IsNullOrEmpty(versionSpec))
            return available.FirstOrDefault();

        var parts = versionSpec.Split('.');
        if (parts.Length == 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            return available.FirstOrDefault(package => package.Version.Version.Major == major && package.Version.Version.Minor == minor);

        var target = new PackageVersion(versionSpec);
        return available.FirstOrDefault(package => package.Version.Equals(target));
    }
}
