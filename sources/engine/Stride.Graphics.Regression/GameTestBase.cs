// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Graphics.Regression
{
    /// <summary>
    ///   Provides a base class for creating and running game tests.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <see cref="GameTestBase"/> class is designed to facilitate the creation of automated game tests
    ///     by providing a framework for managing test execution, capturing screenshots, and simulating input.
    ///   </para>
    ///   This class extends the <see cref="Game"/> class and includes additional functionality for:
    ///   <list type="bullet">
    ///     <item>Registering and executing tests at specific frames using the <see cref="FrameGameSystem"/>.</item>
    ///     <item>Saving images (e.g., textures or back-buffers) for comparison in regression tests.</item>
    ///     <item>Simulating input sources (keyboard and mouse) for automated testing.</item>
    ///     <item>Configuring graphics settings and managing test artifacts.</item>
    ///   </list>
    ///   Derived classes can override the <see cref="RegisterTests"/> method to define their own test logic.
    /// </remarks>
    public abstract class GameTestBase : Game
    {
        /// <summary>
        ///   Gets the logger instance used for logging test game-related messages.
        /// </summary>
        public static Logger TestGameLogger { get; } = GlobalLogger.GetLogger(nameof(TestGameLogger));

        /// <summary>
        ///   Gets or sets a value indicating whether to force interactive mode, i.e. a mode
        ///   where the Game does not use a simulated input source and the user is responsible
        ///   for manipulating the test Game and take screenshots if appropriate.
        /// </summary>
        public static bool ForceInteractiveMode { get; set; }

        /// <summary>
        /// Force image to be saved even if image comparison was a success and no error during the test.
        /// </summary>
        public static bool ForceSaveImageOnSuccess { get; set; }


        /// <summary>
        ///   Gets the instance of the <see cref="FrameGameSystem"/> where the tests
        ///   can be registered to be executed at specific frames, and where screenshots
        ///   can be scheduled to be taken.
        /// </summary>
        public FrameGameSystem FrameGameSystem { get; }

        /// <summary>
        ///   Gets or sets the frame count at which the Game should stop. When this frame
        ///   is reached in the <see cref="Update"/> method, the Game will exit.
        /// </summary>
        public int StopOnFrameCount { get; set; }

        /// <summary>
        ///   Maximum per-channel color difference (0-255) allowed when comparing images.
        ///   Default is 2. Increase for tests with expected minor numerical differences.
        /// </summary>
        public int ImageComparisonTolerance { get; set; } = 2;

        /// <summary>
        ///   Gets or sets the name of the test. It will be reflected in the saved images.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        ///   Gets the input source used for testing or emulating input behavior.
        /// </summary>
        public InputSourceSimulated InputSourceSimulated { get; private set; }
        /// <summary>
        ///   Gets an object that can be used to simulate mouse input.
        /// </summary>
        public MouseSimulated MouseSimulated { get; private set; }
        /// <summary>
        ///   Gets an object that can be used to simulate keyboard input.
        /// </summary>
        public KeyboardSimulated KeyboardSimulated { get; private set; }

        /// <summary>
        ///   Gets the index of the current frame.
        /// </summary>
        public int FrameIndex { get; private set; }

        private bool screenshotAutomationEnabled;
        private readonly List<string> comparisonMissingMessages = [];
        private readonly List<string> comparisonFailedMessages = [];

        private BackBufferSizeMode backBufferSizeMode;

#if STRIDE_PLATFORM_DESKTOP
        /// <summary>
        ///   Controls RenderDoc capture behavior for tests. Defaults to the value of the
        ///   <c>STRIDE_TESTS_RENDERDOC</c> environment variable (lower-cased) and can be
        ///   overridden at runtime by the interactive test runner.
        ///   <list type="bullet">
        ///     <item><c>error</c> — capture frames only for failing tests (discard on success)</item>
        ///     <item><c>always</c> — capture frames for all tests</item>
        ///     <item>anything else / <see langword="null"/> — no capture</item>
        ///   </list>
        /// </summary>
        public static string RenderDocMode { get; set; } =
            Environment.GetEnvironmentVariable("STRIDE_TESTS_RENDERDOC")?.ToLowerInvariant();

        private static bool CaptureRenderDocOnError => RenderDocMode is "error" or "always";
        private static bool ForceCaptureRenderDocOnSuccess => RenderDocMode is "always";

        private RenderDocManager renderDocManager;
#endif

        /// <summary>
        ///   Initializes a new instance of the <see cref="GameTestBase"/> class,
        ///   setting up the default graphics device manager and game systems for testing purposes.
        /// </summary>
        /// <remarks>
        ///   This constructor configures the game environment for testing by overriding the
        ///   default Graphics Device manager with a custom implementation and setting up essential
        ///   services and systems.
        ///   The Graphics Device settings are set to a Back-Buffer size of 800 by 480 pixels,
        ///   a 24-bit Depth Buffer with a 8-bit Stencil Buffer, and it is configured with the
        ///   <see cref="DeviceCreationFlags.Debug"/> flag and the <see cref="GraphicsProfile.Level_9_1"/>
        ///   profile.
        /// </remarks>
        protected GameTestBase()
        {
            ConsoleLogMode = ConsoleLogMode.Always;

            // Override the default Graphic Device manager and settings
            GraphicsDeviceManager.Dispose();
            GraphicsDeviceManager = new TestGraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 800,
                PreferredBackBufferHeight = 480,
                PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt,
#if DEBUG
                DeviceCreationFlags = DeviceCreationFlags.Debug,
#endif
                PreferredGraphicsProfile = [ GraphicsProfile.Level_9_1 ]
            };
            Services.AddService<IGraphicsDeviceManager>(GraphicsDeviceManager);
            Services.AddService<IGraphicsDeviceService>(GraphicsDeviceManager);

            StopOnFrameCount = -1;
            AutoLoadDefaultSettings = false;

            FrameGameSystem = new FrameGameSystem(Services);
            GameSystems.Add(FrameGameSystem);

            // by default we want the same size for the back buffer on mobiles and windows.
            BackBufferSizeMode = BackBufferSizeMode.FitToDesiredValues;

            // Only make window visible in interactive mode,
            // otherwise it's quite disrupting for user: new window might display on top and steal focus
            MakeWindowVisibleOnRun = ForceInteractiveMode;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

#if STRIDE_PLATFORM_DESKTOP
            if (CaptureRenderDocOnError)
            {
                renderDocManager = new RenderDocManager();
                if (!renderDocManager.IsInitialized)
                    renderDocManager = null;
            }
#endif

            // Disable streaming
            Streaming.Enabled = false;

            // Enable profiling
            //Profiler.EnableAll();

            // Disable splash screen
            SceneSystem.SplashScreenEnabled = false;
        }

        /// <summary>
        ///   Saves a Texture and compares it against the gold reference image.
        /// </summary>
        /// <param name="textureToSave">The <see cref="Texture"/> to save.</param>
        /// <param name="testName">
        ///   An optional name for the test that is wanting to save the <paramref name="textureToSave"/>.
        /// </param>
        /// <remarks>
        ///   A test Game can call this method to save any <see cref="Texture"/> it wants
        ///   so it can be compared to a reference image. This is useful for regression tests.
        /// </remarks>
        public void SaveImage(Texture textureToSave, string? testName = null)
        {
            if (textureToSave is null)
                return;

            TestGameLogger.Info(@"Saving image");

            using var image = textureToSave.GetDataAsImage(GraphicsContext.CommandList);
            SaveImage(image, testName);
        }

        /// <summary>
        ///   Saves the Back-Buffer and compares it against the gold reference image.
        /// </summary>
        /// <param name="testName">
        ///   An optional name for the test that is wanting to save the Back-Buffer.
        /// </param>
        /// <remarks>
        ///   A test Game can call this method to save the Back-Buffer
        ///   so it can be compared to a reference image. This is useful for regression tests.
        /// </remarks>
        public void SaveBackBuffer(string? testName = null)
        {
            TestGameLogger.Info(@"Saving the Back-Buffer");

            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            using var image = backBuffer.GetDataAsImage(GraphicsContext.CommandList);
            SaveImage(image, testName);
        }

        /// <summary>
        ///   Gets or sets the value indicating if the screenshots automation is enabled.
        /// </summary>
        /// <value>
        ///   <list type="bullet">
        ///     <item>
        ///       If <see langword="true"/>, the screenshots automation is enabled. The game
        ///       can use <see cref="FrameGameSystem"/> to schedule screenshots and run tests
        ///       at specific frames.
        ///     </item>
        ///     <item>
        ///       If <see langword="false"/>, the screenshots automation is disabled.
        ///     </item>
        ///   </list>
        /// </value>
        public bool ScreenShotAutomationEnabled
        {
            get => screenshotAutomationEnabled;
            set
            {
                FrameGameSystem.Enabled = value;      // No Update
                FrameGameSystem.Visible = value;      // No Draw
                screenshotAutomationEnabled = value;
            }
        }

        /// <summary>
        ///   Gets or sets the mode used to determine the size of the Back-Buffer.
        /// </summary>
        public BackBufferSizeMode BackBufferSizeMode
        {
            get => backBufferSizeMode;
            set => backBufferSizeMode = value;
        }

        /// <summary>
        ///   Enables the use of a simulated input source (keyboard and mouse) for tests.
        /// </summary>
        /// <remarks>
        ///   This method can be used to switch the system to use a simulated input source
        ///   instead of a real one. This allows tests to simulate user input programmatically,
        ///   like sending mouse clicks or key presses.
        /// </remarks>
        protected internal void EnableSimulatedInputSource()
        {
            InputSourceSimulated = new InputSourceSimulated();
            if (Input is not null)
                InitializeSimulatedInputSource();
        }

        /// <summary>
        ///   Initializes and configures a <strong>simulated input source</strong> as only active input source,
        ///   enabling the use of simulated mouse and keyboard devices for testing purposes.
        /// </summary>
        private void InitializeSimulatedInputSource()
        {
            if (InputSourceSimulated is not null)
            {
                Input.Sources.Clear();
                Input.Sources.Add(InputSourceSimulated);

                MouseSimulated = InputSourceSimulated.AddMouse();
                KeyboardSimulated = InputSourceSimulated.AddKeyboard();
            }
        }

        protected override void OnWindowCreated()
        {
            base.OnWindowCreated();

            // Disabled for SDL as a position of (0,0) actually means that the client area of the
            // window will be at (0,0) not the top left corner of the non-client area of the window.
            if (Context.ContextType != AppContextType.DesktopSDL)
                Window.Position = Int2.Zero; // avoid possible side effects due to position of the window in the screen.
        }

        /// <inheritdoc/>
        protected override async Task LoadContent()
        {
            await base.LoadContent();

#if STRIDE_PLATFORM_DESKTOP
            // Setup RenderDoc capture
            if (renderDocManager is not null)
            {
                var localTestsDir = Path.Combine(FindStrideSolutionRootDirectory(), @"tests\local");
                var renderdocCaptureFile = GenerateTestArtifactFileName(testArtifactPath: localTestsDir,
                                                                        frameName: null,
                                                                        platformSpecificDir: GetPlatformSpecificDirectory(),
                                                                        extension: ".rdc");
                renderDocManager.Initialize(renderdocCaptureFile);
                renderDocManager.StartFrameCapture(GraphicsDevice, hwndPtr: IntPtr.Zero);
            }
#endif

            if (!ForceInteractiveMode)
                InitializeSimulatedInputSource();

            Script.AddTask(RegisterTestsInternal);

            //
            // Registers the tests specified by a derived class if not already in feeding mode.
            //
            Task RegisterTestsInternal()
            {
                if (!FrameGameSystem.IsUnitTestFeeding)
                    RegisterTests();

                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            // NOTE: We don't remove RenderDoc hooks in case another unit test needs them later
            //renderDocManager.RemoveHooks();

            base.Destroy();
        }

#if STRIDE_PLATFORM_DESKTOP
        /// <summary>
        ///   Ends the current frame RenderDoc capture session, finalizing the capture of rendering data,
        ///   allowing the captured data to be saved or processed.
        /// </summary>
        /// <remarks>A frame capture session was previously started before calling this method.</remarks>
        private void EndFrameCapture()
        {
            renderDocManager?.EndFrameCapture(GraphicsDevice, IntPtr.Zero);
        }

        /// <summary>
        ///   Ends the current frame RenderDoc capture session, discarding any captured rendering data.
        /// </summary>
        /// <remarks>A frame capture session was previously started before calling this method.</remarks>
        private void DiscardFrameCapture()
        {
            renderDocManager?.DiscardFrameCapture(GraphicsDevice, IntPtr.Zero);
        }

        /// <summary>
        ///   Ends or discards the RenderDoc capture based on test results.
        ///   Must be called while the graphics device is still alive.
        /// </summary>
        internal void EndOrDiscardRenderDocCapture()
        {
            if (renderDocManager is null || !CaptureRenderDocOnError)
                return;

            if (comparisonFailedMessages.Count == 0 &&
                comparisonMissingMessages.Count == 0 &&
                !ForceCaptureRenderDocOnSuccess)
            {
                DiscardFrameCapture();
            }
            else
            {
                EndFrameCapture();
            }
        }
#endif

        /// <inheritdoc/>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (gameTime.FrameCount == StopOnFrameCount)
            {
                Exit();
            }
        }

        /// <inheritdoc/>
        /// <summary>
        /// Loop through all the tests and save the images.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsContext.CommandList.BeginProfile(Color.Orange, $"Frame #{gameTime.FrameCount}");
            try
            {
                base.Draw(gameTime);
            }
            finally
            {
                GraphicsContext.CommandList.EndProfile();
            }

            // If allowed, take any scheduled screenshot
            if (!ScreenShotAutomationEnabled)
                return;

            if (FrameGameSystem.AllTestsCompleted)
                Exit();
            else if (FrameGameSystem.IsScreenshotNeeded(out string? testName))
                SaveBackBuffer(testName);
        }

        /// <summary>
        ///   Executes a test action within the context of a game test environment.
        /// </summary>
        /// <param name="testAction">
        ///   The action to perform during the test in the <see cref="Draw"/> method.
        ///   This action receives an instance of <see cref="GameTestBase"/> to interact with the test environment.
        /// </param>
        /// <param name="profileOverride">
        ///   An optional graphics profile to override the default graphics settings.
        ///   If specified, the test will use the provided <see cref="GraphicsProfile"/>.
        /// </param>
        /// <param name="takeSnapshot">
        ///   A value indicating whether to capture a screenshot once the test has finished.
        ///   <see langword="true"/> to take a screenshot; otherwise, <see langword="false"/>.
        /// </param>
        protected void PerformTest(Action<GameTestBase> testAction,
                                   GraphicsProfile? profileOverride = null,
                                   bool takeSnapshot = false)
        {
            // Create a new test game instance
            var typeGame = GetType();
            var game = (GameTestBase) Activator.CreateInstance(typeGame);

            if (profileOverride.HasValue)
                game.GraphicsDeviceManager.PreferredGraphicsProfile = [ profileOverride.Value ];

            // Register the tests to be executed
            game.FrameGameSystem.IsUnitTestFeeding = true;
            game.FrameGameSystem.Draw(() => testAction(game));
            if (takeSnapshot)
                game.FrameGameSystem.TakeScreenshot();

            // Run the test
            RunGameTest(game);
        }

        /// <summary>
        ///   Executes a rendering test within the context of a game test environment.
        /// </summary>
        /// <param name="drawTestAction">
        ///   The action to perform during the rendering test in the <see cref="Draw"/> method.
        ///   This action receives an instance of <see cref="GameTestBase"/> and the rendering context
        ///   to interact with the test environment.
        /// </param>
        /// <param name="profileOverride">
        ///   An optional graphics profile to override the default graphics settings.
        ///   If specified, the test will use the provided <see cref="GraphicsProfile"/>.
        /// </param>
        /// <param name="subTestName">
        ///   An optional name for the sub-test, used to differentiate between multiple test cases.
        ///   Can be <see langword="null"/>.
        /// </param>
        /// <param name="takeSnapshot">
        ///   A value indicating whether to capture a screenshot once the test has finished.
        ///   <see langword="true"/> to take a screenshot; otherwise, <see langword="false"/>.
        /// </param>
        /// <remarks>
        ///   This method initializes a new test game instance, sets up an empty scene, and configures
        ///   a very simple graphics compositor to use the provided rendering callback.
        /// </remarks>
        protected void PerformDrawTest(Action<GameTestBase, RenderDrawContext> drawTestAction,
                                       GraphicsProfile? profileOverride = null,
                                       string? subTestName = null,
                                       bool takeSnapshot = true)
        {
            // Create a new test game instance
            var typeGame = GetType();
            var game = (GameTestBase) Activator.CreateInstance(typeGame);

            if (profileOverride.HasValue)
                game.GraphicsDeviceManager.PreferredGraphicsProfile = [profileOverride.Value];

            // Register the tests to be executed
            game.FrameGameSystem.IsUnitTestFeeding = true;
            if (takeSnapshot)
                game.FrameGameSystem.TakeScreenshot();

            // Setup an empty scene
            var scene = new Scene();
            game.SceneSystem.SceneInstance = new SceneInstance(Services, scene);

            // Add the render callback to a simple graphics compositor
            game.SceneSystem.GraphicsCompositor = new GraphicsCompositor
            {
                Game = new DelegateSceneRenderer(context => drawTestAction(game, context)),
            };

            // Run the test
            RunGameTest(game);
        }

        /// <summary>
        ///   Method called to register the tests that will be executed by the test game.
        /// </summary>
        /// <remarks>
        ///   Derived classes can override this method to register their own tests using the
        ///   <see cref="FrameGameSystem"/>, where they can schedule methods to be executed
        ///   at specific frames, and schedule screenshots to be taken.
        /// </remarks>
        protected virtual void RegisterTests()
        {
        }

        /// <summary>
        ///   Executes a game test using the specified <see cref="GameTestBase"/> instance.
        /// </summary>
        /// <param name="game">The game test instance to run. Must not be <see langword="null"/>.</param>
        protected static void RunGameTest(GameTestBase game, [CallerMemberName] string callerName = null)
        {
            game.EnableSimulatedInputSource();

            game.ScreenShotAutomationEnabled = !ForceInteractiveMode;

            // Collect allowed GPU validation errors from [AllowGpuValidationError] attributes on the calling test method and class
            var allowedErrors = new List<AllowGpuValidationErrorAttribute>();
            if (callerName != null)
            {
                var callerType = new System.Diagnostics.StackTrace().GetFrames()
                    .Select(f => f.GetMethod())
                    .FirstOrDefault(m => m?.Name == callerName)?.DeclaringType;
                if (callerType != null)
                {
                    // Class-level attributes
                    allowedErrors.AddRange(callerType.GetCustomAttributes<AllowGpuValidationErrorAttribute>(true));
                    // Method-level attributes
                    var method = callerType.GetMethod(callerName);
                    if (method != null)
                        allowedErrors.AddRange(method.GetCustomAttributes<AllowGpuValidationErrorAttribute>(true));
                }
            }

            // Track GPU validation errors and warnings during the test
            var gpuValidationErrors = new List<string>();
            var gpuValidationWarnings = new List<string>();
            void OnGlobalMessage(ILogMessage msg)
            {
                if (msg.Module != GraphicsDevice.DebugLogModule)
                    return;
                if (msg.Type == LogMessageType.Error)
                    gpuValidationErrors.Add(msg.Text);
                else if (msg.Type == LogMessageType.Warning && !IsKnownHarmlessWarning(msg.Text))
                    gpuValidationWarnings.Add(msg.Text);
            }
            GlobalLogger.GlobalMessageLogged += OnGlobalMessage;

            try
            {
                GameTester.RunGameTest(game);
            }
            finally
            {
                GlobalLogger.GlobalMessageLogged -= OnGlobalMessage;
            }

            // Filter out allowed errors for the current platform
            var platform = GraphicsDevice.Platform;
            gpuValidationErrors.RemoveAll(error =>
                allowedErrors.Any(a => a.Platform == platform && error.Contains(a.MessageSubstring)));

            // Assert no GPU validation errors
            if (gpuValidationErrors.Count > 0)
                Assert.Fail($"GPU validation reported {gpuValidationErrors.Count} error(s):" + Environment.NewLine
                    + string.Join(Environment.NewLine, gpuValidationErrors));

            // Assert no GPU validation warnings
            if (gpuValidationWarnings.Count > 0)
                Assert.Fail($"GPU validation reported {gpuValidationWarnings.Count} warning(s):" + Environment.NewLine
                    + string.Join(Environment.NewLine, gpuValidationWarnings));

            // If there were comparison failures, assert them now
            if (game.ScreenShotAutomationEnabled)
            {
                if (game.comparisonFailedMessages.Count > 0)
                    AssertImageComparisonFailed();
                if (game.comparisonMissingMessages.Count > 0)
                    AssertMissingComparisonImages();

                [DoesNotReturn]
                void AssertImageComparisonFailed()
                {
                    var failedImages = string.Join(Environment.NewLine, game.comparisonFailedMessages);
                    Assert.Fail("Image comparison failed — review the diff, it may be a regression:" + Environment.NewLine
                        + failedImages + Environment.NewLine + GoldHelp(game.GetType(), suggestPromote: false));
                }

                [DoesNotReturn]
                void AssertMissingComparisonImages()
                {
                    var missingImages = string.Join(Environment.NewLine, game.comparisonMissingMessages);
                    Assert.Fail("No gold reference yet for these images:" + Environment.NewLine
                        + missingImages + Environment.NewLine + GoldHelp(game.GetType(), suggestPromote: true));
                }
            }
        }

        /// <summary>
        ///   Filters out known harmless D3D11 debug layer warnings that cannot be avoided
        ///   without significant refactoring.
        /// </summary>
        private static bool IsKnownHarmlessWarning(string text)
        {
            return text.Contains("Live ");                        // D3D11/D3D12: ReportLiveObjects at shutdown
        }

        // Help text appended to gold comparison failures: a CompareGold pointer plus a ready
        // `gh workflow run test-gold-gen` command pre-filtered to the failing test class, with the
        // broader whole-suite / everything scopes shown too.
        private static string GoldHelp(Type testType, bool suggestPromote)
        {
            var (repo, branch) = RepoAndBranch.Value;
            // Always emit both flags: if a value couldn't be resolved, leave an obvious placeholder
            // to fill in (better than dropping the flag and letting gh silently pick the wrong repo /
            // default branch).
            var ctx = $" --repo {(string.IsNullOrEmpty(repo) ? "<owner>/stride" : repo)}"
                    + $" --ref {(string.IsNullOrEmpty(branch) ? "<branch>" : branch)}";
            var promote = suggestPromote ? " -f update-gold=auto" : "";

            // Build only this suite's project (not the whole solution). The csproj is named by the
            // assembly (unlike the namespace-based test filter); emitted only when it actually exists.
            var assembly = testType.Assembly.GetName().Name;
            var projectRel = $"sources/engine/{assembly}/{assembly}.csproj";
            var project = "";
            try { if (File.Exists(Path.Combine(GetTestsRootDirectory(), projectRel))) project = $" -f project={projectRel}"; }
            catch { /* root not resolvable (e.g. Android) — omit, builds all */ }

            return $"Regenerate gold on CI (or review/promote locally with CompareGold -- tests/compare-gold.cmd):{Environment.NewLine}"
                 + $"  gh workflow run test-gold-gen.yml{ctx}{project} -f test-filter='FullyQualifiedName~{testType.FullName}'{promote}{Environment.NewLine}"
                 + $"  (widen: trim -f test-filter to a namespace; remove -f project to span all assemblies)";
        }

        // Resolved once per process: repo/branch don't change within a run, and the lookup shells
        // out to git up to four times — not worth repeating for every test's failure message.
        private static readonly Lazy<(string Repo, string Branch)> RepoAndBranch = new(ResolveRepoAndBranch);

        // --repo/--ref for the gh command. On CI the env vars are authoritative. Locally we fill them
        // from the checkout's push remote + current branch: `gh workflow run` otherwise defaults
        // --ref to the repo's *default* branch (not the one you're on), so the pasted command would
        // silently target master. Any lookup failure leaves a fill-in placeholder.
        private static (string Repo, string Branch) ResolveRepoAndBranch()
        {
            var repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
            var branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
            if (!string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(branch))
                return (repo, branch);

            try
            {
                var root = GetTestsRootDirectory();
                if (string.IsNullOrEmpty(branch))
                {
                    var b = RunGit(root, "rev-parse --abbrev-ref HEAD");
                    if (!string.IsNullOrEmpty(b) && b != "HEAD")    // omit on detached HEAD
                        branch = b;
                }
                if (string.IsNullOrEmpty(repo))
                {
                    // Resolve the remote this branch *pushes to* (fork-aware), following git's own
                    // precedence, not just origin: someone working from a fork pushes feature
                    // branches there, so that's where `gh workflow run --ref <branch>` must dispatch.
                    var remote = "";
                    if (!string.IsNullOrEmpty(branch))
                        remote = FirstNonEmpty(
                            RunGit(root, $"config branch.{branch}.pushRemote"),
                            RunGit(root, "config remote.pushDefault"),
                            RunGit(root, $"config branch.{branch}.remote"));

                    if (string.IsNullOrEmpty(remote))
                    {
                        // No push tracking. A lone remote is unambiguous; with several we can't tell
                        // which fork this branch belongs to, so leave the owner as a "<owner>"
                        // placeholder rather than guess origin (which may be upstream, not the fork).
                        var remotes = RunGit(root, "remote")
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (remotes.Length == 1)
                            remote = remotes[0];
                    }

                    if (!string.IsNullOrEmpty(remote))
                    {
                        var m = Regex.Match(RunGit(root, $"remote get-url {remote}"),
                            @"github\.com[:/](?<slug>[^/]+/[^/]+?)(?:\.git)?/?\s*$");
                        if (m.Success)
                            repo = m.Groups["slug"].Value;
                    }
                }
            }
            catch { /* git unavailable / not a repo (e.g. Android) — leave gh to deduce */ }

            return (repo ?? "", branch ?? "");
        }

        private static string RunGit(string root, string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", $"-C \"{root}\" {args}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null)
                return "";
            var output = p.StandardOutput.ReadToEnd().Trim();
            if (!p.WaitForExit(2000))
            {
                try { p.Kill(); } catch { }
                return "";
            }
            return p.ExitCode == 0 ? output : "";
        }

        private static string FirstNonEmpty(params string[] values)
            => values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";

        /// <summary>
        ///   Gets the root directory that contains the <c>tests</c> folder with reference and local images.
        /// </summary>
        /// <remarks>
        ///   On desktop this is the Stride solution root, located by traversing upward for <c>build/Stride.sln</c>.
        ///   On Android it is the app's internal files directory (Context.FilesDir): targetSdk 30+ scoped
        ///   storage blocks the app's own writes through the FUSE-bound external-files path. The host
        ///   script pushes gold + pulls generated images via <c>adb shell run-as &lt;pkg&gt;</c>.
        /// </remarks>
        private static string GetTestsRootDirectory()
        {
#if STRIDE_PLATFORM_ANDROID
            return Android.App.Application.Context.FilesDir!.AbsolutePath;
#else
            return FindStrideSolutionRootDirectory();
#endif
        }

