// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Tests.Helpers;

namespace Stride.Core.Presentation.Quantum.Tests;

/// <summary>
/// Tests for editing nullable value type members through the <c>NodeViewModel</c> (the path used by the
/// property grid of GameStudio). See https://github.com/stride3d/stride/issues/3014
/// </summary>
public class TestNullableMemberValues
{
    private static TestInstanceContext BuildContext(object instance)
    {
        var context = new TestContainerContext();
        return context.CreateInstanceContext(instance);
    }

    [Fact]
    public void TestDoubleIsConvertedToNullableFloat()
    {
        // The numeric text box of the property grid supplies a boxed double
        var instance = new Types.ClassWithNullableMembers { NullableFloat = 1.5f };
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        node.NodeValue = 2.25;

        Assert.Equal(2.25f, instance.NullableFloat);
    }

    [Fact]
    public void TestBoxedFloatIsConvertedToNullableFloat()
    {
        var instance = new Types.ClassWithNullableMembers { NullableFloat = 1.5f };
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        node.NodeValue = 2.5f;

        Assert.Equal(2.5f, instance.NullableFloat);
    }

    [Fact]
    public void TestDoubleIsConvertedToNullableInt()
    {
        var instance = new Types.ClassWithNullableMembers { NullableInt = 1 };
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableInt))!;

        node.NodeValue = 3.0;

        Assert.Equal(3, instance.NullableInt);
    }

    [Fact]
    public void TestBoxedDoubleIsConvertedToNullableDouble()
    {
        var instance = new Types.ClassWithNullableMembers { NullableDouble = 1.5 };
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableDouble))!;

        node.NodeValue = 4.5;

        Assert.Equal(4.5, instance.NullableDouble);
    }

    [Fact]
    public void TestStringIsConvertedToNullableFloat()
    {
        var instance = new Types.ClassWithNullableMembers();
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        node.NodeValue = "3.5";

        Assert.Equal(3.5f, instance.NullableFloat);
    }

    [Fact]
    public void TestNullRemovesTheNullableValue()
    {
        var instance = new Types.ClassWithNullableMembers { NullableFloat = 1.5f };
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        Assert.Equal(1.5f, node.NodeValue);

        // The user cleared the value (e.g. removed the text in the numeric text box)
        node.NodeValue = null;

        Assert.Null(instance.NullableFloat);
        Assert.Null(node.NodeValue);
    }

    [Fact]
    public void TestNullKeepsANullNullableValue()
    {
        var instance = new Types.ClassWithNullableMembers();
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        Assert.Null(node.NodeValue);

        node.NodeValue = null;

        Assert.Null(instance.NullableFloat);
    }

    [Fact]
    public void TestTheNullableTypeIsReportedByTheNode()
    {
        var instance = new Types.ClassWithNullableMembers();
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        // The editor templates rely on the node type to know whether an empty text can be committed as null
        Assert.Equal(typeof(float?), node.Type);
    }

    [Fact]
    public void TestNodeValueRoundTripsThroughTheViewModel()
    {
        var instance = new Types.ClassWithNullableMembers();
        var context = BuildContext(instance);
        var viewModel = context.CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithNullableMembers.NullableFloat))!;

        node.NodeValue = 2.5;
        Assert.Equal(2.5f, node.NodeValue);
        Assert.Equal(2.5f, instance.NullableFloat);

        node.NodeValue = null;
        Assert.Null(node.NodeValue);
        Assert.Null(instance.NullableFloat);
    }
}
