// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.Helpers;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Saving the session in the shell, as in the WPF editor: File > Save and Ctrl+S, the "*" of the assets with unsaved changes
/// (and of the window title, which WPF does not have), and the question (Save, Don't save, Cancel) before the session closes.
/// </summary>
public sealed class SessionSaveTests
{
    private const string AssetLocation = "Text";

    [AvaloniaFact]
    public async Task TheSaveItemIsEnabledWhenASessionIsOpen()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var save = SaveItem(window);
            Assert.Equal(new KeyGesture(Key.S, KeyModifiers.Control), save.InputGesture);
            Assert.False(await IsEnabledInOpenMenuAsync(save));

            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();

            // Enabled even without a change, as in WPF
            Assert.False(fixture.Session.HasUnsavedAssets());
            Assert.True(await IsEnabledInOpenMenuAsync(save));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CtrlSSavesTheSession()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            Edit(fixture, asset, "edited");
            Assert.True(fixture.Session.HasUnsavedAssets());

            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            window.KeyRelease(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            await WindowTestHelper.WaitUntilAsync(() => !fixture.Session.HasUnsavedAssets(), "the save of the session");

            Assert.Equal("edited", ReadLabel(fixture));
            Assert.Single(dialogs.ProgressWindows);
            Assert.Empty(dialogs.Messages);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnAssetWithUnsavedChangesIsMarked()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var assets = fixture.Session.AssetCollection;
            assets.DisplayAssetMode = DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
            assets.SelectedLocations.Add(fixture.Package);
            var view = window.GetVisualDescendants().OfType<AssetExplorerView>().Single();
            await WindowTestHelper.WaitUntilAsync(() => Marker(view) is not null, "the tile of the asset");
            Assert.False(Marker(view)!.IsVisible);

            Edit(fixture, asset, "edited");
            await WindowTestHelper.PumpAsync();
            Assert.True(Marker(view)!.IsVisible);

            Assert.True(await fixture.Session.SaveSessionAsync());
            await WindowTestHelper.PumpAsync();
            Assert.False(Marker(view)!.IsVisible);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheWindowTitleMarksUnsavedChanges()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var undoRedo = fixture.Session.UndoRedoService!;
            var clean = $"{StrideGameStudio.EditorTitle} - {ShellSessionFixture.PackageName}";
            Assert.Equal(clean, window.Title);

            Edit(fixture, fixture.Asset(AssetLocation), "edited");
            await WindowTestHelper.PumpAsync();
            Assert.Equal(clean + "*", window.Title);

            // Back to the file: no unsaved change
            undoRedo.Undo();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(clean, window.Title);

            undoRedo.Redo();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(clean + "*", window.Title);

            Assert.True(await fixture.Session.SaveSessionAsync());
            await WindowTestHelper.PumpAsync();
            Assert.Equal(clean, window.Title);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaTheory]
    [InlineData("Save", true, "edited")]
    [InlineData("Don't save", true, "label")]
    [InlineData("Cancel", false, "label")]
    public async Task ClosingTheWindowAsksWhetherToSave(string answer, bool closes, string saved)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        try
        {
            Edit(fixture, fixture.Asset(AssetLocation), "edited");
            string[]? buttons = null;
            dialogs.PickButton = x =>
            {
                buttons = [.. x.Select(y => (string)y.Content!)];
                return x.Single(y => (string)y.Content! == answer).Result;
            };

            await window.CloseAsync();
            if (closes)
                await WindowTestHelper.WaitUntilAsync(() => closed, "the close of the window");

            Assert.Equal(["Save", "Don't save", "Cancel"], buttons);
            Assert.Equal("The project has unsaved changes. Do you want to save it?", Assert.Single(dialogs.Messages));
            Assert.Equal(closes, closed);
            Assert.Equal(saved, ReadLabel(fixture));
            if (!closes)
            {
                Assert.Same(fixture.Session, fixture.Main.Session);
                Assert.True(fixture.Session.HasUnsavedAssets());
            }
        }
        finally
        {
            if (!closed)
            {
                dialogs.PickButton = x => x.Single(b => (string)b.Content! == "Don't save").Result;
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ClosingACleanSessionDoesNotAsk()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        try
        {
            await window.CloseAsync();
            await WindowTestHelper.WaitUntilAsync(() => closed, "the close of the window");

            Assert.Empty(dialogs.Messages);
        }
        finally
        {
            if (!closed)
                await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ReloadingAndClosingTheProjectAskToo()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            Edit(fixture, fixture.Asset(AssetLocation), "edited");
            dialogs.PickButton = x => x.Single(b => b.IsCancel).Result;

            // File > Reload project, then Cancel
            fixture.Main.ReloadCommand.Execute();
            await WindowTestHelper.PumpAsync();
            // File > Close, then Cancel
            fixture.Main.CloseCommand.Execute();
            await WindowTestHelper.PumpAsync();

            Assert.Equal(2, dialogs.Messages.Count);
            Assert.Same(fixture.Session, fixture.Main.Session);
            Assert.True(fixture.Session.HasUnsavedAssets());
        }
        finally
        {
            dialogs.PickButton = x => x.Single(b => (string)b.Content! == "Don't save").Result;
            await window.CloseAsync();
        }
    }

    private static MenuItem SaveItem(Window window) => window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == "SaveMenuItem");

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
    /// The "*" after the name of the asset in the asset view.
    /// </summary>
    private static TextBlock? Marker(AssetExplorerView view)
    {
        return view.GetVisualDescendants().OfType<TextBlock>().SingleOrDefault(x => x.Text == "*" && x.DataContext is AssetViewModel);
    }

    /// <summary>
    /// Sets the label of the asset the way the property grid does: through its property graph, in a transaction of the history.
    /// </summary>
    private static void Edit(ShellSessionFixture fixture, AssetViewModel asset, string label)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Update(label);
        undoRedo.SetName(transaction, "Edit label");
    }

    /// <summary>
    /// The label saved in the file of the asset.
    /// </summary>
    private static string? ReadLabel(ShellSessionFixture fixture)
    {
        var path = Path.Combine(fixture.PackageDirectory, "Assets", AssetLocation + ".sdtextpropertiestest");
        return ((TextPropertiesAsset)AssetFileSerializer.Load<TextPropertiesAsset>(path).Asset).Label;
    }
}
