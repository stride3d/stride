// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Which project on a newer version an older editor still opens: only a switch between a local build and another build
/// of the same major.minor or newer.
/// </summary>
public class TestLocalBuildSwitch
{
    [Theory]
    // A local build on either side, same major.minor
    [InlineData("4.4.0-dev3", "4.4.0-beta8")]
    [InlineData("4.4.0-beta8", "4.4.0-dev")]
    [InlineData("4.4.0-beta7-dev4", "4.4.0-beta7")]
    // The editor on a newer major.minor
    [InlineData("4.4.0-dev3", "4.5.0-beta1")]
    public void NewerProject_LocalBuildSwitch_IsAllowed(string projectVersion, string editorVersion)
    {
        Assert.True(PackageSession.IsLocalBuildSwitch(new PackageVersion(projectVersion), new PackageVersion(editorVersion)));
    }

    [Theory]
    // Releases only: a real downgrade
    [InlineData("4.4.0-beta8", "4.4.0-beta7")]
    [InlineData("4.4.1", "4.4.0")]
    // A local build, but the editor on an older major.minor
    [InlineData("4.4.0-beta8", "4.3.0-dev")]
    [InlineData("4.4.0-dev3", "4.3.0.2")]
    public void NewerProject_NotLocalBuildSwitch_IsRefused(string projectVersion, string editorVersion)
    {
        Assert.False(PackageSession.IsLocalBuildSwitch(new PackageVersion(projectVersion), new PackageVersion(editorVersion)));
    }

    [Theory]
    [InlineData("4.4.0-dev", true)]
    [InlineData("4.4.0-dev3", true)]
    [InlineData("4.4.0-beta7-dev4", true)]
    [InlineData("4.4.0-beta8", false)]
    [InlineData("4.4.0", false)]
    public void IsLocalBuild(string version, bool expected)
    {
        Assert.Equal(expected, new PackageVersion(version).IsLocalBuild);
    }
}
