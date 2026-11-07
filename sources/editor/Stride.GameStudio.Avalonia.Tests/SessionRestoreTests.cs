// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// What opens with a session, as in WPF: the editors of the last session, or the default scene; the list of open editors is
/// kept in the internal settings that both editors share.
/// </summary>
public sealed class SessionRestoreTests
{
    [AvaloniaFact]
    public async Task TheDefaultSceneOpensWhenTheSessionKeptNoEditor()
    {
        var scene = new SceneAsset();
        var gameSettings = new GameSettingsAsset { DefaultScene = AttachedReferenceManager.CreateProxyObject<Scene>(scene.Id, "MainScene") };
        using var fixture = await ShellSessionFixture.OpenAsync(("MainScene", scene), (GameSettingsAsset.GameSettingsLocation, gameSettings), ("Sheet", new SpriteSheetAsset()));

        var editor = Assert.Single(fixture.Session.EditorCollection.Editors);
        Assert.Same(fixture.Asset("MainScene"), editor.Asset);
        Assert.Same(editor, fixture.Session.EditorCollection.ActiveEditor);
        Assert.Equal([fixture.Asset("MainScene")], fixture.Session.AssetCollection.SelectedAssets);
        Assert.Equal([scene.Id], OpenedAssets(fixture));
    }

    [AvaloniaFact]
    public async Task TheEditorsOfTheLastSessionOpenAgainAndTheLastIsSelected()
    {
        var first = new SpriteSheetAsset();
        var second = new SpriteSheetAsset();
        var scene = new SceneAsset();
        var gameSettings = new GameSettingsAsset { DefaultScene = AttachedReferenceManager.CreateProxyObject<Scene>(scene.Id, "MainScene") };
        using var fixture = ShellSessionFixture.Create(("First", first), ("Second", second), ("MainScene", scene), (GameSettingsAsset.GameSettingsLocation, gameSettings));
        // An asset that was deleted since is skipped
        SetOpenedAssets(fixture, first.Id, AssetId.New(), second.Id);

        await fixture.OpenAsync();

        Assert.Equal([fixture.Asset("First"), fixture.Asset("Second")], fixture.Session.EditorCollection.Editors.Select(x => x.Asset));
        Assert.Same(fixture.Asset("Second"), fixture.Session.EditorCollection.ActiveEditor?.Asset);
        Assert.Equal([fixture.Asset("Second")], fixture.Session.AssetCollection.SelectedAssets);
        Assert.Equal([first.Id, second.Id], OpenedAssets(fixture));
    }

    [AvaloniaFact]
    public async Task TheSolutionExplorerShowsTheFolderOfTheSelectedAsset()
    {
        var sheet = new SpriteSheetAsset();
        using var fixture = ShellSessionFixture.Create(("Folder/Sub/Sheet", sheet));
        SetOpenedAssets(fixture, sheet.Id);
        await fixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            await WindowTestHelper.PumpAsync();
            var folder = Assert.IsAssignableFrom<DirectoryViewModel>(Assert.Single(fixture.Session.AssetCollection.SelectedLocations));
            Assert.Equal("Sub", folder.Name);

            var tree = window.GetLogicalDescendants().OfType<TreeView>().Single(x => x.Name == "SolutionTree");
            var item = tree.GetVisualDescendants().OfType<TreeViewItem>().SingleOrDefault(x => x.DataContext == folder);
            Assert.NotNull(item);
            Assert.True(item.IsSelected);
            // The folders, the package and its category above it are expanded
            Assert.All(item.GetVisualAncestors().OfType<TreeViewItem>(), x => Assert.True(x.IsExpanded));
        }
        finally
        {
            await WindowTestHelper.CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task AnAssetThatOnlyTheOtherEditorCanEditStaysInTheList()
    {
        // No editor of this editor edits a material
        var material = new MaterialAsset();
        var sheet = new SpriteSheetAsset();
        using var fixture = ShellSessionFixture.Create(("Material", material), ("Sheet", sheet));
        SetOpenedAssets(fixture, material.Id, sheet.Id);

        await fixture.OpenAsync();

        Assert.Equal([fixture.Asset("Sheet")], fixture.Session.EditorCollection.Editors.Select(x => x.Asset));
        Assert.Equal([material.Id, sheet.Id], OpenedAssets(fixture));

        // The list follows the editors that close
        Assert.True(await fixture.Session.EditorCollection.CloseAssetEditorAsync(fixture.Asset("Sheet"), false));
        Assert.Equal([material.Id], OpenedAssets(fixture));
    }

    [AvaloniaFact]
    public async Task ClosingTheSessionKeepsItsEditorsForNextTime()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("First", new SpriteSheetAsset()), ("Second", new SpriteSheetAsset()));
        fixture.ReplaceDialogService();
        fixture.Session.EditorCollection.OpenAssetEditor(fixture.Asset("First"));
        fixture.Session.EditorCollection.OpenAssetEditor(fixture.Asset("Second"));
        var expected = new[] { fixture.Asset("First").Id, fixture.Asset("Second").Id };
        Assert.Equal(expected, OpenedAssets(fixture));

        Assert.True(await fixture.Main.CloseSessionAsync());

        Assert.Empty(fixture.Session.EditorCollection.Editors);
        Assert.Equal(expected, OpenedAssets(fixture));
    }

    [AvaloniaFact]
    public async Task TheDataOfOtherSessionsAndTheLayoutsOfWpfAreKept()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        var other = new MRUAdditionalData(Path.Combine(fixture.Directory, "Other", "Other.sln")) { DockingLayout = "<Layout />", DockingLayoutEditors = "<EditorLayout />", DockingLayoutVersion = 3 };
        GameStudioInternalSettings.CreateMostRecentlyUsedSessionsData().UpdateData(other);

        await fixture.OpenAsync();
        fixture.Session.EditorCollection.OpenAssetEditor(fixture.Asset("Sheet"));

        var kept = GameStudioInternalSettings.CreateMostRecentlyUsedSessionsData().GetData(other.FilePath);
        Assert.NotNull(kept);
        Assert.Equal("<Layout />", kept.DockingLayout);
        Assert.Equal("<EditorLayout />", kept.DockingLayoutEditors);
        Assert.Equal(3, kept.DockingLayoutVersion);
        Assert.Equal([fixture.Asset("Sheet").Id], OpenedAssets(fixture));
    }

    private static void SetOpenedAssets(ShellSessionFixture fixture, params AssetId[] assets)
    {
        GameStudioInternalSettings.CreateMostRecentlyUsedSessionsData().UpdateOpenedAssets(fixture.PackagePath, assets);
    }

    private static List<AssetId> OpenedAssets(ShellSessionFixture fixture)
    {
        return GameStudioInternalSettings.CreateMostRecentlyUsedSessionsData().GetData(fixture.PackagePath)?.OpenedAssets.ToList() ?? [];
    }
}
