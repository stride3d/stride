// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System;
using Stride.Core.Mathematics;
using OpenTK;
using OpenTK.Platform.Android;

namespace Stride.Graphics
{
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        internal static Action<int, int, PresentationParameters> ProcessPresentationParametersOverride;

        private AndroidGameView gameWindow;
        private readonly Texture backBuffer;
        private readonly GraphicsDevice graphicsDevice;
        private readonly PresentationParameters startingPresentationParameters;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters) : base(device, presentationParameters)
        {
            gameWindow = (AndroidGameView)Description.DeviceWindowHandle.NativeWindow;

            graphicsDevice = device;
            startingPresentationParameters = presentationParameters;
            device.InitDefaultRenderTarget(Description);

            backBuffer = Texture.New2D(device, Description.BackBufferWidth, Description.BackBufferHeight, presentationParameters.BackBufferFormat, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        }

        public override Texture BackBuffer => backBuffer;

        public override object NativePresenter => null;

        public override bool IsFullScreen
        {
            get
            {
                return gameWindow.WindowState == WindowState.Fullscreen;
            }
            set
            {
                gameWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
            }
        }

        public override void EndDraw(CommandList commandList, bool present)
        {
            if (present)
            {
                GraphicsDevice.WindowProvidedRenderTexture.InternalSetSize(gameWindow.Width, gameWindow.Height);

                // If we made a fake render target to avoid OpenGL limitations on window-provided back buffer, let's copy the rendering result to it
                commandList.CopyScaler2D(backBuffer, GraphicsDevice.WindowProvidedRenderTexture,
                    new Rectangle(0, 0, backBuffer.Width, backBuffer.Height),
                    new Rectangle(0, 0, GraphicsDevice.WindowProvidedRenderTexture.Width, GraphicsDevice.WindowProvidedRenderTexture.Height), true);

                gameWindow.GraphicsContext.SwapBuffers();
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
