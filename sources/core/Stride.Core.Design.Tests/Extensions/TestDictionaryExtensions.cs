// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for the <see cref="DictionaryExtensions"/> class.
/// </summary>
public class TestDictionaryExtensions
{
    [Fact]
    public void TryGetValue_WithExistingKey_ShouldReturnValue()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            { "key1", 42 },
            { "key2", 100 }
        };

        var result = dictionary.TryGetValue("key1");

        Assert.Equal(42, result);
    }

    [Fact]
    public void TryGetValue_WithNonExistingKey_ShouldReturnDefault()
    {
        IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>
        {
            { "key1", 42 }
        };

        var result = dictionary.TryGetValue("nonexistent");

        Assert.Equal(0, result);
    }

    [Fact]
    public void TryGetValue_WithNullableType_ShouldReturnNull()
    {
        IReadOnlyDictionary<string, string?> dictionary = new Dictionary<string, string?>
        {
            { "key1", "value1" }
        };

        var result = dictionary.TryGetValue("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void AddRange_WithMultipleItems_ShouldAddAllItems()
    {
        var dictionary = new Dictionary<string, int>();
        var itemsToAdd = new[]
        {
            new KeyValuePair<string, int>("key1", 1),
            new KeyValuePair<string, int>("key2", 2),
            new KeyValuePair<string, int>("key3", 3)
        };

        dictionary.AddRange(itemsToAdd);

        Assert.Equal(3, dictionary.Count);
        Assert.Equal(1, dictionary["key1"]);
        Assert.Equal(2, dictionary["key2"]);
        Assert.Equal(3, dictionary["key3"]);
    }

    [Fact]
    public void AddRange_WithEmptyEnumerable_ShouldNotModifyDictionary()
    {
        var dictionary = new Dictionary<string, int> { { "existing", 99 } };

        dictionary.AddRange(Enumerable.Empty<KeyValuePair<string, int>>());

        Assert.Single(dictionary);
        Assert.Equal(99, dictionary["existing"]);
    }

    [Fact]
    public void AddRange_WithDuplicateKey_ShouldThrowArgumentException()
    {
        var dictionary = new Dictionary<string, int> { { "key1", 1 } };
        var itemsToAdd = new[]
        {
            new KeyValuePair<string, int>("key1", 2)
        };

        Assert.Throws<ArgumentException>(() => dictionary.AddRange(itemsToAdd));
    }

    [Fact]
    public void Merge_WithNewKeys_ShouldAddItems()
    {
        var dictionary = new Dictionary<string, int> { { "key1", 1 } };
        var itemsToMerge = new[]
        {
            new KeyValuePair<string, int>("key2", 2),
            new KeyValuePair<string, int>("key3", 3)
        };

        dictionary.Merge(itemsToMerge);

        Assert.Equal(3, dictionary.Count);
        Assert.Equal(1, dictionary["key1"]);
        Assert.Equal(2, dictionary["key2"]);
        Assert.Equal(3, dictionary["key3"]);
    }

    [Fact]
    public void Merge_WithExistingKeys_ShouldOverwriteValues()
    {
        var dictionary = new Dictionary<string, int>
        {
            { "key1", 1 },
            { "key2", 2 }
        };
        var itemsToMerge = new[]
        {
            new KeyValuePair<string, int>("key1", 100),
            new KeyValuePair<string, int>("key2", 200)
        };

        dictionary.Merge(itemsToMerge);

        Assert.Equal(2, dictionary.Count);
        Assert.Equal(100, dictionary["key1"]);
        Assert.Equal(200, dictionary["key2"]);
    }

    [Fact]
    public void Merge_WithMixOfNewAndExistingKeys_ShouldMergeCorrectly()
    {
        var dictionary = new Dictionary<string, int> { { "key1", 1 } };
        var itemsToMerge = new[]
        {
            new KeyValuePair<string, int>("key1", 100),
            new KeyValuePair<string, int>("key2", 2)
        };

        dictionary.Merge(itemsToMerge);

        Assert.Equal(2, dictionary.Count);
        Assert.Equal(100, dictionary["key1"]);
        Assert.Equal(2, dictionary["key2"]);
    }

    [Fact]
    public void Merge_WithEmptyEnumerable_ShouldNotModifyDictionary()
    {
        var dictionary = new Dictionary<string, int> { { "existing", 99 } };

        dictionary.Merge(Enumerable.Empty<KeyValuePair<string, int>>());

        Assert.Single(dictionary);
        Assert.Equal(99, dictionary["existing"]);
    }
}
