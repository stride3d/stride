// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

// Concrete test factory implementation for testing AssetFactory
public class TestAssetWithPartsFactory : AssetFactory<TestAssetWithParts>
{
    public override TestAssetWithParts New()
    {
        return new TestAssetWithParts();
    }
}

public class TestAssetFactory
{
    [Fact]
    public void TestAssetTypeProperty()
    {
        var factory = new TestAssetWithPartsFactory();

        Assert.Equal(typeof(TestAssetWithParts), factory.AssetType);
    }

    [Fact]
    public void TestNewMethod()
    {
        var factory = new TestAssetWithPartsFactory();

        var asset = factory.New();

        Assert.IsType<TestAssetWithParts>(asset);
    }

    [Fact]
    public void TestNewCreatesNewInstances()
    {
        var factory = new TestAssetWithPartsFactory();

        var asset1 = factory.New();
        var asset2 = factory.New();

        Assert.NotSame(asset1, asset2);
        Assert.NotEqual(asset1.Id, asset2.Id);
    }
}
