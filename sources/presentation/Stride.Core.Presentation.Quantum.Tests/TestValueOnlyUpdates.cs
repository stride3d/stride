// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Tests;

/// <summary>
/// A change of the value only of a leaf (a value with no object node of its own, of the same type as before) updates its
/// presenter and its view model in place; any other change refreshes the node as before.
/// </summary>
public sealed class TestValueOnlyUpdates
{
    public enum Mode
    {
        First,
        Second,
    }

    public sealed class Leaves
    {
        public float Speed { get; set; } = 1;

        public string Label { get; set; } = "label";

        public string? Text { get; set; }

        public Mode Mode { get; set; }

        public bool Show { get; set; }

        public string Shown { get; set; } = "shown";

        public List<float> Values { get; set; } = [1, 2];

        public Inner Inner { get; set; } = new();

        public Inner Other { get; set; } = new();
    }

    public sealed class Inner
    {
        public float Value { get; set; }

        public float Extra { get; set; }
    }

    /// <summary>
    /// Counts the calls of the updaters.
    /// </summary>
    private sealed class CountingUpdater : NodePresenterUpdaterBase
    {
        public int Updates { get; private set; }

        public int Finalizations { get; private set; }

        public override void UpdateNode(INodePresenter node) => ++Updates;

        public override void FinalizeTree(INodePresenter root) => ++Finalizations;
    }

    [Fact]
    public void ALeafWriteDoesNotRebuildNorFinalizeTheTree()
    {
        var instance = new Leaves();
        var counter = new CountingUpdater();
        var root = CreatePresenter(instance, counter);
        var speed = root[nameof(Leaves.Speed)];
        var values = root[nameof(Leaves.Values)];
        var items = values.Children.ToList();
        var (updates, finalizations) = (counter.Updates, counter.Finalizations);
        var changes = new List<ValueChangedEventArgs>();
        speed.ValueChanged += (_, e) => changes.Add(e);

        speed.UpdateValue(2f);
        speed.UpdateValue(3f);
        // An int written to the float member is converted (as the int row of a float member writes it, and its undo and redo)
        speed.UpdateValue(4);
        speed.UpdateValue(3f);
        root[nameof(Leaves.Label)].UpdateValue("other");
        root[nameof(Leaves.Mode)].UpdateValue(Mode.Second);
        items[1].UpdateValue(5f);

        Assert.Equal((3f, "other", Mode.Second, 5f), (instance.Speed, instance.Label, instance.Mode, instance.Values[1]));
        Assert.Equal((3f, 5f), ((float)speed.Value!, (float)items[1].Value!));
        Assert.Same(speed, root[nameof(Leaves.Speed)]);
        Assert.Equal(items, values.Children);
        Assert.All(changes, x => Assert.True(x.IsValueOnly));
        Assert.Equal(4, changes.Count);
        Assert.Equal((updates, finalizations), (counter.Updates, counter.Finalizations));
    }

    [Fact]
    public void AWriteThatChangesTheStructureStillRefreshes()
    {
        var instance = new Leaves();
        var counter = new CountingUpdater();
        var root = CreatePresenter(instance, counter);
        var inner = root[nameof(Leaves.Inner)];
        var innerValue = inner[nameof(Inner.Value)];
        var values = root[nameof(Leaves.Values)];
        var items = values.Children.ToList();
        var changes = new List<ValueChangedEventArgs>();
        inner.ValueChanged += (_, e) => changes.Add(e);
        root[nameof(Leaves.Text)].ValueChanged += (_, e) => changes.Add(e);
        var finalizations = counter.Finalizations;

        // An object replaced by another one: new children
        inner.UpdateValue(new Inner { Value = 4 });
        Assert.NotSame(innerValue, inner[nameof(Inner.Value)]);
        Assert.Equal(4f, inner[nameof(Inner.Value)].Value);
        // A null value that becomes a value
        root[nameof(Leaves.Text)].UpdateValue("text");
        // An item added: the collection is rebuilt
        values.AddItem(3f, new NodeIndex(2));
        Assert.Equal(3, values.Children.Count);
        Assert.NotSame(items[0], values.Children[0]);

        Assert.Equal(2, changes.Count);
        Assert.All(changes, x => Assert.False(x.IsValueOnly));
        Assert.Equal(finalizations + 3, counter.Finalizations);
    }

