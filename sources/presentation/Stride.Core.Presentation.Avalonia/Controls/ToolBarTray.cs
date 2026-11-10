// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/)
// Licensed under MIT license, courtesy of The .NET Foundation.

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Metadata;
using Stride.Core.Presentation.Avalonia.Internal;

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
        OrientationProperty.Changed.AddClassHandler<ToolBarTray>(OnOrientationChanged);

        AffectsMeasure<ToolBarTray>(OrientationProperty);
        return;

        static void OnOrientationChanged(ToolBarTray tray, AvaloniaPropertyChangedEventArgs _)
        {
            foreach (var toolBar in tray.ToolBars)
            {
                toolBar.CoerceValue(ToolBar.OrientationProperty);
            }
        }
    }

    private readonly List<BandInfo> bandInfos = [];
    private bool bandsDirty = true;
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

    [Content]
    public Collection<ToolBar> ToolBars => toolBars;

    void IAddChild<ToolBar>.AddChild(ToolBar child)
    {
        ToolBars.Add(child);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        GenerateBands();

        var trayDesiredSize = new Size();
        var isHorizontal = Orientation is Orientation.Horizontal;
        var toolBarAvailableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (var bandInfo in bandInfos)
        {
            var band = bandInfo.Band;

            // remainingLength is the available size minus sum of all minimum sizes.
            var remainingLength = isHorizontal ? availableSize.Width : availableSize.Height;
            foreach (var toolBar in band)
            {
                remainingLength -= toolBar.MinLength;
                if (DoubleUtil.LessThan(remainingLength, 0))
                {
                    remainingLength = 0;
                    break;
                }
            }

            // measure all children passing the remainingLength as the available size
            var bandThickness = 0.0;
            var bandLength = 0.0;
            foreach (var toolBar in band)
            {
                remainingLength += toolBar.MinLength;

                toolBarAvailableSize = isHorizontal
                    ? toolBarAvailableSize.WithWidth(remainingLength)
                    : toolBarAvailableSize.WithHeight(remainingLength);
                toolBar.Measure(toolBarAvailableSize);

                bandThickness = Math.Max(bandThickness, isHorizontal ? toolBar.DesiredSize.Height : toolBar.DesiredSize.Width);

                bandLength += isHorizontal ? toolBar.DesiredSize.Width : toolBar.DesiredSize.Height;

                remainingLength -= isHorizontal ? toolBar.DesiredSize.Width : toolBar.DesiredSize.Height;
                if (DoubleUtil.LessThan(remainingLength, 0))
                {
                    remainingLength = 0;
                }
            }

            // store band thickness (used during Arrange)
            bandInfo.Thickness = bandThickness;

            if (isHorizontal)
            {
                trayDesiredSize = trayDesiredSize.WithHeight(trayDesiredSize.Height + bandThickness);
                trayDesiredSize = trayDesiredSize.WithWidth(Math.Max(trayDesiredSize.Width, bandLength));
            }
            else
            {
                trayDesiredSize = trayDesiredSize.WithWidth(trayDesiredSize.Width + bandThickness);
                trayDesiredSize = trayDesiredSize.WithHeight(Math.Max(trayDesiredSize.Height, bandLength));
            }
        }

        return trayDesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rectToolBar = new Rect(finalSize);
        var isHorizontal = Orientation is Orientation.Horizontal;

        foreach (var bandInfo in bandInfos)
        {
            var band = bandInfo.Band;
            var bandThickness = bandInfo.Thickness;

            rectToolBar = isHorizontal
                ? rectToolBar.WithX(0)
                : rectToolBar.WithY(0);

            foreach (var toolBar in band)
            {
                // skip calculation if toolbar isn't visible
                if (!toolBar.IsVisible)
                    continue;

                var toolBarArrangeSize = new Size(
                    isHorizontal ? toolBar.DesiredSize.Width : bandThickness,
                    isHorizontal ? bandThickness : toolBar.DesiredSize.Height);
                rectToolBar = new Rect(rectToolBar.Position, toolBarArrangeSize);
                toolBar.Arrange(rectToolBar);
                rectToolBar = isHorizontal
                    ? rectToolBar.WithX(rectToolBar.X + toolBarArrangeSize.Width)
                    : rectToolBar.WithY(rectToolBar.Y + toolBarArrangeSize.Height);
            }

            rectToolBar = isHorizontal
                ? rectToolBar.WithY(rectToolBar.Y + bandThickness)
                : rectToolBar.WithX(rectToolBar.X + bandThickness);
        }

        return finalSize;
    }

    private void GenerateBands()
    {
        if (!IsBandsDirty())
            return;

        bandInfos.Clear();
        for (var i = 0; i < toolBars.Count; ++i)
        {
            InsertBand(toolBars[i], i);
        }

        // normalize band numbers and indices
        for (var i = 0; i < bandInfos.Count; ++i)
        {
            var band = bandInfos[i].Band;
            for (var j = 0; j < band.Count; ++j)
            {
                var toolBar = band[j];
                toolBar.Band = i;
                toolBar.BandIndex = j;
            }
        }
        bandsDirty = false;

        return;

        // creates a new band and add all toolbars with the same Band number than the ToolBar with the given index.
        BandInfo CreateBand(int index)
        {
            BandInfo bandInfo = new();
            var toolBar = toolBars[index];
            bandInfo.Band.Add(toolBar);
            var bandNumber = toolBar.Band;
            for (var i = index + 1; i < toolBars.Count; ++i)
            {
                toolBar = toolBars[i];
                if (toolBar.Band == bandNumber)
                    InsertToolBar(toolBar, bandInfo.Band);
            }

            return bandInfo;
        }

        void InsertBand(ToolBar toolBar, int index)
        {
            var bandNumber = toolBar.Band;
            for (var i = 0; i < bandInfos.Count; ++i)
            {
                var currentBandNumber = bandInfos[i].Band[0].Band;
                if (currentBandNumber == bandNumber)
                    return;
                if (currentBandNumber > bandNumber)
                {
                    // band number is lower than an existing one
                    bandInfos.Insert(i, CreateBand(index));
                    return;
                }
            }

            // band number doesn't exist and is greater than any other band
            bandInfos.Add(CreateBand(index));
        }

        // inserts the toolbar into the band while preserving order
        void InsertToolBar(ToolBar toolBar, List<ToolBar> band)
        {
            for (var i = 0; i < band.Count; ++i)
            {
                if (toolBar.BandIndex >= band[i].BandIndex)
                    continue;

                band.Insert(i, toolBar);
                return;
            }

            band.Add(toolBar);
        }

        // checks whether all toolbars are sorted by Band and BandIndex
        bool IsBandsDirty()
        {
            if (bandsDirty)
                return true;

            var totalCount = 0;
            for (var i = 0; i < bandInfos.Count; ++i)
            {
                var band = bandInfos[i].Band;
                for (var j = 0; j < band.Count; ++j)
                {
                    var toolBar = band[j];
                    if (toolBar.Band != i || toolBar.BandIndex != j || !toolBars.Contains(toolBar))
                        return true;
                }

                totalCount += band.Count;
            }

            return totalCount != toolBars.Count;
        }
    }

    private sealed class BandInfo
    {
        public List<ToolBar> Band { get; } = [];

        public double Thickness { get; set; }
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
            if (toolBar == currentToolBar)
                return;

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
