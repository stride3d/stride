// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Point = Avalonia.Point;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Changing a number of an asset with the mouse in the property grid: a drag is one history entry, which undo reverts; the
/// arrows make one entry per press. The number editors of the grid show no spin buttons.
/// </summary>
public sealed class NumberDragTests
{
    private const string AssetLocation = "Dragged";

    [AvaloniaFact]
    public async Task DraggingAVectorComponentMakesOneHistoryEntry()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // After it opens, as the window restores its saved size then: wide enough for the grid to give the editors their width
        window.Width = 1600;
        window.Height = 1000;
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var grid = await ShowAsync(fixture, window, asset);
            var editor = Editor<Vector3Editor>(grid, nameof(DisplayedValuesAsset.Offset));
            var xBox = editor.GetVisualDescendants().OfType<NumericTextBox>().First();

            // 15 pixels to the right: 5 steps of 1
            await DragAsync(window, xBox, 15);

            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            Assert.Equal(new Vector3(6, 2, 3), Offset(asset));
            await WindowTestHelper.PumpAsync();
            Assert.Single(history.Transactions);

            history.UndoCommand.Execute();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(new Vector3(1, 2, 3), Offset(asset));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task DraggingAFloatMakesOneHistoryEntry()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // After it opens, as the window restores its saved size then: wide enough for the grid to give the editors their width
        window.Width = 1600;
        window.Height = 1000;
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var grid = await ShowAsync(fixture, window, asset);
            // A ranged member: its small step (0.1) is the step of the drag, 3 steps for 9 pixels
            var box = Editor<NumericTextBox>(grid, nameof(DisplayedValuesAsset.TwoDecimals));

            await DragAsync(window, box, 9);

            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            Assert.Equal(0.7f, ((DisplayedValuesAsset)asset.Asset).TwoDecimals, 4);
            await WindowTestHelper.PumpAsync();
            Assert.Single(history.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task EachArrowPressMakesOneHistoryEntry()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // After it opens, as the window restores its saved size then: wide enough for the grid to give the editors their width
        window.Width = 1600;
        window.Height = 1000;
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var grid = await ShowAsync(fixture, window, asset);
            var box = Editor<NumericTextBox>(grid, nameof(DisplayedValuesAsset.Ratio));
            Assert.True(box.GetVisualDescendants().OfType<TextBox>().Single().Focus());

            window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
            await WindowTestHelper.PumpAsync();
            window.KeyPressQwerty(PhysicalKey.PageUp, RawInputModifiers.None);
            await WindowTestHelper.PumpAsync();

            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 2, "the history entries");
            Assert.Equal(12f, ((DisplayedValuesAsset)asset.Asset).Ratio, 4);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheNumberEditorsOfTheGridShowNoSpinButtons()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // After it opens, as the window restores its saved size then: wide enough for the grid to give the editors their width
        window.Width = 1600;
        window.Height = 1000;
        try
        {
            var grid = await ShowAsync(fixture, window, fixture.Asset(AssetLocation));

            var boxes = grid.GetVisualDescendants().OfType<NumericTextBox>().ToList();
            Assert.NotEmpty(boxes);
            Assert.All(boxes, x => Assert.False(x.DisplayUpDownButtons, (x.DataContext as NodeViewModel)?.Name));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static DisplayedValuesAsset NewAsset() => new()
    {
        Ratio = 1f,
        TwoDecimals = 0.4f,
        Precise = 1,
        Offset = new Vector3(1, 2, 3),
        Angle = new AngleSingle(0, AngleType.Radian),
    };

    private static Vector3 Offset(AssetViewModel asset) => ((DisplayedValuesAsset)asset.Asset).Offset;

    private static async Task<PropertyGridView> ShowAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel asset)
    {
        // Selecting an asset generates its properties asynchronously; wait for the grid to show the editors of the members
        fixture.Session.AssetCollection.SelectAssets([asset]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        await WindowTestHelper.WaitUntilAsync(() => grid.GetVisualDescendants().OfType<Vector3Editor>().Any(x => IsEditorOf(x, nameof(DisplayedValuesAsset.Offset))), "the editors of the members");
        // and settle: a second generation would replace the editors
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return grid;
    }

    private static T Editor<T>(PropertyGridView grid, string member) where T : Control
    {
        return grid.GetVisualDescendants().OfType<T>().First(x => IsEditorOf(x, member));
    }

    private static bool IsEditorOf(Control editor, string member) => editor.DataContext is NodeViewModel node && node.Name == member;

    /// <summary>
    /// Drags the mouse to the right over the text of a box, by the given distance after the drag started.
    /// </summary>
    private static async Task DragAsync(Window window, NumericTextBox box, double distance)
    {
        box.BringIntoView();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();
        var start = textBox.TranslatePoint(new Point(textBox.Bounds.Width / 3, textBox.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("The box is not in the window.");
        ToolTip.SetServiceEnabled(window, false);
        try
        {
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            // The first move starts the drag; the next ones change the value
            window.MouseMove(start + new Point(5, 0));
            for (var x = 1.0; x <= distance; ++x)
                window.MouseMove(start + new Point(5 + x, 0));
            await WindowTestHelper.PumpAsync();
            window.MouseUp(start + new Point(5 + distance, 0), MouseButton.Left);
        }
        finally
        {
            window.ClearValue(ToolTip.ServiceEnabledProperty);
        }
        await WindowTestHelper.PumpAsync();
    }
}
