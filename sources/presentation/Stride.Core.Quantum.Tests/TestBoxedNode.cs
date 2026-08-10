// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestBoxedNode
{
    public class ClassWithBoxedPrimitive
    {
        public object BoxedInt { get; set; }
        public object BoxedString { get; set; }
        public object BoxedStruct { get; set; }
    }

    public struct SimpleStruct
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }

    public class ClassWithBoxedList
    {
        public object BoxedList { get; set; }
    }

    [Fact]
    public void TestBoxedInt()
    {
        var obj = new ClassWithBoxedPrimitive { BoxedInt = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var boxedMemberNode = rootNode[nameof(ClassWithBoxedPrimitive.BoxedInt)];

        Assert.NotNull(boxedMemberNode);
        Assert.Equal(42, boxedMemberNode.Retrieve());
        Assert.True(boxedMemberNode.IsReference);

        var targetNode = boxedMemberNode.Target;
        Assert.NotNull(targetNode);
        Assert.IsType<BoxedNode>(targetNode);
        Assert.Equal(42, targetNode.Retrieve());
    }

    [Fact]
    public void TestBoxedIntUpdate()
    {
        var obj = new ClassWithBoxedPrimitive { BoxedInt = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var boxedMemberNode = rootNode[nameof(ClassWithBoxedPrimitive.BoxedInt)];
        var targetNode = boxedMemberNode.Target;

        // Update through the member node
        boxedMemberNode.Update(100);
        Assert.Equal(100, obj.BoxedInt);
        Assert.Equal(100, targetNode.Retrieve());
        Assert.Equal(100, boxedMemberNode.Retrieve());
    }

    [Fact]
    public void TestBoxedStruct()
    {
        var structValue = new SimpleStruct { Value = 42, Name = "test" };
        var obj = new ClassWithBoxedPrimitive { BoxedStruct = structValue };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var boxedMemberNode = rootNode[nameof(ClassWithBoxedPrimitive.BoxedStruct)];

        Assert.NotNull(boxedMemberNode);
        Assert.True(boxedMemberNode.IsReference);

        var targetNode = boxedMemberNode.Target;
        Assert.NotNull(targetNode);
        Assert.IsType<BoxedNode>(targetNode);

        var retrievedValue = (SimpleStruct)targetNode.Retrieve();
        Assert.Equal(42, retrievedValue.Value);
        Assert.Equal("test", retrievedValue.Name);
    }

    [Fact]
    public void TestBoxedStructMemberUpdate()
    {
        var structValue = new SimpleStruct { Value = 42, Name = "test" };
        var obj = new ClassWithBoxedPrimitive { BoxedStruct = structValue };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var boxedMemberNode = rootNode[nameof(ClassWithBoxedPrimitive.BoxedStruct)];
        var targetNode = boxedMemberNode.Target;

        // Update a member of the boxed struct
        var valueMemberNode = targetNode[nameof(SimpleStruct.Value)];
        valueMemberNode.Update(100);

        var retrievedValue = (SimpleStruct)obj.BoxedStruct;
        Assert.Equal(100, retrievedValue.Value);
        Assert.Equal("test", retrievedValue.Name);
    }

    [Fact]
    public void TestBoxedToString()
    {
        var obj = new ClassWithBoxedPrimitive { BoxedInt = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var boxedMemberNode = rootNode[nameof(ClassWithBoxedPrimitive.BoxedInt)];
        var targetNode = boxedMemberNode.Target as BoxedNode;

        Assert.NotNull(targetNode);
        var str = targetNode.ToString();
        Assert.Equal("{Node: Boxed Int32 = [42]}", str);
    }
}
