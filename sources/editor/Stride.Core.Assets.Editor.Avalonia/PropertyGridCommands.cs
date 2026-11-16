// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia;

/// <summary>
/// The built-in commands of the context menu of a property in the property grid (<see cref="CommandLocations.PropertyGridContext"/>):
/// the nested properties toggle, which the menu shows in its slot, and the commands of the property's node, which it binds by
/// their identifier. Copy and Paste are the Edit commands, which act on the property the menu opened on.
/// </summary>
[Experimental("STREXP003")]
public static class PropertyGridCommands
{
    /// <summary>
    /// The identifier of the command that expands or collapses the nested properties of a property.
    /// </summary>
    public const string ToggleNestedPropertiesId = "Stride.PropertyGrid.ToggleNestedProperties";

    /// <summary>
    /// The identifier of the command that gives a property the value of its base (an archetype's, a prefab's).
    /// </summary>
    public const string ResetToBaseId = "Stride.PropertyGrid.ResetToBase";

    /// <summary>
    /// The identifier of the command that pastes the copied value in place of the property's value (Ctrl+Shift+V by default),
    /// rather than into it.
    /// </summary>
    public const string ReplaceId = "Stride.PropertyGrid.Replace";

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
            Category = CommandCategories.Edit,
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

    /// <summary>
    /// Creates the commands of a property that run a command of its node: Reset to base value and Replace. They act on the
    /// property the menu opened on or, for a shortcut, the property that holds the focus: the context's
    /// <see cref="CommandContext.Source"/> is its <see cref="PropertyViewItem"/>, or a control inside it.
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> CreateNodeCommands() =>
    [
        NodeCommand(ResetToBaseId, () => Tr._p("Menu", "Reset to base value"), ResetOverrideCommand.CommandName, null, ["archetype", "prefab", "inherit", "override"],
            // Only a property that has a base
            node => node is AssetNodeViewModel { HasBase: true }),
        NodeCommand(ReplaceId, () => Tr._p("Menu", "Replace"), ReplacePropertyCommand.CommandName, "Ctrl+Shift+V", ["paste", "clipboard", "value"], null),
    ];

    private static CommandDescriptor NodeCommand(string id, Func<string> title, string commandName, string? gesture, string[] keywords, Func<NodeViewModel, bool>? condition) => new()
    {
        Id = id,
        Category = CommandCategories.Edit,
        Title = title,
        DefaultGesture = gesture,
        Keywords = keywords,
        // It acts on the property the menu opened on, or that holds the focus
        ShowInPalette = false,
        CanExecute = context => FindNodeCommand(context, commandName, condition)?.CanExecute(null) ?? false,
        ExecuteAsync = context =>
        {
            if (FindNodeCommand(context, commandName, condition) is { } command && command.CanExecute(null))
                command.Execute(null);
            return Task.CompletedTask;
        },
    };

    private static ICommandBase? FindNodeCommand(CommandContext context, string commandName, Func<NodeViewModel, bool>? condition)
    {
        return FindItem(context)?.DataContext is NodeViewModel node && (condition?.Invoke(node) ?? true) ? node.GetCommand(commandName) : null;
    }

    private static PropertyViewItem? FindItem(CommandContext context)
    {
        return context.Source as PropertyViewItem ?? (context.Source as StyledElement)?.FindVisualParentOfType<PropertyViewItem>();
    }
}
