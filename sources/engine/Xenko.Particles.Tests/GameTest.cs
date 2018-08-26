// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Graphics.Regression;


namespace Xenko.Particles.Tests
{
    /// <summary>
    /// Game class for test on the Input sensors.
    /// </summary>
    public class GameTest : GameTestBase
    {
        /// <summary>
        /// The <see cref="ParticleTestVersion"/> is shared for all tests
        /// </summary>
        // Breaking changes
        //  Please update the version number every time there is a breaking change to the particle engine and write down what has been changed
        //        const int ParticleTestVersion = 1;  // Initial tests
        //        const int ParticleTestVersion = 2;  // Changed the tests on purpose to check if the tests fail
        //        const int ParticleTestVersion = 3;  // Added actual visual tests, bumping up the version since they are quite different
        //        const int ParticleTestVersion = 4;  // Changed the default size for billboards, hexagons and quads (previous visual tests are broken)
        //        const int ParticleTestVersion = 5;  // Changed the colliders behavior (non-uniform scales weren't supported before)
        //        const int ParticleTestVersion = 6;  // Moved the main update from Update() to Draw() cycle
        //        const int ParticleTestVersion = 7;  // Children Particles visual test updated
        //        const int ParticleTestVersion = 8;  // Camera ignores scaling, due to float precision issues it renders slightly differently
        //        const int ParticleTestVersion = 10;  // Skip 2 (to ignore colliding with tests on the master branch) + Camera ignores scaling, due to float precision issues it renders slightly differently
        //        const int ParticleTestVersion = 11;  // Merged version between version 8 and 10
        //        const int ParticleTestVersion = 12; // NUnit3 switch
        //        const int ParticleTestVersion = 13; // Bug fix where soft edge particles don't render and the gold image was mistaken too
        const int ParticleTestVersion = 114; //  Changed to avoid collisions with 1.9

        /// <summary>
        ///  The <see cref="IndividualTestVersion"/> can be defined per test when only one of them is affected
        /// </summary>
        protected int IndividualTestVersion;

        // Local screenshots
        private readonly string xenkoDir;
        private readonly string assemblyName;
        private readonly string testName;
        private int screenShots;

        private GraphicsProfile overrideGraphicsProfile;

        public GameTest(string name, GraphicsProfile profile = GraphicsProfile.Level_9_3)
        {
            screenShots = 0;
            testName = name;
            xenkoDir = Environment.GetEnvironmentVariable("XenkoDir");
            assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

#if XENKO_PLATFORM_WINDOWS_DESKTOP
            //  SaveScreenshot is only defined for windows
            Directory.CreateDirectory(xenkoDir + "\\screenshots\\");
#endif

            AutoLoadDefaultSettings = true; // Note! This will override the preferred graphics profile so save it for later
            overrideGraphicsProfile = profile;
            
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            // This still doesn't work IsDrawDesynchronized = false; // Double negation!
            TargetElapsedTime = TimeSpan.FromTicks(10000000 / 60); // target elapsed time is by default 60Hz
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { overrideGraphicsProfile };
        }

        protected override void Initialize()
        {
            base.Initialize();
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { overrideGraphicsProfile };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var assetManager = Services.GetSafeServiceAs<ContentManager>();

            // Make sure you have created a Scene with the same name (testName) in your XenkoGameStudio project.
            // The scene should be included in the build as Root and copied together with the other 
            //  assets to the /GameAssets directory contained in this assembly's directory
            // Finally, make sure the scene is also added to the Xenko.Particles.Tests.xkpkg
            //  and it has a proper uid. Example (for the VisualTestSpawners scene):
            //     - a9ba28ad-d83b-4957-8ed6-42863c1d903c:VisualTestSpawners
            SceneSystem.SceneInstance = new SceneInstance(Services, assetManager.Load<Scene>(testName));
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // Take a screenshot after 60 frames
            FrameGameSystem.TakeScreenshot(60);
        }

        protected override void Update(GameTime gameTime)
        {
            // Do not update the state while a screenshot is being requested
            if (ScreenshotRequested)
                return;

            base.Update(gameTime);

            if (gameTime.FrameCount == 60)
            {
                RequestScreenshot();
            }

            if (gameTime.FrameCount >= 65)
            {
                Exit();
            }
        }

        protected bool ScreenshotRequested = false;
        protected void RequestScreenshot()
        {
            ScreenshotRequested = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenshotRequested)
                return;

            ScreenshotRequested = false;
        }

        /// <summary>
        /// This is useful if you want to run all the tests on your own machine and compare images
        /// </summary>
        public static void Main()
        {
            //using (var game = new GameTest("GameTest")) { game.Run(); }

            using (var game = new VisualTestInitializers()) { game.Run(); }

            using (var game = new VisualTestSpawners()) { game.Run(); }

            using (var game = new VisualTestGeneral()) { game.Run(); }

            using (var game = new VisualTestUpdaters()) { game.Run(); }

            using (var game = new VisualTestMaterials()) { game.Run(); }

            using (var game = new VisualTestCurves()) { game.Run(); }

            using (var game = new VisualTestRibbons()) { game.Run(); }

            using (var game = new VisualTestChildren()) { game.Run(); }

            // using (var game = new VisualTestSoftEdge(GraphicsProfile.Level_9_3)) { game.Run(); } // This is not implemented yet and may not be

            using (var game = new GameTest("VisualTestSoftEdge", GraphicsProfile.Level_10_0)) { game.Run(); }

            using (var game = new GameTest("VisualTestSoftEdge", GraphicsProfile.Level_11_0)) { game.Run(); }
        }
    }
}

