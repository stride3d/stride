//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// This file is distributed under GPL v3. See LICENSE.md for details.
//using System;
//using System.Collections.Generic;
//using System.IO;
//using Xunit;
//
//using Xenko.Core.IO;
//using Xenko.Core.Mathematics;
//using Xenko.Core.Serialization.Assets;
//using Xenko.Engine;
//
//namespace Xenko.Audio.Tests
//{
//    /// <summary>
//    /// Tests for <see cref="SoundMusic"/>.
//    /// </summary>
////    public class TestSoundMusic
//    {
//        private AudioEngine defaultEngine;
//
//        private SoundMusic monoInstance;
//        private SoundMusic stereoInstance;
//        private SoundMusic contInstance;
//        private SoundMusic mp3Instance;
//        private SoundMusic wavInstance;
//
//
//        private static Stream OpenDataBaseStream(string name)
//        {
//            return ContentManager.FileProvider.OpenStream(name, VirtualFileMode.Open, VirtualFileAccess.Read);
//        }
//
//        [TestFixtureSetUp]
//        public void Initialize()
//        {
//            Game.InitializeAssetDatabase();
//
//            defaultEngine = AudioEngineFactory.NewAudioEngine();
//
//            monoInstance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicBip"));
//            stereoInstance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicStereo"));
//            contInstance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicToneA"));
//            mp3Instance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            wavInstance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicBip"));
//        }
//
//        [TestFixtureTearDown]
//        public void Uninitialize()
//        {
//            monoInstance.Dispose();
//            stereoInstance.Dispose();
//            contInstance.Dispose();
//            mp3Instance.Dispose();
//            wavInstance.Dispose();
//
//            defaultEngine.Dispose();
//        }
//
//        private void ActiveAudioEngineUpdate(int miliSeconds)
//        {
//            TestAudioUtilities.ActiveAudioEngineUpdate(defaultEngine, miliSeconds);
//        }
//
//        /// <summary>
//        /// Test the behaviour of the load function.
//        /// </summary>
//        [Fact]
//        public void TestLoad()
//        {
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that SoundMusic.Load throws "ArgumentNullException" when either 'engine' or 'filename' param is null
//            // 1.1 Null engine
//            Assert.Throws<ArgumentNullException>(() => SoundMusic.Load(null, new MemoryStream()), "SoundMusic.Load did not throw 'ArgumentNullException' when called with a null engine.");
//            // 1.2 Null stream
//            Assert.Throws<ArgumentNullException>(() => SoundMusic.Load(defaultEngine, null), "SoundMusic.Load did not throw 'ArgumentNullException' when called with a null stream.");
//
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that the load function throws "ObjectDisposedException" when the audio engine is disposed.
//            var disposedEngine = AudioEngineFactory.NewAudioEngine();
//            disposedEngine.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => SoundMusic.Load(disposedEngine, OpenDataBaseStream("EffectToneA")), "SoundMusic.Load did not throw 'ObjectDisposedException' when called with a displosed audio engine.");
//            
//            ///////////////////////////
//            // 4. Load a valid wav file
//            Assert.DoesNotThrow(() => SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA")), "SoundMusic.Load have thrown an exception when called with an valid wav file.");
//
//            ///////////////////////////
//            // 5. Load a valid mp3 file
//            Assert.DoesNotThrow(() => SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3")), "SoundMusic.Load have thrown an exception when called with an valid wav file.");
//        }
//
//        /// <summary>
//        /// Test the behaviour of the load and play function.
//        /// </summary>
//        [Fact]
//        public void TestLoadAndPlayValid()
//        {
//            ///////////////////////////
//            // 1. Load a valid wav file
//            using (var music = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA")))
//            {
//                music.Play();
//                Assert.DoesNotThrow(() => ActiveAudioEngineUpdate(1000), "Successive SoundMusic.Load -> SoundMusic.Play have thrown an exception when called with an valid wav file.");
//            }
//
//            ///////////////////////////
//            // 2. Load a valid mp3 file
//            using (var music = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3")))
//            {
//                music.Play();
//                Assert.DoesNotThrow(() => ActiveAudioEngineUpdate(1000), "Successive SoundMusic.Load -> SoundMusic.Play have thrown an exception when called with an valid mp3 file.");
//            }
//
//            ActiveAudioEngineUpdate(100);
//        }
//
//        /// <summary>
//        /// Test the behaviour of the load and play function.
//        /// </summary>
//        /// <remarks> Check that the load function throws "InvalidOperationException" when the audio file stream is not valid</remarks>
//        [Fact]
//        public void TestLoadAndPlayInvalid1()
//        {
//            // 3.2 Invalid wav file format (4-channels)
//            using (var music = SoundMusic.Load(defaultEngine, OpenDataBaseStream("Music4Channels")))
//            {
//                music.Play();
//                Assert.Throws<InvalidOperationException>(() => ActiveAudioEngineUpdate(1000),
//                    "Successive SoundMusic.Load -> SoundMusic.Play did not throw 'InvalidOperationException' when called with an invalid 4-channels wav format.");
//            }
//        }
//
//        /// <summary>
//        /// Test the behaviour of the load and play function.
//        /// </summary>
//        /// <remarks> Check that the load function throws "InvalidOperationException" when the audio file stream is not valid</remarks>
//        [Fact]
//        public void TestLoadAndPlayInvalid2()
//        {
//            // 3.3 Corrupted Header wav file
//            using (var music = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicHeaderCorrupted")))
//            {
//                music.Play();
//                Assert.Throws<InvalidOperationException>(() => ActiveAudioEngineUpdate(1000),
//                    "Successive SoundMusic.Load -> SoundMusic.Play did not throw 'InvalidOperationException' when called with a corrupted wav data stream.");
//            }
//        }
//        /// <summary>
//        /// Test the behaviour of the load and play function.
//        /// </summary>
//        /// <remarks> Check that the load function throws "InvalidOperationException" when the audio file stream is not valid</remarks>
//        [Fact]
//        public void TestLoadAndPlayInvalid3()
//        {
//            // 3.4 Other wav file format
//            using (var music = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicSurround5Dot1")))
//            {
//                music.Play();
//                Assert.Throws<InvalidOperationException>(() => ActiveAudioEngineUpdate(1000),
//                    "Successive SoundMusic.Load -> SoundMusic.Play did not throw 'InvalidOperationException' when called with a unsupported wav format.");
//            }
//        }
//
//        /// <summary>
//        /// Test the behaviour of the load and play function.
//        /// </summary>
//        /// <remarks> Check that the load function throws "InvalidOperationException" when the audio file stream is not valid</remarks>
//        [Fact]
//        public void TestLoadAndPlayInvalid4()
//        {
//            // 3.6 Other file format
//            using (var music = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicInvalidFile")))
//            {
//                music.Play();
//                Assert.Throws<InvalidOperationException>(() => ActiveAudioEngineUpdate(1000),
//                    "Successive SoundMusic.Load -> SoundMusic.Play did not throw 'InvalidOperationException' when called with a unsupported txt format.");
//            }
//        }
//
//        /// <summary>
//        /// Test the behaviour of the dispose function.
//        /// </summary>
//        [Fact]
//        public void TestDispose()
//        {
//            //////////////////////////////////////////////////////////////
//            // 1. Check the value of the IsDisposed function before Disposal
//            var instance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            Assert.False(instance.IsDisposed, "The soundEffectInstance returned by CreateInstance is already marked as disposed.");
//
//            /////////////////////////////////////////
//            // 2. Check that dispose does not crash
//            Assert.DoesNotThrow(instance.Dispose, "SoundEffectInstance.Dispose crashed throwing an exception");
//
//            ///////////////////////////////////////////////////////////////////
//            // 3. Check the Disposal status of the instance after Dispose call
//            Assert.True(instance.IsDisposed, "The soundEffectInstance is not marked as 'Disposed' after call to SoundEffectInstance.Dispose");
//
//            /////////////////////////////////////////////////////////
//            // 4. Check that another call to Dispose does not crash
//            Assert.Throws<InvalidOperationException>(instance.Dispose, "Extra call to SoundEffectInstance.Dispose did not throw invalid operation exception");
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 5. Check that there is not crash when disposing an instance while it is playing
//            var otherInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            otherInst.Play();
//            ActiveAudioEngineUpdate(1000);
//            Assert.DoesNotThrow(otherInst.Dispose, "Call to SoundMusic.Dispose after Play crashed throwing an exception");
//
//            ///////////////////////////////////////////////////////////
//            // 6. Check that sound is correctly stopped after dispose.
//            ActiveAudioEngineUpdate(2000);
//
//            ///////////////////////////////////////////////////////////////////////////////////////////////////
//            // 7. Check that there is no crash if sound disposal happen before the sound is ready to be played
//            var otherInst2 = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            otherInst2.Play();
//            defaultEngine.Update();
//            defaultEngine.Update();
//            Assert.DoesNotThrow(otherInst2.Dispose, "Call to SoundMusic.Dispose after Play and before ready-to-play state crashed throwing an exception");
//            ActiveAudioEngineUpdate(1000);
//
//            ////////////////////////////////////////////////////////////////////////////////////
//            // 8. Check that there is no crash if sound disposal happen before real call to Play
//            var otherInst3 = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            otherInst3.Play();
//            otherInst3.Dispose();
//            Assert.DoesNotThrow(() => ActiveAudioEngineUpdate(1000), "Call to Engine.Update after commited Play/Dispose calls crashed throwing an exception");
//
//            /////////////////////////////////////////////////////////////////////////////////////
//            // 9. Check that there is no crash if sound disposal happen before real call to Stop
//            var otherInst4 = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            otherInst4.Play();
//            ActiveAudioEngineUpdate(1000);
//            otherInst4.Stop();
//            otherInst4.Dispose();
//            Assert.DoesNotThrow(() => ActiveAudioEngineUpdate(1000), "Call to Engine.Update after commited Stop/Dispose calls crashed throwing an exception");
//
//            /////////////////////////////////////////////////////////////////////////////////////
//            // 10. Check that there is no crash if sound disposal happen before real call to Stop
//            var otherInst5 = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            otherInst5.Play();
//            ActiveAudioEngineUpdate(1000);
//            otherInst5.Pause();
//            otherInst5.Dispose();
//            Assert.DoesNotThrow(() => ActiveAudioEngineUpdate(1000), "Call to Engine.Update after commited Pause/Dispose calls crashed throwing an exception");
//
//            /////////////////////////////////////////////////////////////////////////////////////
//            // 11. Check that there is no crash if sound disposal happen before real call to Stop
//            var otherInst6 = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//            otherInst6.Play();
//            ActiveAudioEngineUpdate(1000);
//            otherInst6.Volume = 0.2f;
//            otherInst6.Dispose();
//            Assert.DoesNotThrow(() => ActiveAudioEngineUpdate(1000), "Call to Engine.Update after commited Volume/Dispose calls crashed throwing an exception");
//        }
//
//        /// <summary>
//        /// Test the behaviour of the Play function.
//        /// </summary>
//        [Fact]
//        public void TestPlay()
//        {
//            /////////////////////////////////////
//            // 1. Check that Play does not crash
//            Assert.DoesNotThrow(monoInstance.Play, "Call to SoundMusic.Play crashed throwing an exception.");
//
//            ////////////////////////////////////////////////////////////////////
//            // 2. Check that a second call to Play while playing does not crash
//            Assert.DoesNotThrow(monoInstance.Play, "Second call to SoundMusic.Play while playing crashed throwing an exception.");
//
//            ////////////////////////////////////////////
//            // 3. Listen that the played sound is valid
//            ActiveAudioEngineUpdate(1500);
//
//            //////////////////////////////////////////////////////////////////
//            // 4. Check that there is no crash when restarting the sound 
//            Assert.DoesNotThrow(monoInstance.Play, "Restarting the audio sound after it finishes crashed throwing an exception.");
//            ActiveAudioEngineUpdate(200);
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 5. Check  that there is no crash when playing after pausing the sound and that play flow is correct
//            monoInstance.Pause();
//            ActiveAudioEngineUpdate(200);
//            Assert.DoesNotThrow(monoInstance.Play, "Restarting the audio sound after pausing it crashed throwing an exception.");
//            ActiveAudioEngineUpdate(1500);
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 6. Check that there is no crash when playing after stopping a sound and that the play flow restart
//            monoInstance.Play();
//            ActiveAudioEngineUpdate(200);
//
//            monoInstance.Stop();
//            ActiveAudioEngineUpdate(200);
//            Assert.DoesNotThrow(monoInstance.Play, "Restarting the audio sound after stopping it crashed throwing an exception.");
//            int timeCount = 0;
//            while (monoInstance.PlayState == SoundPlayState.Playing)
//            {
//                ActiveAudioEngineUpdate(1000);
//                timeCount += 1000;
//
//                if (timeCount>3000)
//                    Assert.Fail("SoundMusic.Play has not finished after 3 seconds."); 
//            }
//
//            ////////////////////////////////////////////////////////////////
//            // 7. Play a stereo file a listen that the played sound is valid
//            stereoInstance.Play();
//            ActiveAudioEngineUpdate(3000);
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 8. Check that Playing an Disposed instance throw the 'ObjectDisposedException'
//            var dispInstance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInstance.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInstance.Play, "SoundEffectInstance.Play did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            ////////////////////////////////////////////////////////////////
//            // 9. Play a mp3 file and listen that the played sound is valid
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(4000);
//
//            //////////////////////////////////////////////////////////////////////////////
//            // 10. Play another music and check that the previous one is correctly stopped
//            monoInstance.Play();
//            Assert.Equal(SoundPlayState.Playing, monoInstance.PlayState, "Mono intstance play status is not what it is supposed to be.");
//            Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "MP3 intstance play status is not what it is supposed to be.");
//            ActiveAudioEngineUpdate(1500);
//        }
//
//        /// <summary>
//        /// Test the behaviour of the Pause function.
//        /// </summary>
//        [Fact]
//        public void TestPause()
//        {
//            //////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that pausing an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.Pause, "SoundMusic.Pause did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////
//            // 2. Check that Pause does not crash when the sound is not started/stopped
//            Assert.DoesNotThrow(monoInstance.Pause, "Call to SoundMusic.Pause crashed throwing an exception with a stopped sound.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that Pause does not crash and has the correct effect when called after playing
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2000);
//            Assert.DoesNotThrow(mp3Instance.Pause, "Call to SoundMusic.Pause crashed throwing an exception.");
//            ActiveAudioEngineUpdate(500);
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2000);
//
//            ////////////////////////////////////////////////////////////////////
//            // 4. Check that a second call to Pause while playing does not crash
//            mp3Instance.Play();
//            mp3Instance.Pause();
//            Assert.DoesNotThrow(mp3Instance.Pause, "Second call to SoundMusic.Pause while paused crashed throwing an exception.");
//
//            ///////////////////////////////////////////////////////////////
//            // 5. Check that Pause from another instance has no influence
//            mp3Instance.Play();
//            monoInstance.Pause();
//            ActiveAudioEngineUpdate(1000);
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "Pause from another music instance stopped current music.");
//            mp3Instance.Stop();
//            ActiveAudioEngineUpdate(1000);
//        }
//
//        /// <summary>
//        /// Test the behaviour of the Stop function.
//        /// </summary>
//        [Fact]
//        public void TestStop()
//        {
//            //////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that stopping an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.Stop, "SoundMusic.Stop did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////
//            // 2. Check that Stop does not crash when the sound is not started/stopped
//            Assert.DoesNotThrow(mp3Instance.Stop, "Call to SoundMusic.Stop crashed throwing an exception with a stopped sound.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that Stop does not crash and has the correct effect when called during playing
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2000);
//            Assert.DoesNotThrow(mp3Instance.Stop, "Call to SoundMusic.Stop crashed throwing an exception.");
//            ActiveAudioEngineUpdate(600);
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2000);
//            mp3Instance.Stop();
//            ActiveAudioEngineUpdate(2000);
//
//            ///////////////////////////////////////////////////////////////////////////
//            // 4. Check that a call to stop  does not crash when paused reset the sound
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2000);
//            mp3Instance.Pause();
//            Assert.DoesNotThrow(mp3Instance.Stop, "Call to SoundMusic.Stop while paused crashed throwing an exception.");
//            ActiveAudioEngineUpdate(600);
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2000);
//            
//            ///////////////////////////////////////////////////////////////
//            // 5. Check that Pause from another instance has no influence
//            mp3Instance.Play();
//            monoInstance.Stop();
//            ActiveAudioEngineUpdate(1000);
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "Stop from another music instance stopped current music.");
//            ActiveAudioEngineUpdate(1000);
//            mp3Instance.Stop();
//
//            ActiveAudioEngineUpdate(100);
//        }
//        
//        /// <summary>
//        /// Test the behaviour of the ExitLoop function.
//        /// </summary>
//        [Fact]
//        public void TestExitLoop()
//        {
//            //////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that ExitLoop an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.ExitLoop, "SoundMusic.ExitLoop did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////////
//            // 2. Check that ExitLoop does not crash when the sound is not started/stopped
//            Assert.DoesNotThrow(monoInstance.ExitLoop, "Call to SoundMusic.ExitLoop crashed throwing an exception with a stopped sound.");
//            
//            /////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that ExitLoop does not crash and has the correct effect when called during looped playing
//            var loopedInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectBip"));
//            loopedInst.IsLooped = true;
//            loopedInst.Play();
//            ActiveAudioEngineUpdate(1000); // Play need to be commited
//            Assert.DoesNotThrow(loopedInst.ExitLoop, "Call to SoundMusic.ExitLoop crashed throwing an exception.");
//            ActiveAudioEngineUpdate(1500);
//            Assert.Equal(SoundPlayState.Stopped, loopedInst.PlayState, "SoundMusic.ExitLoop has not properly stopped the looping proccess");
//
//            ////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that a call to ExitLoop does not crash when the sound is not looping
//            monoInstance.Play();
//            ActiveAudioEngineUpdate(100); // Play need to be commited
//            Assert.DoesNotThrow(monoInstance.ExitLoop, "Call to SoundMusic.ExitLoop with a non-looping sound crashed throwing an exception.");
//            ActiveAudioEngineUpdate(1250);
//
//            ////////////////////////////////////////////////////////////////
//            // 5. Check that 2 consecutive calls to ExitLoop does not crash 
//            loopedInst.Play();
//            ActiveAudioEngineUpdate(100); // Play need to be commited
//            loopedInst.ExitLoop();
//            Assert.DoesNotThrow(loopedInst.ExitLoop, "A consecutive second call to SoundEffectInstance.ExitLoop crashed throwing an exception.");
//            ActiveAudioEngineUpdate(1500);
//
//            ////////////////////////////////////////////////////////////////
//            // 6. Check that ExitLoop does not modify the value of IsLooped 
//            Assert.Equal(true, loopedInst.IsLooped, "SoundMusic.ExitLoop modified the value of IsLooped.");
//
//            ///////////////////////////////////////////////////////////////////////////////////////////
//            // 7. Check that a call to ExitLoop from another instance do not affect current instance.
//            loopedInst.Play();
//            ActiveAudioEngineUpdate(100); // Play need to be commited
//            monoInstance.ExitLoop();
//            ActiveAudioEngineUpdate(2000);
//            Assert.Equal(SoundPlayState.Playing, loopedInst.PlayState, "Call to ExitLoop from another instance influenced current playing instance.");
//            loopedInst.ExitLoop();
//            ActiveAudioEngineUpdate(1500);
//        }
//        
//        /// <summary>
//        /// Test the behaviour of the Volume function.
//        /// </summary>
//        [Fact]
//        public void TestVolume()
//        {
//            float vol = 0;
//            //////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that get and set Volume for an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => vol = dispInst.Volume, "SoundMusic.Volume { get } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//            Assert.Throws<ObjectDisposedException>(() => dispInst.Volume = 0, "SoundMusic.Volume { set } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            ///////////////////////////////////////////////////////////
//            // 2. Check that Volume set/get do not crash on valid sound
//            var volInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectBip"));
//            Assert.DoesNotThrow(() => vol = volInst.Volume, "SoundMusic.Volume { get } crashed.");
//            Assert.DoesNotThrow(() => volInst.Volume = 0.5f, "SoundMusic.Volume { set } crashed.");
//
//            /////////////////////////////////////////////////////
//            // 3. Check that Volume value is set to 1 by default
//            Assert.Equal(1f, vol, "Default volume value is not 1.");
//            
//            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that modifying the volume works and is correctly clamped (result => sound should go up and back down)
//            volInst.IsLooped = true;
//            volInst.Play();
//            var currentVol = -0.3f;
//            var sign = 1f;
//            while (currentVol >= -0.3f)
//            {
//                Assert.DoesNotThrow(() => volInst.Volume = currentVol, "SoundMusic.Volume { set } crashed.");
//                Assert.DoesNotThrow(() => vol = volInst.Volume, "SoundMusic.Volume { get } crashed.");
//                Assert.Equal(MathUtil.Clamp(currentVol, 0, 1), vol, "The volume value is not what is supposed to be.");
//
//                ActiveAudioEngineUpdate(10);
//                if (currentVol > 1.3)
//                    sign = -1;
//
//                currentVol += sign * 0.005f;
//            }
//            volInst.Stop();
//            ActiveAudioEngineUpdate(1000);
//
//            ///////////////////////////////////////////////////////////////////////////////////////////
//            // 5. Check that modifying the volume on another instance does not affect current instance
//            var volInst2 = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectBip"));
//            volInst.Play();
//            volInst.Volume = 1;
//            ActiveAudioEngineUpdate(1500);
//            volInst2.Volume = 0.2f;
//            ActiveAudioEngineUpdate(1500);
//            volInst.Stop();
//            ActiveAudioEngineUpdate(1000);
//            
//            //////////////////////////////////////////////////////////////////////////////////
//            // 6. Check that volume is correctly memorised and updated when changing music
//            volInst2.Volume = 0.2f;
//            volInst.Volume = 1f;
//            volInst2.IsLooped = true;
//            volInst.Play();
//            ActiveAudioEngineUpdate(1500);
//            volInst2.Play();
//            ActiveAudioEngineUpdate(1500);
//            volInst.Play();
//            ActiveAudioEngineUpdate(1500);
//            volInst2.Stop();
//
//            ///////////////////////////////////////////////////////////////////////////////
//            // 7. Check that modifying SoundMusic volume does not modify SoundEffectVolume
//            SoundEffect soundEffect;
//            using (var contStream = ContentManager.FileProvider.OpenStream("EffectToneA", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                soundEffect = SoundEffect.Load(defaultEngine, contStream);
//            }
//            soundEffect.IsLooped = true;
//            soundEffect.Play();
//            volInst.Play();
//            currentVol = -0.3f;
//            sign = 1f;
//            while (currentVol >= -0.3f)
//            {
//                volInst.Volume = currentVol;
//
//                ActiveAudioEngineUpdate(10);
//                if (currentVol > 1.3)
//                    sign = -1;
//
//                currentVol += sign * 0.005f;
//            }
//            volInst.Stop();
//            soundEffect.Stop();
//            ActiveAudioEngineUpdate(1000);
//        }
//
//        /// <summary>
//        /// Test the behaviour of the IsLooped function.
//        /// </summary>
//        [Fact]
//        public void TestIsLooped()
//        {
//            var looped = false;
//            //////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that get and set IsLooped for an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => looped = dispInst.IsLooped, "SoundMusic.IsLooped { get } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//            Assert.Throws<ObjectDisposedException>(() => dispInst.IsLooped = false, "SoundMusic.IsLooped { set } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that set IsLooped for a playing instance throw the 'InvalidOperationException'
//            var loopedWavInstance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectBip"));
//            loopedWavInstance.Play();
//            Assert.Throws<InvalidOperationException>(() => loopedWavInstance.IsLooped = true, "SoundMusic.IsLooped { set } did not throw the 'InvalidOperationException' when called from a playing sound.");
//            ActiveAudioEngineUpdate(2000);
//
//            /////////////////////////////////////////////////
//            // 3. Check that IsLooped default value is false
//            Assert.False(loopedWavInstance.IsLooped, "Default looping status is not false.");
//
//            /////////////////////////////////////////////////////////////
//            // 4. Check that IsLooped set/get do not crash on valid sound
//            Assert.DoesNotThrow(() => looped = loopedWavInstance.IsLooped, "SoundMusic.IsLooped { get } crashed.");
//            Assert.DoesNotThrow(() => loopedWavInstance.IsLooped = true, "SoundMusic.IsLooped { set } crashed.");
//
//            //////////////////////////////////////////////////////////////
//            // 5. Check that sound is looping when IsLooped is set to true
//            loopedWavInstance.IsLooped = true;
//            loopedWavInstance.Play();
//            ActiveAudioEngineUpdate(3000);
//            Assert.Equal(SoundPlayState.Playing, loopedWavInstance.PlayState, "Sound does not loop when Islooped is set to true.");
//            loopedWavInstance.Stop();
//            ActiveAudioEngineUpdate(2000);
//
//            //////////////////////////////////////////////////////////////
//            // 6. Check that sound is looping when IsLooped is set to true
//            var loopedMP3Instance = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicBipMp3"));
//            loopedMP3Instance.IsLooped = true;
//            loopedMP3Instance.Play();
//            ActiveAudioEngineUpdate(3000);
//            Assert.Equal(SoundPlayState.Playing, loopedMP3Instance.PlayState, "Sound does not loop when Islooped is set to true.");
//            loopedMP3Instance.Stop();
//            ActiveAudioEngineUpdate(2000);
//
//            ////////////////////////////////////////////////////////////////////////
//            // 7. Check that sound has no glitches when looped a continuous sound
//            contInstance.IsLooped = true;
//            contInstance.Play();
//            ActiveAudioEngineUpdate(3000);
//            Assert.Equal(SoundPlayState.Playing, contInstance.PlayState , "Sound does not loop when Islooped is set to true.");
//            contInstance.Stop();
//        }
//
//        /// <summary>
//        /// Test the behaviour of the PlayState function.
//        /// </summary>
//        [Fact]
//        public void TestPlayState()
//        {
//            var state = SoundPlayState.Stopped;
//            ////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that get PlayState for an Disposed instance does not throw the 'ObjectDisposedException'
//            var dispInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectToneA"));
//            dispInst.Dispose();
//            try
//            {
//                state = dispInst.PlayState;
//            }
//            catch (Exception e)
//            {
//                Assert.False(e is ObjectDisposedException, "SoundMusic.PlayState { get } did throw the 'ObjectDisposedException' when called from a disposed object.");
//            }
//
//            //////////////////////////////////////////////
//            // 2. Check that PlayState get does not crash
//            Assert.DoesNotThrow(() => state = monoInstance.PlayState, "SoundMusic.PlayState { get } crashed on valid soundEffectInstance.");
//
//            ////////////////////////////////////////////////////////////////////
//            // 3. Check that PlayState default value is SoundPlayState.Stopped.
//            var newInst = SoundMusic.Load(defaultEngine, OpenDataBaseStream("EffectBip"));
//            Assert.Equal(SoundPlayState.Stopped, newInst.PlayState, "Default value of SoundMusic.PlayState is not SoundPlayState.Stopped.");
//            
//            //////////////////////////////////////////////////////////////////////
//            // 4. Check that PlayState value after Play is SoundPlayState.Playing.
//            monoInstance.Play();
//            Assert.Equal(SoundPlayState.Playing, monoInstance.PlayState, "Value of SoundMusic.PlayState is not SoundPlayState.Playing after a call to Play");
//            
//            //////////////////////////////////////////////////////////////////////
//            // 5. Check that PlayState value after Pause is SoundPlayState.Pause.
//            monoInstance.Pause();
//            Assert.Equal(SoundPlayState.Paused, monoInstance.PlayState, "Value of SoundMusic.PlayState is not SoundPlayState.Pause after a call to Pause");
//
//            //////////////////////////////////////////////////////////////////////
//            // 6. Check that PlayState value after Stop is SoundPlayState.Stopped.
//            monoInstance.Stop();
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "Value of SoundMusic.PlayState is not SoundPlayState.Stopped after a call to Stop");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 7. Check that PlayState value is SoundMusic.Stopped when the sound stops by itself.
//            monoInstance.Play();
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "Value of SoundMusic.PlayState is not SoundPlayState.Stopped when we reach the sound end");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 8. Check that PlayState value is SoundMusic.Stopped when the sound stops by another music.
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(800);
//            monoInstance.Play();
//            Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "Value of SoundMusic.PlayState is not SoundPlayState.Stopped when stopped by another music");
//            ActiveAudioEngineUpdate(2500);
//        }
//
//
//        delegate void TestSimpleAction();
//
//        /// <summary>
//        /// Test the various problems that happened are fixed.
//        /// </summary>
//        [Fact]
//        public void TestVarious()
//        {
//            // test 0
//            mp3Instance.Play();
//            mp3Instance.Stop();
//            mp3Instance.Play();
//            mp3Instance.Stop();
//            mp3Instance.Play();
//            mp3Instance.Stop();
//            mp3Instance.Play();
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "mp3Instance is not playing (test0)");
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "mp3Instance is not still playing (test0)");
//            mp3Instance.Stop();
//            Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "mp3Instance is not stopped (test0)");
//            
//            //// test 1
//            monoInstance.Play();
//            monoInstance.Stop();
//            monoInstance.Play();
//            monoInstance.Stop();
//            monoInstance.Play();
//            monoInstance.Stop();
//            monoInstance.Play();
//            Assert.Equal(SoundPlayState.Playing, monoInstance.PlayState, "monoInstance is not playing (test1)");
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not stopped (test1)");
//
//            // test 2
//            mp3Instance.Play();
//            mp3Instance.Stop();
//            monoInstance.Play();
//            monoInstance.Stop();
//            monoInstance.Play();
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not stopped (test2)");
//            Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "mp3Instance is not stopped (test2)");
//            
//            // test 3
//            monoInstance.Play();
//            mp3Instance.Play();
//            monoInstance.Play();
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "mp3Instance is not playing (test3)");
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not stopped (test3)");
//            monoInstance.Play();
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "mp3Instance is not stopped (test3)");
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not playing (test3)");
//
//            // test 4
//            monoInstance.Play();
//            mp3Instance.Play();
//            monoInstance.Play();
//            Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "mp3Instance is not stopped (test4)");
//            ActiveAudioEngineUpdate(2500);
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not Stopped (test4)");
//
//            // test 5
//            monoInstance.Play();
//            contInstance.Play();
//            mp3Instance.Play();
//            monoInstance.Stop();
//            contInstance.Stop();
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not stopped (test5)");
//            Assert.Equal(SoundPlayState.Stopped, contInstance.PlayState, "contInstance is not stopped (test5)");
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "mp3Instance is not playing (test5)");
//            ActiveAudioEngineUpdate(2100);
//            Assert.Equal(SoundPlayState.Playing, mp3Instance.PlayState, "mp3Instance is not still playing (test5)");
//            
//            // test 6: delayed Pause after several play
//            monoInstance.Play();
//            monoInstance.Pause();
//            Assert.Equal(SoundPlayState.Paused, monoInstance.PlayState, "monoInstance is not Paused 1(test6)");
//            ActiveAudioEngineUpdate(200);
//            Assert.Equal(SoundPlayState.Paused, monoInstance.PlayState, "monoInstance is not Paused 2(test6)");
//            monoInstance.Stop();
//            monoInstance.Play();
//            mp3Instance.Play();
//            mp3Instance.Pause();
//            Assert.Equal(SoundPlayState.Paused, mp3Instance.PlayState, "mp3Instance is not Paused 1(test6)");
//            ActiveAudioEngineUpdate(500);
//            Assert.Equal(SoundPlayState.Paused, mp3Instance.PlayState, "mp3Instance is not Paused 2(test6)");
//            monoInstance.Play();
//            contInstance.Play();
//            mp3Instance.Play();
//            mp3Instance.Pause();
//            Assert.Equal(SoundPlayState.Paused, mp3Instance.PlayState, "mp3Instance is not Paused 3(test6)");
//            ActiveAudioEngineUpdate(500);
//            Assert.Equal(SoundPlayState.Paused, mp3Instance.PlayState, "mp3Instance is not Paused 4(test6)");
//          
//
//            // test 7 : random tests.
//            var actionsSet = new List<TestSimpleAction>
//                {
//                    //() => { Console.WriteLine("monoInstancePlay");monoInstance.Play();   } ,
//                    //() => {Console.WriteLine("monoInstancePause");monoInstance.Pause();  } ,
//                    //() => { Console.WriteLine("monoInstanceStop");monoInstance.Stop();   } ,
//                    //() => { Console.WriteLine("contInstancePlay");contInstance.Play();   } ,
//                    //() => { Console.WriteLine("contInstancePause");contInstance.Pause(); } ,
//                    //() => { Console.WriteLine("contInstanceStop");contInstance.Stop();   } ,
//                    //() => { Console.WriteLine("mp3InstancePlay"); mp3Instance.Play();    } ,
//                    //() => { Console.WriteLine("mp3InstancePause");mp3Instance.Pause();   } ,
//                    //() => { Console.WriteLine("mp3InstanceStop"); mp3Instance.Stop();    } ,
//                    monoInstance.Play,
//                    monoInstance.Pause,
//                    monoInstance.Stop,
//                    contInstance.Play,
//                    contInstance.Pause,
//                    contInstance.Stop,
//                    mp3Instance.Play,
//                    mp3Instance.Pause,
//                    mp3Instance.Stop
//                };
//            contInstance.IsLooped = false;
//            const int NbOfRandActions = 50;
//            for (int i = 0; i < 10; i++)
//            {
//                var rand1 = new Random(i);
//                for (int j = 0; j < NbOfRandActions; j++)
//                    actionsSet[rand1.Next(actionsSet.Count)].Invoke();
//                //Console.WriteLine("monoInstancePlay");
//                monoInstance.Play();
//                Assert.Equal(SoundPlayState.Playing, monoInstance.PlayState, "monoInstance is not Playing 1(test7)");
//                Assert.Equal(SoundPlayState.Stopped, contInstance.PlayState, "contInstance is not Stopped 1(test7)");
//                Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "mp3Instance is not Stopped 1(test7)");
//                ActiveAudioEngineUpdate(2500);
//                Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not Stopped 2(test7)");
//
//
//                //Console.WriteLine("");
//                //Console.WriteLine("END OF LOOP {0}", i);
//                //Console.WriteLine("");
//                
//                var rand2 = new Random();   // random seed
//                for (int j = 0; j < NbOfRandActions; j++)
//                    actionsSet[rand2.Next(actionsSet.Count)].Invoke();
//                //Console.WriteLine("contInstancePlay");
//                contInstance.Play();
//                Assert.Equal(SoundPlayState.Playing, contInstance.PlayState, "contInstance is not Playing 3(test7)");
//                Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "monoInstance is not Stopped 3(test7)");
//                Assert.Equal(SoundPlayState.Stopped, mp3Instance.PlayState, "mp3Instance is not Stopped 3(test7)");
//                ActiveAudioEngineUpdate(3000);
//                Assert.Equal(SoundPlayState.Stopped, contInstance.PlayState, "contInstance is not Stopped 4(test7)");
//            }
//            contInstance.Stop();
//            contInstance.IsLooped = true;
//
//            // test 8 : try to create a lot of instances
//            {
//                var musics = new SoundMusic[1000];
//                for (int i = 0; i < musics.Length; i++)
//                    musics[i] = SoundMusic.Load(defaultEngine, OpenDataBaseStream("MusicFishLampMp3"));
//                
//                musics[musics.Length-1].Play();
//                ActiveAudioEngineUpdate(3000);
//                musics[musics.Length / 2].Play();
//                ActiveAudioEngineUpdate(3000);
//
//                foreach (var music in musics)
//                    music.Dispose();
//            }
//        }
//
//
//        /// <summary>
//        /// Specific tests based on how it has been implemented.
//        /// </summary>
//        [Fact]
//        public void TestImplSpecific()
//        {
//            // start a music
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2500);
//            
//            ////////////////////////////////////////////////////////
//            // Check that the music restart with an interleaved play
//            monoInstance.Play();
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2500);
//            
//            ////////////////////////////////////////////////////////
//            // Check that the music restart with an interleaved stop
//            mp3Instance.Play();
//            mp3Instance.Stop();
//            mp3Instance.Play();
//            ActiveAudioEngineUpdate(2500);
//            
//            ////////////////////////////////////////////////////////////
//            // Check that music is correctly paused directly after play
//            monoInstance.Stop();
//            ActiveAudioEngineUpdate(100);
//            monoInstance.Play();
//            monoInstance.Pause();
//            ActiveAudioEngineUpdate(2500);
//
//            ////////////////////////////////////////////////////////////
//            // Check that music is correctly stopped directly after play
//            monoInstance.Stop();
//            ActiveAudioEngineUpdate(100);
//            monoInstance.Play();
//            monoInstance.Pause();
//            ActiveAudioEngineUpdate(2500);
//
//            //////////////////////////////////////////////////////////////////////
//            // Check that a music play is not perturb by other instance pause/stop
//            mp3Instance.Play();
//            monoInstance.Pause();
//            monoInstance.Stop();
//            monoInstance.Play();
//            monoInstance.Pause();
//            monoInstance.Stop();
//            mp3Instance.Play();
//            monoInstance.Pause();
//            monoInstance.Stop();
//            contInstance.Stop();
//            ActiveAudioEngineUpdate(2500);
//
//            /////////////////////////////////////////////////////////
//            // Check that a sound restart correctly after it finishes
//            monoInstance.Play();
//            ActiveAudioEngineUpdate(2500);
//            monoInstance.Play();
//            mp3Instance.Stop();
//            mp3Instance.Pause();
//            ActiveAudioEngineUpdate(2500);
//
//            /////////////////////////////////////////////////////////
//            // Check what happens if paused or stopped when stopped
//            monoInstance.Play();
//            monoInstance.Stop();
//            ActiveAudioEngineUpdate(100);
//            monoInstance.Pause();
//            monoInstance.Stop();
//            ActiveAudioEngineUpdate(2500);
//        }
//    }
//}
