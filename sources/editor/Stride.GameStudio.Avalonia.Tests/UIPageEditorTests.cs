// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.UI.Editor.ViewModels;
using Stride.UI.Editor.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The Avalonia editor of a UI page, on a session with a new page and the standard UI library: the toolbox, the tree and its drag and drop, the rename,
/// the delete and the copy and paste, as in the WPF UI editor. Each change is one entry of the undo history.
/// </summary>
/// <remarks>
/// The tests have no graphics device (the previews, which bring the viewport of the editor games, are disabled): the editor
/// edits the page without its game, as the scene editor does, therefore the view of the game is checked through its drop
/// behavior.
/// </remarks>
public sealed class UIPageEditorTests
{
    [AvaloniaFact]
    public async Task TheToolboxGroupsTheLibrariesByCategory()
    {
        await using var page = await OpenPageAsync();

        // The library of the package is a user library, whose category is its URL
        var kind = Assert.Single(page.View.ToolboxGroups);
        Assert.Equal(UIElementFactoryGroup.UserLibraries, kind.Name);
        var library = Assert.Single(kind.Groups);
        Assert.Equal(ShellSessionFixture.Url("UILibrary"), library.Name);
        // Sorted by name
        var names = library.Factories.Select(x => x.Name).ToList();
        Assert.Equal(names.Order(StringComparer.CurrentCulture), names);
        Assert.Contains("Grid", names);
        Assert.Contains("Button", names);

        // Each element of the toolbox is a drag source
        var rows = page.View.GetVisualDescendants().OfType<Border>().Where(x => x.Classes.Contains("factory")).ToList();
        Assert.Contains(rows, x => x.DataContext is IUIElementFactory { Name: "Grid" });
        Assert.All(rows, x => Assert.Single(global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(x).OfType<ItemDragBehavior>()));
    }

    [AvaloniaFact]
    public async Task AnElementOfTheToolboxDroppedOnTheEmptyTreeBecomesTheRootInOneTransaction()
    {
        await using var page = await OpenPageAsync();
        var history = page.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;

        page.Drop([page.Factory("Grid")], page.EmptyArea());

        var root = Assert.Single(page.Root.Children);
        Assert.Equal(typeof(Stride.UI.Panels.Grid), root.ElementType);
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        await page.WaitForRowAsync(root);

        history.UndoCommand.Execute();
        Assert.Empty(page.Root.Children);
        history.RedoCommand.Execute();
        Assert.Single(page.Root.Children);
    }

    [AvaloniaFact]
    public async Task AnElementOfTheToolboxDroppedOnARowBecomesItsLastChild()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        var history = page.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;

        page.Drop([page.Factory("Button")], page.RowCenter(grid));
        page.Drop([page.Factory("TextBlock")], page.RowCenter(grid));

