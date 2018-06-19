// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Input;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Graphics.Regression
{
    public abstract class GameTestBase : Game
    {
        public static bool ForceInteractiveMode;

        public static readonly Logger TestGameLogger = GlobalLogger.GetLogger("TestGameLogger");

        public FrameGameSystem FrameGameSystem { get; }

        protected TestContext CurrentTestContext { get; set; }

        public int StopOnFrameCount { get; set; }

        public InputSourceSimulated InputSourceSimulated { get; private set; }
        public MouseSimulated MouseSimulated { get; private set; }
        public KeyboardSimulated KeyboardSimulated { get; private set; }

        /// <summary>
        /// The current version of the test
        /// </summary>
        public int CurrentVersion;

        /// <summary>
        /// The current version extra parameter (concatenated to CurrentVersionNumber).
        /// </summary>
        public string CurrentVersionExtra;

        public int FrameIndex;

        private bool screenshotAutomationEnabled;

        private BackBufferSizeMode backBufferSizeMode;

        protected GameTestBase()
        {
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

            CurrentVersion = 0;
            StopOnFrameCount = -1;
            AutoLoadDefaultSettings = false;

            FrameGameSystem = new FrameGameSystem(Services);
            GameSystems.Add(FrameGameSystem);

            // by default we want the same size for the back buffer on mobiles and windows.
            BackBufferSizeMode = BackBufferSizeMode.FitToDesiredValues;

#if XENKO_PLATFORM_WINDOWS_DESKTOP
            // get build number
            if (ImageTester.ImageTestResultConnection.BuildNumber <= 0 && int.TryParse(Environment.GetEnvironmentVariable("XENKO_BUILD_NUMBER"), out int buildNumber))
                ImageTester.ImageTestResultConnection.BuildNumber = buildNumber;

            // get branch name
            if (string.IsNullOrEmpty(ImageTester.ImageTestResultConnection.BranchName))
                ImageTester.ImageTestResultConnection.BranchName = Environment.GetEnvironmentVariable("XENKO_BRANCH_NAME") ?? "";
#endif
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

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

            TestGameLogger.Info(@"Saving non null image");
            testName = testName ?? CurrentTestContext?.Test.FullName;
            TestGameLogger.Info(@"saving remotely.");
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
#if XENKO_PLATFORM_ANDROID
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
            if(Input != null)
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

            if(!ForceInteractiveMode)
                InitializeSimulatedInputSource();

#if !XENKO_UI_SDL
            // Disabled for SDL as a position of (0,0) actually means that the client area of the
            // window will be at (0,0) not the top left corner of the non-client area of the window.
            Window.Position = Int2.Zero; // avoid possible side effects due to position of the window in the screen.
#endif

#if XENKO_PLATFORM_WINDOWS_DESKTOP
            // Register 3D card name
            // TODO: This doesn't work well because ImageTester.ImageTestResultConnection is static, this will need improvements
            if (!ImageTester.ImageTestResultConnection.DeviceName.Contains("_"))
                ImageTester.ImageTestResultConnection.DeviceName += "_" + GraphicsDevice.Adapter.Description.Split('\0')[0].TrimEnd(' '); // Workaround for sharpDX bug: Description ends with an series trailing of '\0' characters
#endif

            Script.AddTask(RegisterTestsInternal);
        }

        private Task RegisterTestsInternal()
        {
            if(!FrameGameSystem.IsUnitTestFeeding)
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
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                return;

            string testName;

            if (FrameGameSystem.AllTestsCompleted)
                Exit();
            else if (FrameGameSystem.IsScreenshotNeeded(out testName))
                SaveBackBuffer(testName);
        }

        protected void PerformTest(Action<Game> testAction, GraphicsProfile? profileOverride = null, bool takeSnapshot = false)
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

        protected void PerformDrawTest(Action<Game, RenderDrawContext> drawTestAction, GraphicsProfile? profileOverride = null, string subTestName = null, bool takeSnapshot = true)
        {
            // create the game instance
            var typeGame = GetType();
            var game = (GameTestBase)Activator.CreateInstance(typeGame);
            if (profileOverride.HasValue)
                game.GraphicsDeviceManager.PreferredGraphicsProfile = new[] { profileOverride.Value };

            // register the tests.
            game.FrameGameSystem.IsUnitTestFeeding = true;
            var testName = TestContext.CurrentContext.Test.FullName+subTestName;
            if (takeSnapshot)
                game.FrameGameSystem.TakeScreenshot(null, testName);

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

            game.CurrentTestContext = TestContext.CurrentContext;

            game.ScreenShotAutomationEnabled = !ForceInteractiveMode;

            GameTester.RunGameTest(game);

            var failedTests = new List<string>();

            if (game.ScreenShotAutomationEnabled)
            {
                foreach (var testName in game.FrameGameSystem.TestNames)
                {
                    var localTestName = testName;
                    if (!ImageTester.RequestImageComparisonStatus(ref localTestName))
                        failedTests.Add(localTestName);
                }
            }

            if (failedTests.Count > 0)
                Assert.Fail($"Some image comparison tests failed: {string.Join(", ", failedTests.Select(x => x))}");
        }

        /// <summary>
        /// Send the data of the test to the server.
        /// </summary>
        /// <param name="image">The image to send.</param>
        /// <param name="testName">The name of the test.</param>
        public void SendImage(Image image, string testName)
        {
            var currentVersion = CurrentVersion.ToString();
            if (CurrentVersionExtra != null)
                currentVersion += CurrentVersionExtra;

            // TODO: Allow textual frame names (and use FrameIndex if not properly set)
            var frameIndex = FrameIndex++;

            ImageTester.SendImage(new TestResultImage { CurrentVersion = currentVersion, Frame = frameIndex.ToString(), Image = image, TestName = testName });
        }

        protected void SaveTexture(Texture texture, string filename)
        {
#if XENKO_PLATFORM_WINDOWS_DESKTOP
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
            if(Platform.Type == platform)
                Assert.Ignore("This test is not valid for the '{0}' platform. It has been ignored", platform);
        }

        /// <summary>
        /// Ignore the test on any other platform than the provided one.
        /// </summary>
        public static void RequirePlatform(PlatformType platform)
        {
            if(Platform.Type != platform)
                Assert.Ignore("This test requires the '{0}' platform. It has been ignored", platform);
        }

        /// <summary>
        /// Ignore the test on the given graphic platform
        /// </summary>
        public static void IgnoreGraphicPlatform(GraphicsPlatform platform)
        {
            if (GraphicsDevice.Platform == platform)
                Assert.Ignore("This test is not valid for the '{0}' graphic platform. It has been ignored", platform);
        }

        /// <summary>
        /// Ignore the test on any other graphic platform than the provided one.
        /// </summary>
        public static void RequireGraphicPlatform(GraphicsPlatform platform)
        {
            if (GraphicsDevice.Platform != platform)
                Assert.Ignore("This test requires the '{0}' platform. It has been ignored", platform);
        }
    }
}
