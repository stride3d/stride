namespace Stride.Graphics.RHI;

public abstract class FrameRenderer(uint width = 800, uint height = 600, byte[]? vertexSpirv = null, byte[]? fragmentSpirv = null)
{
    uint width = width;
    uint height = height;
    byte[]? vertexSpirv = vertexSpirv;
    byte[]? fragmentSpirv = fragmentSpirv;
    public abstract void RenderFrame(Span<byte> bytes);
}