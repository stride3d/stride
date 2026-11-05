// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class RecentProjectTests
{
    [Theory]
    [InlineData("4.3.0.2501", "4.3.0.2503")]
    [InlineData("4.3.0.2503", "4.4.0-beta8")]
    [InlineData("4.4.0-beta7", "4.4.0-beta8")]
    [InlineData("4.4.0-beta8", "4.4.0")]
    [InlineData("4.3.0-dev3", "4.4.0-beta8")]
    // A local build of a newer major.minor: Game Studio doesn't switch back
    [InlineData("4.3.0.2503", "4.4.0-dev3")]
    public void IsUpgrade_OneWay(string project, string target)
    {
        Assert.True(RecentProjectViewModel.IsUpgrade(PackageVersion.Parse(project), PackageVersion.Parse(target)));
    }

    [Theory]
    [InlineData("4.4.0-beta8", "4.4.0-beta8")]
    [InlineData("4.4.0-beta8", "4.4.0-beta7")]
    // Switches to and from a local build of the same numbers
    [InlineData("4.4.0-beta8", "4.4.0-dev3")]
    [InlineData("4.4.0-beta8", "4.4.0-beta8-dev3")]
    [InlineData("4.4.0-dev3", "4.4.0-beta8")]
    [InlineData("4.4.0-dev3", "4.4.1")]
    [InlineData("4.4.0-beta8", "4.4.1-dev4")]
    public void IsUpgrade_SameOlderOrSwitch(string project, string target)
    {
        Assert.False(RecentProjectViewModel.IsUpgrade(PackageVersion.Parse(project), PackageVersion.Parse(target)));
    }

    [Theory]
    // The same number, from 4.5
    [InlineData("4.5.3-beta", "4.5.3-alpha", true)]
    [InlineData("4.5.3", "4.5.3-beta", true)]
    [InlineData("4.5.3-mystudio", "4.5.3-beta", true)]
    // A local or nightly build on either side
    [InlineData("4.5.7-dev3", "4.5.6-beta", true)]
    [InlineData("4.5.7-beta", "4.5.6-dev", true)]
    [InlineData("4.5.7-nightly.20261015.4", "4.5.6-nightly.20261014.3", true)]
    [InlineData("4.5.7-beta", "4.5.6-nightly.20261014.3", true)]
    // A real downgrade
    [InlineData("4.5.7-beta", "4.5.6-beta", false)]
    [InlineData("4.5.7-mystudio", "4.5.6-mystudio", false)]
    // An older major.minor
    [InlineData("4.5.0-nightly.20261014.3", "4.4.2", false)]
    [InlineData("4.5.0-dev3", "4.4.2", false)]
    // Before 4.5, Game Studio only switches with a local build
    [InlineData("4.4.0-beta8", "4.4.0-beta7", false)]
    [InlineData("4.4.0-nightly.20261015.4", "4.4.0-nightly.20261014.3", false)]
    [InlineData("4.4.0-beta8", "4.4.0-dev3", true)]
    [InlineData("4.4.1-dev3", "4.4.0", true)]
    public void OpensNewerProject_FollowsGameStudio(string project, string editor, bool expected)
    {
        Assert.Equal(expected, RecentProjectViewModel.OpensNewerProject(PackageVersion.Parse(project), PackageVersion.Parse(editor)));
    }

    [Theory]
    [InlineData("4.4.0", "4.5.0", true)]
    [InlineData("4.5.0-nightly.20261014.3", "4.5.0-nightly.20261015.4", true)]
    [InlineData("4.5.0-nightly.20261014.3", "4.5.0", true)]
    // A nightly is an upgrade from a release or a prerelease, but never suggested to one
    [InlineData("4.5.0-beta01", "4.5.0-nightly.20261014.3", false)]
    [InlineData("4.4.0", "4.5.0-nightly.20261014.3", false)]
    public void IsSuggestedUpgrade_NightlyOnlyFromNightly(string project, string target, bool expected)
    {
        Assert.True(RecentProjectViewModel.IsUpgrade(PackageVersion.Parse(project), PackageVersion.Parse(target)));
        Assert.Equal(expected, RecentProjectViewModel.IsSuggestedUpgrade(PackageVersion.Parse(project), PackageVersion.Parse(target)));
    }
}
