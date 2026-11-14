// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class VersionRowOrderTests
{
    private const string Nightly = StrideVersionViewModel.NightlyChannel;
    private const string Releases = "";

    [Fact]
    public void NightlyRow_PrecedesTheReleaseRowOfItsMajorMinor()
    {
        (int, int, string)[] rows = [(4, 4, Releases), (4, 5, Nightly), (4, 3, Releases), (4, 5, Releases), (4, 4, Nightly)];

        var ordered = rows.Order(Comparer<(int, int, string)>.Create((x, y) => StrideVersionViewModel.Compare(x, y))).ToArray();

        Assert.Equal([(4, 5, Nightly), (4, 5, Releases), (4, 4, Nightly), (4, 4, Releases), (4, 3, Releases)], ordered);
    }

    [Fact]
    public void CustomBuildRows_PrecedeTheNightlyAndReleaseRowsOfTheirMajorMinor_ByTag()
    {
        (int, int, string)[] rows = [(4, 4, Releases), (4, 5, "zstudio"), (4, 5, Releases), (4, 4, "mystudio"), (4, 5, "mystudio"), (4, 5, Nightly)];

        var ordered = rows.Order(Comparer<(int, int, string)>.Create((x, y) => StrideVersionViewModel.Compare(x, y))).ToArray();

        Assert.Equal([(4, 5, "mystudio"), (4, 5, "zstudio"), (4, 5, Nightly), (4, 5, Releases), (4, 4, "mystudio"), (4, 4, Releases)], ordered);
    }

    [Fact]
    public void SameKey_IsEqual()
    {
        Assert.Equal(0, StrideVersionViewModel.Compare((4, 5, Nightly), (4, 5, Nightly)));
        Assert.NotEqual(0, StrideVersionViewModel.Compare((4, 5, Nightly), (4, 5, Releases)));
        Assert.NotEqual(0, StrideVersionViewModel.Compare((4, 5, "mystudio"), (4, 5, "zstudio")));
    }
}
