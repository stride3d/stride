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
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    /// Manages the <see cref="GraphicsDevice"/> lifecycle.
    /// </summary>
    public class GameWindowManager : ComponentBase
    {
        #region Fields

        /// <summary>
        /// Default width for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferWidth = 1280;

        /// <summary>
        /// Default height for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferHeight = 720;

        private readonly object lockPresenterCreation;

        private GameWindow window;

        private bool presenterParametersChanged;

        private bool isFullScreen;

        private MultisampleCount preferredMultisampleCount;

        private PixelFormat preferredBackBufferFormat;

        private int preferredBackBufferHeight;

        private int preferredBackBufferWidth;

        private Rational preferredRefreshRate;

        private PixelFormat preferredDepthStencilFormat;

        private DisplayOrientation supportedOrientations;

        private bool synchronizeWithVerticalRetrace;

        private int preferredFullScreenOutputIndex;

        private bool isChangingPresenter;

        private int resizedBackBufferWidth;

        private int resizedBackBufferHeight;

        private bool isBackBufferToResize = false;

        private DisplayOrientation currentWindowOrientation;

        private bool beginDrawOk;

        private IGraphicsDeviceService graphicsDeviceService;

        private bool isReallyFullScreen;

        private ColorSpace preferredColorSpace;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GameWindowManager" /> class.
        /// </summary>
        internal GameWindowManager()
        {
            lockPresenterCreation = new object();

            // Defines all default values
            SynchronizeWithVerticalRetrace = true;
            PreferredColorSpace = ColorSpace.Linear;
            PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
            PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            preferredBackBufferWidth = DefaultBackBufferWidth;
            preferredBackBufferHeight = DefaultBackBufferHeight;
            preferredRefreshRate = new Rational(60, 1);
            PreferredMultisampleCount = MultisampleCount.None;
            PreferredGraphicsProfile = new[]
                {
                    GraphicsProfile.Level_11_1, 
                    GraphicsProfile.Level_11_0, 
                    GraphicsProfile.Level_10_1, 
                    GraphicsProfile.Level_10_0, 
                    GraphicsProfile.Level_9_3, 
                    GraphicsProfile.Level_9_2, 
                    GraphicsProfile.Level_9_1, 
                };
        }

        #endregion

        #region Public Properties

        public GraphicsPresenter Presenter { get; private set; }

        /// <summary>
        /// Gets or sets the list of graphics profile to select from the best feature to the lower feature. See remarks.
        /// </summary>
        /// <value>The graphics profile.</value>
        /// <remarks>
        /// By default, the PreferredGraphicsProfile is set to { <see cref="GraphicsProfile.Level_11_1"/>, 
        /// <see cref="GraphicsProfile.Level_11_0"/>,
        /// <see cref="GraphicsProfile.Level_10_1"/>,
        /// <see cref="GraphicsProfile.Level_10_0"/>,
        /// <see cref="GraphicsProfile.Level_9_3"/>,
        /// <see cref="GraphicsProfile.Level_9_2"/>,
        /// <see cref="GraphicsProfile.Level_9_1"/>}
        /// </remarks>
        public GraphicsProfile[] PreferredGraphicsProfile { get; set; }

        /// <summary>
        /// Gets or sets the shader graphics profile that will be used to compile shaders. See remarks.
        /// </summary>
        /// <value>The shader graphics profile.</value>
        /// <remarks>If this property is not set, the profile used to compile the shader will be taken from the <see cref="GraphicsDevice"/> 
        /// based on the list provided by <see cref="PreferredGraphicsProfile"/></remarks>
        public GraphicsProfile? ShaderProfile { get; set; }

        /// <summary>
        /// If populated the engine will try to initialize the device with the same unique id
        /// </summary>
        public string RequiredAdapterUid { get; set; }

        /// <summary>
        /// Gets or sets the default color space.
        /// </summary>
        /// <value>The default color space.</value>
        public ColorSpace PreferredColorSpace
        {
            get
            {
                return preferredColorSpace;
            }
            set
            {
                if (preferredColorSpace != value)
                {
                    preferredColorSpace = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Sets the preferred graphics profile.
        /// </summary>
        /// <param name="levels">The levels.</param>
        /// <seealso cref="PreferredGraphicsProfile"/>
        public void SetPreferredGraphicsProfile(params GraphicsProfile[] levels)
        {
            PreferredGraphicsProfile = levels;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is full screen.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        public bool IsFullScreen
        {
            get
            {
                return isFullScreen;
            }

            set
            {
                if (isFullScreen == value) return;

                isFullScreen = value;
                presenterParametersChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [prefer multi sampling].
        /// </summary>
        /// <value><c>true</c> if [prefer multi sampling]; otherwise, <c>false</c>.</value>
        public MultisampleCount PreferredMultisampleCount
        {
            get
            {
                return preferredMultisampleCount;
            }

            set
            {
                if (preferredMultisampleCount != value)
                {
                    preferredMultisampleCount = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get
            {
                return preferredBackBufferFormat;
            }

            set
            {
                if (preferredBackBufferFormat != value)
                {
                    preferredBackBufferFormat = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get
            {
                return preferredBackBufferHeight;
            }

            set
            {
                if (preferredBackBufferHeight != value)
                {
                    preferredBackBufferHeight = value;
                    isBackBufferToResize = false;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get
            {
                return preferredBackBufferWidth;
            }

            set
            {
                if (preferredBackBufferWidth != value)
                {
                    preferredBackBufferWidth = value;
                    isBackBufferToResize = false;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get
            {
                return preferredDepthStencilFormat;
            }

            set
            {
                if (preferredDepthStencilFormat != value)
                {
                    preferredDepthStencilFormat = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred refresh rate.
        /// </summary>
        /// <value>The preferred refresh rate.</value>
        public Rational PreferredRefreshRate
        {
            get
            {
                return preferredRefreshRate;
            }

            set
            {
                if (preferredRefreshRate != value)
                {
                    preferredRefreshRate = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex
        {
            get
            {
                return preferredFullScreenOutputIndex;
            }

            set
            {
                if (preferredFullScreenOutputIndex != value)
                {
                    preferredFullScreenOutputIndex = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the supported orientations.
        /// </summary>
        /// <value>The supported orientations.</value>
        public DisplayOrientation SupportedOrientations
        {
            get
            {
                return supportedOrientations;
            }

            set
            {
                if (supportedOrientations != value)
                {
                    supportedOrientations = value;
                    presenterParametersChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [synchronize with vertical retrace].
        /// </summary>
        /// <value><c>true</c> if [synchronize with vertical retrace]; otherwise, <c>false</c>.</value>
        public bool SynchronizeWithVerticalRetrace
        {
            get
            {
                return synchronizeWithVerticalRetrace;
            }
            set
            {
                if (synchronizeWithVerticalRetrace != value)
                {
                    synchronizeWithVerticalRetrace = value;
                    presenterParametersChanged = true;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        public void Initialize(GameWindow window)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));

            graphicsDeviceService = window.Services.GetService<IGraphicsDeviceService>();
            if (graphicsDeviceService == null)
            {
                throw new InvalidOperationException("IGraphicsDeviceService is not registered as a service");
            }

            window.ClientSizeChanged += Window_ClientSizeChanged;
            window.OrientationChanged += Window_OrientationChanged;
            window.FullscreenChanged += Window_FullscreenChanged;
        }

        /// <summary>
        /// Applies the changes from this instance and change or create the <see cref="GraphicsPresenter"/> according to the new values.
        /// </summary>
        public void ApplyChanges()
        {
            if (Presenter is null || presenterParametersChanged)
            {
                if (graphicsDeviceService.GraphicsDevice != null)
                    ChangeOrCreatePresenter();
            }
        }

        #endregion

        protected static DisplayOrientation SelectOrientation(DisplayOrientation orientation, int width, int height, bool allowLandscapeLeftAndRight)
        {
            if (orientation != DisplayOrientation.Default)
            {
                return orientation;
            }

            if (width <= height)
            {
                return DisplayOrientation.Portrait;
            }

            if (allowLandscapeLeftAndRight)
            {
                return DisplayOrientation.LandscapeRight | DisplayOrientation.LandscapeLeft;
            }

            return DisplayOrientation.LandscapeRight;
        }

        protected override void Destroy()
        {
            if (window != null)
            {
                window.ClientSizeChanged -= Window_ClientSizeChanged;
                window.OrientationChanged -= Window_OrientationChanged;
                window.FullscreenChanged -= Window_FullscreenChanged;
            }

            if (Presenter != null)
            {
                // Make sure that the Presenter is reverted to window before shuting down
                // otherwise the Direct3D11.Device will generate an exception on Dispose()
                Presenter.IsFullScreen = false;
                Presenter.Dispose();
                Presenter = null;
            }

            base.Destroy();
        }

        /// <summary>
        /// Determines whether this instance is compatible with the the specified new <see cref="PresentationParameters"/>.
        /// </summary>
        /// <param name="presentationParameters">The new presentation parameters.</param>
        /// <returns><c>true</c> if this instance is compatible with the the specified new <see cref="PresentationParameters"/>; otherwise, <c>false</c>.</returns>
        protected virtual bool CanChangePresenter(PresentationParameters presentationParameters)
        {
            // A change is only possible if width, height, back buffer format or fullscreen state changed while all the other parameters stayed the same
            return Presenter.Description.ColorSpace == presentationParameters.ColorSpace &&
                   Presenter.Description.DepthStencilFormat == presentationParameters.DepthStencilFormat &&
                   Presenter.Description.DeviceWindowHandle == presentationParameters.DeviceWindowHandle &&
                   Presenter.Description.MultisampleCount == presentationParameters.MultisampleCount &&
                   Presenter.Description.PresentationInterval == presentationParameters.PresentationInterval;
        }

        /// <summary>
        /// Finds the best device that is compatible with the preferences defined in this instance.
        /// </summary>
        /// <returns>The graphics device information.</returns>
        protected virtual PresentationParameters FindBestPresentationParameters()
        {
            // TODO: Nearly same code as in GraphicsDeviceManager

            // Setup preferred parameters before passing them to the factory
            var preferredParameters = new PresentationParameters()
            {
                BackBufferWidth = PreferredBackBufferWidth,
                BackBufferHeight = PreferredBackBufferHeight,
                BackBufferFormat = PreferredBackBufferFormat,
                DepthStencilFormat = PreferredDepthStencilFormat,
                RefreshRate = PreferredRefreshRate,
                PreferredFullScreenOutputIndex = PreferredFullScreenOutputIndex,
                IsFullScreen = IsFullScreen,
                MultisampleCount = PreferredMultisampleCount,
                PresentationInterval = SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate,
                ColorSpace = PreferredColorSpace,
            };

            // TODO: Isn't the presenter code already doing this?
            // Remap to Srgb backbuffer if necessary
            if (PreferredColorSpace == ColorSpace.Linear)
            {
                // If the device support SRgb and ColorSpace is linear, we use automatically a SRgb backbuffer
                if (preferredParameters.BackBufferFormat == PixelFormat.R8G8B8A8_UNorm)
                {
                    preferredParameters.BackBufferFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                }
                else if (preferredParameters.BackBufferFormat == PixelFormat.B8G8R8A8_UNorm)
                {
                    preferredParameters.BackBufferFormat = PixelFormat.B8G8R8A8_UNorm_SRgb;
                }
            }
            else
            {
                // If we are looking for gamma and the backbuffer format is SRgb, switch back to non srgb
                if (preferredParameters.BackBufferFormat == PixelFormat.R8G8B8A8_UNorm_SRgb)
                {
                    preferredParameters.BackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
                }
                else if (preferredParameters.BackBufferFormat == PixelFormat.B8G8R8A8_UNorm_SRgb)
                {
                    preferredParameters.BackBufferFormat = PixelFormat.B8G8R8A8_UNorm;
                }
            }

            // Setup resized value if there is a resize pending
            if (!IsFullScreen && isBackBufferToResize)
            {
                preferredParameters.BackBufferWidth = resizedBackBufferWidth;
                preferredParameters.BackBufferHeight = resizedBackBufferHeight;
            }

            var parameters = graphicsDeviceService.FindBestScreenModes(preferredParameters);
            if (parameters.Count == 0)
            {
                throw new InvalidOperationException("No screen modes found");
            }

            RankScreenModes(parameters);

            if (parameters.Count == 0)
            {
                throw new InvalidOperationException("No screen modes found after ranking");
            }
            return parameters[0];
        }

        /// <summary>
        /// Ranks a list of <see cref="GraphicsDeviceInformation"/> before creating a new device.
        /// </summary>
        /// <param name="foundParameters">The list of devices that can be reorder.</param>
        protected virtual void RankScreenModes(List<PresentationParameters> foundParameters)
        {
            foundParameters.Sort(
                (leftParams, rightParams) =>
                    {
                        // Sort by FullScreen mode
                        if (leftParams.IsFullScreen != rightParams.IsFullScreen)
                        {
                            return IsFullScreen != leftParams.IsFullScreen ? 1 : -1;
                        }

                        // Sort by BackBufferFormat
                        int leftFormat = CalculateRankForFormat(leftParams.BackBufferFormat);
                        int rightFormat = CalculateRankForFormat(rightParams.BackBufferFormat);
                        if (leftFormat != rightFormat)
                        {
                            return leftFormat >= rightFormat ? 1 : -1;
                        }

                        // Sort by MultisampleCount
                        if (leftParams.MultisampleCount != rightParams.MultisampleCount)
                        {
                            return leftParams.MultisampleCount <= rightParams.MultisampleCount ? 1 : -1;
                        }

                        // Sort by AspectRatio
                        var targetAspectRatio = (PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0) ? (float)DefaultBackBufferWidth / DefaultBackBufferHeight : (float)PreferredBackBufferWidth / PreferredBackBufferHeight;
                        var leftDiffRatio = Math.Abs(((float)leftParams.BackBufferWidth / leftParams.BackBufferHeight) - targetAspectRatio);
                        var rightDiffRatio = Math.Abs(((float)rightParams.BackBufferWidth / rightParams.BackBufferHeight) - targetAspectRatio);
                        if (Math.Abs(leftDiffRatio - rightDiffRatio) > 0.2f)
                        {
                            return leftDiffRatio >= rightDiffRatio ? 1 : -1;
                        }

                        // Sort by PixelCount
                        int optimalPixelCount;
                        if (IsFullScreen)
                        {
                            var adapter = graphicsDeviceService.GraphicsDevice.Adapter;
                            if (((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0)) &&
                                PreferredFullScreenOutputIndex < adapter.Outputs.Length)
                            {
                                // assume we got here only adapters that have the needed number of outputs:
                                var output = adapter.Outputs[PreferredFullScreenOutputIndex];

                                optimalPixelCount = output.CurrentDisplayMode.Width * output.CurrentDisplayMode.Height;
                            }
                            else
                            {
                                optimalPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                            }
                        }
                        else if ((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0))
                        {
                            optimalPixelCount = DefaultBackBufferWidth * DefaultBackBufferHeight;
                        }
                        else
                        {
                            optimalPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                        }

                        int leftDeltaPixelCount = Math.Abs((leftParams.BackBufferWidth * leftParams.BackBufferHeight) - optimalPixelCount);
                        int rightDeltaPixelCount = Math.Abs((rightParams.BackBufferWidth * rightParams.BackBufferHeight) - optimalPixelCount);
                        if (leftDeltaPixelCount != rightDeltaPixelCount)
                        {
                            return leftDeltaPixelCount >= rightDeltaPixelCount ? 1 : -1;
                        }

                        return 0;
                    });
        }

        private int CalculateRankForFormat(PixelFormat format)
        {
            if (format == PreferredBackBufferFormat)
            {
                return 0;
            }

            if (CalculateFormatSize(format) == CalculateFormatSize(PreferredBackBufferFormat))
            {
                return 1;
            }

            return int.MaxValue;
        }
        
        private int CalculateFormatSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                case PixelFormat.R10G10B10A2_UNorm:
                    return 32;

                case PixelFormat.B5G6R5_UNorm:
                case PixelFormat.B5G5R5A1_UNorm:
                    return 16;
            }

            return 0;
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (!isChangingPresenter && ((window.ClientBounds.Height != 0) || (window.ClientBounds.Width != 0)))
            {
                resizedBackBufferWidth = window.ClientBounds.Width;
                resizedBackBufferHeight = window.ClientBounds.Height;
                isBackBufferToResize = true;
                if (Presenter != null)
                {
                    ChangeOrCreatePresenter();
                }
            }
        }

        private void Window_OrientationChanged(object sender, EventArgs e)
        {
            if ((!isChangingPresenter && ((window.ClientBounds.Height != 0) || (window.ClientBounds.Width != 0))) && (window.CurrentOrientation != currentWindowOrientation))
            {
                if ((window.ClientBounds.Height > window.ClientBounds.Width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                    (window.ClientBounds.Width > window.ClientBounds.Height && preferredBackBufferHeight > preferredBackBufferWidth))
                {
                    //Client size and Back Buffer size are different things
                    //in this case all we care is if orientation changed, if so we swap width and height
                    var w = PreferredBackBufferWidth;
                    PreferredBackBufferWidth = PreferredBackBufferHeight;
                    PreferredBackBufferHeight = w;
                    ApplyChanges();
                }
            }
        }

        private void Window_FullscreenChanged(object sender, EventArgs eventArgs)
        {
            if (sender is GameWindow window)
            {
                IsFullScreen = window.IsFullscreen;
                if (IsFullScreen)
                {
                    PreferredBackBufferWidth = window.PreferredFullscreenSize.X;
                    PreferredBackBufferHeight = window.PreferredFullscreenSize.Y;
                }
                else
                {
                    PreferredBackBufferWidth = window.PreferredWindowedSize.X;
                    PreferredBackBufferHeight = window.PreferredWindowedSize.Y;
                }

                ApplyChanges();
            }
        }

        private void CreatePresenter(PresentationParameters newInfo)
        {
            newInfo.IsFullScreen = isFullScreen;
            newInfo.PresentationInterval = SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate;

            // Create the graphics presenter
            Presenter = graphicsDeviceService.CreatePresenter(newInfo);
        }

        private void ChangeOrCreatePresenter()
        {
            if (graphicsDeviceService.GraphicsDevice is null)
                throw new InvalidOperationException("The graphics device is not yet initialized.");

            // We make sure that we won't be call by an asynchronous event (windows resized)
            lock (lockPresenterCreation)
            {
                using (Profiler.Begin(GraphicsDeviceManagerProfilingKeys.CreateDevice))
                {
                    isChangingPresenter = true;
                    var width = window.ClientBounds.Width;
                    var height = window.ClientBounds.Height;

                    //If the orientation is free to be changed from portrait to landscape we actually need this check now, 
                    //it is mostly useful only at initialization actually tho because Window_OrientationChanged does the same logic on runtime change
                    if (window.CurrentOrientation != currentWindowOrientation)
                    {
                        if ((window.ClientBounds.Height > window.ClientBounds.Width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                            (window.ClientBounds.Width > window.ClientBounds.Height && preferredBackBufferHeight > preferredBackBufferWidth))
                        {
                            //Client size and Back Buffer size are different things
                            //in this case all we care is if orientation changed, if so we swap width and height
                            var w = preferredBackBufferWidth;
                            preferredBackBufferWidth = preferredBackBufferHeight;
                            preferredBackBufferHeight = w;
                        }
                    }

                    var isBeginScreenDeviceChange = false;
                    try
                    {
                        // Notifies the game window for the new orientation
                        var orientation = SelectOrientation(supportedOrientations, PreferredBackBufferWidth, PreferredBackBufferHeight, true);
                        window.SetSupportedOrientations(orientation);

                        var presentationParameters = FindBestPresentationParameters();

                        isFullScreen = presentationParameters.IsFullScreen;
                        window.BeginScreenDeviceChange(presentationParameters.IsFullScreen);
                        isBeginScreenDeviceChange = true;
                        bool needToCreateNewPresenter = true;

                        // Try to resize
                        if (Presenter != null)
                        {
                            if (CanChangePresenter(presentationParameters))
                            {
                                try
                                {
                                    var newWidth = presentationParameters.BackBufferWidth;
                                    var newHeight = presentationParameters.BackBufferHeight;
                                    var newFormat = presentationParameters.BackBufferFormat;
                                    var newOutputIndex = presentationParameters.PreferredFullScreenOutputIndex;

                                    Presenter.Description.PreferredFullScreenOutputIndex = newOutputIndex;
                                    Presenter.Description.RefreshRate = presentationParameters.RefreshRate;
                                    Presenter.Resize(newWidth, newHeight, newFormat);

                                    // Change full screen if needed
                                    Presenter.IsFullScreen = presentationParameters.IsFullScreen;

                                    needToCreateNewPresenter = false;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }

                        // If we still need to create a presenter, then we need to create it
                        if (needToCreateNewPresenter)
                        {
                            CreatePresenter(presentationParameters);
                        }

                        if (Presenter == null)
                        {
                            throw new InvalidOperationException("Unexpected null Presenter");
                        }

                        isReallyFullScreen = Presenter.Description.IsFullScreen;
                        if (Presenter.Description.BackBufferWidth != 0)
                        {
                            width = Presenter.Description.BackBufferWidth;
                        }

                        if (Presenter.Description.BackBufferHeight != 0)
                        {
                            height = Presenter.Description.BackBufferHeight;
                        }
                        presenterParametersChanged = false;
                    }
                    finally
                    {
                        if (isBeginScreenDeviceChange)
                        {
                            window.EndScreenDeviceChange(width, height);
                            window.SetIsReallyFullscreen(isReallyFullScreen);
                        }

                        currentWindowOrientation = window.CurrentOrientation;
                        isChangingPresenter = false;
                    }
                }
            }
        }
    }
}
