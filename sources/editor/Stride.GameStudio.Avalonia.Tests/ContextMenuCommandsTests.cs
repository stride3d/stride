// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Commands;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The context menus bind their built-in items to commands of the registry by identifier, which act on the selection the menu
/// opened on and show the user's shortcuts; the asset editors' shortcuts are commands scoped to their editor.
/// </summary>
public sealed class ContextMenuCommandsTests
{
    private const string Folder = "Models";
    private const string AssetLocation = Folder + "/Sphere";
    private const string SheetLocation = "Sheet";

    [AvaloniaFact]
    public async Task TheContextMenusBindTheirItemsByIdentifier()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                var host = fixture.Plugins;

                // The asset view
                fixture.Session.AssetCollection.SelectAssets([asset]);
                await WindowTestHelper.PumpAsync();
                var list = AssetList(window);
                await OpenAsync(list.ContextMenu!, list);
                Assert.Equal(
                [
                    SessionCommands.EditSelectedId, SessionCommands.ToggleRootId,
                    GameStudioCommands.CutId, GameStudioCommands.CopyId, SessionCommands.CopyAssetsRecursivelyId, GameStudioCommands.PasteId, GameStudioCommands.DeleteId,
                    SessionCommands.CopyUrlId, GameStudioCommands.RenameId,
                    SessionCommands.NewFolderId, SessionCommands.AddAssetId, SessionCommands.UpdateSelectedFromSourceId, SessionCommands.UpdateAllFromSourceId,
                    SessionCommands.OpenWithTextEditorId, SessionCommands.OpenAssetFileId, SessionCommands.OpenSourceFileId, SessionCommands.ShowInExplorerId, SessionCommands.ShowSourceInExplorerId,
                ], BoundIds(list.ContextMenu!));
                AssertBound(list.ContextMenu!, host);
                list.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();

                // The solution explorer, on the package
                var locations = fixture.Session.AssetCollection.SelectedLocations;
                locations.Clear();
                locations.Add(fixture.Package);
                await WindowTestHelper.PumpAsync();
                var tree = SolutionTree(window);
                await OpenAsync(tree.ContextMenu!, tree);
                Assert.Equal(
                [
                    SessionCommands.NewFolderId,
                    SessionCommands.AddDependencyId, SessionCommands.SetCurrentProjectId, SessionCommands.PackagePropertiesId,
                    SessionCommands.NewProjectId, SessionCommands.AddExistingProjectId, SessionCommands.OpenInIdeId,
                    GameStudioCommands.CutId, GameStudioCommands.CopyId, GameStudioCommands.PasteId, GameStudioCommands.DeleteId, GameStudioCommands.RenameId,
                    SessionCommands.AddAssetId, SessionCommands.ShowInExplorerId,
                ], BoundIds(tree.ContextMenu!));
                AssertBound(tree.ContextMenu!, host);
                tree.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();

                // A property of the property grid
                var item = await ShowPropertyAsync(fixture, window, asset, nameof(TextPropertiesAsset.Label));
                await OpenAsync(item.ContextMenu!, item);
                Assert.Equal([PropertyGridCommands.ResetToBaseId, GameStudioCommands.CopyId, GameStudioCommands.PasteId, PropertyGridCommands.ReplaceId], BoundIds(item.ContextMenu!));
                AssertBound(item.ContextMenu!, host);
                var replace = MenuItems(item.ContextMenu!).Single(x => x.Command is CommandReference { Id: PropertyGridCommands.ReplaceId });
                Assert.Equal(new KeyGesture(Key.V, KeyModifiers.Control | KeyModifiers.Shift), replace.InputGesture);
                item.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ACommandOfTheAssetMenuActsOnTheSelectionTheMenuOpenedOn()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                fixture.Session.AssetCollection.SelectAssets([asset]);
                await WindowTestHelper.PumpAsync();
                // The focus is in the solution explorer: the Edit menu's Copy would copy its folder
                var tree = SolutionTree(window);
                Assert.True(tree.Focus(), "The solution explorer does not take the focus");

                var list = AssetList(window);
                var items = await OpenAsync(list.ContextMenu!, list);
                var copy = items.OfType<MenuItem>().Single(x => x.Command is CommandReference { Id: GameStudioCommands.CopyId });
                Assert.Equal("Copy", copy.Header);
                Assert.True(copy.IsEffectivelyEnabled);
                copy.Command!.Execute(null);
                list.ContextMenu!.Close();
                var text = await WaitForClipboardAsync(window);
                Assert.Contains("Sphere", text);
                Assert.Contains(nameof(TextPropertiesAsset), text);

