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
    public class GameWindowSDL : GameWindow
    {
        private bool isMouseVisible;

        private bool isMouseCurrentlyHidden;

        public Window Window { get; private set; }

        private WindowHandle windowHandle;

        private bool isFullScreenMaximized;
        private Point savedFormLocation;
        private bool? deviceChangeWillBeFullScreen;

        private bool allowUserResizing;
        private bool isBorderLess;

        public GameWindowSDL()
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
            if (!isFullScreenMaximized && Window != null)
            {
                savedFormLocation = Window.Location;
            }

            deviceChangeWillBeFullScreen = willBeFullScreen;
        }

        public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
        {
            if (!deviceChangeWillBeFullScreen.HasValue)
                return;

            isFullScreenMaximized = deviceChangeWillBeFullScreen.Value;
            if (Window != null)
            {
                Window.FullscreenIsBorderlessWindow = FullscreenIsBorderlessWindow;
                if (deviceChangeWillBeFullScreen.Value) //windowed to fullscreen
                {
                    Window.ClientSize = new Size2(clientWidth, clientHeight);
                    Window.IsFullScreen = true;
                }
                else //fullscreen to windowed or window resize
                {
                    Window.IsFullScreen = false;
                    Window.ClientSize = new Size2(clientWidth, clientHeight);
                    Window.Location = savedFormLocation;
                    UpdateFormBorder();
                }
                Window.BringToFront();
            }

            deviceChangeWillBeFullScreen = null;
        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
            // Desktop doesn't have orientation (unless on Windows 8?)
        }

        public override void CreateWindow(int width, int height)
        {
            Window = new GameFormSDL();

            // Setup the initial size of the window
            if (width == 0)
            {
                width = Window.ClientSize.Width;
            }

            if (height == 0)
            {
                height = Window.ClientSize.Height;
            }

            windowHandle = new WindowHandle(AppContextType.Desktop, Window, Window.Handle);

            Window.ClientSize = new Size2(width, height);

            Window.MouseEnterActions += WindowOnMouseEnterActions;   
            Window.MouseLeaveActions += WindowOnMouseLeaveActions;

            var gameForm = Window as GameFormSDL;
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
                Window.ResizeEndActions += WindowOnResizeEndActions;
            }
        }

        private void GameForm_CloseActions()
        {
            OnClosing(this, new EventArgs());
        }

        public override void Run()
        {
            Debug.Assert(InitCallback != null, $"{nameof(InitCallback)} is null");
            Debug.Assert(RunCallback != null, $"{nameof(RunCallback)} is null");

            // Initialize the init callback
            InitCallback();

            var runCallback = new SDLMessageLoop.RenderCallback(RunCallback);
            // Run the rendering loop
            try
            {
                SDLMessageLoop.Run(Window, () =>
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

        public override IMessageLoop CreateUserManagedMessageLoop()
        {
            return new SDLMessageLoop(Window);
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
            OnClientSizeChanged(Window, EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="GameWindow" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public override bool Visible
        {
            get
            {
                return Window.Visible;
            }
            set
            {
                Window.Visible = value;
            }
        }

        public override Int2 Position
        {
            get
            {
                if (Window == null)
                    return base.Position;

                return new Int2(Window.Location.X, Window.Location.Y);
            }
            set
            {
                if (Window != null)
                    Window.Location = new Point(value.X, value.Y);

                base.Position = value;
            }
        }

        protected override void SetTitle(string title)
        {
            if (Window != null)
            {
                Window.Text = title;
            }
        }

        public override void Resize(int width, int height)
        {
            Window.ClientSize = new Size2(width, height);
        }

        public override bool AllowUserResizing
        {
            get
            {
                return allowUserResizing;
            }
            set
            {
                if (Window != null)
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
            if (Window != null)
            {
                Window.MaximizeBox = allowUserResizing;
                Window.FormBorderStyle = isFullScreenMaximized || isBorderLess ? FormBorderStyle.None : allowUserResizing ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;

                if (isFullScreenMaximized)
                {
                    Window.TopMost = true;
                    Window.BringToFront();
                }
            }
        }

        public override Rectangle ClientBounds
        {
            get
            {
                // Ensure width and height are at least 1 to avoid divisions by 0
                return new Rectangle(0, 0, Math.Max(Window.ClientSize.Width, 1), Math.Max(Window.ClientSize.Height, 1));
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
                if (Window != null)
                {
                    return Window.WindowState == FormWindowState.Minimized;
                }
                // Check for non-window control
                return false;
            }
        }

        public override bool Focused
        {
            get
            {
                if (Window != null)
                {
                    return Window.Focused;
                }
                // Check for non-window control
                return false;
            }
        }

        protected override void Destroy()
        {
            if (Window != null)
            {
                Window.Dispose();
                Window = null;
            }

            base.Destroy();
        }
    }
}
#endif
