// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/)
// Licensed under MIT license, courtesy of The .NET Foundation.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart("PART_ToolBarPanel", typeof(ToolBarPanel), IsRequired = true)]
public sealed class ToolBar : ItemsControl
{
    /// <summary>
    /// Defines the <see cref="Band"/> Property
    /// </summary>
    public static readonly StyledProperty<int> BandProperty =
        AvaloniaProperty.Register<ToolBar, int>(nameof(Band));

    /// <summary>
    /// Defines the <see cref="BandIndex"/> Property
    /// </summary>
    public static readonly StyledProperty<int> BandIndexProperty =
        AvaloniaProperty.Register<ToolBar, int>(nameof(BandIndex));

    /// <summary>
    /// Defines the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ToolBar, Orientation>(nameof(Orientation), coerce: CoerceOrientation);

    private static Orientation CoerceOrientation(AvaloniaObject d, Orientation value)
    {
        ToolBarTray? toolBarTray = ((ToolBar)d).ToolBarTray;
        return toolBarTray?.Orientation ?? value;
    }

    public ToolBar()
    {
        Items.CollectionChanged += OnItemsChanged;
        return;

        void OnItemsChanged(object? o, NotifyCollectionChangedEventArgs e)
        {
            InvalidateLayout();
        }
    }

    public int Band
    {
        get => GetValue(BandProperty);
        set => SetValue(BandProperty, value);
    }

    public int BandIndex
    {
        get => GetValue(BandIndexProperty);
        set => SetValue(BandIndexProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
    }

    internal double MaxLength { get; private set; }

    internal double MinLength { get; private set; }

    internal ToolBarPanel? ToolBarPanel { get; private set; }

    private ToolBarTray? ToolBarTray => Parent as ToolBarTray;

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = base.MeasureOverride(availableSize);

        // note: MinLength and MaxLength are used by ToolBarTray.
        if (ToolBarPanel is not null)
        {
            var margin = ToolBarPanel.Margin;
            var extraLength = ToolBarPanel.Orientation is Orientation.Horizontal
                ? Math.Max(0.0, desiredSize.Width - ToolBarPanel.DesiredSize.Width + margin.Left + margin.Right)
                : Math.Max(0.0, desiredSize.Height - ToolBarPanel.DesiredSize.Height + margin.Top + margin.Bottom);

            MaxLength = ToolBarPanel.MaxLength + extraLength;
            MinLength = ToolBarPanel.MinLength + extraLength;
        }

        return desiredSize;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ToolBarPanel = e.NameScope.Find<ToolBarPanel>("PART_ToolBarPanel");
        ArgumentNullException.ThrowIfNull(ToolBarPanel);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        CoerceValue(OrientationProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BandProperty || change.Property == BandIndexProperty)
        {
            if (Parent is not Layoutable visualParent)
                return;

            visualParent.InvalidateMeasure();
        }

        base.OnPropertyChanged(change);
    }

    protected override void OnTemplateChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Invalidate template references
        ToolBarPanel = null;

        base.OnTemplateChanged(e);
    }

    internal void AddLogicalChild(Control c)
    {
        if (!LogicalChildren.Contains(c))
        {
            LogicalChildren.Add(c);
        }
    }

    internal void RemoveLogicalChild(Control c)
    {
        LogicalChildren.Remove(c);
    }

    private void InvalidateLayout()
    {
        // reset the calculated min and max size
        MinLength = 0.0;
        MaxLength = 0.0;

        // min and max sizes are calculated in ToolBar.MeasureOverride
        InvalidateMeasure();

        // whether children are in the overflow or not is decided in ToolBarPanel.MeasureOverride.
        ToolBarPanel?.InvalidateMeasure();
    }
}
