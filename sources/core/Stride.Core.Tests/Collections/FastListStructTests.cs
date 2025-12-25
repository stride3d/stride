// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests.Collections;

public class FastListStructTests
{
    [Fact]
    public void Constructor_WithCapacity_CreatesEmptyListWithCapacity()
    {
        var list = new FastListStruct<int>(10);

        Assert.Equal(0, list.Count);
        Assert.True(list.Items.Length >= 10);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_CreatesEmptyList()
    {
        var list = new FastListStruct<int>(0);

        Assert.Equal(0, list.Count);
        Assert.Empty(list.Items);
    }

    [Fact]
    public void Constructor_WithArray_CopiesArrayElements()
    {
        var array = new[] { 1, 2, 3, 4, 5 };

        var list = new FastListStruct<int>(array);

        Assert.Equal(5, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(5, list[4]);
    }

    [Fact]
    public void Constructor_WithFastList_CopiesElements()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var fastList = new FastList<int> { 10, 20, 30 };
#pragma warning restore CS0618

        var list = new FastListStruct<int>(fastList);

        Assert.Equal(3, list.Count);
        Assert.Equal(10, list[0]);
        Assert.Equal(30, list[2]);
    }

    [Fact]
    public void Add_AddsItemToEnd()
    {
        var list = new FastListStruct<int>(2);

        list.Add(1);
        list.Add(2);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void Add_AutomaticallyExpandsCapacity()
    {
        var list = new FastListStruct<int>(2);

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Equal(3, list.Count);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void AddRange_AddsMultipleItems()
    {
        var list1 = new FastListStruct<int>(5);
        list1.Add(1);
        list1.Add(2);

        var list2 = new FastListStruct<int>(5);
        list2.Add(3);
        list2.Add(4);

        list1.AddRange(list2);

        Assert.Equal(4, list1.Count);
        Assert.Equal(3, list1[2]);
        Assert.Equal(4, list1[3]);
    }

    [Fact]
    public void Insert_InsertsItemAtIndex()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(3);

        list.Insert(1, 2);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Insert_AtBeginning_ShiftsElements()
    {
        var list = new FastListStruct<int>(5);
        list.Add(2);
        list.Add(3);

        list.Insert(0, 1);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void Insert_ExpandsCapacityIfNeeded()
    {
        var list = new FastListStruct<int>(2);
        list.Add(1);
        list.Add(3);

        list.Insert(1, 2);

        Assert.Equal(3, list.Count);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void RemoveAt_RemovesItemAndShifts()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.RemoveAt(1);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(3, list[1]);
    }

    [Fact]
    public void Remove_RemovesFirstOccurrence()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var result = list.Remove(2);

        Assert.True(result);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(3, list[1]);
    }

    [Fact]
    public void Remove_ReturnsFalseIfNotFound()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);

        var result = list.Remove(99);

        Assert.False(result);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.Clear();

        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Contains_ReturnsTrueForExistingItem()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.True(list.Contains(2));
        Assert.False(list.Contains(99));
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var list = new FastListStruct<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        Assert.Equal(1, list.IndexOf(20));
        Assert.Equal(-1, list.IndexOf(99));
    }

    [Fact]
    public void Indexer_GetsAndSetsValues()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.Equal(2, list[1]);

        list[1] = 22;

        Assert.Equal(22, list[1]);
    }

    [Fact]
    public void SwapRemoveAt_RemovesItemBySwappingWithLast()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);

        list.SwapRemoveAt(1);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(4, list[1]); // Last item swapped here
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void SwapRemoveAt_OnLastItem_JustRemoves()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        list.SwapRemoveAt(2);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void ToArray_ReturnsArrayWithCorrectElements()
    {
        var list = new FastListStruct<int>(10);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var array = list.ToArray();

        Assert.Equal(3, array.Length);
        Assert.Equal(1, array[0]);
        Assert.Equal(3, array[2]);
    }

    [Fact]
    public void EnsureCapacity_IncreasesCapacity()
    {
        var list = new FastListStruct<int>(2);
        list.Add(1);
        list.Add(2);

        list.EnsureCapacity(10);

        Assert.True(list.Items.Length >= 10);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void ImplicitConversion_FromFastList()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var fastList = new FastList<int> { 1, 2, 3 };
#pragma warning restore CS0618

        FastListStruct<int> list = fastList;

        Assert.Equal(3, list.Count);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void ImplicitConversion_FromArray()
    {
        var array = new[] { 1, 2, 3 };

        FastListStruct<int> list = array;

        Assert.Equal(3, list.Count);
        Assert.Equal(2, list[1]);
    }

    [Fact]
    public void GetEnumerator_IteratesOverItems()
    {
        var list = new FastListStruct<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        var sum = 0;
        foreach (var item in list)
        {
            sum += item;
        }

        Assert.Equal(6, sum);
    }

    [Fact]
    public void Enumerator_ImplementsIEnumerator()
    {
        var list = new FastListStruct<string>(5);
        list.Add("a");
        list.Add("b");
        list.Add("c");

        var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal("c", enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }
}
