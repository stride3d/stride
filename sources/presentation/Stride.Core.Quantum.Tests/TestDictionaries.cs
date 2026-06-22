// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestDictionaries
{
    public class SimpleDictionaryContainer
    {
        public Dictionary<string, int> StringIntDict { get; set; } = [];
        public Dictionary<int, string> IntStringDict { get; set; } = [];
    }

    public class ObjectDictionaryContainer
    {
        public Dictionary<string, SimpleObject> ObjectDict { get; set; } = [];
    }

    public class SimpleObject
    {
        public string Name { get; set; }
        public override string ToString() => $"{{SimpleObject: {Name}}}";
    }

    [Theory]
    [InlineData(nameof(SimpleDictionaryContainer.StringIntDict))]
    [InlineData(nameof(SimpleDictionaryContainer.IntStringDict))]
    public void TestStringIntDictionary(string memberName)
    {
        var obj = new SimpleDictionaryContainer
        {
            StringIntDict = { ["key1"] = 10, ["key2"] = 20 },
            IntStringDict = { [1] = "value1", [2] = "value2" }
        };
        // The two indices we expect to find, depending on the dictionary key type.
        var expectedKeys = memberName == nameof(SimpleDictionaryContainer.StringIntDict)
            ? new object[] { "key1", "key2" }
            : new object[] { 1, 2 };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[memberName];

        Assert.NotNull(dictMemberNode);
        Assert.True(dictMemberNode.IsReference);
        Assert.NotNull(dictMemberNode.Target);
        Assert.NotNull(dictMemberNode.Target.Indices);
        Assert.Equal(2, dictMemberNode.Target.Indices.Count());
        // The actual keys must be present as indices, not merely the right count.
        foreach (var key in expectedKeys)
        {
            Assert.Contains(new NodeIndex(key), dictMemberNode.Target.Indices);
        }
    }

    [Theory]
    // String-keyed, int-valued dictionary.
    [InlineData(nameof(SimpleDictionaryContainer.StringIntDict), "key1", 42)]
    // Int-keyed, string-valued dictionary (exercises a different key/value type).
    [InlineData(nameof(SimpleDictionaryContainer.IntStringDict), 1, "updated")]
    public void TestDictionaryUpdate(string memberName, object key, object newValue)
    {
        var obj = new SimpleDictionaryContainer
        {
            StringIntDict = { ["key1"] = 10, ["key2"] = 20 },
            IntStringDict = { [1] = "value1", [2] = "value2" }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[memberName];

        dictMemberNode.Target.Update(newValue, new NodeIndex(key));

        Assert.Equal(newValue, dictMemberNode.Target.Retrieve(new NodeIndex(key)));
        if (memberName == nameof(SimpleDictionaryContainer.StringIntDict))
            Assert.Equal(newValue, obj.StringIntDict[(string)key]);
        else
            Assert.Equal(newValue, obj.IntStringDict[(int)key]);
    }

    [Fact]
    public void TestDictionaryAdd()
    {
        var obj = new SimpleDictionaryContainer
        {
            StringIntDict = { ["key1"] = 10 }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(SimpleDictionaryContainer.StringIntDict)];

        dictMemberNode.Target.Add(42, new NodeIndex("key2"));

        Assert.Equal(2, obj.StringIntDict.Count);
        Assert.Equal(42, obj.StringIntDict["key2"]);
        Assert.Equal(2, dictMemberNode.Target.Indices.Count());
    }

    [Fact]
    public void TestDictionaryRemove()
    {
        var obj = new SimpleDictionaryContainer
        {
            StringIntDict = { ["key1"] = 10, ["key2"] = 20, ["key3"] = 30 }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(SimpleDictionaryContainer.StringIntDict)];

        dictMemberNode.Target.Remove(20, new NodeIndex("key2"));

        Assert.Equal(2, obj.StringIntDict.Count);
        Assert.False(obj.StringIntDict.ContainsKey("key2"));
        Assert.Equal(2, dictMemberNode.Target.Indices.Count());
    }

    [Fact]
    public void TestObjectDictionary()
    {
        var item1 = new SimpleObject { Name = "Object1" };
        var item2 = new SimpleObject { Name = "Object2" };
        var obj = new ObjectDictionaryContainer
        {
            ObjectDict = { ["obj1"] = item1, ["obj2"] = item2 }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(ObjectDictionaryContainer.ObjectDict)];

        Assert.NotNull(dictMemberNode);
        Assert.True(dictMemberNode.IsReference);
        Assert.NotNull(dictMemberNode.Target.ItemReferences);
        Assert.Equal(2, dictMemberNode.Target.ItemReferences.Count);
    }

    [Fact]
    public void TestObjectDictionaryUpdate()
    {
        var item1 = new SimpleObject { Name = "Object1" };
        var item2 = new SimpleObject { Name = "Object2" };
        var item3 = new SimpleObject { Name = "Object3" };
        var obj = new ObjectDictionaryContainer
        {
            ObjectDict = { ["obj1"] = item1, ["obj2"] = item2 }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(ObjectDictionaryContainer.ObjectDict)];

        dictMemberNode.Target.Update(item3, new NodeIndex("obj1"));

        Assert.Equal(item3, obj.ObjectDict["obj1"]);
        Assert.Equal(item3, dictMemberNode.Target.Retrieve(new NodeIndex("obj1")));
    }
}
