// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.TemplateGenerator;

namespace Stride.Assets.Presentation.Templates;

/// <summary>
/// Boots the <see cref="DotNetNewTemplateRegistry"/>, installs each known Stride template
/// nupkg into it, and surfaces each contained dotnet new template as a
/// <see cref="TemplateDotNetNewDescription"/> registered with <see cref="TemplateManager"/>.
/// </summary>
/// <remarks>
/// Singleton: the registry is process-scoped so the bootstrapper's loaded components survive
/// across multiple New-Project invocations. The host settings dir lives under
/// <c>%LocalAppData%\Stride\TemplateEngine\&lt;engineVersion&gt;</c> so we don't share state
/// with the user's global <c>dotnet new</c> installation, and so side-by-side Stride versions
/// keep their own template caches.
/// </remarks>
internal static class DotNetNewTemplateBridge
{
    /// <summary>
    /// Package IDs the bridge tries to resolve via <see cref="PackageStore"/> on startup.
    /// Only <c>Stride.Templates.Games</c> (NewGame) ships in the GameStudio installer; the
    /// other two are dev-only here (present in <c>%LocalAppData%\Stride\NugetDev</c> when the
    /// solution has been built, absent in installer-only setups). End users reach Starters /
    /// Samples via the editor's template store (future) or CLI <c>dotnet new install</c>; this
    /// list just controls which packages the bridge proactively probes on startup. Missing
    /// packages are tolerated (per-package warning, no error).
    /// </summary>
    private static readonly string[] BundledTemplatePackageIds =
    {
        "Stride.Templates.Games",
        "Stride.Templates.Games.Starters",
        "Stride.Templates.Samples",
    };

    private static DotNetNewTemplateRegistry? registry;
    private static readonly object InitLock = new();

    /// <summary>
    /// In-process singleton; null until <see cref="RegisterProjectTemplates"/> has run.
    /// Consumed by <c>DotNetNewTemplateGenerator</c> at instantiation time.
    /// </summary>
    public static DotNetNewTemplateRegistry? Registry => registry;

