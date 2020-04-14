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
    public class ParticlesSampleTest : IClassFixture<ParticlesSampleTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\ParticlesSample";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("35C3FB4D-2A6E-40EB-825E-D4E5670FEE78"))
            {
            }
        }

        [Fact]
        public void TestLaunch()
        {
            using (var game = new GameTestingClient(Path, SampleTestsData.TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(5000));
            }
        }

        [Fact]
        public void TestInputs()
        {
            using (var game = new GameTestingClient(Path, SampleTestsData.TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(2000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(200)); // Transition to the next scene

                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
