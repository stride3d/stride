// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class PropertyView : ItemsControl
{
    private PropertyViewItem? highlightedItem;
    private PropertyViewItem? hoveredItem;
    private readonly ObservableList<PropertyViewItem> properties = [];

    public static readonly DirectProperty<PropertyView, PropertyViewItem?> HighlightedItemProperty =
        AvaloniaProperty.RegisterDirect<PropertyView, PropertyViewItem?>(nameof(HighlightedItem), o => o.HighlightedItem);

    public static readonly DirectProperty<PropertyView, PropertyViewItem?> HoveredItemProperty =
        AvaloniaProperty.RegisterDirect<PropertyView, PropertyViewItem?>(nameof(HoveredItem), o => o.HoveredItem);

    public static readonly StyledProperty<GridLength> NameColumnSizeProperty =
        AvaloniaProperty.Register<PropertyView, GridLength>(nameof(NameColumnSize), new GridLength(150), defaultBindingMode: BindingMode.TwoWay);

    public PropertyViewItem? HighlightedItem
    {
        get => highlightedItem;
        internal set => SetAndRaise(HighlightedItemProperty, ref highlightedItem, value);
    }

    public PropertyViewItem? HoveredItem
    {
        get => hoveredItem;
        internal set => SetAndRaise(HoveredItemProperty, ref hoveredItem, value);
    }

    public GridLength NameColumnSize
    {
        get => GetValue(NameColumnSizeProperty);
        set => SetValue(NameColumnSizeProperty, value);
    }

    public IReadOnlyCollection<PropertyViewItem> Properties => properties;

    protected override void ClearContainerForItemOverride(Control container)
    {
        var property = (PropertyViewItem)container;
        properties.Remove(property);
        base.ClearContainerForItemOverride(container);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new PropertyViewItem(this);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        HighlightItem(null);
        HoverItem(null);
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        var property = (PropertyViewItem)container;
        properties.Add(property);
    }

    internal void ItemMouseMove(PropertyViewItem item)
    {
        if (item.IsHighlightable)
            HighlightItem(item);

        HoverItem(item);
    }

    private void HighlightItem(PropertyViewItem? item)
    {
        var previousItem = HighlightedItem;
        if (previousItem == item)
            return;

        if (previousItem is not null)
            previousItem.IsHighlighted = false;
        HighlightedItem = item;
        if (item is not null)
            item.IsHighlighted = true;
    }

    private void HoverItem(PropertyViewItem? item)
    {
        var previousItem = HoveredItem;
        if (previousItem == item)
            return;

        if (previousItem is not null)
            previousItem.IsHovered = false;
        HoveredItem = item;
        if (item is not null)
            item.IsHovered = true;
    }
}
