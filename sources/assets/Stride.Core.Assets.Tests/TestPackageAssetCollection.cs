// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestPackageAssetCollection
{
    [Fact]
    public void TestConstructor()
    {
        var package = new Package();
        var collection = new PackageAssetCollection(package);

        Assert.NotNull(collection);
        Assert.Same(package, collection.Package);
        Assert.Empty(collection);
    }

    [Fact]
    public void TestAdd()
    {
        var package = new Package();
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem("Assets/Test.sdtest", asset);

        package.Assets.Add(assetItem);

        Assert.Single(package.Assets);
        Assert.Contains(assetItem, package.Assets);
    }

    [Fact]
    public void TestRemove()
    {
        var package = new Package();
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem("Assets/Test.sdtest", asset);
        package.Assets.Add(assetItem);

        var removed = package.Assets.Remove(assetItem);

        Assert.True(removed);
        Assert.Empty(package.Assets);
    }

    [Fact]
    public void TestFindById()
    {
        var package = new Package();
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem("Assets/Test.sdtest", asset);
        package.Assets.Add(assetItem);

        var found = package.Assets.Find(assetItem.Id);

        Assert.NotNull(found);
        Assert.Same(assetItem, found);
    }

    [Fact]
    public void TestFindByLocation()
    {
        var package = new Package();
        var asset = new AssetObjectTest { Name = "Test" };
        var location = "Assets/Test.sdtest";
        var assetItem = new AssetItem(location, asset);
        package.Assets.Add(assetItem);

        var found = package.Assets.Find(location);

        Assert.NotNull(found);
        Assert.Same(assetItem, found);
    }

    [Fact]
    public void TestFindNotFound()
    {
        var package = new Package();

        var found = package.Assets.Find("NonExistent.sdtest");

        Assert.Null(found);
    }

    [Fact]
    public void TestContainsById()
    {
        var package = new Package();
        var asset = new AssetObjectTest { Name = "Test" };
        var assetItem = new AssetItem("Assets/Test.sdtest", asset);
        package.Assets.Add(assetItem);

        Assert.True(package.Assets.ContainsById(assetItem.Id));
        Assert.False(package.Assets.ContainsById(AssetId.New()));
    }

    [Fact]
    public void TestClear()
    {
        var package = new Package();
        var asset1 = new AssetObjectTest { Name = "Test1" };
        var asset2 = new AssetObjectTest { Name = "Test2" };
        package.Assets.Add(new AssetItem("Assets/Test1.sdtest", asset1));
        package.Assets.Add(new AssetItem("Assets/Test2.sdtest", asset2));

        package.Assets.Clear();

        Assert.Empty(package.Assets);
    }

    [Fact]
    public void TestCount()
    {
        var package = new Package();

        Assert.Empty(package.Assets);

        var item1 = new AssetItem("Assets/Test1.sdtest", new AssetObjectTest { Name = "Test1" });
        package.Assets.Add(item1);
        Assert.Single(package.Assets);

        var item2 = new AssetItem("Assets/Test2.sdtest", new AssetObjectTest { Name = "Test2" });
        package.Assets.Add(item2);
        Assert.Equal(2, package.Assets.Count);

        // Enumeration yields all added items
        var items = package.Assets.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(item1, items);
        Assert.Contains(item2, items);
    }

    [Fact]
    public void TestIsDirty()
    {
        var package = new Package();

        Assert.False(package.Assets.IsDirty);

        package.Assets.IsDirty = true;
        Assert.True(package.Assets.IsDirty);
    }
}
