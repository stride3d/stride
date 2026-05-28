// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class IndexingDictionaryTests
{
    [Fact]
    public void Constructor_CreatesEmptyDictionary()
    {
        var dict = new IndexingDictionary<string>();
        Assert.Empty(dict);
        Assert.Equal(0, dict.Count);
    }

    [Fact]
    public void Add_AddsItemAtSpecificIndex()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(5, "value5");

        Assert.Equal(1, dict.Count);
        Assert.Equal("value5", dict[5]);
    }

    [Fact]
    public void Add_DuplicateKey_ThrowsException()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(1, "first");

        Assert.Throws<ArgumentException>(() => dict.Add(1, "second"));
    }

    [Fact]
    public void Indexer_Get_ReturnsCorrectValue()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(0, "zero");
        dict.Add(10, "ten");

        Assert.Equal("zero", dict[0]);
        Assert.Equal("ten", dict[10]);
    }

    [Fact]
    public void Indexer_Set_UpdatesValue()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(3, "old");
        dict[3] = "new";

        Assert.Equal("new", dict[3]);
        Assert.Equal(1, dict.Count);
    }

    [Fact]
    public void Indexer_Set_AddsNewValue()
    {
        var dict = new IndexingDictionary<string>();
        dict[5] = "value";

        Assert.Equal("value", dict[5]);
        Assert.Equal(1, dict.Count);
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(7, "seven");

        Assert.True(dict.ContainsKey(7));
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(3, "three");

        Assert.False(dict.ContainsKey(5));
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(4, "four");

        var result = dict.TryGetValue(4, out var value);

        Assert.True(result);
        Assert.Equal("four", value);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(2, "two");

        var result = dict.TryGetValue(8, out var value);

        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void Remove_ExistingItem_RemovesAndReturnsTrue()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(3, "three");
        dict.Add(4, "four");

        var removed = dict.Remove(3);

        Assert.True(removed);
        Assert.Equal(1, dict.Count);
        Assert.False(dict.ContainsKey(3));
    }

    [Fact]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(1, "one");

        var removed = dict.Remove(5);

        Assert.False(removed);
        Assert.Equal(1, dict.Count);
    }

    [Fact]
    public void Remove_ShrinksList()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(0, "zero");
        dict.Add(1, "one");
        dict.Add(2, "two");
        dict.Add(3, "three");

        dict.Remove(3);
        dict.Remove(2);

        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(1, "one");
        dict.Add(2, "two");
        dict.Add(3, "three");

        dict.Clear();

        Assert.Empty(dict);
        Assert.Equal(0, dict.Count);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(0, "zero");
        dict.Add(2, "two");
        dict.Add(5, "five");

        var keys = dict.Keys.OrderBy(k => k).ToList();

        Assert.Equal(new[] { 0, 2, 5 }, keys);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(0, "zero");
        dict.Add(2, "two");
        dict.Add(5, "five");

        var values = dict.Values.OrderBy(v => v).ToList();

        Assert.Equal(new[] { "five", "two", "zero" }, values);
    }

    [Fact]
    public void GetEnumerator_EnumeratesKeyValuePairs()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(1, "one");
        dict.Add(3, "three");
        dict.Add(5, "five");

        var pairs = dict.OrderBy(kvp => kvp.Key).ToList();

        Assert.Equal(3, pairs.Count);
        Assert.Equal(new KeyValuePair<int, string>(1, "one"), pairs[0]);
        Assert.Equal(new KeyValuePair<int, string>(3, "three"), pairs[1]);
        Assert.Equal(new KeyValuePair<int, string>(5, "five"), pairs[2]);
    }

    [Fact]
    public void Contains_ExistingPair_ReturnsTrue()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(2, "two");

        Assert.True(dict.Contains(new KeyValuePair<int, string>(2, "two")));
    }

    [Fact]
    public void Contains_WrongValue_ReturnsFalse()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(2, "two");

        Assert.False(dict.Contains(new KeyValuePair<int, string>(2, "other")));
    }

    [Fact]
    public void CopyTo_CopiesAllItems()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(1, "one");
        dict.Add(3, "three");

        var array = new KeyValuePair<int, string>[4];
        dict.CopyTo(array, 1);

        Assert.Equal(default, array[0]);
        Assert.NotEqual(default, array[1]);
        Assert.NotEqual(default, array[2]);
        Assert.Equal(default, array[3]);
    }

    [Fact]
    public void SafeGet_InvalidIndex_ReturnsNull()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(5, "five");

        Assert.Null(dict.SafeGet(-1));
        Assert.Null(dict.SafeGet(10));
    }

    [Fact]
    public void SparseIndices_WorksCorrectly()
    {
        var dict = new IndexingDictionary<string>();
        dict.Add(0, "zero");
        dict.Add(100, "hundred");
        dict.Add(50, "fifty");

        Assert.Equal(3, dict.Count);
        Assert.Equal("zero", dict[0]);
        Assert.Equal("fifty", dict[50]);
        Assert.Equal("hundred", dict[100]);
        Assert.Null(dict.SafeGet(25));
    }
}
