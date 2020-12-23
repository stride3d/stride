// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Text.RegularExpressions;

namespace Stride.Graphics.Regression
{
    public abstract class GameTestBase : Game
    {
        public static bool ForceInteractiveMode;
        // Note: it might cause OOM on 32-bit processes
        public static bool CaptureRenderDocOnError = string.Compare(Environment.GetEnvironmentVariable("STRIDE_TESTS_CAPTURE_RENDERDOC_ON_ERROR"), "true", StringComparison.OrdinalIgnoreCase) == 0;

        public static readonly Logger TestGameLogger = GlobalLogger.GetLogger("TestGameLogger");

        public FrameGameSystem FrameGameSystem { get; }

        public int StopOnFrameCount { get; set; }

        public string TestName { get; set; }

        public InputSourceSimulated InputSourceSimulated { get; private set; }
        public MouseSimulated MouseSimulated { get; private set; }
        public KeyboardSimulated KeyboardSimulated { get; private set; }

        public int FrameIndex;

        private bool screenshotAutomationEnabled;
        private List<string> comparisonMissingMessages = new List<string>();
        private List<string> comparisonFailedMessages = new List<string>();

        private BackBufferSizeMode backBufferSizeMode;
        private RenderDocManager renderDocManager;

        protected GameTestBase()
        {
            ConsoleLogMode = ConsoleLogMode.Always;

            // Override the default graphic device manager
            GraphicsDeviceManager.Dispose();
            GraphicsDeviceManager = new TestGraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 800,
                PreferredBackBufferHeight = 480,
                PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt,
                DeviceCreationFlags = DeviceCreationFlags.Debug,
                PreferredGraphicsProfile = new[] { GraphicsProfile.Level_9_1 }
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

            if (CaptureRenderDocOnError)
            {
                renderDocManager = new RenderDocManager();
                if (!renderDocManager.IsInitialized)
                    renderDocManager = null;
            }

            // Disable streaming
            Streaming.Enabled = false;

            // Enable profiling
            //Profiler.EnableAll();

            // Disable splash screen
            SceneSystem.SplashScreenEnabled = false;
        }

        /// <summary>
        /// Save the image locally or on the server.
        /// </summary>
        /// <param name="textureToSave">The texture to save.</param>
        /// <param name="testName">The name of the test corresponding to the image to save</param>
        public void SaveImage(Texture textureToSave, string testName = null)
        {
            if (textureToSave == null)
                return;

            TestGameLogger.Info(@"Saving image");
            using (var image = textureToSave.GetDataAsImage(GraphicsContext.CommandList))
            {
                try
                {
                    SendImage(image, testName);
                }
                catch (Exception)
                {
                    TestGameLogger.Error(@"An error occurred when trying to send the data to the server.");
                    throw;
                }
            }
        }

        /// <summary>
        /// Save the image locally or on the server.
        /// </summary>
        public void SaveBackBuffer(string testName = null)
        {
            TestGameLogger.Info(@"Saving the backbuffer");
            // TODO GRAPHICS REFACTOR switched to presenter backbuffer, need to check if it's good
            SaveImage(GraphicsDevice.Presenter.BackBuffer, testName);
        }

        /// <summary>
        /// Gets or sets the value indicating if the screen shots automation should be enabled or not.
        /// </summary>
        public bool ScreenShotAutomationEnabled
        {
            get { return screenshotAutomationEnabled; }
            set
            {
                FrameGameSystem.Visible = value;
                FrameGameSystem.Enabled = value;
                screenshotAutomationEnabled = value;
            }
        }

        public BackBufferSizeMode BackBufferSizeMode
        {
            get { return backBufferSizeMode; }
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
#endif // TODO implement it other mobile platforms
            }
        }

        protected internal void EnableSimulatedInputSource()
        {
            InputSourceSimulated = new InputSourceSimulated();
            if (Input != null)
                InitializeSimulatedInputSource();
        }

        private void InitializeSimulatedInputSource()
        {
            if (InputSourceSimulated != null)
            {
                Input.Sources.Clear();
                Input.Sources.Add(InputSourceSimulated);
                MouseSimulated = InputSourceSimulated.AddMouse();
                KeyboardSimulated = InputSourceSimulated.AddKeyboard();
            }
        }

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

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Setup RenderDoc capture
            if (renderDocManager != null)
            {
                var renderdocCaptureFile = GenerateTestArtifactFileName(Path.Combine(FindStrideRootFolder(), @"tests\local"), null, GetPlatformSpecificFolder(), ".rdc");
                renderDocManager.Initialize(renderdocCaptureFile);
                renderDocManager.StartFrameCapture(GraphicsDevice, IntPtr.Zero);
            }

            if (!ForceInteractiveMode)
                InitializeSimulatedInputSource();

#if !STRIDE_UI_SDL
            // Disabled for SDL as a position of (0,0) actually means that the client area of the
            // window will be at (0,0) not the top left corner of the non-client area of the window.
            Window.Position = Int2.Zero; // avoid possible side effects due to position of the window in the screen.
#endif

