// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Stride.Assets.Materials;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The tiles of the asset view, as in WPF: folders and assets, each with its thumbnail on a dark backdrop, its name and its type;
/// an asset also has the badge of its build status. The "*" of unsaved changes is checked by <see cref="SessionSaveTests"/>.
/// </summary>
public sealed class AssetViewTileTests
{
    [AvaloniaFact]
    public async Task TheFoldersAndTheAssetsOfTheLocationHaveTiles()
    {
        var (fixture, window) = await OpenAsync(("Materials/Ground", new MaterialAsset()), ("Sheet", new SpriteSheetAsset()));
        using (fixture)
        {
            try
            {
                var assets = fixture.Session.AssetCollection;
                assets.DisplayAssetMode = DisplayAssetMode.AssetAndFolderInSelectedFolder;
                assets.SelectedLocations.Clear();
                assets.SelectedLocations.Add(fixture.Package.AssetMountPoint);
                await WindowTestHelper.PumpAsync();

                var list = AssetList(window);
                Assert.Equal(["Materials", "Sheet"], list.Items.Cast<ISessionObjectViewModel>().Select(x => x.Name));

                var folder = Tile(list, "Materials");
                Assert.Equal("Folder", TypeText(folder));
                Assert.Empty(folder.GetVisualDescendants().OfType<Button>());

                var sheet = Tile(list, "Sheet");
                Assert.Equal(fixture.Asset("Sheet").TypeDisplayName, TypeText(sheet));
                Assert.Single(sheet.GetVisualDescendants().OfType<Button>(), x => x.Classes.Contains("buildStatus"));

                // The thumbnails are on the dark backdrop of the WPF tiles, whatever the theme
                foreach (var tile in new[] { folder, sheet })
                {
                    var backdrop = tile.GetVisualDescendants().OfType<Border>().Single(x => x.Classes.Contains("tileThumbnail"));
                    Assert.Equal(Color.Parse("#FF3E3E40"), Assert.IsAssignableFrom<ISolidColorBrush>(backdrop.Background).Color);
                }
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheBuildStatusBadgeShowsAndTogglesTheRootState()
    {
        var (fixture, window) = await OpenAsync(("Sheet", new SpriteSheetAsset()));
        using (fixture)
        {
            try
            {
                var sheet = fixture.Asset("Sheet");
                fixture.Session.AssetCollection.SelectAssets([sheet]);
                await WindowTestHelper.PumpAsync();
                var tile = Tile(AssetList(window), "Sheet");
                var badge = tile.GetVisualDescendants().OfType<Button>().Single(x => x.Classes.Contains("buildStatus"));
                var wasRoot = sheet.Dependencies.IsBuiltAsRoot;
                Assert.Equal(wasRoot, VisibleStatus(badge) == "ImageAssetIsRoot");

                // A click on the badge includes the selected asset as root, or removes it, as in WPF
                badge.Command!.Execute(badge.CommandParameter);
                await WindowTestHelper.WaitUntilAsync(() => sheet.Dependencies.IsBuiltAsRoot != wasRoot, "the root state");
                await WindowTestHelper.PumpAsync();

                Assert.Equal(!wasRoot, VisibleStatus(badge) == "ImageAssetIsRoot");
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
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    private static ListBox AssetList(MainWindow window) => window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");

    /// <summary>
    /// The tile of the item with the given name.
    /// </summary>
    private static StackPanel Tile(ListBox list, string name)
    {
        var item = list.Items.Cast<ISessionObjectViewModel>().Single(x => x.Name == name);
        var container = list.ContainerFromItem(item) ?? throw new InvalidOperationException($"No container for {name}");
        return container.GetVisualDescendants().OfType<StackPanel>().First(x => x.Classes.Contains("tile"));
    }

    private static string? TypeText(StackPanel tile) => tile.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Classes.Contains("tileType")).Text;

    /// <summary>
    /// The key of the image the build status badge shows.
    /// </summary>
    private static string? VisibleStatus(Button badge)
    {
        var images = badge.GetVisualDescendants().OfType<Image>().Where(x => x.IsVisible).ToList();
        var image = Assert.Single(images);
        foreach (var key in new[] { "ImageAssetIsRoot", "ImageAssetIsIndirectlyIncluded", "ImageAssetIsOverridden", "ImageAssetIsExcluded" })
        {
            if (badge.TryFindResource(key, out var resource) && ReferenceEquals(resource, image.Source))
                return key;
        }
        return null;
    }
}
