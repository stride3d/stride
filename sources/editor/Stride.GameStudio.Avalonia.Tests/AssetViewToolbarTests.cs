// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The asset view's thumbnail zoom, its tile panel that creates only the tiles on screen, and its button that updates the assets
/// whose source files changed.
/// </summary>
public sealed class AssetViewToolbarTests
{
    [AvaloniaFact]
    public async Task TheZoomButtonsResizeTheTilesWithinTheLimits()
    {
        var savedZoom = InternalSettings.AssetViewTileThumbnailZoom.GetValue();
        var (fixture, window) = await OpenAsync(("Many/AnAssetWithANameLongEnoughForTwoLines", new SkeletonAsset()));
        using (fixture)
        {
            try
            {
                var view = View(window);
                view.ThumbnailSize = 96;
                await ShowFolderAsync(fixture, "Many");
                Assert.Equal(96, ThumbnailWidth(window));
                AssertTheTileFitsItsSlot(window);

                await ClickAsync(window, "ZoomInButton");
                Assert.Equal(112, view.ThumbnailSize);
                Assert.Equal(112, ThumbnailWidth(window));
                // The tile panel gives each tile the room of its thumbnail, and the editor keeps the zoom for the next sessions
                Assert.Equal(112 + 12, AssetList(window).ContainerFromIndex(0)!.Bounds.Width);
                Assert.Equal(112, InternalSettings.AssetViewTileThumbnailZoom.GetValue());

                await ClickAsync(window, "ZoomInButton");
                await ClickAsync(window, "ZoomInButton");
                Assert.Equal(AssetExplorerView.MaximumThumbnailSize, view.ThumbnailSize);
                AssertTheTileFitsItsSlot(window);

                for (var i = 0; i < 10; ++i)
                    await ClickAsync(window, "ZoomOutButton");
                Assert.Equal(AssetExplorerView.MinimumThumbnailSize, view.ThumbnailSize);
                Assert.Equal(16, ThumbnailWidth(window));
                AssertTheTileFitsItsSlot(window);
            }
            finally
            {
                await window.CloseAsync();
                InternalSettings.AssetViewTileThumbnailZoom.SetValue(savedZoom);
            }
        }
    }

    [AvaloniaFact]
    public async Task CtrlWheelZoomsAndTheWheelAloneScrolls()
    {
        var savedZoom = InternalSettings.AssetViewTileThumbnailZoom.GetValue();
        var assets = Enumerable.Range(0, 60).Select(i => ($"Many/Asset{i:D2}", (Asset)new SkeletonAsset())).ToArray();
        var (fixture, window) = await OpenAsync(assets);
        using (fixture)
        {
            try
            {
                var view = View(window);
                view.ThumbnailSize = 96;
                await ShowFolderAsync(fixture, "Many");
                var list = AssetList(window);
                var scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
                var point = list.TranslatePoint(new Point(list.Bounds.Width / 2, list.Bounds.Height / 2), window)!.Value;

                window.MouseWheel(point, new Vector(0, 1), RawInputModifiers.Control);
                await WindowTestHelper.PumpAsync();
                Assert.Equal(112, view.ThumbnailSize);
                window.MouseWheel(point, new Vector(0, -1), RawInputModifiers.Control);
                window.MouseWheel(point, new Vector(0, -1), RawInputModifiers.Control);
                await WindowTestHelper.PumpAsync();
                Assert.Equal(80, view.ThumbnailSize);
                Assert.Equal(0, scrollViewer.Offset.Y);

                // Without Ctrl, the wheel scrolls the tiles
                window.MouseWheel(point, new Vector(0, -1), RawInputModifiers.None);
                await WindowTestHelper.PumpAsync();
                Assert.Equal(80, view.ThumbnailSize);
                Assert.True(scrollViewer.Offset.Y > 0);
            }
            finally
            {
                await window.CloseAsync();
                InternalSettings.AssetViewTileThumbnailZoom.SetValue(savedZoom);
            }
        }
    }

