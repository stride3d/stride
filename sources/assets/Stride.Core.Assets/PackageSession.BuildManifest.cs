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
    /// Loads a session from a build manifest (.sdbuild) chain instead of walking csproj files.
    /// Each manifest contributes its authored package (folders/metadata), the exact assemblies to
    /// load (<see cref="AssetBuildManifest.AssetAssemblies"/>) and its project assets — no MSBuild
    /// evaluation, no reference-graph loading.
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
                var directory = Path.GetDirectoryName(file)!;
                foreach (var reference in manifest.ReferencedManifests)
                    queue.Enqueue(Path.GetFullPath(Path.Combine(directory, reference)));
            }

            // Multiple manifests can share one authored sdpkg (e.g. a platform-head exe and its
            // game library both point at the game's sdpkg via StrideCurrentPackagePath). Dedup by
            // authored-package path and merge each project's assemblies/assets into the one package.
            // Root first so PackageSession.LocalPackages.FirstOrDefault() resolves to it.
            var rootManifest = manifests[rootManifestFile];
            var packagesByPath = new Dictionary<string, StandalonePackage>(StringComparer.OrdinalIgnoreCase);
            var rootContainer = ContributeManifest(session, packagesByPath, rootManifestFile, rootManifest, sessionResult);
            foreach (var (file, manifest) in manifests)
            {
                if (string.Equals(file, rootManifestFile, StringComparison.OrdinalIgnoreCase))
                    continue;
                ContributeManifest(session, packagesByPath, file, manifest, sessionResult);
            }

            // NuGet package dependencies that ship a stride/<Id>.sdpkg (engine/plugin packages):
            // read the root's lock file directly — cheap JSON, no MSBuild. Project references are
            // already covered by the manifest chain above.
            if (rootManifest.ProjectAssetsFile is not null)
            {
                var lockFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(rootManifestFile)!, rootManifest.ProjectAssetsFile.ToOSPath()));
                if (File.Exists(lockFile) && rootManifest.TargetFramework is not null)
                    session.LoadPackageDependenciesFromLockFile(lockFile, NuGetFramework.Parse(rootManifest.TargetFramework), sessionResult, loadParameters);
            }

            // The root depends on every other loaded package (project chain + sdpkg packages):
            // mirror the lock file's flattened closure so the compiler reaches their assets
            foreach (var container in session.Projects.OfType<StandalonePackage>())
            {
                if (container != rootContainer)
                    rootContainer.FlattenedDependencies.Add(new Dependency(container.Package));
            }

            // Load + register exactly the declared assemblies, then load assets (folder scan +
            // precomputed project assets); no dependency resolution, no MSBuild
            session.LoadMissingAssets(sessionResult, [.. session.Packages], loadParameters);

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

    private static StandalonePackage ContributeManifest(PackageSession session, Dictionary<string, StandalonePackage> packagesByPath, string manifestFile, AssetBuildManifest manifest, ILogger log)
    {
        var manifestDirectory = Path.GetDirectoryName(manifestFile)!;
        string Resolve(UFile path) => Path.GetFullPath(Path.Combine(manifestDirectory, path.ToOSPath()));

        var packagePath = manifest.Package is not null ? Resolve(manifest.Package) : Path.ChangeExtension(Resolve(manifest.ProjectFile!), Package.PackageFileExtension);
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
        if ((isOwner || container.Package.Meta.Name is null) && !string.IsNullOrEmpty(manifest.PackageName))
            container.Package.Meta.Name = manifest.PackageName;
        if (container.Package.Meta.Version is null)
            container.Package.Meta.Version = !string.IsNullOrEmpty(manifest.PackageVersion) ? new PackageVersion(manifest.PackageVersion) : new PackageVersion("1.0.0");

        foreach (var assembly in manifest.AssetAssemblies)
            container.Assemblies.Add(Resolve(assembly));

        // Project assets resolved at build time (the manifest replaces FindAssetsInProject)
        if (projectDirectory is not null)
        {
            foreach (var item in manifest.ProjectAssets)
            {
                if (item.Path is null)
                    continue;
                var link = item.Link is not null ? UPath.Combine(projectDirectory, item.Link) : null;
                container.Package.PrecomputedProjectAssets!.Add(new PackageLoadingAssetFile(new UFile(Resolve(item.Path)), projectDirectory) { Link = link });
            }
        }

        return container;
    }

    private void LoadPackageDependenciesFromLockFile(string lockFilePath, NuGetFramework framework, ILogger log, PackageLoadParameters loadParameters)
    {
        var lockFile = new LockFileFormat().Read(lockFilePath);
        var target = lockFile.Targets.FirstOrDefault(t => t.RuntimeIdentifier == null && Equals(t.TargetFramework, framework))
            ?? lockFile.Targets.FirstOrDefault(t => t.RuntimeIdentifier == null);
        if (target is null)
            return;

        var libraries = lockFile.Libraries.ToDictionary(l => (l.Name, l.Version));

        foreach (var targetLibrary in target.Libraries)
        {
            if (targetLibrary.Type != "package")
                continue;
            if (!libraries.TryGetValue((targetLibrary.Name, targetLibrary.Version), out var library))
                continue;

            var libraryPath = lockFile.PackageFolders
                .Select(folder => Path.Combine(folder.Path, library.Path.Replace('/', Path.DirectorySeparatorChar)))
                .FirstOrDefault(Directory.Exists);
            if (libraryPath is null)
                continue;

            // Make every dependency assembly known to the container for later lazy resolution
            var assemblies = new List<string>();
            foreach (var runtimeAssembly in targetLibrary.RuntimeAssemblies.Concat(targetLibrary.RuntimeTargets))
            {
                if (runtimeAssembly.Path.EndsWith("_._", StringComparison.Ordinal) || runtimeAssembly.Path.Contains("/native/"))
                    continue;
                var assemblyFile = Path.Combine(libraryPath, runtimeAssembly.Path.Replace('/', Path.DirectorySeparatorChar));
                assemblies.Add(assemblyFile);
                AssemblyContainer.RegisterDependency(assemblyFile);
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
            var container = new StandalonePackage(package);
            container.Assemblies.AddRange(assemblies);
            Projects.Add(container);
            package.State = PackageState.DependenciesReady;
        }
    }
}
