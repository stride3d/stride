// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestNodeContainer
{
    public class SimpleClass
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }

    public class ClassWithReference
    {
        public SimpleClass Reference { get; set; }
    }

    [Fact]
    public void TestGetOrCreateNode()
    {
        var container = new NodeContainer();
        var obj = new SimpleClass { Value = 42 };

        var node1 = container.GetOrCreateNode(obj);
        Assert.NotNull(node1);
        Assert.Equal(obj, node1.Retrieve());

        // Getting the same object should return the same node
        var node2 = container.GetOrCreateNode(obj);
        Assert.Equal(node1, node2);
        Assert.Equal(node1.Guid, node2.Guid);
    }

    [Fact]
    public void TestGetOrCreateNodeNull()
    {
        var container = new NodeContainer();
        var node = container.GetOrCreateNode(null);
        Assert.Null(node);
    }

    [Fact]
    public void TestGetNode()
    {
        var container = new NodeContainer();
        var obj = new SimpleClass { Value = 42 };

        // GetNode should return null if node doesn't exist yet
        var node1 = container.GetNode(obj);
        Assert.Null(node1);

        // Create the node
        container.GetOrCreateNode(obj);

        // Now GetNode should return the node
        var node2 = container.GetNode(obj);
        Assert.NotNull(node2);
        Assert.Equal(obj, node2.Retrieve());
    }

    [Fact]
    public void TestGetNodeNull()
    {
        var container = new NodeContainer();
        var node = container.GetNode(null);
        Assert.Null(node);
    }

    [Fact]
    public void TestClear()
    {
        var container = new NodeContainer();
        var obj = new SimpleClass { Value = 42 };

        var node1 = container.GetOrCreateNode(obj);
        Assert.NotNull(node1);

        container.Clear();

        // After clear, GetNode should return null
        var node2 = container.GetNode(obj);
        Assert.Null(node2);

        // GetOrCreateNode should create a new node with a different Guid
        var node3 = container.GetOrCreateNode(obj);
        Assert.NotNull(node3);
        Assert.NotEqual(node1.Guid, node3.Guid);
    }

    [Fact]
    public void TestReferencedObjectsAreCreated()
    {
        var container = new NodeContainer();
        var referenced = new SimpleClass { Value = 42 };
        var obj = new ClassWithReference { Reference = referenced };

        var node = container.GetOrCreateNode(obj);
        Assert.NotNull(node);

        // The referenced object should have its own node created
        var referencedNode = container.GetNode(referenced);
        Assert.NotNull(referencedNode);
        Assert.Equal(referenced, referencedNode.Retrieve());
    }

    [Fact]
    public void TestMultipleObjects()
    {
        var container = new NodeContainer();
        var obj1 = new SimpleClass { Value = 1 };
        var obj2 = new SimpleClass { Value = 2 };
        var obj3 = new SimpleClass { Value = 3 };

        var node1 = container.GetOrCreateNode(obj1);
        var node2 = container.GetOrCreateNode(obj2);
        var node3 = container.GetOrCreateNode(obj3);

        Assert.NotEqual(node1.Guid, node2.Guid);
        Assert.NotEqual(node2.Guid, node3.Guid);
        Assert.NotEqual(node1.Guid, node3.Guid);

        Assert.Equal(node1, container.GetNode(obj1));
        Assert.Equal(node2, container.GetNode(obj2));
        Assert.Equal(node3, container.GetNode(obj3));
    }
}