            Script.AddTask(RegisterTestsInternal);
        }

        protected override void Destroy()
        {
            if (renderDocManager != null)
            {
                // Note: if no comparison error, let's discard the capture
                if (comparisonFailedMessages.Count == 0 && comparisonMissingMessages.Count == 0)
                    renderDocManager.DiscardFrameCapture(GraphicsDevice, IntPtr.Zero);
                else
                    renderDocManager.EndFrameCapture(GraphicsDevice, IntPtr.Zero);
                // Note: we don't remove hooks in case another unit test need them later
                //renderDocManager.RemoveHooks();
            }

            base.Destroy();
        }

        private Task RegisterTestsInternal()
        {
            if (!FrameGameSystem.IsUnitTestFeeding)
                RegisterTests();

            return Task.FromResult(true);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (gameTime.FrameCount == StopOnFrameCount)
            {
                Exit();
            }
        }

        /// <summary>
        /// Loop through all the tests and save the images.
        /// </summary>
        /// <param name="gameTime">the game time.</param>
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

            if (!ScreenShotAutomationEnabled)
                return;

            string testName;

            if (FrameGameSystem.AllTestsCompleted)
                Exit();
            else if (FrameGameSystem.IsScreenshotNeeded(out testName))
                SaveBackBuffer(testName);
        }

        protected void PerformTest(Action<GameTestBase> testAction, GraphicsProfile? profileOverride = null, bool takeSnapshot = false)
        {
            // create the game instance
            var typeGame = GetType();
            var game = (GameTestBase)Activator.CreateInstance(typeGame);
            if (profileOverride.HasValue)
                game.GraphicsDeviceManager.PreferredGraphicsProfile = new[] { profileOverride.Value };

            // register the tests.
            game.FrameGameSystem.IsUnitTestFeeding = true;
            game.FrameGameSystem.Draw(() => testAction(game));
            if (takeSnapshot)
                game.FrameGameSystem.TakeScreenshot();

            RunGameTest(game);
        }

        protected void PerformDrawTest(Action<GameTestBase, RenderDrawContext> drawTestAction, GraphicsProfile? profileOverride = null, string subTestName = null, bool takeSnapshot = true)
        {
            // create the game instance
            var typeGame = GetType();
            var game = (GameTestBase)Activator.CreateInstance(typeGame);
            if (profileOverride.HasValue)
                game.GraphicsDeviceManager.PreferredGraphicsProfile = new[] { profileOverride.Value };

            // register the tests.
            game.FrameGameSystem.IsUnitTestFeeding = true;
            if (takeSnapshot)
                game.FrameGameSystem.TakeScreenshot();

            // setup empty scene
            var scene = new Scene();
            game.SceneSystem.SceneInstance = new SceneInstance(Services, scene);

            // add the render callback
            game.SceneSystem.GraphicsCompositor = new GraphicsCompositor
            {
                Game = new DelegateSceneRenderer(context => drawTestAction(game, context)),
            };

            RunGameTest(game);
        }

        /// <summary>
        /// Method to register the tests.
        /// </summary>
        protected virtual void RegisterTests()
        {
        }

        protected static void RunGameTest(GameTestBase game)
        {
            game.EnableSimulatedInputSource();

            game.ScreenShotAutomationEnabled = !ForceInteractiveMode;

            GameTester.RunGameTest(game);

            if (game.ScreenShotAutomationEnabled)
            {
                Assert.True(game.comparisonFailedMessages.Count == 0, "Some image comparison failed:" + Environment.NewLine + string.Join(Environment.NewLine, game.comparisonFailedMessages));
                Assert.True(game.comparisonMissingMessages.Count == 0, "Some reference images are missing, please copy them manually:" + Environment.NewLine + string.Join(Environment.NewLine, game.comparisonMissingMessages));
            }
        }

        private static string FindStrideRootFolder()
        {
            // Make sure our nuget local store is added to nuget config
            var folder = PlatformFolders.ApplicationBinaryDirectory;
            while (folder != null)
            {
                if (File.Exists(Path.Combine(folder, @"build\Stride.sln")))
                    return folder;
                folder = Path.GetDirectoryName(folder);
            }

            throw new InvalidOperationException("Could not locate Stride folder");
        }

        /// <summary>
        /// Send the data of the test to the server.
        /// </summary>
        /// <param name="image">The image to send.</param>
        /// <param name="testName">The name of the test.</param>
        public void SendImage(Image image, string testName)
        {
            var frame = testName;
            if (frame == null && FrameIndex++ > 0)
                frame = "f" + (FrameIndex - 1);

            // Register 3D card name
            // TODO: This doesn't work well because ImageTester.ImageTestResultConnection is static, this will need improvements
            //if (!ImageTester.ImageTestResultConnection.DeviceName.Contains("_"))
            //    ImageTester.ImageTestResultConnection.DeviceName += "_" + GraphicsDevice.Adapter.Description.Split('\0')[0].TrimEnd(' '); // Workaround for sharpDX bug: Description ends with an series trailing of '\0' characters

            var platformSpecific = GetPlatformSpecificFolder();
            var rootFolder = FindStrideRootFolder();

            var testFilename = GenerateTestArtifactFileName(Path.Combine(rootFolder, "tests"), frame, platformSpecific, ".png");
            var testFilenameUser = GenerateTestArtifactFileName(Path.Combine(rootFolder, @"tests\local"), frame, platformSpecific, ".png");

            var testFilenames = new List<string> { testFilename };

            // First, if exact match doesn't exist, test any other pattern
            // TODO: We might want to sort/filter partially (platform, etc...)?
            var matchingImage = File.Exists(testFilename);
            if (!matchingImage)
            {
                testFilenames.Clear();

                var testFilenamePattern = GenerateTestArtifactFileName(Path.Combine(rootFolder, "tests"), frame, @"*\*", ".png");
                var testFilenameRoot = testFilenamePattern.Substring(0, testFilenamePattern.IndexOf('*'));
                var testFilenameRegex = new Regex("^" + Regex.Escape(testFilenamePattern).Replace(@"\*", @"[^\\]*") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (var file in Directory.EnumerateFiles(testFilenameRoot, "*.*", SearchOption.AllDirectories))
                {
                    if (testFilenameRegex.IsMatch(file))
                    {
                        testFilenames.Add(file);
                    }
                }
            }

            if (testFilenames.Count == 0)
            {
                // No source image, save this one so that user can later copy it to validated folder
                ImageTester.SaveImage(image, testFilenameUser);
                comparisonMissingMessages.Add($"* {testFilenameUser} (current)");
            }
            else if (!testFilenames.Any(file => ImageTester.CompareImage(image, file)))
            {
                // Comparison failed, save current version so that user can compare/promote it manually
                ImageTester.SaveImage(image, testFilenameUser);
                comparisonFailedMessages.Add($"* {testFilenameUser} (current)");
                foreach (var file in testFilenames)
                    comparisonFailedMessages.Add($"  {file} ({ (matchingImage ? "reference" : "different platform/device") })");
            }
            else
            {
                // If test is a success, let's delete the local file if it was previously generated
                if (File.Exists(testFilenameUser))
                    File.Delete(testFilenameUser);
            }
        }

        private string GetPlatformSpecificFolder()
        {
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
            return $"Windows.{GraphicsDevice.Platform}\\{GraphicsDevice.Adapter.Description.Split('\0')[0].TrimEnd(' ')}";
#else
            var platformSpecific = string.Empty;
            throw new NotImplementedException();
#endif
        }

        private string GenerateTestArtifactFileName(string testArtifactPath, string frame, string platformSpecific, string extension)
        {
            var fullClassName = GetType().FullName;
            var classNameIndex = fullClassName.LastIndexOf('.');
            var @namespace = classNameIndex != -1 ? fullClassName.Substring(0, classNameIndex) : string.Empty;
            var className = fullClassName.Substring(classNameIndex + 1);

            var testFolder = Path.Combine(testArtifactPath, @namespace);
            var testFilename = className;
            if (TestName != null)
                testFilename += $".{TestName}";
            if (frame != null)
                testFilename += $".{frame}";
            testFilename += extension;
            testFilename = Path.Combine(testFolder, platformSpecific, testFilename);

            // Collapse parent directories
            return testFilename;
        }

        protected void SaveTexture(Texture texture, string filename)
        {
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
            using (var image = texture.GetDataAsImage(GraphicsContext.CommandList))
            {
                using (var resultFileStream = File.OpenWrite(filename))
                {
                    image.Save(resultFileStream, ImageFileType.Png);
                }
            }
#endif
        }

        /// <summary>
        /// A structure to store information about the connected test devices.
        /// </summary>
        public struct ConnectedDevice
        {
            public string Serial;
            public string Name;
            public TestPlatform Platform;

            public override string ToString()
            {
                return Name + " " + Serial + " " + PlatformPermutator.GetPlatformName(Platform);
            }
        }

        /// <summary>
        /// Ignore the test on the given platform
        /// </summary>
        public static void IgnorePlatform(PlatformType platform)
        {
            Skip.If(Platform.Type == platform, $"This test is not valid for the '{platform}' platform. It has been ignored");
        }

        /// <summary>
        /// Ignore the test on any other platform than the provided one.
        /// </summary>
        public static void RequirePlatform(PlatformType platform)
        {
            Skip.If(Platform.Type != platform, $"This test requires the '{platform}' platform. It has been ignored");
        }

        /// <summary>
        /// Ignore the test on the given graphic platform
        /// </summary>
        public static void IgnoreGraphicPlatform(GraphicsPlatform platform)
        {
            Skip.If(GraphicsDevice.Platform == platform, $"This test is not valid for the '{platform}' graphic platform. It has been ignored");
        }

        /// <summary>
        /// Ignore the test on any other graphic platform than the provided one.
        /// </summary>
        public static void RequireGraphicPlatform(GraphicsPlatform platform)
        {
            Skip.If(GraphicsDevice.Platform != platform, $"This test requires the '{platform}' platform. It has been ignored");
        }
    }
}
