// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Design.Tests.Collections;

/// <summary>
/// Tests for the <see cref="HybridDictionary{TKey,TValue}"/> class.
/// </summary>
public class TestHybridDictionary
{
    [Fact]
    public void Constructor_Default_ShouldCreateEmptyDictionary()
    {
        var dictionary = new HybridDictionary<string, int>();

        Assert.Empty(dictionary);
        Assert.False(dictionary.IsReadOnly);
    }

    [Fact]
    public void Constructor_WithCapacity_ShouldCreateEmptyDictionaryWithCapacity()
    {
        var dictionary = new HybridDictionary<string, int>(10);

        Assert.Empty(dictionary);
    }

    [Fact]
    public void Constructor_WithComparer_ShouldUseComparer()
    {
        var dictionary = new HybridDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        dictionary.Add("Key", 1);
        Assert.True(dictionary.ContainsKey("key"));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HybridDictionary<string, int>(-1));
    }

    [Fact]
    public void Add_ToEmptyDictionary_ShouldAddItem()
    {
        var dictionary = new HybridDictionary<string, int>();

        dictionary.Add("key1", 42);

        Assert.Single(dictionary);
        Assert.Equal(42, dictionary["key1"]);
    }

    [Fact]
    public void Add_MultipleItems_ShouldAddAllItems()
    {
        var dictionary = new HybridDictionary<string, int>();

        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);
        dictionary.Add("key3", 3);

        Assert.Equal(3, dictionary.Count);
        Assert.Equal(1, dictionary["key1"]);
        Assert.Equal(2, dictionary["key2"]);
        Assert.Equal(3, dictionary["key3"]);
    }

    [Fact]
    public void Add_DuplicateKey_ShouldThrowArgumentException()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key", 1);

        Assert.Throws<ArgumentException>(() => dictionary.Add("key", 2));
    }

    [Fact]
    public void Add_ManyItems_ShouldTransitionToDictionary()
    {
        var dictionary = new HybridDictionary<int, string>();

        for (int i = 0; i < 20; i++)
        {
            dictionary.Add(i, $"value{i}");
        }

        Assert.Equal(20, dictionary.Count);
        Assert.Equal("value10", dictionary[10]);
    }

    [Fact]
    public void Remove_ExistingKey_ShouldRemoveAndReturnTrue()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);

        var result = dictionary.Remove("key1");

        Assert.True(result);
        Assert.Equal(1, dictionary.Count);
        Assert.False(dictionary.ContainsKey("key1"));
    }

    [Fact]
    public void Remove_NonExistingKey_ShouldReturnFalse()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);

        var result = dictionary.Remove("key2");

        Assert.False(result);
        Assert.Equal(1, dictionary.Count);
    }

    [Fact]
    public void ContainsKey_WithExistingKey_ShouldReturnTrue()
    {
        var dictionary = new HybridDictionary<string, int>(1);
        dictionary.Add("key", 42);

        Assert.True(dictionary.ContainsKey("key"));
    }

    [Fact]
    public void ContainsKey_WithNonExistingKey_ShouldReturnFalse()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key", 42);

        Assert.False(dictionary.ContainsKey("other"));
    }

    [Fact]
    public void TryGetValue_WithExistingKey_ShouldReturnTrueAndValue()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key", 42);

        var result = dictionary.TryGetValue("key", out var value);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_WithNonExistingKey_ShouldReturnFalse()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key", 42);

        var result = dictionary.TryGetValue("other", out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Fact]
    public void Indexer_Get_WithExistingKey_ShouldReturnValue()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key", 42);

        var value = dictionary["key"];

        Assert.Equal(42, value);
    }

    [Fact]
    public void Indexer_Set_WithExistingKey_ShouldUpdateValue()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key", 42);

        dictionary["key"] = 99;

        Assert.Equal(99, dictionary["key"]);
    }

    [Fact]
    public void Indexer_Set_WithNewKey_ShouldAddItem()
    {
        var dictionary = new HybridDictionary<string, int>();

        dictionary["key"] = 42;

        Assert.Single(dictionary);
        Assert.Equal(42, dictionary["key"]);
    }

    [Fact]
    public void Clear_WithItems_ShouldRemoveAllItems()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);

        dictionary.Clear();

        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void Keys_ShouldReturnAllKeys()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);
        dictionary.Add("key3", 3);

        var keys = dictionary.Keys;

        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public void Values_ShouldReturnAllValues()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);
        dictionary.Add("key3", 3);

        var values = dictionary.Values;

        Assert.Equal(3, values.Count);
        Assert.Contains(1, values);
        Assert.Contains(2, values);
        Assert.Contains(3, values);
    }

    [Fact]
    public void GetEnumerator_ShouldEnumerateAllItems()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);
        dictionary.Add("key3", 3);

        var count = 0;
        foreach (var kvp in dictionary)
        {
            count++;
            Assert.True(kvp.Key.StartsWith("key"));
        }

        Assert.Equal(3, count);
    }

    [Fact]
    public void CopyTo_ShouldCopyAllItemsToArray()
    {
        var dictionary = new HybridDictionary<string, int>();
        dictionary.Add("key1", 1);
        dictionary.Add("key2", 2);

        var array = new KeyValuePair<string, int>[2];
        dictionary.CopyTo(array, 0);

        Assert.Equal(2, array.Length);
        Assert.Contains(new KeyValuePair<string, int>("key1", 1), array);
        Assert.Contains(new KeyValuePair<string, int>("key2", 2), array);
    }
}
