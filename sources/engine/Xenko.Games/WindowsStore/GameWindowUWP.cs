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
#if XENKO_PLATFORM_UWP

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Xenko.Graphics;
using Xenko.Core.Mathematics;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.UI.Xaml;
//using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Xenko.Games
{
    /// <summary>
    /// <see cref="GameWindow"/> implementation for UWP. Handles both <see cref="SwapChainPanel"/> and <see cref="CoreWindow"/>.
    /// </summary>
    internal class GameWindowUWP : GameWindow
    {
#region Fields
        private const DisplayOrientations PortraitOrientations = DisplayOrientations.Portrait | DisplayOrientations.PortraitFlipped;
        private const DisplayOrientations LandscapeOrientations = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped;

        private WindowHandle windowHandle;
        private int currentWidth;
        private int currentHeight;

        private SwapChainPanel swapChainPanel = null;
        private CoreWindow coreWindow = null;

        private static readonly Windows.Devices.Input.MouseCapabilities mouseCapabilities = new Windows.Devices.Input.MouseCapabilities();

        private DispatcherTimer resizeTimer = null;

        private double requiredRatio;
        private ApplicationView applicationView;
        private bool canResize;

        private bool visible;
        private bool focused;
#endregion

#region Public Properties

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
                if (swapChainPanel != null)
                {
                    return new Rectangle(0, 0, 
                        (int)(swapChainPanel.ActualWidth  * swapChainPanel.CompositionScaleX + 0.5f),
                        (int)(swapChainPanel.ActualHeight * swapChainPanel.CompositionScaleY + 0.5f));

                }

                if (coreWindow != null)
                {
                    return new Rectangle((int)(coreWindow.Bounds.X), (int)(coreWindow.Bounds.Y), (int)(coreWindow.Bounds.Width), (int)(coreWindow.Bounds.Height));
                }

                throw new ArgumentException($"{nameof(GameWindow)} should have either a {nameof(SwapChainPanel)} or a {nameof(CoreWindow)}");
            }
        }

        public override DisplayOrientation CurrentOrientation => DisplayOrientation.Default;

        public override bool IsMinimized => false;
        
        public override bool Focused => focused;

        private bool isMouseVisible;
        private CoreCursor cursor;

        public override bool IsMouseVisible
        {
            get
            {
                return isMouseVisible;
            }
            set
            {
                if (isMouseVisible == value)
                    return;

                if (mouseCapabilities.MousePresent == 0)
                    return;

                if (value)
                {
                    if (cursor != null)
                    {
                        coreWindow.PointerCursor = cursor;
                    }

                    isMouseVisible = true;
                }
                else
                {
                    if (coreWindow.PointerCursor != null)
                    {
                        cursor = coreWindow.PointerCursor;
                    }

                    //yep thats how you hide the cursor under WinRT api...
                    coreWindow.PointerCursor = null;
                    isMouseVisible = false;
                }
            }
        }

        public override WindowHandle NativeWindow
        {
            get
            {
                return windowHandle;
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
                return visible;
            }
            set
            {
            }
        }

        /// <inheritdoc/>
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

#endregion

#region Public Methods and Operators

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
        {
        }

#endregion