    [AvaloniaFact]
    public async Task OnlyTheTilesOnScreenAreCreated()
    {
        var assets = Enumerable.Range(0, 400).Select(i => ($"Many/Asset{i:D3}", (Asset)new SkeletonAsset())).ToArray();
        var (fixture, window) = await OpenAsync(assets);
        using (fixture)
        {
            try
            {
                await ShowFolderAsync(fixture, "Many");
                var list = AssetList(window);
                Assert.Equal(400, list.ItemCount);
                var panel = Assert.IsType<VirtualizingTilePanel>(list.ItemsPanelRoot);

                var tiles = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
                Assert.InRange(tiles, 1, 60);
                Assert.Equal(0, panel.RealizedRange.First);

                // Scrolling to the end shows the last assets, with about as many tiles
                var scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().First();
                scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height);
                window.UpdateLayout();
                Assert.Equal(399, panel.RealizedRange.Last);
                var last = Assert.IsType<ListBoxItem>(list.ContainerFromIndex(399));
                Assert.Equal("Asset399", ((AssetViewModel)last.DataContext!).Name);
                Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(), 1, tiles + 10);

                // Selecting an asset that has no tile scrolls to it
                scrollViewer.Offset = default;
                window.UpdateLayout();
                fixture.Session.AssetCollection.SelectAssets([fixture.Asset("Many/Asset250")]);
                await WindowTestHelper.WaitUntilAsync(() => list.ContainerFromIndex(250) is { IsVisible: true }, "the tile of the selected asset");
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheUpdateButtonShowsHowManyAssetsHaveChangedSources()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "hero.fbx");
        await File.WriteAllTextAsync(source, "model");
        // A skeleton whose source has no hash yet: the session finds that its source changed
        var (fixture, window) = await OpenAsync(("Hero", new SkeletonAsset { Source = new UFile(source) }), ("Other", new SkeletonAsset()));
        using (fixture)
        {
            try
            {
                var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "UpdateFromSourceButton");
                var count = window.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Name == "AssetsToUpdateCount");
                var tracker = fixture.Session.SourceTracker!;
                Assert.True(button.IsVisible);
                Assert.Same(tracker.UpdateAllAssetsWithModifiedSourceCommand, button.Command);

                await WindowTestHelper.WaitUntilAsync(() => tracker.AssetsToUpdate.Count == 1, "the changed source", 20000);
                await WindowTestHelper.PumpAsync();
                Assert.True(count.IsVisible);
                Assert.Equal("(1)", count.Text);
                Assert.True(button.IsEffectivelyEnabled);
            }
            finally
            {
                await window.CloseAsync();
                Directory.Delete(directory, true);
            }
        }
    }

    [AvaloniaFact]
    public async Task TheUpdateButtonShowsNoCountWhenNoSourceChanged()
    {
        var (fixture, window) = await OpenAsync(("Hero", new SkeletonAsset()));
        using (fixture)
        {
            try
            {
                var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "UpdateFromSourceButton");
                var count = window.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Name == "AssetsToUpdateCount");

                Assert.True(button.IsVisible);
                Assert.Empty(fixture.Session.SourceTracker!.AssetsToUpdate);
                Assert.False(count.IsVisible);
                Assert.False(button.IsEffectivelyEnabled);
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
        await window.ShowAsync();
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    private static async Task ShowFolderAsync(ShellSessionFixture fixture, string name)
    {
        var collection = fixture.Session.AssetCollection;
        collection.SelectedLocations.Clear();
        collection.SelectedLocations.Add(fixture.Package.AssetMountPoint.SubDirectories.OfType<DirectoryViewModel>().Single(x => x.Name == name));
        await WindowTestHelper.WaitUntilAsync(() => collection.FilteredContent.Count > 0, $"the assets of {name}");
        await WindowTestHelper.PumpAsync();
    }

    private static AssetExplorerView View(Window window) => window.GetVisualDescendants().OfType<AssetExplorerView>().Single();

    private static ListBox AssetList(Window window) => window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");

    // The width of the thumbnail frame of the first tile, without its border: the image itself has no size until the thumbnail exists
    private static double ThumbnailWidth(Window window)
    {
        window.UpdateLayout();
        var tile = AssetList(window).ContainerFromIndex(0)!;
        var border = tile.GetVisualDescendants().OfType<Border>().First(x => x.Classes.Contains("tileThumbnail"));
        return border.Bounds.Width - border.BorderThickness.Left - border.BorderThickness.Right;
    }

    // A tile with a name on two lines takes no more height than its slot in the tile panel
    private static void AssertTheTileFitsItsSlot(Window window)
    {
        window.UpdateLayout();
        var tile = AssetList(window).ContainerFromIndex(0)!;
        var slot = tile.Bounds.Height;
        tile.Measure(new Size(tile.Bounds.Width, double.PositiveInfinity));
        Assert.InRange(tile.DesiredSize.Height, 0, slot);
        tile.InvalidateMeasure();
        window.UpdateLayout();
    }

    private static async Task ClickAsync(Window window, string buttonName)
    {
        var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == buttonName);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WindowTestHelper.PumpAsync();
    }
}
