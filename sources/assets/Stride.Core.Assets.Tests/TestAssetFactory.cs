// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

// Concrete test factory implementation for testing AssetFactory
public class TestAssetFactory : AssetFactory<TestAssetWithParts>
{
    public override TestAssetWithParts New()
    {
        return new TestAssetWithParts();
    }
}

public class TestAssetFactory_Class
{
    [Fact]
    public void TestAssetTypeProperty()
    {
        var factory = new TestAssetFactory();

        Assert.Equal(typeof(TestAssetWithParts), factory.AssetType);
    }

    [Fact]
    public void TestNewMethod()
    {
        var factory = new TestAssetFactory();

        var asset = factory.New();

        Assert.NotNull(asset);
        Assert.IsType<TestAssetWithParts>(asset);
    }

    [Fact]
    public void TestNewCreatesNewInstances()
    {
        var factory = new TestAssetFactory();

        var asset1 = factory.New();
        var asset2 = factory.New();

        Assert.NotSame(asset1, asset2);
        Assert.NotEqual(asset1.Id, asset2.Id);
    }

    [Fact]
    public void TestFactoryImplementsInterface()
    {
        var factory = new TestAssetFactory();

        Assert.IsAssignableFrom<IAssetFactory<TestAssetWithParts>>(factory);
    }

    [Fact]
    public void TestAssetTypeIsCorrectGenericArgument()
    {
        var factory = new TestAssetFactory();

        // Verify the AssetType matches the generic parameter T
        Assert.True(typeof(TestAssetWithParts).IsAssignableFrom(factory.AssetType));
    }
}
