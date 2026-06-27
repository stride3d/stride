// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using System.Text.Json;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Packages;
using Stride.Core.Solutions;

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
    ///   a major.minor line (newest patch in that line), or null for the newest version. The newest/line
    ///   resolution prefers stable releases unless <paramref name="includePrerelease"/> is set; an explicit
    ///   version is always honored. Existing versions are left untouched.
    /// </summary>
    public async Task<StrideVersion> Install(string? versionSpec, bool includePrerelease, CancellationToken cancellationToken)
    {
        var available = (await store.FindSourcePackagesById(MainPackageId, cancellationToken))
            .OrderByDescending(package => package.Version)
            .ToList();

        var target = Resolve(available, versionSpec, includePrerelease)
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
    public async Task<IReadOnlyList<StrideVersion>> Update(string? line, bool includePrerelease, CancellationToken cancellationToken)
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
            var newest = available.FirstOrDefault(package => LineOf(package.Version) == targetLine
                && (includePrerelease || string.IsNullOrEmpty(package.Version.SpecialVersion)));
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

        var projectFiles = GetProjectFiles(workingDirectory);
        if (projectFiles.Count == 0)
            return GetDefault()?.Version;  // not inside a project: use the newest installed version

        var version = FirstRestoredVersion(projectFiles);
        if (version is not null)
            return version;

        // A project is present but not restored yet: restore the projects, then read the version again.
        foreach (var projectFile in projectFiles)
        {
            Restore(projectFile);
            version = ReadRestoredStrideVersion(projectFile);
            if (version is not null)
                return version;
        }

        throw new InvalidOperationException(
            $"Could not determine the Stride version for the project in '{workingDirectory}', even after restoring it.");
    }

    // Template packages carry a Stride.Core dependency stamping the engine version they were built against
    // (see Stride.Templates.Common.targets).
    private const string TemplateMarkerDependencyId = "Stride.Core";

    // Discovery is scoped to packages carrying this tag (set by Stride.Templates.Common.targets), so a
    // template package from an unrelated local source (e.g. the VS offline packages folder) is not picked up.
    private const string TemplateTag = "stride-template";

    /// <summary>
    ///   Opens a template registry over the installed template packages compatible with the given version,
    ///   plus any explicitly requested <paramref name="extraPackages"/> (a package id or a local .nupkg path).
    ///   Returns null if none could be installed. The caller owns the returned registry and must dispose it.
    /// </summary>
    public async Task<DotNetNewTemplateRegistry?> OpenTemplateRegistry(PackageVersion version, IEnumerable<string>? extraPackages = null)
    {
        // Keep template-engine state under a CLI-owned, per-version directory so it stays isolated
        // from the user's global `dotnet new` installation and is deterministic regardless of whether
        // Game Studio has been run.
        var profileDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "stride", "cli", "templates", version.ToString());

        var registry = new DotNetNewTemplateRegistry(version.ToString(), profileDir);
        var installedAny = false;
        foreach (var packageDir in ResolveTemplatePackages(version, extraPackages))
        {
            var (success, _) = await registry.InstallPackageAsync(packageDir);
            installedAny |= success;
        }

        if (!installedAny)
        {
            registry.Dispose();
            return null;
        }

        return registry;
    }

    // The directories to install into the registry: one per discovered template package, plus any explicit
    // extra packages.
    private IEnumerable<string> ResolveTemplatePackages(PackageVersion version, IEnumerable<string>? extraPackages)
    {
        // Discover template packages by package type + tag, then per id pick the newest version whose
        // Stride.Core marker floor is <= the requested version (unmarked packages fall back to newest).
        var discovered = store.GetAllPackagesInstalled()
            .Where(package => package.IsTemplatePackage && package.HasTag(TemplateTag))
            .GroupBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .Where(package => package.GetDependencyFloor(TemplateMarkerDependencyId) is not { } floor || floor.CompareTo(version) <= 0)
                .OrderByDescending(package => package.Version)
                .FirstOrDefault())
            .OfType<NugetLocalPackage>();

        foreach (var package in discovered)
        {
            // Prefer the extracted directory; fall back to the loose .nupkg of a not-yet-mirrored local source.
            var path = package.NupkgPath ?? package.Path;
            if (!string.IsNullOrEmpty(path))
                yield return path;
        }

        foreach (var extra in extraPackages ?? [])
        {
            // A local .nupkg or already-extracted directory is installed as-is; anything else is treated as a
            // package id and resolved to its newest installed copy.
            if (File.Exists(extra) || Directory.Exists(extra))
            {
                yield return extra;
                continue;
            }

            var newest = store.GetPackagesInstalled([extra]).FirstOrDefault();
            var resolved = newest is null ? null : store.GetInstalledPath(newest.Id, newest.Version);
            if (!string.IsNullOrEmpty(resolved))
                yield return resolved;
        }
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

    // The csproj files in the current directory, plus those referenced by any .sln/.slnx/.slnf there. Mirrors
    // how `dotnet build` finds its target: the current directory only, not a recursive scan.
    private static List<string> GetProjectFiles(string workingDirectory)
    {
        if (!Directory.Exists(workingDirectory))
            return [];

        var solutionFiles = Directory.EnumerateFiles(workingDirectory).Where(Solution.IsSolutionFile);
        return Directory.EnumerateFiles(workingDirectory, "*.csproj")
            .Concat(solutionFiles.SelectMany(ReferencedProjects))
            .ToList();
    }

    private static PackageVersion? FirstRestoredVersion(IEnumerable<string> projectFiles)
        => projectFiles.Select(ReadRestoredStrideVersion).FirstOrDefault(version => version is not null);

    // Runs `dotnet restore <project>` so the project produces a project.assets.json.
    private static void Restore(string projectFile)
    {
        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(projectFile);
        Process.Start(startInfo)?.WaitForExit();
    }

    // The csproj paths referenced by a solution. Stride.Core.Solutions handles the .sln/.slnx formats and
    // resolves a .slnf filter to its underlying solution.
    private static IEnumerable<string> ReferencedProjects(string solutionFile)
        => Solution.FromFile(solutionFile).Projects
            .Select(project => project.FullPath)
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

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

        // Keys look like "Stride.Engine/4.4.0". Read the engine version from the canonical engine package
        // (Stride.Engine), falling back to Stride.Core for a code library that references only Core. Other
        // Stride.*-prefixed packages can use independent versioning, so they aren't reliable indicators.
        PackageVersion? engine = null, core = null;
        foreach (var library in libraries.EnumerateObject())
        {
            var separator = library.Name.IndexOf('/');
            if (separator <= 0)
                continue;

            var packageId = library.Name[..separator];
            if (string.Equals(packageId, "Stride.Engine", StringComparison.OrdinalIgnoreCase))
                engine = new PackageVersion(library.Name[(separator + 1)..]);
            else if (string.Equals(packageId, "Stride.Core", StringComparison.OrdinalIgnoreCase))
                core = new PackageVersion(library.Name[(separator + 1)..]);
        }

        return engine ?? core;
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
    private static NugetServerPackage? Resolve(IReadOnlyList<NugetServerPackage> available, string? versionSpec, bool includePrerelease)
    {
        // Newest/line resolution skips prereleases unless opted in; an explicit version is always honored.
        bool Eligible(NugetServerPackage package) => includePrerelease || string.IsNullOrEmpty(package.Version.SpecialVersion);

        if (string.IsNullOrEmpty(versionSpec))
            return available.FirstOrDefault(Eligible);

        var parts = versionSpec.Split('.');
        if (parts.Length == 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            return available.FirstOrDefault(package => package.Version.Version.Major == major && package.Version.Version.Minor == minor && Eligible(package));

        var target = new PackageVersion(versionSpec);
        return available.FirstOrDefault(package => package.Version.Equals(target));
    }
}
