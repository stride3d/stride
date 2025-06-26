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

using System;
using System.Collections.Generic;
using System.Threading;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Graphics;

using static Stride.Graphics.GraphicsProfile;

namespace Stride.Games
{
    /// <summary>
    /// Manages the <see cref="GraphicsDevice"/> lifecycle.
    /// </summary>
    public class GraphicsDeviceManager : ComponentBase, IGraphicsDeviceManager, IGraphicsDeviceService
    {
        /// <summary>
        /// Default width for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferWidth = 1280;
        /// <summary>
        /// Default height for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferHeight = 720;

        private readonly object lockDeviceCreation = new();

        private readonly GameBase game;

        private bool deviceSettingsChanged;

        private bool isFullScreen;
        private bool isReallyFullScreen;

        //                         Device settings                 Default values
        private PixelFormat        preferredBackBufferFormat       = PixelFormat.R8G8B8A8_UNorm;
        private int                preferredBackBufferWidth        = DefaultBackBufferWidth;
        private int                preferredBackBufferHeight       = DefaultBackBufferHeight;
        private Rational           preferredRefreshRate            = 60;
        private PixelFormat        preferredDepthStencilFormat     = PixelFormat.D24_UNorm_S8_UInt;
        private MultisampleCount   preferredMultisampleCount       = MultisampleCount.None;
        private ColorSpace         preferredColorSpace             = ColorSpace.Linear;
        private bool               synchronizeWithVerticalRetrace  = true;
        private int                preferredFullScreenOutputIndex; // Populated when ranking devices
        private DisplayOrientation supportedOrientations;          // Populated when ranking devices

        private bool isChangingDevice;

        private int resizedBackBufferWidth;
        private int resizedBackBufferHeight;
        private bool isBackBufferToResize = false;

        private DisplayOrientation currentWindowOrientation;

        private bool beginDrawOk;

        private readonly IGraphicsDeviceFactory graphicsDeviceFactory;


        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDeviceManager" /> class.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <exception cref="System.ArgumentNullException">The game instance cannot be null.</exception>
        internal GraphicsDeviceManager(GameBase game)
        {
            ArgumentNullException.ThrowIfNull(game);
            this.game = game;

            lockDeviceCreation = new object();

            // Defines all default values
            PreferredGraphicsProfile = [ Level_11_1, Level_11_0, Level_10_1, Level_10_0, Level_9_3, Level_9_2, Level_9_1 ];

            graphicsDeviceFactory = game.Services.GetService<IGraphicsDeviceFactory>()
                ?? throw new InvalidOperationException("IGraphicsDeviceFactory is not registered as a service");

            game.WindowCreated += GameOnWindowCreated;
        }


        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;

        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

        private void GameOnWindowCreated(object sender, EventArgs eventArgs)
        {
            game.Window.ClientSizeChanged += Window_ClientSizeChanged;
            game.Window.OrientationChanged += Window_OrientationChanged;
            game.Window.FullscreenChanged += Window_FullscreenChanged;
        }


        public GraphicsDevice GraphicsDevice { get; internal set; }

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
        public GraphicsProfile[] PreferredGraphicsProfile { get; set; } = [ Level_11_1, Level_11_0, Level_10_1, Level_10_0, Level_9_3, Level_9_2, Level_9_1 ];

        /// <summary>
        /// Gets or sets the shader graphics profile that will be used to compile shaders. See remarks.
        /// </summary>
        /// <value>The shader graphics profile.</value>
        /// <remarks>If this property is not set, the profile used to compile the shader will be taken from the <see cref="GraphicsDevice"/> 
        /// based on the list provided by <see cref="PreferredGraphicsProfile"/></remarks>
        public GraphicsProfile? ShaderProfile { get; set; }

