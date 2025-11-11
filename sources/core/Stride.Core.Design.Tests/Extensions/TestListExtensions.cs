// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for the <see cref="ListExtensions"/> class.
/// </summary>
public class TestListExtensions
{
    [Fact]
    public void Subset_WithValidRange_ShouldReturnCorrectElements()
    {
        var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var result = list.Subset(2, 3).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(3, result[0]);
        Assert.Equal(4, result[1]);
        Assert.Equal(5, result[2]);
    }

    [Fact]
    public void Subset_WithStartIndexZero_ShouldReturnFromBeginning()
    {
        var list = new List<string> { "a", "b", "c", "d" };

        var result = list.Subset(0, 2).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0]);
        Assert.Equal("b", result[1]);
    }

    [Fact]
    public void Subset_WithCountZero_ShouldReturnEmpty()
    {
        var list = new List<int> { 1, 2, 3 };

        var result = list.Subset(1, 0).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Subset_WithFullRange_ShouldReturnAllElements()
    {
        var list = new List<int> { 10, 20, 30 };

        var result = list.Subset(0, 3).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(10, result[0]);
        Assert.Equal(20, result[1]);
        Assert.Equal(30, result[2]);
    }

    [Fact]
    public void AddRange_WithListType_ShouldUseBuiltInAddRange()
    {
        var list = new List<int> { 1, 2 };
        ICollection<int> collection = list;
        var itemsToAdd = new[] { 3, 4, 5 };

        collection.AddRange(itemsToAdd);

        Assert.Equal(5, collection.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list);
    }

    [Fact]
    public void AddRange_WithNonListCollection_ShouldAddItemsIndividually()
    {
        ICollection<string> collection = new HashSet<string> { "a", "b" };
        var itemsToAdd = new[] { "c", "d", "e" };

        collection.AddRange(itemsToAdd);

        Assert.Equal(5, collection.Count);
        Assert.Contains("c", collection);
        Assert.Contains("d", collection);
        Assert.Contains("e", collection);
    }

    [Fact]
    public void AddRange_WithEmptyEnumerable_ShouldNotModifyCollection()
    {
        var list = new List<int> { 1, 2, 3 };
        ICollection<int> collection = list;

        collection.AddRange(Enumerable.Empty<int>());

        Assert.Equal(3, collection.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void AddRange_WithSingleItem_ShouldAddThatItem()
    {
        var list = new List<string> { "existing" };
        ICollection<string> collection = list;

        collection.AddRange(new[] { "new" });

        Assert.Equal(2, collection.Count);
        Assert.Equal(new[] { "existing", "new" }, list);
    }
}
