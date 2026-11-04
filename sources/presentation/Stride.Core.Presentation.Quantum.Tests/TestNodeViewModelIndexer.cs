// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Presentation.Quantum.Tests;

/// <summary>
/// Bindings to the indexer of a <see cref="NodeViewModel"/> (the Avalonia property grid) follow its children.
/// </summary>
public sealed class TestNodeViewModelIndexer
{
    [Fact]
    public void ReplacingAnObjectRaisesOneIndexerChangeWithTheNewChildren()
    {
        var instance = new Types.ClassWithRef { String = "aaa", Ref = new Types.ClassWithRef { String = "bbb" } };
        var viewModel = new TestContainerContext().CreateInstanceContext(instance).CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithRef.Ref))!;
        var oldChild = node[nameof(Types.ClassWithRef.String)];
        var seenChildren = new List<object?>();
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "Item")
                seenChildren.Add(node[nameof(Types.ClassWithRef.String)]);
        };

        node.NodeValue = new Types.ClassWithRef { String = "ccc" };

        // One change, once the new child is there: a binding never sees the node without its child
        var newChild = Assert.IsType<NodeViewModel>(Assert.Single(seenChildren));
        Assert.NotSame(oldChild, newChild);
        Assert.Equal("ccc", newChild.NodeValue);
    }

    [Fact]
    public void AMemberNamedItemWaitsForTheIndexerChangeOfARefresh()
    {
        // A member named Item keeps its name, so its own changes are named like the indexer: in the middle of a refresh, they
        // would tell the bindings to the indexer that the node has no children
        var instance = new Types.ClassWithRefToMembersNamedLikeTheViewModel { Ref = new() { Item = "aaa", Children = "bbb" } };
        var viewModel = new TestContainerContext().CreateInstanceContext(instance).CreateViewModel();
        var node = viewModel.RootNode.GetChild(nameof(Types.ClassWithRefToMembersNamedLikeTheViewModel.Ref))!;
        var oldChild = node["Item"];
        var seenChildren = new List<object?>();
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "Item")
                seenChildren.Add(node["Item"]);
        };

        node.NodeValue = new Types.ClassWithMembersNamedLikeTheViewModel { Item = "ccc", Children = "ddd" };

        // One change, once the new child is there; a binding to the member Item (WPF) gets it too
        var newChild = Assert.IsType<NodeViewModel>(Assert.Single(seenChildren));
        Assert.NotSame(oldChild, newChild);
        Assert.Equal("Item", newChild.Name);
        Assert.Equal("ccc", newChild.NodeValue);
    }
}
