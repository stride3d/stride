// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The asset preview panel: what it asks of the preview service of the session as the selection changes, and how long it keeps
/// that service.
/// </summary>
public sealed class AssetPreviewTests
{
    private const string FirstLocation = "First";
    private const string SecondLocation = "Second";

    [AvaloniaFact]
    public async Task OpeningASessionCreatesThePreviewPanel()
    {
        using var fixture = await OpenAsync();

        Assert.NotNull(fixture.Main.Preview);
        Assert.Null(fixture.Main.Preview.PreviewObject);
        Assert.True(fixture.Main.Preview.IsPreviewVisible);
    }

    [AvaloniaFact]
    public async Task AnAssetSelectedAloneIsPreviewed()
    {
        using var fixture = await OpenAsync();
        var preview = RegisterPreviewService(fixture);
        var first = fixture.Asset(FirstLocation);

        fixture.Session.AssetCollection.SelectAssets([first]);
        await WindowTestHelper.PumpAsync();

        Assert.Same(first, preview.Requests.Last());
        Assert.Same(first, PreviewedAsset(fixture.Main.Preview!));
    }

    [AvaloniaFact]
    public async Task AMultipleSelectionHasNoPreview()
    {
        using var fixture = await OpenAsync();
        var preview = RegisterPreviewService(fixture);
        var first = fixture.Asset(FirstLocation);
        var second = fixture.Asset(SecondLocation);
        fixture.Session.AssetCollection.SelectAssets([first]);
        await WindowTestHelper.PumpAsync();

        fixture.Session.AssetCollection.SelectAssets([first, second]);
        await WindowTestHelper.PumpAsync();

        Assert.Null(preview.Requests.Last());
        Assert.Null(fixture.Main.Preview!.PreviewObject);
    }

    [AvaloniaFact]
    public async Task ThePanelShowsThePreviewOfTheSelectedAsset()
    {
        using var fixture = await OpenAsync();
        RegisterPreviewService(fixture);
        var first = fixture.Asset(FirstLocation);
        var second = fixture.Asset(SecondLocation);
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var panel = window.GetVisualDescendants().OfType<AssetPreviewView>().Single();
            Assert.Null(ShownPreview(panel));

            fixture.Session.AssetCollection.SelectAssets([first]);
            await WindowTestHelper.WaitUntilAsync(() => ShownPreview(panel)?.Tag == first, "the preview of the first asset");

            // A multiple selection empties the panel, and the next single selection fills it again
            fixture.Session.AssetCollection.SelectAssets([first, second]);
            await WindowTestHelper.WaitUntilAsync(() => ShownPreview(panel) is null, "an empty panel");
            fixture.Session.AssetCollection.SelectAssets([second]);
            await WindowTestHelper.WaitUntilAsync(() => ShownPreview(panel)?.Tag == second, "the preview of the second asset");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task HidingThePanelPausesThePreview()
    {
        using var fixture = await OpenAsync();
        var preview = RegisterPreviewService(fixture);
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            fixture.Session.AssetCollection.SelectAssets([fixture.Asset(FirstLocation)]);
            await WindowTestHelper.PumpAsync();
            var previewTab = window.GetVisualDescendants().OfType<TabItem>().Single(x => Equals(x.Header, "Asset preview"));
            var historyTab = window.GetVisualDescendants().OfType<TabItem>().Single(x => Equals(x.Header, "Edit history"));

            historyTab.IsSelected = true;
            await WindowTestHelper.PumpAsync();
            Assert.False(fixture.Main.Preview!.IsPreviewVisible);
            Assert.Equal((0, 1), (preview.ShowCount, preview.HideCount));

            previewTab.IsSelected = true;
            await WindowTestHelper.PumpAsync();
            Assert.True(fixture.Main.Preview.IsPreviewVisible);
            Assert.Equal((1, 1), (preview.ShowCount, preview.HideCount));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task DisposingThePanelDisposesThePreviewServiceAndStopsFollowingTheSelection()
    {
        using var fixture = await OpenAsync();
        var preview = RegisterPreviewService(fixture);
        var panel = fixture.Main.Preview!;
        fixture.Session.AssetCollection.SelectAssets([fixture.Asset(FirstLocation)]);
        await WindowTestHelper.PumpAsync();
        var requests = preview.Requests.Count;

        panel.Dispose();
        fixture.Session.AssetCollection.SelectAssets([fixture.Asset(SecondLocation)]);
        await WindowTestHelper.PumpAsync();

        Assert.True(preview.IsDisposed);
        Assert.Null(panel.PreviewObject);
        Assert.Equal(requests, preview.Requests.Count);
    }

    [AvaloniaFact]
    public async Task ClosingTheSessionDisposesThePreview()
    {
        using var fixture = await OpenAsync();
        var preview = RegisterPreviewService(fixture);
        var panel = fixture.Main.Preview!;
        fixture.Session.AssetCollection.SelectAssets([fixture.Asset(FirstLocation)]);
        await WindowTestHelper.PumpAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();

        await window.CloseAsync();

        Assert.True(preview.IsDisposed);
        Assert.Null(panel.PreviewObject);
    }

    private static Task<ShellSessionFixture> OpenAsync()
    {
        return ShellSessionFixture.OpenAsync((FirstLocation, new SkeletonAsset()), (SecondLocation, new SkeletonAsset()));
    }

    /// <summary>
    /// Gives the session a preview service: the tests turn the real one off, as it runs a game.
    /// </summary>
    private static FakeAssetPreviewService RegisterPreviewService(ShellSessionFixture fixture)
    {
        var preview = new FakeAssetPreviewService();
        fixture.Session.ServiceProvider.RegisterService(preview);
        return preview;
    }

    private static AssetViewModel? PreviewedAsset(PreviewViewModel preview) => (preview.PreviewObject as Control)?.Tag as AssetViewModel;

    /// <summary>
    /// The preview view that the panel shows, if any.
    /// </summary>
    private static Control? ShownPreview(AssetPreviewView panel)
    {
        var content = panel.GetVisualDescendants().OfType<ContentControl>().Single(x => x.Name == "PreviewContent");
        return content.IsEffectivelyVisible ? content.Presenter?.Child : null;
    }
}
