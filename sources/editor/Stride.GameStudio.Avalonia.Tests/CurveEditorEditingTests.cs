// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views.CurveEditor;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Assets.Editor.ViewModels.CurveEditor.ControlPoints;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Drawing;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using static Stride.GameStudio.Avalonia.Tests.CurveEditorPanelTests;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Editing the keys of a curve in the curve editor panel, with the pointer, the keys, the toolbar and the menus: each edit is one
/// entry of the history, on the size curve of a particle system (keys 0, 0.5 and 1, values 1, 3 and 2).
/// </summary>
public sealed class CurveEditorEditingTests
{
    [AvaloniaFact]
    public async Task AltClickAddsAKeyWithOneHistoryEntry()
    {
        await using var editing = await OpenSizeCurveAsync();
        var history = editing.Scene.Fixture.Session.ActionHistory;
        var entries = history.Transactions.Count;

        // Halfway between the first two keys: key 0.25, value 2
        var position = editing.Between(0, 1);
        editing.Window.MouseDown(position, MouseButton.Left, RawInputModifiers.Alt);
        editing.Window.MouseUp(position, MouseButton.Left, RawInputModifiers.Alt);
        await WindowTestHelper.PumpAsync();

        var keys = editing.Scene.SizeCurve.KeyFrames;
        Assert.Equal(4, keys.Count);
        Assert.Equal(0.25f, keys[1].Key, 0.02f);
        Assert.Equal(2f, keys[1].Value, 0.1f);
        await editing.WaitForHistoryAsync(entries + 1);
        // A press without Alt adds nothing
        editing.Click(editing.Between(1, 2));
        await WindowTestHelper.PumpAsync();
        Assert.Equal(4, keys.Count);

        // With Alt, a rectangle selects nothing, but its press adds a key, as in WPF
        editing.RectangleAround([0, 1, 2, 3], RawInputModifiers.Alt);
        await WindowTestHelper.PumpAsync();
        Assert.Equal(5, keys.Count);
        Assert.Empty(editing.SelectedIndices());
        await editing.WaitForHistoryAsync(entries + 2);
    }

    [AvaloniaFact]
    public async Task ClicksAndRectanglesSelectKeysWithShiftAndCtrl()
    {
        await using var editing = await OpenSizeCurveAsync();
        var editor = editing.Editor;

        // A click selects the key under the pointer, a click elsewhere clears the selection
        editing.Click(editing.Position(1));
        Assert.Equal([1], editing.SelectedIndices());
        Assert.Same(editing.Points[1], editor.SingleSelectedControlPoint);
        editing.Click(editing.Between(0, 1) + new Point(0, 40));
        Assert.Empty(editing.SelectedIndices());

        // A rectangle around every key selects them all; with Ctrl, it removes the first; with Shift, a click adds it back
        editing.RectangleAround([0, 1, 2]);
        Assert.Equal([0, 1, 2], editing.SelectedIndices());
        editing.RectangleAround([0], RawInputModifiers.Control);
        Assert.Equal([1, 2], editing.SelectedIndices());
        editing.Click(editing.Position(0), RawInputModifiers.Shift);
        Assert.Equal([0, 1, 2], editing.SelectedIndices());
        // With Shift, a rectangle adds; without, it replaces
        editing.RectangleAround([2]);
        Assert.Equal([2], editing.SelectedIndices());
        editing.RectangleAround([1], RawInputModifiers.Shift);
        Assert.Equal([1, 2], editing.SelectedIndices());
        // The rectangle shows while it is drawn only
        Assert.False(editing.View.FindControl<Control>("SelectionRectangle")!.IsVisible);
    }

    [AvaloniaFact]
    public async Task DraggingTheSelectedKeysMovesThemWithOneHistoryEntry()
    {
        await using var editing = await OpenSizeCurveAsync();
        var history = editing.Scene.Fixture.Session.ActionHistory;
        var keys = editing.Scene.SizeCurve.KeyFrames;
        editing.RectangleAround([0, 1]);
        Assert.Equal([0, 1], editing.SelectedIndices());
        var entries = history.Transactions.Count;

        // A quarter of the way to the third key, and up: both keys move by the same key and value
        var from = editing.Position(1);
        var to = from + (editing.Position(2) - from) * 0.25 + new Point(0, -20);
        editing.Window.MouseDown(from, MouseButton.Left);
        editing.Window.MouseMove(from + new Point(2, -2), RawInputModifiers.LeftMouseButton);
        editing.Window.MouseMove(to, RawInputModifiers.LeftMouseButton);
        editing.Window.MouseUp(to, MouseButton.Left);
        await WindowTestHelper.PumpAsync();

        await editing.WaitForHistoryAsync(entries + 1);
        Assert.Equal(0.125f, keys[0].Key, 0.01f);
        Assert.Equal(0.625f, keys[1].Key, 0.01f);
        Assert.True(keys[0].Value > 1 && keys[1].Value > 3, $"{keys[0].Value} {keys[1].Value}");
        Assert.Equal(keys[1].Value - 3, keys[0].Value - 1, 0.01f);
        Assert.Equal(1f, keys[2].Key);

        // Undo puts them back
        history.UndoCommand.Execute();
        await WindowTestHelper.PumpAsync();
        Assert.Equal([0f, 0.5f, 1f], keys.Select(x => x.Key));
        Assert.Equal([1f, 3f, 2f], keys.Select(x => x.Value));
    }

