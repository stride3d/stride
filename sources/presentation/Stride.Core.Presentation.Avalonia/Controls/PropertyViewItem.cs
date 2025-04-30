// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class PropertyViewItem : ExpandableItemsControl
{
    private readonly ObservableList<PropertyViewItem> properties = [];

    static PropertyViewItem()
    {
        IncrementProperty.Changed.AddClassHandler<PropertyViewItem, double>(OnIncrementChanged);
    }

    public static readonly StyledProperty<double> IncrementProperty =
        AvaloniaProperty.Register<PropertyView, double>(nameof(Increment));

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<PropertyView, double>(nameof(Offset));

    public PropertyViewItem(PropertyView propertyView)
    {
        ArgumentNullException.ThrowIfNull(propertyView);
        PropertyView = propertyView;
    }

    public double Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public double Offset
    {
        get => GetValue(OffsetProperty);
        private set => SetValue(OffsetProperty, value);
    }

    public IReadOnlyCollection<PropertyViewItem> Properties => properties;

    public PropertyView PropertyView { get; }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new PropertyViewItem(PropertyView) { Offset = Offset + Increment };
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        var property = (PropertyViewItem)container;
        properties.Add(property);
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        var property = (PropertyViewItem)container;
        properties.Remove(property);
        base.ClearContainerForItemOverride(container);
    }

    private static void OnIncrementChanged(PropertyViewItem sender, AvaloniaPropertyChangedEventArgs<double> e)
    {
        var delta = e.NewValue.Value - e.OldValue.Value;
        var subItems = sender.FindVisualChildrenOfType<PropertyViewItem>();
        foreach (var subItem in subItems)
        {
            subItem.Offset += delta;
        }
    }
}
