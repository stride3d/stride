// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.UI.Assets;
using Stride.UI.Editor.ViewModels;
using Stride.UI.Panels;
using static Stride.GameStudio.Avalonia.Tests.Helpers.UIEditorFixture;
using Canvas = Stride.UI.Panels.Canvas;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The context menus of the Avalonia UI editor (of an element, of the tree's empty area, of the view) and its view settings,
/// as the WPF UI editor: each part of the menu shows for the element it opens on, and each change is one entry of the history.
/// </summary>
public sealed class UIEditorMenuTests
{
    [AvaloniaFact]
    public async Task TheMenuOfAnElementShowsItsPanelToolsAndTheToolsOfItsParentsLayout()
    {
        await using var page = await OpenPageAsync();

        // The empty area (the whole tree of an empty page; the headless window leaves little empty area under the rows):
        // the menu of the root of the asset, which pastes as a root
        var menu = await page.OpenTreeMenuAsync(null);
        Assert.IsAssignableFrom<UIRootViewModel>(menu.DataContext);
        Assert.DoesNotContain("Rename", Describe(menu.Items));
        Assert.DoesNotContain("View", Describe(menu.Items));
        Assert.Equal(true, Item(menu.Items, "Paste").CommandParameter);
        await page.CloseMenuAsync();

        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("Button")], page.RowCenter(grid));
        var button = Assert.Single(grid.Children);
        await page.WaitForRowAsync(button);

        // A child of a grid: the grid's row, column and layer moves; the right-click selects it
        menu = await page.OpenTreeMenuAsync(button);
        Assert.Same(button, Assert.Single(page.Editor.SelectedContent));
        Assert.Same(button, menu.DataContext);
        Assert.Equal(["Layout", "Group into", "Grid", "Row", "Column", "Layer",
                      "Action", "Open library in editor", "Select library in asset view", "Break link to library", "Create page from selection", "Create library from selection",
                      "-", "Duplicate", "Cut", "Copy", "Paste", "Delete", "-", "Rename"], Describe(menu.Items));
        Assert.Equal(["Move up", "Move down"], Describe(Item(menu.Items, "Row").Items));
        // The button comes from a user library: its link can be broken
        Assert.True(Item(menu.Items, "Break link to library").IsEffectivelyEnabled);
        await page.CloseMenuAsync();

        // A panel at the root: its panel tools, and nothing of a parent's layout; a root can't be ungrouped
        menu = await page.OpenTreeMenuAsync(grid);
        Assert.Equal(["Layout", "Group into", "-", "Ungroup", "Change layout type", "Action"], Describe(menu.Items).Take(6));
        Assert.False(Item(menu.Items, "Ungroup").IsEnabled);
        Assert.DoesNotContain("Row", Describe(menu.Items));
        Assert.Equal(false, Item(menu.Items, "Paste").CommandParameter);
        await page.CloseMenuAsync();

    }

    [AvaloniaFact]
    public async Task APinOfTheMenuMovesTheOriginOfACanvasChildInOneTransaction()
    {
        await using var page = await OpenPageAsync();
        var canvas = await page.AddRootAsync("Canvas");
        await page.DropAsync([page.Factory("Button")], page.RowCenter(canvas));
        var button = Assert.Single(canvas.Children);
        await page.WaitForRowAsync(button);
        var history = page.Fixture.Session.ActionHistory;
        await WindowTestHelper.PumpAsync();
        var count = history.Transactions.Count;

        var menu = await page.OpenTreeMenuAsync(button);
        Assert.Contains("Canvas", Describe(menu.Items));
        var pin = await OpenSubMenuAsync(Item(menu.Items, "Pin origin"));
        Assert.Equal(["Top left", "Top", "Top right", "Left", "Center", "Right", "Bottom left", "Bottom", "Bottom right", "Front", "Middle", "Back"], Describe(pin.Items));
        Click(Item(pin.Items, "Center"));
        await page.CloseMenuAsync();

        Assert.Equal(new Vector3(0.5f, 0.5f, 0.0f), button.AssetSideUIElement.DependencyProperties.Get(Canvas.PinOriginPropertyKey));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.Equal(Vector3.Zero, button.AssetSideUIElement.DependencyProperties.Get(Canvas.PinOriginPropertyKey));
    }

    [AvaloniaFact]
    public async Task UngroupMovesTheChildrenOfAPanelToItsParentInOneTransaction()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("StackPanel")], page.RowCenter(grid));
        var stack = Assert.Single(grid.Children);
        await page.WaitForRowAsync(stack);
        await page.DropAsync([page.Factory("Button")], page.RowCenter(stack));
        await page.DropAsync([page.Factory("TextBlock")], page.RowCenter(stack));
        Assert.Equal(2, stack.Children.Count);
        await page.WaitForRowAsync(stack.Children[1]);
        var history = page.Fixture.Session.ActionHistory;
        await WindowTestHelper.PumpAsync();
        var count = history.Transactions.Count;

        // A child of a stack panel: its moves
        var menu = await page.OpenTreeMenuAsync(stack.Children[1]);
        Assert.Contains("StackPanel", Describe(menu.Items));
        Click(Item(menu.Items, "Move up"));
        await page.CloseMenuAsync();
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry of the move");
        history.UndoCommand.Execute();
        await WindowTestHelper.PumpAsync();
        count = history.Transactions.Count;

        menu = await page.OpenTreeMenuAsync(stack);
        Click(Item(menu.Items, "Ungroup"));
        await page.CloseMenuAsync();

        Assert.Equal([typeof(Stride.UI.Controls.Button), typeof(Stride.UI.Controls.TextBlock)], grid.Children.Select(x => x.ElementType));
        // The new entry replaces the undone move
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count && !history.CanRedo, "the history entry of the ungroup");
        history.UndoCommand.Execute();
        Assert.Equal(typeof(Stride.UI.Panels.StackPanel), Assert.Single(grid.Children).ElementType);
    }

    [AvaloniaFact]
    public async Task GroupIntoPutsTheSelectionInANewPanelInOneTransaction()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("Button")], page.RowCenter(grid));
        await page.DropAsync([page.Factory("TextBlock")], page.RowCenter(grid));
        await page.WaitForRowAsync(grid.Children[1]);
        await page.SelectAsync(grid.Children[0]);
        page.Editor.SelectedContent.Add(grid.Children[1]);
        await WindowTestHelper.PumpAsync();
        var history = page.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;

        // The panels to group into are those of the standard library, which the test session doesn't have (its library is
        // a user library): the menu lists none
        var menu = await page.OpenTreeMenuAsync(grid.Children[1]);
        Assert.Equal(2, page.Editor.SelectedContent.Count);
        Assert.Empty(Item(menu.Items, "Group into").Items);
        await page.CloseMenuAsync();

        // The command of its items, with the stack panel of the library as a standard one
        page.Editor.GroupIntoCommand.Execute(StandardFactory(page, "StackPanel"));

        var stack = Assert.Single(grid.Children);
        Assert.Equal(typeof(Stride.UI.Panels.StackPanel), stack.ElementType);
        Assert.Equal([typeof(Stride.UI.Controls.Button), typeof(Stride.UI.Controls.TextBlock)], stack.Children.Select(x => x.ElementType));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.Equal(2, grid.Children.Count);
    }

    [AvaloniaFact]
    public async Task TheMenuOfTheViewIsTheMenuOfTheLastSelectedElementWithTheViewOptions()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");

        // Nothing selected: the view options only (without a game, nothing is found under the pointer)
        var menu = await page.OpenViewMenuAsync();
        Assert.Equal(["View"], Describe(menu.Items));
        Assert.Equal(["Zoom in", "Zoom out", "Reset camera"], Describe(Item(menu.Items, "View").Items));
        await page.CloseMenuAsync();

        await page.SelectAsync(grid);
        menu = await page.OpenViewMenuAsync();
        Assert.Same(grid, menu.DataContext);
        var items = Describe(menu.Items);
        Assert.Equal("Layout", items[0]);
        Assert.Equal("View", items[^1]);
        Assert.Contains("Change layout type", items);
        await page.CloseMenuAsync();
    }

    [AvaloniaFact]
    public async Task TheViewSettingsEditTheColorsAndThicknessesOfTheAdorners()
    {
        await using var page = await OpenPageAsync();
        var button = page.View.FindControl<Button>("ViewSettingsButton") ?? throw new InvalidOperationException("No view settings button.");

        await page.Window.ClickAsync(button);
        var flyout = Assert.IsType<Flyout>(button.Flyout);
        await WindowTestHelper.WaitUntilAsync(() => flyout.IsOpen, "the view settings");
        var panel = Assert.IsType<global::Avalonia.Controls.StackPanel>(flyout.Content);
        await WindowTestHelper.PumpAsync();
        Assert.Equal(4, panel.GetVisualDescendants().OfType<ColorEditor>().Count());
        Assert.Contains(panel.GetVisualDescendants().OfType<ColorEditor>(), x => x.Value == page.Editor.SelectionColor);

        var thicknesses = panel.GetVisualDescendants().OfType<NumericTextBox>().ToList();
        Assert.Equal(4, thicknesses.Count);
        thicknesses[2].Value = 5;
        Assert.Equal(5.0f, page.Editor.SelectionThickness);
        flyout.Hide();
        await WindowTestHelper.PumpAsync();
    }

    /// <summary>
    /// A factory of the standard library for an element of the session's library, as the editor makes them for the
    /// read-only packages (the factory type is internal to the UI editor).
    /// </summary>
    private static IUIElementFactory StandardFactory(UIEditorFixture page, string name)
    {
        var library = Assert.IsType<UILibraryViewModel>(page.Fixture.Asset("UILibrary"));
        var id = ((UILibraryAsset)library.Asset).PublicUIElements.Single(x => x.Value == name).Key;
        var type = typeof(IUIElementFactory).Assembly.GetType("Stride.UI.Editor.ViewModels.UIElementFromSystemLibrary", throwOnError: true)!;
        return (IUIElementFactory)Activator.CreateInstance(type, page.Editor.ServiceProvider, library, id)!;
    }

    private static Task<UIEditorFixture> OpenPageAsync() => UIEditorFixture.OpenAsync<UIPageAsset>("Page");
}