    [AvaloniaFact]
    public async Task ThePressOfAClickMovesNothing()
    {
        await using var editing = await OpenSizeCurveAsync();
        var history = editing.Scene.Fixture.Session.ActionHistory;
        var entries = history.Transactions.Count;

        // A move shorter than the drag distance is a click
        var from = editing.Position(1);
        editing.Window.MouseDown(from, MouseButton.Left);
        editing.Window.MouseMove(from + new Point(1, 1), RawInputModifiers.LeftMouseButton);
        editing.Window.MouseUp(from + new Point(1, 1), MouseButton.Left);
        await WindowTestHelper.PumpAsync();

        Assert.Equal([1], editing.SelectedIndices());
        Assert.Equal(entries, history.Transactions.Count);
        Assert.Equal(0.5f, editing.Scene.SizeCurve.KeyFrames[1].Key);
    }

    [AvaloniaFact]
    public async Task TheEditMenuAndTheDeleteKeyDeleteTheSelectedKeysOfTheFocusedCurveArea()
    {
        await using var editing = await OpenSizeCurveAsync();
        var history = editing.Scene.Fixture.Session.ActionHistory;
        var keys = editing.Scene.SizeCurve.KeyFrames;
        var entries = history.Transactions.Count;

        // The click gives the curve area the focus: Edit > Delete deletes the selected key
        editing.Click(editing.Position(1));
        Assert.True(editing.View.FindControl<Control>("CurveArea")!.IsFocused);
        var delete = editing.Window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == "DeleteMenuItem");
        Assert.True(delete.Command!.CanExecute(delete.CommandParameter));
        delete.Command.Execute(delete.CommandParameter);
        await WindowTestHelper.PumpAsync();
        Assert.Equal([0f, 1f], keys.Select(x => x.Key));
        await editing.WaitForHistoryAsync(entries + 1);

