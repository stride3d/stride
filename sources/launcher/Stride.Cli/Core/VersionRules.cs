// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics.CodeAnalysis;
using Stride.Core;

namespace Stride.Cli.Core;

// Which versions install, update, available, upgrade and the tools (studio, asset) take when no exact version is given.
internal static class VersionRules
{
    // Whether the newest version or the newest of a line may be this one. With a tag, only the builds of that tag: the
    // custom builds of a studio (PackageVersion.CustomTag, mystudio), or the local builds of a checkout
    // (PackageVersion.LocalBuildTag, dev8). Without one, a release, a nightly and a prerelease (alpha, beta, preview, rc)
    // each only when opted in, and never a custom or a local build, which are only taken by their version or their tag.
    public static bool IsEligible(PackageVersion version, bool includePrerelease, bool includeNightly, string? tag = null)
        => tag is not null ? string.Equals(version.CustomTag ?? version.LocalBuildTag, tag, StringComparison.OrdinalIgnoreCase)
            : version.IsNightly ? includeNightly
            : version.IsPrerelease ? includePrerelease
            : version.CustomTag is null && !version.IsLocalBuild;

    // The newest version that the newest version or the newest of a line takes (IsEligible); null for any line. A line with
    // no such version, such as a Major.Minor that only has betas so far, takes its newest prerelease, as the launcher does.
    public static PackageVersion? ChooseNewest(IEnumerable<PackageVersion> versions, string? line, bool includePrerelease, bool includeNightly, string? tag = null)
    {
        var ofLine = versions.Where(version => line is null || LineOf(version) == line).ToList();
        return ofLine.Where(version => IsEligible(version, includePrerelease, includeNightly, tag)).Max()
            ?? (tag is null ? ofLine.Where(version => version.IsPrerelease).Max() : null);
    }

    // The version that a version text names among the candidates, as install and the tools take it: the newest one
    // (ChooseNewest) when there is no text, the newest of a Major.Minor line, else exactly that version.
    public static PackageVersion? Resolve(IEnumerable<PackageVersion> candidates, string? versionSpec, bool includePrerelease, bool includeNightly, string? tag = null)
    {
        if (string.IsNullOrEmpty(versionSpec))
            return ChooseNewest(candidates, null, includePrerelease, includeNightly, tag);
        if (TryParseLine(versionSpec, out var line))
            return ChooseNewest(candidates, line, includePrerelease, includeNightly, tag);
        var target = new PackageVersion(versionSpec);
        return candidates.FirstOrDefault(version => version.Equals(target));
    }

    // What to say when no installed version matches a line or options: what installs one, or, for the tag of a checkout
    // (dev8, a label that PackageVersion.IsLocalBuild takes), where its builds come from.
    public static string NoInstalledMatch(string? versionSpec, VersionFilter filter)
    {
        var asked = $"{versionSpec} {filter.Options}".Trim();
        return filter.Tag?.StartsWith("dev", StringComparison.OrdinalIgnoreCase) == true
            ? $"No installed Stride version matches '{asked}'. The builds tagged {filter.Tag} come from building that Stride checkout."
            : $"No installed Stride version matches '{asked}'. Install one with: stride sdk install {asked}";
    }

    // Whether a version text is a Major.Minor line (4.5), and that line as LineOf writes it.
    public static bool TryParseLine(string versionSpec, [NotNullWhen(true)] out string? line)
    {
        var parts = versionSpec.Split('.');
        line = parts.Length == 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor) ? $"{major}.{minor}" : null;
        return line is not null;
    }

    // Whether update takes the prereleases of a line without --prerelease: when the newest release or prerelease installed in
    // it is a prerelease, which then follows the prereleases until the next release, as in the launcher. Nightlies, custom
    // builds and local builds don't count.
    public static bool FollowsPrereleases(IEnumerable<PackageVersion> installed, string line)
        => installed.Where(version => LineOf(version) == line && (version.IsPrerelease || string.IsNullOrEmpty(version.SpecialVersion))).Max()?.IsPrerelease == true;

    public static string LineOf(PackageVersion version) => $"{version.Version.Major}.{version.Version.Minor}";

    // What update updates: each line, with the tag of the custom builds it follows (null for its releases). The given line
    // (on the given tag), else every line with an installed version; an installed custom build keeps its line on its tag.
    // With a tag and no line, only the lines with a build of that tag installed.
    public static IReadOnlyList<(string Line, string? Tag)> UpdateTargets(IEnumerable<PackageVersion> installed, string? line, string? tag)
    {
        var installedTargets = installed.Select(version => (Line: LineOf(version), Tag: version.CustomTag)).Distinct().ToList();
        if (line is not null)
        {
            // An explicit line is updated even with nothing installed in it, as before; without a tag, its custom builds too
            var targets = new List<(string Line, string? Tag)> { (line, tag) };
            if (tag is null)
                targets.AddRange(installedTargets.Where(target => target.Line == line && target.Tag is not null));
            return targets;
        }
        return tag is null ? installedTargets : installedTargets.Where(target => target.Tag == tag).ToList();
    }

    // What update does in a line once it has found its newest version: install it unless it is installed already, and
    // retire the version it managed before, if another. Update manages only what it installs: a newest version installed
    // with 'install' stays the user's, so a later update never removes it.
    public static (bool Install, PackageVersion? Retire) UpdateStep(PackageVersion newest, bool newestInstalled, PackageVersion? managed)
        => (!newestInstalled, managed is not null && managed != newest ? managed : null);

    // The key in installs.json of the version update manages for a line: the line for its releases, line-tag for the
    // custom builds of a tag (4.5-mystudio), so that each is replaced only by a build of its own.
    public static string ManagedKey(string line, string? tag) => tag is null ? line : $"{line}-{tag}";

    // The build of another Game Studio (the Avalonia one) that install and update add to a version: the one the sources
    // publish at exactly that version, else none, as it isn't published for every version. Never another version, whose
    // engine packages would not be the version's.
    public static TBuild? CompanionBuild<TBuild>(IEnumerable<TBuild> published, Func<TBuild, PackageVersion> versionOf, PackageVersion version)
        where TBuild : class
        => published.FirstOrDefault(build => versionOf(build).Equals(version));
}
