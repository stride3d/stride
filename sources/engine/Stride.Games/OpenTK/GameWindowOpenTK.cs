// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if (STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UNIX) && STRIDE_GRAPHICS_API_OPENGL && STRIDE_UI_OPENTK
using System.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    internal class GameWindowOpenTK : GameWindow<OpenTK.GameWindow>
    {
        private bool isMouseVisible;

        private bool isMouseCurrentlyHidden;

        private OpenTK.GameWindow gameForm;
        private WindowHandle nativeWindow;

        internal GameWindowOpenTK()
        {
        }

        public override WindowHandle NativeWindow
        {
            get
            {
                return nativeWindow;
            }
        }

        public override Int2 Position
        {
            get
            {
                if (gameForm == null)
                    return base.Position;

                return new Int2(gameForm.X, gameForm.Y);
            }
            set
            {
                if (gameForm != null)
                {
                    gameForm.X = value.X;
                    gameForm.Y = value.Y;
                }

                base.Position = value;
            }
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {

        }

        public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
        {

        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
            // Desktop doesn't have orientation (unless on Windows 8?)
        }

        protected override void Initialize(GameContext<OpenTK.GameWindow> gameContext)
        {
            gameForm = gameContext.Control;
            nativeWindow = new WindowHandle(AppContextType.DesktopOpenTK, gameForm, gameForm.WindowInfo.Handle);

            // Setup the initial size of the window
            var width = gameContext.RequestedWidth;
            if (width == 0)
            {
                width = gameForm.Width;
            }

            var height = gameContext.RequestedHeight;
            if (height == 0)
            {
                height = gameForm.Height;
            }

            // Update gameForm.ClientSize with (width, height)
            Resize(width, height);

            gameForm.MouseEnter += GameWindowForm_MouseEnter;
            gameForm.MouseLeave += GameWindowForm_MouseLeave;

            gameForm.Resize += OnClientSizeChanged;
            gameForm.Unload += OnClosing;
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null);
            Debug.Assert(RunCallback != null);

            // Initialize the init callback
            InitCallback();

            // Make the window visible
            gameForm.Visible = true;

            // Run the rendering loop
            try
            {
                while (!Exiting)
                {
                    gameForm.ProcessEvents();

                    RunCallback();
                }

                if (gameForm != null)
                {
                    if (OpenTK.Graphics.GraphicsContext.CurrentContext == gameForm.Context)
                        gameForm.Context.MakeCurrent(null);
                    gameForm.Close();
                    gameForm.Dispose();
                    gameForm = null;
                }
            }
            finally
            {
                if (ExitCallback != null)
                {
                    ExitCallback();
                }
            }
        }

        public override IMessageLoop CreateUserManagedMessageLoop()
        {
            return new OpenTKMessageLoop(gameForm);
        }

        private void GameWindowForm_MouseEnter(object sender, System.EventArgs e)
        {
            if (!isMouseVisible && !isMouseCurrentlyHidden)
            {
                gameForm.CursorVisible = false;
                isMouseCurrentlyHidden = true;
            }
        }

        private void GameWindowForm_MouseLeave(object sender, System.EventArgs e)
        {
            if (isMouseCurrentlyHidden)
            {
                gameForm.CursorVisible = true;
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
                            gameForm.CursorVisible = true;
                            isMouseCurrentlyHidden = false;
                        }
                    }
                    else if (!isMouseCurrentlyHidden)
                    {
                        gameForm.CursorVisible = false;
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
                return gameForm.Visible;
            }
            set
            {
                gameForm.Visible = value;
            }
        }

        protected override void SetTitle(string title)
        {
            gameForm.Title = title;
        }

        internal override void Resize(int width, int height)
        {
            // Unfortunately on OpenTK, depending on how you compile it, it may use System.Drawing.Size or
            // OpenTK.Size. To avoid having to put the exact type, we will use C# inference to guess the right
            // type at compile time which will depend on the OpenTK.dll used to compile this code.
            var size = gameForm.ClientSize;
            size.Width = width;
            size.Height = height;
            gameForm.ClientSize = size;
        }

        public override bool IsBorderLess
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public override bool AllowUserResizing
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public override Rectangle ClientBounds
        {
            get
            {
                return new Rectangle(0, 0, gameForm.ClientSize.Width, gameForm.ClientSize.Height);
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
                return gameForm.WindowState == OpenTK.WindowState.Minimized;
            }
        }

        public override bool Focused
        {
            get
            {
                return gameForm.Focused;
            }
        }

        protected override void Destroy()
        {
            if (gameForm != null)
            {
                gameForm.Context.MakeCurrent(null);
                gameForm.Close();
                gameForm.Dispose();
                gameForm = null;
            }

            base.Destroy();
        }
    }
}
#endif
