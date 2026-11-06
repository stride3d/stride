// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Serialization;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Rendering;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The asset picker: its window, and the "Select an asset" button of a reference property that opens it.
/// </summary>
public sealed class AssetPickerTests
{
    private const string SkeletonLocation = "Skeleton";
    private const string OtherSkeletonLocation = "Characters/OtherSkeleton";
    private const string ModelLocation = "Model";

    [AvaloniaFact]
    public async Task TheWindowReturnsThePickedAsset()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        try
        {
            var other = fixture.Asset(OtherSkeletonLocation);
            var picker = new AssetPickerViewModel(fixture.Session, [typeof(SkeletonAsset)]);
            var window = new AssetPickerWindow { DataContext = picker };
            var result = window.ShowDialog<bool>(owner);
            await WindowTestHelper.PumpAsync();

            // Only the skeletons are listed, and OK waits for a selection
            var list = window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "Assets");
            Assert.Equal(["OtherSkeleton", "Skeleton"], picker.FilteredAssets.Select(x => x.Name));
            var ok = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "OkButton");
            Assert.False(ok.IsEnabled);

            list.SelectedItem = other;
            await WindowTestHelper.PumpAsync();
            Assert.Same(other, picker.SelectedAsset);
            Assert.True(ok.IsEnabled);

            ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(await result);
            Assert.Same(other, picker.SelectedAsset);
        }
        finally
        {
            await owner.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TypingInTheSearchBoxNarrowsTheList()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        try
        {
            var picker = new AssetPickerViewModel(fixture.Session, [typeof(SkeletonAsset)]);
            var window = new AssetPickerWindow { DataContext = picker };
            var result = window.ShowDialog<bool>(owner);
            await WindowTestHelper.PumpAsync();

            // The search box has the focus when the picker opens
            window.KeyTextInput("Other");
            await WindowTestHelper.PumpAsync();
            Assert.Equal("Other", picker.SearchText);
            Assert.Equal(["OtherSkeleton"], picker.FilteredAssets.Select(x => x.Name));

            window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "CancelButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(await result);
        }
        finally
        {
            await owner.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheSelectAnAssetButtonSetsThePickedAsset()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var dialogs = fixture.ReplaceDialogService();
        var model = fixture.Asset(ModelLocation);
        var skeleton = fixture.Asset(SkeletonLocation);
        var other = fixture.Asset(OtherSkeletonLocation);
        dialogs.PickAsset = picker => picker.FilteredAssets.Single(x => x == other);
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var pickup = await ShowModelAndFindPickupButtonAsync(fixture, window, model, skeleton);
            await window.ClickAsync(pickup);

            // The picker opened on the current skeleton, with the skeletons only
            await WindowTestHelper.WaitUntilAsync(() => ReferencedId(model) == other.Id, "the picked skeleton");
            var picker = Assert.Single(dialogs.AssetPickers);
            Assert.Same(skeleton, picker.SelectedAsset);
            Assert.All(picker.FilteredAssets, x => Assert.Equal(typeof(SkeletonAsset), x.AssetType));

            // One transaction, that Undo reverts
            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            history.UndoCommand.Execute();
            Assert.Equal(skeleton.Id, ReferencedId(model));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CancellingThePickerChangesNothing()
    {
        using var fixture = await OpenModelWithSkeletonsAsync();
        var dialogs = fixture.ReplaceDialogService();
        var model = fixture.Asset(ModelLocation);
        var skeleton = fixture.Asset(SkeletonLocation);
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var pickup = await ShowModelAndFindPickupButtonAsync(fixture, window, model, skeleton);
            await window.ClickAsync(pickup);
            await WindowTestHelper.WaitUntilAsync(() => dialogs.AssetPickers.Count == 1, "the picker");
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();

            Assert.Equal(skeleton.Id, ReferencedId(model));
            Assert.False(fixture.Session.ActionHistory.CanUndo);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    internal static async Task<ShellSessionFixture> OpenModelWithSkeletonsAsync()
    {
        var skeleton = new SkeletonAsset();
        var model = new ModelAsset { Skeleton = AttachedReferenceManager.CreateProxyObject<Skeleton>(skeleton.Id, SkeletonLocation) };
        return await ShellSessionFixture.OpenAsync((SkeletonLocation, skeleton), (OtherSkeletonLocation, new SkeletonAsset()), (ModelLocation, model));
    }

    /// <summary>
    /// Selects the model and waits for the property grid to show the reference box of its skeleton.
    /// </summary>
    internal static async Task<TextBox> ShowModelAndFindReferenceBoxAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel model, AssetViewModel skeleton)
    {
        fixture.Session.AssetCollection.SelectAssets([model]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        TextBox? textBox = null;
        await WindowTestHelper.WaitUntilAsync(() => (textBox = grid.GetVisualDescendants().OfType<TextBox>().SingleOrDefault(x => x.Text == skeleton.Url)) is not null, "the reference box");
        // and settle: a second generation would replace the box
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return textBox!;
    }

    internal static AssetId ReferencedId(AssetViewModel model)
    {
        var reference = ((ModelAsset)model.Asset).Skeleton;
        return AttachedReferenceManager.GetAttachedReference(reference)?.Id ?? AssetId.Empty;
    }

    private static async Task<Button> ShowModelAndFindPickupButtonAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel model, AssetViewModel skeleton)
    {
        var textBox = await ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);
        // The buttons of the reference sit next to its text box, in the template of the property
        var template = textBox.GetVisualAncestors().OfType<DockPanel>().First(x => x.GetVisualDescendants().OfType<Button>().Any());
        return template.GetVisualDescendants().OfType<Button>().First(x => x.IsEffectivelyVisible && x.Command is not null && Equals(x.CommandParameter, typeof(Skeleton)));
    }
}
