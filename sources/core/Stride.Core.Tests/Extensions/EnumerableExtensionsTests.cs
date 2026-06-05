// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Extensions;
using System.Collections;

namespace Stride.Core.Tests.Extensions;

public class EnumerableExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_ReturnsTrueForNull()
    {
        IEnumerable? enumerable = null;

        Assert.True(enumerable.IsNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_ReturnsTrueForEmptyEnumerable()
    {
        IEnumerable enumerable = new List<int>();

        Assert.True(enumerable.IsNullOrEmpty());
    }

    [Fact]
    public void IsNullOrEmpty_ReturnsFalseForNonEmptyEnumerable()
    {
        IEnumerable enumerable = new List<int> { 1, 2, 3 };

        Assert.False(enumerable.IsNullOrEmpty());
    }

    [Fact]
    public void ForEach_WithIEnumerable_ExecutesActionForEachItem()
    {
        IEnumerable items = new ArrayList { 1, 2, 3 };
        var sum = 0;

        items.ForEach<int>(x => sum += x);

        Assert.Equal(6, sum);
    }

    [Fact]
    public void ForEach_WithGenericEnumerable_ExecutesActionForEachItem()
    {
        var items = new List<int> { 1, 2, 3, 4 };
        var sum = 0;

        items.ForEach(x => sum += x);

        Assert.Equal(10, sum);
    }

    [Fact]
    public void ForEach_WithGenericEnumerable_ModifiesExternalState()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = new List<string>();

        items.ForEach(x => result.Add(x.ToUpper()));

        Assert.Equal(new[] { "A", "B", "C" }, result);
    }

    [Fact]
    public void IndexOf_ReturnsIndexOfFirstMatch()
    {
        var items = new List<int> { 10, 20, 30, 40, 50 };

        var index = items.IndexOf(x => x > 25);

        Assert.Equal(2, index);
    }

    [Fact]
    public void IndexOf_ReturnsMinusOneWhenNoMatch()
    {
        var items = new List<int> { 10, 20, 30 };

        var index = items.IndexOf(x => x > 100);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void IndexOf_ReturnsZeroForFirstElement()
    {
        var items = new List<string> { "apple", "banana", "cherry" };

        var index = items.IndexOf(x => x.StartsWith("a"));

        Assert.Equal(0, index);
    }

    [Fact]
    public void LastIndexOf_ReturnsIndexOfLastMatch()
    {
        var items = new List<int> { 10, 20, 30, 20, 10 };

        var index = items.LastIndexOf(x => x == 20);

        Assert.Equal(3, index);
    }

    [Fact]
    public void LastIndexOf_ReturnsMinusOneWhenNoMatch()
    {
        var items = new List<int> { 10, 20, 30 };

        var index = items.LastIndexOf(x => x > 100);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void LastIndexOf_OptimizesForIList()
    {
        IEnumerable<int> items = new List<int> { 10, 20, 30, 20, 10 };

        var index = items.LastIndexOf(x => x == 20);

        Assert.Equal(3, index);
    }

    [Fact]
    public void NotNull_FiltersOutNullReferenceTypes()
    {
        var items = new List<string?> { "a", null, "b", null, "c" };

        var result = items.NotNull().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    [Fact]
    public void NotNull_FiltersOutNullValueTypes()
    {
        var items = new List<int?> { 1, null, 2, null, 3 };

        var result = items.NotNull().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void NotNull_ReturnsEmptyForAllNulls()
    {
        var items = new List<string?> { null, null, null };

        var result = items.NotNull().ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void ToHashCode_ReturnsZeroForEmptyEnumerable()
    {
        var items = new List<object>();

        var hash = items.ToHashCode();

        Assert.Equal(0, hash);
    }

    [Fact]
    public void ToHashCode_ReturnsConsistentHashForSameData()
    {
        var items1 = new List<object> { new object(), new object() };
        var items2 = items1; // Same reference

        var hash1 = items1.ToHashCode();
        var hash2 = items2.ToHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ToHashCode_ComputesCombinedHash()
    {
        var obj1 = new object();
        var obj2 = new object();
        var items = new List<object> { obj1, obj2 };

        var hash = items.ToHashCode();

        // Verify it's not zero and not just one item's hash
        Assert.NotEqual(0, hash);
        Assert.NotEqual(obj1.GetHashCode(), hash);
        Assert.NotEqual(obj2.GetHashCode(), hash);
    }
}
