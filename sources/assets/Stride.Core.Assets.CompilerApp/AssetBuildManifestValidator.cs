// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.CompilerApp
{
    /// <summary>
    /// Shadow validation: compares the .sdbuild manifest chain against what the legacy csproj walk loaded.
    /// Temporary until manifests become the only input of the asset compiler.
    /// </summary>
    public static class AssetBuildManifestValidator
    {
        private class Report
        {
            public List<string> Lines { get; } = new();
            public bool HasWarnings { get; set; }
        }

        public static void Validate(PackageSession session, string packageFile, string manifestFile, string buildDirectory, ILogger log)
        {
            try
            {
                manifestFile = !string.IsNullOrEmpty(manifestFile) ? Path.GetFullPath(manifestFile) : FindManifest(packageFile);
                if (manifestFile == null || !File.Exists(manifestFile))
                {
                    log.Info($"[BuildManifest] No manifest found for [{packageFile}], skipping validation");
                    return;
                }

                var manifests = LoadManifestChain(manifestFile, log);

                var report = new Report();
                CompareProjects(session, manifests, report, log);
                CompareAssemblies(session, manifests, report, log);
                CompareProjectAssets(session, manifests, report, log);
                ComparePackages(session, manifestFile, manifests, report, log);

                if (report.Lines.Count > 0 && !string.IsNullOrEmpty(buildDirectory))
                {
                    var reportFile = Path.Combine(buildDirectory, "BuildManifestValidation.log");
                    Directory.CreateDirectory(buildDirectory);
                    File.WriteAllLines(reportFile, report.Lines);
                    var message = $"[BuildManifest] Full report: {reportFile}";
                    if (report.HasWarnings)
                        log.Warning(message);
                    else
                        log.Info(message);
                }
            }
            catch (Exception e)
            {
                log.Warning($"[BuildManifest] Validation failed: {e.Message}");
            }
        }

        private static string FindManifest(string packageFile)
        {
            if (!string.Equals(Path.GetExtension(packageFile), ".csproj", StringComparison.OrdinalIgnoreCase))
                return null;
            var objDirectory = Path.Combine(Path.GetDirectoryName(packageFile), "obj");
            if (!Directory.Exists(objDirectory))
                return null;
            var manifestName = Path.GetFileNameWithoutExtension(packageFile) + AssetBuildManifest.FileExtension;
            return Directory.EnumerateFiles(objDirectory, manifestName, SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static Dictionary<string, AssetBuildManifest> LoadManifestChain(string rootManifestFile, ILogger log)
        {
            var manifests = new Dictionary<string, AssetBuildManifest>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(Path.GetFullPath(rootManifestFile));
            while (queue.Count > 0)
            {
                var file = queue.Dequeue();
                if (manifests.ContainsKey(file))
                    continue;
                if (!File.Exists(file))
                {
                    log.Warning($"[BuildManifest] Referenced manifest missing: [{file}]");
                    continue;
                }
                var manifest = YamlSerializer.Load<AssetBuildManifest>(file);
                manifests.Add(file, manifest);
                var directory = Path.GetDirectoryName(file);
                foreach (var reference in manifest.ReferencedManifests)
                    queue.Enqueue(Path.GetFullPath(Path.Combine(directory, reference)));
            }
            return manifests;
        }

        private static string ResolvePath(string manifestFile, UFile path)
        {
            return path != null ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestFile), path)) : null;
        }

        private static void CompareSets(string what, IEnumerable<string> legacy, IEnumerable<string> manifest, Report report, ILogger log, bool warnLegacyOnly = true, bool warnManifestOnly = true)
        {
            var legacySet = new HashSet<string>(legacy, StringComparer.OrdinalIgnoreCase);
            var manifestSet = new HashSet<string>(manifest, StringComparer.OrdinalIgnoreCase);
            var legacyOnly = legacySet.Except(manifestSet, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var manifestOnly = manifestSet.Except(legacySet, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            if (legacyOnly.Count == 0 && manifestOnly.Count == 0)
            {
                log.Info($"[BuildManifest] {what}: match ({legacySet.Count})");
                return;
            }
            var message = $"[BuildManifest] {what}: {legacyOnly.Count} legacy only, {manifestOnly.Count} manifest only";
            if ((legacyOnly.Count > 0 && warnLegacyOnly) || (manifestOnly.Count > 0 && warnManifestOnly))
            {
                report.HasWarnings = true;
                log.Warning(message);
            }
            else
            {
                log.Info(message);
            }
            foreach (var item in legacyOnly)
                report.Lines.Add($"{what}: legacy only: {item}");
            foreach (var item in manifestOnly)
                report.Lines.Add($"{what}: manifest only: {item}");
        }

        private static void CompareProjects(PackageSession session, Dictionary<string, AssetBuildManifest> manifests, Report report, ILogger log)
        {
            var legacy = session.Projects.OfType<SolutionProject>().Where(x => x.FullPath != null).Select(x => (string)x.FullPath.ToOSPath());
            var manifest = manifests.Where(x => x.Value.ProjectFile != null).Select(x => ResolvePath(x.Key, x.Value.ProjectFile));
            CompareSets("projects", legacy, manifest, report, log);
        }

        private static void CompareAssemblies(PackageSession session, Dictionary<string, AssetBuildManifest> manifests, Report report, ILogger log)
        {
            var legacy = session.Packages.SelectMany(x => x.LoadedAssemblies).Where(x => x.Path != null).Select(x => Path.GetFullPath(x.Path));
            var manifest = manifests.SelectMany(x => x.Value.AssetAssemblies.Select(assembly => ResolvePath(x.Key, assembly)));
            // Legacy-only = graph over-loading that manifest mode stops doing; only new loads are suspicious
            CompareSets("assemblies", legacy, manifest, report, log, warnLegacyOnly: false);
        }

        private static void CompareProjectAssets(PackageSession session, Dictionary<string, AssetBuildManifest> manifests, Report report, ILogger log)
        {
            // Only project-backed packages: standalone packages keep folder-based discovery from
            // their packed sdpkg, which manifests don't replace
            var legacy = session.Packages.Where(x => x.Container is SolutionProject).SelectMany(x => x.Assets)
                .Where(x => x.Asset is IProjectAsset && x.FullPath != null)
                .Select(x => (string)x.FullPath.ToOSPath()).ToList();
            var manifest = manifests.SelectMany(x => x.Value.ProjectAssets.Where(item => item.Path != null).Select(item => ResolvePath(x.Key, item.Path))).ToList();
            bool isSource(string path) => string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase);
            CompareSets("project assets", legacy.Where(x => !isSource(x)), manifest.Where(x => !isSource(x)), report, log);
            // .cs project assets don't produce bundle content; the evaluation-time vs build-time
            // item view differs for generated/injected sources, so report informationally only
            CompareSets("project assets (.cs)", legacy.Where(isSource), manifest.Where(isSource), report, log, warnLegacyOnly: false, warnManifestOnly: false);
        }

        private static void ComparePackages(PackageSession session, string rootManifestFile, Dictionary<string, AssetBuildManifest> manifests, Report report, ILogger log)
        {
            if (!manifests.TryGetValue(rootManifestFile, out var root))
                return;
            var assetsFile = ResolvePath(rootManifestFile, root.ProjectAssetsFile);
            if (assetsFile == null || !File.Exists(assetsFile))
            {
                log.Warning($"[BuildManifest] packages: lock file not found: [{assetsFile}]");
                return;
            }

            var lockFile = new LockFileFormat().Read(assetsFile);
            var framework = NuGetFramework.Parse(root.TargetFramework);
            var target = lockFile.Targets.FirstOrDefault(x => x.RuntimeIdentifier == null && x.TargetFramework == framework)
                ?? lockFile.Targets.FirstOrDefault(x => x.RuntimeIdentifier == null);
            if (target == null)
            {
                log.Warning($"[BuildManifest] packages: no target for [{root.TargetFramework}] in lock file");
                return;
            }

            var manifestPackages = new List<string>();
            foreach (var library in target.Libraries.Where(x => x.Type == "package"))
            {
                var lockFileLibrary = lockFile.GetLibrary(library.Name, library.Version);
                var sdpkgFile = $"stride/{library.Name}.sdpkg";
                if (lockFileLibrary != null && lockFileLibrary.Files.Any(x => string.Equals(x, sdpkgFile, StringComparison.OrdinalIgnoreCase)))
                    manifestPackages.Add($"{library.Name}/{library.Version}");
            }

            var legacy = session.Packages.Where(x => x.Container is StandalonePackage && x.Meta?.Name != null)
                .Select(x => $"{x.Meta.Name}/{x.Meta.Version}");
            CompareSets("packages", legacy, manifestPackages, report, log);
        }
    }
}

