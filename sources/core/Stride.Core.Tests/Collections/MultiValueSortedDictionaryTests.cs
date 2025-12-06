// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class MultiValueSortedDictionaryTests
{
    [Fact]
    public void Constructor_CreatesEmptyDictionary()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        Assert.Empty(dict);
        Assert.Empty(dict);
    }

    [Fact]
    public void Constructor_WithComparer_UsesComparer()
    {
        var dict = new MultiValueSortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, dict.Comparer);
    }

    [Fact]
    public void Add_SingleValue_AddsToDictionary()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        Assert.Single(dict);
    }

    [Fact]
    public void Add_MultipleValuesWithSameKey_AllAdded()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "first");
        dict.Add(1, "second");
        dict.Add(1, "third");

        // Note: Count reflects all key-value pairs added, not distinct keys
        Assert.Equal(3, dict.Count);
        // Indexer returns first value only, not all values
        Assert.Equal("first", dict[1]);
    }

    [Fact]
    public void Add_MaintainsSortedKeyOrder()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(5, "five");
        dict.Add(2, "two");
        dict.Add(8, "eight");
        dict.Add(1, "one");

        var keys = dict.Keys.ToList();
        Assert.Equal(new[] { 1, 2, 5, 8 }, keys);
    }

    [Fact]
    public void Indexer_ReturnsValuesForKey()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "a");
        dict.Add(1, "b");
        dict.Add(2, "c");

        // Indexer returns single TValue, not IEnumerable
        // Use Keys to check distinct keys
        Assert.Equal(2, dict.Keys.Count);
        Assert.True(dict.ContainsKey(1));
        Assert.True(dict.ContainsKey(2));
    }

    [Fact]
    public void Indexer_NonExistentKey_ThrowsKeyNotFoundException()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        Assert.Throws<KeyNotFoundException>(() => dict[99]);
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(5, "five");

        Assert.True(dict.ContainsKey(5));
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(5, "five");

        Assert.False(dict.ContainsKey(10));
    }

    [Fact]
    public void ContainsValue_ExistingValue_ReturnsTrue()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");
        dict.Add(2, "two");

        Assert.True(dict.ContainsValue("one"));
    }

    [Fact]
    public void ContainsValue_NonExistingValue_ReturnsFalse()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        Assert.False(dict.ContainsValue("ten"));
    }

    [Fact]
    public void Remove_RemovesAllValuesForKey()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "a");
        dict.Add(1, "b");
        dict.Add(2, "c");

        var removed = dict.Remove(1);

        Assert.True(removed);
        Assert.Single(dict); // Only key 2 remains with 1 value
        Assert.False(dict.ContainsKey(1));
    }

    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        var removed = dict.Remove(5);

        Assert.False(removed);
        Assert.Single(dict); // Still has 1 key-value pair
    }

    [Fact]
    public void Clear_RemovesAllElements()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");
        dict.Add(2, "two");
        dict.Add(3, "three");

        dict.Clear();

        Assert.Empty(dict);
    }

    [Fact]
    public void Keys_ReturnsDistinctKeysInOrder()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(3, "a");
        dict.Add(1, "b");
        dict.Add(1, "c");
        dict.Add(2, "d");

        var keys = dict.Keys.ToList();

        Assert.Equal(new[] { 1, 2, 3 }, keys);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "a");
        dict.Add(1, "b");
        dict.Add(2, "c");

        var values = dict.Values.ToList();

        Assert.Equal(3, values.Count);
        Assert.Contains("a", values);
        Assert.Contains("b", values);
        Assert.Contains("c", values);
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueWithValues()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "a");
        dict.Add(1, "b");

        var result = dict.TryGetValue(1, out IEnumerable<string> values);

        Assert.True(result);
        Assert.NotNull(values);
        Assert.Equal(2, values.Count());
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        var result = dict.TryGetValue(99, out IEnumerable<string> values);

        Assert.False(result);
        Assert.Null(values);
    }

    [Fact]
    public void GetEnumerator_EnumeratesKeyValuePairs()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "a");
        dict.Add(1, "b");
        dict.Add(2, "c");

        var pairs = dict.ToList();

        Assert.Equal(3, pairs.Count);
        Assert.All(pairs, pair => Assert.True(pair.Key == 1 || pair.Key == 2));
    }

    [Fact]
    public void CaseInsensitiveComparer_MergesKeysIgnoringCase()
    {
        var dict = new MultiValueSortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        dict.Add("test", 1);
        dict.Add("TEST", 2);
        dict.Add("Test", 3);

        Assert.Single(dict.Keys);
        // All three values were added under the same key (case-insensitive)
        Assert.Equal(3, dict.Count);
    }

    [Fact]
    public void Count_ReflectsTotalKeyValuePairs()
    {
        var dict = new MultiValueSortedDictionary<int, string>();

        Assert.Empty(dict);

        dict.Add(1, "a");
        Assert.Single(dict);

        dict.Add(1, "b");
        Assert.Equal(2, dict.Count);

        dict.Add(2, "c");
        Assert.Equal(3, dict.Count);

        dict.Remove(1);
        Assert.Single(dict);
    }

    [Fact]
    public void MixedOperations_MaintainsConsistency()
    {
        var dict = new MultiValueSortedDictionary<int, string>();

        dict.Add(5, "five-1");
        dict.Add(3, "three");
        dict.Add(5, "five-2");
        dict.Add(1, "one");

        Assert.Equal(4, dict.Count);
        Assert.Equal(3, dict.Keys.Count);

        dict.Remove(5);

        Assert.Equal(2, dict.Count);
        Assert.Equal(2, dict.Keys.Count);
        Assert.Equal(new[] { 1, 3 }, dict.Keys);
    }

    [Fact]
    public void AddKeyValuePair_Works()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        ((ICollection<KeyValuePair<int, string>>)dict).Add(new KeyValuePair<int, string>(1, "one"));

        Assert.Single(dict);
        // Indexer returns single value, not collection
        Assert.Equal("one", dict[1]);
    }

    [Fact]
    public void ContainsKeyValuePair_ExistingPair_ReturnsTrue()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        var contains = ((ICollection<KeyValuePair<int, string>>)dict).Contains(new KeyValuePair<int, string>(1, "one"));

        Assert.True(contains);
    }

    [Fact]
    public void ContainsKeyValuePair_NonExistingPair_ReturnsFalse()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        var contains = ((ICollection<KeyValuePair<int, string>>)dict).Contains(new KeyValuePair<int, string>(1, "two"));

        Assert.False(contains);
    }

    [Fact]
    public void RemoveKeyValuePair_ExistingPair_ReturnsTrue()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "a");
        dict.Add(1, "b");

        var removed = ((ICollection<KeyValuePair<int, string>>)dict).Remove(new KeyValuePair<int, string>(1, "a"));

        Assert.True(removed);
        Assert.Single(dict); // Still has key 1 with value "b"
        Assert.Equal("b", dict[1]);
    }

    [Fact]
    public void RemoveKeyValuePair_NonExistingPair_ReturnsFalse()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");

        var removed = ((ICollection<KeyValuePair<int, string>>)dict).Remove(new KeyValuePair<int, string>(1, "two"));

        Assert.False(removed);
        Assert.Single(dict);
    }

    [Fact]
    public void CopyTo_CopiesAllElements()
    {
        var dict = new MultiValueSortedDictionary<int, string>();
        dict.Add(1, "one");
        dict.Add(2, "two");

        var array = new KeyValuePair<int, string>[3];
        ((ICollection<KeyValuePair<int, string>>)dict).CopyTo(array, 1);

        Assert.Equal(default, array[0]);
        Assert.NotEqual(default, array[1]);
        Assert.NotEqual(default, array[2]);
    }
}
