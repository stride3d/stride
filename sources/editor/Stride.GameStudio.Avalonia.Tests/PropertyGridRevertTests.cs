// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// A property whose value differs from its default shows a revert icon next to its name, which reverts it; its context menu
/// has "Reset to default value".
/// </summary>
public sealed class PropertyGridRevertTests
{
    private const string AssetLocation = "Text";
    private const string OtherLocation = "Other";

    [AvaloniaFact]
    public async Task TheIconShowsOnTheChangedPropertiesOnlyAndAClickRevertsIt()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        // The property grid alone, wider than in the main window of the headless screen
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        await window.ShowAsync();
        try
        {
            var asset = (TextPropertiesAsset)fixture.Asset(AssetLocation).Asset;
            var label = await ShowPropertyAsync(fixture, window, nameof(TextPropertiesAsset.Label));
            var separator = Item(window, nameof(TextPropertiesAsset.Separator));

            Assert.True(RevertButton(label).IsEffectivelyVisible);
            Assert.False(RevertButton(separator).IsEffectivelyVisible);

            await window.ClickAsync(RevertButton(label));
            await WindowTestHelper.WaitUntilAsync(() => asset.Label is null, "the reverted label");
            Assert.False(RevertButton(Item(window, nameof(TextPropertiesAsset.Label))).IsEffectivelyVisible);

            // One undo step brings the value and the icon back
            fixture.Session.UndoRedoService.Undo();
            await WindowTestHelper.WaitUntilAsync(() => asset.Label == "label" && RevertButton(Item(window, nameof(TextPropertiesAsset.Label))).IsEffectivelyVisible, "the label back");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnEditShowsTheIconOnTheSameRow()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset()));
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        await window.ShowAsync();
        try
        {
            var asset = (TextPropertiesAsset)fixture.Asset(AssetLocation).Asset;
            var separator = await ShowPropertyAsync(fixture, window, nameof(TextPropertiesAsset.Separator));
            var node = (NodeViewModel)separator.DataContext!;
            Assert.False(RevertButton(separator).IsEffectivelyVisible);

            // A change of the value only keeps the row and its view model: the icon follows the value
            node.NodeValue = ',';
            await WindowTestHelper.PumpAsync();
            Assert.Equal(',', asset.Separator);
            Assert.Same(separator, Item(window, nameof(TextPropertiesAsset.Separator)));
            Assert.Same(node, separator.DataContext);
            Assert.True(RevertButton(separator).IsEffectivelyVisible);

            node.NodeValue = '\0';
            await WindowTestHelper.PumpAsync();
            Assert.False(RevertButton(separator).IsEffectivelyVisible);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheContextMenuResetsAPropertyToItsDefault()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        await window.ShowAsync();
        try
        {
            var asset = (TextPropertiesAsset)fixture.Asset(AssetLocation).Asset;
            var label = await ShowPropertyAsync(fixture, window, nameof(TextPropertiesAsset.Label));
            var menu = label.ContextMenu!;
            label.RaiseEvent(new ContextRequestedEventArgs());
            await WindowTestHelper.PumpAsync();
            Assert.True(menu.IsOpen);

            var reset = menu.Items.OfType<MenuItem>().Single(x => x.Command is CommandReference { Id: PropertyGridCommands.ResetToDefaultId });
            Assert.True(reset.IsVisible);
            Assert.Equal("Reset to default value", reset.Header);
            Assert.True(reset.Command!.CanExecute(reset.CommandParameter));
            reset.Command.Execute(reset.CommandParameter);
            menu.Close();
            await WindowTestHelper.WaitUntilAsync(() => asset.Label is null, "the reverted label");

            // At its default, the item is there but does nothing
            label = Item(window, nameof(TextPropertiesAsset.Label));
            label.RaiseEvent(new ContextRequestedEventArgs());
            await WindowTestHelper.PumpAsync();
            reset = label.ContextMenu!.Items.OfType<MenuItem>().Single(x => x.Command is CommandReference { Id: PropertyGridCommands.ResetToDefaultId });
            Assert.False(reset.Command!.CanExecute(reset.CommandParameter));
            label.ContextMenu.Close();
            await WindowTestHelper.PumpAsync();
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheIconRevertsTheCombinedPropertyOfTwoSelectedAssets()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "first" }), (OtherLocation, new TextPropertiesAsset { Label = "second" }));
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        await window.ShowAsync();
        try
        {
            var (first, second) = (fixture.Asset(AssetLocation), fixture.Asset(OtherLocation));
            fixture.Session.AssetCollection.SelectAssets([first, second]);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActiveProperties.Selection?.Count == 2 && TryItem(window, nameof(TextPropertiesAsset.Label)) is not null, "the two assets");
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();

            var label = Item(window, nameof(TextPropertiesAsset.Label));
            Assert.True(RevertButton(label).IsEffectivelyVisible);
            await window.ClickAsync(RevertButton(label));
            await WindowTestHelper.WaitUntilAsync(() => ((TextPropertiesAsset)first.Asset).Label is null && ((TextPropertiesAsset)second.Asset).Label is null, "both labels reverted");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheIconRevertsTheCheckBoxOfTwoSelectedAssets()
    {
        // The check box shows the values of both assets: it must not write a value back while they are reverted one by one
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TogglePropertiesAsset { IsEnabled = false }), (OtherLocation, new TogglePropertiesAsset { IsEnabled = false }));
        var window = new Window { Content = new PropertyGridView { DataContext = fixture.Session.ActiveProperties } };
        await window.ShowAsync();
        try
        {
            var (first, second) = (fixture.Asset(AssetLocation), fixture.Asset(OtherLocation));
            fixture.Session.AssetCollection.SelectAssets([first, second]);
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActiveProperties.Selection?.Count == 2 && TryItem(window, nameof(TogglePropertiesAsset.IsEnabled)) is not null, "the two assets");
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();

            await window.ClickAsync(RevertButton(Item(window, nameof(TogglePropertiesAsset.IsEnabled))));
            await WindowTestHelper.PumpAsync();
            await WindowTestHelper.WaitUntilAsync(() => ((TogglePropertiesAsset)first.Asset).IsEnabled && ((TogglePropertiesAsset)second.Asset).IsEnabled, "both check boxes reverted");
            // The check box of the value (the header has a hidden one, for the objects that can be turned off)
            Assert.True(Item(window, nameof(TogglePropertiesAsset.IsEnabled)).GetVisualDescendants().OfType<CheckBox>().Single(x => x.IsEffectivelyVisible).IsChecked);

            // The undo and the redo change them one by one too
            fixture.Session.UndoRedoService.Undo();
            await WindowTestHelper.PumpAsync();
            Assert.False(((TogglePropertiesAsset)first.Asset).IsEnabled);
            Assert.False(((TogglePropertiesAsset)second.Asset).IsEnabled);
            fixture.Session.UndoRedoService.Redo();
            await WindowTestHelper.PumpAsync();
            Assert.True(((TogglePropertiesAsset)first.Asset).IsEnabled);
            Assert.True(((TogglePropertiesAsset)second.Asset).IsEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task<PropertyViewItem> ShowPropertyAsync(ShellSessionFixture fixture, Window window, string member)
    {
        fixture.Session.AssetCollection.SelectAssets([fixture.Asset(AssetLocation)]);
        await WindowTestHelper.WaitUntilAsync(() => TryItem(window, member) is not null, $"the property {member}");
        // Settle: a second generation would replace the properties
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return Item(window, member);
    }

    private static PropertyViewItem? TryItem(Window window, string member)
    {
        return window.GetVisualDescendants().OfType<PropertyViewItem>().FirstOrDefault(x => x.DataContext is NodeViewModel node && node.Name == member);
    }

    private static PropertyViewItem Item(Window window, string member) => TryItem(window, member) ?? throw new InvalidOperationException($"No property {member}.");

    private static Button RevertButton(PropertyViewItem item)
    {
        // The button of this property's header, not of a nested property's
        return item.GetVisualDescendants().OfType<Button>().First(x => x.Name == "RevertToDefaultButton");
    }
}