    [Fact]
    public void ANodeThatAsksForItIsRefreshedByAValueChange()
    {
        // As the updater of the particle transforms: the value of a toggle shows or hides another member, in FinalizeTree
        var instance = new Leaves();
        var root = CreatePresenter(instance, new ToggleUpdater());
        Assert.False(root[nameof(Leaves.Shown)].IsVisible);

        root[nameof(Leaves.Show)].UpdateValue(true);
        Assert.True(root[nameof(Leaves.Shown)].IsVisible);

        root[nameof(Leaves.Show)].UpdateValue(false);
        Assert.False(root[nameof(Leaves.Shown)].IsVisible);
    }

    [Fact]
    public void ADependentIsRefreshedByAValueChangeOfItsDependency()
    {
        // As the updaters of the textures, animations and cameras: a member shown or hidden by the value of another one, which it
        // depends on
        var instance = new Leaves();
        var root = CreatePresenter(instance, new DependentUpdater());
        Assert.False(root[nameof(Leaves.Shown)].IsVisible);

        root[nameof(Leaves.Show)].UpdateValue(true);
        Assert.True(root[nameof(Leaves.Shown)].IsVisible);

        root[nameof(Leaves.Show)].UpdateValue(false);
        Assert.False(root[nameof(Leaves.Shown)].IsVisible);
    }

    [Fact]
    public void AValueDependencyFollowsTheValueWithoutBeingRefreshed()
    {
        // As an inlined member: the parent shows the value of one of its members
        var instance = new Leaves();
        var counter = new CountingUpdater();
        var root = CreatePresenter(instance, new ValueDependencyUpdater(), counter);
        var inner = root[nameof(Leaves.Inner)];
        var value = inner[nameof(Inner.Value)];
        var extra = inner[nameof(Inner.Extra)];
        var changes = new List<ValueChangedEventArgs>();
        inner.ValueChanged += (_, e) => changes.Add(e);
        var finalizations = counter.Finalizations;

        for (var i = 1; i <= 3; i++)
        {
            value.UpdateValue((float)i);
            Assert.Same(value, inner[nameof(Inner.Value)]);
            Assert.Same(extra, inner[nameof(Inner.Extra)]);
            Assert.Equal((float)i, inner[nameof(Inner.Value)].Value);
        }
        Assert.Equal(3, changes.Count);
        Assert.All(changes, x => Assert.True(x.IsValueOnly));
        Assert.Equal(finalizations, counter.Finalizations);

        // A change of structure of the parent still refreshes it, and the dependency follows the new node
        inner.UpdateValue(new Inner { Value = 10 });
        Assert.False(changes[^1].IsValueOnly);
        inner[nameof(Inner.Value)].UpdateValue(11f);
        Assert.True(changes[^1].IsValueOnly);
        Assert.Equal(11f, instance.Inner.Value);
    }

