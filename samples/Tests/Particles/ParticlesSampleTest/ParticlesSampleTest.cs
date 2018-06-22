// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Games.Testing;

namespace ParticlesSampleTest
{
    public class ParticlesSampleTest
    {
        private const string Path = "samplesGenerated\\ParticlesSample\\Bin\\Windows\\Debug\\ParticlesSample.exe";

#if TEST_ANDROID
        private const PlatformType TestPlatform = PlatformType.Android;
#elif TEST_IOS
        private const PlatformType TestPlatform = PlatformType.iOS;
#else
        private const PlatformType TestPlatform = PlatformType.Windows;
#endif

        [Fact]
        public void TestLaunch()
        {
            using (var game = new GameTestingClient(Path, TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(5000));
            }
        }

        [Fact]
        public void TestInputs()
        {
            using (var game = new GameTestingClient(Path, TestPlatform))
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
