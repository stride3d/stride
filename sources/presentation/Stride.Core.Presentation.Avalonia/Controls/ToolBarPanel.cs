// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/)
// Licensed under MIT license, courtesy of The .NET Foundation.

using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Stride.Core.Presentation.Avalonia.Internal;

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

    private ToolBarOverflowPanel? ToolBarOverflowPanel => ToolBar?.ToolBarOverflowPanel;

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = new Size();

        var isHorizontal = Orientation is Orientation.Horizontal;
        var childrenSlotSize = isHorizontal
            ? availableSize.WithWidth(double.PositiveInfinity)
            : availableSize.WithHeight(double.PositiveInfinity);
        var maxExtent = isHorizontal
            ? availableSize.Width
            : availableSize.Height;

        // first pass measure all the non as-needed items (i.e. we know whether they go to the main bar of the overflow)
        var hasAlwaysOverflowItems = MeasureGeneratedItems(checkAsNeeded: false, childrenSlotSize, isHorizontal, maxExtent, ref desiredSize, out _);

        MinLength = isHorizontal ? desiredSize.Width : desiredSize.Height;

        // second pass will measure all the as-needed items and place them accordingly
        var hasAsNeededOverflowItems = MeasureGeneratedItems(checkAsNeeded: true, childrenSlotSize, isHorizontal, maxExtent, ref desiredSize, out var overflowExtent);

        // the measurement is now complete, and the max size is the desired size plus the extent
        MaxLength = (isHorizontal ? desiredSize.Width : desiredSize.Height) + overflowExtent;

        ToolBar?.SetValue(ToolBar.HasOverflowItemsProperty, hasAlwaysOverflowItems || hasAsNeededOverflowItems);

        return desiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var isHorizontal = Orientation is Orientation.Horizontal;
        var rectChild = new Rect(finalSize);
        var previousDimension = 0.0;

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

    private bool MeasureGeneratedItems(bool checkAsNeeded, Size availableSize, bool isHorizontal, double maxExtent, ref Size desiredSize, out double overflowExtent)
    {
        overflowExtent = 0.0;

        if (GeneratedItems is null)
            return false;

        var hasOverflowItems = false;
        var shouldInvalidateOverflow = false;
        var shouldSendToOverflow = false;

        var childrenCount = Children.Count;
        var childIndex = 0;

        foreach (var child in GeneratedItems)
        {
            var mode = ToolBar.GetOverflowMode(child);
            var isModeAsNeeded = mode is OverflowMode.AsNeeded;

            if (isModeAsNeeded == checkAsNeeded)
            {
                var visualParent = child.GetVisualParent();

                // in this mode, measure for placement in the main bar
                if (mode is not OverflowMode.Always && !shouldSendToOverflow)
                {
                    ToolBar.SetIsOverflowItem(child, false);
                    child.Measure(availableSize);
                    var childSize = child.DesiredSize;

                    if (isModeAsNeeded)
                    {
                        var newExtent = isHorizontal
                            ? childSize.Width + desiredSize.Width
                            : childSize.Height + desiredSize.Height;

                        if (DoubleUtil.GreaterThan(newExtent, maxExtent))
                        {
                            shouldSendToOverflow = true;
                        }
                    }

                    if (!shouldSendToOverflow)
                    {
                        // accumulate the size
                        if (isHorizontal)
                        {
                            desiredSize = desiredSize.WithWidth(desiredSize.Width + childSize.Width);
                            desiredSize = desiredSize.WithHeight(Math.Max(desiredSize.Height, childSize.Height));
                        }
                        else
                        {
                            desiredSize = desiredSize.WithWidth(Math.Max(desiredSize.Width, childSize.Width));
                            desiredSize = desiredSize.WithHeight(desiredSize.Height + childSize.Height);
                        }
                        
                        if (visualParent != this)
                        {
                            if (visualParent == ToolBarOverflowPanel)
                            {
                                ToolBarOverflowPanel?.Children.Remove(child);
                            }

                            if (childIndex < childrenCount)
                            {
                                Children.Insert(childIndex, child);
                            }
                            else
                            {
                                Children.Add(child);
                            }
                            childrenCount++;
                        }
                        
                        childIndex++;
                    }
                }

                // in this mode, the child goes to the overflow
                if (mode is OverflowMode.Always || shouldSendToOverflow)
                {
                    hasOverflowItems |= mode is OverflowMode.Always || (shouldSendToOverflow && child is not Separator);

                    var childSize = child.DesiredSize;
                    if (isHorizontal)
                    {
                        overflowExtent += childSize.Width;
                        desiredSize = desiredSize.WithHeight(Math.Max(desiredSize.Height, childSize.Height));
                    }
                    else
                    {
                        overflowExtent += childSize.Height;
                        desiredSize = desiredSize.WithWidth(Math.Max(desiredSize.Width, childSize.Width));
                    }

                    ToolBar.SetIsOverflowItem(child, true);

                    // if the child was in the main panel, remove it
                    if (visualParent == this)
                    {
                        Children.Remove(child);
                        childrenCount--;
                        shouldInvalidateOverflow = true;
                    }
                    // if the child isn't in the visual tree yet
                    else if (visualParent is null)
                    {
                        shouldInvalidateOverflow = true;
                    }
                }
            }
            else
            {
                if (childIndex < childrenCount && Children[childIndex] == child)
                {
                    // this child is ignored during the current pass
                    childIndex++;
                }
            }
        }

        // a child was added to the overflow panel
        if (shouldInvalidateOverflow)
        {
            ToolBarOverflowPanel?.InvalidateMeasure();
        }

        return hasOverflowItems;
    }
}
