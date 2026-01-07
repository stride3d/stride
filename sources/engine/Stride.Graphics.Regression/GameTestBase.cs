// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
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
        ///   Gets or sets a value indicating whether RenderDoc should capture a frame when an error occurs
        ///   or a test fails.
        /// </summary>
        /// <remarks>Enabling this feature may cause an Out-of-Memory exception on 32-bit processes.</remarks>
        public static bool CaptureRenderDocOnError =
  #if STRIDE_TESTS_CAPTURE_RENDERDOC_ON_ERROR
            true;
  #else
            string.Equals(Environment.GetEnvironmentVariable("STRIDE_TESTS_CAPTURE_RENDERDOC_ON_ERROR"), "true", StringComparison.OrdinalIgnoreCase);
  #endif

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
        ///   Saves a Texture locally or on the test server.
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
        ///   Saves the Back-Buffer locally or on the test server.
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

            // TODO: GRAPHICS REFACTOR: Switched to presenter backbuffer, need to check if it's good
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            using var image = backBuffer.GetDataAsImage(GraphicsContext.CommandList);
            SaveImage(image, testName);
        }

        /// <summary>
        ///   Saves an Image locally or on the test server.
        /// </summary>
        /// <param name="imageToSave">The <see cref="Image"/> to save.</param>
        /// <param name="testName">
        ///   An optional name for the test that is wanting to save the <paramref name="textureToSave"/>.
        /// </param>
        private void SaveImage(Image imageToSave, string? testName = null)
        {
            try
            {
                SendImage(imageToSave, testName);
            }
            catch
            {
                TestGameLogger.Error(@"An error occurred when trying to send the data to the server.");
                throw;
            }
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
            set
            {
                backBufferSizeMode = value;
#if STRIDE_PLATFORM_ANDROID
                switch (backBufferSizeMode)
                {
                    case BackBufferSizeMode.FitToDesiredValues:
                        SwapChainGraphicsPresenter.ProcessPresentationParametersOverride = FitPresentationParametersToDesiredValues;
                        break;
                    case BackBufferSizeMode.FitToWindowSize:
                        SwapChainGraphicsPresenter.ProcessPresentationParametersOverride = FitPresentationParametersToWindowSize;
                        break;
                    case BackBufferSizeMode.FitToWindowRatio:
                        SwapChainGraphicsPresenter.ProcessPresentationParametersOverride = FitPresentationParametersToWindowRatio;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
#endif // TODO: Implement it for other mobile platforms
            }
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

#if STRIDE_PLATFORM_ANDROID
        private void FitPresentationParametersToDesiredValues(int windowWidth, int windowHeight, PresentationParameters parameters)
        {
            // nothing to do (default behavior)
        }

        private void FitPresentationParametersToWindowSize(int windowWidth, int windowHeight, PresentationParameters parameters)
        {
            parameters.BackBufferWidth = windowWidth;
            parameters.BackBufferHeight = windowHeight;
        }

        private void FitPresentationParametersToWindowRatio(int windowWidth, int windowHeight, PresentationParameters parameters)
        {
            var desiredWidth = parameters.BackBufferWidth;
            var desiredHeight = parameters.BackBufferHeight;

            if (windowWidth >= windowHeight) // Landscape => use height as base
            {
                parameters.BackBufferHeight = (int)(desiredWidth * (float)windowHeight / (float)windowWidth);
            }
            else // Portrait => use width as base
            {
                parameters.BackBufferWidth = (int)(desiredHeight * (float)windowWidth / (float)windowHeight);
            }
        }
#endif

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

#if !STRIDE_UI_SDL
            // Disabled for SDL as a position of (0,0) actually means that the client area of the
            // window will be at (0,0) not the top left corner of the non-client area of the window.
            Window.Position = Int2.Zero; // avoid possible side effects due to position of the window in the screen.
#endif

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
        protected static void RunGameTest(GameTestBase game)
        {
            game.EnableSimulatedInputSource();

            game.ScreenShotAutomationEnabled = !ForceInteractiveMode;

            ExceptionDispatchInfo exceptionOrFailedAssert = null;

            try
            {
                GameTester.RunGameTest(game);
            }
            catch (Exception ex)
            {
                // This catches both errors in the test execution and assertion failures
                exceptionOrFailedAssert = ExceptionDispatchInfo.Capture(ex);
            }

#if STRIDE_PLATFORM_DESKTOP
            if (CaptureRenderDocOnError)
            {
                // If no comparison errors, and no test errors, discard the capture
                if (game.comparisonFailedMessages.Count == 0 &&
                    game.comparisonMissingMessages.Count == 0 &&
                    exceptionOrFailedAssert is null)
                {
                    game.DiscardFrameCapture();
                }
                else game.EndFrameCapture();
            }
#endif
            // If there was an exception, rethrow it now
            exceptionOrFailedAssert?.Throw();

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
                    Assert.Fail("Some image comparison failed:" + Environment.NewLine + failedImages);
                }

                [DoesNotReturn]
                void AssertMissingComparisonImages()
                {
                    var missingImages = string.Join(Environment.NewLine, game.comparisonMissingMessages);
                    Assert.Fail("Some reference images are missing, please copy them manually:" + Environment.NewLine + missingImages);
                }
            }
        }

        /// <summary>
        ///   Searches for the root folder of the Stride solution by traversing upward from the test's binary directory.
        /// </summary>
        /// <returns>The full path to the root folder of the Stride solution.</returns>
        /// <exception cref="InvalidOperationException">The Stride solution root folder could not be located.</exception>
        private static string FindStrideSolutionRootDirectory()
        {
            var dir = PlatformFolders.ApplicationBinaryDirectory;
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir, @"build\Stride.sln")))
                    return dir;

                dir = Path.GetDirectoryName(dir);
            }

            throw new InvalidOperationException("Could not locate the Stride solution root directory");
        }

        /// <summary>
        ///   Send the test result image data to the server for verification.
        /// </summary>
        /// <param name="image">The Image to send.</param>
        /// <param name="testName">
        ///   An optional name for the test. If not provided, the frame index will be used.
        /// </param>
        public void SendImage(Image image, string? testName)
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
            var strideRootDir = FindStrideSolutionRootDirectory();

            var testsBaseDir = Path.Combine(strideRootDir, "tests");
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

                var testFileNamePattern = GenerateTestArtifactFileName(testsBaseDir, frameName, @"*\*", ".png");
                var testFileNameRegex = new Regex("^" + Regex.Escape(testFileNamePattern).Replace(@"\*", @"[^\\]*") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var testFileNameRoot = testFileNamePattern[..testFileNamePattern.IndexOf('*')];

                foreach (var file in Directory.EnumerateFiles(testFileNameRoot, "*.*", SearchOption.AllDirectories))
                {
                    if (testFileNameRegex.IsMatch(file))
                    {
                        testFileNames.Add(file);
                    }
                }
            }

            if (testFileNames.Count == 0)
            {
                // No source image, save this one so that user can later copy it to validated folder
                ImageTester.SaveImage(image, testLocalFileName);
                comparisonMissingMessages.Add($"* {testLocalFileName} (current)");
            }
            else if (!testFileNames.Any(file => ImageTester.CompareImage(image, file)))
            {
                // Comparison failed, save current version so that user can compare / promote it manually
                ImageTester.SaveImage(image, testLocalFileName);
                comparisonFailedMessages.Add($"* {testLocalFileName} (current)");
                foreach (var file in testFileNames)
                    comparisonFailedMessages.Add($"  {file} ({ (matchingImage ? "reference" : "different platform/device") })");
            }
            else
            {
                // If test is a success, let's delete the local file if it was previously generated
                if (File.Exists(testLocalFileName))
                    File.Delete(testLocalFileName);
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
            if (Platform.Type == PlatformType.Windows)
                return $"Windows.{GraphicsDevice.Platform}\\{GraphicsDevice.Adapter.Description.Split('\0')[0].TrimEnd(' ')}";
            else
                throw new NotImplementedException();
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
        ///   A fully qualified file path for the test artifact, including the namespace, class name,
        ///   test name, frame name, and platform-specific directory.
        /// </returns>
        private string GenerateTestArtifactFileName(string testArtifactPath, string frameName, string platformSpecificDir, string extension)
        {
            var fullClassName = GetType().FullName;
            var classNameIndex = fullClassName.LastIndexOf('.');
            var @namespace = classNameIndex != -1 ? fullClassName[..classNameIndex] : string.Empty;
            var className = fullClassName[(classNameIndex + 1)..];

            var testFileName = className;
            if (TestName is not null)
                testFileName += $".{TestName}";
            if (frameName is not null)
                testFileName += $".{frameName}";
            testFileName += extension;

            var testDir = Path.Combine(testArtifactPath, @namespace);
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

        /// <summary>
        ///   Skips the test on any other graphics platform than the provided one.
        /// </summary>
        public static void RequireGraphicPlatform(GraphicsPlatform platform)
        {
            Skip.IfNot(GraphicsDevice.Platform == platform, $"This test requires the '{platform}' graphics platform. It has been skipped");
        }
    }
}