    /// <summary>
    /// Resolves each <see cref="BundledTemplatePackageIds"/> entry via <see cref="PackageStore"/>,
    /// installs them into the shared registry, and wraps every loaded dotnet new template as a
    /// <see cref="TemplateDotNetNewDescription"/> registered with <see cref="TemplateManager"/>.
    /// Tolerates missing packages — dev workflows that haven't built every template project yet
    /// still load the editor.
    /// </summary>
    public static void RegisterProjectTemplates()
    {
        var logger = GlobalLogger.GetLogger("DotNetNewTemplateBridge");
        logger.ActivateLog(LogMessageType.Info);
        lock (InitLock)
        {
            if (registry == null)
            {
                // Bootstrapper's settings tree lands at {profileDir}/.templateengine/...
                // The custom IEnvironment in the registry makes {profileDir} the effective
                // USERPROFILE for path derivation; we point it at a Stride-owned dir to keep
                // state out of the user's global ~/.templateengine (which dotnet new CLI also
                // uses). NuGet's package cache at ~/.nuget/packages/ stays shared.
                // Versioned subfolder so side-by-side Stride installs don't share template state
                // (4.4 and 4.5 GameStudios coexist with their respective template versions).
                var profileDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Stride", "TemplateEngine", StrideVersion.NuGetVersion);
                registry = new DotNetNewTemplateRegistry(StrideVersion.NuGetVersion, profileDir);
                logger.Info($"Bootstrapper created (profileDir={profileDir}, hostVersion={StrideVersion.NuGetVersion})");
            }

            // Aggregated metadata across all installed Stride template packages. Each package
            // ships its own templates.sdtpls; we merge them so a single template-identity lookup
            // can dispatch to the right per-package metadata block at registration time. The
            // entry also carries the package's installSource so Icon/Screenshot relative paths
            // can be resolved back to absolute on-disk locations at registration time.
            var sdtplsByIdentity = new Dictionary<string, TemplateMetadataSource>();
            foreach (var packageId in BundledTemplatePackageIds)
                InstallBundledPackage(packageId, sdtplsByIdentity, logger);

            var templatesTask = registry.GetTemplatesAsync();
            templatesTask.Wait();
            var templates = templatesTask.Result;
            logger.Info($"Loaded {templates.Count} dotnet new template(s) total");

            // Synthetic package: holds only the TemplateDescriptions we want TemplateManager to
            // surface. Never saved to disk, never has a real .sdpkg — the descriptions are the
            // only thing FindTemplates() reads (it does `packages.SelectMany(p => p.Templates)`).
            // FullPath needs to be non-null so the DistinctPackagePathComparer (used to de-dup
            // ExtraPackages in FindTemplates) doesn't NRE in GetHashCode; using a sentinel path
            // ensures it doesn't collide with any real package.
            var synthetic = new Package { FullPath = new UFile("Stride.DotNetNewTemplates.synthetic") };
            foreach (var template in templates)
            {
                // Cross-ref by template.json identity → matches the sdtpl Id (we set
                // `identity = sdtpl.Id` in the preprocessor) so the dict lookup hits.
                sdtplsByIdentity.TryGetValue(template.Identity, out var source);
                var sdtpl = source?.Metadata;
                var shortName = template.ShortNameList.FirstOrDefault();
                // Per-template content dir inside the package, e.g. <packageDir>/content/stride-fps/.
                // Icon/Screenshot relative paths in .sdtpl resolve against this dir at runtime.
                var templateContentDir = source != null && shortName != null
                    ? Path.Combine(source.InstallSource, "content", shortName)
                    : null;
                var description = new TemplateDotNetNewDescription
                {
                    Id = sdtpl?.Id ?? TryParseGuid(template.Identity),
                    Name = sdtpl?.Name ?? template.Name ?? shortName ?? template.Identity,
                    Description = sdtpl?.Description ?? template.Description,
                    FullDescription = sdtpl?.FullDescription,
                    DefaultOutputName = sdtpl?.DefaultOutputName ?? template.DefaultName ?? "MyGame",
                    Group = sdtpl?.Group ?? template.GroupIdentity ?? "Stride",
                    Scope = TemplateScope.Session,
                    TemplateIdentity = template.Identity,
                    TemplateShortName = shortName ?? string.Empty,
                    // FullPath must be non-null: TemplateDescriptionViewModel calls
                    // Template.FullPath.GetFullDirectory() unconditionally when constructing
                    // image paths. We point it at a synthetic file inside the per-template
                    // content dir so the directory part is the natural relative-resolution root
                    // (matches what TemplateDescription.FullPath means for real .sdpkg-backed
                    // templates). Falls back to a per-identity sentinel if we couldn't resolve
                    // a content dir (e.g. template not in the aggregated metadata).
                    FullPath = templateContentDir != null
                        ? new UFile(Path.Combine(templateContentDir, ".synthetic.sdtpl"))
                        : new UFile($"{template.Identity}.synthetic"),
                    Icon = templateContentDir != null ? ResolveTemplateAsset(sdtpl?.Icon, templateContentDir) : null,
                };
                if (templateContentDir != null && sdtpl != null)
                {
                    foreach (var s in sdtpl.Screenshots)
                    {
                        var resolved = ResolveTemplateAsset(s, templateContentDir);
                        if (resolved != null)
                            description.Screenshots.Add(resolved);
                    }
                }
                synthetic.Templates.Add(description);
            }

            TemplateManager.RegisterPackage(synthetic);
        }
    }

