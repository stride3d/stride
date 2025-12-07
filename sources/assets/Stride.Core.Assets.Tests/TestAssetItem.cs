// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestAssetItem
{
    [Fact]
    public void TestConstructor()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };

        var assetItem = new AssetItem(location, asset);

        Assert.Equal(location, assetItem.Location);
        Assert.Same(asset, assetItem.Asset);
        Assert.Equal(asset.Id, assetItem.Id);
    }

    [Fact]
    public void TestConstructorWithPackage()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };
        var package = new Package();

        var assetItem = new AssetItem(location, asset, package);

        Assert.Equal(location, assetItem.Location);
        Assert.Same(asset, assetItem.Asset);
        Assert.Same(package, assetItem.Package);
        Assert.Equal(asset.Id, assetItem.Id);
    }

    [Fact]
    public void TestSourceFolder()
    {
        var location = "Assets/Subfolder/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };

        var assetItem = new AssetItem(location, asset);
        assetItem.SourceFolder = "Assets/Subfolder";

        Assert.Equal("Assets/Subfolder", assetItem.SourceFolder);
    }

    [Fact]
    public void TestSourceFolderCanBeSet()
    {
        var location = "MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };

        var assetItem = new AssetItem(location, asset);
        Assert.Null(assetItem.SourceFolder);

        assetItem.SourceFolder = "Assets";
        Assert.Equal("Assets", assetItem.SourceFolder);
    }

    [Fact]
    public void TestFullPath()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };
        var package = new Package { FullPath = new UFile(@"C:\Projects\Package.sdpkg") };

        var assetItem = new AssetItem(location, asset, package);

        Assert.NotNull(assetItem.FullPath);
        Assert.Contains("MyAsset.sdtest", assetItem.FullPath.ToString());
    }

    [Fact]
    public void TestFullPathWithNullPackage()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };

        var assetItem = new AssetItem(location, asset);

        // FullPath returns the location when package is null
        Assert.NotNull(assetItem.FullPath);
        Assert.Contains("MyAsset.sdtest", assetItem.FullPath.ToString());
    }

    [Fact]
    public void TestAlternativePath()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };
        var alternativePath = new UFile("Alternative/Path/MyAsset.sdtest");

        var assetItem = new AssetItem(location, asset) { AlternativePath = alternativePath };

        Assert.Equal(alternativePath, assetItem.AlternativePath);
    }

    [Fact]
    public void TestIsDirty()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };

        var assetItem = new AssetItem(location, asset);

        // AssetItem starts as dirty
        Assert.True(assetItem.IsDirty);

        assetItem.IsDirty = true;
        Assert.True(assetItem.IsDirty);
    }

    [Fact]
    public void TestIsDeleted()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };

        var assetItem = new AssetItem(location, asset);

        Assert.False(assetItem.IsDeleted);

        assetItem.IsDeleted = true;
        Assert.True(assetItem.IsDeleted);
    }

    [Fact]
    public void TestClone()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem(location, asset);

        var clonedItem = assetItem.Clone(false);

        Assert.NotSame(assetItem, clonedItem);
        Assert.Equal(assetItem.Location, clonedItem.Location);
        Assert.NotSame(assetItem.Asset, clonedItem.Asset);
        Assert.Equal(assetItem.Id, clonedItem.Id);
    }

    [Fact]
    public void TestCloneWithGeneratedId()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem(location, asset);
        var originalId = assetItem.Id;

        // Clone doesn't change the asset ID, need to pass a new asset with a new ID
        var newAsset = new AssetObjectTest { Name = "Test" };
        var clonedItem = assetItem.Clone(newLocation: null, newAsset: newAsset);

        Assert.NotSame(assetItem, clonedItem);
        Assert.Equal(assetItem.Location, clonedItem.Location);
        Assert.NotSame(assetItem.Asset, clonedItem.Asset);
        Assert.NotEqual(originalId, clonedItem.Id);
    }

    [Fact]
    public void TestToReference()
    {
        var location = "Assets/MyAsset.sdtest";
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem(location, asset);

        var reference = assetItem.ToReference();

        Assert.Equal(assetItem.Id, reference.Id);
        Assert.Equal(assetItem.Location, reference.Location);
    }
}
