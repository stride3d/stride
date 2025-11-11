// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Design.Tests;

/// <summary>
/// Tests for the <see cref="KeyValuePair"/> static class.
/// </summary>
public class TestKeyValuePair
{
    [Fact]
    public void Create_WithIntAndString_ShouldReturnCorrectPair()
    {
        var result = KeyValuePair.Create(42, "test");

        Assert.Equal(42, result.Key);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Create_WithStringAndInt_ShouldReturnCorrectPair()
    {
        var result = KeyValuePair.Create("key", 123);

        Assert.Equal("key", result.Key);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void Create_WithNullableTypes_ShouldHandleNulls()
    {
        var result = KeyValuePair.Create<string?, int?>(null, null);

        Assert.Null(result.Key);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Create_WithComplexTypes_ShouldWork()
    {
        var key = new List<int> { 1, 2, 3 };
        var value = new Dictionary<string, string> { ["a"] = "b" };

        var result = KeyValuePair.Create(key, value);

        Assert.Same(key, result.Key);
        Assert.Same(value, result.Value);
    }

    [Fact]
    public void Create_WithSameType_ShouldWork()
    {
        var result = KeyValuePair.Create("first", "second");

        Assert.Equal("first", result.Key);
        Assert.Equal("second", result.Value);
    }
}
