// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Games.Testing;

namespace Stride.Samples.Tests
{
    public class RPGTest : IClassFixture<RPGTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\TopDownRPG";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("A363FBC5-89EF-4E7A-B870-6D070813D034"))
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
