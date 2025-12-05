// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests.Collections;

// Test implementation of KeyedSortedList for testing purposes
public class TestKeyedSortedList : KeyedSortedList<string, TestItem>
{
    protected override string GetKeyForItem(TestItem item)
    {
        return item.Key;
    }
}

public class TestItem
{
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }

    public TestItem(string key, int value)
    {
        Key = key;
        Value = value;
    }
}

public class KeyedSortedListTests
{
    [Fact]
    public void Constructor_CreatesEmptyList()
    {
        var list = new TestKeyedSortedList();

        Assert.Empty(list);
    }

    [Fact]
    public void Add_AddsItemInSortedOrder()
    {
        var list = new TestKeyedSortedList();

        list.Add(new TestItem("b", 2));
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("c", 3));

        Assert.Equal(3, list.Count);
        Assert.Equal("a", list[0].Key);
        Assert.Equal("b", list[1].Key);
        Assert.Equal("c", list[2].Key);
    }

    [Fact]
    public void Add_ThrowsOnDuplicateKey()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));

        Assert.Throws<InvalidOperationException>(() => list.Add(new TestItem("a", 2)));
    }

    [Fact]
    public void ContainsKey_ReturnsTrueForExistingKey()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));

        Assert.True(list.ContainsKey("a"));
        Assert.True(list.ContainsKey("b"));
        Assert.False(list.ContainsKey("z"));
    }

    [Fact]
    public void Contains_ReturnsTrueForExistingItem()
    {
        var list = new TestKeyedSortedList();
        var item = new TestItem("a", 1);
        list.Add(item);

        Assert.Contains(item, list);
        Assert.DoesNotContain(new TestItem("z", 99), list);
    }

    [Fact]
    public void IndexerByKey_GetsCorrectItem()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));

        var item = list["a"];

        Assert.Equal(1, item.Value);
    }

    [Fact]
    public void IndexerByKey_ThrowsForNonExistentKey()
    {
        var list = new TestKeyedSortedList();

        Assert.Throws<KeyNotFoundException>(() => list["nonexistent"]);
    }

    [Fact]
    public void IndexerByKey_SetUpdatesExistingItem()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));

        list["a"] = new TestItem("a", 100);

        Assert.Equal(100, list["a"].Value);
    }

    [Fact]
    public void IndexerByKey_SetAddsNewItemIfNotExists()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));

        list["b"] = new TestItem("b", 2);

        Assert.Equal(2, list.Count);
        Assert.Equal(2, list["b"].Value);
    }

    [Fact]
    public void IndexerByIndex_GetsAndSetsItems()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));

        Assert.Equal(1, list[0].Value);

        list[0] = new TestItem("a", 10);

        Assert.Equal(10, list[0].Value);
    }

    [Fact]
    public void TryGetValue_ReturnsItemIfExists()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));

        var found = list.TryGetValue("a", out var item);

        Assert.True(found);
        Assert.NotNull(item);
        Assert.Equal(1, item.Value);
    }

    [Fact]
    public void TryGetValue_ReturnsFalseIfNotExists()
    {
        var list = new TestKeyedSortedList();

        var found = list.TryGetValue("nonexistent", out var item);

        Assert.False(found);
        Assert.Null(item);
    }

    [Fact]
    public void Remove_WithKey_RemovesItem()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));

        var removed = list.Remove("a");

        Assert.True(removed);
        Assert.Single(list);
        Assert.False(list.ContainsKey("a"));
    }

    [Fact]
    public void Remove_WithKey_ReturnsFalseIfNotFound()
    {
        var list = new TestKeyedSortedList();

        var removed = list.Remove("nonexistent");

        Assert.False(removed);
    }

    [Fact]
    public void Remove_WithItem_RemovesItem()
    {
        var list = new TestKeyedSortedList();
        var item = new TestItem("a", 1);
        list.Add(item);
        list.Add(new TestItem("b", 2));

        list.Remove(item);

        Assert.Single(list);
        Assert.False(list.ContainsKey("a"));
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));
        list.Add(new TestItem("c", 3));

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var list = new TestKeyedSortedList();
        var item = new TestItem("b", 2);
        list.Add(new TestItem("a", 1));
        list.Add(item);
        list.Add(new TestItem("c", 3));

        var index = list.IndexOf(item);

        Assert.Equal(1, index);
    }

    [Fact]
    public void Sort_ReordersItemsAfterKeyMutation()
    {
        var list = new TestKeyedSortedList();
        var item = new TestItem("b", 2);
        list.Add(new TestItem("a", 1));
        list.Add(item);
        list.Add(new TestItem("c", 3));

        // Mutate the key (this is generally not recommended but Sort() exists for this case)
        item.Key = "z";
        list.Sort();

        Assert.Equal("a", list[0].Key);
        Assert.Equal("c", list[1].Key);
        Assert.Equal("z", list[2].Key);
    }

    [Fact]
    public void CopyTo_CopiesItemsToArray()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));
        list.Add(new TestItem("c", 3));

        var array = new TestItem[5];
        ((System.Collections.Generic.ICollection<TestItem>)list).CopyTo(array, 1);

        Assert.Null(array[0]);
        Assert.Equal("a", array[1].Key);
        Assert.Equal("b", array[2].Key);
        Assert.Equal("c", array[3].Key);
        Assert.Null(array[4]);
    }

    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        var list = new TestKeyedSortedList();

        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void GetEnumerator_IteratesInSortedOrder()
    {
        var list = new TestKeyedSortedList();
        list.Add(new TestItem("c", 3));
        list.Add(new TestItem("a", 1));
        list.Add(new TestItem("b", 2));

        var keys = new List<string>();
        foreach (var item in list)
        {
            keys.Add(item.Key);
        }

        Assert.Equal(new[] { "a", "b", "c" }, keys);
    }

    [Fact]
    public void SyncRoot_ReturnsSelf()
    {
        var list = new TestKeyedSortedList();
        var collection = (System.Collections.ICollection)list;

        Assert.Same(list, collection.SyncRoot);
    }

    [Fact]
    public void IsSynchronized_ReturnsFalse()
    {
        var list = new TestKeyedSortedList();
        var collection = (System.Collections.ICollection)list;

        Assert.False(collection.IsSynchronized);
    }
}