                // Copy asset URL
                await window.Clipboard!.SetTextAsync(string.Empty);
                items = await OpenAsync(list.ContextMenu!, list);
                var copyUrl = items.OfType<MenuItem>().Single(x => x.Command is CommandReference { Id: SessionCommands.CopyUrlId });
                Assert.Equal("Copy asset URL", copyUrl.Header);
                copyUrl.Command!.Execute(null);
                list.ContextMenu!.Close();
                Assert.Equal(asset.Url, await WaitForClipboardAsync(window));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task AUsersShortcutShowsInTheContextMenuAndRunsItsCommand()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var overrides = GameStudioEditorSettings.ShortcutOverrides;
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                fixture.Session.AssetCollection.SelectAssets([asset]);
                var list = AssetList(window);

                // The defaults first: Add asset... shows Ctrl+I, Copy asset URL has no shortcut
                var items = await OpenAsync(list.ContextMenu!, list);
                Assert.Equal(KeyGesture.Parse("Ctrl+I"), Item(items, SessionCommands.AddAssetId).InputGesture);
                Assert.Null(Item(items, SessionCommands.CopyUrlId).InputGesture);
                list.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();

                overrides.SetValue(new Dictionary<string, string>
                {
                    [SessionCommands.AddAssetId] = "Ctrl+Alt+I",
                    [SessionCommands.CopyUrlId] = "Ctrl+Alt+U",
                });
                window.View.CommandSlots!.Update();
                items = await OpenAsync(list.ContextMenu!, list);
                Assert.Equal(KeyGesture.Parse("Ctrl+Alt+I"), Item(items, SessionCommands.AddAssetId).InputGesture);
                Assert.Equal(KeyGesture.Parse("Ctrl+Alt+U"), Item(items, SessionCommands.CopyUrlId).InputGesture);
                list.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();

