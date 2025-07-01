// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class ToolBarTray : Control, IAddChild<ToolBar>
{
    /// <summary>
    /// Defines the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ToolBarTray, Orientation>(nameof(Orientation));

    static ToolBarTray()
    {
        OrientationProperty.Changed.AddClassHandler<ToolBarTray>((tray, _) =>
        {
            foreach (var toolBar in tray.ToolBars)
            {
                toolBar.CoerceValue(ToolBar.OrientationProperty);
            }
        });
    }

    private readonly ToolBarCollection toolBars;

    public ToolBarTray()
    {
        toolBars = new ToolBarCollection(this);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public Collection<ToolBar> ToolBars => toolBars;

    void IAddChild<ToolBar>.AddChild(ToolBar child)
    {
        ToolBars.Add(child);
    }

    // Note: for now follow a similar layouting than StackPanel
    protected override Size MeasureOverride(Size availableSize)
    {
        Size trayDesiredSize = new();
        Size toolBarConstraint = availableSize;
        bool isHorizontal = Orientation == Orientation.Horizontal;
        toolBarConstraint = isHorizontal
            ? toolBarConstraint.WithWidth(double.PositiveInfinity)
            : toolBarConstraint.WithHeight(double.PositiveInfinity);

        foreach (var toolBar in toolBars)
        {
            // Measure the toolbar
            toolBar.Measure(toolBarConstraint);
            var toolBarDesiredSize = toolBar.DesiredSize;

            // Accumulate the size
            if (isHorizontal)
            {
                trayDesiredSize = trayDesiredSize.WithWidth(trayDesiredSize.Width + toolBarDesiredSize.Width);
                trayDesiredSize = trayDesiredSize.WithHeight(Math.Max(trayDesiredSize.Height, toolBarDesiredSize.Height));
            }
            else
            {
                trayDesiredSize = trayDesiredSize.WithWidth(Math.Max(trayDesiredSize.Width, toolBarDesiredSize.Width));
                trayDesiredSize = trayDesiredSize.WithHeight(trayDesiredSize.Height + toolBarDesiredSize.Height);
            }
        }

        return trayDesiredSize;
    }

    // Note: for now follow a similar layouting than StackPanel
    protected override Size ArrangeOverride(Size finalSize)
    {
        bool isHorizontal = Orientation == Orientation.Horizontal;
        Rect rcToolBar = new(finalSize);
        double previousDimension = 0.0;

        foreach (var toolBar in toolBars)
        {
            if (!toolBar.IsVisible) continue;

            if (isHorizontal)
            {
                rcToolBar = rcToolBar.WithX(rcToolBar.X + previousDimension);
                previousDimension = toolBar.DesiredSize.Width;
                rcToolBar = rcToolBar.WithWidth(previousDimension);
                rcToolBar = rcToolBar.WithHeight(Math.Max(finalSize.Height, toolBar.DesiredSize.Height));
            }
            else
            {
                rcToolBar = rcToolBar.WithY(rcToolBar.Y + previousDimension);
                previousDimension = toolBar.DesiredSize.Height;
                rcToolBar = rcToolBar.WithHeight(previousDimension);
                rcToolBar = rcToolBar.WithWidth(Math.Max(finalSize.Width, toolBar.DesiredSize.Width));
            }

            toolBar.Arrange(rcToolBar);
        }

        return finalSize;
    }

    private sealed class ToolBarCollection : Collection<ToolBar>
    {
        private readonly ToolBarTray parent;

        public ToolBarCollection(ToolBarTray parent)
        {
            this.parent = parent;
        }

        protected override void ClearItems()
        {
            var count = Count;
            if (count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    var toolBar = this[i];
                    parent.VisualChildren.Remove(toolBar);
                    parent.LogicalChildren.Remove(toolBar);
                }

                parent.InvalidateMeasure();
            }

            base.ClearItems();
        }

        protected override void InsertItem(int index, ToolBar toolBar)
        {
            base.InsertItem(index, toolBar);

            parent.LogicalChildren.Add(toolBar);
            parent.VisualChildren.Add(toolBar);

            parent.InvalidateMeasure();
        }

        protected override void RemoveItem(int index)
        {
            var toolBar = this[index];
            base.RemoveItem(index);

            parent.VisualChildren.Remove(toolBar);
            parent.LogicalChildren.Remove(toolBar);

            parent.InvalidateMeasure();
        }

        protected override void SetItem(int index, ToolBar toolBar)
        {
            var currentToolBar = this[index];
            if (toolBar != currentToolBar)
            {
                base.SetItem(index, toolBar);

                // remove current toolBar
                parent.VisualChildren.Remove(currentToolBar);
                parent.LogicalChildren.Remove(currentToolBar);

                // add new toolBar
                parent.LogicalChildren.Add(toolBar);
                parent.VisualChildren.Add(toolBar);

                parent.InvalidateMeasure();
            }
        }
    }
}
