// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class VersionRowOrderTests
{
    [Fact]
    public void NightlyRow_FollowsTheReleaseRowOfItsMajorMinor()
    {
        (int, int, bool)[] rows = [(4, 4, false), (4, 5, true), (4, 3, false), (4, 5, false), (4, 4, true)];

        var ordered = rows.Order(Comparer<(int, int, bool)>.Create((x, y) => StrideVersionViewModel.Compare(x, y))).ToArray();

        Assert.Equal([(4, 5, false), (4, 5, true), (4, 4, false), (4, 4, true), (4, 3, false)], ordered);
    }

    [Fact]
    public void SameKey_IsEqual()
    {
        Assert.Equal(0, StrideVersionViewModel.Compare((4, 5, true), (4, 5, true)));
        Assert.NotEqual(0, StrideVersionViewModel.Compare((4, 5, true), (4, 5, false)));
    }
}
