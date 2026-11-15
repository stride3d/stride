// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestLoadProjectAsset : TestBase
{
    [DataContract("TestProjectCodeAsset")]
    [AssetDescription(".sdprojtest")]
    public class TestProjectCodeAsset : ProjectSourceCodeAsset;

    [DataContract("OtherTestProjectCodeAsset")]
    [AssetDescription(".sdprojtestother")]
    public class OtherTestProjectCodeAsset : ProjectSourceCodeAsset;

    private (Package Package, string Directory) CreateProject(string testName, string? assetNamespace)
    {
        var directory = Path.Combine(DirectoryTestBase, nameof(TestLoadProjectAsset), testName);
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        Directory.CreateDirectory(Path.Combine(directory, "Effects"));

        var package = new Package { FullPath = Path.Combine(directory, "MyGame.sdpkg") };
        package.Meta.Name = "MyGame";
        _ = new SolutionProject(package, Guid.NewGuid(), Path.Combine(directory, "MyGame.csproj")) { AssetNamespace = assetNamespace };
        return (package, directory);
    }

    private static string WriteFile(string directory, string relativePath)
    {
        var path = Path.Combine(directory, relativePath);
        File.WriteAllText(path, "shader Foo {}");
        return path;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("MyGame")]
    public void TheAssetIsTheSameAsWhenTheProjectOpens(string? assetNamespace)
    {
        var (package, directory) = CreateProject(nameof(TheAssetIsTheSameAsWhenTheProjectOpens) + assetNamespace, assetNamespace);
        var file = new UFile(WriteFile(directory, "Effects/Foo.sdprojtest"));

        var log = new LoggerResult();
        package.LoadTemporaryAssets(log, [new PackageLoadingAssetFile(file, new UDirectory(directory))]);
        var atOpen = Assert.Single(package.TemporaryAssets);

        var loaded = package.LoadProjectAsset(log, file, null, new UDirectory(directory));

        Assert.False(log.HasErrors);
        Assert.NotNull(loaded);
        Assert.IsType<TestProjectCodeAsset>(loaded.Asset);
        Assert.Equal(atOpen.Id, loaded.Id);
        Assert.Equal(atOpen.Location, loaded.Location);
        Assert.Equal(atOpen.SourceFolder, loaded.SourceFolder);
        Assert.Equal(atOpen.AlternativePath, loaded.AlternativePath);
        Assert.Equal(file, loaded.FullPath);
        Assert.Equal(loaded.Location, package.GetProjectAssetLocation(file, null, new UDirectory(directory)));
    }

    [Fact]
    public void TheAssetIsNotDirtySoASaveDoesNotWriteIt()
    {
        var (package, directory) = CreateProject(nameof(TheAssetIsNotDirtySoASaveDoesNotWriteIt), null);
        var file = new UFile(WriteFile(directory, "Effects/Foo.sdprojtest"));

        var loaded = package.LoadProjectAsset(new LoggerResult(), file, null, new UDirectory(directory));

        Assert.NotNull(loaded);
        Assert.False(loaded.IsDirty);
        Assert.Equal(File.GetLastWriteTime(file.ToOSPath()), loaded.ModifiedTime);
        Assert.Equal("shader Foo {}", ((SourceCodeAsset)loaded.Asset).Text);
    }

    [Fact]
    public void ALinkedFileTakesItsLocationFromTheLink()
    {
        var (package, directory) = CreateProject(nameof(ALinkedFileTakesItsLocationFromTheLink), null);
        var outside = Path.Combine(directory, "..", nameof(ALinkedFileTakesItsLocationFromTheLink) + "-outside");
        Directory.CreateDirectory(outside);
        var file = new UFile(Path.GetFullPath(WriteFile(outside, "Foo.sdprojtest")));

        var loaded = package.LoadProjectAsset(new LoggerResult(), file, new UFile(Path.Combine(directory, "Effects", "Linked.sdprojtest")), new UDirectory(directory));

        Assert.NotNull(loaded);
        Assert.Equal(new UFile("Effects/Linked"), loaded.Location);
        Assert.Equal(file, loaded.AlternativePath);
    }

    [Fact]
    public void TheSameFileIsSkippedInSilence()
    {
        var (package, directory) = CreateProject(nameof(TheSameFileIsSkippedInSilence), null);
        var file = new UFile(WriteFile(directory, "Effects/Foo.sdprojtest"));
        var log = new LoggerResult();
        package.Assets.Add(package.LoadProjectAsset(log, file, null, new UDirectory(directory))!);

        Assert.Null(package.LoadProjectAsset(log, file, null, new UDirectory(directory)));
        Assert.Empty(log.Messages);
    }

    [Fact]
    public void AnotherFileWithTheSameLocationIsSkippedWithAWarning()
    {
        var (package, directory) = CreateProject(nameof(AnotherFileWithTheSameLocationIsSkippedWithAWarning), null);
        var log = new LoggerResult();
        var first = package.LoadProjectAsset(log, new UFile(WriteFile(directory, "Effects/Foo.sdprojtest")), null, new UDirectory(directory));
        package.Assets.Add(first!);

        var second = package.LoadProjectAsset(log, new UFile(WriteFile(directory, "Effects/Foo.sdprojtestother")), null, new UDirectory(directory));

        Assert.Null(second);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("Foo.sdprojtestother"));
        Assert.Single(package.Assets);
    }
}