        // The Delete key too
        editing.Click(editing.Position(1));
        editing.Window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
        editing.Window.KeyReleaseQwerty(PhysicalKey.Delete, RawInputModifiers.None);
        await WindowTestHelper.PumpAsync();
        Assert.Equal([0f], keys.Select(x => x.Key));
        await editing.WaitForHistoryAsync(entries + 2);
        // The curve list keeps its curve: Delete went to the curve area
        Assert.Single(editing.Editor.Curves);
    }

    [AvaloniaFact]
    public async Task TheContextMenuAddsAKeyWhereItOpened()
    {
        await using var editing = await OpenSizeCurveAsync();
        var keys = editing.Scene.SizeCurve.KeyFrames;

        // Halfway between the last two keys: key 0.75, value 2.5
        var position = editing.Between(1, 2);
        editing.Window.MouseDown(position, MouseButton.Right);
        editing.Window.MouseUp(position, MouseButton.Right);
        await WindowTestHelper.PumpAsync();
        var menu = editing.Canvas.ContextMenu!;
        try
        {
            Assert.True(menu.IsOpen);
            var items = menu.Items.OfType<MenuItem>().Where(x => x.IsVisible).Select(x => x.Header).ToList();
            Assert.Equal(["Curve", "Add point", "Delete selected points", "Clear whole curve", "View", "Fit to curve", "Fit to curve height", "Fit to curve width"], items);
            var add = menu.Items.OfType<MenuItem>().Single(x => x.Name == "AddPointMenuItem");
            add.Command!.Execute(add.CommandParameter);
        }
        finally
        {
            menu.Close();
        }
        await WindowTestHelper.PumpAsync();

        Assert.Equal(4, keys.Count);
        Assert.Equal(0.75f, keys[2].Key, 0.02f);
        Assert.Equal(2.5f, keys[2].Value, 0.1f);
    }

    [AvaloniaFact]
    public async Task TheNavigationKeysAndTheKeyBoxesEditTheSelectedKey()
    {
        await using var editing = await OpenSizeCurveAsync();
        var history = editing.Scene.Fixture.Session.ActionHistory;
        var keys = editing.Scene.SizeCurve.KeyFrames;

        // Home, Right, End and Left move the selection along the keys, as the toolbar's buttons
        editing.Click(editing.Position(1));
        void Press(PhysicalKey key)
        {
            editing.Window.KeyPressQwerty(key, RawInputModifiers.None);
            editing.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        }
        Press(PhysicalKey.Home);
        Assert.Equal([0], editing.SelectedIndices());
        Press(PhysicalKey.ArrowRight);
        Assert.Equal([1], editing.SelectedIndices());
        Press(PhysicalKey.End);
        Assert.Equal([2], editing.SelectedIndices());
        Press(PhysicalKey.ArrowLeft);
        Assert.Equal([1], editing.SelectedIndices());
        var view = editing.View;
        foreach (var (button, expected) in new[] { ("FirstKeyButton", 0), ("NextKeyButton", 1), ("LastKeyButton", 2), ("PreviousKeyButton", 1) })
        {
            var control = view.FindControl<Button>(button)!;
            Assert.True(control.IsEffectivelyVisible, button);
            control.Command!.Execute(control.CommandParameter);
            Assert.Equal([expected], editing.SelectedIndices());
        }

        // The boxes show the selected key, and change it in one history entry each
        await WindowTestHelper.PumpAsync();
        var keyBox = view.GetVisualDescendants().OfType<NumericTextBox>().Single(x => x.Name == "KeyBox");
        var valueBox = view.GetVisualDescendants().OfType<NumericTextBox>().Single(x => x.Name == "ValueBox");
        Assert.Equal(0.5, keyBox.Value!.Value, 6);
        Assert.Equal(3, valueBox.Value!.Value, 6);
        var entries = history.Transactions.Count;
        valueBox.Value = 5;
        await WindowTestHelper.PumpAsync();
        Assert.Equal(5f, keys[1].Value);
        await editing.WaitForHistoryAsync(entries + 1);
        keyBox.Value = 0.4;
        await WindowTestHelper.PumpAsync();
        Assert.Equal(0.4f, keys[1].Key, 0.0001f);
        await editing.WaitForHistoryAsync(entries + 2);

        // With two keys selected, the boxes go
        editing.Click(editing.Position(0), RawInputModifiers.Shift);
        await WindowTestHelper.PumpAsync();
        Assert.Empty(view.GetVisualDescendants().OfType<NumericTextBox>());
    }

    private static async Task<EditingFixture> OpenSizeCurveAsync()
    {
        var scene = await OpenSceneAsync();
        try
        {
            (await scene.CurvePropertyAsync(0)).GetCommand(EditCurveCommand.CommandName)!.Execute(null);
            var panel = await scene.WaitForPanelAsync();
            var view = await scene.WaitForViewAsync();
            var editing = new EditingFixture(scene, panel.Editor, view);
            // The entity added to the scene is the history's only entry
            await editing.WaitForHistoryAsync(1);
            return editing;
        }
        catch
        {
            await scene.DisposeAsync();
            throw;
        }
    }

    private sealed class EditingFixture(SceneFixture scene, CurveEditorViewModel editor, CurveEditorView view) : IAsyncDisposable
    {
        public SceneFixture Scene { get; } = scene;

        public CurveEditorViewModel Editor { get; } = editor;

        public CurveEditorView View { get; } = view;

        public Window Window => Scene.Window;

        public DrawingCanvas Canvas => View.GetVisualDescendants().OfType<DrawingCanvas>().Single();

        public IReadOnlyList<ControlPointViewModelBase> Points => ((IEditableCurveViewModel)Editor.SelectedCurve!).ControlPoints.ToList();

        /// <summary>
        /// The position of a key in the window.
        /// </summary>
        public Point Position(int index) => Canvas.TranslatePoint(Points[index].ActualPoint.ToAvalonia(), Window)!.Value;

        /// <summary>
        /// The position halfway between two keys, in the window.
        /// </summary>
        public Point Between(int first, int second) => (Position(first) + Position(second)) / 2;

        public List<int> SelectedIndices()
        {
            var points = Points.ToList();
            return Editor.SelectedControlPoints.Cast<ControlPointViewModelBase>().Select(x => points.IndexOf(x)).Order().ToList();
        }

        public void Click(Point position, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            Window.MouseDown(position, MouseButton.Left, modifiers);
            Window.MouseUp(position, MouseButton.Left, modifiers);
        }

        /// <summary>
        /// Draws a selection rectangle around keys, from its bottom left corner to its top right one.
        /// </summary>
        public void RectangleAround(int[] indices, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            var positions = indices.Select(Position).ToList();
            var from = new Point(positions.Min(x => x.X) - 10, positions.Max(x => x.Y) + 10);
            var to = new Point(positions.Max(x => x.X) + 10, positions.Min(x => x.Y) - 10);
            Rectangle(from, to, modifiers);
        }

        /// <summary>
        /// Waits for the history to have its expected number of entries (it adds them once the properties of the asset are updated),
        /// and checks that it has no more.
        /// </summary>
        public async Task WaitForHistoryAsync(int expected)
        {
            var history = Scene.Fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count >= expected, $"{expected} entries in the history");
            await WindowTestHelper.PumpAsync();
            Assert.Equal(expected, history.Transactions.Count);
        }

        public void Rectangle(Point from, Point to, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            Window.MouseDown(from, MouseButton.Left, modifiers);
            Window.MouseMove((from + to) / 2, modifiers | RawInputModifiers.LeftMouseButton);
            Assert.True(View.FindControl<Control>("SelectionRectangle")!.IsVisible);
            Window.MouseMove(to, modifiers | RawInputModifiers.LeftMouseButton);
            Window.MouseUp(to, MouseButton.Left, modifiers);
        }

        public ValueTask DisposeAsync() => Scene.DisposeAsync();
    }
}
