// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games.Testing;

namespace Stride.Samples.Tests
{
    public class AnimatedModelTest : IClassFixture<AnimatedModelTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\AnimatedModel";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("99371864-55BD-4C78-B25C-42471F977540"))
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

                //Play Idle anim and take screenshot
                game.Tap(new Vector2(0.83f, 0.05f), TimeSpan.FromMilliseconds(500));
                game.Wait(TimeSpan.FromMilliseconds(2000));
                game.TakeScreenshot();
                game.Wait(TimeSpan.FromMilliseconds(500));

                //Play Run anim and take screenshot
                game.Tap(new Vector2(0.83f, 0.15f), TimeSpan.FromMilliseconds(500));
                game.Wait(TimeSpan.FromMilliseconds(2000));
                game.TakeScreenshot();
                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
