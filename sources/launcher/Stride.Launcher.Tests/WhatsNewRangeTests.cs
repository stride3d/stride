// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class WhatsNewRangeTests
{
    private static (PackageVersion, bool, bool) Build(string version, bool installed, bool onFeed)
        => (PackageVersion.Parse(version), installed, onFeed);

    private static PackageVersion[] Releases(params string[] versions) => versions.Select(PackageVersion.Parse).ToArray();

    private static (PackageVersion?, PackageVersion) Range(string? from, string to)
        => (from is not null ? PackageVersion.Parse(from) : null, PackageVersion.Parse(to));

    [Fact]
    public void FromInstalledNightly_ToNewestOnFeed()
    {
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
        [
            Build("4.5.0-nightly.20261016.3", false, true),
            Build("4.5.0-nightly.20261015.2", false, true),
            Build("4.5.0-nightly.20261014.1", true, true),
        ], Releases("4.4.0", "4.5.0-beta01"));

        Assert.Equal(Range("4.5.0-nightly.20261014.1", "4.5.0-nightly.20261016.3"), range);
    }

    [Fact]
    public void InstalledButPrunedFromTheFeed_StillTheStart()
    {
        // The feed keeps the newest nightlies only: an old installed one is no longer on it
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
        [
            Build("4.5.0-nightly.20261030.20", false, true),
            Build("4.5.0-nightly.20261001.1", true, false),
        ], Releases("4.4.0"));

        Assert.Equal(Range("4.5.0-nightly.20261001.1", "4.5.0-nightly.20261030.20"), range);
    }

    [Fact]
    public void NoNightlyInstalled_FromTheNewestReleaseBefore_OfAnyLine()
    {
        // No 4.5 prerelease yet: since 4.4.0; releases after the nightly (4.5.0) don't count
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
            [Build("4.5.0-nightly.20261016.3", false, true), Build("4.5.0-nightly.20261015.2", false, true)],
            Releases("4.3.0.2507", "4.4.0-beta9", "4.4.0", "4.5.0"));

        Assert.Equal(Range("4.4.0", "4.5.0-nightly.20261016.3"), range);
    }

    [Fact]
    public void NoNightlyInstalled_FromThePrereleaseOfItsLine()
    {
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
            [Build("4.5.0-nightly.20261016.3", false, true)],
            Releases("4.4.0", "4.5.0-beta02", "4.5.0-beta03"));

        Assert.Equal(Range("4.5.0-beta03", "4.5.0-nightly.20261016.3"), range);
    }

    [Fact]
    public void NewestNightlyInstalled_FromTheNewestReleaseBefore()
    {
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
            [Build("4.4.1-nightly.20261016.3", true, true), Build("4.4.1-nightly.20261015.2", false, true)],
            Releases("4.4.0", "4.5.0-beta01"));

        Assert.Equal(Range("4.4.0", "4.4.1-nightly.20261016.3"), range);
    }

    [Fact]
    public void LocalBuilds_AreNeverTheStart()
    {
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
            [Build("4.5.0-nightly.20261016.3", false, true)],
            Releases("4.4.0", "4.5.0-dev3", "4.4.0-beta7-dev4", "4.5.0-beta01-dev2"));

        Assert.Equal(Range("4.4.0", "4.5.0-nightly.20261016.3"), range);
    }

    [Fact]
    public void NoReleaseBefore_FromThePreviousNightly()
    {
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange(
            [Build("4.5.0-nightly.20261016.3", false, true), Build("4.5.0-nightly.20261015.2", false, true)],
            Releases());

        Assert.Equal(Range("4.5.0-nightly.20261015.2", "4.5.0-nightly.20261016.3"), range);
    }

    [Fact]
    public void OnlyOneBuild_AndNoRelease_NoFrom()
    {
        var range = StrideStoreVersionViewModel.ChooseWhatsNewRange([Build("4.5.0-nightly.20261016.3", false, true)], Releases());

        Assert.Equal(Range(null, "4.5.0-nightly.20261016.3"), range);
    }

    [Fact]
    public void NoBuild_Nothing()
    {
        Assert.Null(StrideStoreVersionViewModel.ChooseWhatsNewRange([], Releases("4.4.0")));
    }
}
