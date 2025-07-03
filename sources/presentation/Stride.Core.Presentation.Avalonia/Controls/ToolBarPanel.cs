// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/)
// Licensed under MIT license, courtesy of The .NET Foundation.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class ToolBarPanel : Panel
{
    /// <summary>
    /// Defines the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ToolBarPanel, Orientation>(nameof(Orientation));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double MaxLength { get; private set; }

    public double MinLength { get; private set; }

    internal global::Avalonia.Controls.Controls? GeneratedItems { get; private set; }

    internal ToolBar? ToolBar => TemplatedParent as ToolBar;

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = new Size();
        var childSlotSize = availableSize;
        bool isHorizontal = Orientation is Orientation.Horizontal;
        childSlotSize = isHorizontal
            ? childSlotSize.WithWidth(double.PositiveInfinity)
            : childSlotSize.WithHeight(double.PositiveInfinity);

        var childrenCount = Children.Count;
        var childrenIndex = 0;

        foreach (var child in GeneratedItems!)
        {
            var visualParent = child.GetVisualParent();

            child.Measure(childSlotSize);
            var childDesiredSize = child.DesiredSize;

            // accumulate the size
            if (isHorizontal)
            {
                desiredSize = desiredSize.WithWidth(desiredSize.Width + childDesiredSize.Width);
                desiredSize = desiredSize.WithHeight(Math.Max(desiredSize.Height, childDesiredSize.Height));
            }
            else
            {
                desiredSize = desiredSize.WithWidth(Math.Max(desiredSize.Width, childDesiredSize.Width));
                desiredSize = desiredSize.WithHeight(desiredSize.Height + childDesiredSize.Height);
            }

            if (visualParent != this)
            {
                if (childrenIndex < childrenCount)
                {
                    Children.Insert(childrenIndex, child);
                }
                else
                {
                    Children.Add(child);
                }
                childrenCount++;
            }

            Debug.Assert(Children[childrenIndex] == child, "Children is out of sync with generatedItems.");
            childrenIndex++;
        }

        MinLength = isHorizontal ? desiredSize.Width : desiredSize.Height;
        MaxLength = isHorizontal ? desiredSize.Width : desiredSize.Height; // will be different when we have overflow

        return desiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool isHorizontal = Orientation is Orientation.Horizontal;
        Rect rectChild = new(finalSize);
        double previousDimension = 0.0;

        foreach (var child in Children)
        {
            // skip calculation if child isn't visible
            if (!child.IsVisible)
                continue;

            if (isHorizontal)
            {
                rectChild = rectChild.WithX(rectChild.X + previousDimension);
                previousDimension = child.DesiredSize.Width;
                rectChild = rectChild.WithWidth(previousDimension);
                rectChild = rectChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
            }
            else
            {
                rectChild = rectChild.WithY(rectChild.Y + previousDimension);
                previousDimension = child.DesiredSize.Height;
                rectChild = rectChild.WithHeight(previousDimension);
                rectChild = rectChild.WithWidth(Math.Max(finalSize.Width, child.DesiredSize.Width));
            }

            child.Arrange(rectChild);
        }

        return finalSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ToolBar is { } toolBar)
        {
            toolBar.Items.CollectionChanged += ItemsOnCollectionChanged;

            if (GeneratedItems is null)
            {
                GeneratedItems = [];
            }
            else
            {
                GeneratedItems.Clear();
            }

            AddItems(toolBar.Items);
        }

        return;

        void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (GeneratedItems is null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddItems(e.NewItems!, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    AddItems(e.NewItems!, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    ClearItemsControlLogicalChildren();
                    GeneratedItems.Clear();
                    AddItems(toolBar.Items);
                    break;
            }

            return;

            void ClearItemsControlLogicalChildren()
            {
                if (GeneratedItems is null)
                    return;

                foreach (var child in GeneratedItems)
                {
                    toolBar.RemoveLogicalChild(child);
                }
            }

            void Remove(int index, int count)
            {
                if (GeneratedItems == null)
                    return;

                var generator = toolBar.ItemContainerGenerator;
                for (var i = 0; i < count; i++)
                {
                    var control = GeneratedItems[index + i];

                    var visualParent = control.GetVisualParent();

                    if (visualParent == this)
                    {
                        Children.Remove(control);
                    }

                    toolBar.RemoveLogicalChild(control);
                    generator.ClearItemContainer(control);
                }

                GeneratedItems.RemoveRange(index, count);

                var childCount = GeneratedItems.Count;

                for (var i = index; i < childCount; ++i)
                {
                    generator.ItemContainerIndexChanged(GeneratedItems[i], i + count, i);
                }
            }
        }
    }

    private void AddItems(IEnumerable items, int index = 0)
    {
        if (ToolBar is not { } toolBar || GeneratedItems is null)
            return;

        var generator = toolBar.ItemContainerGenerator;
        var i = index;
        foreach (var item in items)
        {
            InsertContainer(item, i++);
        }

        var childCount = GeneratedItems.Count;
        var delta = i - index;

        for (; i < childCount; ++i)
        {
            generator.ItemContainerIndexChanged(GeneratedItems[i], i - delta, i);
        }
    }

    private void InsertContainer(object item, int index)
    {
        if (ToolBar is not { } toolBar || GeneratedItems is null)
            return;

        var generator = toolBar.ItemContainerGenerator;
        Control container;
        if (generator.NeedsContainer(item, index, out var recycleKey))
        {
            container = generator.CreateContainer(item, index, recycleKey);
        }
        else
        {
            container = (Control)item;
        }

        generator.PrepareItemContainer(container, item, index);
        toolBar.AddLogicalChild(container);
        GeneratedItems.Insert(index, container);
        generator.ItemContainerPrepared(container, item, index);
    }
}
