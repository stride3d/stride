// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.UI.Editor;
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

        Drop(page, [page.Factory("Grid")], page.EmptyArea());

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

        Drop(page, [page.Factory("Button")], page.RowCenter(grid));
        Drop(page, [page.Factory("TextBlock")], page.RowCenter(grid));

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
        Drop(page, [page.Factory("Button")], page.RowCenter(grid));
        Drop(page, [page.Factory("TextBlock")], page.RowCenter(grid));
        var button = grid.Children[0];
        var text = grid.Children[1];
        await page.WaitForRowAsync(text);
        var history = page.Fixture.Session.ActionHistory;
        await WindowTestHelper.PumpAsync();
        var count = history.Transactions.Count;

        var target = page.Behavior.GetDropTarget([text], page.RowTopBand(button), AddChildModifiers.None);
        Assert.Equal(TreeViewDropKind.Insert, target.Kind);
        Assert.Equal(InsertPosition.Before, target.InsertPosition);
        Drop(page, [text], page.RowTopBand(button));

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
        Drop(page, [page.Factory("Canvas")], page.EmptyArea());
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
        Drop(page, [page.Factory("Button")], page.RowCenter(grid));
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
        Drop(page, [page.Factory("Button")], page.RowCenter(grid));
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
        Drop(page, [page.Factory("StackPanel")], page.RowCenter(grid));
        var stack = Assert.Single(grid.Children);
        await page.WaitForRowAsync(stack);
        Drop(page, [page.Factory("Button")], page.RowCenter(stack));
        var button = Assert.Single(stack.Children);
        await page.WaitForRowAsync(button);
        var buttons = page.View.GetVisualDescendants().OfType<Button>().Where(x => x.Classes.Contains("tool")).ToList();

        await page.Window.ClickAsync(buttons[1]);
        await WindowTestHelper.PumpAsync();
        Assert.False(page.Row(grid).IsExpanded);

        await page.Window.ClickAsync(buttons[0]);
        await WindowTestHelper.WaitUntilAsync(() => page.TryRow(button) is not null, "the deepest row");
        Assert.True(page.Row(grid).IsExpanded);
        Assert.True(page.Row(stack).IsExpanded);
    }

    private static void Drop(PageFixture page, IReadOnlyCollection<object> items, Point positionInTree)
    {
        var point = page.ToWindow(positionInTree);
        var data = AssetDragDropHelper.CreateItemData(items);
        page.Window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Move, RawInputModifiers.None);
        page.Window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Move, RawInputModifiers.None);
        page.Window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Move, RawInputModifiers.None);
    }

    /// <summary>
    /// Opens a session with a new UI page, then its editor.
    /// </summary>
    private static async Task<PageFixture> OpenPageAsync()
    {
        UIEditorAssemblies.Register();
        var fixture = ShellSessionFixture.Create(("Page", new Stride.UI.Assets.UIPageAsset()));
        // The elements of the toolbox come from the libraries of the session: the standard library of the UI package, which
        // this session doesn't load, is given to its package
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "StrideUILibrary.sduilib"), Path.Combine(fixture.PackageDirectory, "Assets", "UILibrary.sduilib"));
        MainWindow? window = null;
        try
        {
            // A dialog of the session would wait forever for an answer in a headless test
            var dialogs = fixture.ReplaceDialogService();
            window = new MainWindow { DataContext = fixture.Main, Width = 1600, Height = 1000 };
            window.Show();
            // The shell registers the clipboard of its main window before the session opens; the headless platform keeps one in memory
            fixture.ServiceProvider.RegisterService(new ClipboardService(window.Clipboard!));
            await fixture.OpenAsync();
            var asset = Assert.IsType<UIPageViewModel>(fixture.Asset("Page"));
            fixture.Session.AssetCollection.SelectAssets([asset]);
            await WindowTestHelper.PumpAsync();
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<UIPageEditorView>().Any(), "the UI page editor");
            var editor = Assert.IsType<UIPageEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
            var view = window.GetVisualDescendants().OfType<UIPageEditorView>().Single();
            await WindowTestHelper.WaitUntilAsync(() => editor.RootPart is not null, "the hierarchy of the page");
            Assert.False(editor.HasGame);
            await WindowTestHelper.WaitUntilAsync(() => view.ToolboxGroups.Count > 0, "the toolbox");

            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var tree = view.FindControl<TreeView>("HierarchyTree") ?? throw new InvalidOperationException("No tree in the UI editor.");
            // The headless screen keeps the window small and its font is wide: the rows of the tree would scroll horizontally,
            // and the scroll bar would cover the row the test points at
            ScrollViewer.SetHorizontalScrollBarVisibility(tree, global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
            Dispatcher.UIThread.RunJobs();
            var behavior = global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(tree).OfType<TreeViewDragDropBehavior>().Single();
            return new PageFixture(fixture, dialogs, window, editor, view, tree, behavior);
        }
        catch
        {
            // What an editor started ends with it
            await fixture.Session.EditorCollection.CloseAllEditorsAsync(false);
            if (window is not null)
                await window.CloseAsync();
            fixture.Dispose();
            throw;
        }
    }

    private sealed class PageFixture(ShellSessionFixture fixture, RecordingDialogService dialogs, MainWindow window, UIPageEditorViewModel editor, UIPageEditorView view, TreeView tree, TreeViewDragDropBehavior behavior) : IAsyncDisposable
    {
        public ShellSessionFixture Fixture { get; } = fixture;

        public RecordingDialogService Dialogs { get; } = dialogs;

        public MainWindow Window { get; } = window;

        public UIPageEditorViewModel Editor { get; } = editor;

        public UIPageEditorView View { get; } = view;

        public TreeView Tree { get; } = tree;

        public TreeViewDragDropBehavior Behavior { get; } = behavior;

        public UIRootViewModel Root => (UIRootViewModel)Editor.RootPart!;

        public IUIElementFactory Factory(string name) => Editor.Factories.Single(x => x.Name == name);

        /// <summary>
        /// Adds an element of the toolbox as the root of the page, through the empty area of the tree.
        /// </summary>
        public async Task<UIElementViewModel> AddRootAsync(string name)
        {
            Drop(this, [Factory(name)], EmptyArea());
            var root = Assert.Single(Root.Children);
            await WaitForRowAsync(root);
            return root;
        }

        public async Task SelectAsync(UIElementViewModel element)
        {
            await Window.ClickAsync(NameOf(element));
            await WindowTestHelper.WaitUntilAsync(() => Editor.SelectedContent.Count == 1 && Editor.SelectedContent.Contains(element), "the selection");
        }

        public async Task WaitForRowAsync(UIElementViewModel element)
        {
            await WindowTestHelper.WaitUntilAsync(() =>
            {
                foreach (var item in Tree.GetVisualDescendants().OfType<TreeViewItem>())
                    item.IsExpanded = true;
                return TryRow(element) is not null;
            }, "the row of the element");
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public TreeViewItem? TryRow(UIElementViewModel element)
        {
            return Tree.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(x => ReferenceEquals(x.DataContext, element));
        }

        public TreeViewItem Row(UIElementViewModel element) => TryRow(element) ?? throw new InvalidOperationException($"No row {element.Name} in the tree.");

        private TextBlock NameOf(UIElementViewModel element)
        {
            Row(element).BringIntoView();
            Dispatcher.UIThread.RunJobs();
            return Row(element).GetVisualDescendants().OfType<TextBlock>().First(x => x.Text == element.Name && ReferenceEquals(x.DataContext, element));
        }

        /// <summary>
        /// Gets the bounds of the name of a row, relative to the tree.
        /// </summary>
        private Rect Header(UIElementViewModel element)
        {
            var text = NameOf(element);
            var origin = text.TranslatePoint(default, Tree) ?? throw new InvalidOperationException("The row is not in the tree.");
            var row = Row(element);
            var header = row.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_Header" && ReferenceEquals(x.TemplatedParent, row)) ?? row;
            var top = header.TranslatePoint(default, Tree)!.Value.Y;
            // The name may go past the right edge of the tree, whose rows scroll: the bounds stop at the edge
            var width = Math.Clamp(Tree.Bounds.Width - 24 - origin.X, 1, Math.Max(1, text.Bounds.Width));
            return new Rect(origin.X, top, width, header.Bounds.Height);
        }

        public Point RowCenter(UIElementViewModel element) => Header(element).Center;

        public Point RowTopBand(UIElementViewModel element)
        {
            var header = Header(element);
            return new Point(header.Center.X, header.Y + 2);
        }

        /// <summary>
        /// A point of the empty area of the tree, below its rows.
        /// </summary>
        public Point EmptyArea() => new(Tree.Bounds.Width / 2, Tree.Bounds.Height - 10);

        public Point ToWindow(Point positionInTree) => Tree.TranslatePoint(positionInTree, Window) ?? throw new InvalidOperationException("The tree is not in the window.");

        public async ValueTask DisposeAsync()
        {
            // What the editor started ends with it: closing waits for it
            Assert.True(await Fixture.Session.EditorCollection.CloseAllEditorsAsync(false));
            await Window.CloseAsync();
            Fixture.Dispose();
        }
    }

    /// <summary>
    /// The assemblies of the UI editor, registered as a session that loads the UI package does (the editor package and its
    /// Avalonia companion), once per process: the asset of the page is then a UI page for the session that opens it.
    /// </summary>
    private static class UIEditorAssemblies
    {
        private static bool registered;

        public static void Register()
        {
            if (registered)
                return;
            registered = true;

            Assembly[] assemblies = [typeof(Stride.UI.Assets.UIPageAsset).Assembly, typeof(UIEditorPlugin).Assembly, typeof(UIEditorAvaloniaPlugin).Assembly];
            foreach (var assembly in assemblies)
            {
                // The assemblies register their scan indexes when they initialize
                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
                AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
            }
        }
    }
}
