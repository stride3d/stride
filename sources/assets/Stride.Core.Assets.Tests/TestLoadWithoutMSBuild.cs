// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// <see cref="PackageSession.LoadWithoutMSBuild"/> gives the session that <see cref="PackageSession.Load(string, PackageSessionResult, PackageLoadParameters?)"/>
/// gives for a project with no reference that holds no asset types, as the projects that the editor tests write.
/// </summary>
public class TestLoadWithoutMSBuild : TestBase
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(PlatformType.Windows, false)]
    [InlineData(null, true)]
    public void TheSessionIsTheOneTheProjectLoadGives(PlatformType? executablePlatform, bool withProjectAsset)
    {
        var directory = Path.Combine(DirectoryTestBase, nameof(TestLoadWithoutMSBuild), $"{executablePlatform}{withProjectAsset}");
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);

        var executable = executablePlatform is { } platform
            ? $"""
                  <OutputType>Exe</OutputType>
                  <StridePlatform>{platform}</StridePlatform>

              """
            : string.Empty;
        File.WriteAllText(Path.Combine(directory, "MyGame.csproj"),
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
            {executable}    <StrideContainsAssetTypes>false</StrideContainsAssetTypes>
              </PropertyGroup>
            </Project>
            """);
        var packagePath = Path.Combine(directory, "MyGame.sdpkg");
        var package = new Package
        {
            FullPath = packagePath,
            Meta = { Name = "MyGame", Version = new PackageVersion("1.0.0") },
            AssetFolders = { new AssetFolder("Assets") },
        };
        AssetFileSerializer.Save(packagePath, package, null);
        var asset = new AssetObjectTest { Name = "Ground" };
        var assetPath = Path.Combine(directory, "Assets", "Sub", "Ground.sdtest");
        AssetFileSerializer.Save(assetPath, asset, null);

        // A project asset, as a script: the project lists it with its other files, the asset file included
        List<UFile> projectFiles = [new UFile(assetPath)];
        if (withProjectAsset)
        {
            Directory.CreateDirectory(Path.Combine(directory, "Effects"));
            File.WriteAllText(Path.Combine(directory, "Effects", "Foo.sdprojtest"), "shader Foo {}");
            projectFiles.Add(new UFile("Effects/Foo.sdprojtest"));
        }

        var loadResult = new PackageSessionResult();
        PackageSession.Load(packagePath, loadResult);
        var withoutMSBuildResult = new PackageSessionResult();
        PackageSession.LoadWithoutMSBuild(packagePath, executablePlatform is null ? ProjectType.Library : ProjectType.Executable, withoutMSBuildResult,
            platform: executablePlatform ?? PlatformType.Shared, projectFiles: projectFiles);

        using var loaded = loadResult.Session;
        using var withoutMSBuild = withoutMSBuildResult.Session;
        Assert.False(loadResult.HasErrors, string.Join(Environment.NewLine, loadResult.Messages));
        Assert.False(withoutMSBuildResult.HasErrors, string.Join(Environment.NewLine, withoutMSBuildResult.Messages));

        var expected = Assert.Single(loaded.LocalPackages);
        var actual = Assert.Single(withoutMSBuild.LocalPackages);
        var expectedProject = Assert.IsType<SolutionProject>(expected.Container);
        var actualProject = Assert.IsType<SolutionProject>(actual.Container);
        Assert.Equal(expectedProject.FullPath, actualProject.FullPath);
        Assert.Equal(expectedProject.Type, actualProject.Type);
        Assert.Equal(expectedProject.Platform, actualProject.Platform);
        Assert.Equal(executablePlatform ?? PlatformType.Shared, actualProject.Platform);
        Assert.Equal(expectedProject.ContainsAssetTypes, actualProject.ContainsAssetTypes);
        Assert.Equal(expectedProject.AssetNamespace, actualProject.AssetNamespace);
        Assert.Same(expectedProject, loaded.CurrentProject);
        Assert.Same(actualProject, withoutMSBuild.CurrentProject);
        Assert.Equal(expected.Meta.Name, actual.Meta.Name);
        Assert.Equal(expected.Meta.Version, actual.Meta.Version);
        Assert.Equal(expected.RootNamespace, actual.RootNamespace);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(PackageState.AssetsReady, actual.State);

        var expectedAssets = expected.Assets.OrderBy(x => x.Location.FullPath).ToList();
        var actualAssets = actual.Assets.OrderBy(x => x.Location.FullPath).ToList();
        Assert.Equal(withProjectAsset ? 2 : 1, actualAssets.Count);
        Assert.Equal(expectedAssets.Count, actualAssets.Count);
        for (var i = 0; i < expectedAssets.Count; i++)
        {
            Assert.Equal(expectedAssets[i].Id, actualAssets[i].Id);
            Assert.Equal(expectedAssets[i].Location, actualAssets[i].Location);
            Assert.Equal(expectedAssets[i].FullPath, actualAssets[i].FullPath);
            Assert.Equal(expectedAssets[i].SourceFolder, actualAssets[i].SourceFolder);
            Assert.Equal(expectedAssets[i].AlternativePath, actualAssets[i].AlternativePath);
            Assert.Equal(expectedAssets[i].Asset.GetType(), actualAssets[i].Asset.GetType());
            Assert.False(actualAssets[i].IsDirty);
        }
        Assert.Equal("Ground", Assert.Single(actualAssets.Select(x => x.Asset).OfType<AssetObjectTest>()).Name);
        Assert.Equal(asset.Id, actualAssets.Single(x => x.Asset is AssetObjectTest).Id);
        if (withProjectAsset)
            Assert.Equal("shader Foo {}", Assert.Single(actualAssets.Select(x => x.Asset).OfType<TestLoadProjectAsset.TestProjectCodeAsset>()).Text);

        Assert.False(withoutMSBuild.IsDirty);
        Assert.False(actual.IsDirty);
        Assert.False(actual.Assets.IsDirty);
    }
}
