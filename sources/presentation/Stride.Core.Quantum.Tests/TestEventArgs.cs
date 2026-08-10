// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestEventArgs
{
    public class SimpleClass
    {
        public int Value { get; set; }
        public List<string> Items { get; set; } = [];
    }

    [Fact]
    public void TestMemberNodeChangeEventArgs()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = rootNode[nameof(SimpleClass.Value)];

        MemberNodeChangeEventArgs capturedArgs = null;
        memberNode.ValueChanged += (sender, args) =>
        {
            capturedArgs = args as MemberNodeChangeEventArgs;
        };

        memberNode.Update(100);

        Assert.NotNull(capturedArgs);
        Assert.Equal(ContentChangeType.ValueChange, capturedArgs.ChangeType);
        Assert.Equal(memberNode, capturedArgs.Member);
        Assert.Equal(42, capturedArgs.OldValue);
        Assert.Equal(100, capturedArgs.NewValue);
    }

    [Fact]
    public void TestItemChangeEventArgs()
    {
        var obj = new SimpleClass { Items = { "a", "b", "c" } };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var listMemberNode = rootNode[nameof(SimpleClass.Items)];

        ItemChangeEventArgs capturedArgs = null;
        listMemberNode.Target.ItemChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        listMemberNode.Target.Update("d", new NodeIndex(1));

        Assert.NotNull(capturedArgs);
        Assert.Equal(ContentChangeType.CollectionUpdate, capturedArgs.ChangeType);
        Assert.Equal(1, capturedArgs.Index.Int);
        Assert.Equal("b", capturedArgs.OldValue);
        Assert.Equal("d", capturedArgs.NewValue);
    }

    [Fact]
    public void TestItemChangeEventArgsAdd()
    {
        var obj = new SimpleClass { Items = { "a", "b" } };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var listMemberNode = rootNode[nameof(SimpleClass.Items)];

        ItemChangeEventArgs capturedArgs = null;
        listMemberNode.Target.ItemChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        listMemberNode.Target.Add("c");

        Assert.NotNull(capturedArgs);
        Assert.Equal(ContentChangeType.CollectionAdd, capturedArgs.ChangeType);
        Assert.Equal(2, capturedArgs.Index.Int);
        Assert.Null(capturedArgs.OldValue);
        Assert.Equal("c", capturedArgs.NewValue);
    }

    [Fact]
    public void TestItemChangeEventArgsRemove()
    {
        var obj = new SimpleClass { Items = { "a", "b", "c" } };
        var nodeContainer = new NodeContainer();
        var rootNode = nodeContainer.GetOrCreateNode(obj);
        var listMemberNode = rootNode[nameof(SimpleClass.Items)];

        ItemChangeEventArgs capturedArgs = null;
        listMemberNode.Target.ItemChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        listMemberNode.Target.Remove("b", new NodeIndex(1));

        Assert.NotNull(capturedArgs);
        Assert.Equal(ContentChangeType.CollectionRemove, capturedArgs.ChangeType);
        Assert.Equal(1, capturedArgs.Index.Int);
        Assert.Equal("b", capturedArgs.OldValue);
        Assert.Null(capturedArgs.NewValue);
    }
}
