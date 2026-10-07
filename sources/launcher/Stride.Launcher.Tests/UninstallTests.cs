// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Launcher.Services;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class UninstallTests
{
    [Theory]
    [InlineData(new[] { "/uninstall" }, false)]
    [InlineData(new[] { "/uninstall", "/quiet" }, true)]
    [InlineData(new[] { "/Quiet", "/Uninstall" }, true)]
    public void ProcessArguments_ReadsUninstallAndQuiet(string[] args, bool quiet)
    {
        var arguments = Launcher.ProcessArguments(args);

        Assert.Equal([LauncherArguments.ActionType.Uninstall], arguments.Actions);
        Assert.Equal(quiet, arguments.Quiet);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    // No desktop (SYSTEM, session 0): a dialog would be invisible and block the setup
    [InlineData(false, false, true)]
    public void IsQuietUninstall(bool quiet, bool userInteractive, bool expected)
    {
        Assert.Equal(expected, Launcher.IsQuietUninstall(quiet, userInteractive));
    }

    // GameStudio 4.4 and 4.3 share Core, a local build uses its own dev packages and one 4.4 tool
    private static readonly Dictionary<string, string[]> Graph = new()
    {
        ["GameStudio/4.4"] = ["Engine/4.4", "Tool/4.4"],
        ["Engine/4.4"] = ["Core/shared"],
        ["GameStudio/4.3"] = ["Engine/4.3"],
        ["Engine/4.3"] = ["Core/shared"],
        ["GameStudio/dev"] = ["Engine/dev", "Tool/4.4"],
        ["Engine/dev"] = [],
    };

    private static IEnumerable<string> Dependencies(string package) => Graph.GetValueOrDefault(package, []);

    [Fact]
    public void FindRemovable_RemovesReleasesThenTheirPackages()
    {
        var removable = StridePackageReferences.FindRemovable(["GameStudio/4.4", "GameStudio/4.3"], [], Dependencies, StringComparer.Ordinal);

        Assert.Equal(["GameStudio/4.4", "GameStudio/4.3"], removable.Take(2));
        Assert.Equal(["Core/shared", "Engine/4.3", "Engine/4.4", "Tool/4.4"], removable.Skip(2).Order());
    }

    [Fact]
    public void FindRemovable_KeepsLocalBuildsAndWhatTheyUse()
    {
        var removable = StridePackageReferences.FindRemovable(["GameStudio/4.4"], ["GameStudio/dev"], Dependencies, StringComparer.Ordinal);

        Assert.Equal(["Core/shared", "Engine/4.4", "GameStudio/4.4"], removable.Order());
    }

    [Fact]
    public void Find_ExcludesMainPackages()
    {
        var referenced = StridePackageReferences.Find(["GameStudio/4.3"], Dependencies, StringComparer.Ordinal);

        Assert.Equal(["Core/shared", "Engine/4.3"], referenced.Order());
    }

    [Theory]
    [InlineData(0L, "1 MB")]
    [InlineData(300L << 20, "300 MB")]
    [InlineData(3L << 30, "3 GB")]
    public void FormatSize(long bytes, string expected)
    {
        Assert.Equal(expected, Launcher.FormatSize(bytes));
    }
}