    /// <summary>
    /// Resolves <paramref name="packageId"/> via <see cref="PackageStore"/>, installs it into the
    /// registry (skipping if unchanged since last session), and merges its <c>templates.sdtpls</c>
    /// entries into <paramref name="sdtplsByIdentity"/>. Tolerates a missing package by logging.
    /// </summary>
    private static void InstallBundledPackage(string packageId, Dictionary<string, TemplateMetadataSource> sdtplsByIdentity, Logger logger)
    {
        var packageDir = PackageStore.Instance.GetPackageDirectory(
            packageId,
            new PackageVersionRange(new PackageVersion(StrideVersion.NuGetVersion)));
        if (packageDir is null)
        {
            // Not installed yet (e.g. fresh checkout before first build of that templates
            // project). Not fatal — the editor still opens; this package's templates just
            // won't appear in the New-Project dialog until built at least once.
            logger.Warning($"{packageId} {StrideVersion.NuGetVersion} not found via PackageStore; its templates will be unavailable.");
            return;
        }
        // Install from the extracted package directory rather than the .nupkg file inside it.
        // Reasoning: pointing the bootstrapper at the .nupkg path inside NuGet's global cache
        // triggers the NuGet installer's "download" code path which then tries to "fetch" the
        // package from the same local file — and fails with "Failed to download X from X".
        // The Folder installer (selected automatically when CanInstallAsync sees a directory)
        // reads the extracted content directly, no download dance.
        var installSource = packageDir.ToOSPath();
        logger.Info($"{packageId} resolved to {installSource}");

        // Skip the install when the bootstrapper's recorded LastChangeTime is at or after the
        // .nupkg's on-disk mtime — same content as last session, no need to spend a few hundred
        // ms re-indexing. Dev rebuilds bump the .nupkg mtime and re-trigger.
        var nupkgFile = Directory.EnumerateFiles(installSource, "*.nupkg", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var nupkgMtime = nupkgFile != null ? File.GetLastWriteTimeUtc(nupkgFile) : DateTime.MinValue;
        var lastChangeTask = registry!.GetLastChangeTimeAsync(installSource);
        lastChangeTask.Wait();
        var lastInstalled = lastChangeTask.Result;
        if (lastInstalled.HasValue && lastInstalled.Value >= nupkgMtime)
        {
            logger.Info($"{packageId} unchanged since {lastInstalled.Value:O}; skipping reinstall.");
        }
        else
        {
            logger.Info($"Installing {packageId} from {installSource}");
            var installTask = registry.InstallPackageAsync(installSource);
            installTask.Wait();
            var (success, diagnostics) = installTask.Result;
            if (!success)
            {
                foreach (var d in diagnostics)
                    logger.Error($"Template install ({packageId}): {d}");
                return;
            }
        }

        // Merge this package's aggregated metadata into the shared dict. Each entry remembers
        // the installSource it came from so Icon/Screenshot relative paths can be resolved later.
        foreach (var (id, meta) in LoadAggregatedSdtpls(installSource, logger))
            sdtplsByIdentity[id] = new TemplateMetadataSource(meta, installSource);
    }

    /// <summary>
    /// Loads the aggregated <c>templates.sdtpls</c> shipped at the package root. Returns an
    /// empty dict if the file isn't present (older packages, dev mode before aggregator runs);
    /// callers fall back to <see cref="ITemplateInfo"/> properties in that case.
    /// </summary>
    private static Dictionary<string, SdtplMetadata> LoadAggregatedSdtpls(string installSource, Logger logger)
    {
        var aggregatedPath = Path.Combine(installSource, "content", "templates.sdtpls");
        if (!File.Exists(aggregatedPath))
        {
            logger.Warning($"Aggregated metadata not found at {aggregatedPath}; falling back to ITemplateInfo properties for display.");
            return new Dictionary<string, SdtplMetadata>();
        }
        var entries = SdtplMetadata.ParseAll(aggregatedPath);
        // Key by Id-as-string, because that's what the preprocessor stamps into template.json's
        // identity field — and identity is what dotnet new's ITemplateInfo exposes back to us.
        return entries
            .Where(e => e.Id.HasValue)
            .ToDictionary(e => e.Id!.Value.ToString(), e => e);
    }

    /// <summary>
    /// Resolves an Icon/Screenshot path declared in a <c>.sdtpl</c> file (relative to the sample
    /// dir) to an absolute on-disk path inside the installed package content. Tries the
    /// as-declared relative path first; falls back to <c>.sdtpl/&lt;basename&gt;</c> inside the
    /// per-template dir, which is where the preprocessor stashes icons whose canonical location
    /// was outside the sample dir (e.g. the shared <c>samples/Templates/.sdtpl/Icon2*.png</c>
    /// used by the genre starters). Returns null if neither candidate exists.
    /// </summary>
    private static UFile? ResolveTemplateAsset(string? relPath, string templateContentDir)
    {
        if (string.IsNullOrEmpty(relPath))
            return null;
        var primary = Path.GetFullPath(Path.Combine(templateContentDir, relPath));
        if (File.Exists(primary))
            return new UFile(primary);
        var fallback = Path.Combine(templateContentDir, ".sdtpl", Path.GetFileName(relPath));
        if (File.Exists(fallback))
            return new UFile(fallback);
        return null;
    }

    /// <summary>
    /// Pairs a parsed <see cref="SdtplMetadata"/> with the <c>installSource</c> of the package
    /// it came from, so Icon/Screenshot paths inside the metadata can be resolved against the
    /// right per-template content dir at registration time.
    /// </summary>
    private sealed record TemplateMetadataSource(SdtplMetadata Metadata, string InstallSource);

    private static Guid TryParseGuid(string s) => Guid.TryParse(s, out var g) ? g : new Guid(GetHash16(s));

    /// <summary>
    /// Derives a stable 16-byte seed from a template identity string so the same identity always
    /// produces the same <see cref="TemplateDescription.Id"/>. We don't need cryptographic
    /// uniqueness — collisions between distinct identities are astronomically unlikely under
    /// SHA1/MD5 truncation, and a deterministic Guid lets us key persistent UI state (e.g. last-
    /// selected template) by identity without bumping it across editor sessions.
    /// </summary>
    private static byte[] GetHash16(string s)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        return md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
    }
}
