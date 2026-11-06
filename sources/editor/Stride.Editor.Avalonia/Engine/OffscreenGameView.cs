// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// Shows the frames of a game that renders offscreen: the viewport of the editor. The control is an ordinary Avalonia
/// control, so other controls can be laid over it, it can be docked anywhere, and it receives the keyboard and pointer
/// input (see <see cref="AvaloniaInputBridge"/>).
/// </summary>
/// <remarks>
/// <para>
/// The frames reach the control in one of two ways. Through the GPU: <see cref="GpuFramePresenter"/> updates a
/// <see cref="CompositionDrawingSurface"/>, hosted as the child visual of this control, with a texture the game shares
/// with Avalonia's compositor. Or by readback: the game thread copies each frame into one of three
/// <see cref="WriteableBitmap"/>s (<see cref="AcquireBitmap"/>, <see cref="PublishBitmap"/>), and the control draws
/// the latest published bitmap at its next render.
/// </para>
/// <para>
/// A published frame the control had no time to draw is skipped (<see cref="SkippedFrameCount"/>): the control never
/// waits for the game, and the game never waits for the control.
/// </para>
/// </remarks>
public sealed class OffscreenGameView : Control, IOffscreenFrameSink
{
    private readonly object bitmapLock = new();
    private WriteableBitmap? front;
    private WriteableBitmap? pending;
    private WriteableBitmap? free;
    private int skippedFrameCount;
    private TopLevel? topLevel;
    private bool animationFrameRequested;
    private CompositionSurfaceVisual? surfaceVisual;

    static OffscreenGameView()
    {
        FocusableProperty.OverrideMetadata<OffscreenGameView>(new StyledPropertyMetadata<bool>(true));
        ClipToBoundsProperty.OverrideMetadata<OffscreenGameView>(new StyledPropertyMetadata<bool>(true));
    }

    public OffscreenGameView()
    {
        MinWidth = 32;
        MinHeight = 32;
    }

    /// <summary>
    /// Raised on the UI thread when the size of the control in device pixels changes: the game sizes its back buffer after it.
    /// </summary>
    public event EventHandler<PixelSize>? PixelSizeChanged;

    /// <summary>
    /// Gets the size of the control in device pixels.
    /// </summary>
    public PixelSize PixelSize { get; private set; }

    /// <summary>
    /// Gets the number of readback frames the game published that the control never drew, because a newer frame
    /// arrived before its render.
    /// </summary>
    public int SkippedFrameCount => skippedFrameCount;

    /// <summary>
    /// Gets a value indicating whether the GPU interop surface is the content of the control.
    /// </summary>
    public bool HasDrawingSurface => surfaceVisual != null;

    /// <summary>
    /// Gets the compositor of the visual tree this control is in, or <c>null</c> while it is detached.
    /// </summary>
    internal Compositor? Compositor => ElementComposition.GetElementVisual(this)?.Compositor;

    /// <inheritdoc />
    WriteableBitmap IOffscreenFrameSink.AcquireBitmap(int width, int height, PixelFormat format) => AcquireBitmap(width, height, format);

    /// <inheritdoc />
    void IOffscreenFrameSink.PublishBitmap(WriteableBitmap bitmap) => PublishBitmap(bitmap);

    /// <summary>
    /// Gets a bitmap of the given size that the game thread can write a frame into, then <see cref="PublishBitmap"/>.
    /// </summary>
    internal WriteableBitmap AcquireBitmap(int width, int height, PixelFormat format)
    {
        lock (bitmapLock)
        {
            var bitmap = free;
            free = null;
            if (bitmap != null && (bitmap.PixelSize.Width != width || bitmap.PixelSize.Height != height || bitmap.Format != format))
            {
                bitmap.Dispose();
                bitmap = null;
            }
            return bitmap ?? new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), format, AlphaFormat.Opaque);
        }
    }

    /// <summary>
    /// Publishes a bitmap filled by the game thread: the control shows it at its next render.
    /// </summary>
    internal void PublishBitmap(WriteableBitmap bitmap)
    {
        lock (bitmapLock)
        {
            // A frame the control never showed is dropped, and its bitmap goes back to the game
            if (pending != null)
            {
                free?.Dispose();
                free = pending;
                skippedFrameCount++;
            }
            pending = bitmap;
        }
        // The control picks the frame up at its next animation frame (OnAnimationFrame)
    }

    /// <summary>
    /// Creates the drawing surface of the GPU interop and makes it the content of this control (UI thread).
    /// </summary>
    /// <returns>The surface, or <c>null</c> when the control is not in a visual tree.</returns>
    internal CompositionDrawingSurface? CreateDrawingSurface()
    {
        var compositor = Compositor;
        if (compositor == null)
            return null;
        var surface = compositor.CreateDrawingSurface();
        surfaceVisual = compositor.CreateSurfaceVisual();
        surfaceVisual.Surface = surface;
        surfaceVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        ElementComposition.SetElementChildVisual(this, surfaceVisual);
        return surface;
    }

    /// <summary>
    /// Removes the drawing surface of the GPU interop, after a failure: the control shows the readback frames again (UI thread).
    /// </summary>
    internal void RemoveDrawingSurface()
    {
        if (surfaceVisual == null)
            return;
        ElementComposition.SetElementChildVisual(this, null);
        surfaceVisual = null;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        WriteableBitmap? bitmap;
        lock (bitmapLock)
        {
            if (pending != null)
            {
                if (front != null)
                {
                    free?.Dispose();
                    free = front;
                }
                front = pending;
                pending = null;
            }
            bitmap = front;
        }

        // The background shows while the game has no frame yet, and behind the surface of the GPU interop
        context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
        if (bitmap != null)
            context.DrawImage(bitmap, new Rect(bitmap.Size), new Rect(Bounds.Size));
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (surfaceVisual != null)
            surfaceVisual.Size = new Vector(e.NewSize.Width, e.NewSize.Height);
        UpdatePixelSize();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        topLevel = TopLevel.GetTopLevel(this);
        RequestAnimationFrame();
        UpdatePixelSize();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void RequestAnimationFrame()
    {
        if (topLevel == null || animationFrameRequested)
            return;
        animationFrameRequested = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void OnAnimationFrame(TimeSpan time)
    {
        animationFrameRequested = false;
        // A detached control stops asking for animation frames; its next attachment asks again
        if (topLevel == null)
            return;
        bool hasPending;
        lock (bitmapLock)
            hasPending = pending != null;
        if (hasPending)
            InvalidateVisual();
        RequestAnimationFrame();
    }

    private void UpdatePixelSize()
    {
        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var size = PixelSize.FromSize(Bounds.Size, scaling);
        if (size == PixelSize || size.Width <= 0 || size.Height <= 0)
            return;
        PixelSize = size;
        PixelSizeChanged?.Invoke(this, size);
    }
}
