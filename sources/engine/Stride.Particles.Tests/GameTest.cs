// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;


namespace Stride.Particles.Tests
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
        private readonly string testName;

        private GraphicsProfile overrideGraphicsProfile;

        public GameTest(string name, GraphicsProfile profile = GraphicsProfile.Level_9_3)
        {
            testName = name;

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

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var assetManager = Services.GetSafeServiceAs<ContentManager>();

            // Make sure you have created a Scene with the same name (testName) in your StrideGameStudio project.
            // The scene should be included in the build as Root and copied together with the other 
            //  assets to the /GameAssets directory contained in this assembly's directory
            // Finally, make sure the scene is also added to the Stride.Particles.Tests.sdpkg
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
    }
}

