// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.Frameworks;
using NuGet.ProjectModel;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Yaml;

namespace Stride.Core.Assets;

partial class PackageSession
{
    /// <summary>
    /// Loads the companion packages (<see cref="Package.CompanionPackages"/>) this host takes, including companions of companions.
    /// Each comes from the session, a dev-redirect stub or a NuGet restore, at its declared version.
    /// </summary>
    /// <param name="rootDirectory">The root directory used to resolve packages, if available.</param>
    /// <param name="log">The logger for package loading.</param>
    /// <param name="loadEditorPackages">Whether to load editor companion packages.</param>
    /// <param name="editorToolkit">The editor's UI toolkit (Wpf or Avalonia); Editor companions for another toolkit are skipped.</param>
    /// <remarks>An Editor companion is also skipped when the session's <see cref="CompanionPackageFilter"/> says so.</remarks>
    internal void LoadCompanionPackages(string? rootDirectory, ILogger log, bool loadEditorPackages, string? editorToolkit)
    {
        var loadedByName = new Dictionary<string, StandalonePackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in Projects.OfType<StandalonePackage>())
        {
            if (container.Package.Meta.Name is { } name)
                loadedByName.TryAdd(name, container);
        }

        // Every replacement counts, whichever package declares it and whether or not this host takes the replacing entry
        var replaced = new HashSet<string>(Packages.SelectMany(p => p.CompanionPackages).SelectMany(c => c.Replaces), StringComparer.OrdinalIgnoreCase);

        // The Editor companions the editor leaves out, decided before anything loads: one that is already in the session (a
        // project of the solution) must not bring its own companions in, whatever order the packages are visited in
        var filteredOut = FindFilteredOutCompanions(replaced, loadEditorPackages, editorToolkit, log);