#region Methods

        protected internal override void Initialize(GameContext windowContext)
        {
            swapChainPanel = (windowContext as GameContextUWPXaml)?.Control;
            coreWindow = (windowContext as GameContextUWPCoreWindow)?.Control;

            if (swapChainPanel != null)
            {
                resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                resizeTimer.Tick += ResizeTimerOnTick;

                coreWindow = CoreWindow.GetForCurrentThread();
                windowHandle = new WindowHandle(AppContextType.UWPXaml, swapChainPanel, IntPtr.Zero);
            }
            else if (coreWindow != null)
            {
                coreWindow.SizeChanged += ResizeOnWindowChange;
                windowHandle = new WindowHandle(AppContextType.UWPCoreWindow, coreWindow, IntPtr.Zero);
            }
            else
            {
                Debug.Assert(swapChainPanel == null && coreWindow == null, "GameContext was neither UWPXaml nor UWPCoreWindow");
            }

            applicationView = ApplicationView.GetForCurrentView();            
            if (applicationView != null && windowContext.RequestedWidth != 0 && windowContext.RequestedHeight != 0)
            {
                applicationView.SetPreferredMinSize(new Size(windowContext.RequestedWidth, windowContext.RequestedHeight));
                canResize = applicationView.TryResizeView(new Size(windowContext.RequestedWidth, windowContext.RequestedHeight));
            }

            requiredRatio = windowContext.RequestedWidth/(double)windowContext.RequestedHeight;

            if (swapChainPanel != null)
            {
                swapChainPanel.SizeChanged += swapChainPanel_SizeChanged;
                swapChainPanel.CompositionScaleChanged += swapChainPanel_CompositionScaleChanged;
            }

            coreWindow.SizeChanged += CurrentWindowOnSizeChanged;

            visible = coreWindow.Visible;
            coreWindow.VisibilityChanged += CurrentWindowOnVisibilityChanged;
            coreWindow.Activated += CurrentWindowOnActivated;
        }

        private void CurrentWindowOnSizeChanged(object sender, WindowSizeChangedEventArgs windowSizeChangedEventArgs)
        {
            var newBounds = windowSizeChangedEventArgs.Size;
            HandleSizeChanged(sender, newBounds);
        }

        private void CurrentWindowOnVisibilityChanged( CoreWindow window, VisibilityChangedEventArgs args )
        {
            visible = args.Visible;
        }
        
        private void CurrentWindowOnActivated(CoreWindow window, WindowActivatedEventArgs args)
        {
            switch( args.WindowActivationState )
            {
                case CoreWindowActivationState.PointerActivated:
                case CoreWindowActivationState.CodeActivated:
                    focused = true;
                    break;
                case CoreWindowActivationState.Deactivated:
                    focused = false;
                    break;
                default:
                    focused = true;
                    Debug.WriteLine( $"{nameof(args.WindowActivationState)} '{args.WindowActivationState}' not implemented for {nameof(GameWindowUWP)} in {nameof(CurrentWindowOnActivated)}" );
                    break;
            }
        }

        void swapChainPanel_CompositionScaleChanged(SwapChainPanel sender, object args)
        {
            OnClientSizeChanged(sender, EventArgs.Empty);
        }

        private void ResizeTimerOnTick(object sender, object o)
        {
            resizeTimer.Stop();
            OnClientSizeChanged(sender, EventArgs.Empty);
        }

        private void ResizeOnWindowChange(object sender, object o)
        {
            OnClientSizeChanged(sender, EventArgs.Empty);
        }

        private void HandleSizeChanged(object sender, Size newSize)
        {
            var bounds = newSize;

            // Only supports swapChainPanel for now
            if (swapChainPanel != null && bounds.Width > 0 && bounds.Height > 0 && currentWidth > 0 && currentHeight > 0)
            {
                double panelWidth;
                double panelHeight;
                panelWidth = bounds.Width;
                panelHeight = bounds.Height;

                if (canResize)
                {
                    if (swapChainPanel.Width != panelWidth || swapChainPanel.Height != panelHeight)
                    {
                        // Center the panel
                        swapChainPanel.HorizontalAlignment = HorizontalAlignment.Center;
                        swapChainPanel.VerticalAlignment = VerticalAlignment.Center;

                        swapChainPanel.Width = panelWidth;
                        swapChainPanel.Height = panelHeight;
                    }
                }
                else
                {
                    //mobile device, keep aspect fine
                    var aspect = panelWidth / panelHeight;
                    if (aspect < requiredRatio)
                    {
                        panelWidth = bounds.Width; //real screen width
                        panelHeight = panelWidth / requiredRatio;
                    }
                    else
                    {
                        panelHeight = bounds.Height;
                        panelWidth = panelHeight * requiredRatio;
                    }

                    if (swapChainPanel.Width != panelWidth || swapChainPanel.Height != panelHeight)
                    {
                        // Center the panel
                        swapChainPanel.HorizontalAlignment = HorizontalAlignment.Center;
                        swapChainPanel.VerticalAlignment = VerticalAlignment.Center;

                        swapChainPanel.Width = panelWidth;
                        swapChainPanel.Height = panelHeight;
                    }
                }
            }

            if (resizeTimer != null)
            {
                resizeTimer.Stop();
                resizeTimer.Start();
            }
        }

        private void swapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = e.NewSize;
            HandleSizeChanged(sender, bounds);
            if (resizeTimer != null)
            {
                resizeTimer.Stop();
                resizeTimer.Start();
            }
        }

        internal override void Resize(int width, int height)
        {
            currentWidth = width;
            currentHeight = height;
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            // Call InitCallback only first time
            if (InitCallback != null)
            {
                InitCallback();
                InitCallback = null;
            }

            RunCallback();
        }

        internal override void Run()
        {
            if (swapChainPanel != null)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
                return;
            }

            // Call InitCallback only first time
            if (InitCallback != null)
            {
                InitCallback();
                InitCallback = null;
            }

            try
            {
                while (true)
                {
                    coreWindow.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
                    if (Exiting)
                    {
                        Destroy();
                        break;
                    }

                    RunCallback();
                }
            }
            finally
            {
                ExitCallback?.Invoke();
            }
        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
            // Desktop doesn't have orientation (unless on Windows 8?)
        }

        protected override void SetTitle(string title)
        {

        }

        protected override void Destroy()
        {
            if (swapChainPanel != null)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }
            base.Destroy();
        }
#endregion
    }
}

#endif
