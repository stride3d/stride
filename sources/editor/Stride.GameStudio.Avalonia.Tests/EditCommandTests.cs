// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Cut, copy, paste, delete and rename from the Edit menu, the keyboard and the context menus: they act on the panel that has
/// the focus, as the routed commands of the WPF editor do.
/// </summary>
public sealed class EditCommandTests
{
    private const string Folder = "Models";
    private const string OtherFolder = "Textures";
    private const string AssetLocation = Folder + "/Sphere";
    private const string OtherAssetLocation = OtherFolder + "/Wood";

    [AvaloniaFact]
    public async Task TheEditMenuHasTheItemsAndShortcutsOfWpf()
    {
        using var fixture = ShellSessionFixture.Create();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var edit = window.GetLogicalDescendants().OfType<MenuItem>().Single(x => Equals(x.Header, "Edit"));
            Assert.Equal(["Undo", "Redo", "-", "Cut", "Copy", "Paste", "Delete", "Rename", "-", "_Settings..."], Describe(edit.Items));
            Assert.Equal(new KeyGesture(Key.X, KeyModifiers.Control), MenuItem(window, "CutMenuItem").InputGesture);
            Assert.Equal(new KeyGesture(Key.C, KeyModifiers.Control), MenuItem(window, "CopyMenuItem").InputGesture);
            Assert.Equal(new KeyGesture(Key.V, KeyModifiers.Control), MenuItem(window, "PasteMenuItem").InputGesture);
            Assert.Equal(new KeyGesture(Key.Delete), MenuItem(window, "DeleteMenuItem").InputGesture);
            Assert.Equal(new KeyGesture(Key.F2), MenuItem(window, "RenameMenuItem").InputGesture);
            // Nothing has the focus: the items have nothing to act on
            Assert.False(await IsEnabledInOpenMenuAsync(MenuItem(window, "CopyMenuItem")));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// Copy and Rename go to the asset view, the solution explorer or a text box, whichever has the focus.
    /// </summary>
    [AvaloniaFact]
    public async Task TheEditMenuActsOnTheFocusedPanel()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                var folder = asset.Directory;
                var list = AssetList(window);
                var tree = SolutionTree(window);
                var copy = MenuItem(window, "CopyMenuItem");
                var rename = MenuItem(window, "RenameMenuItem");

                // The asset view with an asset selected: Copy puts the asset on the clipboard, Rename edits its name in place
                fixture.Session.AssetCollection.SelectAssets([asset]);
                await WindowTestHelper.PumpAsync();
                Assert.True(list.Focus(), "The asset view does not take the focus");
                Assert.True(await IsEnabledInOpenMenuAsync(copy));
                Assert.True(await IsEnabledInOpenMenuAsync(rename));
                copy.Command!.Execute(null);
                var text = await WaitForClipboardAsync(window);
                Assert.Contains("Sphere", text);
                Assert.Contains(nameof(TextPropertiesAsset), text);
                rename.Command!.Execute(null);
                Assert.True(asset.IsEditing);
                asset.IsEditing = false;

                // The solution explorer with the folder selected: Rename edits the folder
                fixture.Session.AssetCollection.ClearSelection();
                await WindowTestHelper.PumpAsync();
                Assert.True(tree.Focus(), "The solution explorer does not take the focus");
                Assert.Equal([folder], fixture.Session.AssetCollection.SelectedLocations);
                Assert.True(await IsEnabledInOpenMenuAsync(rename));
                rename.Command!.Execute(null);
                Assert.False(asset.IsEditing);
                Assert.True(folder.IsEditing);
                folder.IsEditing = false;

                // A text box: Copy copies its selected text and Rename has nothing to do
                await window.Clipboard!.ClearAsync();
                var textBox = FilterBox(window);
                Assert.True(textBox.Focus());
                window.KeyTextInput("abc");
                textBox.SelectAll();
                await WindowTestHelper.PumpAsync();
                Assert.True(await IsEnabledInOpenMenuAsync(copy));
                Assert.False(await IsEnabledInOpenMenuAsync(rename));
                copy.Command!.Execute(null);
                Assert.Equal("abc", await WaitForClipboardAsync(window));
                Assert.False(asset.IsEditing);
                Assert.False(folder.IsEditing);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Ctrl+X in the asset view, then Ctrl+V in another folder: the cut and the paste make one history entry each, and undoing
    /// both puts the asset back.
    /// </summary>
    [AvaloniaFact]
    public async Task CutAndPasteMoveAnAssetBetweenFoldersWithOneHistoryEntryEach()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }), (OtherAssetLocation, new TextPropertiesAsset { Label = "wood" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                var target = fixture.Asset(OtherAssetLocation).Directory;
                var assets = fixture.Session.AssetCollection;
                var history = fixture.Session.ActionHistory;
                var list = AssetList(window);

                assets.SelectAssets([asset]);
                await WindowTestHelper.PumpAsync();
                Assert.True(list.Focus());
                window.KeyPressQwerty(PhysicalKey.X, RawInputModifiers.Control);
                window.KeyReleaseQwerty(PhysicalKey.X, RawInputModifiers.Control);
                await WindowTestHelper.WaitUntilAsync(() => asset.IsDeleted, "the cut");
                await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry of the cut");
                Assert.Null(fixture.Session.GetAssetByUrl(ShellSessionFixture.Url(AssetLocation)));

                assets.SelectedLocations.Clear();
                assets.SelectedLocations.Add(target);
                await WindowTestHelper.PumpAsync();
                Assert.True(list.Focus());
                window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
                window.KeyReleaseQwerty(PhysicalKey.V, RawInputModifiers.Control);
                var pastedUrl = ShellSessionFixture.Url(OtherFolder + "/Sphere");
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.GetAssetByUrl(pastedUrl) is not null, "the pasted asset");
                await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 2, "the history entry of the paste");
                Assert.Equal("label", ((TextPropertiesAsset)fixture.Session.GetAssetByUrl(pastedUrl)!.Asset).Label);

                // Undo the paste, then the cut
                history.UndoCommand.Execute();
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.GetAssetByUrl(pastedUrl) is null, "the undone paste");
                Assert.Null(fixture.Session.GetAssetByUrl(ShellSessionFixture.Url(AssetLocation)));
                history.UndoCommand.Execute();
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.GetAssetByUrl(ShellSessionFixture.Url(AssetLocation)) is not null, "the undone cut");
                Assert.Null(fixture.Session.GetAssetByUrl(pastedUrl));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    /// <summary>
    /// F2 on the selected asset shows a box with its name; Enter renames the asset in one history entry.
    /// </summary>
    [AvaloniaFact]
    public async Task F2RenamesTheSelectedAssetInPlace()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                var list = AssetList(window);
                fixture.Session.AssetCollection.SelectAssets([asset]);
                await WindowTestHelper.PumpAsync();
                Assert.True(list.Focus());

                window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
                window.KeyReleaseQwerty(PhysicalKey.F2, RawInputModifiers.None);
                Assert.True(asset.IsEditing);
                TextBox? box = null;
                await WindowTestHelper.WaitUntilAsync(() => (box = list.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.IsFocused)) is not null, "the name box");
                Assert.Equal("Sphere", box!.Text);
                Assert.Equal("Sphere", box.SelectedText);

                window.KeyTextInput("Cube");
                window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
                window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
                await WindowTestHelper.WaitUntilAsync(() => asset.Name == "Cube", "the rename");
                Assert.False(asset.IsEditing);
                Assert.Equal(ShellSessionFixture.Url(Folder + "/Cube"), asset.Url);
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.Transactions.Count == 1, "the history entry");
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheAssetContextMenuHasTheItemsOfWpf()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                var list = AssetList(window);
                var menu = list.ContextMenu!;
                // One asset selected: the items of WPF, with the commands of the asset (an archetype can be derived from any
                // asset) inlined after the asset items. The test asset has no editor and no archetype.
                fixture.Session.AssetCollection.SelectAssets([asset]);
                await WindowTestHelper.PumpAsync();
                var items = await OpenAsync(menu, list);
                Assert.Equal(
                [
                    "Asset", "Edit asset...", "Include in build as root asset (in TestGame)", "Include in build as root asset in",
                    "-", "Create derived asset", "Create replacing asset", "Clear archetype", "Clear replaces",
                    "-", "Cut", "Copy", "Copy with dependencies", "Paste", "Delete",
                    "-", "Copy asset URL", "Rename",
                    "-", "Create folder", "Update selected assets from their source", "Update all assets with modified source",
                ], Describe(items));
                Assert.Equal(
                [
                    "Include in build as root asset (in TestGame)", "Include in build as root asset in",
                    "Create derived asset", "Create replacing asset",
                    "Cut", "Copy", "Copy with dependencies", "Paste", "Delete", "Copy asset URL", "Rename",
                    "Create folder", "Update selected assets from their source",
                ], Enabled(items));
                // The default target of the root-asset submenu is bold, as in WPF
                var targets = (MenuItem)items.Single(x => x is MenuItem { Header: "Include in build as root asset in" });
                targets.Open();
                await WindowTestHelper.PumpAsync();
                var target = Assert.Single(targets.GetLogicalChildren().OfType<MenuItem>());
                Assert.Equal(fixture.Package.Name, target.Header);
                Assert.Equal(FontWeight.Bold, target.FontWeight);
                targets.Close();
                menu.Close();
                await WindowTestHelper.PumpAsync();

                // Nothing selected in the folder: no asset item, Paste and the folder commands apply
                fixture.Session.AssetCollection.ClearSelection();
                await WindowTestHelper.PumpAsync();
                items = await OpenAsync(menu, list);
                Assert.Equal(
                [
                    "Asset", "Edit asset...", fixture.Session.RootAssetToggleHeader,
                    "-", "Cut", "Copy", "Copy with dependencies", "Paste", "Delete",
                    "-", "Copy asset URL", "Rename",
                    "-", "Create folder", "Update selected assets from their source", "Update all assets with modified source",
                ], Describe(items));
                var enabled = Enabled(items);
                Assert.Contains("Paste", enabled);
                Assert.Contains("Create folder", enabled);
                Assert.DoesNotContain("Cut", enabled);
                Assert.DoesNotContain("Copy", enabled);
                Assert.DoesNotContain("Delete", enabled);
                Assert.DoesNotContain("Rename", enabled);
                Assert.DoesNotContain("Copy asset URL", enabled);
                menu.Close();
                await WindowTestHelper.PumpAsync();
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheSolutionContextMenuHasTheItemsOfWpf()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var folder = fixture.Asset(AssetLocation).Directory;
                var locations = fixture.Session.AssetCollection.SelectedLocations;
                var tree = SolutionTree(window);
                var menu = tree.ContextMenu!;

                // A folder: the folder, package (its properties apply to a folder, as in WPF) and solution groups, then the actions
                locations.Clear();
                locations.Add(folder);
                await WindowTestHelper.PumpAsync();
                var items = await OpenAsync(menu, tree);
                Assert.Equal(["Folder", "Create folder", "Package", "Package properties", "Solution", "Add existing project...", "Actions", "Cut", "Copy", "Paste", "Delete", "Rename"], Describe(items));
                Assert.Equal(["Create folder", "Package properties", "Add existing project...", "Cut", "Copy", "Paste", "Delete", "Rename"], Enabled(items));
                menu.Close();
                await WindowTestHelper.PumpAsync();

                // The project: it can be made the current project
                locations.Clear();
                locations.Add(fixture.Package);
                await WindowTestHelper.PumpAsync();
                items = await OpenAsync(menu, tree);
                Assert.Equal(["Folder", "Create folder", "Package", "Set as current project", "Package properties", "Solution", "Add existing project...", "Actions", "Cut", "Copy", "Paste", "Delete", "Rename"], Describe(items));
                Assert.Equal(["Create folder", "Set as current project", "Package properties", "Add existing project...", "Cut", "Copy", "Paste", "Delete", "Rename"], Enabled(items));
                menu.Close();
                await WindowTestHelper.PumpAsync();
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    /// <summary>
    /// A property of the grid has the copy, paste and replace of the WPF property grid: in its context menu, and from the Edit
    /// menu while it has the focus.
    /// </summary>
    [AvaloniaFact]
    public async Task APropertyCopiesAndPastesItsValue()
    {
        var (fixture, window) = await OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        using (fixture)
        {
            try
            {
                var asset = fixture.Asset(AssetLocation);
                var item = await ShowPropertyAsync(fixture, window, asset, nameof(TextPropertiesAsset.Label));
                // Nothing on the clipboard yet, and the asset has no archetype to reset to
                var items = await OpenAsync(item.ContextMenu!, item);
                Assert.Equal(["Reset to base value", "-", "Copy", "Paste", "Replace"], Describe(items));
                Assert.Equal(["Copy"], Enabled(items));
                item.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();

                // Copy the value, change it, paste it back: the property has the focus
                Assert.True(item.Focus(), "The property does not take the focus");
                var copy = MenuItem(window, "CopyMenuItem");
                var paste = MenuItem(window, "PasteMenuItem");
                Assert.True(await IsEnabledInOpenMenuAsync(copy));
                Assert.True(item.Focus());
                copy.Command!.Execute(null);
                Assert.Contains("label", await WaitForClipboardAsync(window));
                items = await OpenAsync(item.ContextMenu!, item);
                Assert.Equal(["Copy", "Paste", "Replace"], Enabled(items));
                item.ContextMenu!.Close();
                await WindowTestHelper.PumpAsync();

                Edit(fixture, asset, "edited");
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.CanUndo, "the history");
                item = await ShowPropertyAsync(fixture, window, asset, nameof(TextPropertiesAsset.Label));
                Assert.True(item.Focus());
                Assert.True(await IsEnabledInOpenMenuAsync(paste));
                Assert.True(item.Focus());
                paste.Command!.Execute(null);
                await WindowTestHelper.WaitUntilAsync(() => Equals(LabelOf(asset), "label"), "the pasted value");

                // Replace with its shortcut
                Edit(fixture, asset, "edited again");
                await WindowTestHelper.WaitUntilAsync(() => Equals(LabelOf(asset), "edited again"), "the edit");
                item = await ShowPropertyAsync(fixture, window, asset, nameof(TextPropertiesAsset.Label));
                Assert.True(item.Focus());
                window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control | RawInputModifiers.Shift);
                window.KeyReleaseQwerty(PhysicalKey.V, RawInputModifiers.Control | RawInputModifiers.Shift);
                await WindowTestHelper.WaitUntilAsync(() => Equals(LabelOf(asset), "label"), "the replaced value");
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync(params (string Location, Asset Asset)[] assets)
    {
        var fixture = ShellSessionFixture.Create(assets);
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        // The shell registers the clipboard of its main window before the session opens; the headless platform keeps one in memory
        fixture.ServiceProvider.RegisterService(new ClipboardService(window.Clipboard!));
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    private static ListBox AssetList(MainWindow window) => window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");

    private static TreeView SolutionTree(MainWindow window) => window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "SolutionTree");

    /// <summary>
    /// The filter box of the asset view: a plain text box of the shell.
    /// </summary>
    private static TextBox FilterBox(MainWindow window)
    {
        var filter = window.GetVisualDescendants().OfType<SearchComboBox>().Single(x => x.Name == "FilterSearchComboBox");
        return filter.GetVisualDescendants().OfType<TextBox>().First();
    }

    private static MenuItem MenuItem(Window window, string name) => window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == name);

    /// <summary>
    /// Whether the item is enabled when the user opens its menu: Avalonia evaluates the command of an item of a closed menu
    /// only when the menu opens.
    /// </summary>
    private static async Task<bool> IsEnabledInOpenMenuAsync(MenuItem item)
    {
        var menu = Assert.IsType<MenuItem>(item.Parent);
        menu.Open();
        await WindowTestHelper.PumpAsync();
        try
        {
            return item.IsEffectivelyEnabled;
        }
        finally
        {
            menu.Close();
            await WindowTestHelper.PumpAsync();
        }
    }

    /// <summary>
    /// Opens the context menu of a control the way a right-click does (a context request raises the Opening event of the menu,
    /// which fills in the commands of the asset) and gives its visible items.
    /// </summary>
    private static async Task<List<Control>> OpenAsync(ContextMenu menu, Control control)
    {
        control.RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(menu.IsOpen);
        return menu.Items.OfType<Control>().Where(x => x.IsVisible).ToList();
    }

    /// <summary>
    /// The headers of the visible items, "-" for a separator.
    /// </summary>
    private static List<string> Describe(IEnumerable<object?> items)
    {
        return items.OfType<Control>().Where(x => x.IsVisible).Select(x => x is MenuItem item ? item.Header?.ToString() ?? "" : "-").ToList();
    }

    private static List<string> Enabled(IEnumerable<Control> items)
    {
        return items.OfType<MenuItem>().Where(x => x.IsHitTestVisible && x.IsEffectivelyEnabled).Select(x => x.Header?.ToString() ?? "").ToList();
    }

    private static async Task<string> WaitForClipboardAsync(MainWindow window)
    {
        string? text = null;
        await WindowTestHelper.WaitUntilAsync(() => !string.IsNullOrEmpty(text = window.Clipboard!.TryGetTextAsync().Result), "the clipboard");
        return text!;
    }

    /// <summary>
    /// Selects the asset and waits for the grid to show the property.
    /// </summary>
    private static async Task<PropertyViewItem> ShowPropertyAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel asset, string member)
    {
        fixture.Session.AssetCollection.SelectAssets([asset]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        PropertyViewItem? item = null;
        await WindowTestHelper.WaitUntilAsync(() => (item = grid.GetVisualDescendants().OfType<PropertyViewItem>().FirstOrDefault(x => x.DataContext is NodeViewModel node && node.Name == member)) is not null, $"the property {member}");
        // and settle: a second generation would replace the item
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        Assert.True(item!.IsAttachedToVisualTree());
        return item;
    }

    private static object? LabelOf(AssetViewModel asset) => asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Retrieve();

    /// <summary>
    /// Sets the label of the asset through its property graph, in a transaction of the history.
    /// </summary>
    private static void Edit(ShellSessionFixture fixture, AssetViewModel asset, string label)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Update(label);
        undoRedo.SetName(transaction, "Edit label");
    }
}
