// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Animations;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Assets.Editor.ViewModels.CurveEditor.ControlPoints;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.Tests.CurveEditor;

/// <summary>
/// The curve editor's view models, shared by the WPF and Avalonia editors, on the curves of a particle system in a real session:
/// the list of curves, what is drawn, and each edit as one entry of the undo history with the names of the WPF editor.
/// </summary>
public sealed class CurveEditorViewModelTests
{
    [Fact]
    public async Task OpeningACurveListsAndSelectsItAndOpeningItAgainOnlySelectsIt()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var editor = test.Editor;

        Assert.True(editor.AddCurve((object)test.SizeCurve, "Size"));
        var size = Assert.Single(editor.Curves);
        Assert.Same(size, editor.SelectedCurve);
        Assert.Equal("Size", size.DisplayName);
        var points = Assert.IsAssignableFrom<IEditableCurveViewModel>(size).ControlPoints;
        Assert.Equal([0, 0.5, 1], points.Select(x => x.ActualKey));
        Assert.Equal([1, 3, 2], points.Select(x => x.ActualValue));

        Assert.True(editor.AddCurve((object)test.ColorCurve, "Color"));
        Assert.Equal(2, editor.Curves.Count);
        var color = editor.Curves.ElementAt(1);
        Assert.Same(color, editor.SelectedCurve);
        // A color curve shows one editable child curve per component
        Assert.Equal(["R", "G", "B", "A"], color.Children.Select(x => x.DisplayName));
        Assert.All(color.Children, x => Assert.Equal(2, Assert.IsAssignableFrom<IEditableCurveViewModel>(x).ControlPoints.Count));

