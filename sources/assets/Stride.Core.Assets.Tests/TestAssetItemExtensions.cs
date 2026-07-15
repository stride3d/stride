// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestAssetItemExtensions
{
    [Fact]
    public void TestGetProjectIncludeWithPackage()
    {
        var package = new Package { FullPath = new UFile("/Projects/MyPackage/MyPackage.sdpkg") };
        var solutionProject = new SolutionProject(package, Guid.NewGuid(), "/Projects/MyPackage/MyPackage.csproj");
        package.Container = solutionProject;

        var asset = new TestAssetSimple();
        var assetItem = new AssetItem("/Projects/MyPackage/Assets/MyAsset", asset)
        {
            Package = package
        };

        var include = assetItem.GetProjectInclude();

        Assert.NotNull(include);
        Assert.Contains("Assets", include);
    }

    [Fact]
    public void TestGetGeneratedAbsolutePath()
    {
        var asset = new TestAssetSimple();
        var assetItem = new AssetItem("/Projects/MyPackage/Assets/MyAsset", asset);

        var generatedPath = assetItem.GetGeneratedAbsolutePath();

        Assert.NotNull(generatedPath);
        Assert.EndsWith(".cs", generatedPath.ToString());
        Assert.Equal("/Projects/MyPackage/Assets/MyAsset.cs", generatedPath.ToString());
    }

    [Fact]
    public void TestGetGeneratedInclude()
    {
        var package = new Package { FullPath = new UFile("/Projects/MyPackage/MyPackage.sdpkg") };
        var solutionProject = new SolutionProject(package, Guid.NewGuid(), "/Projects/MyPackage/MyPackage.csproj");
        package.Container = solutionProject;

        var asset = new TestAssetSimple();
        var assetItem = new AssetItem("/Projects/MyPackage/Assets/MyAsset", asset)
        {
            Package = package
        };

        var generatedInclude = assetItem.GetGeneratedInclude();

        Assert.NotNull(generatedInclude);
        Assert.EndsWith(".cs", generatedInclude);
        Assert.Contains("Assets", generatedInclude);
    }

    [Fact]
    public void TestGetProjectIncludeWithNullPackage()
    {
        var asset = new TestAssetSimple();
        var assetItem = new AssetItem("Assets/MyAsset", asset);

        // GetProjectInclude throws NullReferenceException when Package is null
        Assert.Throws<NullReferenceException>(() => assetItem.GetProjectInclude());
    }

    [Fact]
    public void TestGetGeneratedAbsolutePathWithNullPackage()
    {
        var asset = new TestAssetSimple();
        var assetItem = new AssetItem("/Test/Assets/MyAsset", asset);

        // GetGeneratedAbsolutePath works even without a package
        var generatedPath = assetItem.GetGeneratedAbsolutePath();
        Assert.Equal("/Test/Assets/MyAsset.cs", generatedPath.ToString());
    }

    [Fact]
    public void TestGetGeneratedIncludeWithNullPackage()
    {
        var asset = new TestAssetSimple();
        var assetItem = new AssetItem("Assets/MyAsset", asset);

        // GetGeneratedInclude calls GetProjectInclude which throws NullReferenceException
        Assert.Throws<NullReferenceException>(() => assetItem.GetGeneratedInclude());
    }
}

// Simple test asset class
public class TestAssetSimple : Asset
{
}
