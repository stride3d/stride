// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Packages;

namespace Stride.Assets.Templates;

/// <summary>
/// Picks the installed copy of a content template package (Samples, Starters, AssetPacks) for an engine. Shared by
/// source between the Game Studio bridge and the stride CLI, so both hosts resolve the same package for the same
/// engine.
/// </summary>
/// <remarks>
/// The content packages are versioned independently of the engine: an engine names the exact content version it was
/// released with (<c>StrideSamplesVersion</c> in sources/templates/StrideSamplesVersion.props, recorded as
/// Stride.GameStudio's pinned dependency), and that is what resolves. The one exception is a developer's own
/// checkout: with StridePackContentTemplates, a build with the checkout's <c>-devN</c> suffix packs the content as
/// exactly that version plus the suffix
/// (<c>4.4.0-beta7-dev4</c>), and that pack wins over the published one (it is what the samples being edited look
/// like; the build removes it when the flag is turned off). Only a dev engine looks for it, and only with its own
/// suffix, and then does not copy a local build of the published version (see <see cref="LocalBuildRange"/>).
/// </remarks>
public static class ContentTemplateResolver
{
    public const string SamplesPackageId = "Stride.Templates.Samples";
    public const string StartersPackageId = "Stride.Templates.Games.Starters";
    public const string AssetPacksPackageId = "Stride.Templates.AssetPacks";

    /// <summary>The content-versioned template packages, all released together at one content version.</summary>
    public static readonly IReadOnlyList<string> PackageIds = [SamplesPackageId, StartersPackageId, AssetPacksPackageId];

    /// <summary>
    /// The package whose pinned dependency on <see cref="SamplesPackageId"/> in Stride.GameStudio names the content
    /// version of an installed engine (the CLI reads it, since it targets engines other than its own build).
    /// </summary>
    public const string ContentVersionDependencyId = SamplesPackageId;

    // The checkout suffix ends the prerelease label: "dev4", or "beta8-dev4" for a dev build of a beta engine.
    private static readonly Regex DevSuffix = new("(?:^|-)(dev[0-9]*)$", RegexOptions.CultureInvariant);

