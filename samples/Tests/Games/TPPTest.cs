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
    public class TPPTest : IClassFixture<TPPTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\ThirdPersonPlatformer";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("990311E4-152B-458D-8CBD-180903845DA7"))
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
                game.Tap(new Vector2(0.5f, 0.7f), TimeSpan.FromMilliseconds(500));
                game.Wait(TimeSpan.FromMilliseconds(500));
                game.KeyPress(Keys.Space, TimeSpan.FromMilliseconds(500));
                game.Wait(TimeSpan.FromMilliseconds(500));
                game.TakeScreenshot();
                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
