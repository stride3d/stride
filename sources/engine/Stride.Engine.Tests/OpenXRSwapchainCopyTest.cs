// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.VirtualReality;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Checks that the frame rendered for the headset reaches the OpenXR swapchain image. A swapchain image is created by
    /// the OpenXR runtime, so a texture stands in for it, and the screenshot shows what the headset would show.
    /// </summary>
    public class OpenXRSwapchainCopyTest : GameTestBase
    {
        private const int Size = 256;

        private Texture pattern;
        private Texture renderFrame;
        private Texture swapchainImage;

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Four colored quarters, so a flipped or partial copy shows too
            var pixels = new Color[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                    pixels[y * Size + x] = (x < Size / 2, y < Size / 2) switch
                    {
                        (true, true) => Color.Red,
                        (false, true) => Color.Lime,
                        (true, false) => Color.Blue,
                        _ => Color.White,
                    };
            }
            pattern = Texture.New2D(GraphicsDevice, Size, Size, PixelFormat.R8G8B8A8_UNorm, pixels).DisposeBy(this);

            var flags = TextureFlags.ShaderResource | TextureFlags.RenderTarget;
            renderFrame = Texture.New2D(GraphicsDevice, Size, Size, PixelFormat.R8G8B8A8_UNorm, flags).DisposeBy(this);
            swapchainImage = Texture.New2D(GraphicsDevice, Size, Size, PixelFormat.R8G8B8A8_UNorm, flags).DisposeBy(this);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawHeadsetFrame).TakeScreenshot();
        }

        private unsafe void DrawHeadsetFrame()
        {
            var commandList = GraphicsContext.CommandList;

            // A swapchain image holds nothing the game drew
            commandList.Clear(swapchainImage, Color.Black);

            commandList.SetRenderTargetAndViewport(null, renderFrame);
            GraphicsContext.DrawTexture(pattern);

            OpenXRHmd.CopyToSwapchainImage(commandList, renderFrame, swapchainImage.NativeResource.Handle);

            commandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Gray);
            commandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
            commandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            GraphicsContext.DrawTexture(swapchainImage);
        }

        [Fact]
        public void RenderedFrameReachesTheSwapchainImage()
        {
            RunGameTest(new OpenXRSwapchainCopyTest());
        }
    }
}
#endif
