// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

#pragma warning disable CS0618 // AssetPart is obsolete

public class TestAssetPart
{
    [Fact]
    public void TestConstructor()
    {
        var partId = Guid.NewGuid();
        var basePart = new BasePart(new AssetReference(AssetId.New(), "Assets/Base.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        Action<BasePart> updater = _ => { };

        var assetPart = new AssetPart(partId, basePart, updater);

        Assert.Equal(partId, assetPart.PartId);
        Assert.Equal(basePart, assetPart.Base);
    }

    [Fact]
    public void TestConstructorWithEmptyPartIdThrows()
    {
        var basePart = new BasePart(new AssetReference(AssetId.New(), "Assets/Base.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        Action<BasePart> updater = _ => { };

        Assert.Throws<ArgumentException>(() => new AssetPart(Guid.Empty, basePart, updater));
    }

    [Fact]
    public void TestConstructorWithNullUpdaterThrows()
    {
        var partId = Guid.NewGuid();
        var basePart = new BasePart(new AssetReference(AssetId.New(), "Assets/Base.sdobj"), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => new AssetPart(partId, basePart, null!));
    }

    [Fact]
    public void TestConstructorWithNullBasePart()
    {
        var partId = Guid.NewGuid();
        Action<BasePart> updater = _ => { };

        var assetPart = new AssetPart(partId, null, updater);

        Assert.Equal(partId, assetPart.PartId);
        Assert.Null(assetPart.Base);
    }

    [Fact]
    public void TestUpdateBase()
    {
        var partId = Guid.NewGuid();
        var oldBase = new BasePart(new AssetReference(AssetId.New(), "Assets/Old.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        var newBase = new BasePart(new AssetReference(AssetId.New(), "Assets/New.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        BasePart? capturedBase = null;
        Action<BasePart> updater = b => capturedBase = b;

        var assetPart = new AssetPart(partId, oldBase, updater);
        assetPart.UpdateBase(newBase);

        Assert.Equal(newBase, capturedBase);
    }

    [Fact]
    public void TestEqualityWithSameValues()
    {
        var partId = Guid.NewGuid();
        var assetRef = new AssetReference(AssetId.New(), "Assets/Base.sdobj");
        var basePartId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var basePart = new BasePart(assetRef, basePartId, instanceId);
        Action<BasePart> updater = _ => { };

        var assetPart1 = new AssetPart(partId, basePart, updater);
        var assetPart2 = new AssetPart(partId, basePart, updater);

        Assert.Equal(assetPart1, assetPart2);
        Assert.True(assetPart1 == assetPart2);
    }

    [Fact]
    public void TestInequalityWithDifferentPartId()
    {
        var assetRef = new AssetReference(AssetId.New(), "Assets/Base.sdobj");
        var basePart = new BasePart(assetRef, Guid.NewGuid(), Guid.NewGuid());
        Action<BasePart> updater = _ => { };

        var assetPart1 = new AssetPart(Guid.NewGuid(), basePart, updater);
        var assetPart2 = new AssetPart(Guid.NewGuid(), basePart, updater);

        Assert.NotEqual(assetPart1, assetPart2);
        Assert.True(assetPart1 != assetPart2);
    }

    [Fact]
    public void TestInequalityWithDifferentBasePart()
    {
        var partId = Guid.NewGuid();
        var basePart1 = new BasePart(new AssetReference(AssetId.New(), "Assets/Base1.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        var basePart2 = new BasePart(new AssetReference(AssetId.New(), "Assets/Base2.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        Action<BasePart> updater = _ => { };

        var assetPart1 = new AssetPart(partId, basePart1, updater);
        var assetPart2 = new AssetPart(partId, basePart2, updater);

        Assert.NotEqual(assetPart1, assetPart2);
    }

    [Fact]
    public void TestEqualityWithBothNullBase()
    {
        var partId = Guid.NewGuid();
        Action<BasePart> updater = _ => { };

        var assetPart1 = new AssetPart(partId, null, updater);
        var assetPart2 = new AssetPart(partId, null, updater);

        Assert.Equal(assetPart1, assetPart2);
    }

    [Fact]
    public void TestGetHashCode()
    {
        var partId = Guid.NewGuid();
        var basePart = new BasePart(new AssetReference(AssetId.New(), "Assets/Base.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        Action<BasePart> updater = _ => { };

        var assetPart = new AssetPart(partId, basePart, updater);

        // GetHashCode should not throw and should be consistent
        var hash1 = assetPart.GetHashCode();
        var hash2 = assetPart.GetHashCode();
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TestEqualsWithObject()
    {
        var partId = Guid.NewGuid();
        var basePart = new BasePart(new AssetReference(AssetId.New(), "Assets/Base.sdobj"), Guid.NewGuid(), Guid.NewGuid());
        Action<BasePart> updater = _ => { };

        var assetPart1 = new AssetPart(partId, basePart, updater);
        object assetPart2 = new AssetPart(partId, basePart, updater);

        Assert.True(assetPart1.Equals(assetPart2));
    }

    [Fact]
    public void TestEqualsWithNull()
    {
        var partId = Guid.NewGuid();
        Action<BasePart> updater = _ => { };
        var assetPart = new AssetPart(partId, null, updater);

        Assert.False(assetPart.Equals(null));
    }
}

#pragma warning restore CS0618
