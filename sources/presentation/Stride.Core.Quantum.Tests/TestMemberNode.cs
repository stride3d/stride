// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestMemberNode
{
    public class SimpleClass
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public NestedClass Reference { get; set; }
    }

    public class NestedClass
    {
        public int Value { get; set; }
    }

    [Fact]
    public void TestMemberNodeProperties()
    {
        var obj = new SimpleClass { IntValue = 42 };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.IntValue)];

        Assert.NotNull(memberNode);
        Assert.Equal(nameof(SimpleClass.IntValue), memberNode.Name);
        Assert.Equal(parentNode, memberNode.Parent);
        Assert.Equal(42, memberNode.Retrieve());
        Assert.Equal(typeof(int), memberNode.Type);
        Assert.False(memberNode.IsReference);
        Assert.NotEqual(Guid.Empty, memberNode.Guid);
    }

    [Theory]
    // Value-type member.
    [InlineData(nameof(SimpleClass.IntValue), 100)]
    // Reference-type (string) member.
    [InlineData(nameof(SimpleClass.StringValue), "updated")]
    public void TestMemberNodeUpdate(string memberName, object newValue)
    {
        var obj = new SimpleClass { IntValue = 42, StringValue = "original" };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[memberName];

        memberNode.Update(newValue);

        Assert.Equal(newValue, memberNode.Retrieve());
        if (memberName == nameof(SimpleClass.IntValue))
            Assert.Equal(newValue, obj.IntValue);
        else
            Assert.Equal(newValue, obj.StringValue);
    }

    [Fact]
    public void TestMemberNodeReferenceUpdate()
    {
        var ref1 = new NestedClass { Value = 1 };
        var ref2 = new NestedClass { Value = 2 };
        var obj = new SimpleClass { Reference = ref1 };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.Reference)];

        Assert.True(memberNode.IsReference);
        Assert.Equal(ref1, memberNode.Retrieve());

        memberNode.Update(ref2);

        Assert.Equal(ref2, obj.Reference);
        Assert.Equal(ref2, memberNode.Retrieve());
    }

    [Fact]
    public void TestMemberNodeReferenceUpdateToNull()
    {
        var ref1 = new NestedClass { Value = 1 };
        var obj = new SimpleClass { Reference = ref1 };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.Reference)];

        memberNode.Update(null);

        Assert.Null(obj.Reference);
        Assert.Null(memberNode.Retrieve());
    }

    [Fact]
    public void TestMemberNodeValueChangedEvent()
    {
        var obj = new SimpleClass { IntValue = 42 };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.IntValue)];

        var eventRaised = false;
        object oldValue = null;
        object newValue = null;

        memberNode.ValueChanged += (sender, args) =>
        {
            eventRaised = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
        };

        memberNode.Update(100);

        Assert.True(eventRaised);
        Assert.Equal(42, oldValue);
        Assert.Equal(100, newValue);
    }

    [Fact]
    public void TestMemberNodeValueChangingEvent()
    {
        var obj = new SimpleClass { IntValue = 42 };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.IntValue)];

        var eventRaised = false;
        object oldValue = null;
        object newValue = null;
        object valueDuringChanging = null;

        memberNode.ValueChanging += (sender, args) =>
        {
            eventRaised = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
            // Capture the underlying value at the moment the event fires to prove
            // ValueChanging is raised BEFORE the member is mutated.
            valueDuringChanging = obj.IntValue;
        };

        memberNode.Update(100);

        Assert.True(eventRaised);
        Assert.Equal(42, oldValue);
        Assert.Equal(100, newValue);
        // The member must still hold its original value while ValueChanging is being raised.
        Assert.Equal(42, valueDuringChanging);
        // After Update completes, the new value must be applied.
        Assert.Equal(100, obj.IntValue);
    }

    [Fact]
    public void TestMemberNodeToString()
    {
        var obj = new SimpleClass { IntValue = 42 };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.IntValue)];

        var str = memberNode.ToString();
        Assert.Equal($"{{Node: Member {nameof(SimpleClass.IntValue)} = [42]}}", str);
    }

    [Fact]
    public void TestMemberNodeTarget()
    {
        var nested = new NestedClass { Value = 42 };
        var obj = new SimpleClass { Reference = nested };
        var nodeContainer = new NodeContainer();
        var parentNode = nodeContainer.GetOrCreateNode(obj);
        var memberNode = parentNode[nameof(SimpleClass.Reference)];

        Assert.NotNull(memberNode.Target);
        Assert.Equal(nested, memberNode.Target.Retrieve());
        Assert.IsAssignableFrom<IObjectNode>(memberNode.Target);
    }
}
