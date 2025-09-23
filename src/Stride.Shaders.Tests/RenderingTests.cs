
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Silk.NET.OpenGL;
using Stride.Graphics.RHI;
using CommunityToolkit.HighPerformance.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Stride.Shaders.Parsing.Tests;

public class RenderingTests
{
    static int width = 800;
    static int height = 600;


    [Theory]
    [InlineData("test.spv")]
    public void RenderTest1(string path)
    {
        // var shader = File.ReadAllBytes(path);
        var renderer = new OpenGLFrameRenderer();
        using var frameBuffer = MemoryOwner<byte>.Allocate(width * height * 4);
        renderer.RenderFrame(frameBuffer.Span);
        var pixels = Image.LoadPixelData<Rgba32>(frameBuffer.Span, width, height);
        Assert.Equal(width, pixels.Width);
        Assert.Equal(height, pixels.Height);
    }
}