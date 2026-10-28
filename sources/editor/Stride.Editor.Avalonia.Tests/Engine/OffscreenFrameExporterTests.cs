// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Stride.Core.Mathematics;
using Stride.Editor.Avalonia.Engine;
using Stride.Graphics;
using AvaloniaPixelFormat = Avalonia.Platform.PixelFormat;

namespace Stride.Editor.Avalonia.Tests.Engine;

/// <summary>
/// The readback ring of the offscreen viewport, on a Stride device without a window (the software renderer of the
/// platform when there is no GPU; skipped when there is no Vulkan or Direct3D device at all).
/// </summary>
public sealed class OffscreenFrameExporterTests
{
    private static readonly object deviceLock = new();

    [AvaloniaTheory]
    [InlineData(PixelFormat.B8G8R8A8_UNorm, 0xFFFF0000u)]
    [InlineData(PixelFormat.B8G8R8A8_UNorm_SRgb, 0xFFFF0000u)]
    [InlineData(PixelFormat.R8G8B8A8_UNorm, 0xFF0000FFu)]
    [InlineData(PixelFormat.R8G8B8A8_UNorm_SRgb, 0xFF0000FFu)]
    public void AClearedBackBufferIsReadBackWithinThreeFrames(PixelFormat format, uint expectedPixel)
    {
        using var device = CreateDeviceOrSkip();
        using var commandList = CommandList.New(device);
        using var backBuffer = Texture.New2D(device, 8, 4, format, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        using var probe = backBuffer.ToStaging();
        var sink = new RecordingSink();
        using var exporter = new OffscreenFrameExporter(sink);

        for (var frame = 0; frame < 3 && sink.Published.Count == 0; frame++)
        {
            commandList.Clear(backBuffer, new Color4(1, 0, 0, 1));
            exporter.EndFrame(commandList, backBuffer);
            WaitForGpu(commandList, backBuffer, probe);
        }

        var bitmap = Assert.Single(sink.Published);
        Assert.Equal(new PixelSize(8, 4), bitmap.PixelSize);
        Assert.Equal(format.ToNonSRgb() == PixelFormat.R8G8B8A8_UNorm ? AvaloniaPixelFormat.Rgba8888 : AvaloniaPixelFormat.Bgra8888, bitmap.Format);
        Assert.Equal(expectedPixel, ReadPixel(bitmap, 7, 3));
        Assert.Equal(0, exporter.DroppedFrameCount);
    }

    [AvaloniaFact]
    public void TheLatestCompletedFrameIsPublishedAndEarlierOnesAreDropped()
    {
        using var device = CreateDeviceOrSkip();
        using var commandList = CommandList.New(device);
        using var backBuffer = Texture.New2D(device, 8, 4, PixelFormat.B8G8R8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        using var probe = backBuffer.ToStaging();
        var sink = new RecordingSink();
        using var exporter = new OffscreenFrameExporter(sink);

        // Three frames without a submission in between: the three staging textures are in flight, the fourth frame is dropped
        var colors = new[] { new Color4(1, 0, 0, 1), new Color4(0, 1, 0, 1), new Color4(0, 0, 1, 1), new Color4(1, 1, 0, 1) };
        foreach (var color in colors)
        {
            commandList.Clear(backBuffer, color);
            exporter.EndFrame(commandList, backBuffer);
        }
        Assert.Equal(1, exporter.DroppedFrameCount);
        WaitForGpu(commandList, backBuffer, probe);

        // The next frame reads the completed copies back and publishes the latest one only
        commandList.Clear(backBuffer, colors[0]);
        exporter.EndFrame(commandList, backBuffer);

        var bitmap = Assert.Single(sink.Published);
        Assert.Equal(0xFF0000FFu, ReadPixel(bitmap, 0, 0));
    }

    private static GraphicsDevice CreateDeviceOrSkip()
    {
        try
        {
            lock (deviceLock)
                return GraphicsDevice.New(DeviceCreationFlags.None, GraphicsProfile.Level_11_0);
        }
        catch (Exception e)
        {
            Assert.Skip($"No graphics device on this machine: {e.GetType().Name}: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Submits the command list and waits for the GPU to finish it, so that a frame's copies are complete at the next frame
    /// as they are at the frame rate of the preview game.
    /// </summary>
    private static void WaitForGpu(CommandList commandList, Texture backBuffer, Texture probe)
    {
        // A blocking readback waits for the fence of everything submitted before it
        var pixels = new byte[backBuffer.Width * backBuffer.Height * 4];
        Assert.True(backBuffer.GetData(commandList, probe, pixels.AsSpan()));
        commandList.Flush();
        commandList.Reset();
    }

    private static unsafe uint ReadPixel(WriteableBitmap bitmap, int x, int y)
    {
        using var buffer = bitmap.Lock();
        return *(uint*)(buffer.Address + y * buffer.RowBytes + x * 4);
    }

    private sealed class RecordingSink : IOffscreenFrameSink
    {
        public List<WriteableBitmap> Published { get; } = [];

        public WriteableBitmap AcquireBitmap(int width, int height, AvaloniaPixelFormat format)
        {
            return new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), format, global::Avalonia.Platform.AlphaFormat.Opaque);
        }

        public void PublishBitmap(WriteableBitmap bitmap) => Published.Add(bitmap);
    }
}
