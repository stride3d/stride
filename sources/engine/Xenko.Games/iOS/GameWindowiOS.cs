// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_IOS
using System;
using System.Diagnostics;
using System.Drawing;
using OpenTK.Platform.iPhoneOS;
using OpenGLES;
using Xenko.Graphics;
using Xenko.Graphics.OpenGL;
using UIKit;
using Rectangle = Xenko.Core.Mathematics.Rectangle;

namespace Xenko.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    internal class GameWindowiOS : GameWindow<iOSWindow>
    {
        private bool hasBeenInitialized;
        private iPhoneOSGameView gameForm;
        private WindowHandle nativeWindow;

        private UIInterfaceOrientation currentOrientation;

        public override WindowHandle NativeWindow
        {
            get
            {
                return nativeWindow;
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

        }

        private UIViewController GetViewController(iPhoneOSGameView form)
        {
            for (UIResponder uiResponder = form; uiResponder != null; uiResponder = uiResponder.NextResponder)
            {
                UIViewController uiViewController = uiResponder as UIViewController;
                if (uiViewController != null)
                    return uiViewController;
            }
            return null;
        }

        protected override void Initialize(GameContext<iOSWindow> gameContext)
        {
            gameForm = gameContext.Control.GameView;
            nativeWindow = new WindowHandle(AppContextType.iOS, gameForm, gameForm.Handle);

            gameForm.Load += gameForm_Load;
            gameForm.Unload += gameForm_Unload;
            gameForm.RenderFrame += gameForm_RenderFrame;
            
            // get the OpenGL ES version
            var contextAvailable = false;
            foreach (var version in OpenGLUtils.GetGLVersions(gameContext.RequestedGraphicsProfile))
            {
                var contextRenderingApi = MajorVersionTOEAGLRenderingAPI(version);
                EAGLContext contextTest = null;
                try
                {
                    contextTest = new EAGLContext(contextRenderingApi);

                    // delete extra context
                    if (contextTest != null)
                        contextTest.Dispose();

                    gameForm.ContextRenderingApi = contextRenderingApi;
                    contextAvailable = true;
                    break;
                }
                catch (Exception)
                {
                    // TODO: log message
                }
            }

            if (!contextAvailable)
                throw new Exception("Graphics context could not be created.");

            gameForm.LayerColorFormat = EAGLColorFormat.RGBA8;
            //gameForm.LayerRetainsBacking = false;

            currentOrientation = UIApplication.SharedApplication.StatusBarOrientation;
        }

        private void gameForm_Load(object sender, EventArgs e)
        {
            hasBeenInitialized = false;
        }

        private void gameForm_Unload(object sender, EventArgs e)
        {
            if (hasBeenInitialized)
            {
                OnPause();
                hasBeenInitialized = false;
            }
        }

        private void gameForm_RenderFrame(object sender, OpenTK.FrameEventArgs e)
        {
            if (InitCallback != null)
            {
                InitCallback();
                InitCallback = null;
            }

            var orientation = UIApplication.SharedApplication.StatusBarOrientation;
            if (orientation != currentOrientation)
            {                
                currentOrientation = orientation;               
                OnOrientationChanged(this, EventArgs.Empty);
            }

            RunCallback();

            if (!hasBeenInitialized)
            {
                OnResume();
                hasBeenInitialized = true;
            }
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null);
            Debug.Assert(RunCallback != null);

            if (gameForm.GraphicsContext != null)
            {
                throw new NotImplementedException("Only supports not yet initialized iPhoneOSGameView.");
            }

            var view = gameForm as IAnimatedGameView;
            if (view != null)
            {
                view.StartAnimating();
            }
            else
            {
                gameForm.Run();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="GameWindow" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public override bool Visible
        {
            get { return gameForm.Visible; }
            set { gameForm.Visible = value; }
        }

        protected override void SetTitle(string title)
        {
            gameForm.Title = title;
        }

        internal override void Resize(int width, int height)
        {
            gameForm.Size = new Size((int)(width / gameForm.ContentScaleFactor), (int)(height / gameForm.ContentScaleFactor));
        }

        public override bool IsBorderLess
        {
            get { return true; }
            set { }
        }

        public override bool AllowUserResizing
        {
            get { return true; }
            set { }
        }

        public override Rectangle ClientBounds => new Rectangle(0, 0, (int)(gameForm.Size.Width*gameForm.ContentScaleFactor), (int)(gameForm.Size.Height*gameForm.ContentScaleFactor));

        public override DisplayOrientation CurrentOrientation
        {
            get
            {
                switch (currentOrientation)
                {
                    case UIInterfaceOrientation.Unknown:
                        return DisplayOrientation.Default;
                    case UIInterfaceOrientation.Portrait:
                        return DisplayOrientation.Portrait;
                    case UIInterfaceOrientation.PortraitUpsideDown:
                        return DisplayOrientation.Portrait;
                    case UIInterfaceOrientation.LandscapeLeft:
                        return DisplayOrientation.LandscapeLeft;
                    case UIInterfaceOrientation.LandscapeRight:
                        return DisplayOrientation.LandscapeRight;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override bool IsMinimized
        {
            get { return gameForm.WindowState == OpenTK.WindowState.Minimized; }
        }

        public override bool Focused
        {
            get { return gameForm.WindowState != OpenTK.WindowState.Minimized;  }
        }

        public override bool IsMouseVisible
        {
            get { return false; }
            set { }
        }

        protected override void Destroy()
        {
            if (gameForm != null)
            {
                GraphicsDevice.UnbindGraphicsContext(gameForm.GraphicsContext);

                var view = gameForm as IAnimatedGameView;
                if (view != null)
                {
                    view.StopAnimating();
                    gameForm.Close();
                }
                else
                {
                    gameForm.Close();
                    gameForm.Dispose();
                }

                gameForm = null;
            }

            base.Destroy();
        }

        private static EAGLRenderingAPI MajorVersionTOEAGLRenderingAPI(int major)
        {
            if (major >= 3)
                return EAGLRenderingAPI.OpenGLES3;
            else
                return EAGLRenderingAPI.OpenGLES2;
        }
    }
}

#endif