    [Fact]
    public void AValueOnlyChangeKeepsTheViewModelsAndNotifiesTheValue()
    {
        var instance = new Leaves();
        var context = new TestContainerContext();
        context.GraphViewModelService.AvailableUpdaters.Add(new HalfUpdater());
        var viewModel = context.CreateInstanceContext(instance).CreateViewModel();
        var root = viewModel.RootNode;
        var speed = root.GetChild(nameof(Leaves.Speed))!;
        var half = speed.GetChild(HalfUpdater.Name)!;
        var values = root.GetChild(nameof(Leaves.Values))!;
        var item = values.Children.Single(x => x.Name == "1");
        var notified = new List<string>();
        void Record(object? sender, PropertyChangedEventArgs e) => notified.Add($"{((NodeViewModel)sender!).Name}.{e.PropertyName}");
        speed.PropertyChanged += Record;
        half.PropertyChanged += Record;
        item.PropertyChanged += Record;

        speed.NodeValue = 4f;
        item.NodeValue = 6f;

        Assert.Same(speed, root.GetChild(nameof(Leaves.Speed)));
        Assert.Same(half, speed.GetChild(HalfUpdater.Name));
        Assert.Same(item, values.Children.Single(x => x.Name == "1"));
        Assert.Equal((4f, 2f, 6f), ((float)speed.NodeValue!, (float)half.NodeValue!, (float)item.NodeValue!));
        Assert.Contains($"{nameof(Leaves.Speed)}.{nameof(NodeViewModel.NodeValue)}", notified);
        Assert.Contains($"{HalfUpdater.Name}.{nameof(NodeViewModel.NodeValue)}", notified);
        Assert.Contains($"1.{nameof(NodeViewModel.NodeValue)}", notified);
    }

    private static INodePresenter CreatePresenter(object instance, params INodePresenterUpdater[] updaters)
    {
        var context = new TestContainerContext();
        context.GraphViewModelService.AvailableUpdaters.AddRange(updaters);
        var rootNode = context.NodeContainer.GetOrCreateNode(instance);
        return context.GraphViewModelService.NodePresenterFactory.CreateNodeHierarchy(rootNode, new GraphNodePath(rootNode));
    }

    /// <summary>
    /// Shows <see cref="Leaves.Shown"/> when <see cref="Leaves.Show"/> is set, in <see cref="INodePresenterUpdater.FinalizeTree"/>.
    /// </summary>
    private sealed class ToggleUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            if (node.Name == nameof(Leaves.Show))
                node.RefreshOnValueChange = true;
        }

        public override void FinalizeTree(INodePresenter root)
        {
            if (root.TryGetChild(nameof(Leaves.Show)) is { } show)
                root[nameof(Leaves.Shown)].IsVisible = (bool)show.Value!;
        }
    }

    /// <summary>
    /// Shows <see cref="Leaves.Shown"/> when <see cref="Leaves.Show"/> is set, in <see cref="INodePresenterUpdater.UpdateNode"/>,
    /// with a dependency.
    /// </summary>
    private sealed class DependentUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            if (node.Name == nameof(Leaves.Shown))
                node.IsVisible = (bool)node.Parent![nameof(Leaves.Show)].Value!;
        }

        public override void FinalizeTree(INodePresenter root)
        {
            if (root.TryGetChild(nameof(Leaves.Shown)) is { } shown)
                shown.AddDependency(root[nameof(Leaves.Show)], false);
        }
    }

    /// <summary>
    /// Makes <see cref="Leaves.Inner"/> show the value of its <see cref="Inner.Value"/>, as an inlined member.
    /// </summary>
    private sealed class ValueDependencyUpdater : NodePresenterUpdaterBase
    {
        public override void FinalizeTree(INodePresenter root)
        {
            if (root.TryGetChild(nameof(Leaves.Inner)) is { } inner)
                inner.AddValueDependency(inner[nameof(Inner.Value)]);
        }
    }

    /// <summary>
    /// Adds a virtual child to <see cref="Leaves.Speed"/> that shows half its value, as the components of a vector are shown.
    /// </summary>
    private sealed class HalfUpdater : NodePresenterUpdaterBase
    {
        public const string Name = "Half";

        public override void UpdateNode(INodePresenter node)
        {
            if (node.Name == nameof(Leaves.Speed) && node.TryGetChild(Name) is null)
            {
                var half = new VirtualNodePresenter((INodePresenterFactoryInternal)node.Factory, node.PropertyProvider, node, Name, typeof(float), 0, () => (float)node.Value! / 2, _ => { });
                half.ChangeParent(node);
            }
        }
    }
}
