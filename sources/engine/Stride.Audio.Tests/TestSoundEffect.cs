//// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// This file is distributed under GPL v3. See LICENSE.md for details.
//using System;
//using System.IO;
//
//using Xunit;
//
//using Stride.Core;
//using Stride.Core.IO;
//using Stride.Core.Mathematics;
//using Stride.Core.Serialization.Assets;
//using Stride.Engine;
//
//namespace Stride.Audio.Tests
//{
//    /// <summary>
//    /// Tests for <see cref="SoundEffect"/>.
//    /// </summary>
////    public class TestSoundEffect
//    {
//        private AudioEngine defaultEngine;
//        private Stream validWavStream; 
//
//        [TestFixtureSetUp]
//        public void Initialize()
//        {
//            Game.InitializeAssetDatabase();
//
//            defaultEngine = AudioEngineFactory.NewAudioEngine();
//
//            validWavStream = ContentManager.FileProvider.OpenStream("EffectBip", VirtualFileMode.Open, VirtualFileAccess.Read);
//        }
//
//        [TestFixtureTearDown]
//        public void Uninitialize()
//        {
//            validWavStream.Dispose();
//
//            defaultEngine.Dispose();
//        }
//
//        /// <summary>
//        /// Test the behaviour of the load function.
//        /// </summary>
//        [Fact]
//        public void TestLoad()
//        {
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that SoundEffect.Load throws "ArgumentNullException" when either 'engine' or 'stream' param is null
//            // 1.1 Null engine
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            Assert.Throws<ArgumentNullException>(() => SoundEffect.Load(null, validWavStream), "SoundEffect.Load did not throw 'ArgumentNullException' when called with a null engine.");
//            // 1.2 Null stream
//            Assert.Throws<ArgumentNullException>(() => SoundEffect.Load(defaultEngine, null), "SoundEffect.Load did not throw 'ArgumentNullException' when called with a null stream.");
//
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that the load function throws "ObjectDisposedException" when the audio engine is disposed.
//            var disposedEngine = AudioEngineFactory.NewAudioEngine();
//            disposedEngine.Dispose();
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            Assert.Throws<ObjectDisposedException>(() => SoundEffect.Load(disposedEngine, validWavStream), "SoundEffect.Load did not throw 'ObjectDisposedException' when called with a displosed audio engine.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that the load function throws "InvalidOperationException" when the audio file stream is not valid
//            // 3.1 Invalid audio file format.
//            var otherFileFormatStream = ContentManager.FileProvider.OpenStream("EffectStereoOgg", VirtualFileMode.Open, VirtualFileAccess.Read);
//            Assert.Throws<InvalidOperationException>(() => SoundEffect.Load(defaultEngine, otherFileFormatStream), "SoundEffect.Load did not throw 'InvalidOperationException' when called with an invalid file extension.");
//            otherFileFormatStream.Dispose();
//            // 3.2 Corrupted Header wav file
//            var corruptedWavFormatStream = ContentManager.FileProvider.OpenStream("EffectHeaderCorrupted", VirtualFileMode.Open, VirtualFileAccess.Read);
//            Assert.Throws<InvalidOperationException>(() => SoundEffect.Load(defaultEngine, corruptedWavFormatStream), "SoundEffect.Load did not throw 'InvalidOperationException' when called with a corrupted wav stream.");
//            corruptedWavFormatStream.Dispose();
//            // 3.3 Invalid wav file format (4-channels)
//            var invalidChannelWavFormatStream = ContentManager.FileProvider.OpenStream("Effect4Channels", VirtualFileMode.Open, VirtualFileAccess.Read);
//            Assert.Throws<InvalidOperationException>(() => SoundEffect.Load(defaultEngine, invalidChannelWavFormatStream), "SoundEffect.Load did not throw 'InvalidOperationException' when called with an invalid 4-channels wav format.");
//            invalidChannelWavFormatStream.Dispose();
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that the load function throws "NotSupportedException" when the audio file stream is not supported
//            // 4.1 extented wav file format (5-channels)
//            var extendedChannelWavFormatStream = ContentManager.FileProvider.OpenStream("EffectSurround5Dot1", VirtualFileMode.Open, VirtualFileAccess.Read);
//            Assert.Throws<NotSupportedException>(() => SoundEffect.Load(defaultEngine, extendedChannelWavFormatStream), "SoundEffect.Load did not throw 'NotSupportedException' when called with an extended 5-channels wav format.");
//            extendedChannelWavFormatStream.Dispose();
//            // 4.2 Invalid wav file format (24bits encoding)
//            var invalidEncodingWavFormatStream = ContentManager.FileProvider.OpenStream("Effect24bits", VirtualFileMode.Open, VirtualFileAccess.Read);
//            Assert.Throws<NotSupportedException>(() => SoundEffect.Load(defaultEngine, invalidEncodingWavFormatStream), "SoundEffect.Load did not throw 'NotSupportedException' when called with an invalid ieeefloat encoding wav format.");
//            invalidEncodingWavFormatStream.Dispose();
//
//            ///////////////////////////////////////////////////////
//            // 5. Load a valid wav file stream and try to play it
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            Assert.DoesNotThrow(() => SoundEffect.Load(defaultEngine, validWavStream), "SoundEffect.Load have thrown an exception when called with an valid wav stream.");
//        }
//
//        /// <summary>
//        /// Test the behaviour of the create function.
//        /// </summary>
//        [Fact]
//        public void TestCreateInstance()
//        {
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that the create instance function throws "ObjectDisposedException" when soundEffect is disposed.
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            var soundEffect = SoundEffect.Load(defaultEngine, validWavStream);
//            soundEffect.Dispose();
//            Assert.Throws<ObjectDisposedException>(() => soundEffect.CreateInstance(), "SoundEffect.CreateInstance did not throw 'ObjectDisposedException' when called with a displosed soundEffect.");
//
//            //////////////////////////////////////////////////////////////
//            // 2. Load a valid wav file stream and create an new instance
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            var soundEffect2 = SoundEffect.Load(defaultEngine, validWavStream);
//            Assert.DoesNotThrow(() => soundEffect2.CreateInstance(), "SoundEffect.CreateInstance have thrown an exception when called with an soundEffect.");
//        }
//        
//        /// <summary>
//        /// Test the behaviour of the dispose function.
//        /// </summary>
//        [Fact]
//        public void TestDispose()
//        {
//            ////////////////////////////////////////////////////////////////////////
//            // 1. Check that the SoundEffect dispose status is correct after Loading
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            var soundEffect = SoundEffect.Load(defaultEngine, validWavStream);
//            Assert.False(soundEffect.IsDisposed, "SoundEffect was marked as 'disposed' after being loaded.");
//
//            ////////////////////////////////////////////////////////
//            // 2. Check that the SoundEffect.Dispose does not crash
//            Assert.DoesNotThrow(soundEffect.Dispose, "SoundEffect.Disposed as crash throwing an exception.");
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that the dispose status of SoundEffect is correct after being disposed
//            Assert.True(soundEffect.IsDisposed, "SoundEffect was not marked as 'Disposed' after being disposed");
//
//            ///////////////////////////////////////////////////////////////////////////////
//            // 4. Check that it does not crash if we make several calls to dispose function
//            Assert.Throws<InvalidOperationException>(soundEffect.Dispose, "SoundEffect.Disposed does not have called invalid operation exception when called a second time.");
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////
//            // 5. Check that soundEffect.Dispose stops playing and displose all subjacent SoundEffectInstances
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            var soundEffect2 = SoundEffect.Load(defaultEngine, validWavStream);
//            var inst1 = soundEffect2.CreateInstance();
//            var inst2 = soundEffect2.CreateInstance();
//            inst1.Play();
//            Utilities.Sleep(100);
//            inst2.Play();
//            Utilities.Sleep(100);
//            Assert.DoesNotThrow(soundEffect2.Dispose, "SoundEffect.Disposed have crashed throwing an exception when called with several subjacent instances playing.");
//            Assert.True(inst1.PlayState == SoundPlayState.Stopped && inst2.PlayState == SoundPlayState.Stopped, "SoundEffect subjacent instances have not been stopped correctly during soundEffect.Dispose");
//            Assert.True(inst1.IsDisposed && inst2.IsDisposed, "SoundEffect subjacent instances have not been disposed correctly during soundEffect.Dispose");
//        }
//
//        /// <summary>
//        /// Brief thests to check that the implementation of the ILocalizable interface based on a default instance works properly.
//        /// For real implementation test take a look at <see cref="TestSoundEffectInstance"/>
//        /// </summary>
//        [Fact]
//        public void TestDefaultInstance()
//        {
//            validWavStream.Seek(0, SeekOrigin.Begin);
//            var soundEffect = SoundEffect.Load(defaultEngine, validWavStream);
//
//            ///////////////////////////
//            // 1. Check default values
//            Assert.False(soundEffect.IsLooped, "SoundEffect is looping by default.");
//            Assert.Equal(SoundPlayState.Stopped, soundEffect.PlayState, "SoundEffect is not stopped by default.");
//            Assert.Equal(1f, soundEffect.Volume, "SoundEffect.Volume default value is not 1f.");
//            Assert.Equal(0f, soundEffect.Pan, "SoundEffect.Pan default value is not 0f.");
//
//            /////////////////////////
//            // 2. Check IPlayable
//            soundEffect.IsLooped = true;
//            soundEffect.Play();
//            Assert.Equal(SoundPlayState.Playing, soundEffect.PlayState, "SoundEffect.PlayState is not Playing.");
//            Utilities.Sleep(1000);
//            soundEffect.Pause();
//            Assert.Equal(SoundPlayState.Paused, soundEffect.PlayState, "SoundEffect.PlayState is not Paused.");
//            Utilities.Sleep(1000);
//            soundEffect.Play();
//            Utilities.Sleep(50);
//            soundEffect.ExitLoop();
//            Utilities.Sleep(1500);
//            Assert.Equal(SoundPlayState.Stopped, soundEffect.PlayState, "SoundEffect.PlayState is not Stopped after exitLoop.");
//
//            ////////////////////////
//            // 3. Volume and stop
//            soundEffect.Play();
//            Utilities.Sleep(1000);
//            soundEffect.Volume = 0.5f;
//            Utilities.Sleep(1000);
//            soundEffect.Stop();
//            Assert.Equal(SoundPlayState.Stopped, soundEffect.PlayState, "SoundEffect.PlayState is not Stopped.");
//            Utilities.Sleep(1000);
//            soundEffect.Volume = 1f;
//
//            /////////////////////
//            // 4. Pan
//            soundEffect.Pan = -1f;
//            soundEffect.Play();
//            Utilities.Sleep(1000);
//            soundEffect.Pan = 1f;
//            Utilities.Sleep(1000);
//            soundEffect.Stop();
//            Utilities.Sleep(1000);
//            soundEffect.Pan = 0;
//            
//            ///////////////////////////////////////////////////////////
//            // 6. Apply3D (should have no effect and then hear nothing)
//            soundEffect.Pan = 1;
//            soundEffect.Apply3D(new AudioListener(), new AudioEmitter());
//            soundEffect.Play();
//            Utilities.Sleep(1000);
//            soundEffect.Stop();
//            soundEffect.Apply3D(new AudioListener { Position = new Vector3(100,0,0)}, new AudioEmitter());
//            soundEffect.Play();
//            Utilities.Sleep(1000);
//            soundEffect.Stop();
//        }
//    }
//}
