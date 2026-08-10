// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestObjectNode
{
    public class SimpleClass
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public NestedClass Nested { get; set; }
    }

    public class NestedClass
    {
        public int Value { get; set; }
    }

    public class ListContainer
    {
        public List<int> Numbers { get; set; } = [];
        public List<SimpleClass> Objects { get; set; } = [];
    }

    [Fact]
    public void TestObjectNodeProperties()
    {
        var obj = new SimpleClass { IntValue = 42, StringValue = "test" };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        Assert.NotNull(node);
        Assert.Equal(obj, node.Retrieve());
        Assert.Equal(typeof(SimpleClass), node.Type);
        Assert.NotEqual(Guid.Empty, node.Guid);
        Assert.False(node.IsReference);
        Assert.Null(node.ItemReferences);
        Assert.Null(node.Indices);
        Assert.NotEmpty(node.Members);
        Assert.Equal(3, node.Members.Count);
    }

    [Fact]
    public void TestObjectNodeMembers()
    {
        var obj = new SimpleClass { IntValue = 42, StringValue = "test" };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        var intMember = node[nameof(SimpleClass.IntValue)];
        Assert.NotNull(intMember);
        Assert.Equal(42, intMember.Retrieve());
        Assert.IsAssignableFrom<IMemberNode>(intMember);

        var stringMember = node[nameof(SimpleClass.StringValue)];
        Assert.NotNull(stringMember);
        Assert.Equal("test", stringMember.Retrieve());
        Assert.IsAssignableFrom<IMemberNode>(stringMember);
    }

    [Fact]
    public void TestObjectNodeWithCollection()
    {
        var obj = new ListContainer { Numbers = { 1, 2, 3 } };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);
        var listMember = node[nameof(ListContainer.Numbers)];

        Assert.True(listMember.IsReference);
        Assert.NotNull(listMember.Target);
        Assert.NotNull(listMember.Target.Indices);
        Assert.Equal(3, listMember.Target.Indices.Count());
    }

    [Fact]
    public void TestObjectNodeCollectionUpdate()
    {
        var obj = new ListContainer { Numbers = { 1, 2, 3 } };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);
        var listMember = node[nameof(ListContainer.Numbers)];

        listMember.Target.Update(42, new NodeIndex(1));

        Assert.Equal(42, obj.Numbers[1]);
        Assert.Equal(42, listMember.Target.Retrieve(new NodeIndex(1)));
    }

    [Fact]
    public void TestObjectNodeCollectionAdd()
    {
        var obj = new ListContainer { Numbers = { 1, 2, 3 } };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);
        var listMember = node[nameof(ListContainer.Numbers)];

        listMember.Target.Add(42);

        Assert.Equal(4, obj.Numbers.Count);
        Assert.Equal(42, obj.Numbers[3]);
        Assert.Equal(4, listMember.Target.Indices.Count());
    }

    [Fact]
    public void TestObjectNodeCollectionRemove()
    {
        var obj = new ListContainer { Numbers = { 1, 2, 3 } };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);
        var listMember = node[nameof(ListContainer.Numbers)];

        listMember.Target.Remove(2, new NodeIndex(1));

        Assert.Equal(2, obj.Numbers.Count);
        Assert.Equal(1, obj.Numbers[0]);
        Assert.Equal(3, obj.Numbers[1]);
        Assert.Equal(2, listMember.Target.Indices.Count());
    }

    [Fact]
    public void TestObjectNodeToString()
    {
        var obj = new SimpleClass { IntValue = 42, StringValue = "test" };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        var str = node.ToString();
        Assert.Equal($"{{Node: Object {nameof(SimpleClass)} = [{obj}]}}", str);
    }
}
