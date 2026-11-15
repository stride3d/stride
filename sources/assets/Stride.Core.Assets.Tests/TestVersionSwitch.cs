// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Which project on a newer version an older editor still opens: a version with the same number, or a switch where one
/// side is a local or nightly build and the editor has the same major.minor or newer.
/// </summary>
public class TestVersionSwitch
{
    [Theory]
    // The same number: the same upgrader gates
    [InlineData("4.5.3-beta", "4.5.3-alpha")]
    [InlineData("4.5.3", "4.5.3-beta")]
    [InlineData("4.5.3-nightly.20261014.3", "4.5.3-beta")]
    [InlineData("4.5.3-mystudio", "4.5.3-beta")]
    [InlineData("4.5.3.0-beta", "4.5.3-alpha")]
    // A local build on either side, same major.minor
    [InlineData("4.5.7-dev3", "4.5.6-beta")]
    [InlineData("4.5.7-beta", "4.5.6-dev")]
    [InlineData("4.5.7-beta-dev4", "4.5.6-beta")]
    // A nightly on either side, same major.minor
    [InlineData("4.5.7-nightly.20261015.4", "4.5.6-nightly.20261014.3")]
    [InlineData("4.5.7-nightly.20261014.3", "4.5.6-beta")]
    [InlineData("4.5.7-beta", "4.5.6-nightly.20261014.3")]
    [InlineData("4.5.7", "4.5.6-nightly.20261014.3")]
    // The editor on a newer major.minor
    [InlineData("4.4.0-dev3", "4.5.0-beta")]
    [InlineData("5.0.0-nightly.20261014.3", "5.1.0-beta")]
    public void NewerProject_VersionSwitch_IsAllowed(string projectVersion, string editorVersion)
    {
        Assert.True(PackageSession.IsVersionSwitch(new PackageVersion(projectVersion), new PackageVersion(editorVersion)));
    }

    [Theory]
    // Releases and custom labels on another number: a real downgrade
    [InlineData("4.5.7-beta", "4.5.6-beta")]
    [InlineData("4.5.7-mystudio", "4.5.6-mystudio")]
    [InlineData("4.4.1", "4.4.0")]
    // A local or nightly build, but the editor on an older major.minor
    [InlineData("4.4.0-beta8", "4.3.0-dev")]
    [InlineData("4.4.0-dev3", "4.3.0.2")]
    [InlineData("4.5.0-nightly.20261014.3", "4.4.2")]
    [InlineData("4.5.0", "4.4.2-nightly.20261014.3")]
    public void NewerProject_NotVersionSwitch_IsRefused(string projectVersion, string editorVersion)
    {
        Assert.False(PackageSession.IsVersionSwitch(new PackageVersion(projectVersion), new PackageVersion(editorVersion)));
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

    [Theory]
    [InlineData("4.5.0-nightly.20261014.3", true)]
    [InlineData("5.0.1-beta1.nightly.20261014.3", true)]
    [InlineData("4.5.0-beta01", false)]
    [InlineData("4.5.0-dev3", false)]
    [InlineData("4.5.0", false)]
    public void IsNightly(string version, bool expected)
    {
        var packageVersion = new PackageVersion(version);
        Assert.Equal(expected, packageVersion.IsNightly);
        Assert.False(packageVersion.IsNightly && packageVersion.IsLocalBuild);
    }
}