        Assert.True(editor.AddCurve((object)test.SizeCurve, "Size again"));
        Assert.Equal(2, editor.Curves.Count);
        Assert.Same(size, editor.SelectedCurve);
    }

    [Fact]
    public async Task OnlyCurvesOfTheSupportedTypesCanBeEdited()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();

        Assert.True(CurveEditorViewModel.CanEdit(typeof(IComputeCurve<float>)));
        Assert.True(CurveEditorViewModel.CanEdit(typeof(ComputeAnimationCurveColor4)));
        Assert.True(CurveEditorViewModel.CanEdit(typeof(IComputeCurve<Quaternion>)));
        Assert.True(CurveEditorViewModel.CanEdit(typeof(IComputeCurve<Vector2>)));
        Assert.True(CurveEditorViewModel.CanEdit(typeof(IComputeCurve<Vector3>)));
        Assert.True(CurveEditorViewModel.CanEdit(typeof(IComputeCurve<Vector4>)));
        Assert.False(CurveEditorViewModel.CanEdit(typeof(IComputeCurve<int>)));
        Assert.False(CurveEditorViewModel.CanEdit(typeof(float)));
        Assert.False(test.Editor.AddCurve("not a curve", "Text"));
        Assert.Empty(test.Editor.Curves);
    }

    [Fact]
    public async Task ARotationCurveIsShownAsOneCurvePerComponentOfTheQuaternion()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();

        Assert.True(test.Editor.AddCurve((object)test.RotationCurve, "Rotation"));

        var rotation = Assert.IsType<RotationKeyFrameCurveViewModel>(test.Editor.SelectedCurve);
        Assert.Equal(["X", "Y", "Z", "W"], rotation.Children.Select(x => x.DisplayName));
        Assert.All(rotation.Children, x => Assert.IsType<RotationComponentCurveViewModel>(x));
    }

    [Fact]
    public async Task DrawingShowsTheAxesTheCurveItsPointsAndTheBox()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        test.OpenSizeCurve();

        var context = test.Draw();

        // The curve area leaves room for the labels of the axes, inside the drawing
        var area = test.Editor.CurveArea;
        Assert.True(area.Left > 0 && area.Bottom < CurveEditorTestScene.Height, area.ToString());
        Assert.True(area.Width > CurveEditorTestScene.Width / 2 && area.Height > CurveEditorTestScene.Height / 2, area.ToString());
        // Grid lines and ticks, then the labels of both axes
        Assert.NotEmpty(context.OfKind("LineSegments"));
        Assert.Equal(2, context.OfKind("Texts").Count());
        Assert.All(context.OfKind("Texts"), x => Assert.NotEmpty(x.Texts));
        // The curve, sampled across the curve area, then its three points
        var curve = Assert.Single(context.OfKind("Polyline"));
        Assert.True(curve.Points.Count >= 3);
        var ellipses = context.OfKind("Ellipses").ToList();
        Assert.Equal(2, ellipses.Count);
        Assert.Equal(3, ellipses[0].Points.Count);
        Assert.Equal(test.ScreenPoint(0.5f, 3), ellipses[0].Points[1]);
        // Then the selection circles, none while no point is selected
        Assert.Empty(ellipses[1].Points);
        // The box around the curve area
        Assert.Contains(context.OfKind("Rectangle"), x => x.Rect == area);
    }

    [Fact]
    public async Task AnEmptyCurveShowsHowToAddAKey()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        test.OpenSizeCurve();
        test.Editor.ClearCurveCommand.Execute();

        var context = test.Draw();

        Assert.Contains(context.OfKind("Texts"), x => x.Texts.Contains("Alt+Left click to add a keyframe"));
        Assert.Empty(context.OfKind("Ellipses"));
    }

    [Fact]
    public async Task AddingAKeyIsOneUndoEntryAtThePointerPosition()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var names = test.RecordUndoNames();

        test.Editor.AddPointCommand.Execute(test.ScreenPoint(0.25f, 2.5f));

        Assert.Equal(["Add control point"], names);
        var keys = test.SizeKeys();
        Assert.Equal(4, keys.Length);
        Assert.Equal(0.25f, keys[1].Key, 3);
        Assert.Equal(2.5f, keys[1].Value, 3);
        Assert.Equal(4, curve.ControlPoints.Count);

        test.UndoRedo.Undo();

        Assert.Equal([(0f, 1f), (0.5f, 3f), (1f, 2f)], test.SizeKeys());
        Assert.Equal(3, curve.ControlPoints.Count);
    }

    [Fact]
    public async Task DraggingTheSelectedKeysMovesThemAllAsOneUndoEntry()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        // Select the first two keys
        var first = test.ScreenPoint(0, 1);
        var second = test.ScreenPoint(0.5f, 3);
        test.Editor.SelectCommand.Execute(new CurveEditorSelection(new DrawingRect(first, second).Inflate(new DrawingThickness(5)), CurveSelectionMode.Replace));
        Assert.Equal(2, test.Editor.SelectedControlPoints.Count);
        var names = test.RecordUndoNames();
        // Move them by a tenth of the key range to the right
        var offset = test.ScreenPoint(0.1f, 0).X - test.ScreenPoint(0, 0).X;
        IResizingTarget target = Assert.IsAssignableFrom<IResizingTarget>(curve);

        target.OnResizingStarted(ResizingDirection.Center);
        target.OnResizingDelta(ResizingDirection.Center, offset / 2, 0);
        target.OnResizingDelta(ResizingDirection.Center, offset / 2, 0);
        target.OnResizingCompleted(ResizingDirection.Center, offset, 0);

        Assert.Equal(["Move control points"], names);
        var keys = test.SizeKeys();
        Assert.Equal(0.1f, keys[0].Key, 3);
        Assert.Equal(0.6f, keys[1].Key, 3);
        Assert.Equal(1f, keys[2].Key, 3);
        Assert.Equal([1f, 3f, 2f], keys.Select(x => (float)Math.Round(x.Value, 3)));

        test.UndoRedo.Undo();

        Assert.Equal([(0f, 1f), (0.5f, 3f), (1f, 2f)], test.SizeKeys());
        Assert.Equal([0, 0.5, 1], curve.ControlPoints.Select(x => Math.Round(x.ActualKey, 3)));
    }

    [Fact]
    public async Task TheSelectionModeReplacesAddsToOrRemovesFromTheSelection()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;
        DrawingRect Around(float key, float value) => new DrawingRect(test.ScreenPoint(key, value), new DrawingSize()).Inflate(new DrawingThickness(3));
        ControlPointViewModelBase[] Selected() => [.. editor.SelectedControlPoints.Cast<ControlPointViewModelBase>().OrderBy(x => x.ActualKey)];

        editor.SelectCommand.Execute(new CurveEditorSelection(Around(0, 1), CurveSelectionMode.Replace));
        Assert.Equal([curve.ControlPoints.ElementAt(0)], Selected());
        Assert.Same(curve.ControlPoints.ElementAt(0), editor.SingleSelectedControlPoint);

        editor.SelectCommand.Execute(new CurveEditorSelection(Around(1, 2), CurveSelectionMode.Add));
        Assert.Equal([curve.ControlPoints.ElementAt(0), curve.ControlPoints.ElementAt(2)], Selected());
        Assert.Null(editor.SingleSelectedControlPoint);

        editor.SelectCommand.Execute(new CurveEditorSelection(Around(0, 1), CurveSelectionMode.Remove));
        Assert.Equal([curve.ControlPoints.ElementAt(2)], Selected());

        editor.SelectCommand.Execute(new CurveEditorSelection(Around(0.5f, 3), CurveSelectionMode.None));
        Assert.Equal([curve.ControlPoints.ElementAt(2)], Selected());

        editor.SelectCommand.Execute(new CurveEditorSelection(Around(0.5f, 3), CurveSelectionMode.Replace));
        Assert.Equal([curve.ControlPoints.ElementAt(1)], Selected());
        Assert.All(curve.ControlPoints, x => Assert.Equal(x == curve.ControlPoints.ElementAt(1), x.IsSelected));
    }

    [Fact]
    public async Task AClickSelectsTheKeyUnderThePointerAndAClickOnEmptySpaceClearsTheSelection()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;

        editor.PreviewClickCommand.Execute(new CurveEditorClick(test.ScreenPoint(0.5f, 3).Offset(2, -2), CurveSelectionMode.Replace));

        Assert.Same(curve.ControlPoints.ElementAt(1), Assert.Single(editor.SelectedControlPoints));
        // A press on a key starts a drag of the selection: the view moves the keys instead of drawing a rectangle
        Assert.True(editor.IsControlPointHovered);

        // Shift adds the clicked key
        editor.PreviewClickCommand.Execute(new CurveEditorClick(test.ScreenPoint(1, 2), CurveSelectionMode.Add));
        Assert.Equal(2, editor.SelectedControlPoints.Count);

        // A click on empty space with a modifier keeps the selection, without one clears it
        var empty = test.ScreenPoint(0.25f, 1);
        editor.PreviewClickCommand.Execute(new CurveEditorClick(empty, CurveSelectionMode.Add));
        Assert.Equal(2, editor.SelectedControlPoints.Count);
        Assert.False(editor.IsControlPointHovered);
        editor.PreviewClickCommand.Execute(new CurveEditorClick(empty, CurveSelectionMode.Replace));
        Assert.Empty(editor.SelectedControlPoints);
    }

    [Fact]
    public async Task AKeyDraggedPastItsNeighborStaysSelected()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;
        editor.PreviewClickCommand.Execute(new CurveEditorClick(test.ScreenPoint(0.5f, 3), CurveSelectionMode.Replace));
        Assert.Single(editor.SelectedControlPoints);
        // Past the last key: the key is removed and added again after it, as a new point
        var offset = test.ScreenPoint(0.7f, 0).X - test.ScreenPoint(0, 0).X;
        IResizingTarget target = Assert.IsAssignableFrom<IResizingTarget>(curve);

        target.OnResizingStarted(ResizingDirection.Center);
        target.OnResizingDelta(ResizingDirection.Center, offset, 0);
        target.OnResizingCompleted(ResizingDirection.Center, offset, 0);

        Assert.Equal([0f, 1f, 1.2f], test.SizeKeys().Select(x => (float)Math.Round(x.Key, 3)));
        // The new point of the moved key is the selection, and the one it replaced has left it
        var selected = Assert.IsAssignableFrom<ControlPointViewModelBase>(Assert.Single(editor.SelectedControlPoints));
        Assert.Same(curve.ControlPoints.ElementAt(2), selected);
        Assert.True(selected.IsSelected);
        Assert.Same(selected, editor.SingleSelectedControlPoint);
        // The navigation goes on from it
        editor.NavigateToControlPointCommand.Execute(-1);
        Assert.Same(curve.ControlPoints.ElementAt(1), Assert.Single(editor.SelectedControlPoints));
    }

    [Fact]
    public async Task AKeyThatGoesLeavesTheSelection()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;
        editor.AddPointCommand.Execute(test.ScreenPoint(0.25f, 2));
        editor.PreviewClickCommand.Execute(new CurveEditorClick(test.ScreenPoint(0.25f, 2), CurveSelectionMode.Replace));
        Assert.Single(editor.SelectedControlPoints);

        // Undoing the add removes the selected key: the selection is empty, not a destroyed point
        test.UndoRedo.Undo();

        Assert.Equal(3, curve.ControlPoints.Count);
        Assert.Empty(editor.SelectedControlPoints);
        Assert.Null(editor.SingleSelectedControlPoint);
    }

    [Fact]
    public async Task DeletingTheSelectedKeysAndClearingTheCurveAreOneUndoEntryEach()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;
        editor.SelectCommand.Execute(new CurveEditorSelection(new DrawingRect(test.ScreenPoint(0, 1), test.ScreenPoint(0.5f, 3)).Inflate(new DrawingThickness(3)), CurveSelectionMode.Replace));
        var names = test.RecordUndoNames();

        editor.DeleteSelectedPointsCommand.Execute();

        Assert.Equal([(1f, 2f)], test.SizeKeys());
        Assert.Single(curve.ControlPoints);

        editor.ClearCurveCommand.Execute();

        Assert.Empty(test.SizeKeys());
        Assert.Empty(curve.ControlPoints);
        Assert.Equal(["Delete 2 points from curve 'Size'", "Clear curve 'Size'"], names);

        test.UndoRedo.Undo();
        test.UndoRedo.Undo();

        Assert.Equal([(0f, 1f), (0.5f, 3f), (1f, 2f)], test.SizeKeys());
        Assert.Equal(3, curve.ControlPoints.Count);
    }

    [Fact]
    public async Task TheNavigationCommandsSelectTheFirstPreviousNextAndLastKey()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;
        editor.PreviewClickCommand.Execute(new CurveEditorClick(test.ScreenPoint(0.5f, 3), CurveSelectionMode.Replace));

        editor.NavigateToControlPointCommand.Execute(1);
        Assert.Same(curve.ControlPoints.ElementAt(2), editor.SingleSelectedControlPoint);
        editor.NavigateToControlPointCommand.Execute(1);
        Assert.Same(curve.ControlPoints.ElementAt(2), editor.SingleSelectedControlPoint);
        editor.NavigateToControlPointCommand.Execute(-1);
        Assert.Same(curve.ControlPoints.ElementAt(1), editor.SingleSelectedControlPoint);
        editor.NavigateToControlPointCommand.Execute(int.MinValue);
        Assert.Same(curve.ControlPoints.ElementAt(0), editor.SingleSelectedControlPoint);
        editor.NavigateToControlPointCommand.Execute(int.MaxValue);
        Assert.Same(curve.ControlPoints.ElementAt(2), editor.SingleSelectedControlPoint);
        Assert.True(curve.ControlPoints.ElementAt(2).IsSelected);
        Assert.False(curve.ControlPoints.ElementAt(0).IsSelected);
    }

    [Fact]
    public async Task EditingTheKeyAndValueOfTheSelectedKeyIsUndoable()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        test.OpenSizeCurve();
        var editor = test.Editor;
        editor.PreviewClickCommand.Execute(new CurveEditorClick(test.ScreenPoint(0.5f, 3), CurveSelectionMode.Replace));
        var point = editor.SingleSelectedControlPoint!;
        var names = test.RecordUndoNames();

        point.ActualValue = 4;

        Assert.Equal(4f, test.SizeKeys()[1].Value);
        Assert.Single(names);

        test.UndoRedo.Undo();

        Assert.Equal(3f, test.SizeKeys()[1].Value);
        Assert.Equal(3, point.ActualValue, 3);
    }

    [Fact]
    public async Task TheEditorFollowsAChangeOfTheKeysMadeElsewhere()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var keyFrames = test.Fixture.Session.AssetNodeContainer.GetNode(test.SizeCurve)[nameof(ComputeAnimationCurveFloat.KeyFrames)].Target!;

        using (test.UndoRedo.CreateTransaction())
        {
            keyFrames.Add(new AnimationKeyFrame<float> { Key = 2, Value = 5 }, new NodeIndex(3));
        }

        Assert.Equal(4, curve.ControlPoints.Count);
        Assert.Equal(2, curve.ControlPoints.ElementAt(3).ActualKey);

        test.UndoRedo.Undo();

        Assert.Equal(3, curve.ControlPoints.Count);
    }

    [Fact]
    public async Task FittingTheViewShowsTheWholeCurveAndFocusCentersOnTheSelection()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        var editor = test.Editor;
        var xAxis = editor.SelectedCurve!.XAxis;
        var yAxis = editor.SelectedCurve.YAxis;

        xAxis.ZoomAt(4, 0);
        yAxis.ZoomAt(4, 1);
        test.Draw();
        Assert.False(editor.CurveArea.Contains(curve.ControlPoints.ElementAt(2).ActualPoint));

        editor.ResetViewCommand.Execute(0);
        test.Draw();
        Assert.All(curve.ControlPoints, x => Assert.True(editor.CurveArea.Inflate(new DrawingThickness(1)).Contains(x.ActualPoint), x.ActualPoint.ToString()));

        editor.PreviewClickCommand.Execute(new CurveEditorClick(curve.ControlPoints.ElementAt(2).ActualPoint, CurveSelectionMode.Replace));
        editor.FocusCommand.Execute();
        test.Draw();
        var center = editor.CurveArea.Center;
        var focused = curve.ControlPoints.ElementAt(2).ActualPoint;
        Assert.Equal(center.X, focused.X, 0);
        Assert.Equal(center.Y, focused.Y, 0);
    }

    [Fact]
    public async Task ACurveOpensFittedInTheView()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();

        // Added then focused before it is ever drawn, as the "Edit curve" command does
        var curve = test.OpenSizeCurve();

        var area = test.Editor.CurveArea.Inflate(new DrawingThickness(1));
        Assert.All(curve.ControlPoints, x => Assert.True(area.Contains(x.ActualPoint), $"{x.ActualPoint} outside {area}"));
    }

    [Fact]
    public void AxisLabelsHaveNoRoundingNoise()
    {
        var axis = new Stride.Assets.Editor.ViewModels.CurveEditor.Axes.LinearAxis();

        // The labels are sums of intervals: 0.2 * 3 is 0.6000000000000001
        Assert.Equal(0.6.ToString(System.Globalization.CultureInfo.CurrentCulture), axis.FormatValue(0.2 * 3));
        Assert.Equal("2.5", axis.FormatValue(2.5).Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."));
    }

    [Fact]
    public async Task FocusOnTheSelectionRedrawsTheView()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        var curve = test.OpenSizeCurve();
        test.Draw();
        var view = new CountingView(test.Editor);
        ((IDrawingModel)test.Editor).Attach(view);
        try
        {
            test.Editor.PreviewClickCommand.Execute(new CurveEditorClick(curve.ControlPoints.ElementAt(2).ActualPoint, CurveSelectionMode.Replace));
            var before = view.Invalidations;

            test.Editor.FocusCommand.Execute();

            // The axes moved: the view draws again without waiting for the next input
            Assert.True(view.Invalidations > before);
        }
        finally
        {
            ((IDrawingModel)test.Editor).Detach(view);
        }
    }

    [Fact]
    public async Task RemovingACurveFromTheListIsNotAnEditOfTheAsset()
    {
        await using var test = await CurveEditorTestScene.OpenAsync();
        test.OpenSizeCurve();
        Assert.True(test.Editor.AddCurve((object)test.ColorCurve, "Color"));
        var names = test.RecordUndoNames();

        test.Editor.RemoveSelectedCurveCommand.Execute();

        Assert.Equal(["Size"], test.Editor.Curves.Select(x => x.DisplayName));
        Assert.Empty(names);
        Assert.Equal(2, test.ColorCurve.KeyFrames.Count);
    }

    [Theory]
    [InlineData(false, false, false, CurveSelectionMode.Replace)]
    [InlineData(true, false, false, CurveSelectionMode.Add)]
    [InlineData(false, true, false, CurveSelectionMode.Remove)]
    [InlineData(true, true, false, CurveSelectionMode.None)]
    [InlineData(false, false, true, CurveSelectionMode.None)]
    [InlineData(true, false, true, CurveSelectionMode.None)]
    public void TheModifierKeysGiveTheSelectionModeOfTheWpfEditor(bool shift, bool control, bool other, CurveSelectionMode expected)
    {
        Assert.Equal(expected, CurveSelectionModes.FromModifiers(shift, control, other));
    }

    private sealed class CountingView(IDrawingModel model) : IDrawingView
    {
        public int Invalidations { get; private set; }

        public IDrawingModel? Model => model;

        public void InvalidateDrawing() => Invalidations++;
    }
}
