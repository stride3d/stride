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
    public class GameMenuTest : IClassFixture<GameMenuTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\GameMenu";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("7ac2c705-6240-4ddc-af63-fc438d10f4de"))
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

                /*
                [GameMenu.MainScript]: Info: X:0.4765625 Y:0.8389084
                [GameMenu.MainScript]: Info: X:0.6609375 Y:0.7315141
                [GameMenu.MainScript]: Info: X:0.6609375 Y:0.7315141
                [GameMenu.MainScript]: Info: X:0.5390625 Y:0.7764084
                [GameMenu.MainScript]: Info: X:0.5390625 Y:0.7764084
                */

                game.Tap(new Vector2(0.4765625f, 0.8389084f), TimeSpan.FromMilliseconds(250));
                game.Wait(TimeSpan.FromMilliseconds(250));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.6609375f, 0.7315141f), TimeSpan.FromMilliseconds(250));
                game.Wait(TimeSpan.FromMilliseconds(250));
                game.TakeScreenshot();

                game.Tap(new Vector2(0.5390625f, 0.7764084f), TimeSpan.FromMilliseconds(250));
                game.Wait(TimeSpan.FromMilliseconds(250));
                game.TakeScreenshot();

                game.Wait(TimeSpan.FromMilliseconds(2000));
            }
        }
    }
}