        Assert.Equal([typeof(Stride.UI.Controls.Button), typeof(Stride.UI.Controls.TextBlock)], grid.Children.Select(x => x.ElementType));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 2, "the history entries");
        // The tree shows the number of children of an item, as the WPF UI editor
        await page.WaitForRowAsync(grid.Children[1]);
        Assert.Contains(page.Row(grid).GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "(2)" && x.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task AnElementDroppedOnTheTopBandOfARowIsMovedBeforeIt()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("Button")], page.RowCenter(grid));
        await page.DropAsync([page.Factory("TextBlock")], page.RowCenter(grid));
        var button = grid.Children[0];
        var text = grid.Children[1];
        await page.WaitForRowAsync(text);
        var history = page.Fixture.Session.ActionHistory;
        await WindowTestHelper.PumpAsync();
        var count = history.Transactions.Count;

        var target = page.Behavior.GetDropTarget([text], page.RowTopBand(button), AddChildModifiers.None);
        Assert.Equal(TreeViewDropKind.Insert, target.Kind);
        Assert.Equal(InsertPosition.Before, target.InsertPosition);
        page.Drop([text], page.RowTopBand(button));

        // A move recreates the view models of the moved elements
        Assert.Equal([text.Id, button.Id], grid.Children.Select(x => x.Id));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.Equal([button.Id, text.Id], grid.Children.Select(x => x.Id));
    }

    [AvaloniaFact]
    public async Task ThePageKeepsOneRoot()
    {
        await using var page = await OpenPageAsync();

        // Two elements can't become the roots of an empty page
        var target = page.Behavior.GetDropTarget([page.Factory("Canvas"), page.Factory("Grid")], page.EmptyArea(), AddChildModifiers.None);
        Assert.Equal(DropAcceptance.Rejected, target.Acceptance);
        Assert.Equal("A UIPage asset can't have more than one root.", target.Message);

        // Once the page has its root, the empty area adds into the root, as in the WPF UI editor
        var grid = await page.AddRootAsync("Grid");
        page.Drop([page.Factory("Canvas")], page.EmptyArea());
        Assert.Same(grid, Assert.Single(page.Root.Children));
        Assert.Equal(typeof(Stride.UI.Panels.Canvas), Assert.Single(grid.Children).ElementType);
    }

    [AvaloniaFact]
    public async Task TheViewOfTheGameTakesItsDropsThroughTheEditor()
    {
        await using var page = await OpenPageAsync();
        var host = page.View.GetVisualDescendants().OfType<ContentControl>().Single(x => x.Name == "GameViewHost");
        // Without its game, the editor says so in place of the view
        Assert.Contains(page.View.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "The UI view is not available." && x.IsEffectivelyVisible);

        // The editor finds the element under the pointer in its game, as the WPF UI editor does
        Assert.Single(global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(host).OfType<AddChildDropBehavior>());
        Assert.Same(page.Editor, host.DataContext);
        Assert.IsAssignableFrom<IAddChildViewModel>(host.DataContext);
    }

    [AvaloniaFact]
    public async Task F2RenamesTheSelectedElementInPlaceInOneTransaction()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        var history = page.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        await page.SelectAsync(grid);
        Assert.True(page.Editor.RenameCommand.IsEnabled);

        page.Window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
        await WindowTestHelper.WaitUntilAsync(() => grid.IsEditing, "the rename");
        var box = page.Row(grid).GetVisualDescendants().OfType<TextBox>().Single(x => x.IsEffectivelyVisible);
        await WindowTestHelper.WaitUntilAsync(() => box.IsFocused, "the focus of the name box");
        box.SelectAll();
        page.Window.KeyTextInput("Layout");
        page.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        await WindowTestHelper.WaitUntilAsync(() => !grid.IsEditing, "the end of the rename");
        Assert.Equal("Layout", grid.Name);
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.NotEqual("Layout", grid.Name);
    }

    [AvaloniaFact]
    public async Task DeleteAsksThenRemovesTheSelectionInOneTransaction()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("Button")], page.RowCenter(grid));
        var button = Assert.Single(grid.Children);
        await page.WaitForRowAsync(button);
        var history = page.Fixture.Session.ActionHistory;
        await WindowTestHelper.PumpAsync();
        var count = history.Transactions.Count;
        await page.SelectAsync(button);

        // The key of the tree, as the Delete of the Edit menu
        page.Window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);

        await WindowTestHelper.WaitUntilAsync(() => grid.Children.Count == 0, "the deletion");
        Assert.Equal(["Are you sure you want to delete this UI element?"], page.Dialogs.Messages);
        Assert.Empty(page.Editor.SelectedContent);
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.Single(grid.Children);
    }

    [AvaloniaFact]
    public async Task DeleteDoesNothingWhenTheUserCancels()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        await page.SelectAsync(grid);
        page.Dialogs.PickButton = buttons => buttons.Last().Result;

        page.Window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);

        await WindowTestHelper.WaitUntilAsync(() => page.Dialogs.Messages.Count == 1, "the question");
        await WindowTestHelper.PumpAsync();
        Assert.Same(grid, Assert.Single(page.Root.Children));
    }

    [AvaloniaFact]
    public async Task ACopiedElementIsPastedIntoTheSelectionAndCtrlDDuplicates()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("Button")], page.RowCenter(grid));
        var button = Assert.Single(grid.Children);
        await page.WaitForRowAsync(button);
        var history = page.Fixture.Session.ActionHistory;

        await page.SelectAsync(button);
        Assert.True(page.Editor.CopyCommand.IsEnabled);
        page.Window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        page.Window.KeyReleaseQwerty(PhysicalKey.C, RawInputModifiers.Control);
        await WindowTestHelper.WaitUntilAsync(() => !string.IsNullOrEmpty(page.Window.Clipboard!.TryGetTextAsync().Result), "the copy");
        await page.SelectAsync(grid);
        await WindowTestHelper.PumpAsync();
        var count = history.Transactions.Count;
        page.Window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        page.Window.KeyReleaseQwerty(PhysicalKey.V, RawInputModifiers.Control);

        await WindowTestHelper.WaitUntilAsync(() => grid.Children.Count == 2, "the pasted button");
        Assert.All(grid.Children, x => Assert.Equal(typeof(Stride.UI.Controls.Button), x.ElementType));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry of the paste");

        // Ctrl+D duplicates the selection next to it, as in the WPF UI editor
        await page.SelectAsync(grid.Children[1]);
        page.Window.KeyPressQwerty(PhysicalKey.D, RawInputModifiers.Control);
        page.Window.KeyReleaseQwerty(PhysicalKey.D, RawInputModifiers.Control);
        await WindowTestHelper.WaitUntilAsync(() => grid.Children.Count == 3, "the duplicate");
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 2, "the history entry of the duplicate");
    }

    [AvaloniaFact]
    public async Task ExpandAndCollapseAllApplyToTheWholeTree()
    {
        await using var page = await OpenPageAsync();
        var grid = await page.AddRootAsync("Grid");
        page.Drop([page.Factory("StackPanel")], page.RowCenter(grid));
        var stack = Assert.Single(grid.Children);
        await page.WaitForRowAsync(stack);
        page.Drop([page.Factory("Button")], page.RowCenter(stack));
        var button = Assert.Single(stack.Children);
        await page.WaitForRowAsync(button);
        var buttons = page.View.GetVisualDescendants().OfType<Button>().Where(x => x.Classes.Contains("tool")).ToList();
        // Their icons come from the image dictionaries of the editor
        Assert.All(buttons, x => Assert.NotNull(Assert.IsType<Image>(x.Content).Source));

        await page.Window.ClickAsync(buttons[1]);
        await WindowTestHelper.PumpAsync();
        Assert.False(page.Row(grid).IsExpanded);

        await page.Window.ClickAsync(buttons[0]);
        await WindowTestHelper.WaitUntilAsync(() => page.TryRow(button) is not null, "the deepest row");
        Assert.True(page.Row(grid).IsExpanded);
        Assert.True(page.Row(stack).IsExpanded);
    }

    private static Task<UIEditorFixture> OpenPageAsync() => UIEditorFixture.OpenAsync<Stride.UI.Assets.UIPageAsset>("Page");
}
