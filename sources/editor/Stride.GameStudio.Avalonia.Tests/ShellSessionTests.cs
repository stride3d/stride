// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Materials;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The shell opens a session the way the desktop host does, and its views show it.
/// </summary>
public sealed class ShellSessionTests
{
    [AvaloniaFact]
    public async Task TheMainViewModelOpensASessionAndInitializesThePlugins()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()), ("Material", new MaterialAsset()));

        var session = fixture.Session;
        Assert.True(session.IsEditorInitialized);
        Assert.Equal(ShellSessionFixture.PackageName, fixture.Package.Name);
        Assert.Equal(2, session.AllAssets.Count());
        Assert.IsType<SpriteSheetViewModel>(fixture.Asset("Sheet"));
        Assert.IsType<MaterialViewModel>(fixture.Asset("Material"));
        Assert.EndsWith(ShellSessionFixture.PackageName, fixture.Main.Title);
        Assert.Same(session, fixture.Main.Session);

        // The plugins' session initialization registered the editor services (no thumbnails and no previews in the tests)
        Assert.NotNull(session.ServiceProvider.TryGet<Stride.Editor.Build.GameStudioBuilderService>());
        Assert.Null(session.ServiceProvider.TryGet<IThumbnailService>());
    }

    [AvaloniaFact]
    public async Task TheSessionAsksForTheAvaloniaToolkit()
    {
        // The host decides which companions the session loads: the neutral ones and the Avalonia ones, never the Wpf ones
        // (the filter itself is tested in Stride.Core.Assets.Tests.TestCompanionToolkits)
        Assert.Equal(EditorToolkits.Avalonia, SessionViewModel.EditorToolkitName);

        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));

        var packageSession = fixture.Package.Package.Session;
        Assert.NotNull(packageSession);
        Assert.Equal(EditorToolkits.Avalonia, packageSession.EditorToolkit);
        Assert.True(packageSession.LoadEditorPackages);
    }

    [AvaloniaFact]
    public async Task TheOpenedSessionIsTheMostRecentOne()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));

        var recent = Assert.Single(fixture.Main.RecentFiles, x => x.FilePath == fixture.PackagePath);
        Assert.Equal(ShellSessionFixture.PackageName, recent.Name);
        Assert.Contains(fixture.Main.MRU.MostRecentlyUsedFiles, x => x.FilePath == fixture.PackagePath);

        // A fresh editor reads it back from the settings, as the startup path when the settings ask to reload the last session
        var mru = Settings.GameStudioInternalSettings.CreateMostRecentlyUsedFiles();
        Assert.Equal(fixture.PackagePath, mru.MostRecentlyUsedFiles.First().FilePath);

        fixture.Main.ClearRecentFilesCommand.Execute();
        Assert.Empty(fixture.Main.RecentFiles);
    }

    [AvaloniaFact]
    public async Task AMissingSessionIsReportedAndForgotten()
    {
        using var fixture = ShellSessionFixture.Create();
        // The message box of the shell's dialog service is modal: a recording one stands in
        var dialogs = fixture.ReplaceDialogService();
        var missing = new Core.IO.UFile(Path.Combine(fixture.Directory, "Missing", "Missing.sln"));
        fixture.Main.MRU.AddFile(missing, Stride.GameStudio.Avalonia.Helpers.StrideGameStudio.EditorVersionMajor);
        Assert.Contains(fixture.Main.RecentFiles, x => x.FilePath == missing);

        var opened = await fixture.Main.OpenSession(missing);

        Assert.False(opened);
        Assert.Null(fixture.Main.Session);
        Assert.DoesNotContain(fixture.Main.RecentFiles, x => x.FilePath == missing);
        Assert.Contains(dialogs.Messages, x => x.Contains("does not exist", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public async Task TheMainWindowShowsTheSolutionExplorerAndTheAssetView()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()), ("Material", new MaterialAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            // The solution explorer lists the package categories, the asset view the assets of the selected location
            var solutionExplorer = window.GetVisualDescendants().OfType<SolutionExplorerView>().Single();
            var tree = solutionExplorer.GetVisualDescendants().OfType<TreeView>().Single();
            Assert.Contains(tree.Items.Cast<object>(), x => x is Stride.Core.Assets.Editor.ViewModel.ICategoryViewModel);

            fixture.Session.AssetCollection.SelectedLocations.Clear();
            fixture.Session.AssetCollection.SelectedLocations.Add(fixture.Package);
            fixture.Session.AssetCollection.DisplayAssetMode = DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
            await WindowTestHelper.PumpAsync();

            var assetView = window.GetVisualDescendants().OfType<AssetExplorerView>().Single();
            var list = assetView.GetVisualDescendants().OfType<ListBox>().Single();
            Assert.Equal(2, list.ItemCount);
            Assert.Equal(["Material", "Sheet"], list.Items.Cast<AssetViewModel>().Select(x => x.Name).Order());
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ThePropertyGridShowsTheSelectedAsset()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var sheet = fixture.Asset("Sheet");
            fixture.Session.AssetCollection.SelectAssets([sheet]);
            // The selection generates the properties asynchronously; the explicit call awaits the same generation
            await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([sheet]);
            await WindowTestHelper.PumpAsync();

            var properties = fixture.Session.ActiveProperties;
            Assert.Same(fixture.Session.AssetViewProperties, properties);
            Assert.Equal(sheet.Url, properties.Name);
            Assert.True(properties.CanDisplayProperties);
            var root = properties.ViewModel?.RootNode;
            Assert.NotNull(root);
            Assert.Contains(root.Children, x => x.Name == nameof(SpriteSheetAsset.Sprites));

            var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
            var view = grid.GetVisualDescendants().OfType<PropertyView>().Single();
            Assert.True(view.IsVisible);
            Assert.Equal(root.Children.Count, view.ItemCount);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task OpeningAnAssetWithAnUnportedEditorShowsItsStub()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()), ("Material", new MaterialAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var sheet = fixture.Asset("Sheet");
            var material = fixture.Asset("Material");
            var plugins = fixture.Session.ServiceProvider.Get<IAssetsPluginService>();
            // The sprite sheet has an editor (its Avalonia view is a stub, "not ported yet"), the material has none
            Assert.True(plugins.HasEditorView(fixture.Session, sheet.GetType()));
            Assert.False(plugins.HasEditorView(fixture.Session, material.GetType()));

            fixture.Session.AssetCollection.SelectAssets([sheet]);
            await WindowTestHelper.PumpAsync();
            Assert.True(fixture.Session.EditSelectedContentCommand.IsEnabled);
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.PumpAsync();

            var editor = Assert.IsType<SpriteSheetEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
            Assert.Same(sheet, editor.Asset);
            var editors = window.GetVisualDescendants().OfType<EditorCollectionView>().Single();
            var tabs = editors.GetVisualDescendants().OfType<TabControl>().Single();
            Assert.Same(editor, tabs.SelectedItem);
            Assert.Single(tabs.GetVisualDescendants().OfType<SpriteSheetEditorView>());

            fixture.Session.AssetCollection.SelectAssets([material]);
            await WindowTestHelper.PumpAsync();
            Assert.False(fixture.Session.EditSelectedContentCommand.IsEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }
}
