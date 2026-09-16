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
/// released with (<c>StrideVersion.SamplesVersion</c>, recorded as Stride.GameStudio's pinned dependency), and
/// that is what resolves, wherever it is installed from. The one exception is a developer's own checkout: a build
/// with the checkout's <c>-devN</c> suffix packs the content under that suffix, and such a pack numbered at or above
/// the named version wins over the published one (it is what the samples being edited look like). A pack from
/// another checkout (a different suffix) or a <c>-beta</c> engine never picks a dev pack.
/// </remarks>
public static class ContentTemplateResolver
{
    public const string SamplesPackageId = "Stride.Templates.Samples";
    public const string StartersPackageId = "Stride.Templates.Games.Starters";
    public const string AssetPacksPackageId = "Stride.Templates.AssetPacks";

    /// <summary>The content-versioned template packages, all cut and published together at one content version.</summary>
    public static readonly IReadOnlyList<string> PackageIds = [SamplesPackageId, StartersPackageId, AssetPacksPackageId];

    /// <summary>
    /// The package whose pinned dependency on <see cref="SamplesPackageId"/> in Stride.GameStudio names the content
    /// version of an installed engine (the CLI reads it, since it targets engines other than its own build).
    /// </summary>
    public const string ContentVersionDependencyId = SamplesPackageId;

    private static readonly Regex DevSuffix = new("^dev[0-9]*$", RegexOptions.CultureInvariant);

    public static bool IsContentPackage(string packageId)
        => PackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The checkout suffix (<c>dev</c>, <c>dev3</c>, ...) of a development engine build, null for a release or
    /// prerelease engine. Only a dev host may prefer a dev content pack.
    /// </summary>
    public static string? DevSuffixOf(PackageVersion hostVersion)
        => hostVersion.SpecialVersion is { Length: > 0 } special && DevSuffix.IsMatch(special) ? special : null;

    /// <summary>
    /// Whether an installed <paramref name="candidate"/> may serve as content version <paramref name="contentVersion"/>
    /// for the engine <paramref name="hostVersion"/>: the exact version, or this checkout's own dev pack numbered at
    /// or above it.
    /// </summary>
    public static bool IsAcceptable(PackageVersion candidate, PackageVersion contentVersion, PackageVersion hostVersion)
    {
        if (candidate.Equals(contentVersion))
            return true;
        var devSuffix = DevSuffixOf(hostVersion);
        return devSuffix is not null
            && string.Equals(candidate.SpecialVersion, devSuffix, StringComparison.OrdinalIgnoreCase)
            && candidate.Version >= contentVersion.Version;
    }

    /// <summary>
    /// The best acceptable package among <paramref name="installed"/>: this checkout's highest-numbered dev pack when
    /// the host is a dev build, else the exact content version. Null when none is acceptable.
    /// </summary>
    public static NugetLocalPackage? Pick(IEnumerable<NugetLocalPackage> installed, PackageVersion contentVersion, PackageVersion hostVersion)
    {
        var acceptable = installed.Where(package => IsAcceptable(package.Version, contentVersion, hostVersion)).ToList();
        var devSuffix = DevSuffixOf(hostVersion);
        var dev = devSuffix is null
            ? null
            : acceptable.Where(package => string.Equals(package.Version.SpecialVersion, devSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(package => package.Version)
                .FirstOrDefault();
        return dev ?? acceptable.FirstOrDefault(package => package.Version.Equals(contentVersion));
    }

    /// <summary>
    /// Resolves <paramref name="packageId"/> for the engine: picks among the installed copies, and when none is
    /// acceptable installs the exact content version from the configured sources (once; it then stays in the
    /// store). Returns null when it is neither installed nor obtainable (offline, or not published yet).
    /// </summary>
    /// <param name="installed">Lists the installed copies of a package id.</param>
    /// <param name="install">Installs a package at a version from the sources; null when it could not be obtained.</param>
    /// <param name="log">Receives one line saying which copy won, or why none did.</param>
    public static async Task<NugetLocalPackage?> ResolveAsync(
        string packageId, PackageVersion contentVersion, PackageVersion hostVersion,
        Func<string, IEnumerable<NugetLocalPackage>> installed,
        Func<string, PackageVersion, Task<NugetLocalPackage?>> install,
        Action<string>? log = null)
    {
        var picked = Pick(installed(packageId), contentVersion, hostVersion);
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

        // Re-pick rather than trust the returned object: the install may report through a different package
        // instance than the store enumerates, and a failed install returns null.
        picked = fetched is null ? null : Pick(installed(packageId), contentVersion, hostVersion) ?? fetched;
        log?.Invoke(picked is null
            ? $"{packageId} {contentVersion} could not be installed (not published, or no package source reachable)."
            : $"{packageId}: installed the content version {contentVersion} this engine names.");
        return picked;
    }
}
