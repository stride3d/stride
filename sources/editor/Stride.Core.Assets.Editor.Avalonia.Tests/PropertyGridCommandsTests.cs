// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Extensions;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The nested properties toggle as a command of the property grid's context menu (Phase 4 study, D13 A).
/// </summary>
public sealed class PropertyGridCommandsTests
{
    private static CommandContext Context(object? source) => new()
    {
        // The command acts on the view only
        Session = null!,
        Location = CommandLocations.PropertyGridContext,
        Source = source,
    };

    [AvaloniaFact]
    public async Task TheToggleExpandsTheNestedPropertiesThenCollapsesThem()
    {
        var command = PropertyGridCommands.CreateToggleNestedProperties();
        Assert.Equal(PropertyGridCommands.ToggleNestedPropertiesId, command.Id);
        Assert.Equal(CommandLocations.PropertyGridContext, Assert.Single(command.Placements).Location);

        var (window, propertyView) = PropertyViewWindow.Show(new NestedSample());
        try
        {
            var outer = propertyView.Item(nameof(NestedSample.Outer));

            // Collapsed: the toggle expands the property, then its nested properties
            Assert.False(outer.IsExpanded);
            Assert.True(command.CanExecute!(Context(outer)));
            Assert.Equal("Expand all nested properties", command.ContextTitle!(Context(outer)));
            await command.ExecuteAsync(Context(outer));
            PropertyViewWindow.Refresh(window);
            Assert.True(outer.IsExpanded);
            var (first, second) = (outer.Item(nameof(NestedOuter.First)), outer.Item(nameof(NestedOuter.Second)));
            Assert.True(first.IsExpanded);
            Assert.True(second.IsExpanded);

            // All expanded: from a control inside the property, it collapses them
            var control = outer.GetVisualDescendants().OfType<Control>().First(x => x.FindVisualParentOfType<PropertyViewItem>() == outer);
            Assert.Equal("Collapse all nested properties", command.ContextTitle!(Context(control)));
            await command.ExecuteAsync(Context(control));
            Assert.False(first.IsExpanded);
            Assert.False(second.IsExpanded);
            Assert.True(outer.IsExpanded);

            // One collapsed: it expands them all again
            first.IsExpanded = true;
            Assert.Equal("Expand all nested properties", command.ContextTitle!(Context(outer)));
            await command.ExecuteAsync(Context(outer));
            Assert.True(first.IsExpanded);
            Assert.True(second.IsExpanded);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheToggleNeedsAPropertyWithNestedProperties()
    {
        var command = PropertyGridCommands.CreateToggleNestedProperties();
        Assert.False(command.CanExecute!(Context(null)));
        Assert.False(command.CanExecute!(Context("not a control")));

        var (window, propertyView) = PropertyViewWindow.Show(new NestedSample());
        try
        {
            var outer = propertyView.Item(nameof(NestedSample.Outer));
            outer.IsExpanded = true;
            PropertyViewWindow.Refresh(window);
            var first = outer.Item(nameof(NestedOuter.First));
            first.IsExpanded = true;
            PropertyViewWindow.Refresh(window);
            var leaf = first.Item(nameof(FilterInner.Radius));
            Assert.False(command.CanExecute!(Context(leaf)));
        }
        finally
        {
            window.Close();
        }
    }
}
