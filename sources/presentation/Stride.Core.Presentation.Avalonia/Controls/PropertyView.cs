// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    /// <summary>
    /// Identifies the ClearPropertyItem event.
    /// This attached routed event may be raised by the <see cref="PropertyView"/> itself or by a <see cref="PropertyViewItem"/> containing sub items.
    /// </summary>
    public static readonly RoutedEvent ClearItemEvent =
        RoutedEvent.Register<PropertyView, PropertyViewItemEventArgs>(nameof(ClearItem), RoutingStrategies.Bubble);

    /// <summary>
    /// Identifies the PreparePropertyItem event.
    /// This attached routed event may be raised by the <see cref="PropertyView"/> itself or by a <see cref="PropertyViewItem"/> containing sub-items.
    /// </summary>
    public static readonly RoutedEvent PrepareItemEvent =
        RoutedEvent.Register<PropertyView, PropertyViewItemEventArgs>(nameof(PrepareItem), RoutingStrategies.Bubble);

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

    /// <summary>
    /// This event is raised when a property item is about to be displayed in the <see cref="PropertyView"/>.
    /// This allow the user to customize the property item just before it is displayed.
    /// </summary>
    public event EventHandler<PropertyViewItemEventArgs> PrepareItem
    {
        add => AddHandler(PrepareItemEvent, value);
        remove => RemoveHandler(PrepareItemEvent, value);
    }

    /// <summary>
    /// This event is raised when an property item is about to be removed from the display in the <see cref="PropertyView"/>
    /// This allow the user to remove any attached handler in the <see cref="PrepareItem"/> event.
    /// </summary>
    public event EventHandler<PropertyViewItemEventArgs> ClearItem
    {
        add => AddHandler(ClearItemEvent, value);
        remove => RemoveHandler(ClearItemEvent, value);
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        var property = (PropertyViewItem)container;
        RaiseEvent(new PropertyViewItemEventArgs(ClearItemEvent, this, property));
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
        RaiseEvent(new PropertyViewItemEventArgs(PrepareItemEvent, this, property));
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
