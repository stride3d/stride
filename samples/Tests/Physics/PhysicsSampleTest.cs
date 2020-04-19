// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Games.Testing;

namespace Stride.Samples.Tests
{
    public class PhysicsSampleTest : IClassFixture<PhysicsSampleTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\PhysicsSample";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("d20d150b-d3cb-454e-8c11-620b4c9d393f"))
            {
            }
        }

        [Fact]
        public void TestLaunch()
        {
            using (var game = new GameTestingClient(Path, SampleTestsData.TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(2000));
            }
        }

        [Fact]
        public void TestInputs()
        {
            using (var game = new GameTestingClient(Path, SampleTestsData.TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(2000));

                // X:0.8367187 Y:0.9375

                // Constraints
                game.TakeScreenshot();

                game.Tap(new Vector2(0.83f, 0.93f), TimeSpan.FromMilliseconds(200));
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.83f, 0.93f), TimeSpan.FromMilliseconds(200));
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.83f, 0.93f), TimeSpan.FromMilliseconds(200));
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.83f, 0.93f), TimeSpan.FromMilliseconds(200));
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.83f, 0.93f), TimeSpan.FromMilliseconds(200));
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                // VolumeTrigger
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                // Raycasting
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.3304687f, 0.6055555f), TimeSpan.FromMilliseconds(200));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.496875f, 0.4125f), TimeSpan.FromMilliseconds(200));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.659375f, 0.5319445f), TimeSpan.FromMilliseconds(200));
                game.TakeScreenshot();

                game.KeyPress(Keys.Right, TimeSpan.FromMilliseconds(500));              
                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
