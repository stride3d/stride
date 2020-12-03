// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
    /// A GameSystem that allows to draw to another window or control. Currently only valid on desktop with Windows.Forms.
    /// </summary>
    public class GameWindowRenderer : GameSystemBase
    {
        private readonly GameWindowManager windowManager;
        private bool beginDrawOk;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameWindowRenderer" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <param name="gameContext">The window context.</param>
        public GameWindowRenderer(IServiceRegistry registry, GameContext gameContext)
            : base(registry)
        {
            GameContext = gameContext;
            windowManager = new GameWindowManager();
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
        public GraphicsPresenter Presenter => windowManager.Presenter;

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get => windowManager.PreferredBackBufferFormat;
            set => windowManager.PreferredBackBufferFormat = value;
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get => windowManager.PreferredBackBufferHeight;
            set => windowManager.PreferredBackBufferHeight = value;
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get => windowManager.PreferredBackBufferWidth;
            set => windowManager.PreferredBackBufferWidth = value;
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get => windowManager.PreferredDepthStencilFormat;
            set => windowManager.PreferredDepthStencilFormat = value;
        }

        public override void Initialize()
        {
            var gamePlatform = Services.GetService<IGamePlatform>();
            GameContext.RequestedWidth = PreferredBackBufferWidth;
            GameContext.RequestedHeight = PreferredBackBufferHeight;
            Window = gamePlatform.CreateWindow(GameContext);
            windowManager.Initialize(Window);
            Window.Visible = true;

            base.Initialize();
        }

        protected override void Destroy()
        {
            windowManager.Dispose();
            Window?.Dispose();
            Window = null;

            base.Destroy();
        }

        public override bool BeginDraw()
        {
            if (GraphicsDevice != null && Window.Visible)
            {
                windowManager.ApplyChanges();

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
            }
        }
    }
}
