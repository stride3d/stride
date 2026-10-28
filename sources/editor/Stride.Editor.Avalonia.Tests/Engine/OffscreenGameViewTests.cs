// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Stride.Editor.Avalonia.Engine;

namespace Stride.Editor.Avalonia.Tests.Engine;

public sealed class OffscreenGameViewTests
{
    private const uint Red = 0xFFFF0000;
    private const uint Green = 0xFF00FF00;

    [AvaloniaFact]
    public void APublishedFrameShowsAtTheNextRender()
    {
        var view = new OffscreenGameView();
        var window = new Window { Width = 16, Height = 16, Content = view };
        window.Show();

        view.PublishBitmap(Fill(view.AcquireBitmap(4, 4, PixelFormat.Bgra8888), Red));

        Assert.Equal(Red, RenderAndReadPixel(window, 8, 8));
    }

    [AvaloniaFact]
    public void AFramePublishedTwiceBeforeARenderIsSkippedAndItsBitmapReused()
    {
        var view = new OffscreenGameView();
        var window = new Window { Width = 16, Height = 16, Content = view };
        window.Show();

        var first = Fill(view.AcquireBitmap(4, 4, PixelFormat.Bgra8888), Red);
        view.PublishBitmap(first);
        var second = Fill(view.AcquireBitmap(4, 4, PixelFormat.Bgra8888), Green);
        Assert.NotSame(first, second);
        view.PublishBitmap(second);

        // The first frame was never drawn: it is skipped, and the game gets its bitmap back
        Assert.Equal(1, view.SkippedFrameCount);
        Assert.Same(first, view.AcquireBitmap(4, 4, PixelFormat.Bgra8888));
        Assert.Equal(Green, RenderAndReadPixel(window, 8, 8));
    }

    [AvaloniaFact]
    public void ABitmapOfAnotherSizeOrFormatIsNotReused()
    {
        var view = new OffscreenGameView();
        var first = view.AcquireBitmap(4, 4, PixelFormat.Bgra8888);
        view.PublishBitmap(first);
        view.PublishBitmap(view.AcquireBitmap(4, 4, PixelFormat.Bgra8888));

        var other = view.AcquireBitmap(8, 4, PixelFormat.Bgra8888);

        Assert.NotSame(first, other);
        Assert.Equal(new PixelSize(8, 4), other.PixelSize);
    }

    [AvaloniaFact]
    public void APixelSizeChangeRaisesPixelSizeChanged()
    {
        var view = new OffscreenGameView { Width = 100, Height = 50 };
        var window = new Window { Content = view, SizeToContent = SizeToContent.WidthAndHeight };
        var sizes = new List<PixelSize>();
        view.PixelSizeChanged += (_, size) => sizes.Add(size);

        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal([new PixelSize(100, 50)], sizes);
        Assert.Equal(new PixelSize(100, 50), view.PixelSize);

        view.Width = 200;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([new PixelSize(100, 50), new PixelSize(200, 50)], sizes);
    }

    private static unsafe WriteableBitmap Fill(WriteableBitmap bitmap, uint color)
    {
        using var buffer = bitmap.Lock();
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            var row = (uint*)(buffer.Address + y * buffer.RowBytes);
            for (var x = 0; x < buffer.Size.Width; x++)
                row[x] = color;
        }
        return bitmap;
    }

    /// <summary>
    /// Renders the window and reads a pixel of the frame, as 0xAARRGGBB.
    /// </summary>
    private static unsafe uint RenderAndReadPixel(Window window, int x, int y)
    {
        // The view picks a published frame up at its next animation frame, and draws it at the render after
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        using var buffer = frame!.Lock();
        var pixel = *(uint*)(buffer.Address + y * buffer.RowBytes + x * 4);
        // The headless platform captures in either byte order
        return frame.Format == PixelFormat.Rgba8888 ? RgbaToArgb(pixel) : pixel;
    }

    private static uint RgbaToArgb(uint rgba)
    {
        // In memory R, G, B, A (little-endian uint 0xAABBGGRR) to 0xAARRGGBB
        return (rgba & 0xFF00FF00) | ((rgba & 0x00FF0000) >> 16) | ((rgba & 0x000000FF) << 16);
    }
}
