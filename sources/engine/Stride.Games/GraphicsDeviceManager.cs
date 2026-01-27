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
    ///   Manages the <see cref="GraphicsDevice"/> life-cycle, providing access to it and events that can be
    ///   subscribed to be notified of when the device is created, reset, or disposed.
    /// </summary>
    public class GraphicsDeviceManager : ComponentBase, IGraphicsDeviceManager, IGraphicsDeviceService
    {
        // Switch to indicate if window events should be delayed until the beginning of frames,
        // just before running the Game.Window.RunCallback
        private const bool DelayWindowEvents = true;

        /// <summary>
        ///   Default width for the Back-Buffer.
        /// </summary>
        public static readonly int DefaultBackBufferWidth = 1280;
        /// <summary>
        ///   Default height for the Back-Buffer.
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

        private bool hasWindowClientSizeChanged;
        private bool hasWindowOrientationChanged;
        private bool hasWindowFullscreenChanged;

        private bool isChangingDevice;

        private int resizedBackBufferWidth;
        private int resizedBackBufferHeight;
        private bool isBackBufferToResize = false;

        private DisplayOrientation currentWindowOrientation;

        private bool beginDrawOk;

        private readonly IGraphicsDeviceFactory graphicsDeviceFactory;


        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDeviceManager"/> class.
        /// </summary>
        /// <param name="game">The Game that needs to manage the Graphics Device.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="game"/> cannot be <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        ///   Could not get the required <see cref="IGraphicsDeviceFactory"/> service from the <paramref name="game"/>'s services.
        /// </exception>
        internal GraphicsDeviceManager(GameBase game)
        {
            ArgumentNullException.ThrowIfNull(game);

            this.game = game;

            lockDeviceCreation = new object();

            graphicsDeviceFactory = game.Services.GetService<IGraphicsDeviceFactory>()
                ?? throw new InvalidOperationException("IGraphicsDeviceFactory is not registered as a service");

            game.WindowCreated += GameOnWindowCreated;
        }


        /// <summary>
        ///   Occurs when a new Graphics Device is successfully created.
        /// </summary>
        public event EventHandler<EventArgs> DeviceCreated;
        /// <summary>
        ///   Occurs when the Graphics Device is being disposed.
        /// </summary>
        public event EventHandler<EventArgs> DeviceDisposing;

        /// <summary>
        ///   Occurs when the Graphics Device is being reset.
        /// </summary>
        public event EventHandler<EventArgs> DeviceReset;
        /// <summary>
        ///   Occurs when the Graphics Device is being reset, but before it is actually reset.
        /// </summary>
        public event EventHandler<EventArgs> DeviceResetting;

        /// <summary>
        ///   Occurs when the Graphics Device is being initialized to give a chance to the application at
        ///   adjusting the final settings for the device creation.
        /// </summary>
        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

        /// <summary>
        ///   Method called when the game window is created. It subscribes to the resizing and orientation events.
        /// </summary>
        private void GameOnWindowCreated(object sender, EventArgs eventArgs)
        {
            // Place ourselves first (in case drawing/present is after, we better rebuild device before that if necessary)
            if (DelayWindowEvents)
                game.Window.RunCallback = Window_ProcessEventsDelayed + game.Window.RunCallback;

            game.Window.ClientSizeChanged += Window_ClientSizeChanged;
            game.Window.OrientationChanged += Window_OrientationChanged;
            game.Window.FullscreenChanged += Window_FullscreenChanged;
        }


        /// <summary>
        ///   Gets the Graphics Device associated with this manager.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; internal set; }

        /// <summary>
        ///   Gets or sets the graphics profiles that are going to be tested when initializing the Graphics Device,
        ///   in order of preference.
        /// </summary>
        /// <remarks>
        ///   By default, the preferred graphics profiles are, in order of preference, better first:
        ///   <see cref="Level_11_1"/>, <see cref="Level_11_0"/>, <see cref="Level_10_1"/>, <see cref="Level_10_0"/>,
        ///   <see cref="Level_9_3"/>, <see cref="Level_9_2"/>, and <see cref="Level_9_1"/>.
        /// </remarks>
        public GraphicsProfile[] PreferredGraphicsProfile { get; set; } = [ Level_11_1, Level_11_0, Level_10_1, Level_10_0, Level_9_3, Level_9_2, Level_9_1 ];

        /// <summary>
        ///   Gets or sets the Shader graphics profile that will be used to compile shaders.
        /// </summary>
        /// <remarks>
        ///   If this property is not set, the profile used to compile Shaders will be taken from the Graphics Device.
        ///   based on the list provided by <see cref="PreferredGraphicsProfile"/>.
        /// </remarks>
        public GraphicsProfile? ShaderProfile { get; set; }

        /// <summary>
        ///   Gets or sets the device creation flags that will be used to create the Graphics Device.
        /// </summary>
        public DeviceCreationFlags DeviceCreationFlags { get; set; }

        /// <summary>
        ///   Gets or sets the unique identifier of the Graphis Adapter that should be used to create the Graphics Device.
        /// </summary>
        /// <value>
        ///   If this property is set to a non-<see langword="null"/> <see langword="string"/> the engine will try to
        ///   initialize the Graphics Device to present to an adapter with the same unique Id
        /// </value>
        public string RequiredAdapterUid { get; set; }

        /// <summary>
        ///   Gets or sets the preferred color space for the Back-Buffers.
        /// </summary>
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
        ///   Gets or sets a value indicating whether the Graphics Device should present in full-screen mode.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the device should render is full screen;
        ///   otherwise, <see langword="false"/>.
        /// </value>
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
        ///   Gets or sets the level of multisampling for the Back-Buffers.
        /// </summary>
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
        ///   Gets or sets the preferred pixel format for the Back-Buffers.
        /// </summary>
        /// <remarks>
        ///   Not all pixel formats are supported for Back-Buffers by all Graphics Devices. Typical formats
        ///   are <see cref="PixelFormat.R8G8B8A8_UNorm"/> or <see cref="PixelFormat.B8G8R8A8_UNorm"/>,
        ///   although there are some more advanced formats depending on the Graphics Device capabilities
        ///   and intended use (e.g. <see cref="PixelFormat.R16G16B16A16_Float"/> for HDR rendering).
        /// </remarks>
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
        ///   Gets or sets the preferred height of the Back-Buffer, in pixels.
        /// </summary>
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
        ///   Gets or sets the preferred width of the Back-Buffer, in pixels.
        /// </summary>
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
        ///   Gets or sets the preferred format for the Depth-Stencil Buffer.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Not all formats are supported for Depth-Stencil Buffers by all Graphics Devices.
        ///     Typical formats are <see cref="PixelFormat.D24_UNorm_S8_UInt"/> or <see cref="PixelFormat.D32_Float_S8X24_UInt"/>.
        ///   </para>
        ///   <para>
        ///     The format also determines the number of bits used for the depth and stencil buffers. For example.
        ///     the <see cref="PixelFormat.D24_UNorm_S8_UInt"/> format uses 24 bits for the Depth-Buffer and 8 bits for the Stencil-Buffer,
        ///     while <see cref="PixelFormat.D32_Float"/> uses 32 bits for the Depth-Buffer and no Stencil-Buffer.
        ///   </para>
        /// </remarks>
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
        ///   Gets or sets the preferred refresh rate, in hertz (number of frames per second).
        /// </summary>
        /// <value>
        ///   The preferred refresh rate as a <see cref="Rational"/> value, where the numerator is the number of frames per second
        ///   and the denominator is usually 1 (e.g., 60 frames per second is represented as <c>60 / 1</c>). However, some adapters
        ///   may support fractional refresh rates, such as 59.94 Hz, which would be represented as <c>5994 / 100</c>.
        /// </value>
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
        ///   Gets or sets the preferred output (monitor) index to use when switching to fullscreen mode.
        ///   Doesn't have any effect when windowed mode is used.
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
        ///   Gets or sets the supported orientations for displaying the Back-Buffers.
        /// </summary>
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
        ///   Gets or sets a value indicating whether the Graphics Device should synchronize with the vertical retrace,
        ///   commonly known as <strong>VSync</strong>.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> to synchronize with the vertical retrace, which can help prevent screen tearing
        ///   and ensure smoother animations by waiting for the monitor to finish displaying the current frame;
        ///   <see langword="false"/> to not synchronize, which may result in faster frame rates but can lead to screen tearing.
        /// </value>
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
        ///   Applies the changes in the graphics settings and changes or recreates the <see cref="GraphicsDevice"/>
        ///   according to the new values.
        ///   <br/>
        ///   Does not have any effect if the <see cref="GraphicsDevice"/> is <see langword="null"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the Graphics Device could not be created or reconfigured.
        /// </exception>
        public void ApplyChanges()
        {
            if (GraphicsDevice is not null && deviceSettingsChanged)
            {
                ChangeOrCreateDevice(forceCreate: false);
            }
        }


        /// <inheritdoc/>
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

            //
            // Checks the current state of the Graphics Device and handles any necessary actions.
            //
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

        /// <inheritdoc/>
        void IGraphicsDeviceManager.CreateDevice()
        {
            ChangeOrCreateDevice(forceCreate: true);
        }

        /// <inheritdoc/>
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
                    catch (GraphicsDeviceException ex) when (ex.Status is not GraphicsDeviceStatus.Removed and not GraphicsDeviceStatus.Reset)
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

            //
            // Ends the current drawing.
            //
            void EndDraw()
            {
                beginDrawOk = false;
                GraphicsDevice.End();
            }
        }


        /// <summary>
        ///   Determines the appropriate display orientation based on the specified parameters.
        /// </summary>
        /// <param name="orientation">
        ///   The desired display orientation. If set to <see cref="DisplayOrientation.Default"/>, the orientation will be
        ///   determined based on the provided dimensions and landscape allowance.
        /// </param>
        /// <param name="width">The width of the display area, in pixels.</param>
        /// <param name="height">The height of the display area, in pixels.</param>
        /// <param name="allowLandscapeLeftAndRight">
        ///   A value indicating whether both landscape orientations (<see cref="DisplayOrientation.LandscapeLeft"/> and
        ///   <see cref="DisplayOrientation.LandscapeRight"/>) are allowed.
        /// </param>
        /// <returns>
        ///   The selected <see cref="DisplayOrientation"/> based on the input parameters.
        ///   <list type="bullet">
        ///     <item>Returns <see cref="DisplayOrientation.Portrait"/> if the height is greater than or equal to the width.</item>
        ///     <item>If landscape is allowed, returns a combination of <see cref="DisplayOrientation.LandscapeLeft"/> and
        ///           <see cref="DisplayOrientation.LandscapeRight"/>.</item>
        ///     <item>Otherwise, returns <see cref="DisplayOrientation.LandscapeRight"/>.</item>
        ///   </list>
        /// </returns>
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


        /// <inheritdoc/>
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
        ///   Determines whether the Graphics Device is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>
        ///   and can be reset with it.
        /// </summary>
        /// <param name="newDeviceInfo">The new device information to check compatibility with.</param>
        /// <returns>
        ///   <see langword="true"/> if the Graphics Device is compatible with <paramref name="newDeviceInfo"/> and can be
        ///   reinitialized with the new settings;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
        {
            // By default, a reset is compatible when we stay under the same graphics profile
            return GraphicsDevice.Features.RequestedProfile == newDeviceInfo.GraphicsProfile;
        }

        /// <summary>
        ///   Finds the best Graphics Device configuration that is compatible with the set preferences.
        /// </summary>
        /// <param name="anySuitableDevice">
        ///   A value indicating whether to search for any suitable device on any of the available adapters (<see langword="true"/>)
        ///   or only from the default adapter (<see langword="false"/>).
        /// </param>
        /// <returns>The graphics device information.</returns>
        /// <exception cref="InvalidOperationException">
        ///   None of the graphics profiles specified in <see cref="PreferredGraphicsProfile"/> are supported.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   A compatible screen mode could not be found based on the current settings. Check the full-screen mode,
        ///   the preferred Back-Buffer width and height, and the preferred Back-Buffer format.
        /// </exception>
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
        ///   Ranks a list of <see cref="GraphicsDeviceInformation"/>s before creating a new Graphics Device.
        /// </summary>
        /// <param name="foundDevices">A list of possible device configurations to be ranked and reordered.</param>
        protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            // Don't sort if there is a single device (mostly for XAML/UWP)
            if (foundDevices.Count == 1)
                return;

            foundDevices.Sort(CompareDeviceInformations);

            //
            // Compares two `GraphicsDeviceInformation` instances to determine their ranking.
            //
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

            //
            // Calculates the rank for a given pixel format.
            //
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

            //
            // Calculates the size in bits of a given pixel format.
            //
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

            //
            // Calculates the pixel count for the left and right Graphiccs Adapters based on their current display modes.
            //
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

        /// <summary>
        ///   Determines whether any of the preferred graphics profiles is available on the system.
        /// </summary>
        /// <param name="preferredProfiles">
        ///   An array of preferred graphics profiles to check for availability. The profiles should be ordered by
        ///   preference, with the most preferred profile first.
        /// </param>
        /// <param name="availableProfile">
        ///   When the method returns, contains the highest graphics profile supported by the system that meets or exceeds
        ///   the preferences specified in <paramref name="preferredProfiles"/>.
        ///   If no preferred profile is available, this will contain the lowest supported graphics profile.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if a graphics profile that meets or exceeds one of the preferred profiles is
        ///   available;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
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


        /// <summary>
        ///   Method called when the Graphics Device is created.
        ///   Invokes the <see cref="DeviceCreated"/> event.
        /// </summary>
        protected virtual void OnDeviceCreated(object sender, EventArgs args)
        {
            DeviceCreated?.Invoke(sender, args);
        }

        /// <summary>
        ///   Method called when the Graphics Device is about to be disposed.
        ///   Invokes the <see cref="DeviceDisposing"/> event.
        /// </summary>
        protected virtual void OnDeviceDisposing(object sender, EventArgs args)
        {
            DeviceDisposing?.Invoke(sender, args);
        }

        /// <summary>
        ///   Method called when the Graphics Device is reset.
        ///   Invokes the <see cref="DeviceReset"/> event.
        /// </summary>
        protected virtual void OnDeviceReset(object sender, EventArgs args)
        {
            DeviceReset?.Invoke(sender, args);
        }

        /// <summary>
        ///   Method called when the Graphics Device is resetting, but before it is actually reset.
        ///   Invokes the <see cref="DeviceResetting"/> event.
        /// </summary>
        protected virtual void OnDeviceResetting(object sender, EventArgs args)
        {
            DeviceResetting?.Invoke(sender, args);
        }

        /// <summary>
        ///   Method called when the Graphics Device is preparing to be created or reset, so the application can
        ///   examine or modify the device settings before the device is created or reset.
        ///   Invokes the <see cref="PreparingDeviceSettings"/> event.
        /// </summary>
        protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
            PreparingDeviceSettings?.Invoke(sender, args);
        }

        /// <summary>
        ///   Processes windows events that require the Graphics Device to be recreated.
        /// </summary>
        private void Window_ProcessEventsDelayed()
        {
            // When embedding a Form / Game in the Editor, various events such as WM_SIZE
            // might be forwarded by InputSourceWinforms.
            //
            // Since this is done using CallWindowProc, those events might be raised outside of the usual
            // Translate / Peek message loop and happen during an unfortunate wait or I/O call during rendering
            // (i.e. waiting on Shader compilation).
            //
            // This is a big no no if we try to resize a Swap Chain in the middle of rendering!
            // So we delay their process during Run() callback (which is before/after Swap Chain Present).
            bool needApplyChanges = false;

            if (hasWindowClientSizeChanged)
            {
                hasWindowClientSizeChanged = false;
                needApplyChanges |= ProcessClientSizeChanged();
            }
            if (hasWindowOrientationChanged)
            {
                hasWindowOrientationChanged = false;
                needApplyChanges |= ProcessOrientationChanged();
            }
            if (hasWindowFullscreenChanged)
            {
                hasWindowFullscreenChanged = false;
                needApplyChanges |= ProcessOrientationChanged();
            }

            if (needApplyChanges)
                ApplyChanges();
        }

        /// <summary>
        ///   Method called when the Window's client size changes, which may require a device reinitialization
        ///   with a new Back-Buffer size.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the Graphics Device could not be created or reconfigured.
        /// </exception>
        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // Ignore changes while we are changing the device
            if (isChangingDevice)
                return;

            if (DelayWindowEvents)
                hasWindowClientSizeChanged = true;

            else if (ProcessClientSizeChanged())
                ApplyChanges();
        }

        /// <summary>
        ///   Method called when the Window's client size changes,
        ///   which may require to reinitialize the Graphics Device with a new Back-Buffer size.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the client size change requires a reinitialization of the Graphics Device;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        private bool ProcessClientSizeChanged()
        {
            // The client size can be zero in some cases (minimized window...)
            // We only process it when we have a valid size
            if (game.Window.ClientBounds.Height != 0 || game.Window.ClientBounds.Width != 0)
            {
                resizedBackBufferWidth = game.Window.ClientBounds.Width;
                resizedBackBufferHeight = game.Window.ClientBounds.Height;
                isBackBufferToResize = true;
                deviceSettingsChanged = true;

                if (GraphicsDevice is not null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Method called when the Window's orientation changes, which may require a device reinitialization
        ///   with a new Back-Buffer size if the orientation allows it.
        /// </summary>
        private void Window_OrientationChanged(object sender, EventArgs e)
        {
            // Ignore changes while we are changing the device
            if (isChangingDevice)
                return;

            if (DelayWindowEvents)
                hasWindowOrientationChanged = true;

            else if (ProcessOrientationChanged())
                ApplyChanges();
        }

        /// <summary>
        ///   Method called when the Window's orientation changes,
        ///   which may require to reinitialize the Graphics Device with a new Back-Buffer size.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the orientation change requires a reinitialization of the Graphics Device;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        private bool ProcessOrientationChanged()
        {
            // The client size can be zero in some cases (minimized window...)
            // We only process it when we have a valid size, and the orientation actually changed
            if ((game.Window.ClientBounds.Height != 0 || game.Window.ClientBounds.Width != 0) &&
                game.Window.CurrentOrientation != currentWindowOrientation)
            {
                if ((game.Window.ClientBounds.Height > game.Window.ClientBounds.Width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                    (game.Window.ClientBounds.Width > game.Window.ClientBounds.Height && preferredBackBufferHeight > preferredBackBufferWidth))
                {
                    // Client size and Back-Buffer size are different things
                    // In this case all we care is if orientation changed, if so we swap width and height
                    (PreferredBackBufferHeight, PreferredBackBufferWidth) = (PreferredBackBufferWidth, PreferredBackBufferHeight);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Method called when the Window's full-screen state changes, whick may require a resize of the Back-Buffer.
        /// </summary>
        private void Window_FullscreenChanged(object sender, EventArgs eventArgs)
        {
            if (sender is GameWindow window)
            {
                if (DelayWindowEvents)
                    hasWindowFullscreenChanged = true;
                else
                {
                    ProcessFullscreenChanged(window);
                    ApplyChanges();
                }
            }
        }

        /// <summary>
        ///   Method called when the Window's full-screen state changes,
        ///   which may require to reinitialize the Graphics Device with a new Back-Buffer size.
        /// </summary>
        private void ProcessFullscreenChanged(GameWindow window)
        {
            // The new state is the Window's full-screen state
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
        }

        /// <summary>
        ///   Method called when the Graphics Device is being reset, but before it is actually reset.
        /// </summary>
        private void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            // TODO: What to do?
        }

        /// <summary>
        ///   Method called when the Graphics Device has been reset.
        /// </summary>
        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            // TODO: What to do?
        }

        /// <summary>
        ///   Method called when the Graphics Device has been lost, meaning it is currently unavailable
        /// </summary>
        private void GraphicsDevice_DeviceLost(object sender, EventArgs e)
        {
            // TODO: What to do?
        }

        /// <summary>
        ///   Method called when the Graphics Device is being disposed.
        /// </summary>
        private void GraphicsDevice_Disposing(object sender, EventArgs e)
        {
            OnDeviceDisposing(sender, e);
        }


        /// <summary>
        ///   Changes or creates the Graphics Device based on the current settings.
        /// </summary>
        /// <param name="forceCreate">
        ///   A value indicating whether the Graphics Device should be forcibly recreated.
        ///   If <see langword="true"/>, a new Graphics Device will be created regardless of the current state.
        ///   If <see langword="false"/>, the method will attempt to reset and reuse the existing device if possible.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   Thrown if the Graphics Device could not be created or is unexpectedly <see langword="null"/> after the operation.
        /// </exception>
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

            //
            // Changes or creates the Graphics Device based on the current settings.
            //
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

            //
            // Creates a new Graphics Device based on the provided `GraphicsDeviceInformation`.
            //
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
