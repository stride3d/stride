// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Launcher.Services;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class SelfUpdaterTests
{
    [Theory]
    [InlineData("6.0.1", "6.0.2")]
    [InlineData("6.0.1-beta1", "6.0.2")]
    public void IsUpdateCandidate_AcceptsRelease(string current, string candidate)
    {
        Assert.True(IsUpdateCandidate(current, candidate, includePrerelease: false));
    }

    [Theory]
    [InlineData("6.0.1", "6.1.0-beta1")]
    [InlineData("6.0.1-beta1", "6.0.2-beta1")]
    public void IsUpdateCandidate_RejectsPrerelease_WhenNotOptedIn(string current, string candidate)
    {
        Assert.False(IsUpdateCandidate(current, candidate, includePrerelease: false));
    }

    [Theory]
    [InlineData("6.0.1-beta1", "6.0.1-beta2")]
    [InlineData("6.0.1-beta2", "6.0.1-rc1")]
    [InlineData("6.0.1-beta1", "6.0.1")]
    public void IsUpdateCandidate_PrereleaseLauncher_FollowsItsOwnVersion(string current, string candidate)
    {
        Assert.True(IsUpdateCandidate(current, candidate, includePrerelease: false));
    }

    [Theory]
    [InlineData("6.0.1", "6.1.0-beta1")]
    [InlineData("6.0.1-beta1", "6.0.2-beta1")]
    public void IsUpdateCandidate_AcceptsEveryPrerelease_WhenOptedIn(string current, string candidate)
    {
        Assert.True(IsUpdateCandidate(current, candidate, includePrerelease: true));
    }

    [Fact]
    public void UpdateRules_ReadsTheUpdateLine()
    {
        var rules = SelfUpdater.UpdateRules.Parse("Stride Launcher\r\n\r\nforce-reinstall: 5.0.1 https://old/setup.exe (note)\r\nupdate: checkpoint reinstall-below=7.0.0 setup=https://new/setup.exe\r\n");

        Assert.True(rules.Checkpoint);
        Assert.Equal(new PackageVersion("7.0.0"), rules.ReinstallBelow);
        Assert.Equal("https://new/setup.exe", rules.Setup);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Stride Launcher\n\nforce-reinstall: 5.0.1 https://old/setup.exe")]
    [InlineData("Stride Launcher\n\nupdate: setup=https://new/setup.exe")]
    public void UpdateRules_WithoutRules_NoCheckpointNorReinstall(string? description)
    {
        var rules = SelfUpdater.UpdateRules.Parse(description);

        Assert.False(rules.Checkpoint);
        Assert.Null(rules.ReinstallBelow);
    }

    [Fact]
    public void UpdateRules_SkipsUnknownAndInvalidWords()
    {
        var rules = SelfUpdater.UpdateRules.Parse("update: later-rule=1 reinstall-below=not-a-version checkpoint");

        Assert.True(rules.Checkpoint);
        Assert.Null(rules.ReinstallBelow);
    }

    [Fact]
    public void ChooseUpdate_WithoutCandidates_ReturnsNull()
    {
        Assert.Null(SelfUpdater.ChooseUpdate([], new PackageVersion("6.0.1")));
    }

    [Fact]
    public void ChooseUpdate_TakesTheNewest()
    {
        Assert.Equal((2, false), ChooseUpdate("6.0.1", "update:", "update:", "update:"));
    }

    [Fact]
    public void ChooseUpdate_TakesTheFirstCheckpoint()
    {
        Assert.Equal((1, false), ChooseUpdate("6.0.1", "update:", "update: checkpoint", "update: checkpoint", "update:"));
    }

    [Fact]
    public void ChooseUpdate_Reinstalls_BelowTheVersionOfTheChosenPackage()
    {
        Assert.Equal((1, true), ChooseUpdate("6.0.1", "update:", "update: reinstall-below=7.0.0"));
    }

    [Fact]
    public void ChooseUpdate_Swaps_FromTheVersionOfTheChosenPackage()
    {
        Assert.Equal((1, false), ChooseUpdate("7.0.0", "update:", "update: reinstall-below=7.0.0"));
    }

    [Fact]
    public void ChooseUpdate_OnlyTheChosenPackageDecidesTheReinstall()
    {
        // The checkpoint is reached by swapping: the newer package's rule applies from there
        Assert.Equal((0, false), ChooseUpdate("6.0.1", "update: checkpoint", "update: reinstall-below=7.0.0"));
    }

    private static (int Index, bool Reinstall)? ChooseUpdate(string current, params string[] descriptions)
    {
        return SelfUpdater.ChooseUpdate(descriptions.Select(SelfUpdater.UpdateRules.Parse).ToList(), new PackageVersion(current));
    }

    private static bool IsUpdateCandidate(string current, string candidate, bool includePrerelease)
    {
        return SelfUpdater.IsUpdateCandidate(new PackageVersion(candidate), new PackageVersion(current), includePrerelease);
    }
}
