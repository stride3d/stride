// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class VersionRowTests
{
    [Theory]
    [InlineData("4.5.3", "")]
    [InlineData("4.5.0-beta06", "")]
    [InlineData("4.5.3-beta", "")]
    [InlineData("4.0.0.1-beta02-0926", "")]
    // The other prereleases go with the releases too
    [InlineData("4.5.0-alpha1", "")]
    [InlineData("4.5.0-preview.2", "")]
    [InlineData("4.5.0-rc1", "")]
    [InlineData("4.5.3-nightly.20261114.60", StrideVersionViewModel.NightlyChannel)]
    // The tag of a custom build: the word its label starts with, without the number after it
    [InlineData("4.5.3-mystudio", "mystudio")]
    [InlineData("4.5.3-mystudio.32", "mystudio")]
    [InlineData("4.5.3-mystudio32", "mystudio")]
    [InlineData("4.5.3-MyStudio.beta", "mystudio")]
    [InlineData("4.5.3-my-studio2.5", "my-studio")]
    [InlineData("4.5.3-betamax.3", "betamax")]
    // A local build has rows of its own
    [InlineData("4.5.4-dev4", "")]
    public void TheChannelOfAVersionIsItsRow(string version, string channel)
        => Assert.Equal(channel, StrideVersionViewModel.ChannelOf(PackageVersion.Parse(version)));

    [Theory]
    [InlineData("mystudio", "MYSTUDIO")]
    [InlineData("mycompanyfor", "MYCOMPANYFOR")]
    [InlineData("mycompanyinternalfork", "MYCOMPANYIN…")]
    public void ALongTagIsCutInItsBadge(string tag, string badge)
        => Assert.Equal(badge, StrideVersionViewModel.TagBadgeOf(tag));

    [Fact]
    public void ACustomBuildIsNotABeta()
        => Assert.False(StrideVersionViewModel.IsBetaVersion(PackageVersion.Parse("4.5.3-mystudio.beta")));

    [Theory]
    [InlineData("dev", 1)]
    [InlineData("dev2", 2)]
    [InlineData("beta8-dev13", 13)]
    [InlineData("beta8", null)]
    public void TheCheckoutNumberIsTheOneOfItsDevSuffix(string label, int? number)
        => Assert.Equal(number, StrideDevVersionViewModel.CheckoutNumber(label));

    private static PackageVersion V(string version) => PackageVersion.Parse(version);

    [Fact]
    public void ACheckoutIsShownAtTheVersionItBuilds()
        => Assert.Equal(V("4.5.0-dev2"), StrideDevVersionViewModel.ChooseCheckoutVersion([(V("4.4.0-dev2"), true), (V("4.4.1-dev2"), true), (V("4.5.0-dev2"), true)], V("4.5.0-dev2")));

    [Fact]
    public void ACheckoutBuiltAtAVersionWithoutAPackageIsShownByItsNewestStub()
        => Assert.Equal(V("4.5.0-dev4"), StrideDevVersionViewModel.ChooseCheckoutVersion([(V("4.4.0-dev4"), true), (V("4.5.0-dev4"), true), (V("4.5.3-dev4"), false)], V("4.5.4-dev4")));

    [Fact]
    public void ACheckoutWithOnlyPackagesIsShownByItsNewest()
        => Assert.Equal(V("4.5.3-dev6"), StrideDevVersionViewModel.ChooseCheckoutVersion([(V("4.5.2-dev6"), false), (V("4.5.3-dev6"), false)], null));
}
