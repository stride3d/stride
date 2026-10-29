// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The status bar of the shell.
/// </summary>
public sealed class StatusBarTests
{
    /// <summary>
    /// As in WPF, the status bar counts the items the asset view shows and, separately, the selected ones.
    /// </summary>
    [AvaloniaFact]
    public async Task TheStatusBarCountsTheShownAndTheSelectedItems()
    {
        using var fixture = ShellSessionFixture.Create(
            ("Models/Sphere", new TextPropertiesAsset()),
            ("Models/Cube", new TextPropertiesAsset()),
            ("Textures/Wood", new TextPropertiesAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();
            var assets = fixture.Session.AssetCollection;
            // The main view remembers the display mode across sessions: start from a known one
            assets.DisplayAssetMode = DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
            assets.SelectedLocations.Clear();
            assets.SelectedLocations.Add(fixture.Package);
            await WindowTestHelper.PumpAsync();
            Assert.Equal([3, 0], Counts(window));

            // Selecting an asset shows its folder, which holds two assets
            assets.SelectAssets([fixture.Asset("Models/Sphere")]);
            await WindowTestHelper.PumpAsync();
            Assert.Equal([2, 1], Counts(window));

            // Another folder: the count of shown items follows the asset view
            assets.SelectedLocations.Clear();
            assets.SelectedLocations.Add(fixture.Asset("Textures/Wood").Directory);
            await WindowTestHelper.PumpAsync();
            Assert.Equal([1, assets.SelectedContent.Count], Counts(window));

            assets.SelectAssets([fixture.Asset("Textures/Wood")]);
            await WindowTestHelper.PumpAsync();
            Assert.Equal([1, 1], Counts(window));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// The numbers the status bar shows: the items of the asset view, then the selected ones.
    /// </summary>
    private static List<int> Counts(MainWindow window)
    {
        var counts = window.GetVisualDescendants().OfType<TextBlock>()
            .Single(x => x.Inlines?.OfType<Run>().Any(r => r.Text?.EndsWith("selected)", StringComparison.Ordinal) == true) == true);
        // "3 items" and "(1 selected)"
        return counts.Inlines!.OfType<Run>().Select(x => x.Text!.Trim('(', ' ')).Where(x => x.Length > 0).Select(x => int.Parse(x.Split(' ')[0])).ToList();
    }
}
