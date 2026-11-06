// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Reflection;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Views;
using Stride.UI.Assets;
using Stride.UI.Editor;
using Stride.UI.Editor.ViewModels;
using Stride.UI.Editor.Views;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// A session with one UI asset (a page or a library) and the standard UI library, and the asset's editor open in a main
/// window: its view, its tree and the drops on the tree. The editor runs without its game (the tests have no graphics
/// device). Disposing closes the editor (waiting for what it started), the window and the session.
/// </summary>
internal sealed class UIEditorFixture : IAsyncDisposable
{
    /// <summary>
    /// Opens a session with a new UI asset (a page or a library) at the given location, then its editor.
    /// </summary>
    /// <remarks>The asset is made once the UI assemblies are registered: its constructor takes its format version from them.</remarks>
    public static async Task<UIEditorFixture> OpenAsync<TAsset>(string location) where TAsset : UIAssetBase, new()
    {
        UIEditorAssemblies.Register();
        var fixture = ShellSessionFixture.Create((location, new TAsset()));
        // The elements of the toolbox come from the libraries of the session: the standard library of the UI package, which
        // this session doesn't load, is given to its package
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "StrideUILibrary.sduilib"), Path.Combine(fixture.PackageDirectory, "Assets", "UILibrary.sduilib"));
        MainWindow? window = null;
        try
        {
            // A dialog of the session would wait forever for an answer in a headless test
            var dialogs = fixture.ReplaceDialogService();
            window = new MainWindow { DataContext = fixture.Main, Width = 1600, Height = 1000 };
            await window.ShowAsync();
            // The shell registers the clipboard of its main window before the session opens; the headless platform keeps one in memory
            fixture.ServiceProvider.RegisterService(new ClipboardService(window.Clipboard!));
            await fixture.OpenAsync();
            fixture.Session.AssetCollection.SelectAssets([fixture.Asset(location)]);
            await WindowTestHelper.PumpAsync();
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<UIEditorView>().Any(), "the UI editor");
            var editor = Assert.IsAssignableFrom<UIEditorBaseViewModel>(fixture.Session.EditorCollection.ActiveEditor);
            var view = window.GetVisualDescendants().OfType<UIEditorView>().Single();
            await WindowTestHelper.WaitUntilAsync(() => editor.RootPart is not null, "the hierarchy of the asset");
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
            return new UIEditorFixture(fixture, dialogs, window, editor, view, tree, behavior);
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

    private UIEditorFixture(ShellSessionFixture fixture, RecordingDialogService dialogs, MainWindow window, UIEditorBaseViewModel editor, UIEditorView view, TreeView tree, TreeViewDragDropBehavior behavior)
    {
        Fixture = fixture;
        Dialogs = dialogs;
        Window = window;
        Editor = editor;
        View = view;
        Tree = tree;
        Behavior = behavior;
    }

    public ShellSessionFixture Fixture { get; }

    public RecordingDialogService Dialogs { get; }

    public MainWindow Window { get; }

    public UIEditorBaseViewModel Editor { get; }

    public UIEditorView View { get; }

    public TreeView Tree { get; }

    public TreeViewDragDropBehavior Behavior { get; }

    public UIRootViewModel Root => (UIRootViewModel)Editor.RootPart!;

    public IUIElementFactory Factory(string name) => Editor.Factories.Single(x => x.Name == name);

    /// <summary>
    /// Adds an element of the toolbox as a root of the asset (the last one), through the empty area of the tree, and waits for
    /// its entry in the history (a test that counts the entries counts from there).
    /// </summary>
    public async Task<UIElementViewModel> AddRootAsync(string name)
    {
        await DropAsync([Factory(name)], EmptyArea());
        var root = Root.Children[^1];
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

    /// <summary>
    /// Right-clicks a row of the tree (or its empty area when no element is given) and waits for the context menu.
    /// </summary>
    public async Task<ContextMenu> OpenTreeMenuAsync(UIElementViewModel? element)
    {
        var point = ToWindow(element is not null ? RowCenter(element) : EmptyArea());
        return await RightClickAsync(point);
    }

    /// <summary>
    /// Right-clicks the middle of the view of the game and waits for the context menu.
    /// </summary>
    public async Task<ContextMenu> OpenViewMenuAsync()
    {
        var area = View.FindControl<Panel>("ViewArea") ?? throw new InvalidOperationException("No view area in the UI editor.");
        var point = area.TranslatePoint(new Point(area.Bounds.Width / 2, area.Bounds.Height / 2), Window) ?? throw new InvalidOperationException("The view is not in the window.");
        return await RightClickAsync(point);
    }

    /// <summary>
    /// Closes the context menu.
    /// </summary>
    public async Task CloseMenuAsync()
    {
        View.ElementContextMenu.Close();
        await WindowTestHelper.PumpAsync();
    }

    /// <summary>
    /// The headers of the visible items of a menu, "-" for a separator.
    /// </summary>
    public static List<string> Describe(IEnumerable<object?> items)
    {
        return items.OfType<Control>().Where(x => x.IsVisible).Select(x => x is MenuItem item ? item.Header?.ToString() ?? "" : "-").ToList();
    }

    /// <summary>
    /// Gets the visible item of a menu (or of a sub menu) with this header.
    /// </summary>
    public static MenuItem Item(IEnumerable<object?> items, string header)
    {
        return items.OfType<MenuItem>().Single(x => x.IsVisible && Equals(x.Header, header));
    }

    /// <summary>
    /// Opens the sub menu of an item, as the pointer over it does: its items take their commands once shown.
    /// </summary>
    public static async Task<MenuItem> OpenSubMenuAsync(MenuItem item)
    {
        item.Open();
        await WindowTestHelper.WaitUntilAsync(() => item.IsSubMenuOpen, $"the sub menu {item.Header}");
        await WindowTestHelper.PumpAsync();
        return item;
    }

    /// <summary>
    /// Runs the command of a menu item, as a click on it.
    /// </summary>
    public static void Click(MenuItem item)
    {
        Assert.True(item.IsEffectivelyEnabled, $"{item.Header} is disabled");
        Assert.NotNull(item.Command);
        Assert.True(item.Command.CanExecute(item.CommandParameter), $"{item.Header} can't execute");
        item.Command.Execute(item.CommandParameter);
    }

    private async Task<ContextMenu> RightClickAsync(Point point)
    {
        var menu = View.ElementContextMenu;
        Window.MouseMove(point);
        Window.MouseDown(point, MouseButton.Right);
        Window.MouseUp(point, MouseButton.Right);
        await WindowTestHelper.WaitUntilAsync(() => menu.IsOpen, "the context menu");
        await WindowTestHelper.PumpAsync();
        return menu;
    }

    /// <summary>
    /// Drops items on the tree, as <see cref="Drop"/>, and waits for the entry of the drop in the history: a test that counts
    /// the entries counts from there.
    /// </summary>
    public async Task DropAsync(IReadOnlyCollection<object> items, Point positionInTree)
    {
        var history = Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        Drop(items, positionInTree);
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count > count, "the history entry of the drop");
    }

    /// <summary>
    /// Drops items on the tree at a point of the tree, as a drag from the toolbox or from the tree itself.
    /// </summary>
    public void Drop(IReadOnlyCollection<object> items, Point positionInTree)
    {
        var point = ToWindow(positionInTree);
        var data = AssetDragDropHelper.CreateItemData(items);
        Window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Move, RawInputModifiers.None);
        Window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Move, RawInputModifiers.None);
        Window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Move, RawInputModifiers.None);
    }

    /// <summary>
    /// The assemblies of the UI editor, registered as a session that loads the UI package does (the editor package and its
    /// Avalonia companion), once per process: the assets are then UI pages and libraries for the session that opens them.
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
