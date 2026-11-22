// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Point = Avalonia.Point;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The editors of asset members with their own template, in the property grid of a session: the node list of a skeleton and
/// the font of a sprite font.
/// </summary>
public sealed class AssetPropertyEditorsTests
{
    [AvaloniaFact]
    public async Task TheAllAndNoneButtonsOfASkeletonSetWhichNodesAreKept()
    {
        var skeleton = new SkeletonAsset();
        skeleton.Nodes.Add(new NodeInformation("Root", 0, true));
        skeleton.Nodes.Add(new NodeInformation("Spine", 1, false));
        skeleton.Nodes.Add(new NodeInformation("Head", 2, true));
        using var fixture = await ShellSessionFixture.OpenAsync(("Skeleton", skeleton));
        var window = await ShowAsync(fixture);
        try
        {
            var asset = fixture.Asset("Skeleton");
            var grid = await SelectAsync(fixture, window, asset, () => Buttons(window).Any());
            var nodes = ((SkeletonAsset)asset.Asset).Nodes;

            Click(window, Buttons(window).Single(x => (string?)x.Content == "All"));
            await WindowTestHelper.WaitUntilAsync(() => nodes.All(x => x.Preserve), "all the nodes kept");
            Click(window, Buttons(window).Single(x => (string?)x.Content == "None"));
            await WindowTestHelper.WaitUntilAsync(() => nodes.All(x => !x.Preserve), "no node kept");
            // One history entry per button
            Assert.Equal(2, fixture.Session.ActionHistory.Transactions.Count);
            Assert.NotNull(grid);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheFontOfASpriteFontIsPickedInTheFontsOfTheSystem()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Font", new SpriteFontAsset { FontSource = new SystemFontProvider("Arial") }));
        var window = await ShowAsync(fixture);
        try
        {
            var asset = fixture.Asset("Font");
            // The font is a member of the font source, collapsed at first
            await SelectAsync(fixture, window, asset, () =>
            {
                foreach (var item in window.GetVisualDescendants().OfType<PropertyViewItem>().Where(x => x.DataContext is NodeViewModel { Name: nameof(SpriteFontAsset.FontSource) }))
                    item.IsExpanded = true;
                return FontComboBox(window) is not null;
            });
            var comboBox = FontComboBox(window)!;
            var fonts = comboBox.ItemsSource!.Cast<string>().ToList();
            Assert.NotEmpty(fonts);
            Assert.Equal(fonts.Order(StringComparer.OrdinalIgnoreCase), fonts);

            var other = fonts.First(x => x != "Arial");
            comboBox.SelectedItem = other;
            await WindowTestHelper.WaitUntilAsync(() => ((SystemFontProvider)((SpriteFontAsset)asset.Asset).FontSource).FontName == other, "the font set");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task<MainWindow> ShowAsync(ShellSessionFixture fixture)
    {
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // After it opens, as the window restores its saved size then
        window.Width = 1600;
        window.Height = 1000;
        return window;
    }

    private static async Task<PropertyGridView> SelectAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel asset, Func<bool> shown)
    {
        // Selecting an asset generates its properties asynchronously
        fixture.Session.AssetCollection.SelectAssets([asset]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        await WindowTestHelper.WaitUntilAsync(shown, "the editors of the members");
        // and settle: a second generation would replace the editors
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return grid;
    }

    private static IEnumerable<Button> Buttons(Window window)
    {
        return window.GetVisualDescendants().OfType<PropertyGridView>().Single().GetVisualDescendants().OfType<Button>()
            .Where(x => x.Content is "All" or "None" && x.DataContext is NodeViewModel { Name: nameof(SkeletonAsset.Nodes) });
    }

    private static ComboBox? FontComboBox(Window window)
    {
        return window.GetVisualDescendants().OfType<PropertyGridView>().Single().GetVisualDescendants().OfType<ComboBox>()
            .FirstOrDefault(x => x.DataContext is NodeViewModel { Name: nameof(SystemFontProvider.FontName) });
    }

    private static void Click(Window window, Control control)
    {
        control.BringIntoView();
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
    }
}
