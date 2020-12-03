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
using System.Linq;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    /// Manages the <see cref="GraphicsDevice"/> lifecycle.
    /// </summary>
    public class GraphicsDeviceManager : ComponentBase, IGraphicsDeviceManager, IGraphicsDeviceService
    {
        public GraphicsPresenter CreatePresenter(PresentationParameters presentationParameters)
        {
#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_PLATFORM_UWP
            if (game.Context is GameContextUWPCoreWindow context && context.IsWindowsMixedReality)
            {
                return new WindowsMixedRealityGraphicsPresenter(GraphicsDevice, presentationParameters);
            }
            else
#endif
            {
                return new SwapChainGraphicsPresenter(GraphicsDevice, presentationParameters);
            }
        }

        public List<PresentationParameters> FindBestScreenModes(PresentationParameters preferredParameter)
        {
            var preferredParameters = new GameGraphicsParameters
            {
                PreferredBackBufferWidth = preferredParameter.BackBufferWidth,
                PreferredBackBufferHeight = preferredParameter.BackBufferHeight,
                PreferredBackBufferFormat = preferredParameter.BackBufferFormat,
                PreferredDepthStencilFormat = preferredParameter.DepthStencilFormat,
                PreferredRefreshRate = preferredParameter.RefreshRate,
                PreferredFullScreenOutputIndex = preferredParameter.PreferredFullScreenOutputIndex,
                IsFullScreen = preferredParameter.IsFullScreen,
                PreferredMultisampleCount = preferredParameter.MultisampleCount,
                SynchronizeWithVerticalRetrace = preferredParameter.PresentationInterval != PresentInterval.Immediate,
                PreferredGraphicsProfile = (GraphicsProfile[])PreferredGraphicsProfile.Clone(),
                ColorSpace = preferredParameter.ColorSpace,
                RequiredAdapterUid = RequiredAdapterUid,
            };
            var devices = graphicsDeviceFactory.FindBestDevices(preferredParameters);
            return devices.Select(device => device.PresentationParameters).ToList();
        }

        #region Fields

        private readonly object lockDeviceCreation;

        private readonly GameBase game;

        private readonly GameWindowManager windowManager;

        private bool deviceSettingsChanged;

        private bool beginDrawOk;

        private readonly IGraphicsDeviceFactory graphicsDeviceFactory;

        private string requiredAdapterUid;

        private DeviceCreationFlags deviceCreationFlags;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDeviceManager" /> class.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <exception cref="System.ArgumentNullException">The game instance cannot be null.</exception>
        internal GraphicsDeviceManager(GameBase game)
        {
            this.game = game ?? throw new ArgumentNullException(nameof(game));

            windowManager = new GameWindowManager();

            lockDeviceCreation = new object();

            // Defines all default values
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

            graphicsDeviceFactory = game.Services.GetService<IGraphicsDeviceFactory>();
            if (graphicsDeviceFactory == null)
            {
                throw new InvalidOperationException("IGraphicsDeviceFactory is not registered as a service");
            }

            game.WindowCreated += GameOnWindowCreated;
        }

        private void GameOnWindowCreated(object sender, EventArgs eventArgs)
        {
            windowManager.Initialize(game.Window);
        }

        #endregion

        #region Public Events

        public event EventHandler<EventArgs> DeviceCreated;

        public event EventHandler<EventArgs> DeviceDisposing;

        public event EventHandler<EventArgs> DeviceReset;

        public event EventHandler<EventArgs> DeviceResetting;

        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

        #endregion

        #region Public Properties

        public GraphicsDevice GraphicsDevice { get; internal set; }

        public GraphicsPresenter Presenter => windowManager.Presenter;

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
        /// Gets or sets the device creation flags that will be used to create the <see cref="GraphicsDevice"/>
        /// </summary>
        /// <value>The device creation flags.</value>
        public DeviceCreationFlags DeviceCreationFlags
        {
            get => deviceCreationFlags;
            set
            {
                if (value != deviceCreationFlags)
                {
                    deviceCreationFlags = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// If populated the engine will try to initialize the device with the same unique id
        /// </summary>
        public string RequiredAdapterUid
        {
            get => requiredAdapterUid;
            set
            {
                if (value != requiredAdapterUid)
                {
                    requiredAdapterUid = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color space.
        /// </summary>
        /// <value>The default color space.</value>
        public ColorSpace PreferredColorSpace
        {
            get => windowManager.PreferredColorSpace;
            set => windowManager.PreferredColorSpace = value;
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
            get => windowManager.IsFullScreen;
            set => windowManager.IsFullScreen = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [prefer multi sampling].
        /// </summary>
        /// <value><c>true</c> if [prefer multi sampling]; otherwise, <c>false</c>.</value>
        public MultisampleCount PreferredMultisampleCount
        {
            get => windowManager.PreferredMultisampleCount;
            set => windowManager.PreferredMultisampleCount = value;
        }

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get => windowManager.PreferredBackBufferFormat;
            set => windowManager.PreferredBackBufferFormat = value;
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get => windowManager.PreferredBackBufferHeight;
            set => windowManager.PreferredBackBufferHeight = value;
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get => windowManager.PreferredBackBufferWidth;
            set => windowManager.PreferredBackBufferWidth = value;
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get => windowManager.PreferredDepthStencilFormat;
            set => windowManager.PreferredDepthStencilFormat = value;
        }

        /// <summary>
        /// Gets or sets the preferred refresh rate.
        /// </summary>
        /// <value>The preferred refresh rate.</value>
        public Rational PreferredRefreshRate
        {
            get => windowManager.PreferredRefreshRate;
            set => windowManager.PreferredRefreshRate = value;
        }

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex
        {
            get => windowManager.PreferredFullScreenOutputIndex;
            set => windowManager.PreferredFullScreenOutputIndex = value;
        }

        /// <summary>
        /// Gets or sets the supported orientations.
        /// </summary>
        /// <value>The supported orientations.</value>
        public DisplayOrientation SupportedOrientations
        {
            get => windowManager.SupportedOrientations;
            set => windowManager.SupportedOrientations = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [synchronize with vertical retrace].
        /// </summary>
        /// <value><c>true</c> if [synchronize with vertical retrace]; otherwise, <c>false</c>.</value>
        public bool SynchronizeWithVerticalRetrace
        {
            get => windowManager.SynchronizeWithVerticalRetrace;
            set => windowManager.SynchronizeWithVerticalRetrace = value;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Applies the changes from this instance and change or create the <see cref="GraphicsDevice"/> according to the new values.
        /// </summary>
        public void ApplyChanges()
        {
            if (GraphicsDevice != null)
            {
                if (deviceSettingsChanged)
                    ChangeOrCreateDevice(false);

                windowManager.ApplyChanges();
            }
        }

        bool IGraphicsDeviceManager.BeginDraw()
        {
            if (GraphicsDevice == null)
            {
                return false;
            }

            beginDrawOk = false;

            if (!CheckDeviceState())
                return false;

            GraphicsDevice.Begin();

            // TODO GRAPHICS REFACTOR
            //// Before drawing, we should clear the state to make sure that there is no unstable graphics device states (On some WP8 devices for example)
            //// An application should not rely on previous state (last frame...etc.) after BeginDraw.
            //GraphicsDevice.ClearState();
            //
            //// By default, we setup the render target to the back buffer, and the viewport as well.
            //if (GraphicsDevice.BackBuffer != null)
            //{
            //    GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            //}

            beginDrawOk = true;
            return beginDrawOk;
        }

        private bool CheckDeviceState()
        {
            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Removed:
                    Utilities.Sleep(TimeSpan.FromMilliseconds(20));
                    return false;
                case GraphicsDeviceStatus.Reset:
                    Utilities.Sleep(TimeSpan.FromMilliseconds(20));
                    try
                    {
                        ChangeOrCreateDevice(true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        void IGraphicsDeviceManager.CreateDevice()
        {
            // Force the creation of the device
            ChangeOrCreateDevice(true);

            windowManager.ApplyChanges();
        }

        void IGraphicsDeviceManager.EndDraw(bool present)
        {
            if (beginDrawOk && GraphicsDevice != null)
            {
                if (present && windowManager.Presenter != null)
                {
                    try
                    {
                        windowManager.Presenter.Present();
                    }
                    catch (GraphicsException ex)
                    {
                        // If this is not a DeviceRemoved or DeviceReset, than throw an exception
                        if (ex.Status != GraphicsDeviceStatus.Removed && ex.Status != GraphicsDeviceStatus.Reset)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        beginDrawOk = false;
                        GraphicsDevice.End();
                    }
                }
                else
                {
                    beginDrawOk = false;
                    GraphicsDevice.End();
                }
            }
        }

        #endregion

        protected override void Destroy()
        {
            if (game != null)
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
            }

            if (windowManager != null)
            {
                windowManager.Dispose();
            }

            if (GraphicsDevice != null)
            {
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
            // By default, a reset is compatible when we stay under the same graphics profile.
            return GraphicsDevice.Features.RequestedProfile == newDeviceInfo.GraphicsProfile;
        }

        /// <summary>
        /// Finds the best device that is compatible with the preferences defined in this instance.
        /// </summary>
        /// <returns>The graphics device information.</returns>
        protected virtual GraphicsDeviceInformation FindBestDevice()
        {
            // TODO: Nearly same code as in GameWindowManager

            // Setup preferred parameters before passing them to the factory
            var preferredParameters = new GameGraphicsParameters
            {
                PreferredBackBufferWidth = PreferredBackBufferWidth,
                PreferredBackBufferHeight = PreferredBackBufferHeight,
                PreferredBackBufferFormat = PreferredBackBufferFormat,
                PreferredDepthStencilFormat = PreferredDepthStencilFormat,
                PreferredRefreshRate = PreferredRefreshRate,
                PreferredFullScreenOutputIndex = PreferredFullScreenOutputIndex,
                IsFullScreen = IsFullScreen,
                PreferredMultisampleCount = PreferredMultisampleCount,
                SynchronizeWithVerticalRetrace = SynchronizeWithVerticalRetrace,
                PreferredGraphicsProfile = (GraphicsProfile[])PreferredGraphicsProfile.Clone(),
                ColorSpace = PreferredColorSpace,
                RequiredAdapterUid = RequiredAdapterUid,
            };

            // Remap to Srgb backbuffer if necessary
            if (PreferredColorSpace == ColorSpace.Linear)
            {
                // If the device support SRgb and ColorSpace is linear, we use automatically a SRgb backbuffer
                if (preferredParameters.PreferredBackBufferFormat == PixelFormat.R8G8B8A8_UNorm)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                }
                else if (preferredParameters.PreferredBackBufferFormat == PixelFormat.B8G8R8A8_UNorm)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.B8G8R8A8_UNorm_SRgb;
                }
            }
            else
            {
                // If we are looking for gamma and the backbuffer format is SRgb, switch back to non srgb
                if (preferredParameters.PreferredBackBufferFormat == PixelFormat.R8G8B8A8_UNorm_SRgb)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
                }
                else if (preferredParameters.PreferredBackBufferFormat == PixelFormat.B8G8R8A8_UNorm_SRgb)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.B8G8R8A8_UNorm;
                }
            }

            var devices = graphicsDeviceFactory.FindBestDevices(preferredParameters);
            if (devices.Count == 0)
            {
                // Nothing was found; first, let's check if graphics profile was actually supported
                // Note: we don't do this preemptively because in some cases it seems to take lot of time (happened on a test machine, several seconds freeze on ID3D11Device.Release())
                GraphicsProfile availableGraphicsProfile;
                if (!IsPreferredProfileAvailable(preferredParameters.PreferredGraphicsProfile, out availableGraphicsProfile))
                {
                    throw new InvalidOperationException($"Graphics profiles [{string.Join(", ", preferredParameters.PreferredGraphicsProfile)}] are not supported by the device. The highest available profile is [{availableGraphicsProfile}].");
                }

                // Otherwise, there was just no screen mode
                throw new InvalidOperationException("No screen modes found");
            }

            RankDevices(devices);

            if (devices.Count == 0)
            {
                throw new InvalidOperationException("No screen modes found after ranking");
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
            {
                return;
            }

            foundDevices.Sort(
                (left, right) =>
                    {
                        var leftParams = left.PresentationParameters;
                        var rightParams = right.PresentationParameters;

                        var leftAdapter = left.Adapter;
                        var rightAdapter = right.Adapter;

                        // Sort by GraphicsProfile
                        if (left.GraphicsProfile != right.GraphicsProfile)
                        {
                            return left.GraphicsProfile <= right.GraphicsProfile ? 1 : -1;
                        }

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
                        var targetAspectRatio = (PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0) ? (float)GameWindowManager.DefaultBackBufferWidth / GameWindowManager.DefaultBackBufferHeight : (float)PreferredBackBufferWidth / PreferredBackBufferHeight;
                        var leftDiffRatio = Math.Abs(((float)leftParams.BackBufferWidth / leftParams.BackBufferHeight) - targetAspectRatio);
                        var rightDiffRatio = Math.Abs(((float)rightParams.BackBufferWidth / rightParams.BackBufferHeight) - targetAspectRatio);
                        if (Math.Abs(leftDiffRatio - rightDiffRatio) > 0.2f)
                        {
                            return leftDiffRatio >= rightDiffRatio ? 1 : -1;
                        }

                        // Sort by PixelCount
                        int leftPixelCount;
                        int rightPixelCount;
                        if (IsFullScreen)
                        {
                            if (((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0)) &&
                                PreferredFullScreenOutputIndex < leftAdapter.Outputs.Length && 
                                PreferredFullScreenOutputIndex < rightAdapter.Outputs.Length)
                            {
                                // assume we got here only adapters that have the needed number of outputs:
                                var leftOutput = leftAdapter.Outputs[PreferredFullScreenOutputIndex];
                                var rightOutput = rightAdapter.Outputs[PreferredFullScreenOutputIndex];

                                leftPixelCount = leftOutput.CurrentDisplayMode.Width * leftOutput.CurrentDisplayMode.Height;
                                rightPixelCount = rightOutput.CurrentDisplayMode.Width * rightOutput.CurrentDisplayMode.Height;
                            }
                            else
                            {
                                leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                            }
                        }
                        else if ((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0))
                        {
                            leftPixelCount = rightPixelCount = GameWindowManager.DefaultBackBufferWidth * GameWindowManager.DefaultBackBufferHeight;
                        }
                        else
                        {
                            leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                        }

                        int leftDeltaPixelCount = Math.Abs((leftParams.BackBufferWidth * leftParams.BackBufferHeight) - leftPixelCount);
                        int rightDeltaPixelCount = Math.Abs((rightParams.BackBufferWidth * rightParams.BackBufferHeight) - rightPixelCount);
                        if (leftDeltaPixelCount != rightDeltaPixelCount)
                        {
                            return leftDeltaPixelCount >= rightDeltaPixelCount ? 1 : -1;
                        }

                        // Sort by default Adapter, default adapter first
                        if (left.Adapter != right.Adapter)
                        {
                            if (left.Adapter.IsDefaultAdapter)
                            {
                                return -1;
                            }

                            if (right.Adapter.IsDefaultAdapter)
                            {
                                return 1;
                            }
                        }

                        return 0;
                    });
        }

        protected virtual bool IsPreferredProfileAvailable(GraphicsProfile[] preferredProfiles, out GraphicsProfile availableProfile)
        {
            availableProfile = GraphicsProfile.Level_9_1;

            var graphicsProfiles = Enum.GetValues(typeof(GraphicsProfile));

            foreach (var graphicsAdapter in GraphicsAdapterFactory.Adapters)
            {
                foreach (var graphicsProfileValue in graphicsProfiles)
                {
                    var graphicsProfile = (GraphicsProfile)graphicsProfileValue;
                    if (graphicsProfile > availableProfile && graphicsAdapter.IsProfileSupported(graphicsProfile))
                        availableProfile = graphicsProfile;
                }
            }

            foreach (var preferredProfile in preferredProfiles)
            {
                if (availableProfile >= preferredProfile)
                    return true;
            }

            return false;
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

        private void CreateDevice(GraphicsDeviceInformation newInfo)
        {
            newInfo.DeviceCreationFlags = DeviceCreationFlags;

            // this.ValidateGraphicsDeviceInformation(newInfo);

            bool deviceRecreate = GraphicsDevice != null;

            // Notify device is resetting (usually this should result in graphics resources being destroyed)
            if (deviceRecreate)
                OnDeviceResetting(this, EventArgs.Empty);

            // Create (or recreate) the graphics device
            GraphicsDevice = graphicsDeviceFactory.ChangeOrCreateDevice(GraphicsDevice, newInfo);

            // Notify device is reset (usually this should result in graphics resources being recreated/reloaded)
            if (deviceRecreate)
                OnDeviceReset(this, EventArgs.Empty);

            // Use the shader profile returned by the GraphicsDeviceInformation otherwise use the one coming from the GameSettings
            GraphicsDevice.ShaderProfile = ShaderProfile;

            // TODO HANDLE Device Resetting/Reset/Lost
            //GraphicsDevice.DeviceResetting += GraphicsDevice_DeviceResetting;
            //GraphicsDevice.DeviceReset += GraphicsDevice_DeviceReset;
            //GraphicsDevice.DeviceLost += GraphicsDevice_DeviceLost;
            if (!deviceRecreate)
                GraphicsDevice.Disposing += GraphicsDevice_Disposing;

            OnDeviceCreated(this, EventArgs.Empty);
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
                    game.ConfirmRenderingSettings(GraphicsDevice == null); //if Device is null we assume we are still at game creation phase

                    var graphicsDeviceInformation = FindBestDevice();

                    OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(graphicsDeviceInformation));

                    // If we are not forced to create a new device and this is already an existing GraphicsDevice try to reset it.
                    if (forceCreate || GraphicsDevice is null || !CanResetDevice(graphicsDeviceInformation))
                    {
                        CreateDevice(graphicsDeviceInformation);
                    }

                    if (GraphicsDevice == null)
                    {
                        throw new InvalidOperationException("Unexpected null GraphicsDevice");
                    }

                    deviceSettingsChanged = false;
                }
            }
        }
    }
}