        var visited = new HashSet<Package>();
        var sourceVisited = false;
        while (true)
        {
            var package = Packages.FirstOrDefault(p => !visited.Contains(p));
            if (package is null)
            {
                // Once the declared companions are in, the ones the host adds (their own companions come with the next turns)
                if (sourceVisited || !loadEditorPackages)
                    break;
                sourceVisited = true;
                LoadSourceCompanionPackages(replaced, filteredOut, loadedByName, rootDirectory, log, editorToolkit);
                continue;
            }
            visited.Add(package);
            if (package.Meta.Name is { } packageName && filteredOut.GetValueOrDefault(packageName))
                continue;
            foreach (var companion in package.CompanionPackages.ToList())
            {
                foreach (var name in companion.Replaces)
                    replaced.Add(name);
                if (companion.Name is not null && replaced.Contains(companion.Name))
                    continue;
                if (!IsLoadedByThisHost(companion, loadEditorPackages, editorToolkit))
                    SkipCompanionPackage(package, companion, loadEditorPackages, editorToolkit, log);
                else if (IsFilteredOut(package, companion, filteredOut, log))
                    MarkSkippedProject(companion);
                else
                    LoadCompanionPackage(package, companion, loadedByName, rootDirectory, log);
            }
        }
    }

    // The filter's decision for each Editor companion of the packages in the session (true: left out), and the companions
    // that a companion left out declares, left out with it. The decisions are kept for the session: the filter is asked, and
    // logs why it leaves a companion out, once per companion, however many times the companions load
    private Dictionary<string, bool> FindFilteredOutCompanions(HashSet<string> replaced, bool loadEditorPackages, string? editorToolkit, ILogger log)
    {
        if (CompanionPackageFilter is null || !loadEditorPackages)
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var decisions = companionFilterDecisions;

        foreach (var package in Packages.ToList())
        {
            foreach (var companion in package.CompanionPackages)
            {
                if (companion.Name is not null && !replaced.Contains(companion.Name) && IsLoadedByThisHost(companion, loadEditorPackages, editorToolkit))
                    IsFilteredOut(package, companion, decisions, log);
            }
        }
        bool added;
        do
        {
            added = false;
            foreach (var package in Packages.Where(p => p.Meta.Name is { } name && decisions.GetValueOrDefault(name)).ToList())
            {
                foreach (var companion in package.CompanionPackages.Where(c => c is { Kind: PackageKind.Editor, Name: not null }))
                {
                    if (decisions.TryAdd(companion.Name!, true) || !decisions[companion.Name!])
                    {
                        decisions[companion.Name!] = true;
                        MarkSkippedProject(companion);
                        added = true;
                    }
                }
            }
        }
        while (added);
        return decisions;
    }

    // The companions the host adds (ICompanionPackageSource), read at the first load with editor packages. One that a package
    // of the session already brings, or declares, is not added: the session's own one loads
    private void LoadSourceCompanionPackages(HashSet<string> replaced, Dictionary<string, bool> filteredOut, Dictionary<string, StandalonePackage> loadedByName, string? rootDirectory, ILogger log, string? editorToolkit)
    {
        if (CompanionPackageSource is not { } source)
            return;
        if (SourceCompanionPackages is null)
        {
            try
            {
                SourceCompanionPackages = source.GetCompanionPackages(log).Where(x => x is not null).ToList();
            }
            catch (Exception ex)
            {
                log.Error("Could not read the packages the editor adds to the session.", ex);
                SourceCompanionPackages = [];
            }
        }

        var declared = new HashSet<string>(Packages.SelectMany(p => p.CompanionPackages).Select(c => c.Name).NotNull(), StringComparer.OrdinalIgnoreCase);
        foreach (var companion in SourceCompanionPackages)
        {
            if (companion.Name is not { } name || loadedSourceCompanions.Contains(name))
                continue;
            if (companion.Version is not { } version)
            {
                log.Warning($"[{name}], added by the editor, has no version: it is not loaded.");
                continue;
            }
            if (replaced.Contains(name))
                continue;
            if (!IsLoadedByThisHost(companion, loadEditorPackages: true, editorToolkit))
            {
                if (companion.Kind == PackageKind.Editor && reportedOtherToolkitCompanions.Add(name))
                    log.Info($"[{name}], added by the editor, is written for the {companion.Toolkit} editor; this editor ({editorToolkit ?? "no toolkit"}) does not load it.");
                continue;
            }
            if (declared.Contains(name) || Projects.Any(p => string.Equals(p.Package.Meta.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                var inSession = Projects.FirstOrDefault(p => string.Equals(p.Package.Meta.Name, name, StringComparison.OrdinalIgnoreCase))?.Package.Meta.Version;
                log.Info($"[{name}] version [{version}], added by the editor, is also a package of this session{(inSession is not null ? $" (version [{inSession}])" : "")}: the session's one loads.");
                continue;
            }
            if (IsFilteredOut(null, companion, filteredOut, log))
                continue;

            var loaded = LoadCompanionPackage(name, version, loadedByName, rootDirectory, log, source.PackageSources);
            if (loaded is null)
            {
                log.Error($"{companion.Kind} package [{name}] version [{version}], added by the editor, was not found.");
                continue;
            }
            // NuGet takes a package from any of its sources: the editor checks that the restored one is the package it added
            if (!source.IsRestoredPackageExpected(name, version, log))
            {
                Projects.Remove(loaded);
                loadedByName.Remove(name);
                continue;
            }
            // A packed package states its kind and toolkit: one that is not what the editor added it as stays out
            if (loaded.Package.Kind != companion.Kind || (loaded.Package.Toolkit is not null && !string.Equals(loaded.Package.Toolkit, editorToolkit, StringComparison.OrdinalIgnoreCase)))
            {
                log.Error($"[{name}], added by the editor as {(companion.Kind == PackageKind.Editor ? "an" : "a")} {companion.Kind} package, is {(loaded.Package.Kind == PackageKind.Editor ? "an" : "a")} {loaded.Package.Kind} package for the {loaded.Package.Toolkit ?? "neutral"} toolkit: it is not loaded.");
                Projects.Remove(loaded);
                loadedByName.Remove(name);
                continue;
            }
            loaded.IsCompanionPackage = true;
            loadedSourceCompanions.Add(name);
            sourcePackageNames.Add(name);
        }
    }

    // The names of the companions the host added that this session loaded
    private readonly HashSet<string> loadedSourceCompanions = new(StringComparer.OrdinalIgnoreCase);

    // Those, with the companions they declare: restored from the source's feeds too
    private readonly HashSet<string> sourcePackageNames = new(StringComparer.OrdinalIgnoreCase);

    // The filter's decisions, kept for the session (see FindFilteredOutCompanions)
    private readonly Dictionary<string, bool> companionFilterDecisions = new(StringComparer.OrdinalIgnoreCase);

    // The Editor companions of another toolkit whose skipping was logged: once per session
    private readonly HashSet<string> reportedOtherToolkitCompanions = new(StringComparer.OrdinalIgnoreCase);

    // A companion this host does not take can still be a project of the solution: its assembly is not loaded either
    private void SkipCompanionPackage(Package package, CompanionPackage companion, bool loadEditorPackages, string? editorToolkit, ILogger log)
    {
        if (loadEditorPackages && companion.Kind == PackageKind.Editor && reportedOtherToolkitCompanions.Add(companion.Name ?? string.Empty))
            log.Info($"[{companion.Name}], declared by [{package.Meta.Name}], is written for the {companion.Toolkit} editor; this editor ({editorToolkit ?? "no toolkit"}) does not load it.");

        MarkSkippedProject(companion);
    }

    private void MarkSkippedProject(CompanionPackage companion)
    {
        var project = Projects.OfType<SolutionProject>().FirstOrDefault(p => string.Equals(p.Package.Meta.Name, companion.Name, StringComparison.OrdinalIgnoreCase));
        if (project is not null)
            project.IsSkippedByThisHost = true;
    }

    // The editor can leave out an Editor companion it would load (a plugin the user switched off, safe mode), asked once per
    // companion; an Assets companion always loads, since the assets of the session need its types
    private bool IsFilteredOut(Package? package, CompanionPackage companion, Dictionary<string, bool> decisions, ILogger log)
    {
        if (companion.Kind != PackageKind.Editor || companion.Name is not { } name || CompanionPackageFilter is not { } filter)
            return false;
        if (!decisions.TryGetValue(name, out var filteredOut))
            decisions[name] = filteredOut = !filter.ShouldLoad(package, companion, log);
        return filteredOut;
    }

    // Which host loads which companion: the asset compiler takes the Assets kind, the editor takes every kind, the
    // views of its own toolkit only
    private static bool IsLoadedByThisHost(CompanionPackage companion, bool loadEditorPackages, string? editorToolkit)
    {
        return companion.Kind switch
        {
            PackageKind.Assets => true,
            PackageKind.Editor => loadEditorPackages && (companion.Toolkit is null || string.Equals(companion.Toolkit, editorToolkit, StringComparison.OrdinalIgnoreCase)),
            _ => false,
        };
    }

    private void LoadCompanionPackage(Package package, CompanionPackage declaration, Dictionary<string, StandalonePackage> loadedByName, string? rootDirectory, ILogger log)
    {
        // A declaration without a version (packed before versions were recorded) means the declaring package's own
        var companionName = declaration.Name;
        var version = declaration.Version ?? package.Meta.Version;
        if (companionName is null || version is null)
            return;
        var kind = declaration.Kind;

        var companion = Projects.FirstOrDefault(p => string.Equals(p.Package.Meta.Name, companionName, StringComparison.OrdinalIgnoreCase));
        // Loading the companions again (a dependency was added) leaves the ones already linked as they are
        if (companion is not null && companionLinks.Contains((package, companion.Package)))
            return;
        if (companion is null)
        {
            // The companions of a package the host added come from where it came from
            var fromSource = package.Meta.Name is { } declaringName && sourcePackageNames.Contains(declaringName);
            var loaded = LoadCompanionPackage(companionName, version, loadedByName, rootDirectory, log, fromSource ? CompanionPackageSource?.PackageSources : null);
            if (loaded is not null && fromSource)
            {
                // Restored from the same sources as the package that declares it: checked the same way
                if (CompanionPackageSource is { } source && !source.IsRestoredPackageExpected(companionName, version, log))
                {
                    Projects.Remove(loaded);
                    loadedByName.Remove(companionName);
                    return;
                }
                sourcePackageNames.Add(companionName);
            }
            if (loaded is null)
            {
                log.Error($"{kind} package [{companionName}] version [{version}], declared by [{package.Meta.Name}], was not found; assets of that package cannot be compiled or edited.");
                return;
            }
            loaded.IsCompanionPackage = true;
            companion = loaded;
        }
        else if (companion is StandalonePackage { IsDependencyPackage: true, IsCompanionPackage: false })
        {
            log.Warning($"[{companionName}] is referenced directly; reference [{package.Meta.Name}] instead, which loads it on its own.");
        }

        // A project in the session is the companion's source: nothing to compare its version with
        if (companion is not SolutionProject && companion.Package.Meta.Version != version)
            log.Error($"{kind} package [{companionName}] is version [{companion.Package.Meta.Version}] but [{package.Meta.Name}] declares version [{version}]; both must match.");

        // A packed companion states its kind and toolkit; an authored sdpkg does not (a solution project answers Runtime)
        if (companion.Package.Kind != PackageKind.Runtime && companion.Package.Kind != kind)
            log.Error($"[{companionName}] is an {companion.Package.Kind} package but [{package.Meta.Name}] declares it as its {kind} package.");
        else if (companion.Package.Kind != PackageKind.Runtime && !string.Equals(companion.Package.Toolkit, declaration.Toolkit, StringComparison.OrdinalIgnoreCase))
            log.Error($"[{companionName}] is written for the {companion.Package.Toolkit ?? "neutral"} toolkit but [{package.Meta.Name}] declares it for the {declaration.Toolkit ?? "neutral"} one.");

        AddCompanionLink(package, companion.Package);
    }

    // Each loaded companion with the package declaring it
    private readonly List<(Package Declaring, Package Companion)> companionLinks = [];

    /// <summary>
    /// Makes <paramref name="companion"/> visible wherever <paramref name="declaring"/> is: in the flattened dependencies
    /// of every project of the session that has it.
    /// </summary>
    internal void AddCompanionLink(Package declaring, Package companion)
    {
        companionLinks.Add((declaring, companion));
        foreach (var container in Projects)
            AddCompanionDependencies(container);
    }

    /// <summary>
    /// Adds to <paramref name="container"/>'s flattened dependencies the loaded companions of the packages it has,
    /// companions of companions included; for a list rebuilt from the project's lock file.
    /// </summary>
    internal void AddCompanionDependencies(PackageContainer container)
    {
        // A companion sees what its declaring package sees (Stride.UI.Assets' system library uses the engine's default font)
        foreach (var (declaring, companion) in companionLinks)
        {
            if (container.Package != companion || Projects.FirstOrDefault(p => p.Package == declaring) is not { } declaringContainer)
                continue;
            foreach (var dependency in declaringContainer.FlattenedDependencies.Select(d => d.Package).Prepend(declaring).NotNull())
            {
                if (dependency != companion && !container.FlattenedDependencies.Any(d => d.Package == dependency))
                    container.FlattenedDependencies.Add(new Dependency(dependency));
            }
        }

        bool added;
        do
        {
            added = false;
            foreach (var (declaring, companion) in companionLinks)
            {
                if (container.Package == companion || container.FlattenedDependencies.Any(d => d.Package == companion) || !Projects.Any(p => p.Package == companion))
                    continue;
                if (container.Package == declaring || container.FlattenedDependencies.Any(d => d.Package == declaring))
                {
                    container.FlattenedDependencies.Add(new Dependency(companion));
                    added = true;
                }
            }
        }
        while (added);
    }

    private StandalonePackage? LoadCompanionPackage(string name, PackageVersion version, Dictionary<string, StandalonePackage> loadedByName, string? rootDirectory, ILogger log, IReadOnlyList<string>? additionalSources = null)
    {
        if (TryLoadDevRedirectPackage(name, version, log) is { } devRedirect)
            return devRedirect;

        // Restore into the local store (a no-op once installed)
        var hostFramework = GetHostTargetFramework();
        LockFile lockFile;
        try
        {
            var outputPath = rootDirectory is not null ? Path.Combine(rootDirectory, "obj", "stride", "companions", name) : Path.Combine(Path.GetTempPath(), "stride-companions", name);
            lockFile = PackageStore.Instance.RestorePackage(name, version, hostFramework, rootDirectory, outputPath, additionalSources ?? []).Result;
        }
        catch (Exception ex)
        {
            log.Error($"Could not restore package [{name}] version [{version}]", ex);
            return null;
        }

        // A freshly installed dev-redirect stub loads from its source tree; anything else through the lock file
        // closure like any other package dependency
        if (TryLoadDevRedirectPackage(name, version, log) is { } installedDevRedirect)
            return installedDevRedirect;
        var loadedBefore = new HashSet<string>(loadedByName.Keys, StringComparer.OrdinalIgnoreCase);
        LoadPackageDependenciesFromLockFile(lockFile, NuGetFramework.Parse(hostFramework), loadedByName, log);

        // A companion can depend on another plugin's companion (an asset type deriving from that plugin's): it comes
        // with this closure, so it is a companion too, not a package the game references directly
        foreach (var (loadedName, container) in loadedByName)
        {
            if (!loadedBefore.Contains(loadedName) && container.Package.Kind != PackageKind.Runtime)
                container.IsCompanionPackage = true;
        }
        return loadedByName.GetValueOrDefault(name);
    }

    // Loads a dev-redirect stub from its source tree. A plain version (4.4.0) also matches this checkout's dev stub (4.4.0-devN).
    private StandalonePackage? TryLoadDevRedirectPackage(string name, PackageVersion version, ILogger log)
    {
        var directory = FindStorePackageDirectory(name, version, out var packageFile);
        if (directory is null && string.IsNullOrEmpty(version.SpecialVersion))
        {
            var devVersion = new PackageVersion(StrideVersion.NuGetVersion);
            if (devVersion.Version == version.Version && devVersion.SpecialVersion.StartsWith("dev", StringComparison.Ordinal))
                directory = FindStorePackageDirectory(name, devVersion, out packageFile);
        }
        if (directory is null)
            return null;
        var projectFile = Path.Combine(directory, name + ".csproj");
        var manifestFile = File.Exists(projectFile) ? FindDevRedirectManifest(projectFile) : null;
        return manifestFile is not null ? LoadDevRedirectPackage(log, packageFile, projectFile, manifestFile, name, version) : null;
    }

    // The store directory of the package at exactly that version (a dev-redirect stub resolves to its source tree),
    // and its sdpkg when it has one
    private static string? FindStorePackageDirectory(string name, PackageVersion version, out string? packageFile)
    {
        var range = new PackageVersionRange(version);
        packageFile = PackageStore.Instance.GetPackageFileName(name, range)?.ToOSPath();
        return packageFile is not null ? Path.GetDirectoryName(packageFile) : PackageStore.Instance.GetPackageDirectory(name, range)?.ToOSPath();
    }

    private StandalonePackage LoadDevRedirectPackage(ILogger log, string? packageFile, string projectFile, string manifestFile, string name, PackageVersion version)
    {
        var package = packageFile is not null
            ? Package.LoadRaw(log, packageFile)
            : new Package { FullPath = Path.ChangeExtension(projectFile, Package.PackageFileExtension), IsDirty = false, PrecomputedProjectAssets = [] };
        package.AuthoredName ??= package.Meta.Name;
        package.Meta.Name = name;
        package.Meta.Version = version;
        var manifest = LoadProjectAssetsFromManifest(package, projectFile, manifestFile);
        if (manifest is not null)
            package.SetCompanionDeclarations(manifest, Path.GetDirectoryName(manifestFile)!);

        var container = new StandalonePackage(package) { IsDependencyPackage = true, IsDevRedirectPackage = true };
        container.AssetNamespace = PackageContainer.ResolveAssetNamespace(manifest?.AssetNamespace, package.AuthoredName ?? package.Meta.Name);
        if (manifest is not null)
        {
            var manifestDirectory = Path.GetDirectoryName(manifestFile)!;
            foreach (var assembly in manifest.HostAssemblies)
                container.Assemblies.Add(Path.GetFullPath(Path.Combine(manifestDirectory, assembly.ToOSPath())));
        }
        package.State = PackageState.DependenciesReady;
        Projects.Add(container);
        return container;
    }

    /// <summary>
    /// A dev-redirect package without an authored sdpkg joins the session only when its build manifest declares
    /// a companion package.
    /// </summary>
    private StandalonePackage? LoadDevRedirectDeclarations(Dependency dependency)
    {
        var directory = PackageStore.Instance.GetPackageDirectory(dependency.Name, new PackageVersionRange(dependency.Version), constraintProvider);
        if (directory is null)
            return null;
        var projectFile = Path.Combine(directory.ToOSPath(), dependency.Name + ".csproj");
        var manifestFile = File.Exists(projectFile) ? FindDevRedirectManifest(projectFile) : null;
        if (manifestFile is null)
            return null;

        AssetBuildManifest manifest;
        try
        {
            manifest = YamlSerializer.Load<AssetBuildManifest>(manifestFile);
        }
        catch
        {
            return null;
        }
        if (manifest.CompanionPackages.Count == 0)
            return null;

        var package = new Package
        {
            Meta = { Name = dependency.Name, Version = dependency.Version },
            FullPath = Path.ChangeExtension(projectFile, Package.PackageFileExtension),
            IsDirty = false,
            PrecomputedProjectAssets = [],
        };
        package.SetCompanionDeclarations(manifest, Path.GetDirectoryName(manifestFile)!);
        var container = new StandalonePackage(package) { IsDependencyPackage = true, IsDevRedirectPackage = true };
        container.Assemblies.AddRange(WithDevBuildAssemblies(dependency.Assemblies, manifest, manifestFile));
        package.State = PackageState.DependenciesReady;
        return container;
    }

    // The host's own framework (the editor runs net10.0-windows, the asset compiler net10.0), so a companion
    // package restores what this process can load
    private static string GetHostTargetFramework()
    {
        var version = Environment.Version.Major;
        return OperatingSystem.IsWindows() ? $"net{version}.0-windows7.0" : $"net{version}.0";
    }

    // The folder whose NuGet settings the restores use: the solution's, or for a session opened from a project (its
    // SolutionPath is empty, not null) the first project's
    private string? GetRootDirectory()
    {
        return HasSolution
            ? Path.GetDirectoryName(SolutionPath.ToOSPath())
            : Projects.OfType<SolutionProject>().Select(x => Path.GetDirectoryName(x.FullPath.ToOSPath())).FirstOrDefault();
    }
}
