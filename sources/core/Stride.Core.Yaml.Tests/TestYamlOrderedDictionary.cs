// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using System.Linq;

namespace Stride.Core.Yaml.Tests;

public class TestYamlOrderedDictionary
{
    [Fact]
    public void TestOrderedDictionaryBasicOperations()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();

        Assert.Empty(dict);

        dict.Add("first", 1);
        dict.Add("second", 2);

        Assert.Equal(2, dict.Count);
        Assert.Equal(1, dict["first"]);
        Assert.Equal(2, dict["second"]);
    }

    [Fact]
    public void TestOrderedDictionaryPreservesInsertionOrder()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("third", 3);
        dict.Add("first", 1);
        dict.Add("second", 2);

        var keys = dict.Keys.ToList();

        Assert.Equal("third", keys[0]);
        Assert.Equal("first", keys[1]);
        Assert.Equal("second", keys[2]);
    }

    [Fact]
    public void TestOrderedDictionaryContainsKey()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("key1", 100);

        Assert.True(dict.ContainsKey("key1"));
        Assert.False(dict.ContainsKey("key2"));
    }

    [Fact]
    public void TestOrderedDictionaryRemove()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        Assert.True(dict.Remove("key1"));
        Assert.Single(dict);
        Assert.False(dict.ContainsKey("key1"));
    }

    [Fact]
    public void TestOrderedDictionaryClear()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        dict.Clear();

        Assert.Empty(dict);
    }

    [Fact]
    public void TestOrderedDictionaryIndexAccess()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("a", 1);
        dict.Add("b", 2);

        Assert.Equal("a", dict[0].Key);
        Assert.Equal(1, dict[0].Value);
        Assert.Equal("b", dict[1].Key);
        Assert.Equal(2, dict[1].Value);
    }

    [Fact]
    public void TestOrderedDictionaryTryGetValue()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("key", 42);

        Assert.True(dict.TryGetValue("key", out var value));
        Assert.Equal(42, value);

        Assert.False(dict.TryGetValue("missing", out var missing));
        Assert.Equal(0, missing);
    }

    [Fact]
    public void TestOrderedDictionaryIndexOf()
    {
        var dict = new Stride.Core.Yaml.Serialization.OrderedDictionary<string, int>();
        dict.Add("first", 1);
        dict.Add("second", 2);

        Assert.Equal(0, dict.IndexOf("first"));
        Assert.Equal(1, dict.IndexOf("second"));
        Assert.Equal(-1, dict.IndexOf("notfound"));
    }
}
