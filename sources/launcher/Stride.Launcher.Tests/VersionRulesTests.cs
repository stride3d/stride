// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Cli.Core;
using Stride.Core;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class VersionRulesTests
{
    private static PackageVersion V(string version) => PackageVersion.Parse(version);

    [Theory]
    [InlineData("4.5.3", false, false, true)]
    [InlineData("4.5.4-beta", false, false, false)]
    [InlineData("4.5.4-beta", true, false, true)]
    [InlineData("4.5.4-rc.1", true, false, true)]
    [InlineData("4.5.4-nightly.20261114.60", true, false, false)]
    [InlineData("4.5.4-nightly.20261114.60", false, true, true)]
    // A custom build sorts after a beta of the same version, but is never taken without its tag
    [InlineData("4.5.3-mystudio.32", true, true, false)]
    public void WithoutATagNoCustomBuildIsTaken(string version, bool includePrerelease, bool includeNightly, bool eligible)
        => Assert.Equal(eligible, VersionRules.IsEligible(V(version), includePrerelease, includeNightly));

    [Theory]
    // A local build is neither a release nor a prerelease: only the tag of its checkout takes it
    [InlineData("4.5.5-dev8", true, true, null, false)]
    [InlineData("4.4.0-beta7-dev4", true, true, null, false)]
    [InlineData("4.5.5-dev8", false, false, "dev8", true)]
    [InlineData("4.5.5-DEV8", false, false, "dev8", true)]
    [InlineData("4.4.0-beta7-dev4", false, false, "dev4", true)]
    [InlineData("4.5.5-dev", false, false, "dev", true)]
    [InlineData("4.5.5-dev19", false, false, "dev8", false)]
    [InlineData("4.5.5-dev8", false, false, "dev", false)]
    // The tag of a checkout takes nothing else
    [InlineData("4.5.3", false, false, "dev8", false)]
    [InlineData("4.5.4-nightly.20261117.80", false, true, "dev8", false)]
    public void ALocalBuildIsTakenOnlyByTheTagOfItsCheckout(string version, bool includePrerelease, bool includeNightly, string? tag, bool eligible)
        => Assert.Equal(eligible, VersionRules.IsEligible(V(version), includePrerelease, includeNightly, tag));

    [Theory]
    [InlineData("4.5.3-mystudio.32", true)]
    [InlineData("4.5.3-MyStudio33", true)]
    [InlineData("4.5.3-otherstudio.5", false)]
    [InlineData("4.5.3", false)]
    [InlineData("4.5.4-beta", false)]
    public void WithATagOnlyItsBuildsAreTaken(string version, bool eligible)
        => Assert.Equal(eligible, VersionRules.IsEligible(V(version), includePrerelease: false, includeNightly: false, tag: "mystudio"));

    private static readonly PackageVersion[] Feed =
        [V("4.6.0-beta02"), V("4.6.0-beta01"), V("4.5.4-beta"), V("4.5.4-nightly.20261114.60"), V("4.5.3-mystudio.32"), V("4.5.3"), V("4.4.2")];

    [Theory]
    [InlineData(null, false, false, "4.5.3")]
    [InlineData("4.5", false, false, "4.5.3")]
    [InlineData("4.5", true, false, "4.5.4-beta")]
    [InlineData("4.5", false, true, "4.5.4-nightly.20261114.60")]
    // A line with only betas takes its newest beta, as the launcher's row offers it
    [InlineData("4.6", false, false, "4.6.0-beta02")]
    [InlineData("4.7", false, false, null)]
    public void TheNewestOfALineIsItsNewestRelease_ElseItsNewestPrerelease(string? line, bool includePrerelease, bool includeNightly, string? newest)
        => Assert.Equal(newest is null ? null : V(newest), VersionRules.ChooseNewest(Feed, line, includePrerelease, includeNightly));

    [Fact]
    public void ALineWithNoReleaseFallsBackToNoNightlyNorCustomBuild()
    {
        PackageVersion[] feed = [V("4.6.0-nightly.20261114.60"), V("4.6.0-mystudio.3")];
        Assert.Null(VersionRules.ChooseNewest(feed, "4.6", includePrerelease: false, includeNightly: false));
    }

    [Fact]
    public void WithATagALineDoesNotFallBackToItsBetas()
        => Assert.Null(VersionRules.ChooseNewest(Feed, "4.6", includePrerelease: false, includeNightly: false, tag: "mystudio"));

    // The versions installed on a contributor's computer: the local builds of two checkouts, at the same version, sort above
    // the nightly of their line
    private static readonly PackageVersion[] InstalledVersions =
        [V("4.6.0-beta01"), V("4.5.5-dev8"), V("4.5.5-dev19"), V("4.5.4-nightly.20261117.80"), V("4.5.3-mystudio.32"), V("4.5.3"), V("4.4.2")];

    [Theory]
    [InlineData(null, false, false, null, "4.5.3")]
    [InlineData(null, false, true, null, "4.5.4-nightly.20261117.80")]
    [InlineData(null, true, false, null, "4.6.0-beta01")]
    [InlineData("4.5", false, false, null, "4.5.3")]
    // A local build is never a prerelease nor a nightly
    [InlineData("4.5", true, false, null, "4.5.3")]
    [InlineData("4.5", false, true, null, "4.5.4-nightly.20261117.80")]
    // The tag of a checkout takes its build, whichever sorts first
    [InlineData(null, false, false, "dev19", "4.5.5-dev19")]
    [InlineData("4.5", false, false, "dev8", "4.5.5-dev8")]
    [InlineData("4.5", false, false, "mystudio", "4.5.3-mystudio.32")]
    [InlineData("4.4", false, true, null, "4.4.2")]
    // A line with only betas takes its newest beta
    [InlineData("4.6", false, false, null, "4.6.0-beta01")]
    [InlineData("4.7", false, true, null, null)]
    [InlineData("4.4", false, false, "dev8", null)]
    // An exact version is taken whatever the options, when it is one of them
    [InlineData("4.5.5-dev8", false, false, null, "4.5.5-dev8")]
    [InlineData("4.5.4-nightly.20261117.80", false, false, null, "4.5.4-nightly.20261117.80")]
    [InlineData("4.5.4-nightly.20261116.63", false, true, null, null)]
    public void AVersionTextNamesTheNewestAllowed_TheNewestOfItsLine_OrExactlyItself(string? versionSpec, bool includePrerelease, bool includeNightly, string? tag, string? expected)
        => Assert.Equal(expected is null ? null : V(expected), VersionRules.Resolve(InstalledVersions, versionSpec, includePrerelease, includeNightly, tag));

    [Theory]
    [InlineData("4.5", "4.5")]
    [InlineData("04.05", "4.5")]
    [InlineData("4.5.3", null)]
    [InlineData("4.5-nightly", null)]
    [InlineData("nightly", null)]
    public void ALineIsAMajorMinor(string versionSpec, string? line)
    {
        Assert.Equal(line is not null, VersionRules.TryParseLine(versionSpec, out var parsed));
        Assert.Equal(line, parsed);
    }

    [Fact]
    public void WhenNothingInstalledMatches_TheMessageSaysWhatInstallsIt()
    {
        Assert.Equal("No installed Stride version matches '4.5 --nightly'. Install one with: stride sdk install 4.5 --nightly",
            VersionRules.NoInstalledMatch("4.5", new VersionFilter(Nightly: true)));
        Assert.Equal("No installed Stride version matches '--prerelease --tag mystudio'. Install one with: stride sdk install --prerelease --tag mystudio",
            VersionRules.NoInstalledMatch(null, new VersionFilter(Prerelease: true, Tag: "mystudio")));
        Assert.Equal("No installed Stride version matches '4.5 --tag dev8'. The builds tagged dev8 come from building that Stride checkout.",
            VersionRules.NoInstalledMatch("4.5", new VersionFilter(Tag: "dev8")));
    }

    [Theory]
    [InlineData("4.5.0", false)]
    [InlineData("4.5.0,4.5.1-beta01", true)]
    // Once the release is out, the line is back on releases
    [InlineData("4.5.1-beta01,4.5.1", false)]
    [InlineData("4.5.0-beta06", true)]
    // Nightlies, custom builds and local builds don't put a line on prereleases
    [InlineData("4.5.0,4.5.1-nightly.20261114.60,4.5.1-mystudio.3", false)]
    // Another line's beta doesn't count
    [InlineData("4.5.0,4.6.0-beta01", false)]
    public void ALineOnABetaFollowsTheBetas(string installed, bool follows)
        => Assert.Equal(follows, VersionRules.FollowsPrereleases(installed.Split(',').Select(V), "4.5"));

    private static readonly PackageVersion[] Installed = [V("4.5.3"), V("4.5.3-mystudio.30"), V("4.5.2-nightly.20261110.50"), V("4.4.2")];

    [Fact]
    public void EveryLineIsUpdated_AndItsCustomBuildsFollowTheirTag()
        => Assert.Equal([("4.5", null), ("4.5", "mystudio"), ("4.4", null)], VersionRules.UpdateTargets(Installed, null, null));

    [Fact]
    public void WithATagOnlyTheLinesOnItAreUpdated()
        => Assert.Equal([("4.5", "mystudio")], VersionRules.UpdateTargets(Installed, null, "mystudio"));

    [Fact]
    public void AGivenLineIsUpdated_WithItsCustomBuilds()
        => Assert.Equal([("4.5", null), ("4.5", "mystudio")], VersionRules.UpdateTargets(Installed, "4.5", null));

    [Fact]
    public void AGivenLineAndTagAreUpdated_EvenWithNothingInstalled()
        => Assert.Equal([("4.6", "mystudio")], VersionRules.UpdateTargets(Installed, "4.6", "mystudio"));

    [Theory]
    [InlineData(false, null, true, null)]
    [InlineData(false, "4.4.1", true, "4.4.1")]
    // Nothing to do: update prints "already up to date"
    [InlineData(true, "4.4.2", false, null)]
    [InlineData(true, null, false, null)]
    // Installed manually: not installed again nor managed, but the version update installed before goes
    [InlineData(true, "4.4.1", false, "4.4.1")]
    public void UpdateInstallsOnlyANewestNotInstalled_AndRetiresTheOneItManaged(bool newestInstalled, string? managed, bool install, string? retire)
        => Assert.Equal((install, retire is null ? null : V(retire)), VersionRules.UpdateStep(V("4.4.2"), newestInstalled, managed is null ? null : V(managed)));

    [Fact]
    public void TheCustomBuildsOfALineAreManagedApart()
    {
        Assert.Equal("4.5", VersionRules.ManagedKey("4.5", null));
        Assert.Equal("4.5-mystudio", VersionRules.ManagedKey("4.5", "mystudio"));
    }

    private static readonly string[] PublishedAvalonia = ["4.5.0-nightly.20261115.12", "4.5.0-nightly.20261116.3", "4.5.0-beta1"];

    [Theory]
    [InlineData("4.5.0-nightly.20261116.3", "4.5.0-nightly.20261116.3")]
    [InlineData("4.5.0-beta1", "4.5.0-beta1")]
    // Not published at that version: the version installs with its main Game Studio only, never with another build
    [InlineData("4.5.0-nightly.20261114.40", null)]
    [InlineData("4.5.0", null)]
    [InlineData("4.4.2", null)]
    public void TheOtherGameStudioInstallsAtExactlyTheVersion(string version, string? expected)
        => Assert.Equal(expected, VersionRules.CompanionBuild(PublishedAvalonia, V, V(version)));
}
