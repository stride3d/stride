// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class OfferedVersionTests
{
    // Betas between releases, as the versioning of 4.5 makes them
    private static readonly PackageVersion[] Feed = [.. new[] { "4.5.4-beta", "4.5.3", "4.5.2-beta", "4.5.1" }.Select(PackageVersion.Parse)];

    private static string? Offered(string? installed, params PackageVersion[] feed)
        => StrideStoreVersionViewModel.ChooseOfferedVersion(installed is null ? null : PackageVersion.Parse(installed), feed)?.ToString();

    [Theory]
    // Not installed: the newest release, not the newer beta
    [InlineData(null, "4.5.3")]
    // On a release: its update is a release; a newer beta isn't one
    [InlineData("4.5.1", "4.5.3")]
    [InlineData("4.5.3", "4.5.3")]
    // On a beta: the newest build, beta or not
    [InlineData("4.5.2-beta", "4.5.4-beta")]
    public void ReleasesFirstUnlessOnABeta(string? installed, string expected)
        => Assert.Equal(expected, Offered(installed, Feed));

    [Fact]
    public void ALineWithOnlyBetasOffersItsNewest()
        => Assert.Equal("4.5.0-beta2", Offered(null, PackageVersion.Parse("4.5.0-beta2"), PackageVersion.Parse("4.5.0-beta1")));

    // A local build packed to the dev feed (StrideDevPackages=false) lands in the release row: it isn't a release
    [Theory]
    [InlineData(null, "4.5.3")]
    [InlineData("4.5.3", "4.5.3")]
    // On the local build: the newest build, not an older release
    [InlineData("4.5.4-dev4", "4.5.4-dev4")]
    public void ALocalBuildIsNotARelease(string? installed, string expected)
        => Assert.Equal(expected, Offered(installed, PackageVersion.Parse("4.5.4-dev4"), PackageVersion.Parse("4.5.3")));

    [Theory]
    [InlineData("4.5.3", true)]
    [InlineData("4.5.4-beta", false)]
    [InlineData("4.5.4-dev4", false)]
    [InlineData("4.5.3-nightly.20261114.60", false)]
    [InlineData("4.5.3-mystudio.32", false)]
    public void AReleaseHasNoLabel(string version, bool isRelease)
        => Assert.Equal(isRelease, StrideVersionViewModel.IsReleaseVersion(PackageVersion.Parse(version)));

    [Fact]
    public void ANightlyRowOffersItsNewestNightly()
        => Assert.Equal("4.5.3-nightly.20261114.60", Offered("4.5.3-nightly.20261113.58",
            PackageVersion.Parse("4.5.3-nightly.20261114.60"), PackageVersion.Parse("4.5.3-nightly.20261113.58")));

    [Theory]
    [InlineData("4.5.0-beta06", true)]
    [InlineData("4.5.3-beta", true)]
    [InlineData("4.5.3", false)]
    [InlineData("4.5.3-nightly.20261114.60", false)]
    [InlineData("4.5.4-dev10", false)]
    [InlineData("4.4.0-beta7-dev4", false)]
    public void ABetaIsAPrereleaseThatIsNeitherANightlyNorALocalBuild(string version, bool isBeta)
        => Assert.Equal(isBeta, StrideVersionViewModel.IsBetaVersion(PackageVersion.Parse(version)));
}
