using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

#pragma warning disable CS0618 // Type or member is obsolete

public class FastListTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyList()
    {
        var list = new FastList<int>();

        Assert.Empty(list);
        Assert.NotNull(list.Items);
    }

    [Fact]
    public void Constructor_WithCapacity_CreatesListWithSpecifiedCapacity()
    {
        var list = new FastList<int>(10);

        Assert.Empty(list);
        Assert.Equal(10, list.Capacity);
    }

    [Fact]
    public void Constructor_WithCollection_CopiesItems()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };

        var list = new FastList<int>(source);

        Assert.Equal(5, list.Count);
        Assert.Equal(source, list);
    }

    [Fact]
    public void Add_AddsItemsToList()
    {
        var list = new FastList<int>();

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Add_AutomaticallyIncreasesCapacity()
    {
        var list = new FastList<int>(2);

        list.Add(1);
        list.Add(2);
        list.Add(3); // Should trigger capacity increase

        Assert.Equal(3, list.Count);
        Assert.True(list.Capacity >= 3);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var list = new FastList<int> { 1, 2, 3 };

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void Contains_ReturnsTrueForExistingItem()
    {
        var list = new FastList<int> { 1, 2, 3 };

        Assert.Contains(2, list);
        Assert.DoesNotContain(4, list);
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var list = new FastList<int> { 10, 20, 30 };

        Assert.Equal(1, list.IndexOf(20));
        Assert.Equal(-1, list.IndexOf(40));
    }

    [Fact]
    public void Insert_InsertsItemAtSpecifiedIndex()
    {
        var list = new FastList<int> { 1, 3 };

        list.Insert(1, 2);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Remove_RemovesSpecifiedItem()
    {
        var list = new FastList<int> { 1, 2, 3 };

        var result = list.Remove(2);

        Assert.True(result);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(3, list[1]);
    }

    [Fact]
    public void Remove_ReturnsFalseForNonExistingItem()
    {
        var list = new FastList<int> { 1, 2, 3 };

        var result = list.Remove(4);

        Assert.False(result);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void RemoveAt_RemovesItemAtSpecifiedIndex()
    {
        var list = new FastList<int> { 1, 2, 3 };

        list.RemoveAt(1);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(3, list[1]);
    }

    [Fact]
    public void Indexer_GetAndSet_WorkCorrectly()
    {
        var list = new FastList<int> { 1, 2, 3 };

        Assert.Equal(2, list[1]);

        list[1] = 20;

        Assert.Equal(20, list[1]);
    }

    [Fact]
    public void CopyTo_CopiesItemsToArray()
    {
        var list = new FastList<int> { 1, 2, 3 };
        var array = new int[5];

        list.CopyTo(array, 1);

        Assert.Equal(0, array[0]);
        Assert.Equal(1, array[1]);
        Assert.Equal(2, array[2]);
        Assert.Equal(3, array[3]);
        Assert.Equal(0, array[4]);
    }

    [Fact]
    public void Enumerator_IteratesThroughAllItems()
    {
        var list = new FastList<int> { 1, 2, 3 };
        var items = new List<int>();

        foreach (var item in list)
        {
            items.Add(item);
        }

        Assert.Equal(3, items.Count);
        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    [Fact]
    public void Capacity_CanBeIncreased()
    {
        var list = new FastList<int> { 1, 2, 3 };

        list.Capacity = 10;

        Assert.Equal(10, list.Capacity);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Capacity_CanBeDecreased()
    {
        var list = new FastList<int>(10) { 1, 2, 3 };

        list.Capacity = 5;

        Assert.Equal(5, list.Capacity);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void EnsureCapacity_IncreasesCapacityWhenNeeded()
    {
        var list = new FastList<int>(2);

        list.EnsureCapacity(10);

        Assert.True(list.Capacity >= 10);
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
