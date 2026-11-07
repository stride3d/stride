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
/// Dropping an asset dragged from the asset view onto a reference property, or onto a folder of the solution explorer.
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

    [AvaloniaFact]
    public async Task DroppingAnAssetOnAFolderMovesItInOneTransaction()
    {
        using var fixture = await AssetPickerTests.OpenModelWithSkeletonsAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var skeleton = fixture.Asset("Skeleton");
            var folder = await FindFolderAsync(window, "Characters");
            var history = fixture.Session.ActionHistory;
            Assert.Empty(history.Transactions);

            Drop(window, folder, skeleton, DragDropEffects.Link | DragDropEffects.Move);

            Assert.Equal("Characters/", skeleton.Directory.Path);
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            Assert.Equal($"Move asset '{ShellSessionFixture.Url("Characters/Skeleton")}'", history.Transactions.Single().Name);

            history.UndoCommand.Execute();
            Assert.Equal(string.Empty, skeleton.Directory.Path);
            history.RedoCommand.Execute();
            Assert.Equal("Characters/", skeleton.Directory.Path);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AFolderRefusesAnAssetItAlreadyHas()
    {
        using var fixture = await AssetPickerTests.OpenModelWithSkeletonsAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var other = fixture.Asset("Characters/OtherSkeleton");
            var folder = await FindFolderAsync(window, "Characters");
            var behavior = FindBehavior<AddChildDropBehavior>(folder);

            Assert.Null(behavior.GetDropTarget([other], AddChildModifiers.None, out var message));
            Assert.Equal("Directory Characters/ already contains an asset named OtherSkeleton", message);

            Drop(window, folder, other, DragDropEffects.Link | DragDropEffects.Move);
            await Task.Delay(200);
            await WindowTestHelper.PumpAsync();

            Assert.Equal("Characters/", other.Directory.Path);
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static void Drop(Window window, Control target, AssetViewModel asset, DragDropEffects effects = DragDropEffects.Link)
    {
        var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window) ?? throw new InvalidOperationException("The target is not in the window.");
        var data = AssetDragDropHelper.CreateData([asset]);
        window.DragDrop(point, RawDragEventType.DragEnter, data, effects, RawInputModifiers.None);
        window.DragDrop(point, RawDragEventType.DragOver, data, effects, RawInputModifiers.None);
        window.DragDrop(point, RawDragEventType.Drop, data, effects, RawInputModifiers.None);
    }

    /// <summary>
    /// Expands the solution explorer down to the folder with the given name and returns the root of its row.
    /// </summary>
    private static async Task<Control> FindFolderAsync(Window window, string name)
    {
        Control? row = null;
        await WindowTestHelper.WaitUntilAsync(() =>
        {
            foreach (var item in window.GetVisualDescendants().OfType<TreeViewItem>())
                item.IsExpanded = true;
            row = window.GetVisualDescendants().OfType<TreeViewItem>()
                .Where(x => x.DataContext is DirectoryViewModel directory && directory.Name == name)
                .Select(x => x.GetVisualDescendants().OfType<StackPanel>().FirstOrDefault(p => global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(p).OfType<AddChildDropBehavior>().Any()))
                .FirstOrDefault(x => x is not null);
            return row is not null;
        }, $"the folder {name} in the solution explorer");
        // The tree scrolls to the row, so that the drop point is on it
        row!.BringIntoView();
        await WindowTestHelper.PumpAsync();
        return row;
    }

    private static ContentReferenceDropBehavior FindDropBehavior(Control control) => FindBehavior<ContentReferenceDropBehavior>(control);

    private static TBehavior FindBehavior<TBehavior>(Control control)
    {
        foreach (var element in control.GetSelfAndVisualAncestors().OfType<Control>())
        {
            if (global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(element).OfType<TBehavior>().FirstOrDefault() is { } behavior)
                return behavior;
        }
        throw new InvalidOperationException("No drop behavior around the control.");
    }
}
