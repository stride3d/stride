// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using Stride.Core.Assets.Analysis;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets;

partial class PackageSession
{
    /// <summary>
    /// Loads a session from a build manifest (.sdbuild) chain. Each manifest contributes its authored
    /// package (folders/metadata), the exact assemblies to load
    /// (<see cref="AssetBuildManifest.AssetAssemblies"/>) and its project assets.
    /// </summary>
    public static void LoadFromBuildManifest(string rootManifestFile, PackageSessionResult sessionResult, PackageLoadParameters? loadParameters = null)
    {
        ArgumentNullException.ThrowIfNull(rootManifestFile);
        ArgumentNullException.ThrowIfNull(sessionResult);

        loadParameters ??= PackageLoadParameters.Default();
        rootManifestFile = FileUtility.GetAbsolutePath(rootManifestFile);
        if (!File.Exists(rootManifestFile)) throw new ArgumentException($"Build manifest [{rootManifestFile}] must exist", nameof(rootManifestFile));

        try
        {
            AssetReferenceAnalysis.EnableCaching = true;
            sessionResult.Clear();
            sessionResult.Progress("Loading..", 0, 1);

            var session = new PackageSession();

            // Chase the manifest chain (referenced project manifests), keyed by absolute path
            var manifests = new Dictionary<string, AssetBuildManifest>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(rootManifestFile);
            while (queue.Count > 0)
            {
                var file = queue.Dequeue();
                if (manifests.ContainsKey(file))
                    continue;
                if (!File.Exists(file))
                {
                    // The referenced project wasn't built
                    sessionResult.Error($"Referenced build manifest [{file}] not found");
                    continue;
                }
                var manifest = YamlSerializer.Load<AssetBuildManifest>(file);
                manifests.Add(file, manifest);
                session.AssetNamespaceUsings.UnionWith(manifest.AssetNamespaceUsings);
                var directory = Path.GetDirectoryName(file)!;
                foreach (var reference in manifest.ReferencedManifests)
                    queue.Enqueue(Path.GetFullPath(Path.Combine(directory, reference)));
            }
            session.LoadedBuildManifests = [.. manifests.Keys];

            // Multiple manifests can share one authored sdpkg (e.g. a platform-head exe and its
            // game library both point at the game's sdpkg via StrideCurrentPackagePath). Dedup by
            // authored-package path and merge each project's assemblies/assets into the one package.
            // Root contributed first so it's first in the package list (PackageBuilder resolves it by
            // authored-package path, falling back to the first package).
            var rootManifest = manifests[rootManifestFile];
            var packagesByPath = new Dictionary<string, StandalonePackage>(StringComparer.OrdinalIgnoreCase);
            var containersByManifest = new Dictionary<string, StandalonePackage>(StringComparer.OrdinalIgnoreCase);
            var rootContainer = ContributeManifest(session, packagesByPath, rootManifestFile, rootManifest, sessionResult);
            containersByManifest.Add(rootManifestFile, rootContainer);
            foreach (var (file, manifest) in manifests)
            {
                if (string.Equals(file, rootManifestFile, StringComparison.OrdinalIgnoreCase))
                    continue;
                containersByManifest.Add(file, ContributeManifest(session, packagesByPath, file, manifest, sessionResult));
            }

            // NuGet package dependencies that ship a stride/<Id>.sdpkg (engine/plugin packages):
            // read each project's lock file directly — cheap JSON, no MSBuild. NuGet already flattened
            // each project's package closure, so a project sees exactly its own lock file's sdpkg packages.
            var sdpkgPackagesByName = new Dictionary<string, StandalonePackage>(StringComparer.OrdinalIgnoreCase);
            var packagesByLockFile = new Dictionary<string, List<StandalonePackage>>(StringComparer.OrdinalIgnoreCase);
            var packagesByManifest = new Dictionary<string, List<StandalonePackage>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (file, manifest) in manifests)
            {
                if (manifest.NuGetLockFile is null || manifest.TargetFramework is null)
                    continue;
                var lockFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, manifest.NuGetLockFile.ToOSPath()));
                if (!File.Exists(lockFile))
                    continue;
                if (!packagesByLockFile.TryGetValue(lockFile, out var packages))
                    packagesByLockFile[lockFile] = packages = session.LoadPackageDependenciesFromLockFile(lockFile, NuGetFramework.Parse(manifest.TargetFramework), sdpkgPackagesByName, sessionResult);
                packagesByManifest[file] = packages;
            }

            // Package-scoped asset lookups (Package.FindAsset) only search FlattenedDependencies, so
            // each project package gets its flattened closure: the projects it references (transitively,
            // from the manifest chain) plus its own lock file's sdpkg packages
            var manifestClosures = new Dictionary<string, HashSet<StandalonePackage>>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> ReferencedManifestFiles(string file) => manifests.TryGetValue(file, out var manifest)
                ? manifest.ReferencedManifests.Select(reference => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, reference)))
                : [];
            foreach (var group in containersByManifest.GroupBy(x => x.Value, x => x.Key))
            {
                var container = group.Key;
                var dependencies = new HashSet<StandalonePackage>();
                foreach (var file in group)
                {
                    dependencies.UnionWith(CollectReachable(file, manifestClosures, ReferencedManifestFiles, containersByManifest.GetValueOrDefault));
                    if (packagesByManifest.TryGetValue(file, out var packages))
                        dependencies.UnionWith(packages);
                }
                dependencies.Remove(container);
                foreach (var dependency in dependencies)
                    container.FlattenedDependencies.Add(new Dependency(dependency.Package));
            }

            // Nupkg packages need a closure too (e.g. a plugin's UI page references the engine's
            // default font by id): the packages of the lock files they came from
            var sdpkgClosures = new Dictionary<StandalonePackage, HashSet<StandalonePackage>>();
            foreach (var packages in packagesByLockFile.Values)
            {
                foreach (var package in packages)
                {
                    if (!sdpkgClosures.TryGetValue(package, out var closure))
                        sdpkgClosures[package] = closure = [];
                    closure.UnionWith(packages);
                }
            }
            foreach (var (package, closure) in sdpkgClosures)
            {
                closure.Remove(package);
                foreach (var dependency in closure)
                    package.FlattenedDependencies.Add(new Dependency(dependency.Package));
            }

            // Load + register exactly the declared assemblies, then load assets (folder scan +
            // precomputed project assets); no dependency resolution, no MSBuild
            session.LoadMissingAssets(sessionResult, [.. session.Packages], loadParameters);

            // Read-only (nupkg) packages need the bare-to-canonical reference restamp too (their
            // authored yaml stores same-prefix references bare); the session analysis below only
            // covers local packages
            var dependencyAnalysisParameters = new AssetAnalysisParameters { IsProcessingAssetReferences = true, IsLoggingAssetNotFoundAsError = false };
            foreach (var package in session.Packages)
            {
                if (package.IsReadOnly)
                    AssetAnalysis.Run(package.Assets, sessionResult, dependencyAnalysisParameters);
            }

            CheckAssetNamespaceDisjointness(session, sessionResult);

            // Fix relative references
            var analysis = new PackageSessionAnalysis(session, GetPackageAnalysisParametersForLoad());
            analysis.Run().CopyTo(sessionResult);
            foreach (var type in AssetRegistry.GetPackageSessionAnalysisTypes())
            {
                var pkgAnalysis = (PackageSessionAnalysisBase)Activator.CreateInstance(type)!;
                pkgAnalysis.Session = session;
                pkgAnalysis.Run().CopyTo(sessionResult);
            }

            sessionResult.Session = session;
            session.IsDirty = false;
        }
        finally
        {
            AssetReferenceAnalysis.EnableCaching = false;
        }
    }

    /// <summary>
    /// Packages sharing an asset namespace present one URL surface, so their rooted URLs must be
    /// disjoint — checked across ALL namespaced packages, independent of dependency visibility.
    /// </summary>
    internal static void CheckAssetNamespaceDisjointness(PackageSession session, ILogger log)
    {
        var packagesByLocation = new Dictionary<UFile, Package>();
        foreach (var package in session.Packages)
        {
            if (package.Container.AssetNamespace is null)
                continue;
            foreach (var asset in package.Assets)
            {
                if (packagesByLocation.TryGetValue(asset.Location, out var other))
                {
                    if (other != package)
                        log.Error($"Asset URL [{asset.Location}] exists in both [{other.Meta.Name}] and [{package.Meta.Name}], which share the same asset namespace; rename one of the assets.");
                }
                else
                {
                    packagesByLocation.Add(asset.Location, package);
                }
            }
        }
    }

    /// <summary>
    /// Merges one parsed <c>.sdbuild</c> manifest into the session: looks up or creates the package for its
    /// authored-package path, then folds in that manifest's identity, asset assemblies, and project assets.
    /// </summary>
    private static StandalonePackage ContributeManifest(PackageSession session, Dictionary<string, StandalonePackage> packagesByPath, string manifestFile, AssetBuildManifest manifest, ILogger log)
    {
        var manifestDirectory = Path.GetDirectoryName(manifestFile)!;
        string Resolve(UFile path) => Path.GetFullPath(Path.Combine(manifestDirectory, path.ToOSPath()));

        var packagePath = manifest.PackageFile is not null ? Resolve(manifest.PackageFile) : Path.ChangeExtension(Resolve(manifest.ProjectFile!), Package.PackageFileExtension);
        var projectDirectory = manifest.ProjectFile is not null ? new UDirectory(new UFile(Resolve(manifest.ProjectFile)).GetFullDirectory()) : null;

        if (!packagesByPath.TryGetValue(packagePath, out var container))
        {
            var package = File.Exists(packagePath)
                ? Package.LoadRaw(log, packagePath)
                : new Package
                {
                    Meta = { Name = Path.GetFileNameWithoutExtension(packagePath) },
                    AssetFolders = { new AssetFolder("Assets") },
                    ResourceFolders = { "Resources" },
                    FullPath = packagePath,
                    IsDirty = false,
                };
            package.PrecomputedProjectAssets = [];
            container = new StandalonePackage(package);
            container.Package.State = PackageState.DependenciesReady;
            packagesByPath.Add(packagePath, container);
            session.Projects.Add(container);
        }

        // Identity/namespace come from the project that owns the sdpkg (sits next to it)
        var isOwner = projectDirectory is not null && string.Equals(projectDirectory.ToOSPath().TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(packagePath), StringComparison.OrdinalIgnoreCase);
        if (isOwner || container.Package.RootNamespace is null)
            container.Package.RootNamespace = manifest.RootNamespace;
        // The session package name stays csproj-derived (it keys dependency matching); the authored
        // sdpkg name only serves as the namespace identity below.
        var authoredName = File.Exists(packagePath) ? container.Package.Meta.Name : null;
        container.Package.AuthoredName ??= authoredName;
        if ((isOwner || container.Package.Meta.Name is null) && !string.IsNullOrEmpty(manifest.PackageName))
            container.Package.Meta.Name = manifest.PackageName;
        if (container.Package.Meta.Version is null)
            container.Package.Meta.Version = !string.IsNullOrEmpty(manifest.PackageVersion) ? new PackageVersion(manifest.PackageVersion) : new PackageVersion("1.0.0");
        if (isOwner || container.AssetNamespace is null)
            container.AssetNamespace = PackageContainer.ResolveAssetNamespace(manifest.AssetNamespace, container.Package.AuthoredName ?? container.Package.Meta.Name);

        foreach (var assembly in manifest.AssetAssemblies)
            container.Assemblies.Add(Resolve(assembly));

        // Project assets resolved at build time
        if (projectDirectory is not null)
        {
            var precomputed = container.Package.PrecomputedProjectAssets!;
            var seen = new HashSet<string>(precomputed.Select(a => a.FilePath.FullPath), StringComparer.OrdinalIgnoreCase);
            foreach (var item in manifest.ProjectAssets)
            {
                if (item.Path is null)
                    continue;
                var filePath = new UFile(Resolve(item.Path));
                // The same package can be contributed by several manifests (e.g. a project's
                // target-TFM manifest and its host-TFM sibling both list the same assets); add
                // each source file once, else the asset gets renamed to "name (2)".
                if (!seen.Add(filePath.FullPath))
                    continue;
                var link = item.Link is not null ? UPath.Combine(projectDirectory, item.Link) : null;
                precomputed.Add(new PackageLoadingAssetFile(filePath, projectDirectory) { Link = link });
            }
        }

        return container;
    }

    /// <summary>
    /// Reads a NuGet lock file and pulls in the dependency packages that ship a Stride <c>.sdpkg</c>,
    /// each seeing the sdpkg packages reachable through the lock file's dependency graph.
    /// Returns the sdpkg packages this lock file contains (loaded here or by a previous call).
    /// </summary>
    private List<StandalonePackage> LoadPackageDependenciesFromLockFile(string lockFilePath, NuGetFramework framework, Dictionary<string, StandalonePackage> sdpkgPackagesByName, ILogger log)
    {
        var lockFile = new LockFileFormat().Read(lockFilePath);
        var target = lockFile.Targets.FirstOrDefault(t => t.RuntimeIdentifier == null && Equals(t.TargetFramework, framework))
            ?? lockFile.Targets.FirstOrDefault(t => t.RuntimeIdentifier == null);
        if (target is null)
            return [];

        var libraries = lockFile.Libraries.ToDictionary(l => (l.Name, l.Version));
        var created = new List<StandalonePackage>();

        foreach (var targetLibrary in target.Libraries)
        {
            if (targetLibrary.Type != "package")
                continue;
            if (!libraries.TryGetValue((targetLibrary.Name, targetLibrary.Version), out var library))
                continue;

            // Already loaded from a previous lock file
            if (sdpkgPackagesByName.ContainsKey(library.Name))
                continue;

            var libraryPath = lockFile.PackageFolders
                .Select(folder => Path.Combine(folder.Path, library.Path.Replace('/', Path.DirectorySeparatorChar)))
                .FirstOrDefault(Directory.Exists);
            if (libraryPath is null)
                continue;

            // Make every dependency assembly known to the container for later lazy resolution
            foreach (var runtimeAssembly in targetLibrary.RuntimeAssemblies.Concat(targetLibrary.RuntimeTargets))
            {
                if (runtimeAssembly.Path.EndsWith("_._", StringComparison.Ordinal) || runtimeAssembly.Path.Contains("/native/"))
                    continue;
                AssemblyContainer.RegisterDependency(Path.Combine(libraryPath, runtimeAssembly.Path.Replace('/', Path.DirectorySeparatorChar)));
            }

            // Only packages shipping a stride/<Id>.sdpkg participate in asset compilation
            var sdpkgRelative = $"stride/{library.Name}{Package.PackageFileExtension}";
            if (!library.Files.Any(f => string.Equals(f, sdpkgRelative, StringComparison.OrdinalIgnoreCase)))
                continue;
            var sdpkgPath = Path.Combine(libraryPath, "stride", library.Name + Package.PackageFileExtension);
            if (!File.Exists(sdpkgPath))
                continue;

            // Dedup against the project manifest chain
            if (Packages.Any(p => string.Equals(p.Meta.Name, library.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var package = Package.LoadRaw(log, sdpkgPath);
            package.Meta.Name = library.Name;
            package.Meta.Version = library.Version.ToPackageVersion();
            var container = new StandalonePackage(package) { IsDependencyPackage = true };
            // The packed sdpkg's declarations (host-loadable, narrowed to asset types) are the
            // complete list; a package declaring none gets no assembly loaded.
            var sdpkgDirectory = Path.GetDirectoryName(sdpkgPath)!;
            var hostAssetAssemblies = SelectHostAssetAssemblies(package.AssetAssemblies);
            container.Assemblies.AddRange(hostAssetAssemblies.Select(a => Path.GetFullPath(Path.Combine(sdpkgDirectory, a.Path!.ToOSPath()))));
            Projects.Add(container);
            package.State = PackageState.DependenciesReady;
            sdpkgPackagesByName.Add(library.Name, container);
            created.Add(container);
        }

        // Each new sdpkg package sees the sdpkg packages its lock-file dependency closure reaches
        // (crossing plain code packages)
        var targetLibrariesByName = target.Libraries.Where(l => l.Name is not null).ToDictionary(l => l.Name!, StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> LibraryDependencies(string name) => targetLibrariesByName.TryGetValue(name, out var library)
            ? library.Dependencies.Select(d => d.Id)
            : [];
        var reachableByName = new Dictionary<string, HashSet<StandalonePackage>>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in created)
        {
            foreach (var dependency in CollectReachable(container.Package.Meta.Name!, reachableByName, LibraryDependencies, sdpkgPackagesByName.GetValueOrDefault))
            {
                if (dependency != container)
                    container.FlattenedDependencies.Add(new Dependency(dependency.Package));
            }
        }

        // The sdpkg packages this lock file can see, whether loaded above or by a previous call
        var result = new List<StandalonePackage>();
        foreach (var targetLibrary in target.Libraries)
        {
            if (targetLibrary.Name is not null && sdpkgPackagesByName.TryGetValue(targetLibrary.Name, out var package))
                result.Add(package);
        }
        return result;
    }

    /// <summary>Memoized DFS over a string-keyed graph, collecting the packages its keys resolve to.</summary>
    private static HashSet<StandalonePackage> CollectReachable(string key, Dictionary<string, HashSet<StandalonePackage>> visited, Func<string, IEnumerable<string>> edges, Func<string, StandalonePackage?> resolve)
    {
        if (visited.TryGetValue(key, out var reachable))
            return reachable;
        visited[key] = reachable = [];
        foreach (var next in edges(key))
        {
            if (resolve(next) is { } package)
                reachable.Add(package);
            reachable.UnionWith(CollectReachable(next, visited, edges, resolve));
        }
        return reachable;
    }

    /// <summary>
    /// Picks, per declared asset assembly, the single build matching this asset compiler's runtime.
    /// </summary>
    /// <remarks>
    /// A package may declare its asset assembly built for several host TFMs (net10.0, net10.0-windows*).
    /// On Windows the -windows build is preferred, else the base (netX.0, no platform suffix); on other hosts
    /// only the base loads. Untagged legacy entries load unconditionally. Entries are grouped by file name so
    /// a package declaring multiple distinct assemblies resolves one host build per assembly.
    /// </remarks>
    private static List<AssetAssembly> SelectHostAssetAssemblies(List<AssetAssembly> declared)
    {
        var result = new List<AssetAssembly>();
        var isWindows = OperatingSystem.IsWindows();
        foreach (var group in declared.Where(a => a.Path is not null)
                     .GroupBy(a => Path.GetFileName(a.Path!.ToOSPath()), StringComparer.OrdinalIgnoreCase))
        {
            AssetAssembly? best = null;
            var bestRank = -1;
            foreach (var a in group)
            {
                var tfm = a.TargetFramework;
                int rank;
                if (tfm is null || !tfm.Contains('-')) rank = 1;            // base host (netX.0), loads anywhere
                else if (isWindows && tfm.Contains("-windows")) rank = 2;   // most-specific on Windows
                else continue;                                             // incompatible with this host
                if (rank > bestRank) { bestRank = rank; best = a; }
            }
            if (best is not null)
                result.Add(best);
        }
        return result;
    }
}
