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
    public class TouchInputsTest : IClassFixture<TouchInputsTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\TouchInputs";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("662A15A4-92C1-4C43-BE06-0303C9E2FF50"))
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

                game.Drag(new Vector2(0.7f, 0.7f), new Vector2(0.1f, 0.1f), TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromMilliseconds(500));
                game.TakeScreenshot();

                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
