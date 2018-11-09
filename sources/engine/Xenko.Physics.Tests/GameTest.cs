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

namespace Xenko.Physics.Tests
{
    /// <summary>
    /// Game class for test on the Input sensors.
    /// </summary>
    public class GameTest : GameTestBase
    {
        const int PhysicsTestVersion = 1; // NUnit3 switch

        /// <summary>
        ///  The <see cref="IndividualTestVersion"/> can be defined per test when only one of them is affected
        /// </summary>
        protected int IndividualTestVersion;

        // Local screenshots
        private readonly string assemblyName;
        private readonly string testName;
        private readonly string platformName;
        private int screenShots;

        private readonly GraphicsProfile overrideGraphicsProfile;

        public GameTest(string name, GraphicsProfile profile = GraphicsProfile.Level_9_3)
        {
            screenShots = 0;
            testName = name;
            assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

#if XENKO_PLATFORM_WINDOWS_DESKTOP
            //  SaveScreenshot is only defined for windows
            platformName = "Windows";
            Directory.CreateDirectory("screenshots\\");
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
            // Finally, make sure the scene is also added to the Xenko.Physics.Tests.xkpkg
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

//            if (gameTime.FrameCount == 60)
//            {
//                RequestScreenshot();
//            }
//
//            if (gameTime.FrameCount >= 65)
//            {
//                Exit();
//            }
        }

        protected bool ScreenshotRequested = false;
        protected void RequestScreenshot()
        {
            ScreenshotRequested = true;
        }

        protected void SaveCurrentFrameBufferToHdd()
        {
            // SaveTexture is only defined for Windows and is only used to test the screenshots locally
            var filename = "screenshots\\" + assemblyName + "." + platformName + "_" + testName + "_" + screenShots + ".png";
            screenShots++;

            SaveTexture(GraphicsDevice.Presenter.BackBuffer, filename);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenshotRequested)
                return;

            SaveCurrentFrameBufferToHdd();
            ScreenshotRequested = false;
        }

        /// <summary>
        /// This is useful if you want to run all the tests on your own machine and compare images
        /// </summary>
        public static void Main()
        {
            using (var game = new ColliderShapesTest()) { game.ColliderShapesTest1(); }
            using (var game = new CharacterTest()) { game.CharacterTest1(); }
            using (var game = new SkinnedTest()) { game.SkinnedTest1(); }
        }
    }
}

