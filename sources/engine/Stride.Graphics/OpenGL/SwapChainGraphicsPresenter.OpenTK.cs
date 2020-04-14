// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if (STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UNIX) && STRIDE_GRAPHICS_API_OPENGL
using Stride.Core.Mathematics;
using OpenTK;
using Rectangle = Stride.Core.Mathematics.Rectangle;
#if STRIDE_UI_SDL
using WindowState = Stride.Graphics.SDL.FormWindowState;
using OpenGLWindow = Stride.Graphics.SDL.Window;
#else
using WindowState = OpenTK.WindowState;
using OpenGLWindow = OpenTK.GameWindow;
#endif

#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Stride.Graphics
{
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private Texture backBuffer;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters) : base(device, presentationParameters)
        {
            device.InitDefaultRenderTarget(presentationParameters);
            backBuffer = Texture.New2D(device, Description.BackBufferWidth, Description.BackBufferHeight, presentationParameters.BackBufferFormat, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        }

        public override Texture BackBuffer
        {
            get { return backBuffer; }
        }

        public override object NativePresenter
        {
            get { return null; }
        }

        public override bool IsFullScreen
        {
            get
            {
                return ((OpenGLWindow)Description.DeviceWindowHandle.NativeWindow).WindowState == WindowState.Fullscreen;
            }
            set
            {
                var gameWindow = (OpenGLWindow)Description.DeviceWindowHandle.NativeWindow;
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
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, GraphicsDevice.WindowProvidedFrameBuffer);
                OpenTK.Graphics.GraphicsContext.CurrentContext.SwapBuffers();
            }
        }

        public override void Present()
        {
        }
        
        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            ReleaseCurrentDepthStencilBuffer();
        }
    }
}
#endif