                // The user's shortcut runs the command on the asset view's selection
                await window.Clipboard!.SetTextAsync(string.Empty);
                Assert.True(list.Focus());
                window.KeyPressQwerty(PhysicalKey.U, RawInputModifiers.Control | RawInputModifiers.Alt);
                window.KeyReleaseQwerty(PhysicalKey.U, RawInputModifiers.Control | RawInputModifiers.Alt);
                Assert.Equal(asset.Url, await WaitForClipboardAsync(window));
            }
            finally
            {
                overrides.SetValue(new Dictionary<string, string>());
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task AnEditorsShortcutRunsOnlyWhileItHasTheFocusAndTheMainWindowsElsewhere()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var overrides = GameStudioEditorSettings.ShortcutOverrides;
        var runs = new List<string>();
        var plugin = new CommandsTestPlugin(context => context.Commands.Register(new CommandDescriptor
        {
            Id = "Tests.InMainWindow",
            Title = () => "In the main window",
            DefaultGesture = "Ctrl+Alt+M",
            ExecuteAsync = _ =>
            {
                runs.Add("main");
                return Task.CompletedTask;
            },
        }));
        try
        {
            fixture.Plugins.PluginsDiscovered([plugin]);
            var sheet = fixture.Asset(SheetLocation);
            fixture.Session.AssetCollection.SelectAssets([sheet]);
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<SpriteSheetEditorView>().Any(), "the sprite editor");
            var editorView = window.GetVisualDescendants().OfType<SpriteSheetEditorView>().Single();
            var editor = (SpriteSheetEditorViewModel)editorView.DataContext!;
            var spriteList = editorView.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "SpriteList");
            var assetList = AssetList(window);

            // The editor's command, scoped to it, on the Keyboard shortcuts page with its category
            var magicWand = Assert.Single(fixture.Plugins.Commands.Items, x => x.Id == SpriteSheetEditorCommands.ToggleMagicWandId);
            Assert.Equal(typeof(SpriteSheetEditorViewModel), magicWand.GestureScope);
            Assert.Equal("M", window.View.CommandSlots!.ShortcutOf(SpriteSheetEditorCommands.ToggleMagicWandId)?.ToString());

            // M in the editor toggles the magic wand; outside of it, nothing
            Assert.NotEqual(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);
            Assert.True(spriteList.Focus());
            Press(window, PhysicalKey.M, RawInputModifiers.None);
            Assert.Equal(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);
            Assert.True(assetList.Focus());
            Press(window, PhysicalKey.M, RawInputModifiers.None);
            Assert.Equal(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);

            // A key that types a character stays with a text box of the editor
            var textBox = new TextBox();
            spriteList.FindAncestorOfType<Panel>()!.Children.Add(textBox);
            await WindowTestHelper.PumpAsync();
            Assert.True(textBox.Focus());
            Press(window, PhysicalKey.M, RawInputModifiers.None);
            Assert.Equal(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);

            // The user gives the editor's command the main window's shortcut: it runs in the editor, the main window's elsewhere
            overrides.SetValue(new Dictionary<string, string> { [SpriteSheetEditorCommands.ToggleMagicWandId] = "Ctrl+Alt+M" });
            window.View.CommandSlots.Update();
            Assert.True(spriteList.Focus());
            Press(window, PhysicalKey.M, RawInputModifiers.Control | RawInputModifiers.Alt);
            Assert.NotEqual(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);
            Assert.Empty(runs);
            Press(window, PhysicalKey.M, RawInputModifiers.None);
            Assert.NotEqual(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);
            Assert.True(assetList.Focus());
            Press(window, PhysicalKey.M, RawInputModifiers.Control | RawInputModifiers.Alt);
            Assert.Equal(["main"], runs);
            Assert.NotEqual(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);
        }
        finally
        {
            overrides.SetValue(new Dictionary<string, string>());
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheEntityTreeMenusBindTheSceneCommandsAndCtrlDDuplicatesInTheEditorOnly()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var editor = scene.Editor;
        var host = scene.Fixture.Plugins;

        // An item of the tree: the scene commands act on that editor, the Edit commands on the tree
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(scene.Entity("Box"));
        var menu = (ContextMenu)scene.View.Resources["ItemContextMenu"]!;
        scene.Row("Box").RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(menu.IsOpen);
        Assert.Equal(
        [
            SceneEditorCommands.NewFolderId, SceneEditorCommands.NewEntityId, SceneEditorCommands.SetActiveSceneId,
            SceneEditorCommands.OpenPrefabId, SceneEditorCommands.SelectPrefabId, SceneEditorCommands.BreakLinkToPrefabId, SceneEditorCommands.CreatePrefabFromSelectionId,
            SceneEditorCommands.DuplicateId, GameStudioCommands.CutId, GameStudioCommands.CopyId, GameStudioCommands.PasteId, GameStudioCommands.DeleteId, GameStudioCommands.RenameId,
        ], BoundIds(menu));
        AssertBound(menu, host);
        var duplicate = Item(menu.Items.OfType<Control>(), SceneEditorCommands.DuplicateId);
        Assert.Equal("Duplicate", duplicate.Header);
        Assert.Equal(KeyGesture.Parse("Ctrl+D"), duplicate.InputGesture);
        Assert.True(duplicate.IsEffectivelyEnabled);
        // The Edit commands follow the editor's commands, which the tree binds (no clipboard in this session: Copy is off)
        Assert.True(editor.DeleteCommand.CanExecute(null));
        Assert.True(Item(menu.Items.OfType<Control>(), GameStudioCommands.DeleteId).IsEffectivelyEnabled);
        Assert.True(Item(menu.Items.OfType<Control>(), GameStudioCommands.RenameId).IsEffectivelyEnabled);
        Assert.Equal(editor.CopyCommand.CanExecute(null), Item(menu.Items.OfType<Control>(), GameStudioCommands.CopyId).IsEffectivelyEnabled);
        // The items under the Create heading keep their own headers
        Assert.Equal("Folder", Item(menu.Items.OfType<Control>(), SceneEditorCommands.NewFolderId).Header);
        menu.Close();
        await WindowTestHelper.PumpAsync();

        // The empty area: the creation and the paste go to the root
        var rootMenu = (ContextMenu)scene.View.Resources["RootContextMenu"]!;
        scene.Tree.RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(rootMenu.IsOpen);
        var ids = BoundIds(rootMenu);
        Assert.Equal([SceneEditorCommands.NewFolderInRootId, SceneEditorCommands.NewEntityInRootId], ids.Take(2));
        Assert.Contains(SceneEditorCommands.PasteInRootId, ids);
        Assert.DoesNotContain(GameStudioCommands.PasteId, ids);
        AssertBound(rootMenu, host);
        rootMenu.Close();
        await WindowTestHelper.PumpAsync();

        // Ctrl+D with the focus in the tree duplicates the selection; in the asset view, it does nothing
        var count = editor.HierarchyRoot.Children.Count;
        Assert.True(AssetList(scene.Window).Focus());
        Press(scene.Window, PhysicalKey.D, RawInputModifiers.Control);
        await WindowTestHelper.PumpAsync();
        Assert.Equal(count, editor.HierarchyRoot.Children.Count);
        Assert.True(scene.Row("Box").Focus());
        Press(scene.Window, PhysicalKey.D, RawInputModifiers.Control);
        await WindowTestHelper.WaitUntilAsync(() => editor.HierarchyRoot.Children.Count == count + 1, "the duplicate");
    }

    [AvaloniaFact]
    public async Task ThePaletteListsTheContextCommandsThatAllowIt()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var ids = window.View.CommandSlots!.GetPaletteItems(null).Select(x => x.Descriptor.Id).ToList();
            Assert.Contains(SessionCommands.CopyUrlId, ids);
            Assert.Contains(SessionCommands.ShowInExplorerId, ids);
            Assert.Contains(SessionCommands.SetCurrentProjectId, ids);
            Assert.Contains(SceneEditorCommands.DuplicateId, ids);
            Assert.Contains(SceneEditorCommands.NewEntityInRootId, ids);
            Assert.Contains(SpriteSheetEditorCommands.ToggleMagicWandId, ids);
            // Those that act on what their menu opened on
            Assert.DoesNotContain(PropertyGridCommands.ResetToBaseId, ids);
            Assert.DoesNotContain(PropertyGridCommands.ReplaceId, ids);
            Assert.DoesNotContain(SceneEditorCommands.PasteInRootId, ids);
            Assert.DoesNotContain(SceneEditorCommands.NewEntityAtCursorId, ids);
            Assert.DoesNotContain(SceneEditorCommands.CutId, ids);

            // The palette runs the asset view's commands on its selection
            var sheet = fixture.Asset(SheetLocation);
            fixture.Session.AssetCollection.SelectAssets([sheet]);
            await WindowTestHelper.PumpAsync();
            var item = window.View.CommandSlots.GetPaletteItems(null).Single(x => x.Descriptor.Id == SessionCommands.OpenWithTextEditorId);
            Assert.True(item.IsEnabled);
            fixture.Session.AssetCollection.ClearSelection();
            await WindowTestHelper.PumpAsync();
            item = window.View.CommandSlots.GetPaletteItems(null).Single(x => x.Descriptor.Id == SessionCommands.OpenWithTextEditorId);
            Assert.False(item.IsEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static void Press(TopLevel topLevel, PhysicalKey key, RawInputModifiers modifiers)
    {
        topLevel.KeyPressQwerty(key, modifiers);
        topLevel.KeyReleaseQwerty(key, modifiers);
    }

    private static MenuItem Item(IEnumerable<Control> items, string id) => items.OfType<MenuItem>().Single(x => x.Command is CommandReference reference && reference.Id == id);

    private static IEnumerable<MenuItem> MenuItems(ContextMenu menu) => menu.Items.OfType<MenuItem>();

    // The identifiers of the items bound by identifier, in order
    private static List<string> BoundIds(ContextMenu menu) => MenuItems(menu).Select(x => x.Command).OfType<CommandReference>().Select(x => x.Id).ToList();

    // Every item bound by identifier has a command of the host, its title and the shortcut bound to it
    private static void AssertBound(ContextMenu menu, Core.Assets.Editor.Services.EditorPluginHost host)
    {
        foreach (var item in MenuItems(menu).Where(x => x.Command is CommandReference))
        {
            var reference = (CommandReference)item.Command!;
            Assert.NotNull(reference.Target);
            Assert.Single(host.Commands.Items, x => x.Id == reference.Id);
            Assert.False(string.IsNullOrEmpty(item.Header?.ToString()), reference.Id);
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync(params (string Location, Asset Asset)[] assets)
    {
        var fixture = ShellSessionFixture.Create(assets);
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // The shell registers the clipboard of its main window before the session opens; the headless platform keeps one in memory
        fixture.ServiceProvider.RegisterService(new ClipboardService(window.Clipboard!));
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    private static ListBox AssetList(MainWindow window) => window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");

    private static TreeView SolutionTree(MainWindow window) => window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "SolutionTree");

    // Opens a context menu the way a right-click does and gives its visible items
    private static async Task<List<Control>> OpenAsync(ContextMenu menu, Control control)
    {
        control.RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(menu.IsOpen);
        return menu.Items.OfType<Control>().Where(x => x.IsVisible).ToList();
    }

    private static async Task<string> WaitForClipboardAsync(MainWindow window)
    {
        string? text = null;
        await WindowTestHelper.WaitUntilAsync(() => !string.IsNullOrEmpty(text = window.Clipboard!.TryGetTextAsync().Result), "the clipboard");
        return text!;
    }

    private static async Task<PropertyViewItem> ShowPropertyAsync(ShellSessionFixture fixture, MainWindow window, Core.Assets.Presentation.ViewModels.AssetViewModel asset, string member)
    {
        fixture.Session.AssetCollection.SelectAssets([asset]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        PropertyViewItem? item = null;
        await WindowTestHelper.WaitUntilAsync(() => (item = grid.GetVisualDescendants().OfType<PropertyViewItem>().FirstOrDefault(x => x.DataContext is NodeViewModel node && node.Name == member)) is not null, $"the property {member}");
        // and settle: a second generation would replace the item
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return item!;
    }
}
