// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests;

public class PropertyKeyTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesPropertyKey()
    {
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.Equal("TestProperty", key.Name);
        Assert.Equal(typeof(int), key.PropertyType);
        Assert.Equal(typeof(PropertyKeyTests), key.OwnerType);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PropertyKey<int>(null!, typeof(PropertyKeyTests)));
    }

    [Fact]
    public void IsValueType_WithValueType_ReturnsTrue()
    {
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.True(key.IsValueType);
    }

    [Fact]
    public void IsValueType_WithReferenceType_ReturnsFalse()
    {
        var key = new PropertyKey<string>("TestProperty", typeof(PropertyKeyTests));

        Assert.False(key.IsValueType);
    }

    [Fact]
    public void ToString_ReturnsPropertyName()
    {
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.Equal("TestProperty", key.ToString());
    }

    [Fact]
    public void CompareTo_WithSameName_ReturnsZero()
    {
        var key1 = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));
        var key2 = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.Equal(0, key1.CompareTo(key2));
    }

    [Fact]
    public void CompareTo_WithDifferentNames_ReturnsNonZero()
    {
        var key1 = new PropertyKey<int>("AProperty", typeof(PropertyKeyTests));
        var key2 = new PropertyKey<int>("BProperty", typeof(PropertyKeyTests));

        Assert.True(key1.CompareTo(key2) < 0);
        Assert.True(key2.CompareTo(key1) > 0);
    }

    [Fact]
    public void CompareTo_IsCaseInsensitive()
    {
        var key1 = new PropertyKey<int>("testproperty", typeof(PropertyKeyTests));
        var key2 = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.Equal(0, key1.CompareTo(key2));
    }

    [Fact]
    public void CompareTo_WithNull_ReturnsZero()
    {
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.Equal(0, key.CompareTo(null));
    }

    [Fact]
    public void DefaultValueMetadata_IsSetByDefault()
    {
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests));

        Assert.NotNull(key.DefaultValueMetadataT);
    }

    [Fact]
    public void Constructor_WithCustomMetadata_UsesProvidedMetadata()
    {
        var defaultMetadata = new StaticDefaultValueMetadata<int>(42);
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests), defaultMetadata);
        var container = new PropertyContainer();

        Assert.Equal(42, key.DefaultValueMetadataT.GetDefaultValue(ref container));
    }

    [Fact]
    public void Metadatas_ReturnsAllMetadatas()
    {
        var metadata1 = new StaticDefaultValueMetadata<int>(10);
        var key = new PropertyKey<int>("TestProperty", typeof(PropertyKeyTests), metadata1);

        Assert.NotEmpty(key.Metadatas);
        Assert.Contains(metadata1, key.Metadatas);
    }
}
