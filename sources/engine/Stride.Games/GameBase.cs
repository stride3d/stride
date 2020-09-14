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
using System.Reflection;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Games.Time;
using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    /// The game.
    /// </summary>
    public abstract class GameBase : ComponentBase, IGame
    {
        #region Fields

        private readonly GamePlatform gamePlatform;
        private IGraphicsDeviceService graphicsDeviceService;
        protected IGraphicsDeviceManager graphicsDeviceManager;
        private ResumeManager resumeManager;
        private bool isEndRunRequired;
        private bool suppressDraw;
        private bool beginDrawOk;

        private readonly TimeSpan maximumElapsedTime;
        private TimeSpan accumulatedElapsedGameTime;
        private bool forceElapsedTimeToZero;

        private readonly TimerTick autoTickTimer;

        protected readonly ILogger Log;

        private bool isMouseVisible;

        internal object TickLock = new object();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GameBase" /> class.
        /// </summary>
        protected GameBase()
        {
            // Internals
            Log = GlobalLogger.GetLogger(GetType().GetTypeInfo().Name);
            UpdateTime = new GameTime();
            DrawTime = new GameTime();
            autoTickTimer = new TimerTick();
            IsFixedTimeStep = false;
            maximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60); // target elapsed time is by default 60Hz
            
            TreatNotFocusedLikeMinimized = true;
            WindowMinimumUpdateRate = new ThreadThrottler(TimeSpan.FromSeconds(0d));
            MinimizedMinimumUpdateRate = new ThreadThrottler(15); // by default 15 updates per second while minimized

            isMouseVisible = true;

            // Externals
            Services = new ServiceRegistry();

            // Database file provider
            Services.AddService<IDatabaseFileProviderService>(new DatabaseFileProviderService(null));

            LaunchParameters = new LaunchParameters();
            GameSystems = new GameSystemCollection(Services);
            Services.AddService<IGameSystemCollection>(GameSystems);

            // Create Platform
            gamePlatform = GamePlatform.Create(this);
            gamePlatform.Activated += GamePlatform_Activated;
            gamePlatform.Deactivated += GamePlatform_Deactivated;
            gamePlatform.Exiting += GamePlatform_Exiting;
            gamePlatform.WindowCreated += GamePlatformOnWindowCreated;

            // Setup registry
            Services.AddService<IGame>(this);
            Services.AddService<IGraphicsDeviceFactory>(gamePlatform);
            Services.AddService<IGamePlatform>(gamePlatform);

            IsActive = true;
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when [activated].
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Occurs when [deactivated].
        /// </summary>
        public event EventHandler<EventArgs> Deactivated;

        /// <summary>
        /// Occurs when [exiting].
        /// </summary>
        public event EventHandler<EventArgs> Exiting;

        /// <summary>
        /// Occurs when [window created].
        /// </summary>
        public event EventHandler<EventArgs> WindowCreated;

        public event EventHandler<GameUnhandledExceptionEventArgs> UnhandledException;

        #endregion

        #region Public Properties

        /// <summary>
        /// The total and delta time to be used for logic running in the update loop.
        /// </summary>
        public GameTime UpdateTime { get; }

        /// <summary>
        /// The total and delta time to be used for logic running in the draw loop.
        /// </summary>
        public GameTime DrawTime { get; }

        /// <summary>
        /// Is used when we draw without running an update beforehand, so when both <see cref="IsFixedTimeStep"/> 
        /// and <see cref="IsDrawDesynchronized"/> are set.<br/>
        /// It returns a number between 0 and 1 which represents the current position our DrawTime is in relation 
        /// to the previous and next step.<br/>
        /// 0.5 would mean that we are rendering at a time halfway between the previous and next fixed-step.
        /// </summary>
        /// <value>
        /// The draw interpolation factor.
        /// </value>
        public float DrawInterpolationFactor { get; private set; }

        /// <summary>
        /// Gets the <see cref="ContentManager"/>.
        /// </summary>
        public ContentManager Content { get; private set; }

        /// <summary>
        /// Gets the game components registered by this game.
        /// </summary>
        /// <value>The game components.</value>
        public GameSystemCollection GameSystems { get; private set; }

        /// <summary>
        /// Gets the game context.
        /// </summary>
        /// <value>The game context.</value>
        public GameContext Context { get; private set; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        public GraphicsContext GraphicsContext { get; private set; }

        /// <summary>
        /// Gets or sets the time between each <see cref="Tick"/> when <see cref="IsActive"/> is false.
        /// </summary>
        /// <value>The inactive sleep time.</value>
        public TimeSpan InactiveSleepTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is exiting.
        /// </summary>
        /// <value><c>true</c> if this instance is exiting; otherwise, <c>false</c>.</value>
        public bool IsExiting{ get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the elapsed time between each update should be constant,
        /// see <see cref="TargetElapsedTime"/> to configure the duration.
        /// </summary>
        /// <value><c>true</c> if this instance is fixed time step; otherwise, <c>false</c>.</value>
        public bool IsFixedTimeStep { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance should force exactly one update step per one draw step
        /// </summary>
        /// <value><c>true</c> if this instance forces one update step per one draw step; otherwise, <c>false</c>.</value>
        protected internal bool ForceOneUpdatePerDraw { get; set; }

        /// <summary>
        /// When <see cref="IsFixedTimeStep"/> is set, is it allowed to render frames between two steps when we have time to do so.
        /// </summary>
        /// <value><c>true</c> if this instance's drawing is desychronized ; otherwise, <c>false</c>.</value>
        public bool IsDrawDesynchronized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mouse should be visible.
        /// </summary>
        /// <value><c>true</c> if the mouse should be visible; otherwise, <c>false</c>.</value>
        public bool IsMouseVisible
        {
            get
            {
                return Window?.IsMouseVisible ?? isMouseVisible;
            }
            set
            {
                isMouseVisible = value;
                var window = Window;
                if (window != null)
                {
                    window.IsMouseVisible = value;
                }
            }
        }

        /// <summary>
        /// Gets the launch parameters.
        /// </summary>
        /// <value>The launch parameters.</value>
        public LaunchParameters LaunchParameters { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the service container.
        /// </summary>
        /// <value>The service container.</value>
        [NotNull]
        public ServiceRegistry Services { get; }

        /// <summary>
        /// Gets or sets the target elapsed time, this is the duration of each tick/update
        /// when <see cref="IsFixedTimeStep"/> is enabled.
        /// </summary>
        /// <value>The target elapsed time.</value>
        public TimeSpan TargetElapsedTime { get; set; }

        /// <summary>
        /// Access to the throttler used to set the minimum time allowed between each updates.
        /// </summary>
        public ThreadThrottler WindowMinimumUpdateRate { get; }

        /// <summary>
        /// Access to the throttler used to set the minimum time allowed between each updates while the window is minimized and,
        /// depending on <see cref="TreatNotFocusedLikeMinimized"/>, while unfocused.
        /// </summary>
        public ThreadThrottler MinimizedMinimumUpdateRate { get; }

        /// <summary>
        /// Considers windows without user focus like a minimized window for <see cref="MinimizedMinimumUpdateRate"/> 
        /// </summary>
        public bool TreatNotFocusedLikeMinimized { get; set; }

        /// <summary>
        /// Should this instance still render/draw when the window is minimized. Updates are still going to run regardless of the value set.
        /// </summary>
        public bool DrawWhileMinimized { get; set; }

        /// <summary>
        /// Gets the abstract window.
        /// </summary>
        /// <value>The window.</value>
        public GameWindow Window
        {
            get
            {
                if (gamePlatform != null)
                {
                    return gamePlatform.MainWindow;
                }
                return null;
            }
        }

        public abstract void ConfirmRenderingSettings(bool gameCreation);

        /// <summary>
        /// Gets the full name of the device this game is running if available
        /// </summary>
        public string FullPlatformName => gamePlatform.FullName;

        #endregion

        internal EventHandler<GameUnhandledExceptionEventArgs> UnhandledExceptionInternal
        {
            get { return UnhandledException; }
        }

        #region Public Methods and Operators

        /// <summary>
        /// Exits the game.
        /// </summary>
        public void Exit()
        {
            IsExiting = true;
            gamePlatform.Exit();
        }

        /// <summary>
        /// Resets the elapsed time counter.
        /// </summary>
        public void ResetElapsedTime()
        {
            forceElapsedTimeToZero = true;
        }

        internal void InitializeBeforeRun()
        {
            try
            {
                using (var profile = Profiler.Begin(GameProfilingKeys.GameInitialize))
                {
                    // Initialize this instance and all game systems before trying to create the device.
                    Initialize();

                    // Make sure that the device is already created
                    graphicsDeviceManager.CreateDevice();

                    // Gets the graphics device service
                    graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();
                    if (graphicsDeviceService == null)
                    {
                        throw new InvalidOperationException("No GraphicsDeviceService found");
                    }

                    // Checks the graphics device
                    if (graphicsDeviceService.GraphicsDevice == null)
                    {
                        throw new InvalidOperationException("No GraphicsDevice found");
                    }

                    // Setup the graphics device if it was not already setup.
                    SetupGraphicsDeviceEvents();

                    // Bind Graphics Context enabling initialize to use GL API eg. SetData to texture ...etc
                    BeginDraw();

                    LoadContentInternal();

                    IsRunning = true;

                    BeginRun();

                    autoTickTimer.Reset();
                    UpdateTime.Reset(UpdateTime.Total);

                    // Run the first time an update
                    using (Profiler.Begin(GameProfilingKeys.GameUpdate))
                    {
                        Update(UpdateTime);
                    }

                    // Unbind Graphics Context without presenting
                    EndDraw(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        /// <summary>
        /// Call this method to initialize the game, begin running the game loop, and start processing events for the game.
        /// </summary>
        /// <param name="gameContext">The window Context for this game.</param>
        /// <exception cref="System.InvalidOperationException">Cannot run this instance while it is already running</exception>
        public void Run(GameContext gameContext = null)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Cannot run this instance while it is already running");
            }

            // Gets the graphics device manager
            graphicsDeviceManager = Services.GetService<IGraphicsDeviceManager>();
            if (graphicsDeviceManager == null)
            {
                throw new InvalidOperationException("No GraphicsDeviceManager found");
            }

            // Gets the GameWindow Context
            Context = gameContext ?? GameContextFactory.NewDefaultGameContext();

            PrepareContext();

            try
            {
                // TODO temporary workaround as the engine doesn't support yet resize
                var graphicsDeviceManagerImpl = (GraphicsDeviceManager)graphicsDeviceManager;
                Context.RequestedWidth = graphicsDeviceManagerImpl.PreferredBackBufferWidth;
                Context.RequestedHeight = graphicsDeviceManagerImpl.PreferredBackBufferHeight;
                Context.RequestedBackBufferFormat = graphicsDeviceManagerImpl.PreferredBackBufferFormat;
                Context.RequestedDepthStencilFormat = graphicsDeviceManagerImpl.PreferredDepthStencilFormat;
                Context.RequestedGraphicsProfile = graphicsDeviceManagerImpl.PreferredGraphicsProfile;
                Context.DeviceCreationFlags = graphicsDeviceManagerImpl.DeviceCreationFlags;

                gamePlatform.Run(Context);

                if (gamePlatform.IsBlockingRun)
                {
                    // If the previous call was blocking, then we can call Endrun
                    EndRun();
                }
                else
                {
                    // EndRun will be executed on Game.Exit
                    isEndRunRequired = true;
                }
            }
            finally
            {
                if (!isEndRunRequired)
                {
                    IsRunning = false;
                }
            }
        }

        /// <summary>
        /// Creates or updates <see cref="Context"/> before window and device are created.
        /// </summary>
        protected virtual void PrepareContext()
        {
            // Content manager
            Content = new ContentManager(Services);
            Services.AddService<IContentManager>(Content);
            Services.AddService(Content);
        }

        /// <summary>
        /// Prevents calls to Draw until the next Update.
        /// </summary>
        public void SuppressDraw()
        {
            suppressDraw = true;
        }

        /// <summary>
        /// Updates the game's clock and calls Update and Draw.
        /// </summary>
        public void Tick()
        {
            lock (TickLock)
            {
                // If this instance is existing, then don't make any further update/draw
                if (IsExiting)
                {
                    CheckEndRun();
                    return;
                }

                // If this instance is not active, sleep for an inactive sleep time
                if (!IsActive)
                {
                    Utilities.Sleep(InactiveSleepTime);
                    return;
                }

                RawTickProducer();
            }
        }

        /// <summary>
        /// Calls <see cref="RawTick"/> automatically based on this game's setup, override it to implement your own system.
        /// </summary>
        protected virtual void RawTickProducer()
        {
            try
            {
                // Update the timer
                autoTickTimer.Tick();

                var elapsedAdjustedTime = autoTickTimer.ElapsedTimeWithPause;

                if (forceElapsedTimeToZero)
                {
                    elapsedAdjustedTime = TimeSpan.Zero;
                    forceElapsedTimeToZero = false;
                }

                if (elapsedAdjustedTime > maximumElapsedTime)
                {
                    elapsedAdjustedTime = maximumElapsedTime;
                }

                bool drawFrame = true;
                int updateCount = 1;
                var singleFrameElapsedTime = elapsedAdjustedTime;
                var drawLag = 0L;

                if (suppressDraw || Window.IsMinimized && DrawWhileMinimized == false)
                {
                    drawFrame = false;
                    suppressDraw = false;
                }

                if (IsFixedTimeStep)
                {
                    // If the rounded TargetElapsedTime is equivalent to current ElapsedAdjustedTime
                    // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA
                    if (Math.Abs(elapsedAdjustedTime.Ticks - TargetElapsedTime.Ticks) < (TargetElapsedTime.Ticks >> 6))
                    {
                        elapsedAdjustedTime = TargetElapsedTime;
                    }

                    // Update the accumulated time
                    accumulatedElapsedGameTime += elapsedAdjustedTime;

                    // Calculate the number of update to issue
                    if (ForceOneUpdatePerDraw)
                    {
                        updateCount = 1;
                    }
                    else
                    {
                        updateCount = (int)(accumulatedElapsedGameTime.Ticks / TargetElapsedTime.Ticks);
                    }

                    if (IsDrawDesynchronized)
                    {
                        drawLag = accumulatedElapsedGameTime.Ticks % TargetElapsedTime.Ticks;
                    }
                    else if (updateCount == 0)
                    {
                        drawFrame = false;
                        // If there is no need for update, then exit
                        return;
                    }

                    // We are going to call Update updateCount times, so we can subtract this from accumulated elapsed game time
                    accumulatedElapsedGameTime = new TimeSpan(accumulatedElapsedGameTime.Ticks - (updateCount * TargetElapsedTime.Ticks));
                    singleFrameElapsedTime = TargetElapsedTime;
                }

                RawTick(singleFrameElapsedTime, updateCount, drawLag / (float)TargetElapsedTime.Ticks, drawFrame);

                var window = gamePlatform.MainWindow;
                if (gamePlatform.IsBlockingRun) // throttle fps if Game.Tick() called from internal main loop
                {
                    if (window.IsMinimized || window.Visible == false || (window.Focused == false && TreatNotFocusedLikeMinimized))
                    {
                        MinimizedMinimumUpdateRate.Throttle(out long _);
                    }
                    else
                    {
                        WindowMinimumUpdateRate.Throttle(out long _);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        /// <summary>
        /// Call this method within your overriden <see cref="RawTickProducer"/> to update and draw the game yourself. <br/>
        /// As this version is manual, there are a lot of functionality purposefully skipped: <br/>
        /// clamping elapsed time to a maximum, skipping drawing when the window is minimized, <see cref="ResetElapsedTime"/>, <see cref="SuppressDraw"/>, <see cref="IsFixedTimeStep"/>, <br/>
        /// <see cref="IsDrawDesynchronized"/>, <see cref="MinimizedMinimumUpdateRate"/> / <see cref="WindowMinimumUpdateRate"/> / <see cref="TreatNotFocusedLikeMinimized"/>.
        /// </summary>
        /// <param name="elapsedTimePerUpdate">
        /// The amount of time passed between each update of the game's system, 
        /// the total time passed would be <paramref name="elapsedTimePerUpdate"/> * <paramref name="updateCount"/>.
        /// </param>
        /// <param name="updateCount">
        /// The amount of updates that will be executed on the game's systems.
        /// </param>
        /// <param name="drawInterpolationFactor">
        /// See <see cref="DrawInterpolationFactor"/>
        /// </param>
        /// <param name="drawFrame">
        /// Draw a frame.
        /// </param>
        protected void RawTick(TimeSpan elapsedTimePerUpdate, int updateCount = 1, float drawInterpolationFactor = 0, bool drawFrame = true)
        {
            bool beginDrawSuccessful = false;
            TimeSpan totalElapsedTime = TimeSpan.Zero;
            try
            {
                beginDrawSuccessful = BeginDraw();

                // Reset the time of the next frame
                for (int i = 0; i < updateCount && !IsExiting; i++)
                {
                    UpdateTime.Update(UpdateTime.Total + elapsedTimePerUpdate, elapsedTimePerUpdate, true);
                    using (Profiler.Begin(GameProfilingKeys.GameUpdate))
                    {
                        Update(UpdateTime);
                    }
                    totalElapsedTime += elapsedTimePerUpdate;
                }

                if (drawFrame && !IsExiting && GameSystems.IsFirstUpdateDone)
                {
                    DrawInterpolationFactor = drawInterpolationFactor;
                    DrawTime.Factor = UpdateTime.Factor;
                    DrawTime.Update(DrawTime.Total + totalElapsedTime, totalElapsedTime, true);

                    var profilingDraw = Profiler.Begin(GameProfilingKeys.GameDrawFPS);
                    var profiler = Profiler.Begin(GameProfilingKeys.GameDraw);

                    GraphicsDevice.FrameTriangleCount = 0;
                    GraphicsDevice.FrameDrawCalls = 0;

                    Draw(DrawTime);

                    profiler.End("Triangle count: {0}", GraphicsDevice.FrameTriangleCount);
                    profilingDraw.End("Frame = {0}, Update = {1:0.000}ms, Draw = {2:0.000}ms, FPS = {3:0.00}", DrawTime.FrameCount, UpdateTime.TimePerFrame.TotalMilliseconds, DrawTime.TimePerFrame.TotalMilliseconds, DrawTime.FramePerSecond);
                }
            }
            finally
            {
                if (beginDrawSuccessful)
                {
                    using (Profiler.Begin(GameProfilingKeys.GameEndDraw))
                    {
                        EndDraw(true);
                    }
                }

                CheckEndRun();
            }
        }

        private void CheckEndRun()
        {
            if (IsExiting && IsRunning && isEndRunRequired)
            {
                EndRun();
                IsRunning = false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called after all components are initialized, before the game loop starts.
        /// </summary>
        protected virtual void BeginRun()
        {
        }

        /// <summary>Called after the game loop has stopped running before exiting.</summary>
        protected virtual void EndRun()
        {
        }

        protected override void Destroy()
        {
            base.Destroy();

            lock (this)
            {
                if (Window != null && Window.IsActivated) // force the window to be in an correct state during destroy (Deactivated events are sometimes dropped on windows)
                {
                    Window.OnPause();
                }

                var array = new IGameSystemBase[GameSystems.Count];
                GameSystems.CopyTo(array, 0);
                for (int i = 0; i < array.Length; i++)
                {
                    var disposable = array[i] as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                // Reset graphics context
                GraphicsContext = null;

                var disposableGraphicsManager = graphicsDeviceManager as IDisposable;
                if (disposableGraphicsManager != null)
                {
                    disposableGraphicsManager.Dispose();
                }

                DisposeGraphicsDeviceEvents();

                if (gamePlatform != null)
                {
                    gamePlatform.Release();
                }
            }
        }

        /// <summary>
        /// Starts the drawing of a frame. This method is followed by calls to <see cref="Update"/>, <see cref="Draw"/> and <see cref="EndDraw"/>.
        /// </summary>
        /// <returns><c>true</c> to continue drawing, false to not call <see cref="Draw"/> and <see cref="EndDraw"/></returns>
        protected virtual bool BeginDraw()
        {
            beginDrawOk = false;

            if ((graphicsDeviceManager != null) && !graphicsDeviceManager.BeginDraw())
            {
                return false;
            }

            // Setup default command list
            if (GraphicsContext == null)
            {
                GraphicsContext = new GraphicsContext(GraphicsDevice);
                Services.AddService(GraphicsContext);
            }
            else
            {
                // Reset allocator
                GraphicsContext.ResourceGroupAllocator.Reset(GraphicsContext.CommandList);
                GraphicsContext.CommandList.Reset();
            }

            beginDrawOk = true;

            // Clear states
            GraphicsContext.CommandList.ClearState();

            // Perform begin of frame presenter operations
            if (GraphicsDevice.Presenter != null)
            {
                GraphicsContext.CommandList.ResourceBarrierTransition(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsResourceState.DepthWrite);
                GraphicsContext.CommandList.ResourceBarrierTransition(GraphicsDevice.Presenter.BackBuffer, GraphicsResourceState.RenderTarget);

                GraphicsDevice.Presenter.BeginDraw(GraphicsContext.CommandList);
            }

            return true;
        }

        /// <summary>
        /// Reference page contains code sample.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Draw.
        /// </param>
        protected virtual void Draw(GameTime gameTime)
        {
            GameSystems.Draw(gameTime);

            // Make sure that the render target is set back to the back buffer
            // From a user perspective this is better. From an internal point of view,
            // this code is already present in GraphicsDeviceManager.BeginDraw()
            // but due to the fact that a GameSystem can modify the state of GraphicsDevice
            // we need to restore the default render targets
            // TODO: Check how we can handle this more cleanly
            if (GraphicsDevice != null && GraphicsDevice.Presenter.BackBuffer != null)
            {
                GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            }
        }

        /// <summary>Ends the drawing of a frame. This method will always be preceeded by calls to <see cref="BeginDraw"/> and perhaps <see cref="Draw"/> depending on if we had to do so. </summary>
        protected virtual void EndDraw(bool present)
        {
            if (beginDrawOk)
            {
                if (GraphicsDevice.Presenter != null)
                {
                    // Perform end of frame presenter operations
                    GraphicsDevice.Presenter.EndDraw(GraphicsContext.CommandList, present);

                    GraphicsContext.CommandList.ResourceBarrierTransition(GraphicsDevice.Presenter.BackBuffer, GraphicsResourceState.Present);
                }

                GraphicsContext.ResourceGroupAllocator.Flush();

                // Close command list
                GraphicsContext.CommandList.Flush();

                // Present (if necessary)
                graphicsDeviceManager.EndDraw(present);

                beginDrawOk = false;
            }
        }

        /// <summary>
        /// Reference page contains links to related conceptual articles.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Update.
        /// </param>
        protected virtual void Update(GameTime gameTime)
        {
            GameSystems.Update(gameTime);
        }

        /// <summary>Called after the Game is created, but before <see cref="GraphicsDevice"/> is available and before LoadContent(). Reference page contains code sample.</summary>
        protected virtual void Initialize()
        {
            GameSystems.Initialize();
        }

        internal virtual void LoadContentInternal()
        {
            GameSystems.LoadContent();
        }

        /// <summary>
        /// Raises the Activated event. Override this method to add code to handle when the game gains focus.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the Activated event.</param>
        protected virtual void OnActivated(object sender, EventArgs args)
        {
            Activated?.Invoke(this, args);
        }

        /// <summary>
        /// Raises the Deactivated event. Override this method to add code to handle when the game loses focus.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the Deactivated event.</param>
        protected virtual void OnDeactivated(object sender, EventArgs args)
        {
            Deactivated?.Invoke(this, args);
        }

        /// <summary>
        /// Raises an Exiting event. Override this method to add code to handle when the game is exiting.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the Exiting event.</param>
        protected virtual void OnExiting(object sender, EventArgs args)
        {
            Exiting?.Invoke(this, args);
        }

        protected virtual void OnWindowCreated()
        {
            WindowCreated?.Invoke(this, EventArgs.Empty);
        }

        private void GamePlatformOnWindowCreated(object sender, EventArgs eventArgs)
        {
            Window.IsMouseVisible = isMouseVisible;
            OnWindowCreated();
        }

        /// <summary>
        /// This is used to display an error message if there is no suitable graphics device or sound card.
        /// </summary>
        /// <param name="exception">The exception to display.</param>
        /// <returns>The <see cref="bool" />.</returns>
        protected virtual bool ShowMissingRequirementMessage(Exception exception)
        {
            return true;
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded. Override this method to unload any game-specific graphics resources.
        /// </summary>
        protected virtual void UnloadContent()
        {
            GameSystems.UnloadContent();
        }

        private void GamePlatform_Activated(object sender, EventArgs e)
        {
            if (!IsActive)
            {
                IsActive = true;
                OnActivated(this, EventArgs.Empty);
            }
        }

        private void GamePlatform_Deactivated(object sender, EventArgs e)
        {
            if (IsActive)
            {
                IsActive = false;
                OnDeactivated(this, EventArgs.Empty);
            }
        }

        private void GamePlatform_Exiting(object sender, EventArgs e)
        {
            OnExiting(this, EventArgs.Empty);
        }

        private void SetupGraphicsDeviceEvents()
        {
            // Find the IGraphicsDeviceSerive.
            graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();

            // If there is no graphics device service, don't go further as the whole Game would not work
            if (graphicsDeviceService == null)
            {
                throw new InvalidOperationException("Unable to create a IGraphicsDeviceService");
            }

            if (graphicsDeviceService.GraphicsDevice == null)
            {
                throw new InvalidOperationException("Unable to find a GraphicsDevice instance");
            }

            resumeManager = new ResumeManager(Services);

            GraphicsDevice = graphicsDeviceService.GraphicsDevice;
            graphicsDeviceService.DeviceCreated += GraphicsDeviceService_DeviceCreated;
            graphicsDeviceService.DeviceResetting += GraphicsDeviceService_DeviceResetting;
            graphicsDeviceService.DeviceReset += GraphicsDeviceService_DeviceReset;
            graphicsDeviceService.DeviceDisposing += GraphicsDeviceService_DeviceDisposing;
        }

        private void DisposeGraphicsDeviceEvents()
        {
            if (graphicsDeviceService != null)
            {
                graphicsDeviceService.DeviceCreated -= GraphicsDeviceService_DeviceCreated;
                graphicsDeviceService.DeviceResetting -= GraphicsDeviceService_DeviceResetting;
                graphicsDeviceService.DeviceReset -= GraphicsDeviceService_DeviceReset;
                graphicsDeviceService.DeviceDisposing -= GraphicsDeviceService_DeviceDisposing;
                GraphicsDevice = null;
            }
        }

        private void GraphicsDeviceService_DeviceCreated(object sender, EventArgs e)
        {
            GraphicsDevice = graphicsDeviceService.GraphicsDevice;

            if (GameSystems.State != GameSystemState.ContentLoaded)
            {
                LoadContentInternal();
            }
        }

        private void GraphicsDeviceService_DeviceDisposing(object sender, EventArgs e)
        {
            if (GameSystems.State == GameSystemState.ContentLoaded)
            {
                UnloadContent();
            }

            resumeManager.OnDestroyed();

            GraphicsDevice = null;
        }

        private void GraphicsDeviceService_DeviceReset(object sender, EventArgs e)
        {
            resumeManager.OnReload();
            resumeManager.OnRecreate();
        }

        private void GraphicsDeviceService_DeviceResetting(object sender, EventArgs e)
        {
            resumeManager.OnDestroyed();
        }

        #endregion
    }
}
