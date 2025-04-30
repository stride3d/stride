// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class PropertyView : ItemsControl
{
    private readonly ObservableList<PropertyViewItem> properties = [];

    public static readonly StyledProperty<GridLength> NameColumnSizeProperty =
        AvaloniaProperty.Register<PropertyView, GridLength>(nameof(NameColumnSize), new GridLength(150), defaultBindingMode: BindingMode.TwoWay);

    public GridLength NameColumnSize
    {
        get => GetValue(NameColumnSizeProperty);
        set => SetValue(NameColumnSizeProperty, value);
    }

    public IReadOnlyCollection<PropertyViewItem> Properties => properties;

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new PropertyViewItem(this);
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
