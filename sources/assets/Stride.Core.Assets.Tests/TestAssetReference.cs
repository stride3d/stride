// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestAssetReference
{
    [Fact]
    public void TestConstructor()
    {
        var id = AssetId.New();
        var location = new UFile("Assets/MyAsset.sdtest");
        var assetRef = new AssetReference(id, location);

        Assert.Equal(id, assetRef.Id);
        Assert.Equal(location, assetRef.Location);
    }

    [Fact]
    public void TestEquality()
    {
        var id = AssetId.New();
        var location = new UFile("Assets/MyAsset.sdtest");
        var ref1 = new AssetReference(id, location);
        var ref2 = new AssetReference(id, location);
        var ref3 = new AssetReference(AssetId.New(), location);

        Assert.Equal(ref1, ref2);
        Assert.True(ref1 == ref2);
        Assert.False(ref1 != ref2);
        Assert.NotEqual(ref1, ref3);
        Assert.True(ref1 != ref3);
        Assert.False(ref1 == ref3);
    }

    [Fact]
    public void TestEqualityWithNull()
    {
        var id = AssetId.New();
        var location = new UFile("Assets/MyAsset.sdtest");
        var ref1 = new AssetReference(id, location);
        AssetReference? ref2 = null;

        Assert.NotEqual(ref1, ref2);
        Assert.False(ref1 == ref2);
        Assert.True(ref1 != ref2);
        Assert.False(ref1!.Equals(ref2));
    }

    [Fact]
    public void TestGetHashCode()
    {
        var id = AssetId.New();
        var location = new UFile("Assets/MyAsset.sdtest");
        var ref1 = new AssetReference(id, location);
        var ref2 = new AssetReference(id, location);

        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
    }

    [Fact]
    public void TestToString()
    {
        var id = AssetId.Parse("11223344-5566-7788-99AA-BBCCDDEEFF00");
        var location = new UFile("Assets/MyAsset.sdtest");
        var assetRef = new AssetReference(id, location);

        var str = assetRef.ToString();
        Assert.Contains(id.ToString(), str);
        Assert.Contains(location, str);
        Assert.Equal($"{id}:{location}", str);
    }

    [Fact]
    public void TestTryParseValidFormat()
    {
        var id = AssetId.Parse("11223344-5566-7788-99AA-BBCCDDEEFF00");
        var location = "Assets/MyAsset.sdtest";
        var text = $"{id}:{location}";

        var result = AssetReference.TryParse(text, out var parsedId, out var parsedLocation);

        Assert.True(result);
        Assert.Equal(id, parsedId);
        Assert.Equal(location, parsedLocation);
    }

    [Fact]
    public void TestTryParseInvalidFormat()
    {
        var result = AssetReference.TryParse("invalid-format", out var id, out var location);

        Assert.False(result);
        Assert.Equal(AssetId.Empty, id);
        Assert.Null(location);
    }

    [Fact]
    public void TestTryParseEmptyString()
    {
        var result = AssetReference.TryParse(string.Empty, out var id, out var location);

        Assert.False(result);
        Assert.Equal(AssetId.Empty, id);
        Assert.Null(location);
    }

    [Fact]
    public void TestTryParseNullString()
    {
        Assert.Throws<ArgumentNullException>(() => AssetReference.TryParse(null!, out var id, out var location));
    }

    [Fact]
    public void TestTryParseWithAssetReference()
    {
        var id = AssetId.Parse("11223344-5566-7788-99AA-BBCCDDEEFF00");
        var location = "Assets/MyAsset.sdtest";
        var text = $"{id}:{location}";

        var result = AssetReference.TryParse(text, out var assetRef);

        Assert.True(result);
        Assert.NotNull(assetRef);
        Assert.Equal(id, assetRef!.Id);
        Assert.Equal(location, assetRef.Location);
    }

    [Fact]
    public void TestTryParseWithAssetReferenceInvalidFormat()
    {
        var result = AssetReference.TryParse("invalid-format", out var assetRef);

        Assert.False(result);
        Assert.Null(assetRef);
    }

    [Fact]
    public void TestHasLocation()
    {
        var id = AssetId.New();
        var location = new UFile("Assets/MyAsset.sdtest");
        var assetRef = new AssetReference(id, location);

        Assert.True(assetRef.HasLocation());
    }

    [Fact]
    public void TestHasLocationWithNullReference()
    {
        AssetReference? assetRef = null;
        Assert.False(assetRef!.HasLocation());
    }
}
