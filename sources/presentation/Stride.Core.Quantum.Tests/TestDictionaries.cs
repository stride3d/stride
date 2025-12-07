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

    [Fact]
    public void TestStringIntDictionary()
    {
        var obj = new SimpleDictionaryContainer
        {
            StringIntDict = { ["key1"] = 10, ["key2"] = 20 }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(SimpleDictionaryContainer.StringIntDict)];

        Assert.NotNull(dictMemberNode);
        Assert.True(dictMemberNode.IsReference);
        Assert.NotNull(dictMemberNode.Target);
        Assert.NotNull(dictMemberNode.Target.Indices);
        Assert.Equal(2, dictMemberNode.Target.Indices.Count());
    }

    [Fact]
    public void TestDictionaryUpdate()
    {
        var obj = new SimpleDictionaryContainer
        {
            StringIntDict = { ["key1"] = 10, ["key2"] = 20 }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(SimpleDictionaryContainer.StringIntDict)];

        dictMemberNode.Target.Update(42, new NodeIndex("key1"));

        Assert.Equal(42, obj.StringIntDict["key1"]);
        Assert.Equal(42, dictMemberNode.Target.Retrieve(new NodeIndex("key1")));
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

    [Fact]
    public void TestIntStringDictionary()
    {
        var obj = new SimpleDictionaryContainer
        {
            IntStringDict = { [1] = "value1", [2] = "value2" }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(SimpleDictionaryContainer.IntStringDict)];

        Assert.NotNull(dictMemberNode);
        Assert.True(dictMemberNode.IsReference);
        Assert.NotNull(dictMemberNode.Target);
        Assert.Equal(2, dictMemberNode.Target.Indices.Count());
    }

    [Fact]
    public void TestIntStringDictionaryUpdate()
    {
        var obj = new SimpleDictionaryContainer
        {
            IntStringDict = { [1] = "value1", [2] = "value2" }
        };
        var nodeContainer = new NodeContainer();
        var containerNode = nodeContainer.GetOrCreateNode(obj);
        var dictMemberNode = containerNode[nameof(SimpleDictionaryContainer.IntStringDict)];

        dictMemberNode.Target.Update("updated", new NodeIndex(1));

        Assert.Equal("updated", obj.IntStringDict[1]);
        Assert.Equal("updated", dictMemberNode.Target.Retrieve(new NodeIndex(1)));
    }
}
