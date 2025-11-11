// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Design.Tests;

/// <summary>
/// Tests for the <see cref="AbsoluteId"/> struct.
/// </summary>
public class TestAbsoluteId
{
    [Fact]
    public void Constructor_WithValidIds_ShouldInitializeCorrectly()
    {
        var assetId = AssetId.New();
        var objectId = Guid.NewGuid();

        var absoluteId = new AbsoluteId(assetId, objectId);

        Assert.Equal(assetId, absoluteId.AssetId);
        Assert.Equal(objectId, absoluteId.ObjectId);
    }

    [Fact]
    public void Constructor_WithEmptyAssetIdAndValidObjectId_ShouldSucceed()
    {
        var objectId = Guid.NewGuid();

        var absoluteId = new AbsoluteId(AssetId.Empty, objectId);

        Assert.Equal(AssetId.Empty, absoluteId.AssetId);
        Assert.Equal(objectId, absoluteId.ObjectId);
    }

    [Fact]
    public void Constructor_WithValidAssetIdAndEmptyObjectId_ShouldSucceed()
    {
        var assetId = AssetId.New();

        var absoluteId = new AbsoluteId(assetId, Guid.Empty);

        Assert.Equal(assetId, absoluteId.AssetId);
        Assert.Equal(Guid.Empty, absoluteId.ObjectId);
    }

    [Fact]
    public void Constructor_WithBothEmptyIds_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AbsoluteId(AssetId.Empty, Guid.Empty));
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        var assetId = AssetId.New();
        var objectId = Guid.NewGuid();

        var id1 = new AbsoluteId(assetId, objectId);
        var id2 = new AbsoluteId(assetId, objectId);

        Assert.True(id1.Equals(id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Equals_WithDifferentAssetId_ShouldReturnFalse()
    {
        var assetId1 = AssetId.New();
        var assetId2 = AssetId.New();
        var objectId = Guid.NewGuid();

        var id1 = new AbsoluteId(assetId1, objectId);
        var id2 = new AbsoluteId(assetId2, objectId);

        Assert.False(id1.Equals(id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void Equals_WithDifferentObjectId_ShouldReturnFalse()
    {
        var assetId = AssetId.New();
        var objectId1 = Guid.NewGuid();
        var objectId2 = Guid.NewGuid();

        var id1 = new AbsoluteId(assetId, objectId1);
        var id2 = new AbsoluteId(assetId, objectId2);

        Assert.False(id1.Equals(id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var id = new AbsoluteId(AssetId.New(), Guid.NewGuid());
        Assert.False(id.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        var id = new AbsoluteId(AssetId.New(), Guid.NewGuid());
        Assert.False(id.Equals("not an AbsoluteId"));
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var assetId = AssetId.New();
        var objectId = Guid.NewGuid();

        var id1 = new AbsoluteId(assetId, objectId);
        var id2 = new AbsoluteId(assetId, objectId);

        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var assetId = new AssetId(new Guid("12345678-1234-1234-1234-123456789abc"));
        var objectId = new Guid("87654321-4321-4321-4321-cba987654321");

        var id = new AbsoluteId(assetId, objectId);
        var result = id.ToString();

        Assert.Contains("12345678-1234-1234-1234-123456789abc", result);
        Assert.Contains("87654321-4321-4321-4321-cba987654321", result);
    }
}
