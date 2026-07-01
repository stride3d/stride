// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestNodeAccessor
{
    public class SimpleClass
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }

    public class ListContainer
    {
        public List<int> Numbers { get; set; } = [];
        public List<SimpleClass> Objects { get; set; } = [];
    }

    [Fact]
    public void TestMemberAccessor()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Value)];

        var accessor = new NodeAccessor(memberNode, NodeIndex.Empty);

        Assert.True(accessor.IsMember);
        Assert.False(accessor.IsItem);
        Assert.Equal(memberNode, accessor.Node);
        Assert.True(accessor.Index.IsEmpty);
        Assert.Equal(42, accessor.RetrieveValue());
    }

    [Fact]
    public void TestMemberAccessorUpdate()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Value)];

        var accessor = new NodeAccessor(memberNode, NodeIndex.Empty);
        accessor.UpdateValue(100);

        Assert.Equal(100, obj.Value);
        Assert.Equal(100, accessor.RetrieveValue());
    }

    [Fact]
    public void TestItemAccessor()
    {
        var obj = new ListContainer { Numbers = { 1, 2, 3 } };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var listMemberNode = rootNode[nameof(ListContainer.Numbers)];

        var accessor = new NodeAccessor(listMemberNode.Target, new NodeIndex(1));

        Assert.False(accessor.IsMember);
        Assert.True(accessor.IsItem);
        Assert.Equal(listMemberNode.Target, accessor.Node);
        Assert.Equal(1, accessor.Index.Int);
        Assert.Equal(2, accessor.RetrieveValue());
    }

    [Fact]
    public void TestItemAccessorUpdate()
    {
        var obj = new ListContainer { Numbers = { 1, 2, 3 } };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var listMemberNode = rootNode[nameof(ListContainer.Numbers)];

        var accessor = new NodeAccessor(listMemberNode.Target, new NodeIndex(1));
        accessor.UpdateValue(42);

        Assert.Equal(42, obj.Numbers[1]);
        Assert.Equal(42, accessor.RetrieveValue());
    }

    [Fact]
    public void TestInvalidMemberAccessorWithIndex()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Value)];

        Assert.Throws<ArgumentException>(() => new NodeAccessor(memberNode, new NodeIndex(0)));
    }

    [Fact]
    public void TestNullNodeThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new NodeAccessor(null!, NodeIndex.Empty));
    }

    [Fact]
    public void TestAcceptType()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Value)];

        var accessor = new NodeAccessor(memberNode, NodeIndex.Empty);

        Assert.True(accessor.AcceptType(typeof(int)));
        Assert.False(accessor.AcceptType(typeof(string)));
    }

    [Fact]
    public void TestAcceptValue()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Value)];

        var accessor = new NodeAccessor(memberNode, NodeIndex.Empty);

        Assert.True(accessor.AcceptValue(100));
        Assert.False(accessor.AcceptValue("not an int"));
    }

    [Fact]
    public void TestAcceptValueNull()
    {
        var obj = new SimpleClass { Name = "test" };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Name)];

        var accessor = new NodeAccessor(memberNode, NodeIndex.Empty);

        // String is a reference type, so null is accepted
        Assert.True(accessor.AcceptValue(null));

        // Int is a value type, so null is not accepted
        var intMemberNode = rootNode[nameof(SimpleClass.Value)];
        var intAccessor = new NodeAccessor(intMemberNode, NodeIndex.Empty);
        Assert.False(intAccessor.AcceptValue(null));
    }
}
