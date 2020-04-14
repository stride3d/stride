// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP && (XENKO_GRAPHICS_API_DIRECT3D || XENKO_GRAPHICS_API_VULKAN) && (XENKO_UI_WINFORMS || XENKO_UI_WPF)
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Form = System.Windows.Forms.Form;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Xenko.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    internal class GameWindowWinforms : GameWindow<Control>
    {
        private bool isMouseVisible;

        private bool isMouseCurrentlyHidden;

        public Control Control;

        private WindowHandle windowHandle;

        private Form form;

        private bool isFullScreenMaximized;
        private FormBorderStyle savedFormBorderStyle;
        private bool oldVisible;
        private bool deviceChangeChangedVisible;
        private bool? deviceChangeWillBeFullScreen;

        private bool allowUserResizing;
        private bool isBorderLess;

        internal GameWindowWinforms()
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
            if (willBeFullScreen && !isFullScreenMaximized && form != null)
            {
                savedFormBorderStyle = form.FormBorderStyle;
            }

            if (willBeFullScreen != isFullScreenMaximized)
            {
                deviceChangeChangedVisible = true;
                oldVisible = Visible;
                Visible = false;

                if (form != null)
                {
                    form.SendToBack();
                }
            }
            else
            {
                deviceChangeChangedVisible = false;
            }

            if (!willBeFullScreen && isFullScreenMaximized && form != null)
            {
                form.TopMost = false;
                form.FormBorderStyle = savedFormBorderStyle;
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
                if (form != null)
                {
                    form.BringToFront();
                }
                isFullScreenMaximized = false;
            }

            UpdateFormBorder();

            if (deviceChangeChangedVisible)
                Visible = oldVisible;

            if (form != null)
            {
                form.ClientSize = new Size(clientWidth, clientHeight);
            }

            // Notifies the GameForm about the fullscreen state
            var gameForm = form as GameForm;
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

        protected override void Initialize(GameContext<Control> gameContext)
        {
            Control = gameContext.Control;

            // Setup the initial size of the window
            var width = gameContext.RequestedWidth;
            if (width == 0)
            {
                width = Control is Form ? GraphicsDeviceManager.DefaultBackBufferWidth : Control.ClientSize.Width;
            }

            var height = gameContext.RequestedHeight;
            if (height == 0)
            {
                height = Control is Form ? GraphicsDeviceManager.DefaultBackBufferHeight : Control.ClientSize.Height;
            }

            windowHandle = new WindowHandle(AppContextType.Desktop, Control, Control.Handle);

            Control.ClientSize = new Size(width, height);

            Control.MouseEnter += GameWindowForm_MouseEnter;
            Control.MouseLeave += GameWindowForm_MouseLeave;

            form = Control as Form;
            var gameForm = Control as GameForm;
            if (gameForm != null)
            {
                //gameForm.AppActivated += OnActivated;
                //gameForm.AppDeactivated += OnDeactivated;
                gameForm.UserResized += OnClientSizeChanged;
                gameForm.FullscreenToggle += OnFullscreenToggle;
                gameForm.FormClosing += OnClosing;
            }
            else
            {
                Control.Resize += OnClientSizeChanged;
            }
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null, $"{nameof(InitCallback)} is null");
            Debug.Assert(RunCallback != null, $"{nameof(RunCallback)} is null");

            // Initialize the init callback
            InitCallback();

            Debug.Assert(GameContext is GameContextWinforms, "There is only one possible descendant of GameContext<Control>.");
            var context = (GameContextWinforms)GameContext;
            if (context.IsUserManagingRun)
            {
                context.RunCallback = RunCallback;
                context.ExitCallback = ExitCallback;
            }
            else
            {
                var runCallback = new WindowsMessageLoop.RenderCallback(RunCallback);
                // Run the rendering loop
                try
                {
                    WindowsMessageLoop.Run(Control, () =>
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

        private void GameWindowForm_MouseEnter(object sender, System.EventArgs e)
        {
            if (!isMouseVisible && !isMouseCurrentlyHidden)
            {
                Cursor.Hide();
                isMouseCurrentlyHidden = true;
            }
        }

        private void GameWindowForm_MouseLeave(object sender, System.EventArgs e)
        {
            if (isMouseCurrentlyHidden)
            {
                Cursor.Show();
                isMouseCurrentlyHidden = false;
            }
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
                return Control.Visible;
            }
            set
            {
                Control.Visible = value;
            }
        }

        public override Int2 Position
        {
            get
            {
                if (Control == null)
                    return base.Position;

                return new Int2(Control.Location.X, Control.Location.Y);
            }
            set
            {
                if (Control != null)
                    Control.Location = new Point(value.X, value.Y);

                base.Position = value;
            }
        }

        protected override void SetTitle(string title)
        {
            if (form != null)
            {
                form.Text = title;
            }
        }

        internal override void Resize(int width, int height)
        {
            Control.ClientSize = new Size(width, height);
        }

        public override bool AllowUserResizing
        {
            get
            {
                return allowUserResizing;
            }
            set
            {
                if (form != null)
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
            if (form != null)
            {
                form.MaximizeBox = allowUserResizing;
                form.FormBorderStyle = isFullScreenMaximized || isBorderLess ? FormBorderStyle.None : allowUserResizing ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;

                if (isFullScreenMaximized)
                {
                    form.TopMost = true;
                    form.BringToFront();
                }
            }
        }

        public override Xenko.Core.Mathematics.Rectangle ClientBounds
        {
            get
            {
                // Ensure width and height are at least 1 to avoid divisions by 0
                return new Xenko.Core.Mathematics.Rectangle(0, 0, Math.Max(Control.ClientSize.Width, 1), Math.Max(Control.ClientSize.Height, 1));
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
                if (form != null)
                {
                    return form.WindowState == FormWindowState.Minimized;
                }
                // Check for non-form control
                return false;
            }
        }

        public override bool Focused
        {
            get
            {
                if (form != null)
                {
                    return form.ContainsFocus;
                }
                // Check for non-form control
                return false;
            }
        }

        protected override void Destroy()
        {
            if (Control != null)
            {
                Control.Dispose();
                Control = null;
            }

            base.Destroy();
        }
    }
}
#endif
