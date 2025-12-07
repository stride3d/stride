// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestAsset
{
    [Fact]
    public void TestNewAssetGeneratesId()
    {
        var asset = new AssetObjectTest { Name = "Test" };

        Assert.NotEqual(AssetId.Empty, asset.Id);
    }

    [Fact]
    public void TestAssetTags()
    {
        var asset = new AssetObjectTest { Name = "Test" };

        Assert.NotNull(asset.Tags);
        Assert.Empty(asset.Tags);
    }

    [Fact]
    public void TestAddTag()
    {
        var asset = new AssetObjectTest { Name = "Test" };
        asset.Tags.Add("MyTag");

        Assert.Single(asset.Tags);
        Assert.Contains("MyTag", asset.Tags);
    }

    [Fact]
    public void TestArchetype()
    {
        var asset = new AssetObjectTest { Name = "Test" };
        var archetypeRef = new AssetReference(AssetId.New(), "Archetypes/Archetype.sdtest");

        asset.Archetype = archetypeRef;

        Assert.Equal(archetypeRef, asset.Archetype);
    }

    [Fact]
    public void TestAssetIdSetter()
    {
        var asset = new AssetObjectTest { Name = "Test" };
        var newId = AssetId.New();

        asset.Id = newId;

        Assert.Equal(newId, asset.Id);
    }

    [Fact]
    public void TestCreateChildAsset()
    {
        var parentAsset = new TestAssetWithParts { Name = "Parent" };
        var baseLocation = "BasePackage/ParentAsset.sdpart";

        var childAsset = (TestAssetWithParts)parentAsset.CreateDerivedAsset(baseLocation, out var idRemapping);

        Assert.NotNull(childAsset);
        Assert.NotEqual(parentAsset.Id, childAsset.Id);
        Assert.NotNull(idRemapping);
    }

    [Fact]
    public void TestAssetSerializedVersionCanBeSet()
    {
        var asset = new AssetObjectTest { Name = "Test" };

        // SerializedVersion is null by default
        Assert.Null(asset.SerializedVersion);

        // Can be set
        var version = new Dictionary<string, PackageVersion> { { "TestPackage", new PackageVersion("1.0.0") } };
        asset.SerializedVersion = version;
        Assert.NotNull(asset.SerializedVersion);
        Assert.Single(asset.SerializedVersion);
    }
}
