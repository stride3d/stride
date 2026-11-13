// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/)
// Licensed under MIT license, courtesy of The .NET Foundation.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class ToolBarOverflowPanel : Panel
{
    /// <summary>
    /// Defines the <see cref="WrapWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> WrapWidthProperty =
        AvaloniaProperty.Register<ToolBarOverflowPanel, double>(nameof(WrapWidth), double.NaN, validate: IsWrapWidthValid);

    private static bool IsWrapWidthValid(double v)
    {
        return double.IsNaN(v) || DoubleUtil.GreaterThanOrClose(v, 0d) && !double.IsPositiveInfinity(v);
    }

    // calculated in MeasureOverride and used in ArrangeOverride
    private double wrapWidth;
    private Size panelSize;

    internal ToolBar? ToolBar => TemplatedParent as ToolBar;

    internal ToolBarPanel? ToolBarPanel => ToolBar?.ToolBarPanel;

    public double WrapWidth
    {
        get => GetValue(WrapWidthProperty);
        set => SetValue(WrapWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var currentLineSize = new Size();
        panelSize = new Size();
        wrapWidth = double.IsNaN(WrapWidth) ? availableSize.Width : WrapWidth;
        var childrenCount = Children.Count;

        // add ToolBar items which have IsOverflowItem = true
        var toolBarPanel = ToolBarPanel;
        if (toolBarPanel is not null)
        {
            // Go through the generated items collection and add to the children collection
            // any that are marked IsOverFlowItem but aren't already in the children collection.
            //
            // The order of both collections matters.
            //
            // It is assumed that any children that were removed from generated items will have
            // already been removed from the children collection.
            var generatedItemsCollection = toolBarPanel.GeneratedItems;
            var generatedItemsCount = generatedItemsCollection?.Count ?? 0;
            var childrenIndex = 0;
            for (var i = 0; i < generatedItemsCount; i++)
            {
                var child = generatedItemsCollection?[i];
                if (child is not null && ToolBar.GetIsOverflowItem(child) && child is not Separator)
                {
                    if (childrenIndex < childrenCount)
                    {
                        if (Children[childrenIndex] != child)
                        {
                            Children.Insert(childrenIndex, child);
                            childrenCount++;
                        }
                    }
                    else
                    {
                        Children.Add(child);
                        childrenCount++;
                    }

                    childrenIndex++;
                }
            }
        }

        // measure all children to determine if we need to increase the desired wrapWidth
        for (var i = 0; i < childrenCount; i++)
        {
            var child = Children[i];
            child.Measure(availableSize);

            var childSize = child.DesiredSize;
            if (DoubleUtil.GreaterThan(childSize.Width, wrapWidth))
            {
                wrapWidth = childSize.Width;
            }
        }

        // wrapWidth should not be larger than availableSize.Width
        wrapWidth = Math.Min(wrapWidth, availableSize.Width);

        foreach (var child in Children)
        {
            var childSize = child.DesiredSize;

            // need to switch to another line
            if (DoubleUtil.GreaterThan(currentLineSize.Width + childSize.Width, wrapWidth))
            {
                panelSize = panelSize.WithWidth(Math.Max(currentLineSize.Width, panelSize.Width));
                panelSize = panelSize.WithHeight(panelSize.Height + currentLineSize.Height);
                currentLineSize = childSize;

                // the element is wider then the available width - give it a separate line
                if (DoubleUtil.GreaterThan(childSize.Width, wrapWidth))
                {
                    panelSize = panelSize.WithWidth(Math.Max(childSize.Width, panelSize.Width));
                    panelSize = panelSize.WithHeight(panelSize.Height + childSize.Height);
                    currentLineSize = new Size();
                }
            }
            // continue to accumulate a line
            else
            {
                currentLineSize = currentLineSize.WithWidth(currentLineSize.Width + childSize.Width);
                currentLineSize = currentLineSize.WithHeight(Math.Max(childSize.Height, currentLineSize.Height));
            }
        }

        // the last line size, if any should be added
        panelSize = panelSize.WithWidth(Math.Max(currentLineSize.Width, panelSize.Width));
        panelSize = panelSize.WithHeight(panelSize.Height + currentLineSize.Height);

        return panelSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var firstInLine = 0;
        var currentLineSize = new Size();
        var accumulatedHeight = 0d;

        // wrapWidth should not be larger than finalSize.Width
        wrapWidth = Math.Min(wrapWidth, finalSize.Width);

        for (var i = 0; i < Children.Count; i++)
        {
            var childSize = Children[i].DesiredSize;

            // switch to a new line
            if (DoubleUtil.GreaterThan(currentLineSize.Width + childSize.Width, wrapWidth))
            {
                // arrange the items in the current line not including the current item
                ArrangeLine(accumulatedHeight, currentLineSize.Height, firstInLine, i);
                accumulatedHeight += currentLineSize.Height;

                // Current item will be first on the next line
                firstInLine = i;
                currentLineSize = childSize;
            }
            // continue to accumulate a line
            else
            {
                currentLineSize = currentLineSize.WithWidth(currentLineSize.Width + childSize.Width);
                currentLineSize = currentLineSize.WithHeight(Math.Max(childSize.Height, currentLineSize.Height));
            }
        }

        ArrangeLine(accumulatedHeight, currentLineSize.Height, firstInLine, Children.Count);

        return panelSize;

        void ArrangeLine(double y, double lineHeight, int start, int end)
        {
            var x = 0.0;
            for (var i = start; i < end; i++)
            {
                var child = Children[i];
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, lineHeight));
                x += child.DesiredSize.Width;
            }
        }
    }
}
