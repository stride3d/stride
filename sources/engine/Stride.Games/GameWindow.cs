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
#pragma warning disable SA1402 // File may only contain a single type

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    public abstract class GameWindow : ComponentBase
    {
        #region Fields

        private string title;

        #endregion

        #region Public Events

        /// <summary>
        /// Indicate if the window is currently activated.
        /// </summary>
        public bool IsActivated;

        /// <summary>
        /// Occurs when this window is activated.
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Occurs when device client size is changed.
        /// </summary>
        public event EventHandler<EventArgs> ClientSizeChanged;

        /// <summary>
        /// Occurs when this window is deactivated.
        /// </summary>
        public event EventHandler<EventArgs> Deactivated;

        /// <summary>
        /// Occurs when device orientation is changed.
        /// </summary>
        public event EventHandler<EventArgs> OrientationChanged;

        /// <summary>
        /// Occurs when device fullscreen mode is changed.
        /// </summary>
        public event EventHandler<EventArgs> FullscreenChanged;

        /// <summary>
        /// Occurs before the window gets destroyed.
        /// </summary>
        public event EventHandler<EventArgs> Closing;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets, user possibility to resize this window.
        /// </summary>
        public abstract bool AllowUserResizing { get; set; }

        /// <summary>
        /// Gets the client bounds.
        /// </summary>
        /// <value>The client bounds.</value>
        public abstract Rectangle ClientBounds { get; }

        /// <summary>
        /// Gets the current orientation.
        /// </summary>
        /// <value>The current orientation.</value>
        public abstract DisplayOrientation CurrentOrientation { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is minimized.
        /// </summary>
        /// <value><c>true</c> if this instance is minimized; otherwise, <c>false</c>.</value>
        public abstract bool IsMinimized { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is in focus.
        /// </summary>
        /// <value><c>true</c> if this instance is in focus; otherwise, <c>false</c>.</value>
        public abstract bool Focused { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the mouse pointer is visible over this window.
        /// </summary>
        /// <value><c>true</c> if this instance is mouse visible; otherwise, <c>false</c>.</value>
        public abstract bool IsMouseVisible { get; set; }

        /// <summary>
        /// Gets the native window.
        /// </summary>
        /// <value>The native window.</value>
        public abstract WindowHandle NativeWindow { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="GameWindow" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public abstract bool Visible { get; set; }

        /// <summary>
        /// Gets or sets the position of the window on the screen.
        /// </summary>
        public virtual Int2 Position { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this window has a border
        /// </summary>
        /// <value><c>true</c> if this window has a border; otherwise, <c>false</c>.</value>
        public abstract bool IsBorderLess { get; set; }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string Title
        {
            get
            {
                return title;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (title != value)
                {
                    title = value;
                    SetTitle(title);
                }
            }
        }

        /// <summary>
        /// The size the window should have when switching from fullscreen to windowed mode.
        /// To get the current actual size use <see cref="ClientBounds"/>.
        /// This gets overwritten when the user resizes the window. 
        /// </summary>
        public Int2 PreferredWindowedSize { get; set; } = new Int2(768, 432);

        /// <summary>
        /// The size the window should have when switching from windowed to fullscreen mode.
        /// To get the current actual size use <see cref="ClientBounds"/>.
        /// </summary>
        public Int2 PreferredFullscreenSize { get; set; } = new Int2(1920, 1080);

        /// <summary>
        /// Whether the fullscreen mode should be a borderless window matching the desktop size.
        /// </summary>
        /// <remarks>This flag is currently ignored on all game platforms other than SDL.</remarks>
        public bool FullscreenIsBorderlessWindow { get; set; } = false;

        /// <summary>
        /// Switches between fullscreen and windowed mode.
        /// </summary>
        public bool IsFullscreen
        {
            get => isFullscreen;
            set
            {
                if (value != isFullscreen)
                {
                    isFullscreen = value;
                    FullscreenChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Allow the GraphicsDeviceMagnager to set the actual window state after applying the device changes.
        /// </summary>
        /// <param name="isReallyFullscreen"></param>
        internal void SetIsReallyFullscreen(bool isReallyFullscreen)
        {
            isFullscreen = isReallyFullscreen;
        }

        #endregion

        #region Public Methods and Operators

        public abstract void BeginScreenDeviceChange(bool willBeFullScreen);

        public void EndScreenDeviceChange()
        {
            EndScreenDeviceChange(ClientBounds.Width, ClientBounds.Height);
        }

        public abstract void EndScreenDeviceChange(int clientWidth, int clientHeight);

        #endregion

        #region Methods

        protected internal abstract void Initialize(GameContext gameContext);

        internal bool Exiting;

        internal Action InitCallback;

        internal Action RunCallback;

        internal Action ExitCallback;
        
        private bool isFullscreen;

        internal abstract void Run();

        /// <summary>
        /// Sets the size of the client area and triggers the <see cref="ClientSizeChanged"/> event.
        /// This will trigger a backbuffer resize too.
        /// </summary>
        public void SetSize(Int2 size)
        {
            Resize(size.X, size.Y);
            OnClientSizeChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Only used internally by the device managers when they adapt the window size to the backbuffer size.
        /// Resizes the window, without sending the resized event.
        /// </summary>
        internal abstract void Resize(int width, int height);

        public virtual IMessageLoop CreateUserManagedMessageLoop()
        {
            // Default: not implemented
            throw new PlatformNotSupportedException();
        }

        internal IServiceRegistry Services { get; set; }

        protected internal abstract void SetSupportedOrientations(DisplayOrientation orientations);

        protected void OnActivated(object source, EventArgs e)
        {
            IsActivated = true;

            var handler = Activated;
            handler?.Invoke(source, e);
        }

        protected void OnClientSizeChanged(object source, EventArgs e)
        {
            if (!isFullscreen)
            {
                // Update preferred windowed size in windowed mode 
                var resizeSize = ClientBounds.Size;
                PreferredWindowedSize = new Int2(resizeSize.Width, resizeSize.Height); 
            }
            var handler = ClientSizeChanged;
            handler?.Invoke(this, e);
        }

        protected void OnDeactivated(object source, EventArgs e)
        {
            IsActivated = false;

            var handler = Deactivated;
            handler?.Invoke(source, e);
        }

        protected void OnOrientationChanged(object source, EventArgs e)
        {
            var handler = OrientationChanged;
            handler?.Invoke(this, e);
        }

        protected void OnFullscreenToggle(object source, EventArgs e)
        {
            IsFullscreen = !IsFullscreen;
        }

        protected void OnClosing(object source, EventArgs e)
        {
            var handler = Closing;
            handler?.Invoke(this, e);
        }

        protected abstract void SetTitle(string title);

        #endregion

        internal void OnPause()
        {
            OnDeactivated(this, EventArgs.Empty);
        }

        internal void OnResume()
        {
            OnActivated(this, EventArgs.Empty);
        }
    }

    public abstract class GameWindow<TK> : GameWindow
    {
        protected internal sealed override void Initialize(GameContext gameContext)
        {
            var context = gameContext as GameContext<TK>;
            if (context != null)
            {
                GameContext = context;
                Initialize(context);
            }
            else
            {
                throw new InvalidOperationException("Invalid context for current game.");
            }
        }

        internal GameContext<TK> GameContext;

        protected abstract void Initialize(GameContext<TK> context);
    }
}
