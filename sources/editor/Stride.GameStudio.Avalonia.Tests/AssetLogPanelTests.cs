// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Controls;
using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The Asset errors panel, as WPF's: a grid of the warnings and errors with the asset of each, their count in the title, a double-click
/// that selects the asset, and the scope, sources and type filters.
/// </summary>
public sealed class AssetLogPanelTests
{
    private const string BuildError = "The sheet could not be compiled.";
    private const string BuildWarning = "The sheet has no sprite.";

    [AvaloniaFact]
    public async Task TheGridShowsTheMessagesOfTheAssetsWithTheirCountInTheTitle()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var tab = AssetLogTab(window);
                // The count, as WPF's title; the singular until the Localize converter finds the translations (plural texts are left for later)
                Assert.Matches(@"^Asset errors? \(0\)$", Assert.IsType<string>(tab.Header));

                var sheet = fixture.Asset("Sheet");
                Build(fixture, sheet);
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 2, "the messages of the build");

                // The verbose messages of the session load are hidden, as in WPF
                Assert.Equal([$"Error: {BuildError}", $"Warning: {BuildWarning}"], Rows(window).Select(x => x.Text));
                // A build message names no asset: the asset of its log
                var url = sheet.AssetItem.Location.ToString();
                Assert.Equal([url, url], Cells(window, "logAsset"));
                Assert.Matches(@"^Asset errors? \(2\)$", Assert.IsType<string>(tab.Header));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheTypeFiltersHideTheirMessages()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                Build(fixture, fixture.Asset("Sheet"));
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 2, "the messages of the build");
                var viewer = Viewer(window);
                Assert.False(Toggle(viewer, "ShowInfo").IsChecked);
                Assert.True(Toggle(viewer, "ShowError").IsChecked);

                await window.ClickAsync(Toggle(viewer, "ShowError"));
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 1, "the error to be hidden");
                Assert.Equal($"Warning: {BuildWarning}", Rows(window).Single().Text);

                await window.ClickAsync(Toggle(viewer, "ShowVerbose"));
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count > 1, "the verbose messages");
                Assert.Contains(Rows(window), x => x.Text!.StartsWith("Verbose: ", StringComparison.Ordinal));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ADoubleClickOnAMessageSelectsItsAsset()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var sheet = fixture.Asset("Sheet");
                Build(fixture, sheet);
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 2, "the messages of the build");
                Assert.Empty(fixture.Session.AssetCollection.SelectedAssets);

                var row = Rows(window).First();
                await window.ClickAsync(row);
                await window.ClickAsync(row);
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.AssetCollection.SelectedAssets.Count == 1, "the selection of the asset");

                Assert.Same(sheet, fixture.Session.AssetCollection.SelectedAssets.Single());
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheScopeAndTheSourcesFilterTheMessages()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var log = fixture.Session.AssetLog;
                Build(fixture, fixture.Asset("Sheet"));
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 2, "the messages of the build");

                // Only the messages of the selected assets: none is selected
                var scope = Viewer(window).GetVisualDescendants().OfType<ComboBox>().Single(x => x.Name == "AssetLogScope");
                Assert.Equal(AssetLogScope.AllAssets, scope.SelectedValue);
                scope.SelectedIndex = 1;
                Assert.Equal(AssetLogScope.SelectedAssets, log.Scope);
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 0, "the messages of the selected assets");

                scope.SelectedIndex = 0;
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 2, "the messages of all assets");

                // The Build source hidden: its messages too, and the header of the menu tells it
                var sources = Viewer(window).GetVisualDescendants().OfType<Menu>().Single(x => x.Name == "AssetLogSources");
                var header = Assert.IsType<MenuItem>(Assert.Single(sources.Items));
                Assert.Equal("Sources", header.Header);
                var build = header.Items.OfType<MenuItem>().Single(x => Equals(x.Header, log.Sources[0].DisplayName));
                Assert.True(build.IsChecked);
                build.IsChecked = false;
                Assert.False(log.Sources[0].IsShown);
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 0, "the build messages to be hidden");
                Assert.Equal("Sources (4/5)", header.Header);

                log.ShowAllSourcesCommand.Execute();
                await WindowTestHelper.WaitUntilAsync(() => Rows(window).Count == 2, "the build messages");
                Assert.True(build.IsChecked);
            }
            finally
            {
                fixture.Session.AssetLog.Scope = AssetLogScope.AllAssets;
                await window.CloseAsync();
            }
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync()
    {
        var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        // Room for the tab headers on one line and for the rows of the grid
        window.Width = 1600;
        window.Height = 1000;
        AssetLogTab(window).IsSelected = true;
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    /// <summary>
    /// Gives the asset a build log with an error and a warning, as the build service does after building it.
    /// </summary>
    private static void Build(ShellSessionFixture fixture, Stride.Core.Assets.Editor.ViewModel.AssetViewModel asset)
    {
        var log = new LoggerResult();
        log.Error(BuildError);
        log.Warning(BuildWarning);
        fixture.Session.AssetLog.SetAssetLog(asset.Id, AssetLogViewModel.BuildLogName, log);
    }

    private static TabItem AssetLogTab(MainWindow window) => window.GetLogicalDescendants().OfType<TabItem>().Single(x => x.Name == "AssetLogTab");

    private static GridLogViewer Viewer(MainWindow window) => window.GetVisualDescendants().OfType<GridLogViewer>().Single();

    private static ToggleButton Toggle(GridLogViewer viewer, string name) => viewer.GetVisualDescendants().OfType<ToggleButton>().Single(x => x.Name == name);

    /// <summary>
    /// The Message cells of the rows of the grid.
    /// </summary>
    private static List<TextBlock> Rows(MainWindow window) => Viewer(window).GetVisualDescendants().OfType<TextBlock>().Where(x => x.Classes.Contains("logText") && x.IsEffectivelyVisible).ToList();

    private static List<string?> Cells(MainWindow window, string column) => Viewer(window).GetVisualDescendants().OfType<TextBlock>().Where(x => x.Classes.Contains(column) && x.IsEffectivelyVisible).Select(x => x.Text).ToList();
}
