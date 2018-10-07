// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_UI_SDL
using System;
using System.Diagnostics;
using SDL2;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.SDL;

namespace Xenko.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    internal class GameWindowSDL : GameWindow<Window>
    {
        private bool isMouseVisible;

        private bool isMouseCurrentlyHidden;

        private Window window;

        private WindowHandle windowHandle;

        private bool isFullScreenMaximized;
        private FormBorderStyle savedFormBorderStyle;
        private bool oldVisible;
        private bool deviceChangeChangedVisible;
        private bool? deviceChangeWillBeFullScreen;

        private bool allowUserResizing;
        private bool isBorderLess;

        internal GameWindowSDL()
        {
        }

        public override WindowHandle NativeWindow
        {
            get
            {
                return windowHandle;
            }
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
            if (willBeFullScreen && !isFullScreenMaximized && window != null)
            {
                savedFormBorderStyle = window.FormBorderStyle;
            }

            if (willBeFullScreen != isFullScreenMaximized)
            {
                deviceChangeChangedVisible = true;
                oldVisible = Visible;
                Visible = false;

                if (window != null)
                {
                    window.SendToBack();
                }
            }
            else
            {
                deviceChangeChangedVisible = false;
            }

            if (!willBeFullScreen && isFullScreenMaximized && window != null)
            {
                window.TopMost = false;
                window.FormBorderStyle = savedFormBorderStyle;
            }

            deviceChangeWillBeFullScreen = willBeFullScreen;
        }

        public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
        {
            if (!deviceChangeWillBeFullScreen.HasValue)
                return;

            if (deviceChangeWillBeFullScreen.Value)
            {
                isFullScreenMaximized = true;
            }
            else if (isFullScreenMaximized)
            {
                if (window != null)
                {
                    window.BringToFront();
                }
                isFullScreenMaximized = false;
            }

            UpdateFormBorder();

            if (deviceChangeChangedVisible)
                Visible = oldVisible;

            if (window != null)
            {
                window.ClientSize = new Size2(clientWidth, clientHeight);
            }

            // Notifies the GameForm about the fullscreen state
            var gameForm = window as GameFormSDL;
            if (gameForm != null)
            {
                gameForm.IsFullScreen = isFullScreenMaximized;
            }

            deviceChangeWillBeFullScreen = null;
        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
            // Desktop doesn't have orientation (unless on Windows 8?)
        }

        protected override void Initialize(GameContext<Window> gameContext)
        {
            window = gameContext.Control;

            // Setup the initial size of the window
            var width = gameContext.RequestedWidth;
            if (width == 0)
            {
                width = window.ClientSize.Width;
            }

            var height = gameContext.RequestedHeight;
            if (height == 0)
            {
                height = window.ClientSize.Height;
            }

            windowHandle = new WindowHandle(AppContextType.Desktop, window, window.Handle);

            window.ClientSize = new Size2(width, height);

            window.MouseEnterActions += WindowOnMouseEnterActions;   
            window.MouseLeaveActions += WindowOnMouseLeaveActions;

            var gameForm = window as GameFormSDL;
            if (gameForm != null)
            {
                //gameForm.AppActivated += OnActivated;
                //gameForm.AppDeactivated += OnDeactivated;
                gameForm.UserResized += OnClientSizeChanged;
            }
            else
            {
                window.ResizeEndActions += WindowOnResizeEndActions;
            }
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null, $"{nameof(InitCallback)} is null");
            Debug.Assert(RunCallback != null, $"{nameof(RunCallback)} is null");

            // Initialize the init callback
            InitCallback();

            var runCallback = new SDLMessageLoop.RenderCallback(RunCallback);
            // Run the rendering loop
            try
            {
                SDLMessageLoop.Run(window, () =>
                {
                    if (Exiting)
                    {
                        Destroy();
                        return;
                    }

                    runCallback();
                });
            }
            finally
            {
                ExitCallback?.Invoke();
            }
        }

        private void WindowOnMouseEnterActions(SDL.SDL_WindowEvent sdlWindowEvent)
        {
            if (!isMouseVisible && !isMouseCurrentlyHidden)
            {
                Cursor.Hide();
                isMouseCurrentlyHidden = true;
            }
        }

        private void WindowOnMouseLeaveActions(SDL.SDL_WindowEvent sdlWindowEvent)
        {
            if (isMouseCurrentlyHidden)
            {
                Cursor.Show();
                isMouseCurrentlyHidden = false;
            }
        }

        private void WindowOnResizeEndActions(SDL.SDL_WindowEvent sdlWindowEvent)
        {
            OnClientSizeChanged(window, EventArgs.Empty);
        }

        public override bool IsMouseVisible
        {
            get
            {
                return isMouseVisible;
            }
            set
            {
                if (isMouseVisible != value)
                {
                    isMouseVisible = value;
                    if (isMouseVisible)
                    {
                        if (isMouseCurrentlyHidden)
                        {
                            Cursor.Show();
                            isMouseCurrentlyHidden = false;
                        }
                    }
                    else if (!isMouseCurrentlyHidden)
                    {
                        Cursor.Hide();
                        isMouseCurrentlyHidden = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="GameWindow" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public override bool Visible
        {
            get
            {
                return window.Visible;
            }
            set
            {
                window.Visible = value;
            }
        }

        public override Int2 Position
        {
            get
            {
                if (window == null)
                    return base.Position;

                return new Int2(window.Location.X, window.Location.Y);
            }
            set
            {
                if (window != null)
                    window.Location = new Point(value.X, value.Y);

                base.Position = value;
            }
        }

        protected override void SetTitle(string title)
        {
            if (window != null)
            {
                window.Text = title;
            }
        }

        internal override void Resize(int width, int height)
        {
            window.ClientSize = new Size2(width, height);
        }

        public override bool AllowUserResizing
        {
            get
            {
                return allowUserResizing;
            }
            set
            {
                if (window != null)
                {
                    allowUserResizing = value;
                    UpdateFormBorder();
                }
            }
        }

        public override bool IsBorderLess
        {
            get
            {
                return isBorderLess;
            }
            set
            {
                if (isBorderLess != value)
                {
                    isBorderLess = value;
                    UpdateFormBorder();
                }
            }
        }

        private void UpdateFormBorder()
        {
            if (window != null)
            {
                window.MaximizeBox = allowUserResizing;
                window.FormBorderStyle = isFullScreenMaximized || isBorderLess ? FormBorderStyle.None : allowUserResizing ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;

                if (isFullScreenMaximized)
                {
                    window.TopMost = true;
                    window.BringToFront();
                }
            }
        }

        public override Rectangle ClientBounds
        {
            get
            {
                // Ensure width and height are at least 1 to avoid divisions by 0
                return new Rectangle(0, 0, Math.Max(window.ClientSize.Width, 1), Math.Max(window.ClientSize.Height, 1));
            }
        }

        public override DisplayOrientation CurrentOrientation
        {
            get
            {
                return DisplayOrientation.Default;
            }
        }

        public override bool IsMinimized
        {
            get
            {
                if (window != null)
                {
                    return window.WindowState == FormWindowState.Minimized;
                }
                // Check for non-window control
                return false;
            }
        }

        public override bool Focused
        {
            get
            {
                if (window != null)
                {
                    return window.Focused;
                }
                // Check for non-window control
                return false;
            }
        }

        protected override void Destroy()
        {
            if (window != null)
            {
                window.Dispose();
                window = null;
            }

            base.Destroy();
        }
    }
}
#endif
