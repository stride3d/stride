// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
using Rectangle = Stride.Core.Mathematics.Rectangle;
using WindowState = Stride.Graphics.SDL.FormWindowState;
using Window = Stride.Graphics.SDL.Window;

namespace Stride.Graphics
{
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private readonly Texture backBuffer;
        private readonly GraphicsDevice graphicsDevice;
        private readonly PresentationParameters startingPresentationParameters;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters) : base(device, presentationParameters)
        {
            graphicsDevice = device;
            startingPresentationParameters = presentationParameters;
            device.InitDefaultRenderTarget(presentationParameters);

            backBuffer = Texture.New2D(device, Description.BackBufferWidth, Description.BackBufferHeight, presentationParameters.BackBufferFormat, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        }

        public override Texture BackBuffer => backBuffer;

        public override object NativePresenter => null;

        public override bool IsFullScreen
        {
            get
            {
                return ((Window)Description.DeviceWindowHandle.NativeWindow).WindowState == WindowState.Fullscreen;
            }
            set
            {
                var gameWindow = (Window)Description.DeviceWindowHandle.NativeWindow;
                if (gameWindow.Exists)
                    gameWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
            }
        }

        public override void EndDraw(CommandList commandList, bool present)
        {
            if (present)
            {
                // If we made a fake render target to avoid OpenGL limitations on window-provided back buffer, let's copy the rendering result to it
                commandList.CopyScaler2D(backBuffer, GraphicsDevice.WindowProvidedRenderTexture,
                new Rectangle(0, 0, backBuffer.Width, backBuffer.Height),
                new Rectangle(0, 0, GraphicsDevice.WindowProvidedRenderTexture.Width, GraphicsDevice.WindowProvidedRenderTexture.Height), true);

                // On macOS, `SwapBuffers` will swap whatever framebuffer is active and in our case it is not the window provided
                // framebuffer, and in addition if the active framebuffer is single buffered, it won't do anything. Forcing a bind
                // will ensure the window is updated.
                commandList.GL.BindFramebuffer(FramebufferTarget.Framebuffer, GraphicsDevice.WindowProvidedFrameBuffer);
                commandList.GraphicsDevice.MainGraphicsContext.SwapBuffers();
            }
        }

        public override void Present()
        {
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            graphicsDevice.OnDestroyed();

            startingPresentationParameters.BackBufferWidth = width;
            startingPresentationParameters.BackBufferHeight = height;

            graphicsDevice.InitDefaultRenderTarget(startingPresentationParameters);

            var newTextureDescrition = backBuffer.Description;
            newTextureDescrition.Width = width;
            newTextureDescrition.Height = height;

            // Manually update the texture
            backBuffer.OnDestroyed();

            // Put it in our back buffer texture
            backBuffer.InitializeFrom(newTextureDescrition);
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescrition = DepthStencilBuffer.Description;
            newTextureDescrition.Width = width;
            newTextureDescrition.Height = height;

            // Manually update the texture
            DepthStencilBuffer.OnDestroyed();

            // Put it in our back buffer texture
            DepthStencilBuffer.InitializeFrom(newTextureDescrition);
        }
    }
}
#endif
