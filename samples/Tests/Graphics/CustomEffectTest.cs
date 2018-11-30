// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Games.Testing;

namespace Xenko.Samples.Tests
{
    public class CustomEffectTest : IClassFixture<CustomEffectTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\CustomEffect";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("16476A4C-C131-4F48-865A-288EC7D5445F"))
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
                game.Wait(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
