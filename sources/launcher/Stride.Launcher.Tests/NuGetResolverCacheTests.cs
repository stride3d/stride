// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using NuGet.Versioning;
using Stride.Core.Assets;
using Stride.Core.Packages;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class NuGetResolverCacheTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "stride-resolver-cache-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    [Theory]
    [InlineData("Stride.GameStudio-4.5.0-dev-net10.0-windows7.0-win-x64", "Stride.GameStudio", "4.5.0-dev", true)]
    [InlineData("Stride.GameStudio-4.5.0-dev-net10.0-linux-x64", "Stride.GameStudio", "4.5.0-dev", true)]
    [InlineData("stride.gamestudio-4.5.0-dev-net10.0-win-x64", "Stride.GameStudio", "4.5.0-dev", true)]
    // The release doesn't take the folders of its dev versions
    [InlineData("Stride.GameStudio-4.5.0-dev-net10.0-win-x64", "Stride.GameStudio", "4.5.0", false)]
    [InlineData("Stride.GameStudio-4.5.0-dev2-net10.0-win-x64", "Stride.GameStudio", "4.5.0", false)]
    [InlineData("Stride.GameStudio-4.5.0-dev2-net10.0-win-x64", "Stride.GameStudio", "4.5.0-dev", false)]
    // Nor a package the folders of another one whose id it starts
    [InlineData("Stride.Core.Assets-4.5.0-dev-net10.0-win-x64", "Stride.Core", "4.5.0-dev", false)]
    public void IsFolderOf(string folderName, string packageName, string version, bool expected)
    {
        Assert.Equal(expected, NuGetResolverCache.IsFolderOf(folderName, packageName, version));
    }

    // Only a local build has its restore deleted with it: a release keeps its dependencies
    [Theory]
    [InlineData("4.5.0-dev", true)]
    [InlineData("4.5.0-dev3", true)]
    [InlineData("4.5.0-beta8-dev3", true)]
    [InlineData("4.4.0.1", false)]
    [InlineData("4.5.0-beta8", false)]
    [InlineData("4.5.0-device", false)]
    public void IsDevVersion(string version, bool expected)
    {
        Assert.Equal(expected, NugetStore.IsDevVersion(new PackageVersion(version)));
    }

    [Fact]
    public void Delete_RemovesTheFoldersOfTheVersionOnly()
    {
        string[] removed = ["Stride.GameStudio-4.5.0-dev-net10.0-windows7.0-win-x64", "Stride.GameStudio-4.5.0-dev-net10.0-linux-x64"];
        string[] kept = ["Stride.GameStudio-4.4.0.1-net10.0-windows7.0-win-x64", "Stride.GameStudio-4.5.0-dev2-net10.0-win-x64", "Stride.Core-4.5.0-dev-net10.0-win-x64", "Avalonia.Desktop-11.3.0-net10.0-win-x64"];
        foreach (var folder in removed.Concat(kept))
        {
            Directory.CreateDirectory(Path.Combine(root, folder));
            File.WriteAllText(Path.Combine(root, folder, "project.assets.json"), "{}");
        }

        Assert.Equal(2, NuGetResolverCache.Delete("Stride.GameStudio", "4.5.0-dev", root));

        Assert.Equal(kept.Order(), Directory.EnumerateDirectories(root).Select(Path.GetFileName).Order());
    }

    // From the PackageVersion the store uninstalls to the folder the resolver restored into, named from NuGetVersion
    [Theory]
    [InlineData("4.5.0-dev")]
    [InlineData("4.5.0-beta8-dev3")]
    [InlineData("4.5.0.1-dev")]
    public void DeleteResolverRestore_DeletesTheFoldersTheResolverMade(string version)
    {
        var folder = NuGetResolverCache.GetFolder("Stride.GameStudio", new NuGetVersion(version).ToString(), "net10.0-windows7.0", "win-x64");
        var restored = Path.Combine(root, Path.GetFileName(folder));
        Directory.CreateDirectory(restored);

        Assert.Equal(1, NugetStore.DeleteResolverRestore("Stride.GameStudio", new PackageVersion(version), root));
        Assert.False(Directory.Exists(restored));
    }

    [Fact]
    public void DeleteResolverRestore_LeavesReleases()
    {
        var restored = Path.Combine(root, "Stride.GameStudio-4.4.0.1-net10.0-windows7.0-win-x64");
        Directory.CreateDirectory(restored);

        Assert.Equal(0, NugetStore.DeleteResolverRestore("Stride.GameStudio", new PackageVersion("4.4.0.1"), root));
        Assert.True(Directory.Exists(restored));
    }

    [Fact]
    public void Delete_WithoutCache()
    {
        Assert.Equal(0, NuGetResolverCache.Delete("Stride.GameStudio", "4.5.0-dev", root));
    }

    [Fact]
    public void GetFolder_IsUnderRoot()
    {
        var folder = NuGetResolverCache.GetFolder("Stride.GameStudio", "4.5.0-dev", "net10.0-windows7.0", "win-x64");

        Assert.Equal(NuGetResolverCache.Root, Path.GetDirectoryName(folder));
        Assert.True(NuGetResolverCache.IsFolderOf(Path.GetFileName(folder), "Stride.GameStudio", "4.5.0-dev"));
    }
}
