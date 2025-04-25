// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Games.SDL;
using Stride.Games.Systems;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Fonts;
using Stride.Rendering.Sprites;
using Stride.Shaders.Compiler;
using Stride.Streaming;
using Stride.VirtualReality;

namespace Stride.Engine
{
    /// <summary>
    /// Main Game class system.
    /// </summary>
    public class DefaultGame : GameBase, ISceneRendererContext, IGameSettingsService
    {

        private bool isMouseVisible;

        /// <summary>
        /// Static event that will be fired when a game is initialized
        /// </summary>
        public static event EventHandler GameStarted;

        /// <summary>
        /// Occurs when [window created].
        /// </summary>
        public event EventHandler<EventArgs> WindowCreated;

        /// <summary>
        /// Static event that will be fired when a game is destroyed
        /// </summary>
        public static event EventHandler GameDestroyed;

        private GameFontSystem gameFontSystem;

        private LogListener logListener;

        private DatabaseFileProvider databaseFileProvider;

        /// <summary>
        /// Readonly game settings as defined in the GameSettings asset
        /// Please note that it will be populated during initialization
        /// It will be ok to read them after the GameStarted event or after initialization
        /// </summary>
        public GameSettings Settings { get; private set; } // for easy transfer from PrepareContext to Initialize

        /// <summary>
        /// Gets the graphics device manager.
        /// </summary>
        /// <value>The graphics device manager.</value>
        public GraphicsDeviceComponent GraphicsDeviceManager { get; internal set; }

