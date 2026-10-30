// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Serialization;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Rendering;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The header and the footer of the property grid, as in the WPF Game Studio: the navigation in the selection history, the
/// description of the selection, the tags, the search, and the description of the property under the mouse.
/// </summary>
public sealed class PropertyGridHeaderTests
{
    private const string SkeletonLocation = "Skeleton";
    private const string ModelLocation = "Model";

    [AvaloniaFact]
    public async Task TheHeaderDescribesTheSelection()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var grid = Grid(window);
            var model = fixture.Asset(ModelLocation);
            await SelectAsync(fixture, grid, model);

            var properties = fixture.Session.ActiveProperties;
            var description = grid.FindControl<TextBlock>("SelectionDescription")!;
            // The type in bold, then the name, as in WPF
            var runs = description.Inlines!.OfType<Run>().Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            Assert.Equal([properties.TypeDescription, properties.Name], runs.Select(x => x.Text));
            Assert.Equal(global::Avalonia.Media.FontWeight.Bold, runs[0].FontWeight);
            Assert.Equal(model.Url, properties.Name);
            Assert.False(string.IsNullOrEmpty(properties.TypeDescription));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheNavigationButtonsGoBackAndForthInTheSelectionHistory()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var grid = Grid(window);
            var (model, skeleton) = (fixture.Asset(ModelLocation), fixture.Asset(SkeletonLocation));
            var previous = grid.FindControl<Button>("PreviousSelectionButton")!;
            var next = grid.FindControl<Button>("NextSelectionButton")!;
            await SelectAsync(fixture, grid, model);
            await SelectAsync(fixture, grid, skeleton);
            await WindowTestHelper.WaitUntilAsync(() => previous.IsEffectivelyEnabled, "the Previous selection button");
            Assert.False(next.IsEffectivelyEnabled);

            await window.ClickAsync(previous);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.AssetCollection.SelectedAssets.SequenceEqual([model]), "the previous selection");
            await WindowTestHelper.WaitUntilAsync(() => next.IsEffectivelyEnabled, "the Next selection button");

            await window.ClickAsync(next);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.AssetCollection.SelectedAssets.SequenceEqual([skeleton]), "the next selection");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheSearchKeepsTheMatchingProperties()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var grid = Grid(window);
            await SelectAsync(fixture, grid, fixture.Asset(ModelLocation));
            var propertyView = grid.FindControl<PropertyView>("Properties")!;
            var search = grid.FindControl<TextBox>("PropertyFilterTextBox")!;
            Assert.True(search.IsEffectivelyVisible);
            Assert.All(propertyView.Properties, x => Assert.True(x.IsVisible));

            search.Text = "skel";
            await WindowTestHelper.PumpAsync();

            var visible = propertyView.Properties.Where(x => x.IsVisible).Select(x => ((NodeViewModel)x.DataContext!).Name).ToList();
            Assert.Equal([nameof(ModelAsset.Skeleton)], visible);

            search.Text = "";
            await WindowTestHelper.PumpAsync();
            Assert.All(propertyView.Properties, x => Assert.True(x.IsVisible));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task EnterInTheTagBoxTagsTheSelectedAssets()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var grid = Grid(window);
            var model = fixture.Asset(ModelLocation);
            await SelectAsync(fixture, grid, model);
            var tagBox = grid.FindControl<TextBox>("NewTagTextBox")!;
            var tags = grid.FindControl<ItemsControl>("Tags")!;
            Assert.True(tagBox.IsEffectivelyVisible);
            Assert.False(tags.IsVisible);

            Assert.True(tagBox.Focus());
            tagBox.Text = "hero";
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            await WindowTestHelper.PumpAsync();

            Assert.Equal(["hero"], model.Tags);
            Assert.Equal("", tagBox.Text);
            Assert.True(tags.IsVisible);
            Assert.Contains(tags.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "hero");

            // Undo takes the tag away, and the list follows
            fixture.Session.UndoRedoService!.Undo();
            await WindowTestHelper.PumpAsync();
            Assert.Empty(model.Tags);
            Assert.False(tags.IsVisible);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheFooterDescribesThePropertyUnderTheMouse()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var grid = Grid(window);
            await SelectAsync(fixture, grid, fixture.Asset(ModelLocation));
            var propertyView = grid.FindControl<PropertyView>("Properties")!;
            var footer = grid.FindControl<DockPanel>("PropertyDescription")!;
            var item = propertyView.Properties.Single(x => ((NodeViewModel)x.DataContext!).Name == nameof(ModelAsset.Skeleton));

            var point = item.TranslatePoint(new Point(4, 4), window)!.Value;
            window.MouseMove(point);
            await WindowTestHelper.PumpAsync();

            Assert.Same(item, propertyView.HoveredItem);
            var texts = footer.GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text).ToList();
            Assert.Contains(((NodeViewModel)item.DataContext!).DisplayName, texts);
            Assert.Contains(typeof(Skeleton).Namespace + ".", texts);
            Assert.Contains(nameof(Skeleton), texts);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task<ShellSessionFixture> OpenModelWithSkeletonAsync()
    {
        var skeleton = new SkeletonAsset();
        var model = new ModelAsset { Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(skeleton.Id, SkeletonLocation) };
        return await ShellSessionFixture.OpenAsync((SkeletonLocation, skeleton), (ModelLocation, model));
    }

    private static PropertyGridView Grid(MainWindow window) => window.GetVisualDescendants().OfType<PropertyGridView>().Single();

    private static async Task SelectAsync(ShellSessionFixture fixture, PropertyGridView grid, AssetViewModel asset)
    {
        // Selecting the asset generates its properties asynchronously; wait for the grid to show them
        fixture.Session.AssetCollection.SelectAssets([asset]);
        var propertyView = grid.FindControl<PropertyView>("Properties")!;
        await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActiveProperties.Name == asset.Url && propertyView.Properties.Count > 0, "the properties of " + asset.Url);
        // and settle: a second generation would replace the properties
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
    }
}
