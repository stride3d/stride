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
    [InlineData("6.0.1", "6.0.2-req")]
    [InlineData("6.0.1-beta1", "6.0.2")]
    public void IsUpdateCandidate_AcceptsReleaseAndRequired(string current, string candidate)
    {
        Assert.True(IsUpdateCandidate(current, candidate, includePrerelease: false));
    }

    [Theory]
    [InlineData("6.0.1", "6.1.0-beta1")]
    [InlineData("6.0.2-req", "6.1.0-beta1")]
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

    private static bool IsUpdateCandidate(string current, string candidate, bool includePrerelease)
    {
        return SelfUpdater.IsUpdateCandidate(new PackageVersion(candidate), new PackageVersion(current), includePrerelease);
    }
}
