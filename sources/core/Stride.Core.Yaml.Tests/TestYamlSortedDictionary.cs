// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using System.Linq;

namespace Stride.Core.Yaml.Tests;

public class TestYamlSortedDictionary
{
    [Fact]
    public void TestSortedDictionaryBasicOperations()
    {
        var dict = new SortedDictionary<string, int>();

        Assert.Empty(dict);

        dict.Add("banana", 2);
        dict.Add("apple", 1);

        Assert.Equal(2, dict.Count);
        Assert.Equal(1, dict["apple"]);
        Assert.Equal(2, dict["banana"]);
    }

    [Fact]
    public void TestSortedDictionaryMaintainsSortedOrder()
    {
        var dict = new SortedDictionary<string, int>();
        dict.Add("zebra", 3);
        dict.Add("apple", 1);
        dict.Add("mango", 2);

        var keys = dict.Keys.ToList();

        Assert.Equal("apple", keys[0]);
        Assert.Equal("mango", keys[1]);
        Assert.Equal("zebra", keys[2]);
    }

    [Fact]
    public void TestSortedDictionaryContainsKey()
    {
        var dict = new SortedDictionary<string, int>();
        dict.Add("key1", 100);

        Assert.True(dict.ContainsKey("key1"));
        Assert.False(dict.ContainsKey("key2"));
    }

    [Fact]
    public void TestSortedDictionaryRemove()
    {
        var dict = new SortedDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        Assert.True(dict.Remove("key1"));
        Assert.Single(dict);
        Assert.False(dict.ContainsKey("key1"));
    }

    [Fact]
    public void TestSortedDictionaryClear()
    {
        var dict = new SortedDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        dict.Clear();

        Assert.Empty(dict);
    }

    [Fact]
    public void TestSortedDictionaryTryGetValue()
    {
        var dict = new SortedDictionary<string, int>();
        dict.Add("key", 42);

        Assert.True(dict.TryGetValue("key", out var value));
        Assert.Equal(42, value);

        Assert.False(dict.TryGetValue("missing", out var missing));
        Assert.Equal(0, missing);
    }

    [Fact]
    public void TestSortedDictionaryWithNumbers()
    {
        var dict = new SortedDictionary<int, string>();
        dict.Add(3, "three");
        dict.Add(1, "one");
        dict.Add(2, "two");

        var keys = dict.Keys.ToList();

        Assert.Equal(1, keys[0]);
        Assert.Equal(2, keys[1]);
        Assert.Equal(3, keys[2]);
    }

    [Fact]
    public void TestSortedDictionaryValues()
    {
        var dict = new SortedDictionary<string, int>();
        dict.Add("b", 2);
        dict.Add("a", 1);

        var values = dict.Values.ToList();

        Assert.Equal(2, values.Count);
        Assert.Equal(1, values[0]);
        Assert.Equal(2, values[1]);
    }
}
