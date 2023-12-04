// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
using System;
using System.Diagnostics;
using Silk.NET.SDL;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.SDL;
using Cursor = Stride.Graphics.SDL.Cursor;
using DisplayOrientation = Stride.Graphics.DisplayOrientation;
using Point = Stride.Core.Mathematics.Point;
using Window = Stride.Graphics.SDL.Window;

namespace Stride.Games
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
        private Point savedFormLocation;
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
            if (!isFullScreenMaximized && window != null)
            {
                savedFormLocation = window.Location;
            }

            deviceChangeWillBeFullScreen = willBeFullScreen;
        }

        public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
        {
            if (!deviceChangeWillBeFullScreen.HasValue)
                return;

            isFullScreenMaximized = deviceChangeWillBeFullScreen.Value;
            if (window != null)
            {
                window.FullscreenIsBorderlessWindow = FullscreenIsBorderlessWindow;
                if (deviceChangeWillBeFullScreen.Value) //windowed to fullscreen
                {
                    window.ClientSize = new Size2(clientWidth, clientHeight);
                    window.IsFullScreen = true;
                }
                else //fullscreen to windowed or window resize
                {
                    window.IsFullScreen = false;
                    window.ClientSize = new Size2(clientWidth, clientHeight);
                    window.Location = savedFormLocation;
                    UpdateFormBorder();
                }
                window.BringToFront();
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
                gameForm.CloseActions += GameForm_CloseActions;
                gameForm.FullscreenToggle += OnFullscreenToggle;
                
            }
            else
            {
                window.ResizeEndActions += WindowOnResizeEndActions;
            }
        }

        private void GameForm_CloseActions()
        {
            OnClosing(this, new EventArgs());
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null, $"{nameof(InitCallback)} is null");
            Debug.Assert(RunCallback != null, $"{nameof(RunCallback)} is null");

            // Initialize the init callback
            InitCallback();

            var context = (GameContextSDL)GameContext;
            if (context.IsUserManagingRun)
            {
                context.RunCallback = RunCallback;
                context.ExitCallback = ExitCallback;
            }
            else
            {
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
        }

        public override IMessageLoop CreateUserManagedMessageLoop()
        {
            return new SDLMessageLoop(window);
        }

        private void WindowOnMouseEnterActions(WindowEvent sdlWindowEvent)
        {
            if (!isMouseVisible && !isMouseCurrentlyHidden)
            {
                Cursor.Hide();
                isMouseCurrentlyHidden = true;
            }
        }

        private void WindowOnMouseLeaveActions(WindowEvent sdlWindowEvent)
        {
            if (isMouseCurrentlyHidden)
            {
                Cursor.Show();
                isMouseCurrentlyHidden = false;
            }
        }

        private void WindowOnResizeEndActions(WindowEvent sdlWindowEvent)
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
