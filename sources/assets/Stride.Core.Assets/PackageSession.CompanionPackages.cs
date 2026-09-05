// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.Frameworks;
using NuGet.ProjectModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Yaml;

namespace Stride.Core.Assets;

partial class PackageSession
{
    /// <summary>
    /// Loads the Assets companion packages declared by session packages (<see cref="Package.CompanionPackages"/>),
    /// each at the version its declaring package was packed with: already in the session, else a dev-redirect from
    /// the local store, else a restore from the NuGet sources configured for <paramref name="rootDirectory"/>.
    /// A loaded companion's own companions are loaded in turn. A companion that another declaration replaces
    /// (<see cref="CompanionPackage.Replaces"/>) is not loaded.
    /// </summary>
    internal void LoadCompanionPackages(string? rootDirectory, ILogger log)
    {
        var loadedByName = new Dictionary<string, StandalonePackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in Projects.OfType<StandalonePackage>())
        {
            if (container.Package.Meta.Name is { } name)
                loadedByName.TryAdd(name, container);
        }

        // Every replacement counts, whichever package declares it
        var replaced = new HashSet<string>(Packages.SelectMany(p => p.CompanionPackages).SelectMany(c => c.Replaces), StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<Package>();
        while (true)
        {
            var package = Packages.FirstOrDefault(p => !visited.Contains(p));
            if (package is null)
                break;
            visited.Add(package);
            foreach (var companion in package.CompanionPackages.ToList())
            {
                foreach (var name in companion.Replaces)
                    replaced.Add(name);
                if (companion.Name is not null && replaced.Contains(companion.Name))
                    continue;
                if (companion.Kind == PackageKind.Assets)
                    LoadCompanionPackage(package, companion, loadedByName, rootDirectory, log);
            }
        }
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
        if (companion is null)
        {
            var loaded = LoadCompanionPackage(companionName, version, loadedByName, rootDirectory, log);
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

        // A packed companion states its kind; an authored sdpkg does not (a solution project answers Runtime)
        if (companion.Package.Kind != PackageKind.Runtime && companion.Package.Kind != kind)
            log.Error($"[{companionName}] is an {companion.Package.Kind} package but [{package.Meta.Name}] declares it as its {kind} package.");

        // Visible wherever the declaring package is
        foreach (var container in Projects)
        {
            if (container == companion || container.FlattenedDependencies.Any(d => d.Package == companion.Package))
                continue;
            if (container.Package == package || container.FlattenedDependencies.Any(d => d.Package == package))
                container.FlattenedDependencies.Add(new Dependency(companion.Package));
        }
    }

    private StandalonePackage? LoadCompanionPackage(string name, PackageVersion version, Dictionary<string, StandalonePackage> loadedByName, string? rootDirectory, ILogger log)
    {
        if (TryLoadDevRedirectPackage(name, version, log) is { } devRedirect)
            return devRedirect;

        // Restore into the local store (a no-op once installed)
        var hostFramework = GetHostTargetFramework();
        LockFile lockFile;
        try
        {
            var outputPath = rootDirectory is not null ? Path.Combine(rootDirectory, "obj", "stride", "companions", name) : Path.Combine(Path.GetTempPath(), "stride-companions", name);
            lockFile = PackageStore.Instance.RestorePackage(name, version, hostFramework, rootDirectory, outputPath).Result;
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
        LoadPackageDependenciesFromLockFile(lockFile, NuGetFramework.Parse(hostFramework), loadedByName, log);
        return loadedByName.GetValueOrDefault(name);
    }

    // Dev-redirect stub in the local store: the package loads from its source tree through its build manifest,
    // with or without an authored sdpkg
    private StandalonePackage? TryLoadDevRedirectPackage(string name, PackageVersion version, ILogger log)
    {
        var directory = FindStorePackageDirectory(name, version, out var packageFile);
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

        var container = new StandalonePackage(package) { IsDependencyPackage = true };
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
        var container = new StandalonePackage(package) { IsDependencyPackage = true };
        container.Assemblies.AddRange(dependency.Assemblies);
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

    private string? GetRootDirectory()
    {
        return SolutionPath is not null
            ? Path.GetDirectoryName(SolutionPath.ToOSPath())
            : Projects.OfType<SolutionProject>().Select(x => Path.GetDirectoryName(x.FullPath.ToOSPath())).FirstOrDefault();
    }
}
