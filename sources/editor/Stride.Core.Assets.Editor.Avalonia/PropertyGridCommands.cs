// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia;

/// <summary>
/// The built-in commands of the context menu of a property in the property grid (<see cref="CommandLocations.PropertyGridContext"/>).
/// </summary>
[Experimental("STREXP003")]
public static class PropertyGridCommands
{
    /// <summary>
    /// The identifier of the command that expands or collapses the nested properties of a property.
    /// </summary>
    public const string ToggleNestedPropertiesId = "Stride.PropertyGrid.ToggleNestedProperties";

    /// <summary>
    /// Creates the command that expands the nested properties of the property the menu opened on, or collapses them when
    /// they are all expanded (<see cref="PropertyViewHelper.ToggleNestedProperties"/>); a collapsed property is expanded
    /// first. It acts on the view: the context's <see cref="CommandContext.Source"/> is the <see cref="PropertyViewItem"/> of
    /// the property, or a control inside it.
    /// </summary>
    public static CommandDescriptor CreateToggleNestedProperties()
    {
        return new CommandDescriptor
        {
            Id = ToggleNestedPropertiesId,
            Title = () => Tr._p("Menu", "Expand or collapse all nested properties"),
            ContextTitle = context => FindItem(context) is { IsExpanded: true } item && item.Properties.Count > 0 && item.Properties.All(x => x.IsExpanded)
                ? Tr._p("Menu", "Collapse all nested properties")
                : Tr._p("Menu", "Expand all nested properties"),
            CanExecute = context => FindItem(context) is { ItemCount: > 0 },
            ExecuteAsync = context =>
            {
                if (FindItem(context) is { } item)
                {
                    if (!item.IsExpanded)
                    {
                        // Its nested properties are created when it is expanded
                        item.SetCurrentValue(ExpandableItemsControl.IsExpandedProperty, true);
                        item.UpdateLayout();
                    }
                    PropertyViewHelper.ToggleNestedProperties(item);
                }
                return Task.CompletedTask;
            },
            Placements = [new CommandPlacement(CommandLocations.PropertyGridContext, "Stride.Expand")],
            // It acts on the property the menu opened on
            ShowInPalette = false,
        };
    }

    private static PropertyViewItem? FindItem(CommandContext context)
    {
        return context.Source as PropertyViewItem ?? (context.Source as StyledElement)?.FindVisualParentOfType<PropertyViewItem>();
    }
}
