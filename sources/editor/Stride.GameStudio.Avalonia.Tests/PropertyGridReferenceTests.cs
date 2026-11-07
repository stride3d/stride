// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Serialization;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Rendering;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The reference fields of the property grid show the name of the asset after its dimmed location, as in WPF, and the URL
/// while they are edited.
/// </summary>
public sealed class PropertyGridReferenceTests
{
    private const string SkeletonLocation = "Models/Skeleton";
    private const string ModelLocation = "Model";

    [AvaloniaFact]
    public async Task AReferenceShowsTheNameOfTheAssetAfterItsLocation()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        // The property grid alone, wider than in the main window of the headless screen
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        window.Show();
        try
        {
            var field = await SkeletonFieldAsync(fixture, window);
            var (urlBox, overlay) = (Named<TextBox>(field, "UrlTextBox"), Named<Grid>(field, "RefOverlay"));

            Assert.True(overlay.IsEffectivelyVisible);
            Assert.Equal("Skeleton", Named<TextBlock>(field, "RefName").Text);
            var location = Named<TextBlock>(field, "RefLocation");
            Assert.Equal("Models", location.Text);
            Assert.True(location.IsEffectivelyVisible);
            // The box keeps the URL, hidden behind the display
            Assert.Equal(fixture.Asset(SkeletonLocation).Url, urlBox.Text);
            Assert.Equal(Colors.Transparent, ((ISolidColorBrush)urlBox.Foreground!).Color);

            // Editing shows the URL
            Assert.True(urlBox.Focus());
            await WindowTestHelper.PumpAsync();
            Assert.False(overlay.IsEffectivelyVisible);
            Assert.NotEqual(Colors.Transparent, ((ISolidColorBrush)urlBox.Foreground!).Color);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ANarrowFieldDropsTheLocationAndKeepsTheName()
    {
        using var fixture = await OpenModelWithSkeletonAsync();
        // The property grid alone, wider than in the main window of the headless screen
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        window.Show();
        try
        {
            var field = await SkeletonFieldAsync(fixture, window);
            var overlay = Named<Grid>(field, "RefOverlay");
            var name = Named<TextBlock>(field, "RefName");

            // Less room than the name and the minimum width of the location
            overlay.Width = name.Bounds.Width + 10;
            await WindowTestHelper.PumpAsync();

            Assert.False(Named<TextBlock>(field, "RefLocation").IsEffectivelyVisible);
            Assert.True(name.IsEffectivelyVisible);
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

    private static async Task<PropertyViewItem> SkeletonFieldAsync(ShellSessionFixture fixture, Window window)
    {
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        var propertyView = grid.FindControl<PropertyView>("Properties")!;
        fixture.Session.AssetCollection.SelectAssets([fixture.Asset(ModelLocation)]);
        await WindowTestHelper.WaitUntilAsync(() => propertyView.Properties.Any(IsSkeleton), "the Skeleton property");
        // Settle: a second generation would replace the properties
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return propertyView.Properties.Single(IsSkeleton);

        static bool IsSkeleton(PropertyViewItem item) => (item.DataContext as NodeViewModel)?.Name == nameof(ModelAsset.Skeleton);
    }

    private static T Named<T>(Control field, string name) where T : Control
    {
        return field.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
    }
}
