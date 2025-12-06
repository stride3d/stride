// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests.Collections;

public class ConstrainedListTests
{
    [Fact]
    public void Constructor_WithoutParameters_CreatesEmptyList()
    {
        var list = new ConstrainedList<int>();

        Assert.Empty(list);
        Assert.True(list.ThrowException);
    }

    [Fact]
    public void Constructor_WithConstraint_SetsProperties()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0, false, "Must be positive");

        Assert.NotNull(list.Constraint);
        Assert.False(list.ThrowException);
    }

    [Fact]
    public void Add_WithoutConstraint_AddsItem()
    {
        var list = new ConstrainedList<int>();

        list.Add(1);
        list.Add(2);

        Assert.Equal(2, list.Count);
        Assert.Contains(1, list);
        Assert.Contains(2, list);
    }

    [Fact]
    public void Add_WithPassingConstraint_AddsItem()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0);

        list.Add(5);
        list.Add(10);

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Add_WithFailingConstraintAndThrowFalse_DoesNotAddItem()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0, false);

        list.Add(-5);

        Assert.Empty(list);
    }

    [Fact]
    public void Add_WithFailingConstraintAndThrowTrue_ThrowsException()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0, true, "Must be positive");

        var ex = Assert.Throws<ArgumentException>(() => list.Add(-5));
        Assert.Contains("Must be positive", ex.Message);
    }

    [Fact]
    public void Insert_WithPassingConstraint_InsertsItem()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0);
        list.Add(10);
        list.Add(30);

        list.Insert(1, 20);

        Assert.Equal(3, list.Count);
        Assert.Equal(20, list[1]);
    }

    [Fact]
    public void Insert_WithFailingConstraint_DoesNotInsert()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0, false);
        list.Add(10);

        list.Insert(0, -5);

        Assert.Single(list);
    }

    [Fact]
    public void Indexer_Set_WithPassingConstraint_UpdatesValue()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0);
        list.Add(10);

        list[0] = 20;

        Assert.Equal(20, list[0]);
    }

    [Fact]
    public void Indexer_Set_WithFailingConstraint_DoesNotUpdate()
    {
        var list = new ConstrainedList<int>((l, item) => item > 0, false);
        list.Add(10);

        list[0] = -5;

        Assert.Equal(10, list[0]);
    }

    [Fact]
    public void Remove_RemovesItem()
    {
        var list = new ConstrainedList<int> { 1, 2, 3 };

        var removed = list.Remove(2);

        Assert.True(removed);
        Assert.Equal(2, list.Count);
        Assert.DoesNotContain(2, list);
    }

    [Fact]
    public void RemoveAt_RemovesItemAtIndex()
    {
        var list = new ConstrainedList<string> { "a", "b", "c" };

        list.RemoveAt(1);

        Assert.Equal(2, list.Count);
        Assert.Equal("a", list[0]);
        Assert.Equal("c", list[1]);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var list = new ConstrainedList<int> { 1, 2, 3, 4 };

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void Contains_ReturnsTrueForExistingItem()
    {
        var list = new ConstrainedList<string> { "a", "b", "c" };

        Assert.Contains("b", list);
        Assert.DoesNotContain("z", list);
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var list = new ConstrainedList<int> { 10, 20, 30 };

        Assert.Equal(1, list.IndexOf(20));
        Assert.Equal(-1, list.IndexOf(99));
    }

    [Fact]
    public void CopyTo_CopiesItemsToArray()
    {
        var list = new ConstrainedList<int> { 1, 2, 3 };
        var array = new int[5];

        list.CopyTo(array, 1);

        Assert.Equal(0, array[0]);
        Assert.Equal(1, array[1]);
        Assert.Equal(2, array[2]);
        Assert.Equal(3, array[3]);
        Assert.Equal(0, array[4]);
    }

    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        var list = new ConstrainedList<int> { 1, 2, 3 };
        var sum = 0;

        foreach (var item in list)
        {
            sum += item;
        }

        Assert.Equal(6, sum);
    }

    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        var list = new ConstrainedList<int>();

        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void Constraint_CanAccessListInPredicate()
    {
        var list = new ConstrainedList<int>((l, item) => l.Count < 3, false);

        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4); // This should fail constraint (count already 3)

        Assert.Equal(3, list.Count);
    }
}
