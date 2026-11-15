// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Presentation.Quantum.Tests;

public class TestNodeViewModelNames
{
    [Fact]
    public void TestMemberNamedItemKeepsItsName()
    {
        // The indexer of NodeViewModel is named Item, but a binding cannot use it by name: a member named Item is not escaped
        var viewModel = CreateViewModel();

        var child = viewModel.RootNode.GetChild(nameof(Types.ClassWithMembersNamedLikeTheViewModel.Item));

        Assert.NotNull(child);
        Assert.Equal("Item", child.Name);
        Assert.False(NodeViewModel.IsReserved("Item"));
    }

    [Fact]
    public void TestMemberNamedLikeAPropertyIsEscaped()
    {
        var viewModel = CreateViewModel();

        var child = viewModel.RootNode.GetChild(nameof(Types.ClassWithMembersNamedLikeTheViewModel.Children));

        Assert.NotNull(child);
        Assert.Equal("Children_", child.Name);
        Assert.True(NodeViewModel.IsReserved(nameof(NodeViewModel.Children)));
    }

    private static GraphViewModel CreateViewModel()
    {
        var testContext = new TestContainerContext();
        var instanceContext = testContext.CreateInstanceContext(new Types.ClassWithMembersNamedLikeTheViewModel { Item = "item", Children = "children" });
        return instanceContext.CreateViewModel();
    }
}
