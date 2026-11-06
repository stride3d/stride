// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Dropping an asset dragged from the asset view onto a reference property.
/// </summary>
public sealed class AssetDropTests
{
    [AvaloniaFact]
    public async Task TheDragDataCarriesTheAssets()
    {
        using var fixture = await AssetPickerTests.OpenModelWithSkeletonsAsync();
        var skeleton = fixture.Asset("Skeleton");
        var model = fixture.Asset("Model");

        var assets = AssetDragDropHelper.GetAssets(AssetDragDropHelper.CreateData([skeleton, model]));

        Assert.Equal([skeleton, model], assets);
        Assert.Empty(AssetDragDropHelper.GetAssets(null));
    }

    [AvaloniaFact]
    public async Task DroppingAnAssetOnAReferenceSetsItInOneTransaction()
    {
        using var fixture = await AssetPickerTests.OpenModelWithSkeletonsAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var model = fixture.Asset("Model");
            var skeleton = fixture.Asset("Skeleton");
            var other = fixture.Asset("Characters/OtherSkeleton");
            var textBox = await AssetPickerTests.ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);
            // Showing the reference wrote nothing
            Assert.Empty(fixture.Session.ActionHistory.Transactions);

            Drop(window, textBox, other);

            Assert.Equal(other.Id, AssetPickerTests.ReferencedId(model));
            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            // The box shows the new reference without writing it back again
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<TextBox>().Any(x => x.Text == other.Url), "the new reference in the box");
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();
            Assert.Single(history.Transactions);

            history.UndoCommand.Execute();
            Assert.Equal(skeleton.Id, AssetPickerTests.ReferencedId(model));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnAssetThatDoesNotFitTheReferenceIsRefused()
    {
        using var fixture = await AssetPickerTests.OpenModelWithSkeletonsAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var model = fixture.Asset("Model");
            var skeleton = fixture.Asset("Skeleton");
            var textBox = await AssetPickerTests.ShowModelAndFindReferenceBoxAsync(fixture, window, model, skeleton);
            var behavior = FindDropBehavior(textBox);

            // A model is not a skeleton
            Assert.Null(behavior.GetDropTarget([model], AddChildModifiers.None, out var message));
            Assert.Equal("Incompatible asset", message);
            Assert.NotNull(behavior.GetDropTarget([skeleton], AddChildModifiers.None, out _));

            Drop(window, textBox, model);
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();

            Assert.Equal(skeleton.Id, AssetPickerTests.ReferencedId(model));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static void Drop(Window window, Control target, AssetViewModel asset)
    {
        var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window) ?? throw new InvalidOperationException("The target is not in the window.");
        var data = AssetDragDropHelper.CreateData([asset]);
        window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Link, RawInputModifiers.None);
        window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Link, RawInputModifiers.None);
        window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Link, RawInputModifiers.None);
    }

    private static ContentReferenceDropBehavior FindDropBehavior(Control control)
    {
        foreach (var ancestor in control.GetVisualAncestors().OfType<Control>())
        {
            if (global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(ancestor).OfType<ContentReferenceDropBehavior>().FirstOrDefault() is { } behavior)
                return behavior;
        }
        throw new InvalidOperationException("No drop behavior around the control.");
    }
}
