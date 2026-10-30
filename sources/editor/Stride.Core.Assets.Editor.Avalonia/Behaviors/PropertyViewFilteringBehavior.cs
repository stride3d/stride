// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// The search of the property grid: hides the properties that don't match the search text (see <see cref="PropertyFilter"/>), at every
/// level, as the WPF behavior of the same name filters the items of the property view and of each property.
/// </summary>
public sealed class PropertyViewFilteringBehavior : StyledElementBehavior<PropertyView>
{
    /// <summary>
    /// Identifies the <see cref="FilterToken"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string?> FilterTokenProperty =
        AvaloniaProperty.Register<PropertyViewFilteringBehavior, string?>(nameof(FilterToken));

    static PropertyViewFilteringBehavior()
    {
        FilterTokenProperty.Changed.AddClassHandler<PropertyViewFilteringBehavior>((sender, _) => sender.ApplyFilter());
    }

    /// <summary>
    /// Gets or sets the search text. An empty text shows every property.
    /// </summary>
    public string? FilterToken
    {
        get => GetValue(FilterTokenProperty);
        set => SetValue(FilterTokenProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } propertyView)
        {
            propertyView.PrepareItem += PrepareItem;
            propertyView.ClearItem += ClearItem;
            ApplyFilter();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } propertyView)
        {
            propertyView.ClearItem -= ClearItem;
            propertyView.PrepareItem -= PrepareItem;
        }
        base.OnDetaching();
    }

    private void ApplyFilter()
    {
        if (AssociatedObject is not { } propertyView)
            return;

        foreach (var item in propertyView.Properties)
        {
            ApplyFilter(item);
        }
    }

    private void ApplyFilter(PropertyViewItem item)
    {
        // The data context of the item might be a "disconnected object"
        var visible = item.DataContext is not NodeViewModel node || PropertyFilter.Matches(node, FilterToken);
        item.SetCurrentValue(Visual.IsVisibleProperty, visible);
        foreach (var child in item.Properties)
        {
            ApplyFilter(child);
        }
    }

    private void PrepareItem(object? sender, PropertyViewItemEventArgs e)
    {
        ApplyFilter(e.Container);
    }

    private static void ClearItem(object? sender, PropertyViewItemEventArgs e)
    {
        e.Container.ClearValue(Visual.IsVisibleProperty);
    }
}
