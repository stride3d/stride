// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestBasePart
{
    [Fact]
    public void TestConstructor()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart = new BasePart(assetRef, basePartId, instanceId);

        Assert.Equal(assetRef, basePart.BasePartAsset);
        Assert.Equal(basePartId, basePart.BasePartId);
        Assert.Equal(instanceId, basePart.InstanceId);
    }

    [Fact]
    public void TestConstructorWithNullAssetRefThrows()
    {
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        Assert.Throws<ArgumentNullException>(() => new BasePart(null!, basePartId, instanceId));
    }

    [Fact]
    public void TestConstructorWithEmptyBasePartIdThrows()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var instanceId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new BasePart(assetRef, Guid.Empty, instanceId));
    }

    [Fact]
    public void TestConstructorWithEmptyInstanceIdThrows()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var basePartId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new BasePart(assetRef, basePartId, Guid.Empty));
    }

    [Fact]
    public void TestEquality()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart1 = new BasePart(assetRef, basePartId, instanceId);
        var basePart2 = new BasePart(assetRef, basePartId, instanceId);

        Assert.True(basePart1.Equals(basePart2));
        Assert.True(basePart1 == basePart2);
    }

    [Fact]
    public void TestInequality()
    {
        var assetRef1 = new AssetReference(AssetId.New(), "Assets/Test1.sdobj");
        var assetRef2 = new AssetReference(AssetId.New(), "Assets/Test2.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart1 = new BasePart(assetRef1, basePartId, instanceId);
        var basePart2 = new BasePart(assetRef2, basePartId, instanceId);

        Assert.False(basePart1.Equals(basePart2));
        Assert.True(basePart1 != basePart2);
    }

    [Fact]
    public void TestGetHashCode()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart = new BasePart(assetRef, basePartId, instanceId);

        // GetHashCode should not throw and should be consistent
        var hash1 = basePart.GetHashCode();
        var hash2 = basePart.GetHashCode();
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TestEqualityWithNull()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart = new BasePart(assetRef, basePartId, instanceId);

        Assert.False(basePart.Equals(null));
        Assert.True(basePart != null);
        Assert.True(null != basePart);
    }

    [Fact]
    public void TestEqualityWithSameReference()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Test.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart = new BasePart(assetRef, basePartId, instanceId);

        Assert.True(basePart.Equals(basePart));
#pragma warning disable CS1718
        Assert.True(basePart == basePart);
#pragma warning restore CS1718
    }

    [Fact]
    public void TestBasePartAssetCanBeUpdated()
    {
        var assetRef1 = new AssetReference(AssetId.New(), "Assets/Test1.sdobj");
        var assetRef2 = new AssetReference(AssetId.New(), "Assets/Test2.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();

        var basePart = new BasePart(assetRef1, basePartId, instanceId);
        Assert.Equal(assetRef1, basePart.BasePartAsset);

        basePart.BasePartAsset = assetRef2;
        Assert.Equal(assetRef2, basePart.BasePartAsset);
    }
}
