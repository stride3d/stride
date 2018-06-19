//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// This file is distributed under GPL v3. See LICENSE.md for details.
//using NUnit.Framework;
//
//using Xenko.Core;
//using Xenko.Core.IO;
//using Xenko.Core.Serialization.Assets;
//using Xenko.Engine;
//
//namespace Xenko.Audio.Tests
//{
//    /// <summary>
//    /// Class to test the behavior of the audio system when the audio device is invalidated (headphone unplug, device disabled, ...)
//    /// </summary>
//    [TestFixture]
//    public class TestInvalidationAudioContext
//    {
//        private AudioEngine engine;
//
//        private SoundEffect oneSound;
//
//        private SoundMusic sayuriPart;
//
//        [TestFixtureSetUp]
//        public void Initialize()
//        {
//            Game.InitializeAssetDatabase();
//
//            engine = AudioEngineFactory.NewAudioEngine();
//
//            using (var stream = ContentManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                oneSound = SoundEffect.Load(engine, stream);
//            }
//            oneSound.IsLooped = true;
//
//            sayuriPart = SoundMusic.Load(engine, ContentManager.FileProvider.OpenStream("MusicFishLampMp3", VirtualFileMode.Open, VirtualFileAccess.Read));
//            sayuriPart.IsLooped = true;
//        }
//
//        [TestFixtureTearDown]
//        public void Unititiliaze()
//        {
//            engine.Dispose();
//        }
//
//        [Test]
//        public void TestInvalidationDuringSoundEffectPlay()
//        {
//            oneSound.Play();
//
//            // user should unplug and plug back the the headphone here
//            // and check that sound restart without throwing any exceptions.
//            var count = 0;
//            while (count < 1500)
//            {
//                Assert.DoesNotThrow(()=>oneSound.Pause());
//                Assert.DoesNotThrow(()=>oneSound.Play());
//                ++count;
//
//                Utilities.Sleep(10);
//            }
//
//            oneSound.Stop();
//        }
//
//        [Test]
//        public void TestInvalidationDuringSoundMusicPlay()
//        {
//            sayuriPart.Play();
//
//            // user should unplug and plug back the the headphone here
//            // and check that sound restart without throwing any exceptions.
//            var count = 0;
//            while (count < 1500)
//            {
//                ++count;
//
//                Assert.DoesNotThrow(() => sayuriPart.Pause());
//                engine.Update();
//
//                Assert.DoesNotThrow(() => sayuriPart.Play());
//                engine.Update();
//
//                Utilities.Sleep(10);
//            }
//
//            sayuriPart.Stop();
//        }
//
//        [Test]
//        public void TestInitialInvalidEngine()
//        {
//            // user should unplug the headphone before starting the test
//
//            // check the initial status of the engine
//            Assert.AreEqual(AudioEngineState.Invalidated, engine.State);
//
//            // check that pausing or resuming the engine does not change the engine status
//            engine.PauseAudio();
//            Assert.AreEqual(AudioEngineState.Invalidated, engine.State);
//            engine.ResumeAudio();
//            Assert.AreEqual(AudioEngineState.Invalidated, engine.State);
//        }
//
//        [Test]
//        public void TestSoundMusicWhenEngineInvalidated()
//        {
//            // user should unplug the headphone before starting the test
//            var soundMusic = SoundMusic.Load(engine, ContentManager.FileProvider.OpenStream("MusicFishLampMp3", VirtualFileMode.Open, VirtualFileAccess.Read));
//
//            // check the initial status of the engine
//            Assert.AreEqual(AudioEngineState.Invalidated, engine.State);
//
//            // check that the play state of the music is never modified when engine is invalid
//            Assert.AreEqual(SoundPlayState.Stopped, soundMusic.PlayState);
//            Assert.DoesNotThrow(soundMusic.Play);
//            Assert.AreEqual(SoundPlayState.Stopped, soundMusic.PlayState);
//            Assert.DoesNotThrow(soundMusic.Pause);
//            Assert.AreEqual(SoundPlayState.Stopped, soundMusic.PlayState);
//            Assert.DoesNotThrow(soundMusic.Stop);
//            Assert.AreEqual(SoundPlayState.Stopped, soundMusic.PlayState);
//
//            // check that the source characteristics can be modified
//            Assert.DoesNotThrow(() => soundMusic.IsLooped = false);
//            Assert.False(soundMusic.IsLooped);
//            Assert.DoesNotThrow(() => soundMusic.IsLooped = true);
//            Assert.True(soundMusic.IsLooped);
//            
//            Assert.DoesNotThrow(() => soundMusic.Volume = 0.5f);
//            Assert.AreEqual(0.5f, soundMusic.Volume);
//            Assert.DoesNotThrow(() => soundMusic.Volume = 0.67f);
//            Assert.AreEqual(0.67f, soundMusic.Volume);
//
//            Assert.DoesNotThrow(soundMusic.ExitLoop);
//
//            Assert.DoesNotThrow(soundMusic.Dispose);
//            Assert.IsTrue(soundMusic.IsDisposed);
//        }
//
//        [Test]
//        public void TestSoundEffectWhenEngineInvalidated()
//        {
//            // user should unplug the headphone before starting the test
//            SoundEffect sound;
//            using (var stream = ContentManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
//                sound = SoundEffect.Load(engine, stream);
//
//            // check the initial status of the engine
//            Assert.AreEqual(AudioEngineState.Invalidated, engine.State);
//
//            // check that the play state of the music is never modified when engine is invalid
//            Assert.AreEqual(SoundPlayState.Stopped, sound.PlayState);
//            Assert.DoesNotThrow(sound.Play);
//            Assert.AreEqual(SoundPlayState.Stopped, sound.PlayState);
//            Assert.DoesNotThrow(sound.Pause);
//            Assert.AreEqual(SoundPlayState.Stopped, sound.PlayState);
//            Assert.DoesNotThrow(sound.Stop);
//            Assert.AreEqual(SoundPlayState.Stopped, sound.PlayState);
//
//            // check that the source characteristics can be modified
//            Assert.DoesNotThrow(() => sound.IsLooped = false);
//            Assert.False(sound.IsLooped);
//            Assert.DoesNotThrow(() => sound.IsLooped = true);
//            Assert.True(sound.IsLooped);
//
//            Assert.DoesNotThrow(() => sound.Volume = 0.5f);
//            Assert.AreEqual(0.5f, sound.Volume);
//            Assert.DoesNotThrow(() => sound.Volume = 0.67f);
//            Assert.AreEqual(0.67f, sound.Volume);
//
//            Assert.DoesNotThrow(() => sound.Pan = 0.5f);
//            Assert.AreEqual(0.5f, sound.Pan);
//            Assert.DoesNotThrow(() => sound.Pan = 0.67f);
//            Assert.AreEqual(0.67f, sound.Pan);
//            
//            Assert.DoesNotThrow(() => sound.Apply3D(new AudioListener(), new AudioEmitter()));
//            Assert.AreEqual(0f, sound.Pan);
//            
//            Assert.DoesNotThrow(sound.Reset3D);
//
//            Assert.DoesNotThrow(sound.ExitLoop);
//
//            Assert.DoesNotThrow(sound.Dispose);
//            Assert.IsTrue(sound.IsDisposed);
//        }
//    }
//}
