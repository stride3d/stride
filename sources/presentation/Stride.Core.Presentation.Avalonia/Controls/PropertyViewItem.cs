// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class PropertyViewItem : ExpandableItemsControl
{
    private readonly ObservableList<PropertyViewItem> properties = [];

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<PropertyView, double>(nameof(Offset));

    public PropertyViewItem(PropertyView propertyView)
    {
        ArgumentNullException.ThrowIfNull(propertyView);
        PropertyView = propertyView;
    }

    public double Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public IReadOnlyCollection<PropertyViewItem> Properties => properties;

    public PropertyView PropertyView { get; }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new PropertyViewItem(PropertyView);
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
}
