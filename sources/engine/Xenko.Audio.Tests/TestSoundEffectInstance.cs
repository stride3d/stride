//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// This file is distributed under GPL v3. See LICENSE.md for details.
//using System;
//
//using Xunit;
//
//using Xenko.Core;
//using Xenko.Core.IO;
//using Xenko.Core.Mathematics;
//using Xenko.Core.Serialization.Assets;
//using Xenko.Engine;
//
//namespace Xenko.Audio.Tests
//{
//    /// <summary>
//    /// Tests for <see cref="SoundEffect"/>.
//    /// </summary>
////    public class TestSoundEffectInstance
//    {
//        private AudioEngine defaultEngine;
//
//        private SoundEffect laugherMono;
//        private SoundEffect monoSoundEffect;
//        private SoundEffect stereoSoundEffect;
//        private SoundEffect continousMonoSoundEffect;
//        private SoundEffect continousStereoSoundEffect;
//
//        private SoundEffectInstance monoInstance;
//        private SoundEffectInstance stereoInstance;
//
//
//        [TestFixtureSetUp]
//        public void Initialize()
//        {
//            Game.InitializeAssetDatabase();
//
//            defaultEngine = AudioEngineFactory.NewAudioEngine();
//
//            using (var monoStream = ContentManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                monoSoundEffect = SoundEffect.Load(defaultEngine, monoStream);
//            }
//            using (var stereoStream = ContentManager.FileProvider.OpenStream("EffectStereo", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                stereoSoundEffect = SoundEffect.Load(defaultEngine, stereoStream);
//            }
//            using (var contStream = ContentManager.FileProvider.OpenStream("EffectToneA", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                continousMonoSoundEffect = SoundEffect.Load(defaultEngine, contStream);
//            }
//            using (var contStream = ContentManager.FileProvider.OpenStream("EffectToneAE", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                continousStereoSoundEffect = SoundEffect.Load(defaultEngine, contStream);
//            }
//            using (var monoStream = ContentManager.FileProvider.OpenStream("Effect44100Hz", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                laugherMono = SoundEffect.Load(defaultEngine, monoStream);
//            }
//
//            monoInstance = monoSoundEffect.CreateInstance();
//            stereoInstance = stereoSoundEffect.CreateInstance();
//        }
//
//        [TestFixtureTearDown]
//        public void Uninitialize()
//        {
//            monoSoundEffect.Dispose();
//            stereoSoundEffect.Dispose();
//            continousMonoSoundEffect.Dispose();
//            continousStereoSoundEffect.Dispose();
//
//            defaultEngine.Dispose();
//        }
//
//        /// <summary>
//        /// Test that:
//        ///  - 2 instances of a <see cref="SoundEffect"/> cannot play at the same time. 
//        ///  - 2 instances of 2 different <see cref="SoundEffect"/> can play at the same time.
//        ///  - an old instance is correctly stopped if the max number of sound tracks is reached.
//        /// </summary>
//        [Fact]
//        public void TestInstanceConcurrency()
//        {
//            ////////////////////////////////////////////////////////////////////////////
//            // 1. Check that two or more instances of different sound effect can play together
//            monoSoundEffect.Play();
//            stereoSoundEffect.Play();
//            continousMonoSoundEffect.Play();
//            Assert.Equal(SoundPlayState.Playing, monoSoundEffect.PlayState);
//            Assert.Equal(SoundPlayState.Playing, stereoSoundEffect.PlayState);
//            Assert.Equal(SoundPlayState.Playing, continousMonoSoundEffect.PlayState);
//            Utilities.Sleep(3000);
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that two or more instances of a same sound effect cannot play together
//            var inst1 = stereoSoundEffect.CreateInstance();
//            var inst2 = stereoSoundEffect.CreateInstance();
//            var inst3 = stereoSoundEffect.CreateInstance();
//            inst1.Play();
//            Assert.Equal(SoundPlayState.Playing, inst1.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst2.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst3.PlayState);
//            inst2.Play();
//            Assert.Equal(SoundPlayState.Stopped, inst1.PlayState);
//            Assert.Equal(SoundPlayState.Playing, inst2.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst3.PlayState);
//            Utilities.Sleep(1000); // wait a little bit for hearing checking
//            inst3.Play();
//            Utilities.Sleep(1000); // wait a little bit for hearing checking
//            Assert.Equal(SoundPlayState.Stopped, inst1.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst2.PlayState);
//            Assert.Equal(SoundPlayState.Playing, inst3.PlayState);
//            inst3.Pause();
//            Assert.Equal(SoundPlayState.Stopped, inst1.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst2.PlayState);
//            Assert.Equal(SoundPlayState.Paused, inst3.PlayState);
//            inst1.Play();
//            Assert.Equal(SoundPlayState.Playing, inst1.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst2.PlayState);
//            Assert.Equal(SoundPlayState.Stopped, inst3.PlayState);
//
//            inst1.Dispose();
//            inst2.Dispose();
//            inst3.Dispose();
//        }
//
//        /// <summary>
//        /// Test that multiple instances creations does not throw any OutOfMemory exceptions.
//        /// </summary>
//        [Fact]
//        public void TestMultipleCreations()
//        {
//            ///////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that creating and starting lots of instances in parallel does not crash
//            const int nbOfInstances = 100;
//            var instances = new SoundEffectInstance[nbOfInstances];
//            for (int i = 0; i < nbOfInstances; ++i)
//            {
//                instances[i] = monoSoundEffect.CreateInstance();
//                instances[i].Play();
//            }
//            Utilities.Sleep(2000);
//            foreach (var instance in instances)
//                instance.Dispose();
//
//            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that restarting again and again instances does not result in memory leaks (android recreate the voices)
//            for (int i = 0; i < 100; ++i)
//            {
//                monoSoundEffect.Play();
//                Utilities.Sleep(1); // SourceVoice.SubmitBuffer crashes in native code after the 64th successive iteration if this line is not here
//                monoSoundEffect.Stop();
//            }
//        }
//        /// <summary>
//        /// Test the behaviour of the dispose function.
//        /// </summary>
//        [Fact]
//        public void TestDispose()
//        {
//            //////////////////////////////////////////////////////////////
//            // 1. Check the value of the IsDisposed function before Disposal
//            var seInstance = monoSoundEffect.CreateInstance();
//            Assert.False(seInstance.IsDisposed, "The soundEffectInstance returned by CreateInstance is already marked as disposed.");
//
//            /////////////////////////////////////////
//            // 2. Check that dispose does not crash
//            Assert.DoesNotThrow(seInstance.Dispose, "SoundEffectInstance.Dispose crashed throwing an exception");
//
//            ///////////////////////////////////////////////////////////////////
//            // 3. Check the Disposal status of the instance after Dispose call
//            Assert.True(seInstance.IsDisposed, "The soundEffectInstance is not marked as 'Disposed' after call to SoundEffectInstance.Dispose");
//
//            /////////////////////////////////////////////////////////
//            // 4. Check that another call to Dispose does not crash
//            Assert.Throws<InvalidOperationException>(seInstance.Dispose, "Extra call to SoundEffectInstance.Dispose did not throw an invalid operation exception");
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 5. Check that there is not crash when disposing an instance while it is playing
//            var otherInst = monoSoundEffect.CreateInstance();
//            otherInst.Play();
//            Utilities.Sleep(100); // wait for commit
//            Assert.DoesNotThrow(otherInst.Dispose, "Call to SoundEffectInstance.Dispose after Play crashed throwing an exception");
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
//            Assert.DoesNotThrow(monoInstance.Play, "Call to SoundEffectInstance.Play crashed throwing an exception.");
//
//            ////////////////////////////////////////////////////////////////////
//            // 2. Check that a second call to Play while playing does not crash
//            Assert.DoesNotThrow(monoInstance.Play, "Second call to SoundEffectInstance.Play while playing crashed throwing an exception.");
//
//            ////////////////////////////////////////////
//            // 3. Listen that the played sound is valid
//            Utilities.Sleep(1500);
//
//            //////////////////////////////////////////////////////////////////
//            // 4. Check that there is no crash when restarting the sound 
//            Assert.DoesNotThrow(laugherMono.Play, "Restarting the audio sound after it finishes crashed throwing an exception.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 5. Check  that there is no crash when playing after pausing the sound and that play flow is correct
//            Utilities.Sleep(3000);
//            laugherMono.Pause();
//            Utilities.Sleep(500);
//            Assert.DoesNotThrow(laugherMono.Play, "Restarting the audio sound after pausing it crashed throwing an exception.");
//            Utilities.Sleep(3000);
//            laugherMono.Stop();
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 6. Check that there is no crash when playing after stopping a sound and that the play flow restart
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            laugherMono.Stop();
//            Utilities.Sleep(500);
//            Assert.DoesNotThrow(laugherMono.Play, "Restarting the audio sound after stopping it crashed throwing an exception.");
//            var countDown = 12000;
//            while (laugherMono.PlayState == SoundPlayState.Playing)
//            {
//                Utilities.Sleep(10);
//                countDown -= 10;
//
//                if (countDown<0)
//                    Assert.Fail("laugherMono never stopped playing.");
//            }
//
//            ////////////////////////////////////////////////////////////////
//            // 7. Play a stereo file a listen that the played sound is valid
//            stereoInstance.Play();
//            Utilities.Sleep(2500);
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 8. Check that Playing an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.Play, "SoundEffectInstance.Play did not throw the 'ObjectDisposedException' when called from a disposed object.");
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
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.Pause, "SoundEffectInstance.Pause did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////
//            // 2. Check that Pause does not crash when the sound is not started/stopped
//            Assert.DoesNotThrow(monoInstance.Pause, "Call to SoundEffectInstance.Pause crashed throwing an exception with a stopped sound.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that Pause does not crash and has the correct effect when called after playing
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            Assert.DoesNotThrow(laugherMono.Pause, "Call to SoundEffectInstance.Pause crashed throwing an exception.");
//            Utilities.Sleep(500);
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            laugherMono.Stop();
//
//            ////////////////////////////////////////////////////////////////////
//            // 4. Check that a second call to Pause while playing does not crash
//            monoInstance.Play();
//            monoInstance.Pause();
//            Assert.DoesNotThrow(monoInstance.Pause, "Second call to SoundEffectInstance.Pause while paused crashed throwing an exception.");
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
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.Stop, "SoundEffectInstance.Stop did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////
//            // 2. Check that Stop does not crash when the sound is not started/stopped
//            Assert.DoesNotThrow(monoInstance.Stop, "Call to SoundEffectInstance.Stop crashed throwing an exception with a stopped sound.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that Stop does not crash and has the correct effect when called during playing
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            Assert.DoesNotThrow(laugherMono.Stop, "Call to SoundEffectInstance.Stop crashed throwing an exception.");
//            Utilities.Sleep(500);
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            laugherMono.Stop();
//
//            ///////////////////////////////////////////////////////////////////////////
//            // 4. Check that a call to stop  does not crash when paused reset the sound
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            laugherMono.Pause();
//            Assert.DoesNotThrow(laugherMono.Stop, "Call to SoundEffectInstance.Stop while paused crashed throwing an exception.");
//            laugherMono.Play();
//            Utilities.Sleep(3000);
//            laugherMono.Stop();
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
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(dispInst.ExitLoop, "SoundEffectInstance.ExitLoop did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////////
//            // 2. Check that ExitLoop does not crash when the sound is not started/stopped
//            Assert.DoesNotThrow(monoInstance.ExitLoop, "Call to SoundEffectInstance.ExitLoop crashed throwing an exception with a stopped sound.");
//
//            /////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that ExitLoop does not crash and has the correct effect when called during looped playing
//            var loopedInst = monoSoundEffect.CreateInstance();
//            loopedInst.IsLooped = true;
//            loopedInst.Play();
//            Utilities.Sleep(10); // Play need to be commited
//            Assert.DoesNotThrow(loopedInst.ExitLoop, "Call to SoundEffectInstance.ExitLoop crashed throwing an exception.");
//            Utilities.Sleep(2000);
//            Assert.True(loopedInst.PlayState == SoundPlayState.Stopped, "SoundEffectInstance.ExitLoop has not properly stopped the looping proccess");
//
//            ////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that a call to ExitLoop does not crash when the sound is not looping
//            monoInstance.Play();
//            Utilities.Sleep(10); // Play need to be commited
//            Assert.DoesNotThrow(monoInstance.ExitLoop, "Call to SoundEffectInstance.ExitLoop with a non-looping sound crashed throwing an exception.");
//            Utilities.Sleep(1250);
//
//            ////////////////////////////////////////////////////////////////
//            // 5. Check that 2 consecutive calls to ExitLoop does not crash 
//            loopedInst.Play();
//            loopedInst.ExitLoop();
//            Utilities.Sleep(10); // Play need to be commited
//            Assert.DoesNotThrow(loopedInst.ExitLoop, "A consecutive second call to SoundEffectInstance.ExitLoop crashed throwing an exception.");
//            Utilities.Sleep(2000);
//
//            ////////////////////////////////////////////////////////////////
//            // 6. Check that ExitLoop does not modify the value of IsLooped 
//            Assert.Equal(true, loopedInst.IsLooped, "SoundEffectInstance.ExitLoop modified the value of IsLooped.");
//
//            loopedInst.Dispose();
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
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => vol = dispInst.Volume, "SoundEffectInstance.Volume { get } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//            Assert.Throws<ObjectDisposedException>(() => dispInst.Volume = 0, "SoundEffectInstance.Volume { set } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            ///////////////////////////////////////////////////////////
//            // 2. Check that Volume set/get do not crash on valid sound
//            var volInst = monoSoundEffect.CreateInstance();
//            Assert.DoesNotThrow(() => vol = volInst.Volume, "SoundEffectInstance.Volume { get } crashed.");
//            Assert.DoesNotThrow(() => volInst.Volume = 0.5f, "SoundEffectInstance.Volume { set } crashed.");
//            volInst.Dispose();
//
//            /////////////////////////////////////////////////////
//            // 3. Check that Volume value is set to 1 by default
//            Assert.Equal(1f, vol, "Default volume value is not 1.");
//
//            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that modifying the volume works and is correctly clamped (result => sound should go up and back down)
//            var contInst = continousMonoSoundEffect.CreateInstance();
//            contInst.IsLooped = true;
//            contInst.Play();
//            var currentVol = -1.5f;
//            var sign = 1f;
//            while (currentVol >= -1.5f)
//            {
//                Assert.DoesNotThrow(() => contInst.Volume = currentVol, "SoundEffectInstance.Volume { set } crashed.");
//                Assert.DoesNotThrow(() => vol = contInst.Volume, "SoundEffectInstance.Volume { get } crashed.");
//                Assert.Equal(MathUtil.Clamp(currentVol, 0, 1), vol, "The volume value is not what is supposed to be.");
//
//                Utilities.Sleep(10);
//                if (currentVol > 1.5)
//                    sign = -1;
//
//                currentVol += sign * 0.01f;
//            }
//            contInst.Stop();
//            contInst.Dispose();
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
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => looped = dispInst.IsLooped, "SoundEffectInstance.IsLooped { get } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//            Assert.Throws<ObjectDisposedException>(() => dispInst.IsLooped = false, "SoundEffectInstance.IsLooped { set } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that set IsLooped for a playing instance throw the 'InvalidOperationException'
//            monoInstance.Play();
//            Utilities.Sleep(10);  // play commit time
//            Assert.Throws<InvalidOperationException>(() => monoInstance.IsLooped = true, "SoundEffectInstance.IsLooped { set } did not throw the 'InvalidOperationException' when called from a playing sound.");
//            Utilities.Sleep(1000);
//
//            /////////////////////////////////////////////////
//            // 3. Check that IsLooped default value is false
//            Assert.False(monoInstance.IsLooped, "Default looping status is not false.");
//
//            /////////////////////////////////////////////////////////////
//            // 4. Check that IsLooped set/get do not crash on valid sound
//            var volInst = monoSoundEffect.CreateInstance();
//            Assert.DoesNotThrow(() => looped = volInst.IsLooped, "SoundEffectInstance.IsLooped { get } crashed.");
//            Assert.DoesNotThrow(() => volInst.IsLooped = true, "SoundEffectInstance.IsLooped { set } crashed.");
//            volInst.Dispose();
//
//            //////////////////////////////////////////////////////////////
//            // 5. Check that sound is looping when IsLooped is set to true
//            var loopedInst = monoSoundEffect.CreateInstance();
//            loopedInst.IsLooped = true;
//            loopedInst.Play();
//            Utilities.Sleep(1500);
//            Assert.True(loopedInst.PlayState == SoundPlayState.Playing, "Sound does not loop when Islooped is set to true.");
//            loopedInst.Stop();
//            loopedInst.Dispose();
//
//            //////////////////////////////////////////////////////////////////////////////
//            // 6. Check that sound is looping without any glitches with a continuous sound 
//            var contInst = continousMonoSoundEffect.CreateInstance();
//            contInst.IsLooped = true;
//            contInst.Play();
//            Utilities.Sleep(3000);
//            Assert.True(contInst.PlayState == SoundPlayState.Playing, "Sound does not loop when Islooped is set to true.");
//            contInst.Stop();
//            contInst.Dispose();
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
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            try
//            {
//                state = dispInst.PlayState;
//            }
//            catch (Exception e)
//            {
//                Assert.False(e is ObjectDisposedException, "SoundEffectInstance.PlayState { get } did throw the 'ObjectDisposedException' when called from a disposed object.");
//            }
//            
//            //////////////////////////////////////////////
//            // 2. Check that PlayState get does not crash
//            Assert.DoesNotThrow(() => state = monoInstance.PlayState, "SoundEffectInstance.PlayState { get } crashed on valid soundEffectInstance.");
//
//            ////////////////////////////////////////////////////////////////////
//            // 3. Check that PlayState default value is SoundPlayState.Stopped.
//            Assert.Equal(SoundPlayState.Stopped, state, "Default value of SoundEffectInstance.PlayState is not SoundPlayState.Stopped.");
//
//            //////////////////////////////////////////////////////////////////////
//            // 4. Check that PlayState value after Play is SoundPlayState.Playing.
//            monoInstance.Play();
//            Assert.Equal(SoundPlayState.Playing, monoInstance.PlayState, "Value of SoundEffectInstance.PlayState is not SoundPlayState.Playing after a call to Play");
//
//            //////////////////////////////////////////////////////////////////////
//            // 5. Check that PlayState value after Pause is SoundPlayState.Pause.
//            monoInstance.Pause();
//            Assert.Equal(SoundPlayState.Paused, monoInstance.PlayState, "Value of SoundEffectInstance.PlayState is not SoundPlayState.Pause after a call to Pause");
//
//            //////////////////////////////////////////////////////////////////////
//            // 6. Check that PlayState value after Stop is SoundPlayState.Stopped.
//            monoInstance.Stop();
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "Value of SoundEffectInstance.PlayState is not SoundPlayState.Stopped after a call to Stop");
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 7. Check that PlayState value is SoundPlayState.Stopped when the sound stops by itself.
//            monoInstance.Play();
//            Utilities.Sleep(1500);
//            Assert.Equal(SoundPlayState.Stopped, monoInstance.PlayState, "Value of SoundEffectInstance.PlayState is not SoundPlayState.Stopped when we reach the sound end");
//        }
//
//        /// <summary>
//        /// Test the behaviour of the Pan function.
//        /// </summary>
//        [Fact]
//        public void TestPan()
//        {
//            var pan = 0f;
//            /////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that get and set Pan for an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => pan = dispInst.Pan, "SoundEffectInstance.Pan { get } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//            Assert.Throws<ObjectDisposedException>(() => dispInst.Pan = 0f, "SoundEffectInstance.Pan { set } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            /////////////////////////////////////////////////////////////
//            // 2. Check that Pan set/get do not crash on valid sound
//            var newInst = monoSoundEffect.CreateInstance();
//            Assert.DoesNotThrow(() => pan = newInst.Pan, "SoundEffectInstance.Pan { get } crashed.");
//            Assert.DoesNotThrow(() => newInst.Pan = 0f, "SoundEffectInstance.Pan { set } crashed.");
//            newInst.Dispose();
//
//            /////////////////////////////////////////
//            // 3. Check that Pan default value is 0f
//            Assert.Equal(0f, monoInstance.Pan, "Default Pan value is not 0f.");
//
//            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that mono sound is Panning correctly (should listen => sound comming from left -> going to right -> going back to left)
//            var monoContInst = continousMonoSoundEffect.CreateInstance();
//            monoContInst.IsLooped = true;
//            monoContInst.Play();
//            const float BornValue = 1.5f;
//            var currentPan = -BornValue;
//            var sign = 1f;
//            while (currentPan >= -BornValue)
//            {
//                Assert.DoesNotThrow(() => monoContInst.Pan = currentPan, "SoundEffectInstance.Pan { set } crashed with varying values.");
//                Assert.Equal(MathUtil.Clamp(currentPan, -1, 1), monoContInst.Pan, "SoundEffectInstance.Pan { get } is not what it is supposed to be.");
//
//                currentPan += sign * 0.01f;
//
//                if (currentPan > BornValue)
//                    sign = -1f;
//
//                Utilities.Sleep(20);
//            }
//            monoContInst.Stop();
//
//            ////////////////////////////////////////////////////////////
//            // Check that Modifying the pan reset the 3D localization
//            var list = new AudioListener();
//            var emit = new AudioEmitter { Position = new Vector3(7, 0, 0) };
//            monoContInst.Apply3D(list, emit);
//            monoContInst.Play();
//            Utilities.Sleep(1000);
//            monoContInst.Pan = 0;
//            Utilities.Sleep(1000);
//            monoContInst.Stop();
//            monoContInst.Dispose();
//
//            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 5. Check that stereo sound is Panning correctly (should listen => sound comming from left -> going to right -> going back to left)
//            Utilities.Sleep(1000);
//            var stereoContInst = continousStereoSoundEffect.CreateInstance();
//            stereoContInst.IsLooped = true;
//            stereoContInst.Play();
//            currentPan = -BornValue;
//            sign = 1f;
//            while (currentPan >= -BornValue)
//            {
//                Assert.DoesNotThrow(() => stereoContInst.Pan = currentPan, "SoundEffectInstance.Pan { set } crashed with varying values.");
//                Assert.Equal(MathUtil.Clamp(currentPan, -1, 1), stereoContInst.Pan, "SoundEffectInstance.Pan { get } is not what it is supposed to be.");
//
//                currentPan += sign * 0.01f;
//
//                if (currentPan > BornValue)
//                    sign = -1f;
//
//                Utilities.Sleep(20);
//            }
//            stereoContInst.Stop();
//            stereoContInst.Dispose();
//            Utilities.Sleep(1000);
//        }
//
//        /// <summary>
//        /// Test the behaviour of the Apply3D function.
//        /// </summary>
//        [Fact]
//        public void TestApply3D()
//        {
//            var list = new AudioListener();
//            var emit = new AudioEmitter();
//            var monoInst = continousMonoSoundEffect.CreateInstance();
//            var stereoInst = continousStereoSoundEffect.CreateInstance();
//            monoInst.IsLooped = true;
//            stereoInst.IsLooped = true;
//
//            ////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that Apply3D for an Disposed instance throw the 'ObjectDisposedException'
//            var dispInst = monoSoundEffect.CreateInstance();
//            dispInst.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => dispInst.Apply3D(list, emit), "SoundEffectInstance.Apply3D did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            ////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that Apply3D with null arguments throw the 'ArgumentNullException'
//            Assert.Throws<ArgumentNullException>(() => monoInstance.Apply3D(null, emit), "SoundEffectInstance.Apply3D did not throw the 'ArgumentNullException' with a null listener.");
//            Assert.Throws<ArgumentNullException>(() => monoInstance.Apply3D(list, null), "SoundEffectInstance.Apply3D did not throw the 'ArgumentNullException' with a null emitter.");
//           
//            /////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that the sound is not modified when applying 3D at same position with mono sound
//            monoInst.Play();
//            Utilities.Sleep(1000);
//            monoInst.Apply3D(list, emit);
//            Utilities.Sleep(1000);
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            
//            ///////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that Apply3D throws InvalidOperationException when used on stereo sound
//            Assert.Throws<InvalidOperationException>(() => stereoInst.Apply3D(list, emit), "InvalidOperationException has not been thrown when used on stereo-sound.");
//            
//            /////////////////////////////////////////////////////
//            // 5. Check that mono signal attenuate with distance
//            monoInst.Play();
//            var currentDist = 0f;
//            while (currentDist < 5)
//            {
//                emit.Position = new Vector3(0, 0, currentDist);
//                monoInst.Apply3D(list, emit);
//
//                currentDist += 0.01f;
//
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            
//            ////////////////////////////////////////////////////////////////////////////////
//            // 6. Check that signal do not attenuate when we move both emitter and listener
//            monoInst.Play();
//            currentDist = 0f;
//            while (currentDist < 5)
//            {
//                emit.Position = new Vector3(0, 0, currentDist);
//                list.Position = new Vector3(0, 0, currentDist);
//                monoInst.Apply3D(list, emit);
//            
//                currentDist += 0.01f;
//            
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            
//            //////////////////////////////////////////////////////////////////////////////////////
//            // 7. Check that attenuation increase/decrease when modifying DistaceScale parameter
//            monoInst.Play();
//            var currentScale = 2f;
//            list.Position = Vector3.Zero;
//            emit.Position = Vector3.One;
//            while (currentScale > 0.01f )
//            {
//                emit.DistanceScale = currentScale;
//                monoInst.Apply3D(list, emit);
//            
//                currentScale -= 0.005f;
//            
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            
//            ////////////////////////////////////////////////////////////////////////////////////////////////
//            // 8. Check that mono the localization works properly by turning the emiter around the listener
//            // Should start from the front, go to the right, go to the back, go to the left, and finnaly go back to the front.
//            monoInst.Play();
//            var angle = 0f;
//            emit.DistanceScale = 1;
//            while (angle > -2*(float)Math.PI)
//            {
//                const float Offset = (float) Math.PI / 2;
//                emit.Position = new Vector3((float)Math.Cos(angle + Offset), 0, (float)Math.Sin(angle + Offset));
//            
//                monoInst.Apply3D(list, emit);
//            
//                angle -= 0.005f;
//            
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 9. Check that the sound frequency is modified properly when object move away and goes back to the listener (doppler effect)
//            monoInst.Play();
//            var speed = 0f;
//            const float SpeedLimit = 500f;
//            var sign = 1f;
//            emit.Position = new Vector3(0,0,1);
//            while (speed > -SpeedLimit)
//            {
//                emit.Velocity = new Vector3(0,0,speed);
//            
//                monoInst.Apply3D(list, emit);
//            
//                speed += sign * 0.005f * SpeedLimit;
//            
//                if (speed > SpeedLimit)
//                {
//                    sign = -1f;
//                    speed = 0;
//                }
//            
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            
//            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 10. Check that the sound frequency is not modified when both objects move away at same speed (doppler effect)
//            monoInst.Play();
//            speed = 0f;
//            while (speed < SpeedLimit)
//            {
//                emit.Velocity = new Vector3(0, 0, speed);
//                list.Velocity = new Vector3(0, 0, speed);
//            
//                monoInst.Apply3D(list, emit);
//            
//                speed += 0.005f * SpeedLimit;
//                
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(600);
//            
//            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 11. Check the doppler effect influence is modified properly when DopplerScale changes (should increase)
//            monoInst.Play();
//            var dopplerScale = 0.1f;
//            emit.Position = new Vector3(0, 0, 1);
//            emit.Velocity = new Vector3(0, 0, 10);
//            list.Velocity = Vector3.Zero;
//            while (dopplerScale < 50f)
//            {
//                emit.DopplerScale = dopplerScale;
//            
//                monoInst.Apply3D(list, emit);
//            
//                dopplerScale *= 1.01f;
//            
//                Utilities.Sleep(10);
//            }
//            monoInst.Stop();
//            Utilities.Sleep(1000);
//            emit.DopplerScale = 1;
//
//            /////////////////////////////////////////////////////////
//            // 12. Check that Apply3D does not perturb Volume value.
//            var contInst = continousMonoSoundEffect.CreateInstance();
//            contInst.IsLooped = true;
//            contInst.Play();
//            var volume = 1f;
//            sign = -1f;
//            while (volume <= 1f)
//            {
//                contInst.Volume = volume;
//                contInst.Apply3D(new AudioListener(), new AudioEmitter());
//                Assert.Equal(MathUtil.Clamp(volume, 0f, 1f), contInst.Volume, "Volume of continous sound is not what it should be.");
//            
//                volume += sign * 0.01f;
//
//                if (volume < 0)
//                    sign = 1f;
//
//                Utilities.Sleep(10);
//            }
//            contInst.Stop();
//            Utilities.Sleep(1000);
//
//            ////////////////////////////////////////////////
//            // 13. Check that Apply3D reset the Pan value.
//            contInst.Play();
//            contInst.Pan = 1;
//            Utilities.Sleep(1000);
//            contInst.Apply3D(new AudioListener(), new AudioEmitter { Position = new Vector3(-1,0,0)});
//            Assert.Equal(0, contInst.Pan, "The Pan value has not been reset.");
//            Utilities.Sleep(1000);
//            contInst.Stop();
//
//            contInst.Dispose();
//            stereoInst.Dispose();
//        }
//
//        [Fact]
//        public void TestReset3D()
//        {
//            var list = new AudioListener();
//            var emit = new AudioEmitter();
//            var monoInst = continousMonoSoundEffect.CreateInstance();
//            monoInst.IsLooped = true;
//
//            /////////////////////////////
//            // Check that Pitch is reset
//            emit.Position = new Vector3(0,0,1);
//            emit.Velocity = new Vector3(500,0,0);
//            monoInst.Apply3D(list, emit);
//            monoInst.Play();
//            Utilities.Sleep(1000);
//            monoInst.Reset3D();
//            Utilities.Sleep(1000);
//            monoInst.Stop();
//            Utilities.Sleep(500);
//
//            ///////////////////////////
//            // Check that Pan is reset
//            emit.Position = new Vector3(1, 0, 0);
//            emit.Velocity = new Vector3(0, 0, 0);
//            monoInst.Apply3D(list, emit);
//            monoInst.Play();
//            Utilities.Sleep(1000);
//            monoInst.Reset3D();
//            Utilities.Sleep(1000);
//            monoInst.Stop();
//            Utilities.Sleep(500);
//
//            ////////////////////////////////////////////
//            // Check that effect on the Volume is reset
//            emit.Position = new Vector3(0, 0, 10);
//            monoInst.Apply3D(list, emit);
//            monoInst.Play();
//            Utilities.Sleep(1000);
//            monoInst.Reset3D();
//            Utilities.Sleep(1000);
//            monoInst.Stop();
//            Utilities.Sleep(500);
//
//            /////////////////////////////////
//            // Check that Volume is not reset
//            monoInst.Volume = 0.5f;
//            monoInst.Reset3D();
//            monoInst.Play();
//            Utilities.Sleep(1000);
//            Assert.Equal(0.5f, monoInst.Volume, "Reset3D has modified the music Volume");
//            monoInst.Volume = 1f;
//            Utilities.Sleep(1000);
//            monoInst.Stop();
//            Utilities.Sleep(500);
//
//            monoInst.Dispose();
//        }
//    }
//}