        /// <summary>
        /// Gets the abstract window.
        /// </summary>
        /// <value>The window.</value>
        public GameWindow Window
        {
            get
            {
                if (GamePlatform is IWindowedPlatform windowedPlatform)
                {
                    return windowedPlatform.MainWindow;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the script system.
        /// </summary>
        /// <value>The script.</value>
        public ScriptSystem Script { get; private set; }

        /// <summary>
        /// Gets the input manager.
        /// </summary>
        /// <value>The input.</value>
        public InputManager Input { get; internal set; }

        /// <summary>
        /// Gets the scene system.
        /// </summary>
        /// <value>The scene system.</value>
        public SceneSystem SceneSystem { get; private set; }

        /// <summary>
        /// Gets the effect system.
        /// </summary>
        /// <value>The effect system.</value>
        public EffectSystem EffectSystem { get; private set; }

        /// <summary>
        /// Gets the streaming system.
        /// </summary>
        /// <value>The streaming system.</value>
        public StreamingManager Streaming { get; private set; }

        /// <summary>
        /// Gets the audio system.
        /// </summary>
        /// <value>The audio.</value>
        public AudioSystem Audio { get; private set; }

        /// <summary>
        /// Gets the sprite animation system.
        /// </summary>
        /// <value>The sprite animation system.</value>
        public SpriteAnimationSystem SpriteAnimation { get; private set; }

        /// <summary>
        /// Gets the game profiler system.
        /// </summary>
        public DebugTextSystem DebugTextSystem { get; private set; }

        /// <summary>
        /// Gets the game profiler system.
        /// </summary>
        public GameProfilingSystem ProfilingSystem { get; private set; }

        /// <summary>
        /// Gets the VR Device System.
        /// </summary>
        public VRDeviceSystem VRDeviceSystem { get; private set; }

        /// <summary>
        /// Gets the font system.
        /// </summary>
        /// <value>The font system.</value>
        /// <exception cref="System.InvalidOperationException">The font system is not initialized yet</exception>
        public IFontFactory Font
        {
            get
            {
                if (gameFontSystem.FontSystem == null)
                    throw new InvalidOperationException("The font system is not initialized yet");

                return gameFontSystem.FontSystem;
            }
        }

        /// <summary>
        /// Gets or sets the console log mode. See remarks.
        /// </summary>
        /// <value>The console log mode.</value>
        /// <remarks>
        /// Defines how the console will be displayed when running the game. By default, on Windows, It will open only on debug
        /// if there are any messages logged.
        /// </remarks>
        public ConsoleLogMode ConsoleLogMode
        {
            get
            {
                return logListener is ConsoleLogListener consoleLogListener ? consoleLogListener.LogMode : default;
            }
            set
            {
                if (logListener is ConsoleLogListener consoleLogListener)
                {
                    consoleLogListener.LogMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the default console log level.
        /// </summary>
        /// <value>The console log level.</value>
        public LogMessageType ConsoleLogLevel
        {
            get
            {
                return logListener is ConsoleLogListener consoleLogListener ? consoleLogListener.LogLevel : default;
            }
            set
            {
                if (logListener is ConsoleLogListener consoleLogListener)
                {
                    consoleLogListener.LogLevel = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the mouse should be visible.
        /// </summary>
        /// <value><c>true</c> if the mouse should be visible; otherwise, <c>false</c>.</value>
        public bool IsMouseVisible
        {
            get
            {
                return isMouseVisible;
            }
            set
            {
                isMouseVisible = value;
                Input?.SetMouseVisibilty(value);
            }
        }

        /// <summary>
        /// Automatically initializes game settings like default scene, resolution, graphics profile.
        /// </summary>
        public bool AutoLoadDefaultSettings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultGame"/> class.
        /// </summary>
        public DefaultGame(GamePlatform gamePlatform = null)
        {

            gamePlatform ??= new SDLGamePlatform();

            Services.AddService<GamePlatform>(gamePlatform);

            gamePlatform.Activated += GamePlatform_Activated;
            gamePlatform.Deactivated += GamePlatform_Deactivated;
            gamePlatform.Exiting += GamePlatform_Exiting;

            if (gamePlatform is IWindowedPlatform windowedPlatform)
            {
                windowedPlatform.WindowCreated += GamePlatformOnWindowCreated;
                Services.AddService(windowedPlatform);
            }

            GamePlatform = gamePlatform;

            // Initialize the GamePlatform with a valid IServiceRegistry
            GamePlatform.Initialize(Services);

            Services.AddService<IGraphicsDeviceFactory>(GamePlatform);
            Services.AddService<IGamePlatform>(GamePlatform);

            // Register the logger backend before anything else
            logListener = GetLogListener();

            if (logListener != null)
                GlobalLogger.GlobalMessageLogged += logListener;

            AutoLoadDefaultSettings = true;

            GraphicsDeviceManager = new GraphicsDeviceComponent();
            Components.Add(GraphicsDeviceManager);
        }

        /// <summary>
        /// Call this method to initialize the game, begin running the game loop, and start processing events for the game.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot run this instance while it is already running</exception>
        protected override void RunInit()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Cannot run this instance while it is already running");
            }

            // Create all core services, except Input which is created during `Initialize'.
            // Registration takes place in `Initialize'.
            Script = new ScriptSystem(Services);
            Services.AddService(Script);

            SceneSystem = new SceneSystem(Services);
            Services.AddService(SceneSystem);

            Streaming = new StreamingManager(Services);

            Audio = new AudioSystem(Services);
            Services.AddService(Audio);
            Services.AddService<IAudioEngineProvider>(Audio);

            gameFontSystem = new GameFontSystem(Services);
            Services.AddService(gameFontSystem.FontSystem);
            Services.AddService<IFontFactory>(gameFontSystem.FontSystem);

            SpriteAnimation = new SpriteAnimationSystem(Services);
            Services.AddService(SpriteAnimation);

            DebugTextSystem = new DebugTextSystem(Services);
            Services.AddService(DebugTextSystem);

            ProfilingSystem = new GameProfilingSystem(Services);
            Services.AddService(ProfilingSystem);

            VRDeviceSystem = new VRDeviceSystem(Services);
            Services.AddService(VRDeviceSystem);

            // Gets the graphics device manager
            graphicsDeviceManager = Services.GetService<IGraphicsDeviceManager>();
            ArgumentNullException.ThrowIfNull(graphicsDeviceManager, nameof(graphicsDeviceManager));

            PrepareContext();

            try
            {
                Window.CreateWindow(600, 900);
                Window.SetSize(new Int2(600, 900));

                // Register on Activated
                Window.Activated += OnActivated;
                Window.Deactivated += OnDeactivated;
                Window.InitCallback = OnInitCallback;
                Window.RunCallback = OnRunCallback;

                WindowCreated?.Invoke(this, EventArgs.Empty);

                // Handles the game loop.
                Window.Run();

                if (GamePlatform.IsBlockingRun)
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
        /// Calls <see cref="RawTick"/> automatically based on this game's setup, override it to implement your own system.
        /// </summary>
        protected override void RawTickProducer()
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

                if (GamePlatform.IsBlockingRun) // throttle fps if Game.Tick() called from internal main loop
                {
                    if (Window.IsMinimized || Window.Visible == false || (Window.Focused == false && TreatNotFocusedLikeMinimized))
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

        protected override void OnExit()
        {
            // Notifies that the GameWindow should exit.
            Window.Exiting = true;
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            OnGameDestroyed(this);

            DestroyAssetDatabase();

            if (Window != null && Window.IsActivated) // force the window to be in an correct state during destroy (Deactivated events are sometimes dropped on windows)
            {
                Window.OnPause();
            }

            base.Destroy();

            GamePlatform?.Release();

            if (logListener != null)
                GlobalLogger.GlobalMessageLogged -= logListener;
        }

        /// <inheritdoc/>
        protected override void PrepareContext()
        {
            base.PrepareContext();

            // Init assets
            databaseFileProvider = InitializeAssetDatabase();
            ((DatabaseFileProviderService)Services.GetService<IDatabaseFileProviderService>()).FileProvider = databaseFileProvider;

            var renderingSettings = new RenderingSettings();
            if (Content.Exists(GameSettings.AssetUrl))
            {
                Settings = Content.Load<GameSettings>(GameSettings.AssetUrl);

                renderingSettings = Settings.Configurations.Get<RenderingSettings>();

                // Set ShaderProfile even if AutoLoadDefaultSettings is false (because that is what shaders in effect logs are compiled against, even if actual instantiated profile is different)
                if (renderingSettings.DefaultGraphicsProfile > 0)
                {
                    var deviceManager = GraphicsDeviceManager;
                    if (!deviceManager.ShaderProfile.HasValue)
                        deviceManager.ShaderProfile = renderingSettings.DefaultGraphicsProfile;
                }

                Services.AddService<IGameSettingsService>(this);
            }

            // Load several default settings
            if (AutoLoadDefaultSettings)
            {
                var deviceManager = GraphicsDeviceManager;
                if (renderingSettings.DefaultGraphicsProfile > 0)
                {
                    deviceManager.PreferredGraphicsProfile = [renderingSettings.DefaultGraphicsProfile];
                }

                if (renderingSettings.DefaultBackBufferWidth > 0) deviceManager.PreferredBackBufferWidth = renderingSettings.DefaultBackBufferWidth;
                if (renderingSettings.DefaultBackBufferHeight > 0) deviceManager.PreferredBackBufferHeight = renderingSettings.DefaultBackBufferHeight;

                deviceManager.PreferredColorSpace = renderingSettings.ColorSpace;
                SceneSystem.InitialSceneUrl = Settings?.DefaultSceneUrl;
                SceneSystem.InitialGraphicsCompositorUrl = Settings?.DefaultGraphicsCompositorUrl;
                SceneSystem.SplashScreenUrl = Settings?.SplashScreenUrl;
                SceneSystem.SplashScreenColor = Settings?.SplashScreenColor ?? Color4.Black;
                SceneSystem.DoubleViewSplashScreen = Settings?.DoubleViewSplashScreen ?? false;
            }
        }

        public override void ConfirmRenderingSettings(bool gameCreation)
        {
            if (!AutoLoadDefaultSettings) return;

            var renderingSettings = Settings?.Configurations.Get<RenderingSettings>();

            var deviceManager = GraphicsDeviceManager;

            if (gameCreation)
            {
                //if our device width or height is actually smaller then requested we use the device one
                deviceManager.PreferredBackBufferWidth = Math.Min(deviceManager.PreferredBackBufferWidth, Window.ClientBounds.Width);
                deviceManager.PreferredBackBufferHeight = Math.Min(deviceManager.PreferredBackBufferHeight, Window.ClientBounds.Height);
            }

            //these might get triggered even during game runtime, resize, orientation change
            if (renderingSettings != null && renderingSettings.AdaptBackBufferToScreen)
            {
                var deviceAr = Window.ClientBounds.Width / (float)Window.ClientBounds.Height;

                if (deviceManager.PreferredBackBufferHeight > deviceManager.PreferredBackBufferWidth)
                {
                    deviceManager.PreferredBackBufferWidth = (int)(deviceManager.PreferredBackBufferHeight * deviceAr);
                }
                else
                {
                    deviceManager.PreferredBackBufferHeight = (int)(deviceManager.PreferredBackBufferWidth / deviceAr);
                }
            }
        }

        protected override void Initialize()
        {
            // ---------------------------------------------------------
            // Add common GameSystems - Adding order is important
            // (Unless overriden by gameSystem.UpdateOrder)
            // ---------------------------------------------------------

            // Add the input manager
            // Add it first so that it can obtained by the UI system
            var inputSystem = new InputSystem(Services);
            Input = inputSystem.Manager;
            Services.AddService(Input);
            GameSystems.Add(inputSystem);

            // Initialize the systems
            base.Initialize();

            Content.Serializer.LowLevelSerializerSelector = ParameterContainerExtensions.DefaultSceneSerializerSelector;

            // Add the scheduler system
            // - Must be after Input, so that scripts are able to get latest input
            // - Must be before Entities/Camera/Audio/UI, so that scripts can apply
            // changes in the same frame they will be applied
            GameSystems.Add(Script);

            // Add the Font system
            GameSystems.Add(gameFontSystem);

            //Add the sprite animation System
            GameSystems.Add(SpriteAnimation);

            GameSystems.Add(DebugTextSystem);
            GameSystems.Add(ProfilingSystem);

            EffectSystem = new EffectSystem(Services);
            Services.AddService(EffectSystem);

            // If requested in game settings, compile effects remotely and/or notify new shader requests
            EffectSystem.Compiler = EffectCompilerFactory.CreateEffectCompiler(Content.FileProvider, EffectSystem, Settings?.PackageName, Settings?.EffectCompilation ?? EffectCompilationMode.Local, Settings?.RecordUsedEffects ?? false);

            // Setup shader compiler settings from a compilation mode. 
            // TODO: We might want to provide overrides on the GameSettings to specify debug and/or optim level specifically.
            if (Settings != null)
                EffectSystem.SetCompilationMode(Settings.CompilationMode);

            GameSystems.Add(EffectSystem);

            if (Settings != null)
                Streaming.SetStreamingSettings(Settings.Configurations.Get<StreamingSettings>());
            GameSystems.Add(Streaming);
            GameSystems.Add(SceneSystem);

            // Add the Audio System
            GameSystems.Add(Audio);

            // Add the VR System
            GameSystems.Add(VRDeviceSystem);

            // TODO: data-driven?
            Content.Serializer.RegisterSerializer(new ImageSerializer());

            var sdlWindow = (GameWindowSDL)Window;
            var input = new InputSourceSDL(sdlWindow.Window);
            Input.Sources.Add(input);

            OnGameStarted(this);
        }

        internal static DatabaseFileProvider InitializeAssetDatabase()
        {
            using (Profiler.Begin(GameProfilingKeys.ObjectDatabaseInitialize))
            {
                // Create and mount database file system
                var objDatabase = ObjectDatabase.CreateDefaultDatabase();

                // Only set a mount path if not mounted already
                var mountPath = VirtualFileSystem.ResolveProviderUnsafe("/asset", true).Provider == null ? "/asset" : null;
                var result = new DatabaseFileProvider(objDatabase, mountPath);

                return result;
            }
        }

        private void DestroyAssetDatabase()
        {
            if (databaseFileProvider != null)
            {
                if (Services.GetService<IDatabaseFileProviderService>() is DatabaseFileProviderService dbfp)
                    dbfp.FileProvider = null;
                databaseFileProvider.Dispose();
                databaseFileProvider = null;
            }
        }

        protected override void EndDraw(bool present)
        {
            // Allow to make a screenshot using CTRL+c+F12 (on release of F12)
            if (Input.HasKeyboard)
            {
                if (Input.IsKeyDown(Keys.LeftCtrl)
                    && Input.IsKeyDown(Keys.C)
                    && Input.IsKeyReleased(Keys.F12))
                {
                    var currentFilePath = PlatformFolders.ApplicationExecutablePath;
                    var timeNow = DateTime.Now.ToString("s", CultureInfo.InvariantCulture).Replace(':', '_');
                    var newFileName = Path.Combine(
                        Path.GetDirectoryName(currentFilePath),
                        Path.GetFileNameWithoutExtension(currentFilePath) + "_" + timeNow + ".png");

                    Console.WriteLine("Saving screenshot: {0}", newFileName);

                    using (var stream = System.IO.File.Create(newFileName))
                    {
                        GraphicsDevice.Presenter.BackBuffer.Save(GraphicsContext.CommandList, stream, ImageFileType.Png);
                    }
                }
            }
            base.EndDraw(present);
        }

        /// <summary>
        /// Loads the content.
        /// </summary>
        protected virtual Task LoadContent()
        {
            return Task.FromResult(true);
        }

        internal override void LoadContentInternal()
        {
            base.LoadContentInternal();
            Script.AddTask(LoadContent);
        }

        protected virtual LogListener GetLogListener()
        {
            return new ConsoleLogListener();
        }

        private static void OnGameStarted(DefaultGame game)
        {
            GameStarted?.Invoke(game, null);
        }

        private static void OnGameDestroyed(DefaultGame game)
        {
            GameDestroyed?.Invoke(game, null);
        }

        protected void OnWindowCreated()
        {
            WindowCreated?.Invoke(this, EventArgs.Empty);
        }

        private void GamePlatformOnWindowCreated(object sender, EventArgs eventArgs)
        {
            OnWindowCreated();
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
    }
}
