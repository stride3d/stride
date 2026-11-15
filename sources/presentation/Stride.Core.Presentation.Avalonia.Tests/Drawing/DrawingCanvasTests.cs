// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Drawing;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Presentation.Extensions;
using Color = Stride.Core.Mathematics.Color;

namespace Stride.Core.Presentation.Avalonia.Tests.Drawing;

/// <summary>
/// The Avalonia canvas of the curve editor: <see cref="DrawingCanvas"/> attaches its model and merges the model's requests into one
/// update, <see cref="AvaloniaDrawingContext"/> turns the drawing calls into Avalonia drawings, with the geometry asked for, and
/// <see cref="TrackerControl"/> follows the pointer.
/// </summary>
public sealed class DrawingCanvasTests
{
    [AvaloniaFact]
    public void TheCanvasAttachesItsModelAndDetachesItWhenReplaced()
    {
        var (window, canvas) = Show();
        var first = new TestModel();
        var second = new TestModel();

        canvas.Model = first;
        Assert.Same(canvas, first.View);
        canvas.Model = second;

        Assert.Null(first.View);
        Assert.Same(canvas, second.View);
        window.Close();
    }

    [AvaloniaFact]
    public void TheRequestsOfAFrameGiveOneUpdateOfTheModel()
    {
        var (window, canvas) = Show();
        var model = new TestModel();
        canvas.Model = model;
        Dispatcher.UIThread.RunJobs();
        var updates = model.UpdateCount;

        model.View!.InvalidateDrawing();
        model.View.InvalidateDrawing();
        model.View.InvalidateDrawing();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(updates + 1, model.UpdateCount);
        Assert.True(model.LastUpdateData);
        window.Close();
    }

    [AvaloniaFact]
    public void AResizeUpdatesTheModel()
    {
        var (window, canvas) = Show();
        var model = new TestModel();
        canvas.Model = model;
        Dispatcher.UIThread.RunJobs();
        var updates = model.UpdateCount;

        window.Width = 500;
        Dispatcher.UIThread.RunJobs();

        Assert.True(model.UpdateCount > updates);
        window.Close();
    }

    [AvaloniaFact]
    public void TheCanvasDrawsItsModelAtItsSizeInsideItsBounds()
    {
        var (window, canvas) = Show();
        var model = new TestModel();
        canvas.Model = model;
        Dispatcher.UIThread.RunJobs();

        var renders = model.RenderCount;
        var drawing = Record(canvas.Render);

        // The model was drawn at the size of the canvas when it updated, outside the render pass, which draws that drawing
        Assert.Equal((canvas.Bounds.Width, canvas.Bounds.Height), model.RenderedSize);
        Assert.Equal(renders, model.RenderCount);
        // The background, then the model inside a clip to the bounds of the canvas (replaying a group nests it in others)
        Assert.IsType<GeometryDrawing>(drawing.Children.First());
        var clipped = Assert.Single(Groups(drawing), x => x.ClipGeometry is not null);
        Assert.Equal(new Rect(canvas.Bounds.Size), clipped.ClipGeometry!.Bounds);
        Assert.NotEmpty(clipped.Children);
        window.Close();
    }

    private static IEnumerable<DrawingGroup> Groups(DrawingGroup group)
    {
        yield return group;
        foreach (var child in group.Children.OfType<DrawingGroup>().SelectMany(Groups))
            yield return child;
    }

