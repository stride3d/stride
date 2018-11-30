// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Games.Testing;

namespace Xenko.Samples.Tests
{
    public class SpaceEscapeTest : IClassFixture<SpaceEscapeTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\SpaceEscape";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("F9C4B79D-E313-47BC-9287-75A0395B8AC4"))
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
                game.TakeScreenshot();
                game.Tap(new Vector2(0.496875f, 0.8010563f), TimeSpan.FromMilliseconds(250));
                game.Wait(TimeSpan.FromMilliseconds(1000));
                game.TakeScreenshot();
                game.KeyPress(Keys.Space, TimeSpan.FromMilliseconds(500));
                game.Wait(TimeSpan.FromMilliseconds(500));
                game.TakeScreenshot();
                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
