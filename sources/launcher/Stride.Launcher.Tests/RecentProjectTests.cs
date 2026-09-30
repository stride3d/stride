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
}
