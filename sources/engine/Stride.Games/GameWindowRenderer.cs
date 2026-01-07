// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    ///   A Game System that allows to render to a window.
    /// </summary>
    /// <remarks>
    ///   Note that this Game System can be used only on desktop Windows with Windows Forms currently.
    /// </remarks>
    public class GameWindowRenderer : GameSystemBase
    {
        private PixelFormat preferredBackBufferFormat;
        private int preferredBackBufferHeight;
        private int preferredBackBufferWidth;
        private ColorSpaceType preferredOutputColorSpace = ColorSpaceType.Rgb_Full_G22_None_P709;

        private GraphicsPresenter savedPresenter;

        private bool isBackBufferToResize;
        private bool isColorSpaceToChange;
        private bool beginDrawOk;
        private bool windowUserResized;


        /// <summary>
        ///   Initializes a new instance of the <see cref="GameWindowRenderer"/> class.
        /// </summary>
        /// <param name="registry">The service registry.</param>
        /// <param name="gameContext">The Game context that contains information about the underlying platform's native window.</param>
        public GameWindowRenderer(IServiceRegistry registry, GameContext gameContext) : base(registry)
        {
            GameContext = gameContext;
        }


        /// <summary>
        ///   Gets a context object that contains information about the underlying platform's native window.
        /// </summary>
        public GameContext GameContext { get; }

        /// <summary>
        ///   Gets the window where the Game is rendered.
        /// </summary>
        public GameWindow Window { get; private set; }

        /// <summary>
        ///   Gets or sets the presenter that is used to render the Game to the window.
        /// </summary>
        public GraphicsPresenter Presenter { get; protected set; }

        /// <summary>
        ///   Gets or sets the preferred format for the Back-Buffer.
        /// </summary>
        public PixelFormat PreferredBackBufferFormat
        {
            get => preferredBackBufferFormat;
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
        ///   Gets or sets the preferred output color space the <see cref="Presenter"/> should use.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The output color space can be used to render to HDR monitors.
        ///   </para>
        ///   <para>
        ///     Note that this is currently only supported in Stride when using the Direct3D Graphics API.
        ///     For more information about High Dynamic Range (HDR) rendering, see
        ///     <see href="https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range"/>.
        ///   </para>
        /// </remarks>
        public ColorSpaceType PreferredOutputColorSpace
        {
            get => preferredOutputColorSpace;
            set
            {
                if (preferredOutputColorSpace != value)
                {
                    preferredOutputColorSpace = value;
                    isColorSpaceToChange = true;
                }
            }
        }

        /// <summary>
        ///   Gets or sets the preferred height for the Back-Buffer, in pixels.
        /// </summary>
        public int PreferredBackBufferHeight
        {
            get => preferredBackBufferHeight;
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
        ///   Gets or sets the preferred width for the Back-Buffer, in pixels.
        /// </summary>
        public int PreferredBackBufferWidth
        {
            get => preferredBackBufferWidth;
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
        ///   Gets or sets the preferred Depth-Stencil format.
        /// </summary>
        public PixelFormat PreferredDepthStencilFormat { get; set; }


        /// <inheritdoc/>
        public override void Initialize()
        {
            var gamePlatform = Services.GetService<IGamePlatform>();

            GameContext.RequestedWidth = PreferredBackBufferWidth;
            GameContext.RequestedHeight = PreferredBackBufferHeight;

            Window = gamePlatform.CreateWindow(GameContext);
            Window.Visible = true;

            Window.ClientSizeChanged += WindowOnClientSizeChanged;

            base.Initialize();

            //
            // Handler for the window's client size changed event to track user resizing.
            //
            void WindowOnClientSizeChanged(object sender, EventArgs eventArgs)
            {
                windowUserResized = true;
            }
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            Presenter?.Dispose();
            Presenter = null;

            Window?.Dispose();
            Window = null;

            base.Destroy();
        }


        /// <summary>
        ///   Determines the requested size for the Back-Buffer based on the current window bounds and user preferences.
        /// </summary>
        /// <param name="format">
        ///   When this method returns, contains the pixel format to be used for the Back-Buffer.
        ///   This will be the preferred Back-Buffer format if specified;
        ///   otherwise, it defaults to <see cref="PixelFormat.R8G8B8A8_UNorm"/>.
        /// </param>
        /// <returns>
        ///   An <see cref="Int2"/> structure representing the width and height of the requested Back-Buffer size.
        ///   If the preferred Back-Buffer dimensions are not set or the window has been resized by the user,
        ///   the current window dimensions are used.
        /// </returns>
        private Int2 GetRequestedSize(out PixelFormat format)
        {
            var bounds = Window.ClientBounds;

            format = PreferredBackBufferFormat == PixelFormat.None ? PixelFormat.R8G8B8A8_UNorm : PreferredBackBufferFormat;

            return new Int2(
                PreferredBackBufferWidth == 0 || windowUserResized ? bounds.Width : PreferredBackBufferWidth,
                PreferredBackBufferHeight == 0 || windowUserResized ? bounds.Height : PreferredBackBufferHeight);
        }

        /// <summary>
        ///   Creates a new <see cref="GraphicsPresenter"/> or updates the existing one for rendering graphics.
        /// </summary>
        /// <remarks>
        ///   This method initializes the <see cref="Presenter"/> if it is currently <see langword="null"/>,
        ///   using the requested size and format. It configures the presentation parameters,
        ///   including Depth-Stencil format and presentation interval.
        /// </remarks>
        protected virtual void CreateOrUpdatePresenter()
        {
            if (Presenter is null || isColorSpaceToChange)
            {
                var size = GetRequestedSize(out PixelFormat resizeFormat);
                var presentationParameters = new PresentationParameters(size.X, size.Y, Window.NativeWindow, resizeFormat)
                {
                    DepthStencilFormat = PreferredDepthStencilFormat,
                    PresentationInterval = PresentInterval.Immediate,
                    OutputColorSpace = preferredOutputColorSpace
                };

#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_PLATFORM_UWP
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
                isColorSpaceToChange = false;
            }
        }

        /// <inheritdoc/>
        public override bool BeginDraw()
        {
            if (GraphicsDevice is not null && Window.Visible)
            {
                savedPresenter = GraphicsDevice.Presenter;

                CreateOrUpdatePresenter();

                if (isBackBufferToResize || windowUserResized)
                {
                    var size = GetRequestedSize(out PixelFormat resizeFormat);
                    Presenter.Resize(size.X, size.Y, resizeFormat);

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

        /// <inheritdoc/>
        public override void EndDraw()
        {
            if (beginDrawOk && GraphicsDevice is not null)
            {
                try
                {
                    Presenter.Present();
                }
                catch (GraphicsDeviceException ex) when (ex.Status is not GraphicsDeviceStatus.Removed and not GraphicsDeviceStatus.Reset)
                {
                    throw;
                }

                if (savedPresenter != null)
                {
                    GraphicsDevice.Presenter = savedPresenter;
                }
            }
        }
    }
}