    [AvaloniaFact]
    public void ShapesAreDrawnWithTheirGeometryAndColors()
    {
        var drawing = Record(context =>
        {
            using var drawingContext = new AvaloniaDrawingContext(context);
            drawingContext.DrawRectangle(new DrawingRect(10, 20, 100, 50), Color.Red, Color.Blue, 2);
            drawingContext.DrawLine(new DrawingPoint(0, 0), new DrawingPoint(30, 40), Color.Green);
            drawingContext.DrawPolyline([new DrawingPoint(0, 0), new DrawingPoint(10, 10), new DrawingPoint(20, 0)], Color.White);
            drawingContext.DrawLineSegments([new DrawingPoint(0, 5), new DrawingPoint(50, 5), new DrawingPoint(0, 15), new DrawingPoint(50, 15)], Color.Gray);
            drawingContext.DrawEllipses([new DrawingPoint(100, 100), new DrawingPoint(200, 100)], 3, 3, Color.Yellow, Color.Yellow);
            drawingContext.DrawPolygon([new DrawingPoint(0, 0), new DrawingPoint(10, 0), new DrawingPoint(5, 5)], Color.Red, Color.Transparent);
        });

        var shapes = drawing.Children.OfType<GeometryDrawing>().ToList();
        Assert.Equal(7, shapes.Count);
        // The rectangle: its fill and its outline
        Assert.Equal(new Rect(10, 20, 100, 50), shapes[0].Geometry!.Bounds);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)shapes[0].Brush!).Color);
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)shapes[0].Pen!.Brush!).Color);
        Assert.Equal(2, shapes[0].Pen!.Thickness);
        // The line, the polyline and the two segments, outlines only
        Assert.Equal(new Rect(0, 0, 30, 40), shapes[1].Geometry!.Bounds);
        Assert.Null(shapes[1].Brush);
        Assert.Equal(new Rect(0, 0, 20, 10), shapes[2].Geometry!.Bounds);
        Assert.Equal(new Rect(0, 5, 50, 10), shapes[3].Geometry!.Bounds);
        // One ellipse per point, centered on it
        Assert.Equal(new Rect(97, 97, 6, 6), shapes[4].Geometry!.Bounds);
        Assert.Equal(new Rect(197, 97, 6, 6), shapes[5].Geometry!.Bounds);
        // A transparent outline draws nothing
        Assert.Null(shapes[6].Pen);
    }

    [AvaloniaFact]
    public void AnEllipseAndACircleAreCenteredOnTheirPoint()
    {
        var drawing = Record(context =>
        {
            using var drawingContext = new AvaloniaDrawingContext(context);
            drawingContext.DrawEllipse(new DrawingPoint(50, 40), new DrawingSize(20, 10), Color.Red, Color.Blue);
            drawingContext.DrawCircle(new DrawingPoint(100, 100), 5, Color.Red, Color.Blue);
        });

        var shapes = drawing.Children.OfType<GeometryDrawing>().ToList();
        Assert.Equal(2, shapes.Count);
        Assert.Equal(new Rect(40, 35, 20, 10), shapes[0].Geometry!.Bounds);
        Assert.Equal(new Rect(95, 95, 10, 10), shapes[1].Geometry!.Bounds);
    }

    [AvaloniaFact]
    public void TheClipIsPushedAndPoppedAndANewClipReplacesTheCurrentOne()
    {
        var drawing = Record(context =>
        {
            using var drawingContext = new AvaloniaDrawingContext(context);
            drawingContext.SetClip(new DrawingRect(0, 0, 50, 50));
            drawingContext.DrawLine(new DrawingPoint(0, 0), new DrawingPoint(100, 100), Color.Red);
            drawingContext.SetClip(new DrawingRect(10, 10, 20, 20));
            drawingContext.DrawLine(new DrawingPoint(0, 0), new DrawingPoint(100, 100), Color.Red);
            drawingContext.ResetClip();
            drawingContext.DrawLine(new DrawingPoint(0, 0), new DrawingPoint(100, 100), Color.Red);
        });

        // Two clipped groups side by side (not nested), then the last line without a clip
        Assert.Equal(3, drawing.Children.Count);
        var first = Assert.IsType<DrawingGroup>(drawing.Children[0]);
        var second = Assert.IsType<DrawingGroup>(drawing.Children[1]);
        Assert.Equal(new Rect(0, 0, 50, 50), first.ClipGeometry!.Bounds);
        Assert.Equal(new Rect(10, 10, 20, 20), second.ClipGeometry!.Bounds);
        Assert.IsType<GeometryDrawing>(Assert.Single(second.Children));
        Assert.IsType<GeometryDrawing>(drawing.Children[2]);
    }

    [AvaloniaFact]
    public void TextsAreMeasuredAndAligned()
    {
        var font = new DrawingFont("Arial", 12);
        Size measured = default;
        var empty = new DrawingSize(-1, -1);
        var drawing = Record(context =>
        {
            using var drawingContext = new AvaloniaDrawingContext(context);
            var size = drawingContext.MeasureText("Label", font);
            measured = new Size(size.Width, size.Height);
            empty = drawingContext.MeasureText(string.Empty, font);
            drawingContext.DrawText(new DrawingPoint(100, 50), Color.White, "Label", font, DrawingHorizontalAlignment.Right, DrawingVerticalAlignment.Bottom);
            drawingContext.DrawTexts([new DrawingPoint(0, 0), new DrawingPoint(0, 20)], Color.White, ["A", "B"], font);
        });

        Assert.True(measured.Width > 0 && measured.Height > 0, measured.ToString());
        // Each text is a drawing of its own
        Assert.Equal(3, drawing.Children.Count);
        // Right and bottom aligned: the text ends at the point, with the measured size
        var label = drawing.Children[0].GetBounds();
        Assert.Equal(100, label.Right, 3);
        Assert.Equal(50, label.Bottom, 3);
        Assert.Equal(measured.Width, label.Width, 3);
        // Left and top aligned (the default): the text starts at the point
        Assert.Equal(new Point(0, 20), drawing.Children[2].GetBounds().TopLeft);
        Assert.Equal(DrawingSize.Empty, empty);
    }

    [AvaloniaFact]
    public void TheTrackerFollowsThePointerOverItsParentAndStaysInsideItsExtents()
    {
        var tracker = new TrackerControl { TrackPointer = true, LineExtents = new Rect(50, 50, 200, 100) };
        var window = new Window { Width = 400, Height = 300, Content = new Grid { Background = Brushes.Transparent, Children = { tracker } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.MouseMove(new Point(100, 80));
        Assert.Equal(new Point(100, 80), tracker.Position);
        Assert.Equal(new Point(100, 80), tracker.ActualPosition);

        window.MouseMove(new Point(390, 10));
        Assert.Equal(new Point(250, 50), tracker.ActualPosition);
        Assert.False(tracker.IsHitTestVisible);

        tracker.TrackPointer = false;
        window.MouseMove(new Point(60, 60));
        Assert.Equal(new Point(390, 10), tracker.Position);
        window.Close();
    }

    [AvaloniaFact]
    public void TheTrackerFollowsThePointerWhenAnotherControlHandlesItsMoves()
    {
        // As the curve editor's canvas while it pans
        var canvas = new Border { Background = Brushes.Transparent };
        canvas.PointerMoved += (_, e) => e.Handled = true;
        var tracker = new TrackerControl { TrackPointer = true, LineExtents = new Rect(0, 0, 400, 300) };
        var window = new Window { Width = 400, Height = 300, Content = new Grid { Background = Brushes.Transparent, Children = { canvas, tracker } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.MouseMove(new Point(100, 80));

        Assert.Equal(new Point(100, 80), tracker.Position);
        window.Close();
    }

    private static (Window Window, DrawingCanvas Canvas) Show()
    {
        var canvas = new DrawingCanvas();
        var window = new Window { Width = 400, Height = 300, Content = canvas };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, canvas);
    }

    private static DrawingGroup Record(Action<DrawingContext> draw)
    {
        var group = new DrawingGroup();
        using (var context = group.Open())
        {
            draw(context);
        }
        return group;
    }

    private sealed class TestModel : IDrawingModel
    {
        public IDrawingView? View { get; private set; }

        public int UpdateCount { get; private set; }

        public bool LastUpdateData { get; private set; }

        public (double Width, double Height) RenderedSize { get; private set; }

        public int RenderCount { get; private set; }

        public void Attach(IDrawingView view) => View = view;

        public void Detach(IDrawingView view)
        {
            if (ReferenceEquals(View, view))
                View = null;
        }

        public void Render(IDrawingContext context, double width, double height)
        {
            RenderedSize = (width, height);
            ++RenderCount;
            context.DrawRectangle(new DrawingRect(0, 0, width, height), Color.Black, Color.White);
        }

        public void Update(bool updateData)
        {
            ++UpdateCount;
            LastUpdateData = updateData;
        }
    }
}
