// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Extensions;
using Stride.Core.Collections;

namespace Stride.Core.Tests.Extensions;

public class CollectionExtensionsTests
{
    [Fact]
    public void SwapRemove_RemovesItemBySwappingWithLast()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };

        list.SwapRemove(2);

        Assert.Equal(4, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(5, list[1]); // Last item swapped here
        Assert.Equal(3, list[2]);
        Assert.Equal(4, list[3]);
    }

    [Fact]
    public void SwapRemove_DoesNothingIfItemNotFound()
    {
        var list = new List<int> { 1, 2, 3 };

        list.SwapRemove(99);

        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void SwapRemove_RemovesLastItemDirectly()
    {
        var list = new List<int> { 1, 2, 3 };

        list.SwapRemove(3);

        Assert.Equal(2, list.Count);
        Assert.Equal(new[] { 1, 2 }, list);
    }

    [Fact]
    public void SwapRemoveAt_RemovesItemAtIndexBySwapping()
    {
        var list = new List<string> { "a", "b", "c", "d" };

        list.SwapRemoveAt(1);

        Assert.Equal(3, list.Count);
        Assert.Equal("a", list[0]);
        Assert.Equal("d", list[1]); // Last item swapped here
        Assert.Equal("c", list[2]);
    }

    [Fact]
    public void SwapRemoveAt_RemovesLastItemDirectly()
    {
        var list = new List<string> { "a", "b", "c" };

        list.SwapRemoveAt(2);

        Assert.Equal(2, list.Count);
        Assert.Equal(new[] { "a", "b" }, list);
    }

    [Fact]
    public void SwapRemoveAt_ThrowsOnNegativeIndex()
    {
        var list = new List<int> { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => list.SwapRemoveAt(-1));
    }

    [Fact]
    public void SwapRemoveAt_ThrowsOnIndexOutOfBounds()
    {
        var list = new List<int> { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => list.SwapRemoveAt(10));
    }

    [Fact]
    public void GetItemOrNull_ReturnsItemWhenIndexValid()
    {
        var list = new List<string> { "a", "b", "c" };

        var result = list.GetItemOrNull(1);

        Assert.Equal("b", result);
    }

    [Fact]
    public void GetItemOrNull_ReturnsNullWhenIndexNegative()
    {
        var list = new List<string> { "a", "b", "c" };

        var result = list.GetItemOrNull(-1);

        Assert.Null(result);
    }

    [Fact]
    public void GetItemOrNull_ReturnsNullWhenIndexOutOfBounds()
    {
        var list = new List<string> { "a", "b", "c" };

        var result = list.GetItemOrNull(10);

        Assert.Null(result);
    }

    [Fact]
    public void GetItemOrNull_ReturnsNullForEmptyList()
    {
        var list = new List<string>();

        var result = list.GetItemOrNull(0);

        Assert.Null(result);
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndexForReadOnlyList()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30, 40 }.AsReadOnly();

        var index = list.IndexOf(30);

        Assert.Equal(2, index);
    }

    [Fact]
    public void IndexOf_ReturnsMinusOneWhenItemNotFound()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30 }.AsReadOnly();

        var index = list.IndexOf(99);

        Assert.Equal(-1, index);
    }

    [Fact]
    public void IndexOf_ReturnsFirstOccurrence()
    {
        IReadOnlyList<string> list = new List<string> { "a", "b", "a", "c" }.AsReadOnly();

        var index = list.IndexOf("a");

        Assert.Equal(0, index);
    }

    [Fact]
    public void IndexOf_WorksWithReferenceTypes()
    {
        var obj1 = new object();
        var obj2 = new object();
        var obj3 = new object();
        IReadOnlyList<object> list = new List<object> { obj1, obj2, obj3 }.AsReadOnly();

        var index = list.IndexOf(obj2);

        Assert.Equal(1, index);
    }
}