#if !STRIDE_PLATFORM_ANDROID
        /// <summary>
        ///   Searches for the root folder of the Stride solution by traversing upward from the test's binary directory.
        /// </summary>
        /// <returns>The full path to the root folder of the Stride solution.</returns>
        /// <exception cref="InvalidOperationException">The Stride solution root folder could not be located.</exception>
        private static string FindStrideSolutionRootDirectory()
        {
            var startDir = PlatformFolders.ApplicationBinaryDirectory;
            ImageTester.DiagLog($"FindRoot: AppContext.BaseDirectory={AppContext.BaseDirectory}");
            ImageTester.DiagLog($"FindRoot: ApplicationBinaryDirectory={startDir}");

            var dir = startDir;
            while (dir is not null)
            {
                var candidate = Path.Combine(dir, "build", "Stride.sln");
                if (File.Exists(candidate))
                {
                    ImageTester.DiagLog($"FindRoot: Found root={dir}");
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            ImageTester.DiagLog($"FindRoot: FAILED from {startDir}");
            throw new InvalidOperationException($"Could not locate the Stride solution root directory (started from {startDir})");
        }
#endif

        /// <summary>
        ///   Compares the test result image against the gold reference and saves a local copy
        ///   when no match is found (or when <see cref="ForceSaveImageOnSuccess"/> is set).
        /// </summary>
        /// <param name="image">The Image to compare and save.</param>
        /// <param name="testName">
        ///   An optional name for the test. If not provided, the frame index will be used.
        /// </param>
        public void SaveImage(Image image, string? testName)
        {
            // Use the test name, or the frame index if no name provided
            var frameName = testName;
            if (frameName is null && FrameIndex++ > 0)
                frameName = "f" + (FrameIndex - 1);

            // Register 3D card name
            // TODO: This doesn't work well because ImageTester.ImageTestResultConnection is static, this will need improvements
            //if (!ImageTester.ImageTestResultConnection.DeviceName.Contains("_"))
            //    ImageTester.ImageTestResultConnection.DeviceName += "_" + GraphicsDevice.Adapter.Description.Split('\0')[0].TrimEnd(' '); // Workaround for sharpDX bug: Description ends with an series trailing of '\0' characters

            var platformSpecificDir = GetPlatformSpecificDirectory();

            var testsBaseDir = Path.Combine(GetTestsRootDirectory(), "tests");
            var testFileName = GenerateTestArtifactFileName(testsBaseDir, frameName, platformSpecificDir, ".png");

            var testsLocalBaseDir = Path.Combine(testsBaseDir, "local");
            var testLocalFileName = GenerateTestArtifactFileName(testsLocalBaseDir, frameName, platformSpecificDir, ".png");

            var testFileNames = new List<string> { testFileName };

            // First, if exact match doesn't exist, test any other pattern
            // TODO: We might want to sort/filter partially (platform, etc...)?
            var matchingImage = File.Exists(testFileName);
            if (!matchingImage)
            {
                testFileNames.Clear();

                var wildcard = "*" + Path.DirectorySeparatorChar + "*";
                var testFileNamePattern = GenerateTestArtifactFileName(testsBaseDir, frameName, wildcard, ".png");
                var regexSep = Regex.Escape(Path.DirectorySeparatorChar.ToString());
                var testFileNameRegex = new Regex("^" + Regex.Escape(testFileNamePattern).Replace(@"\*", "[^" + regexSep + "]*") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var testFileNameRoot = testFileNamePattern[..testFileNamePattern.IndexOf('*')];

                if (Directory.Exists(testFileNameRoot))
                {
                    foreach (var file in Directory.EnumerateFiles(testFileNameRoot, "*.*", SearchOption.AllDirectories))
                    {
                        if (testFileNameRegex.IsMatch(file))
                        {
                            testFileNames.Add(file);
                        }
                    }
                }
            }

            if (testFileNames.Count == 0)
            {
                // No source image, save this one so that user can later copy it to validated folder
                ImageTester.SaveImage(image, testLocalFileName, ImageTester.BuildMetadata(GraphicsDevice));
                comparisonMissingMessages.Add($"* {testLocalFileName} (current)");
                // Treat "missing reference" as a (failed) comparison so interactive runners
                // can still surface the rendered output and offer a create-gold action.
                ImageTester.RaiseImageComparison(new ImageComparisonEventArgs
                {
                    CurrentPath = testLocalFileName,
                    ReferencePath = testFileName,
                    Passed = false,
                });
            }
            else
            {
                // Resolve thresholds from thresholds.jsonc
                var suiteDir = Path.Combine(testsBaseDir, GetType().Assembly.GetName().Name);
                var thresholdRules = ImageThreshold.LoadRules(suiteDir);
                var imageName = Path.GetFileName(testFileName);
                var platformParts = platformSpecificDir.Split(Path.DirectorySeparatorChar, '/');
                var platformApiPart = platformParts.Length > 0 ? platformParts[0] : null;
                var devicePart = platformParts.Length > 1 ? platformParts[1] : null;
                // platformApiPart is e.g. "Linux.Vulkan" — split into platform and API
                string? platformName = null, apiName = null;
                if (platformApiPart != null)
                {
                    var dotIdx = platformApiPart.IndexOf('.');
                    if (dotIdx >= 0)
                    {
                        platformName = platformApiPart[..dotIdx];
                        apiName = platformApiPart[(dotIdx + 1)..];
                    }
                    else
                    {
                        platformName = platformApiPart;
                    }
                }
                var thresholds = ImageThreshold.Resolve(thresholdRules, imageName, platformName, apiName, devicePart);

                // Compare against all available gold images
                var pendingFailMessages = new List<string>();
                var attempts = new List<ImageTester.SidecarAttempt>();
                bool anyMatch = false;
                string matchedFile = testFileName;
                ImageTester.ComparisonStats lastStats = default;
                foreach (var file in testFileNames)
                {
                    bool match = ImageTester.CompareImage(image, file, out var stats, thresholds);
                    lastStats = stats;
                    attempts.Add(ImageTester.ToSidecarAttempt(file, testFileName, stats, thresholds));
                    if (match)
                    {
                        anyMatch = true;
                        matchedFile = file;
                        break;
                    }
                    var isExactMatch = file == testFileName;
                    pendingFailMessages.Add($"  {file} ({(isExactMatch ? "reference" : "different platform/device")}) — {stats}");
                }

                // Sidecar always; PNG only on fail (sidecar carries the stats CompareGold
                // needs to render a passing cell; the pixel data would be redundant with gold
                // for exact matches and isn't worth the disk for the common case).
                ImageTester.SaveSidecar(testLocalFileName, new ImageTester.Sidecar
                {
                    Outcome = anyMatch ? "Pass" : "Fail",
                    At = DateTime.UtcNow,
                    Matched = anyMatch ? matchedFile : null,
                    Attempts = attempts,
                }, ImageTester.BuildMetadata(GraphicsDevice));

                if (!anyMatch)
                {
                    ImageTester.SaveImage(image, testLocalFileName);
                    comparisonFailedMessages.Add($"* {testLocalFileName} (current)");
                    comparisonFailedMessages.AddRange(pendingFailMessages);
                }
                else if (File.Exists(testLocalFileName))
                {
                    // Drop any stale PNG from a prior failing run on the same test.
                    File.Delete(testLocalFileName);
                }

                ImageTester.RaiseImageComparison(new ImageComparisonEventArgs
                {
                    CurrentPath = testLocalFileName,
                    ReferencePath = matchedFile,
                    Passed = anyMatch,
                    Stats = lastStats,
                });
            }
        }

        /// <summary>
        ///   Retrieves the platform-specific directory path based on the current platform type
        ///   and Graphics Adapter description. This is used to organize test artifacts.
        /// </summary>
        /// <returns>
        ///   A string representing the platform-specific directory path.
        /// </returns>
        /// <exception cref="NotImplementedException">The current platform type is not supported.</exception>
        private string GetPlatformSpecificDirectory()
        {
            string platformName = Platform.Type switch
            {
                PlatformType.Windows => "Windows",
                PlatformType.Linux => "Linux",
                PlatformType.macOS => "macOS",
                PlatformType.Android => "Android",
                _ => throw new NotImplementedException($"Platform {Platform.Type} is not supported for image regression tests")
            };

            return Path.Combine($"{platformName}.{GraphicsDevice.Platform}", NormalizeDeviceBucket(GraphicsDevice.Adapter));
        }

        // Stable, vendor-independent bucket name. Avoids Lavapipe's "llvmpipe (LLVM x.y.z, N bits)"
        // description from leaking into the gold path and breaking on every Mesa rebuild. The
        // Android emulator-host Vulkan layer stamps the host OS into the device name (e.g.
        // "...StrideHost=Linux") so Android gold buckets by host, whose Lavapipe build renders
        // slightly differently → "Lavapipe-LinuxHost".
        private static string NormalizeDeviceBucket(GraphicsAdapter adapter)
        {
            var desc = adapter.Description.Split('\0')[0].TrimEnd(' ');

            // Extract the emulator-host stamp before normalising the rest of the name.
            string hostTag = null;
            const string hostMarker = "StrideHost=";
            int markerIndex = desc.IndexOf(hostMarker, StringComparison.Ordinal);
            if (markerIndex >= 0)
            {
                hostTag = desc.Substring(markerIndex + hostMarker.Length).Trim();
                desc = desc.Substring(0, markerIndex).TrimEnd();
            }

            var driverId = adapter.DriverInfo?.DriverId;
            string deviceName;
            if (driverId == "MesaLLVMPipe" && desc.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase))
                deviceName = "Lavapipe";
            else if (driverId == "GoogleSwiftShader" && desc.StartsWith("SwiftShader", StringComparison.OrdinalIgnoreCase))
                deviceName = "SwiftShader";
            else if (adapter.VendorId == 0x1414) deviceName = "WARP"; // Microsoft Basic / WARP
            // Virtualized macOS (e.g. GitHub's macos-15 runner) reports the GPU as
            // "Apple Paravirtual device". On Apple Silicon the GPU is on the same chip as
            // the CPU, so the CPU brand string (minus the "(Virtual)" suffix) is a stable
            // proxy for the chip family — "Apple M1" rather than "Apple Paravirtual device".
            else if (desc.Contains("Paravirtual", StringComparison.OrdinalIgnoreCase)
                && HostEnvironment.CpuName is { } cpu && cpu.StartsWith("Apple ", StringComparison.OrdinalIgnoreCase))
            {
                var idx = cpu.IndexOf(" (", StringComparison.Ordinal);
                deviceName = idx > 0 ? cpu[..idx] : cpu;
            }
            else deviceName = desc;

            // Bucket by the layer-reported host; on Android with no stamp the layer wasn't active,
            // so we genuinely don't know which host's Lavapipe rendered this — fall into a distinct
            // "UnknownHost" bucket rather than silently aliasing onto a real host's gold.
            string bucketSuffix =
                !string.IsNullOrEmpty(hostTag) ? $"{hostTag}Host" :
                Platform.Type == PlatformType.Android ? "UnknownHost" :
                null;
            if (!string.IsNullOrEmpty(bucketSuffix))
                deviceName += $"-{bucketSuffix}";
            return deviceName;
        }

        /// <summary>
        ///   Generates a file name for a test artifact based on the class name, test name, frame name, and other parameters.
        /// </summary>
        /// <param name="testArtifactPath">
        ///   The base directory where test artifacts are stored.
        /// </param>
        /// <param name="frameName">
        ///   The name of the frame associated with the test,
        ///   or <see langword="null"/> if no frame is specified.
        /// </param>
        /// <param name="platformSpecificDir">
        ///   A platform-specific subdirectory to include in the file path.
        /// </param>
        /// <param name="extension">
        ///   The file extension to append to the generated file name (e.g., ".txt", ".log").
        /// </param>
        /// <returns>
        ///   A fully qualified file path for the test artifact, including the assembly name, class name,
        ///   test name, frame name, and platform-specific directory.
        /// </returns>
        private string GenerateTestArtifactFileName(string testArtifactPath, string frameName, string platformSpecificDir, string extension)
        {
            var assemblyName = GetType().Assembly.GetName().Name;
            var className = GetType().Name;

            var testFileName = className;
            if (TestName is not null)
                testFileName += $".{TestName}";
            if (frameName is not null)
                testFileName += $".{frameName}";
            testFileName += extension;

            var testDir = Path.Combine(testArtifactPath, assemblyName);
            testFileName = Path.Combine(testDir, platformSpecificDir, testFileName);
            return testFileName;
        }

        /// <summary>
        ///   Saves the specified Texture to a file in PNG format.
        /// </summary>
        /// <param name="texture">The <see cref="Texture"/> to be saved. Must not be <see langword="null"/>.</param>
        /// <param name="filePath">
        ///   The full path of the file where the Texture will be saved.
        ///   Must not be <see langword="null"/> or empty.
        /// </param>
        protected void SaveTexture(Texture texture, string filePath)
        {
            if (Platform.Type == PlatformType.Windows)
            {
                using var image = texture.GetDataAsImage(GraphicsContext.CommandList);
                using var resultFileStream = File.OpenWrite(filePath);

                image.Save(resultFileStream, ImageFileType.Png);
            }
        }

        /// <summary>
        ///   Stores information about a connected test device.
        /// </summary>
        public struct ConnectedDevice
        {
            public string Serial;
            public string Name;
            public TestPlatform Platform;

            public override readonly string ToString()
            {
                return Name + " " + Serial + " " + PlatformPermutator.GetPlatformName(Platform);
            }
        }

        /// <summary>
        ///   Skips the test if it is running on a specific platform.
        /// </summary>
        public static void SkipTestForPlatform(PlatformType platform)
        {
            Skip.If(Platform.Type == platform, $"This test is not valid for the '{platform}' platform. It has been skipped");
        }

        /// <summary>
        ///   Skips the test on any other platform than the provided one.
        /// </summary>
        public static void RequirePlatform(PlatformType platform)
        {
            Skip.IfNot(Platform.Type == platform, $"This test requires the '{platform}' platform. It has been skipped");
        }

        /// <summary>
        ///   Skips the test if it is using the given graphic platform
        /// </summary>
        public static void SkipTestForGraphicPlatform(GraphicsPlatform platform)
        {
            Skip.If(GraphicsDevice.Platform == platform, $"This test is not valid for the '{platform}' graphic platform. It has been skipped");
        }

        public static void SkipTestForGraphicPlatform(GraphicsPlatform platform, string reason)
        {
            Skip.If(GraphicsDevice.Platform == platform, $"{platform}: {reason}");
        }

        /// <summary>
        ///   Skips the test on any other graphics platform than the provided one.
        /// </summary>
        public static void RequireGraphicPlatform(GraphicsPlatform platform)
        {
            Skip.IfNot(GraphicsDevice.Platform == platform, $"This test requires the '{platform}' graphics platform. It has been skipped");
        }
    }
}
