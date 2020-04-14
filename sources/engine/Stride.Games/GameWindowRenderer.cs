// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Games
{
    /// <summary>
    /// A GameSystem that allows to draw to another window or control. Currently only valid on desktop with Windows.Forms.
    /// </summary>
    public class GameWindowRenderer : GameSystemBase
    {
        private PixelFormat preferredBackBufferFormat;
        private int preferredBackBufferHeight;
        private int preferredBackBufferWidth;
        private PixelFormat preferredDepthStencilFormat;
        private bool isBackBufferToResize;
        private GraphicsPresenter savedPresenter;
        private bool beginDrawOk;
        private bool windowUserResized;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameWindowRenderer" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <param name="gameContext">The window context.</param>
        public GameWindowRenderer(IServiceRegistry registry, GameContext gameContext)
            : base(registry)
        {
            GameContext = gameContext;
        }

        /// <summary>
        /// Gets the underlying native window.
        /// </summary>
        /// <value>The underlying native window.</value>
        public GameContext GameContext { get; private set; }

        /// <summary>
        /// Gets the window.
        /// </summary>
        /// <value>The window.</value>
        public GameWindow Window { get; private set; }

        /// <summary>
        /// Gets or sets the presenter.
        /// </summary>
        /// <value>The presenter.</value>
        public GraphicsPresenter Presenter { get; protected set; }

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get
            {
                return preferredBackBufferFormat;
            }

            set
            {
                if (preferredBackBufferFormat != value)
                {
                    preferredBackBufferFormat = value;
                    isBackBufferToResize = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get
            {
                return preferredBackBufferHeight;
            }

            set
            {
                if (preferredBackBufferHeight != value)
                {
                    preferredBackBufferHeight = value;
                    isBackBufferToResize = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get
            {
                return preferredBackBufferWidth;
            }

            set
            {
                if (preferredBackBufferWidth != value)
                {
                    preferredBackBufferWidth = value;
                    isBackBufferToResize = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get
            {
                return preferredDepthStencilFormat;
            }

            set
            {
                preferredDepthStencilFormat = value;
            }
        }

        public override void Initialize()
        {
            var gamePlatform = Services.GetService<IGamePlatform>();
            GameContext.RequestedWidth = PreferredBackBufferWidth;
            GameContext.RequestedHeight = PreferredBackBufferHeight;
            Window = gamePlatform.CreateWindow(GameContext);
            Window.Visible = true;

            Window.ClientSizeChanged += WindowOnClientSizeChanged;

            base.Initialize();
        }

        private Vector2 GetRequestedSize(out PixelFormat format)
        {
            var bounds = Window.ClientBounds;
            format = PreferredBackBufferFormat == PixelFormat.None ? PixelFormat.R8G8B8A8_UNorm : PreferredBackBufferFormat;
            return new Vector2(
                PreferredBackBufferWidth == 0 || windowUserResized ? bounds.Width : PreferredBackBufferWidth,
                PreferredBackBufferHeight == 0 || windowUserResized ? bounds.Height : PreferredBackBufferHeight);
        }

        protected virtual void CreateOrUpdatePresenter()
        {
            if (Presenter == null)
            {
                PixelFormat resizeFormat;
                var size = GetRequestedSize(out resizeFormat);
                var presentationParameters = new PresentationParameters((int)size.X, (int)size.Y, Window.NativeWindow, resizeFormat) { DepthStencilFormat = PreferredDepthStencilFormat };
                presentationParameters.PresentationInterval = PresentInterval.Immediate;

#if XENKO_GRAPHICS_API_DIRECT3D11 && XENKO_PLATFORM_UWP
                if (Game.Context is GameContextUWPCoreWindow context && context.IsWindowsMixedReality)
                {
                    Presenter = new WindowsMixedRealityGraphicsPresenter(GraphicsDevice, presentationParameters);
                }
                else
#endif
                {
                    Presenter = new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
                }

                isBackBufferToResize = false;
            }
        }

        public override bool BeginDraw()
        {
            if (GraphicsDevice != null && Window.Visible)
            {
                savedPresenter = GraphicsDevice.Presenter;

                CreateOrUpdatePresenter();

                if (isBackBufferToResize || windowUserResized)
                {
                    PixelFormat resizeFormat;
                    var size = GetRequestedSize(out resizeFormat);
                    Presenter.Resize((int)size.X, (int)size.Y, resizeFormat);

                    isBackBufferToResize = false;
                    windowUserResized = false;
                }

                GraphicsDevice.Presenter = Presenter;

                beginDrawOk = true;
                return true;
            }

            beginDrawOk = false;
            return false;
        }

        public override void EndDraw()
        {
            if (beginDrawOk && GraphicsDevice != null)
            {
                try
                {
                    Presenter.Present();
                }
                catch (GraphicsException ex)
                {
                    if (ex.Status != GraphicsDeviceStatus.Removed && ex.Status != GraphicsDeviceStatus.Reset)
                    {
                        throw;
                    }
                }

                if (savedPresenter != null)
                {
                    GraphicsDevice.Presenter = savedPresenter;
                }
            }
        }

        private void WindowOnClientSizeChanged(object sender, EventArgs eventArgs)
        {
            windowUserResized = true;
        }
    }
}
