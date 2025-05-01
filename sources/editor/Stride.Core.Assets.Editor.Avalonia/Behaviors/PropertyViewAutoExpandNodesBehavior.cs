// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors;

public sealed class PropertyViewAutoExpandNodesBehavior : StyledElementBehavior<PropertyView>
{
    private readonly List<PropertyViewItem> expandedItems = [];
    // These are static so that we remember their state for the entire session.
    private static readonly HashSet<string> expandedPropertyPaths = [];
    private static readonly HashSet<string> collapsedPropertyPaths = [];

    /// <summary>
    /// Identifies the <see cref="ViewModel"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<GraphViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<PropertyViewAutoExpandNodesBehavior, GraphViewModel?>(nameof(ViewModel));

    static PropertyViewAutoExpandNodesBehavior()
    {
        ViewModelProperty.Changed.AddClassHandler<PropertyViewAutoExpandNodesBehavior, GraphViewModel?>(OnViewModelChanged);
    }

    /// <summary>
    /// Gets or sets the <see cref="GraphViewModel"/> associated to this behavior.
    /// </summary>
    public GraphViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (ViewModel is not null)
        {
            ViewModel.NodeValueChanged += NodeValueChanged;
        }
        if (AssociatedObject is { } propertyView)
        {
            propertyView.PrepareItem += PrepareItem;
            propertyView.ClearItem += ClearItem;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } propertyView)
        {
            propertyView.ClearItem -= ClearItem;
            propertyView.PrepareItem -= PrepareItem;
        }
        if (ViewModel is not null)
        {
            ViewModel.NodeValueChanged -= NodeValueChanged;
        }
    }

    private void ClearItem(object? sender, PropertyViewItemEventArgs e)
    {
        expandedItems.Clear();
        UnregisterItem(e.Container);
    }

    private static void ExpandedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not PropertyViewItem item)
            return;

        var propertyPath = GetNode(item)!.DisplayPath;
        if (item.IsExpanded)
        {
            expandedPropertyPaths.Add(propertyPath);
            collapsedPropertyPaths.Remove(propertyPath);
        }
        else
        {
            expandedPropertyPaths.Remove(propertyPath);
            collapsedPropertyPaths.Add(propertyPath);
        }
    }

    private void ExpandSingleProperties(PropertyViewItem item)
    {
        var node = GetNode(item);
        // The data context of the item might be a "disconnected object"
        if (node is null)
            return;

        var rule = GetRule(node);

        if (node.Parent is not null)
        {
            switch (rule)
            {
                case ExpandRule.Always:
                    // Always expand nodes that have this rule (without tracking them)
                    item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                    break;

                case ExpandRule.Never:
                    // Always collapse nodes that have this rule (without tracking them)
                    item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, false);
                    break;

                case ExpandRule.Once:
                    {
                        // Expand nodes that have this rule only if they have never been collapsed previously
                        var propertyPath = node.DisplayPath;
                        if (!collapsedPropertyPaths.Contains(propertyPath))
                        {
                            item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                            break;
                        }
                    }
                    goto default;

                case ExpandRule.Auto:
                default:
                    {
                        // If the node was saved as expanded, persist this behavior
                        var propertyPath = node.DisplayPath;
                        if (expandedPropertyPaths.Contains(propertyPath))
                        {
                            item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                        }
                        else if (node.Parent.Children.Count == 1)
                        {
                            // If the node is an only child, let's expand it
                            item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                            // And keep a track of it, in case it has some siblings incoming
                            expandedItems.Add(item);
                        }
                        else
                        {
                            // If one of its siblings has been expanded because it was an only child at the time it was created, let's unexpand it.
                            // This will prevent to always have the first item expanded since the property items are generated as soon as a child is added.
                            expandedItems.Where(x => GetNode(x)?.Parent == node.Parent).ForEach(x => x.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, false));
                        }
                    }
                    break;
            }
        }

        foreach (var container in item.Properties)
        {
            ExpandSingleProperties(container);
        }
    }

    private static PropertyViewItem? FindPropertyItemRecursively(PropertyViewItem root, NodeViewModel target)
    {
        return root.DataContext == target ? root : root.Properties.Select(x => FindPropertyItemRecursively(x, target)).FirstOrDefault(x => x is not null);
    }

    private static NodeViewModel? GetNode(PropertyViewItem item)
    {
        return item.DataContext as NodeViewModel;
    }

    private PropertyViewItem? GetPropertyItem(string propertyPath)
    {
        var currentPropertyCollection = AssociatedObject?.Properties;
        string[] members = propertyPath.Split('.');
        PropertyViewItem? item = null;

        foreach (var member in members.Skip(1))
        {
            item = currentPropertyCollection?.FirstOrDefault(x =>
            {
                var node = x.DataContext as NodeViewModel;
                return node is not null && node.Name == member;
            });
            if (item is null)
                return null;

            currentPropertyCollection = item.Properties;
        }
        return item;
    }

    private static ExpandRule GetRule(NodeViewModel node)
    {
        if (node.AssociatedData.TryGetValue(DisplayData.AutoExpandRule, out var value) && value is ExpandRule rule)
        {
            return rule;
        }
        return ExpandRule.Auto;
    }

    private void NodeValueChanged(object? sender, NodeViewModelValueChangedArgs e)
    {
        var rule = GetRule(e.Node);
        if (rule == ExpandRule.Never)
            return;

        var match = AssociatedObject?.Properties.Select(x => FindPropertyItemRecursively(x, e.Node)).FirstOrDefault(x => x is not null);
        match?.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
    }

    private static void OnViewModelChanged(PropertyViewAutoExpandNodesBehavior sender, AvaloniaPropertyChangedEventArgs<GraphViewModel?> e)
    {
        if (e.OldValue.Value is { } previousViewModel)
            previousViewModel.NodeValueChanged -= sender.NodeValueChanged;

        if (e.NewValue.Value is { } newViewModel)
            newViewModel.NodeValueChanged += sender.NodeValueChanged;
    }

    private void PrepareItem(object? sender, PropertyViewItemEventArgs e)
    {
        RegisterItem(e.Container);
        foreach (var propertyItem in expandedPropertyPaths.ToList().Select(GetPropertyItem).NotNull())
        {
            propertyItem.IsExpanded = true;
        }
        foreach (var propertyItem in collapsedPropertyPaths.ToList().Select(GetPropertyItem).NotNull())
        {
            propertyItem.IsExpanded = false;
        }
        Dispatcher.UIThread.InvokeAsync(() => ExpandSingleProperties(e.Container));
    }

    private static void RegisterItem(PropertyViewItem item)
    {
        item.Expanded += ExpandedChanged;
        item.Collapsed += ExpandedChanged;

        foreach (var container in item.Properties)
        {
            RegisterItem(container);
        }
    }

    private static void UnregisterItem(PropertyViewItem item)
    {
        item.Expanded -= ExpandedChanged;
        item.Collapsed -= ExpandedChanged;
        foreach (var container in item.Properties)
        {
            UnregisterItem(container);
        }
    }
}
