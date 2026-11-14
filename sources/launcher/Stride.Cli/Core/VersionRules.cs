// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Cli.Core;

// Which versions install, update, available and upgrade take when no exact version is given.
internal static class VersionRules
{
    // Whether the newest version or the newest of a line may be this one. With a tag, only the custom builds of that tag
    // (PackageVersion.CustomTag). Without one, never a custom build (a studio's own builds are only taken by their
    // version or their tag), a nightly only when opted in, even with prereleases, and a prerelease only when opted in.
    public static bool IsEligible(PackageVersion version, bool includePrerelease, bool includeNightly, string? tag = null)
        => tag is not null ? string.Equals(version.CustomTag, tag, StringComparison.OrdinalIgnoreCase)
            : version.CustomTag is null && (version.IsNightly ? includeNightly : includePrerelease || string.IsNullOrEmpty(version.SpecialVersion));

    // The newest version that the newest version or the newest of a line takes (IsEligible); null for any line. A line with
    // no such version, such as a Major.Minor that only has betas so far, takes its newest prerelease, as the launcher does.
    public static PackageVersion? ChooseNewest(IEnumerable<PackageVersion> versions, string? line, bool includePrerelease, bool includeNightly, string? tag = null)
    {
        var ofLine = versions.Where(version => line is null || LineOf(version) == line).ToList();
        return ofLine.Where(version => IsEligible(version, includePrerelease, includeNightly, tag)).Max()
            ?? (tag is null ? ofLine.Where(version => version.IsPrerelease).Max() : null);
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

    // The key in installs.json of the version update manages for a line: the line for its releases, line-tag for the
    // custom builds of a tag (4.5-mystudio), so that each is replaced only by a build of its own.
    public static string ManagedKey(string line, string? tag) => tag is null ? line : $"{line}-{tag}";
}
