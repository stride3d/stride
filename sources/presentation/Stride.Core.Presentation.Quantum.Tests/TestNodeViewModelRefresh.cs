// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Presentation.Quantum.Tests;

public class TestNodeViewModelRefresh
{
    private const string Name = nameof(Types.SimpleObject.Name);
    private const string MinimumKey = "Minimum";

    private static readonly PropertyKey<double> Minimum = new PropertyKey<double>(MinimumKey, typeof(TestNodeViewModelRefresh));

    private class MinimumUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            if (node.Name == Name)
            {
                node.AttachedProperties.Set(Minimum, 0.01);
            }
        }
    }

    [Fact]
    public void TestValueChangeDuringRefresh()
    {
        var testContext = new TestContainerContext();
        testContext.GraphViewModelService.AvailableUpdaters.Add(new MinimumUpdater());
        var instanceContext = testContext.CreateInstanceContext(new Types.SimpleObject { Name = "Initial" });
        var viewModel = instanceContext.CreateViewModel();
        var node = viewModel.RootNode.GetChild(Name)!;

        // Simulate the user interface writing a value back to the node while it is being refreshed
        // (this is what the WPF bindings do when a new value is pasted on a ranged numeric property).
        // This re-enters NodeViewModel.Refresh()
        var valueWrittenBack = false;
        void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!valueWrittenBack)
            {
                valueWrittenBack = true;
                node.NodeValue = "WrittenBack";
            }
        }

        node.PropertyChanged += OnNodePropertyChanged;
        try
        {
            node.NodeValue = "Changed";
        }
        finally
        {
            node.PropertyChanged -= OnNodePropertyChanged;
        }

        Assert.True(valueWrittenBack);
        Assert.Equal("WrittenBack", node.NodeValue);
        Assert.Single(node.AssociatedData);
        Assert.Equal(0.01, (double)node.AssociatedData[MinimumKey]);
    }

    [Fact]
    public void TestValueChangeDoesNotFlapAssociatedData()
    {
        var testContext = new TestContainerContext();
        testContext.GraphViewModelService.AvailableUpdaters.Add(new MinimumUpdater());
        var instanceContext = testContext.CreateInstanceContext(new Types.SimpleObject { Name = "Initial" });
        var viewModel = instanceContext.CreateViewModel();
        var node = viewModel.RootNode.GetChild(Name)!;

        var notifiedProperties = new HashSet<string>();
        node.PropertyChanged += (_, e) => notifiedProperties.Add(e.PropertyName!);

        node.NodeValue = "Changed";

        Assert.Equal("Changed", node.NodeValue);
        Assert.Single(node.AssociatedData);
        Assert.Equal(0.01, (double)node.AssociatedData[MinimumKey]);
        Assert.Contains(nameof(NodeViewModel.NodeValue), notifiedProperties);
        Assert.DoesNotContain(MinimumKey, notifiedProperties);
        Assert.DoesNotContain(GraphViewModel.HasAssociatedDataPrefix + MinimumKey, notifiedProperties);
    }

    [Fact]
    public void TestAssociatedDataChangeIsStillNotified()
    {
        var testContext = new TestContainerContext();
        testContext.GraphViewModelService.AvailableUpdaters.Add(new MinimumUpdater());
        var instanceContext = testContext.CreateInstanceContext(new Types.SimpleObject { Name = "Initial" });
        var viewModel = instanceContext.CreateViewModel();
        var node = viewModel.RootNode.GetChild(Name)!;

        Assert.Equal(0.01, (double)node.AssociatedData[MinimumKey]);

        var notifiedProperties = new HashSet<string>();
        node.PropertyChanged += (_, e) => notifiedProperties.Add(e.PropertyName!);

        node.NodePresenters.First().AttachedProperties.Set(Minimum, 2.0);

        Assert.Equal(2.0, (double)node.AssociatedData[MinimumKey]);
        Assert.Contains(MinimumKey, notifiedProperties);
        Assert.Contains(GraphViewModel.HasAssociatedDataPrefix + MinimumKey, notifiedProperties);
    }
}
