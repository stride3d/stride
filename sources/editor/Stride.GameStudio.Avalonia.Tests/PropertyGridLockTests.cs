// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The lock of the property grid's header: a locked grid keeps showing its objects when the selection changes, and its history
/// buttons wait for it to unlock.
/// </summary>
public sealed class PropertyGridLockTests
{
    private const string FirstLocation = "First";
    private const string SecondLocation = "Second";

    [AvaloniaFact]
    public async Task TheToggleLocksTheGridOnItsObjects()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((FirstLocation, new TextPropertiesAsset { Label = "first" }), (SecondLocation, new TextPropertiesAsset { Label = "second" }));
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        await window.ShowAsync();
        try
        {
            var properties = fixture.Session.ActiveProperties;
            var toggle = Named<ToggleButton>(window, "LockButton");
            // Nothing to lock
            Assert.False(toggle.IsEffectivelyEnabled);

            var first = fixture.Asset(FirstLocation);
            var second = fixture.Asset(SecondLocation);
            await SelectAsync(fixture, first);
            Assert.True(toggle.IsEffectivelyEnabled);

            await window.ClickAsync(toggle);
            Assert.True(properties.IsLocked);
            Assert.True(toggle.IsChecked);
            Assert.False(Named<Button>(window, "PreviousSelectionButton").IsEnabled);
            Assert.False(Named<Button>(window, "NextSelectionButton").IsEnabled);

            fixture.Session.AssetCollection.SelectAssets([second]);
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();
            Assert.Equal([first], properties.Selection);
            Assert.Equal(first.Url, properties.Name);

            // Unlocking shows the selection
            await window.ClickAsync(toggle);
            Assert.False(properties.IsLocked);
            await WindowTestHelper.WaitUntilAsync(() => properties.Selection?.SequenceEqual([second]) == true && properties.CanDisplayProperties, "the second asset");
            Assert.Equal(second.Url, properties.Name);
            Assert.True(Named<Button>(window, "PreviousSelectionButton").IsEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task SelectAsync(ShellSessionFixture fixture, Core.Assets.Presentation.ViewModels.AssetViewModel asset)
    {
        fixture.Session.AssetCollection.SelectAssets([asset]);
        await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActiveProperties.Selection?.SequenceEqual([asset]) == true && fixture.Session.ActiveProperties.CanDisplayProperties, asset.Url);
        // Settle: a second generation would replace the properties
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
    }

    private static T Named<T>(Window window, string name) where T : Control
    {
        return window.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
    }
}
