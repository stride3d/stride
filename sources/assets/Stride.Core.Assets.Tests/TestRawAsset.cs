// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestRawAsset
{
    [Fact]
    public void TestConstructor()
    {
        var rawAsset = new RawAsset();

        Assert.True(rawAsset.Compress); // Default value is true
    }

    [Fact]
    public void TestCompressProperty()
    {
        var rawAsset = new RawAsset { Compress = false };

        Assert.False(rawAsset.Compress);
    }

    [Fact]
    public void TestInheritsFromAssetWithSource()
    {
        var rawAsset = new RawAsset();

        Assert.IsAssignableFrom<AssetWithSource>(rawAsset);
        Assert.IsAssignableFrom<Asset>(rawAsset);
    }

    [Fact]
    public void TestIdIsGenerated()
    {
        var rawAsset = new RawAsset();

        Assert.NotEqual(AssetId.Empty, rawAsset.Id);
    }

    [Fact]
    public void TestMultipleInstancesHaveDifferentIds()
    {
        var asset1 = new RawAsset();
        var asset2 = new RawAsset();

        Assert.NotEqual(asset1.Id, asset2.Id);
    }
}