        /// <summary>
        /// Gets or sets the device creation flags that will be used to create the <see cref="GraphicsDevice"/>
        /// </summary>
        /// <value>The device creation flags.</value>
        public DeviceCreationFlags DeviceCreationFlags { get; set; }

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
            get => preferredColorSpace;
            set
            {
                if (preferredColorSpace != value)
                {
                    preferredColorSpace = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Sets the preferred graphics profile.
        /// </summary>
        /// <param name="levels">The levels.</param>
        /// <seealso cref="PreferredGraphicsProfile"/>
        /// <summary>
        /// Gets or sets a value indicating whether this instance is full screen.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        public bool IsFullScreen
        {
            get => isFullScreen;
            set
            {
                if (isFullScreen != value)
                {
                    isFullScreen = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [prefer multi sampling].
        /// </summary>
        /// <value><c>true</c> if [prefer multi sampling]; otherwise, <c>false</c>.</value>
        public MultisampleCount PreferredMultisampleCount
        {
            get => preferredMultisampleCount;
            set
            {
                if (preferredMultisampleCount != value)
                {
                    preferredMultisampleCount = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get => preferredBackBufferFormat;
            set
            {
                if (preferredBackBufferFormat != value)
                {
                    preferredBackBufferFormat = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get => preferredBackBufferHeight;
            set
            {
                if (preferredBackBufferHeight != value)
                {
                    preferredBackBufferHeight = value;
                    isBackBufferToResize = false;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get => preferredBackBufferWidth;
            set
            {
                if (preferredBackBufferWidth != value)
                {
                    preferredBackBufferWidth = value;
                    isBackBufferToResize = false;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get => preferredDepthStencilFormat;
            set
            {
                if (preferredDepthStencilFormat != value)
                {
                    preferredDepthStencilFormat = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred refresh rate.
        /// </summary>
        /// <value>The preferred refresh rate.</value>
        public Rational PreferredRefreshRate
        {
            get => preferredRefreshRate;
            set
            {
                if (preferredRefreshRate != value)
                {
                    preferredRefreshRate = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex
        {
            get => preferredFullScreenOutputIndex;
            set
            {
                if (preferredFullScreenOutputIndex != value)
                {
                    preferredFullScreenOutputIndex = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the supported orientations.
        /// </summary>
        /// <value>The supported orientations.</value>
        public DisplayOrientation SupportedOrientations
        {
            get => supportedOrientations;
            set
            {
                if (supportedOrientations != value)
                {
                    supportedOrientations = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [synchronize with vertical retrace].
        /// </summary>
        /// <value><c>true</c> if [synchronize with vertical retrace]; otherwise, <c>false</c>.</value>
        public bool SynchronizeWithVerticalRetrace
        {
            get => synchronizeWithVerticalRetrace;
            set
            {
                if (synchronizeWithVerticalRetrace != value)
                {
                    synchronizeWithVerticalRetrace = value;
                    deviceSettingsChanged = true;
                }
            }
        }


        /// <summary>
        /// Applies the changes from this instance and change or create the <see cref="GraphicsDevice"/> according to the new values.
        /// </summary>
        public void ApplyChanges()
        {
            if (GraphicsDevice is not null && deviceSettingsChanged)
            {
                ChangeOrCreateDevice(forceCreate: false);
            }
        }

        bool IGraphicsDeviceManager.BeginDraw()
        {
            if (GraphicsDevice is null)
                return false;

            beginDrawOk = false;

            if (!CheckDeviceState())
                return false;

            GraphicsDevice.Begin();

            // TODO: GRAPHICS REFACTOR
            //   Before drawing, we should clear the state to make sure that there is no unstable graphics device states (On some WP8 devices for example)
            //   An application should not rely on previous state (last frame...etc.) after BeginDraw.
            //GraphicsDevice.ClearState();
            //
            // By default, we setup the Render Target to the Back-Buffer, and the Viewport as well.
            //if (GraphicsDevice.BackBuffer is not null)
            //{
            //    GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            //}

            return beginDrawOk = true;

            bool CheckDeviceState()
            {
                const int SLEEP_TIME_WHEN_UNAVAILABLE = 20; // milliseconds

                switch (GraphicsDevice.GraphicsDeviceStatus)
                {
                    case GraphicsDeviceStatus.Removed:
                        Thread.Sleep(SLEEP_TIME_WHEN_UNAVAILABLE);
                        return false;

                    case GraphicsDeviceStatus.Reset:
                        Thread.Sleep(SLEEP_TIME_WHEN_UNAVAILABLE);
                        try
                        {
                            ChangeOrCreateDevice(forceCreate: true);
                        }
                        catch { return false; } // If we fail to reset the device, we return false
                        break;
                }

                return true;
            }
        }

        void IGraphicsDeviceManager.CreateDevice()
        {
            ChangeOrCreateDevice(forceCreate: true);
        }

        void IGraphicsDeviceManager.EndDraw(bool present)
        {
            if (beginDrawOk && GraphicsDevice is not null)
            {
                // If we should present, we need a GraphicsPresenter to call the Present method
                if (present && GraphicsDevice.Presenter is not null)
                {
                    try
                    {
                        GraphicsDevice.Presenter.Present();
                    }
                    catch (GraphicsException ex) when (ex.Status is not GraphicsDeviceStatus.Removed and not GraphicsDeviceStatus.Reset)
                    {
                        throw;
                    }
                    finally
                    {
                        EndDraw();
                    }
                }
                else
                {
                    EndDraw();
                }
            }

            void EndDraw()
            {
                beginDrawOk = false;
                GraphicsDevice.End();
            }
        }


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
            if (game is not null)
            {
                if (game.Services.GetService<IGraphicsDeviceService>() == this)
                {
                    game.Services.RemoveService<IGraphicsDeviceService>();
                }
                if (game.Services.GetService<IGraphicsDeviceManager>() == this)
                {
                    game.Services.RemoveService<IGraphicsDeviceManager>();
                }

                game.WindowCreated -= GameOnWindowCreated;
                if (game.Window is not null)
                {
                    game.Window.ClientSizeChanged -= Window_ClientSizeChanged;
                    game.Window.OrientationChanged -= Window_OrientationChanged;
                }
            }

            if (GraphicsDevice is not null)
            {
                if (GraphicsDevice.Presenter is not null)
                {
                    GraphicsDevice.Presenter.Dispose();
                    GraphicsDevice.Presenter = null;
                }

                //GraphicsDevice.DeviceResetting -= GraphicsDevice_DeviceResetting;
                //GraphicsDevice.DeviceReset -= GraphicsDevice_DeviceReset;
                //GraphicsDevice.DeviceLost -= GraphicsDevice_DeviceLost;

                GraphicsDevice.Dispose();
                GraphicsDevice.Disposing -= GraphicsDevice_Disposing;
                GraphicsDevice = null;
            }

            base.Destroy();
        }

        /// <summary>
        /// Determines whether this instance is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>.
        /// </summary>
        /// <param name="newDeviceInfo">The new device info.</param>
        /// <returns><c>true</c> if this instance this instance is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>; otherwise, <c>false</c>.</returns>
        protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
        {
            // By default, a reset is compatible when we stay under the same graphics profile
            return GraphicsDevice.Features.RequestedProfile == newDeviceInfo.GraphicsProfile;
        }

        /// <summary>
        /// Finds the best device that is compatible with the preferences defined in this instance.
        /// </summary>
        /// <param name="anySuitableDevice">if set to <c>true</c> a device can be selected from any existing adapters, otherwise, it will select only from default adapter.</param>
        /// <returns>The graphics device information.</returns>
        protected virtual GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)  // TODO: anySuitableDevice is not used, remove it?
        {
            // Setup preferred parameters before passing them to the factory
            var preferredParameters = new GameGraphicsParameters
            {
                ColorSpace = PreferredColorSpace,
                PreferredBackBufferWidth = PreferredBackBufferWidth,
                PreferredBackBufferHeight = PreferredBackBufferHeight,
                PreferredBackBufferFormat = PreferredBackBufferFormat,
                PreferredDepthStencilFormat = PreferredDepthStencilFormat,
                PreferredMultisampleCount = PreferredMultisampleCount,
                PreferredRefreshRate = PreferredRefreshRate,
                SynchronizeWithVerticalRetrace = SynchronizeWithVerticalRetrace,
                PreferredFullScreenOutputIndex = PreferredFullScreenOutputIndex,
                RequiredAdapterUid = RequiredAdapterUid,
                IsFullScreen = IsFullScreen,
                PreferredGraphicsProfile = (GraphicsProfile[]) PreferredGraphicsProfile.Clone()
            };

            // Remap to sRGB Back-Buffer if necessary
            preferredParameters.PreferredBackBufferFormat = PreferredColorSpace is ColorSpace.Linear
                // If the device support sRGB and linear color space, we use automatically a sRGB Back-Buffer
                ? preferredParameters.PreferredBackBufferFormat.ToSRgb()
                // If we are looking for gamma color space and the Back-Buffer format is sRGB, switch back to non-sRGB format
                : preferredParameters.PreferredBackBufferFormat.ToNonSRgb();

            // Setup resized value if there is a resize pending
            if (!IsFullScreen && isBackBufferToResize)
            {
                preferredParameters.PreferredBackBufferWidth = resizedBackBufferWidth;
                preferredParameters.PreferredBackBufferHeight = resizedBackBufferHeight;
            }

            var devices = graphicsDeviceFactory.FindBestDevices(preferredParameters);
            if (devices.Count == 0)
            {
                // Nothing was found; first, let's check if graphics profile was actually supported
                // NOTE: We don't do this preemptively because in some cases it seems to take lot of time
                //       (happened on a test machine, several seconds freeze on ID3D11Device.Release())
                if (!IsPreferredProfileAvailable(preferredParameters.PreferredGraphicsProfile, out var availableGraphicsProfile))
                {
                    var notSupportedProfiles = string.Join(", ", preferredParameters.PreferredGraphicsProfile);
                    throw new InvalidOperationException($"None of the graphics profiles [{notSupportedProfiles}] are supported by the Graphics Device. " +
                        $"The highest available profile is [{availableGraphicsProfile}].");
                }

                // Otherwise, there was just no screen mode
                throw new InvalidOperationException("No compatible screen mode found");
            }

            RankDevices(devices);

            if (devices.Count == 0)
            {
                throw new InvalidOperationException("No compatible screen modes found after ranking");
            }
            return devices[0];
        }

        /// <summary>
        /// Ranks a list of <see cref="GraphicsDeviceInformation"/> before creating a new device.
        /// </summary>
        /// <param name="foundDevices">The list of devices that can be reorder.</param>
        protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            // Don't sort if there is a single device (mostly for XAML/WP8)
            if (foundDevices.Count == 1)
                return;

            foundDevices.Sort(CompareDeviceInformations);

            int CompareDeviceInformations(GraphicsDeviceInformation left, GraphicsDeviceInformation right)
            {
                var leftParams = left.PresentationParameters;
                var rightParams = right.PresentationParameters;

                // Sort by GraphicsProfile
                if (left.GraphicsProfile != right.GraphicsProfile)
                {
                    return left.GraphicsProfile <= right.GraphicsProfile ? 1 : -1;
                }

                // Sort by full-screen mode
                if (leftParams.IsFullScreen != rightParams.IsFullScreen)
                {
                    return IsFullScreen != leftParams.IsFullScreen ? 1 : -1;
                }

                // Sort by Back-Buffer format
                int leftFormat = CalculateRankForFormat(leftParams.BackBufferFormat);
                int rightFormat = CalculateRankForFormat(rightParams.BackBufferFormat);
                if (leftFormat != rightFormat)
                {
                    return leftFormat >= rightFormat ? 1 : -1;
                }

                // Sort by multisample count
                if (leftParams.MultisampleCount != rightParams.MultisampleCount)
                {
                    return leftParams.MultisampleCount <= rightParams.MultisampleCount ? 1 : -1;
                }

                // Sort by aspect ratio (width / height)
                var targetAspectRatio = (PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0)
                    ? (float) DefaultBackBufferWidth / DefaultBackBufferHeight
                    : (float) PreferredBackBufferWidth / PreferredBackBufferHeight;

                var leftDiffRatio = Math.Abs(((float) leftParams.BackBufferWidth / leftParams.BackBufferHeight) - targetAspectRatio);
                var rightDiffRatio = Math.Abs(((float) rightParams.BackBufferWidth / rightParams.BackBufferHeight) - targetAspectRatio);

                if (Math.Abs(leftDiffRatio - rightDiffRatio) > 0.2f)
                {
                    return leftDiffRatio >= rightDiffRatio ? 1 : -1;
                }

                // Sort by pixel count
                var leftAdapter = left.Adapter;
                var rightAdapter = right.Adapter;

                var (leftPixelCount, rightPixelCount) = CalculatePixelCount(leftAdapter, rightAdapter);

                int leftDeltaPixelCount = Math.Abs((leftParams.BackBufferWidth * leftParams.BackBufferHeight) - leftPixelCount);
                int rightDeltaPixelCount = Math.Abs((rightParams.BackBufferWidth * rightParams.BackBufferHeight) - rightPixelCount);
                if (leftDeltaPixelCount != rightDeltaPixelCount)
                {
                    return leftDeltaPixelCount >= rightDeltaPixelCount ? 1 : -1;
                }

                // Sort by Graphics Adapter
                if (left.Adapter != right.Adapter)
                {
                    if (left.Adapter.IsDefaultAdapter) return -1;
                    if (right.Adapter.IsDefaultAdapter) return 1;
                }

                return 0;  // If all criteria are equal, consider them equal
            }

            int CalculateRankForFormat(PixelFormat format)
            {
                if (format == PreferredBackBufferFormat)
                {
                    return 0;  // The preferred format is the best
                }
                if (CalculateFormatSizeInBits(format) == CalculateFormatSizeInBits(PreferredBackBufferFormat))
                {
                    return 1;  // The format size matches the preferred format size, so it's the second best
                }
                return int.MaxValue;  // All other formats are ranked lower
            }

            int CalculateFormatSizeInBits(PixelFormat format)
            {
                return format switch
                {
                    PixelFormat.R16G16B16A16_Float => 64,

                    PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb or
                    PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb or
                    PixelFormat.R10G10B10A2_UNorm => 32,

                    PixelFormat.B5G6R5_UNorm or PixelFormat.B5G5R5A1_UNorm => 16,

                    _ => 0,
                };
            }

            (int leftPixelCount, int rightPixelCount) CalculatePixelCount(GraphicsAdapter leftAdapter, GraphicsAdapter rightAdapter)
            {
                int leftPixelCount, rightPixelCount;

                if (IsFullScreen)
                {
                    if (((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0)) &&
                        PreferredFullScreenOutputIndex < leftAdapter.Outputs.Length &&
                        PreferredFullScreenOutputIndex < rightAdapter.Outputs.Length)
                    {
                        // Assume we got here only adapters that have the needed number of outputs:
                        var leftOutput = leftAdapter.Outputs[PreferredFullScreenOutputIndex].CurrentDisplayMode ?? default;
                        var rightOutput = rightAdapter.Outputs[PreferredFullScreenOutputIndex].CurrentDisplayMode ?? default;

                        leftPixelCount = leftOutput.Width * leftOutput.Height;
                        rightPixelCount = rightOutput.Width * rightOutput.Height;
                    }
                    else leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                }
                else if (PreferredBackBufferWidth == 0 || PreferredBackBufferHeight == 0)
                {
                    leftPixelCount = rightPixelCount = DefaultBackBufferWidth * DefaultBackBufferHeight;
                }
                else leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;

                return (leftPixelCount, rightPixelCount);
            }
        }

        protected virtual bool IsPreferredProfileAvailable(GraphicsProfile[] preferredProfiles, out GraphicsProfile availableProfile)
        {
            availableProfile = Level_9_1;  // Start from the lowest profile

            var graphicsProfiles = Enum.GetValues<GraphicsProfile>();

            // Find the highest available profile that is supported by any of the adapters
            foreach (var graphicsAdapter in GraphicsAdapterFactory.Adapters)
            {
                foreach (var graphicsProfile in graphicsProfiles)
                {
                    if (graphicsProfile > availableProfile && graphicsAdapter.IsProfileSupported(graphicsProfile))
                        availableProfile = graphicsProfile;
                }
            }
            // Check if the available profile meets any of the preferred profiles
            foreach (var preferredProfile in preferredProfiles)
            {
                if (availableProfile >= preferredProfile)
                    return true;
            }
            // No preferred profile is available
            return false;
        }


        protected virtual void OnDeviceCreated(object sender, EventArgs args)
        {
            DeviceCreated?.Invoke(sender, args);
        }

        protected virtual void OnDeviceDisposing(object sender, EventArgs args)
        {
            DeviceDisposing?.Invoke(sender, args);
        }

        protected virtual void OnDeviceReset(object sender, EventArgs args)
        {
            DeviceReset?.Invoke(sender, args);
        }

        protected virtual void OnDeviceResetting(object sender, EventArgs args)
        {
            DeviceResetting?.Invoke(sender, args);
        }

        protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
            PreparingDeviceSettings?.Invoke(sender, args);
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // Ignore changes while we are changing the device
            if (isChangingDevice)
                return;

            if (game.Window.ClientBounds.Height != 0 || game.Window.ClientBounds.Width != 0)
            {
                resizedBackBufferWidth = game.Window.ClientBounds.Width;
                resizedBackBufferHeight = game.Window.ClientBounds.Height;
                isBackBufferToResize = true;

                if (GraphicsDevice is not null)
                {
                    ChangeOrCreateDevice(forceCreate: false);
                }
            }
        }

        private void Window_OrientationChanged(object sender, EventArgs e)
        {
            // Ignore changes while we are changing the device
            if (isChangingDevice)
                return;

            if ((game.Window.ClientBounds.Height != 0 || game.Window.ClientBounds.Width != 0) &&
                game.Window.CurrentOrientation != currentWindowOrientation)
            {
                if ((game.Window.ClientBounds.Height > game.Window.ClientBounds.Width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                    (game.Window.ClientBounds.Width > game.Window.ClientBounds.Height && preferredBackBufferHeight > preferredBackBufferWidth))
                {
                    // Client size and Back-Buffer size are different things
                    // In this case all we care is if orientation changed, if so we swap width and height
                    (PreferredBackBufferHeight, PreferredBackBufferWidth) = (PreferredBackBufferWidth, PreferredBackBufferHeight);

                    ApplyChanges();
                }
            }
        }

        private void Window_FullscreenChanged(object sender, EventArgs eventArgs)
        {
            if (sender is GameWindow window)
            {
                // The new state is the Window's fullscreen state
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

        private void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        private void GraphicsDevice_DeviceLost(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        private void GraphicsDevice_Disposing(object sender, EventArgs e)
        {
            OnDeviceDisposing(sender, e);
        }

        private void ChangeOrCreateDevice(bool forceCreate)
        {
            // We make sure that we won't be call by an asynchronous event (windows resized)
            lock (lockDeviceCreation)
            {
                using (Profiler.Begin(GraphicsDeviceManagerProfilingKeys.CreateDevice))
                {
                    ChangeOrCreateDevice();
                }
            }

            void ChangeOrCreateDevice()
            {
                game.ConfirmRenderingSettings(GraphicsDevice is null); // If no device we assume we are still at game creation phase

                isChangingDevice = true;
                var width = game.Window.ClientBounds.Width;
                var height = game.Window.ClientBounds.Height;

                // If the orientation is free to be changed from portrait to landscape we actually need this check now,
                // it is mostly useful only at initialization because `Window_OrientationChanged` does the same logic on runtime change
                if (game.Window.CurrentOrientation != currentWindowOrientation)
                {
                    if ((game.Window.ClientBounds.Height > game.Window.ClientBounds.Width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                        (game.Window.ClientBounds.Width > game.Window.ClientBounds.Height && preferredBackBufferHeight > preferredBackBufferWidth))
                    {
                        // Client size and Back-Buffer size are different things
                        // In this case all we care is if orientation changed, if so we swap width and height
                        (preferredBackBufferHeight, preferredBackBufferWidth) = (preferredBackBufferWidth, preferredBackBufferHeight);
                    }
                }

                var isBeginScreenDeviceChange = false;
                try
                {
                    // Notifies the game Window the new orientation
                    var orientation = SelectOrientation(supportedOrientations, PreferredBackBufferWidth, PreferredBackBufferHeight, allowLandscapeLeftAndRight: true);
                    game.Window.SetSupportedOrientations(orientation);

                    // Find the best device configuration based on the current settings
                    var graphicsDeviceInformation = FindBestDevice(forceCreate);
                    // Give a chance to the game to modify the device settings before the device is created or reset
                    OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(graphicsDeviceInformation));

                    isFullScreen = graphicsDeviceInformation.PresentationParameters.IsFullScreen;
                    game.Window.BeginScreenDeviceChange(graphicsDeviceInformation.PresentationParameters.IsFullScreen);
                    isBeginScreenDeviceChange = true;
                    bool needToCreateNewDevice = true;

                    // If we are not forced to create a new device and this is already an existing GraphicsDevice
                    // try to reset and resize it
                    if (!forceCreate && GraphicsDevice is not null)
                    {
                        if (CanResetDevice(graphicsDeviceInformation))
                        {
                            try
                            {
                                GraphicsDevice.ColorSpace = graphicsDeviceInformation.PresentationParameters.ColorSpace;
                                var newWidth = graphicsDeviceInformation.PresentationParameters.BackBufferWidth;
                                var newHeight = graphicsDeviceInformation.PresentationParameters.BackBufferHeight;
                                var newFormat = graphicsDeviceInformation.PresentationParameters.BackBufferFormat;
                                var newOutputIndex = graphicsDeviceInformation.PresentationParameters.PreferredFullScreenOutputIndex;

                                GraphicsDevice.Presenter.Description.PreferredFullScreenOutputIndex = newOutputIndex;
                                GraphicsDevice.Presenter.Description.RefreshRate = graphicsDeviceInformation.PresentationParameters.RefreshRate;

                                GraphicsDevice.Presenter.Resize(newWidth, newHeight, newFormat);

                                // Change full screen if needed
                                GraphicsDevice.Presenter.IsFullScreen = graphicsDeviceInformation.PresentationParameters.IsFullScreen;

                                needToCreateNewDevice = false;
                            }
                            catch { /* Ignore any exception */ }
                        }
                    }

                    // If we still need to create a device, then we need to create it
                    if (needToCreateNewDevice)
                    {
                        CreateDevice(graphicsDeviceInformation);
                    }

                    if (GraphicsDevice is null)
                        throw new InvalidOperationException("Unexpected null GraphicsDevice");

                    var presentationParameters = GraphicsDevice.Presenter.Description;
                    isReallyFullScreen = presentationParameters.IsFullScreen;

                    if (presentationParameters.BackBufferWidth != 0)
                    {
                        width = presentationParameters.BackBufferWidth;
                    }
                    if (presentationParameters.BackBufferHeight != 0)
                    {
                        height = presentationParameters.BackBufferHeight;
                    }
                    deviceSettingsChanged = false;
                }
                finally
                {
                    // Notify the game Window that the screen device change is over
                    if (isBeginScreenDeviceChange)
                    {
                        game.Window.EndScreenDeviceChange(width, height);
                        game.Window.SetIsReallyFullscreen(isReallyFullScreen);
                    }

                    currentWindowOrientation = game.Window.CurrentOrientation;
                    isChangingDevice = false;
                }
            }

            void CreateDevice(GraphicsDeviceInformation newInfo)
            {
                newInfo.PresentationParameters.IsFullScreen = isFullScreen;
                newInfo.PresentationParameters.PresentationInterval = SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate;
                newInfo.DeviceCreationFlags = DeviceCreationFlags;

                // this.ValidateGraphicsDeviceInformation(newInfo);

                bool recreateDevice = GraphicsDevice is not null;

                // Notify device is resetting (usually this should result in Graphics Resources being destroyed)
                if (recreateDevice)
                    OnDeviceResetting(this, EventArgs.Empty);

                // Create (or recreate) the graphics device
                GraphicsDevice = graphicsDeviceFactory.ChangeOrCreateDevice(GraphicsDevice, newInfo);

                // Notify device is reset (usually this should result in Graphics Resources being recreated / reloaded)
                if (recreateDevice)
                    OnDeviceReset(this, EventArgs.Empty);

                // Use the Shader profile returned by the GraphicsDeviceInformation otherwise use the one coming from the GameSettings
                // TODO: Stale comment?
                GraphicsDevice.ShaderProfile = ShaderProfile;

                // TODO: HANDLE Device Resetting/Reset/Lost
                //GraphicsDevice.DeviceResetting += GraphicsDevice_DeviceResetting;
                //GraphicsDevice.DeviceReset += GraphicsDevice_DeviceReset;
                //GraphicsDevice.DeviceLost += GraphicsDevice_DeviceLost;

                if (!recreateDevice)
                    GraphicsDevice.Disposing += GraphicsDevice_Disposing;

                OnDeviceCreated(this, EventArgs.Empty);
            }
        }
    }
}