    public static bool IsContentPackage(string packageId)
        => PackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The checkout suffix (<c>dev</c>, <c>dev3</c>, ...) of a development engine build (<c>4.4.0-dev3</c>,
    /// <c>4.4.0-beta8-dev3</c>), null for a release or prerelease engine. Only a dev host may use a dev content pack.
    /// </summary>
    public static string? DevSuffixOf(PackageVersion hostVersion)
        => DevSuffix.Match(hostVersion.SpecialVersion ?? string.Empty) is { Success: true } match ? match.Groups[1].Value : null;

    /// <summary>
    /// The version of this checkout's own pack of <paramref name="contentVersion"/> (<c>4.4.0-beta7-dev4</c>), null
    /// when <paramref name="hostVersion"/> is not a dev build.
    /// </summary>
    public static PackageVersion? DevPackVersion(PackageVersion contentVersion, PackageVersion hostVersion)
        => DevSuffixOf(hostVersion) is { } devSuffix ? new PackageVersion($"{contentVersion}-{devSuffix}") : null;

    /// <summary>
    /// The content version an installed engine names through its pinned dependency <paramref name="pinned"/>. A dev
    /// engine built with StridePackContentTemplates pins its own dev pack (<c>4.4.0-beta7-dev4</c>); its content
    /// version is that without the engine's suffix, as the running engine sees it.
    /// </summary>
    public static PackageVersion ContentVersionFromPin(PackageVersion pinned, PackageVersion hostVersion)
    {
        var devSuffix = DevSuffixOf(hostVersion);
        var text = pinned.ToString();
        return devSuffix is not null && text.EndsWith($"-{devSuffix}", StringComparison.OrdinalIgnoreCase)
            ? new PackageVersion(text[..^(devSuffix.Length + 1)])
            : pinned;
    }

    /// <summary>
    /// Whether an installed <paramref name="candidate"/> may serve as content version <paramref name="contentVersion"/>
    /// for the engine <paramref name="hostVersion"/>: exactly that version, or exactly this checkout's pack of it.
    /// </summary>
    public static bool IsAcceptable(PackageVersion candidate, PackageVersion contentVersion, PackageVersion hostVersion)
        => candidate.Equals(contentVersion) || candidate.Equals(DevPackVersion(contentVersion, hostVersion));

    /// <summary>
    /// The package among <paramref name="installed"/> to use: this checkout's pack when there is one, else the exact
    /// content version. Null when neither is installed.
    /// </summary>
    public static NugetLocalPackage? Pick(IEnumerable<NugetLocalPackage> installed, PackageVersion contentVersion, PackageVersion hostVersion)
    {
        var devPack = DevPackVersion(contentVersion, hostVersion);
        var candidates = installed.ToList();
        return candidates.FirstOrDefault(package => package.Version.Equals(devPack))
            ?? candidates.FirstOrDefault(package => package.Version.Equals(contentVersion));
    }

    /// <summary>
    /// The one version a host takes from local package sources (a checkout's <c>bin/packages</c>, the nugetdev feed),
    /// as an exact range. A dev host takes only this checkout's pack: a local file carrying the published version
    /// (same number, maybe different content) is not taken over the published one. A host without a checkout suffix
    /// (a CI build, or a checkout building the clean version) packs the content under the plain version, and takes
    /// that local build.
    /// </summary>
    public static PackageVersionRange LocalBuildRange(PackageVersion contentVersion, PackageVersion hostVersion)
    {
        var version = DevPackVersion(contentVersion, hostVersion) ?? contentVersion;
        return new PackageVersionRange(version, true, version, true);
    }

    /// <summary>
    /// Resolves <paramref name="packageId"/> for the engine: picks among the installed copies, and when none is
    /// acceptable installs the exact content version from the configured sources (once; it then stays in the
    /// store). Returns null when it is neither installed nor obtainable (offline, or not published yet).
    /// </summary>
    /// <param name="installed">
    /// Lists the installed copies of a package id, first taking the given range from local package sources
    /// (<see cref="LocalBuildRange"/>).
    /// </param>
    /// <param name="install">Installs exactly a version of a package from the sources; null when it could not be obtained.</param>
    /// <param name="log">Receives one line saying which copy won, or why none did.</param>
    public static async Task<NugetLocalPackage?> ResolveAsync(
        string packageId, PackageVersion contentVersion, PackageVersion hostVersion,
        Func<string, PackageVersionRange, IEnumerable<NugetLocalPackage>> installed,
        Func<string, PackageVersion, Task<NugetLocalPackage?>> install,
        Action<string>? log = null)
    {
        var localBuildRange = LocalBuildRange(contentVersion, hostVersion);
        var picked = Pick(installed(packageId, localBuildRange), contentVersion, hostVersion);
        if (picked is not null)
        {
            log?.Invoke(picked.Version.Equals(contentVersion)
                ? $"{packageId}: using the content version {contentVersion} this engine names."
                : $"{packageId}: using this checkout's pack {picked.Version} over the content version {contentVersion} this engine names.");
            return picked;
        }

        log?.Invoke($"{packageId} {contentVersion} is not installed; installing it from the package sources.");
        NugetLocalPackage? fetched;
        try
        {
            fetched = await install(packageId, contentVersion).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            log?.Invoke($"{packageId} {contentVersion} could not be installed: {e.Message}");
            return null;
        }

        // Re-pick rather than trust the returned object: only the exact version may be used, whatever the install
        // reports.
        picked = fetched is null ? null : Pick(installed(packageId, localBuildRange), contentVersion, hostVersion);
        log?.Invoke(picked is null
            ? $"{packageId} {contentVersion} could not be installed (not published, or no package source reachable)."
            : $"{packageId}: installed the content version {contentVersion} this engine names.");
        return picked;
    }
}
