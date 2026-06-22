// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestAssetRegistry
{
    [Fact]
    public void TestIsAssetFileExtensionWithNull()
    {
#pragma warning disable CS8625
        Assert.False(AssetRegistry.IsAssetFileExtension(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void TestIsAssetFileExtensionWithEmpty()
    {
        Assert.False(AssetRegistry.IsAssetFileExtension(string.Empty));
    }

    [Fact]
    public void TestIsAssetFileExtensionWithUnknown()
    {
        Assert.False(AssetRegistry.IsAssetFileExtension(".unknown"));
    }

    [Fact]
    public void TestGetAssetTypeFromFileExtension()
    {
        // For unknown extensions, should return null
        var type = AssetRegistry.GetAssetTypeFromFileExtension(".unknown");
        Assert.Null(type);
    }

    [Fact]
    public void TestGetAssetTypeFromFileExtensionWithNull()
    {
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => AssetRegistry.GetAssetTypeFromFileExtension(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void TestGetAssetTypeFromFileExtensionWithEmpty()
    {
        var type = AssetRegistry.GetAssetTypeFromFileExtension(string.Empty);
        Assert.Null(type);
    }

    [Fact]
    public void TestSupportedPlatforms()
    {
        var platforms = AssetRegistry.SupportedPlatforms;

        Assert.NotNull(platforms);
        // SupportedPlatforms is a cached singleton collection: repeated access returns the same instance.
        Assert.Same(platforms, AssetRegistry.SupportedPlatforms);
    }
}
