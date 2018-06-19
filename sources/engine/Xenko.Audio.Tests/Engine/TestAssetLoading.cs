// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Engine;

namespace Xenko.Audio.Tests.Engine
{
    /// <summary>
    /// Test that <see cref="SoundEffect"/> and <see cref="SoundMusic"/> can be loaded without problem with the asset loader.
    /// </summary>
    [TestFixture]
    public class TestAssetLoading
    {
        /// <summary>
        /// Test loading and playing resulting <see cref="SoundEffect"/> 
        /// </summary>
        [Test]
        public void TestSoundEffectLoading()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestSoundEffectLoadingImpl, TestUtilities.ExitGameAfterSleep(1000));
        }

        private static SoundInstance testInstance;

        private static void TestSoundEffectLoadingImpl(Game game)
        {
            Sound sound = null;
            Assert.DoesNotThrow(() => sound = game.Content.Load<Sound>("EffectBip"), "Failed to load the soundEffect.");
            Assert.IsNotNull(sound, "The soundEffect loaded is null.");
            testInstance = sound.CreateInstance(game.Audio.AudioEngine.DefaultListener);
            testInstance.Play();
            // Should hear the sound here.
        }

        /// <summary>
        /// Test loading and playing resulting <see cref="SoundMusic"/>
        /// </summary>
        [Test]
        public void TestSoundMusicLoading()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestSoundMusicLoadingImpl, TestUtilities.ExitGameAfterSleep(2000));
        }

        private static void TestSoundMusicLoadingImpl(Game game)
        {
            Sound sound = null;
            Assert.DoesNotThrow(() => sound = game.Content.Load<Sound>("EffectBip"), "Failed to load the SoundMusic.");
            Assert.IsNotNull(sound, "The SoundMusic loaded is null.");
            testInstance = sound.CreateInstance(game.Audio.AudioEngine.DefaultListener);
            testInstance.Play();
            // Should hear the sound here.
        }
    }
}
