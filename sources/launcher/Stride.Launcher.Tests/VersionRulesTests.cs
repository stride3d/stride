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

    [Fact]
    public void TheCustomBuildsOfALineAreManagedApart()
    {
        Assert.Equal("4.5", VersionRules.ManagedKey("4.5", null));
        Assert.Equal("4.5-mystudio", VersionRules.ManagedKey("4.5", "mystudio"));
    }
}
