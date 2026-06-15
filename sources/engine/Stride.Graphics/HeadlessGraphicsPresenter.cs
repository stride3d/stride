// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A graphics presenter that renders to an offscreen render target without a window or swap chain.
///   Used for headless testing with software renderers.
/// </summary>
internal class HeadlessGraphicsPresenter : GraphicsPresenter
{
    private Texture backBuffer;

    public HeadlessGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        : base(device, presentationParameters)
    {
        backBuffer = Texture.New2D(device, Description.BackBufferWidth, Description.BackBufferHeight,
            Description.BackBufferFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
    }

    public override Texture BackBuffer => backBuffer;

    public override object NativePresenter => null;

    public override bool IsFullScreen { get; set; }

    public override void Present() { }

    protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
    {
        backBuffer?.Dispose();
        backBuffer = Texture.New2D(GraphicsDevice, width, height, format,
            TextureFlags.ShaderResource | TextureFlags.RenderTarget);
    }

    protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
    {
        DepthStencilBuffer?.Dispose();
        // Expose the depth buffer as a shader resource so scene passes that sample depth
        // (soft-edge particles, SSAO, light shafts, ...) get a valid SRV view. Without it,
        // Vulkan leaves NativeImageView = Null and descriptor writes hit VUID 02997.
        var flags = TextureFlags.DepthStencil;
        if (GraphicsDevice.Features.HasDepthAsSRV)
            flags |= TextureFlags.ShaderResource;
        DepthStencilBuffer = Texture.New2D(GraphicsDevice, width, height, format, flags);
    }

    protected override void Destroy()
    {
        backBuffer?.Dispose();
        base.Destroy();
    }
}
