// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Stride.Core.Presentation.Avalonia.Drawing;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A control that draws an <see cref="IDrawingModel"/> (for example the curve editor). The model asks for a new drawing
/// through <see cref="IDrawingView.InvalidateDrawing"/>; the requests of a frame are merged into one update of the model's
/// data and one drawing.
/// </summary>
/// <remarks>
/// The model is drawn into a recorded drawing outside the render pass, before the arrange: a model
/// updates its own properties while it draws (the curve editor's curve area, which the tracker is bound to), and Avalonia does not
/// allow a change that invalidates a visual during the render pass. The render pass draws the recorded drawing.
/// </remarks>
public sealed class DrawingCanvas : Control, IDrawingView
{
    /// <summary>
    /// Identifies the <see cref="Model"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IDrawingModel?> ModelProperty =
        AvaloniaProperty.Register<DrawingCanvas, IDrawingModel?>(nameof(Model));

    /// <summary>
    /// Identifies the <see cref="Background"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Panel.BackgroundProperty.AddOwner<DrawingCanvas>();

    private bool isUpdatePending;
    private DrawingGroup? drawing;

    static DrawingCanvas()
    {
        // The whole area takes the pointer, including where nothing is drawn
        BackgroundProperty.OverrideDefaultValue<DrawingCanvas>(Brushes.Transparent);
        AffectsRender<DrawingCanvas>(BackgroundProperty);
    }

    /// <summary>
    /// The model drawn by this canvas.
    /// </summary>
    public IDrawingModel? Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    /// <summary>
    /// The brush drawn under the model.
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <inheritdoc/>
    public void InvalidateDrawing()
    {
        if (isUpdatePending)
            return;

        isUpdatePending = true;
        // After the current input or binding: several requests of the same frame give one update
        Dispatcher.UIThread.Post(UpdateModel, DispatcherPriority.Render);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        if (Background is { } background)
            context.DrawRectangle(background, null, bounds);

        drawing?.Draw(context);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ModelProperty)
        {
            (change.OldValue as IDrawingModel)?.Detach(this);
            (change.NewValue as IDrawingModel)?.Attach(this);
            InvalidateDrawing();
        }
        else if (change.Property == BoundsProperty)
        {
            var oldSize = ((Rect)change.OldValue!).Size;
            var newSize = ((Rect)change.NewValue!).Size;
            if (oldSize != newSize)
                InvalidateDrawing();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateDrawing();
    }

    private void UpdateModel()
    {
        isUpdatePending = false;
        var size = Bounds.Size;
        if (Model is not { } model || size.Width <= 0 || size.Height <= 0)
        {
            drawing = null;
            InvalidateVisual();
            return;
        }

        // The model updates its data (the axes, the curves' samples) before it is drawn
        model.Update(true);
        var group = new DrawingGroup { ClipGeometry = new RectangleGeometry(new Rect(size)) };
        using (var context = group.Open())
        using (var drawingContext = new AvaloniaDrawingContext(context))
        {
            model.Render(drawingContext, size.Width, size.Height);
        }
        drawing = group;
        InvalidateVisual();
    }
}
