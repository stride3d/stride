//// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// This file is distributed under GPL v3. See LICENSE.md for details.
//using System;
//using System.IO;
//using System.Runtime.InteropServices;
//
//using Xunit;
//
//using Stride.Core;
//using Stride.Core.IO;
//using Stride.Core.Serialization.Assets;
//using Stride.Audio.Wave;
//using Stride.Engine;
//
//namespace Stride.Audio.Tests
//{
//    /// <summary>
//    /// Tests for <see cref="DynamicSoundEffectInstance"/>.
//    /// </summary>
////    public class TestDynamicSoundEffectInstance
//    {
//        private AudioEngine defaultEngine;
//
//        private SoundGenerator generator = new SoundGenerator();
//        
//        [TestFixtureSetUp]
//        public void Initialize()
//        {
//            Game.InitializeAssetDatabase();
//
//            defaultEngine = AudioEngineFactory.NewAudioEngine();
//        }
//
//        [TestFixtureTearDown]
//        public void Uninitialize()
//        {
//            defaultEngine.Dispose();
//        }
//
//
//        /// <summary>
//        /// Test the behavior of the IsLooped function.
//        /// </summary>
//        [Fact]
//        public void TestIsLooped()
//        {
//            mono8Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            var dispInstance = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Stereo, AudioDataEncoding.PCM_16Bits);
//            dispInstance.Dispose();
//
//            bool looped;
//            /////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that get and set IsLooped for an Disposed instance throw the 'ObjectDisposedException'
//            Assert.Throws<ObjectDisposedException>(() => looped = dispInstance.IsLooped, "DynamicSoundEffectInstance.IsLooped { get } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//            Assert.Throws<ObjectDisposedException>(() => dispInstance.IsLooped = false, "DynamicSoundEffectInstance.IsLooped { set } did not throw the 'ObjectDisposedException' when called from a disposed object.");
//
//            //////////////////////////////////////////////////////////////////
//            // 2. Check that IsLooped = true throws InvalidOperationException
//            Assert.Throws<InvalidOperationException>(() => mono8Bits.IsLooped = true, "DynamicSoundEffectInstance.IsLooped { set } did not throw the 'InvalidOperationException' when called with 'true'.");
//
//            ////////////////////////////////////////////////
//            // 3. Check that the value of IsLooped is false
//            Assert.False(mono8Bits.IsLooped, "DynamicSoundEffectInstance.IsLooped { get } did not return 'false'.");
//
//            mono8Bits.Dispose();
//        }
//
//        /// <summary>
//        /// Test the behavior of the constructor function.
//        /// </summary>
//        [Fact]
//        public void TestConstructor()
//        {
//            /////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Test that Constructor throws ArgumentOutOfRangeException with invalid parameters
//            Assert.Throws<ArgumentOutOfRangeException>(() => new DynamicSoundEffectInstance(defaultEngine, 7999, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits), "Did not throw argumentOutOfRangeException with too low sample rate");
//            Assert.Throws<ArgumentOutOfRangeException>(() => new DynamicSoundEffectInstance(defaultEngine, 48001, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits), "Did not throw argumentOutOfRangeException with too high sample rate");
//            Assert.Throws<ArgumentOutOfRangeException>(() => new DynamicSoundEffectInstance(defaultEngine, 44100, (AudioChannels)0, AudioDataEncoding.PCM_8Bits), "Did not throw argumentOutOfRangeException with invalid AudioChannels");
//            Assert.Throws<ArgumentOutOfRangeException>(() => new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, (AudioDataEncoding)0), "Did not throw argumentOutOfRangeException with invalid AudioDataEncoding");
//
//            ////////////////////////////////////////////////////
//            // 2. Test that a valid construction does not crash
//            DynamicSoundEffectInstance instance = null;
//            Assert.DoesNotThrow(() => instance = new DynamicSoundEffectInstance(defaultEngine, 34567, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits));
//            instance.Dispose();
//        }
//
//
//        [Fact]
//        public void TestPendingBufferCount()
//        {
//            mono8Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            var dispInstance = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Stereo, AudioDataEncoding.PCM_16Bits);
//            dispInstance.Dispose();
//
//            var pendingCount = 0;
//            ///////////////////////////////////////////////////////////////////////////
//            // 1. Test that it throws ObjectDisposedException with disposed instance
//            Assert.Throws<ObjectDisposedException>(() => pendingCount = dispInstance.PendingBufferCount, "PendingBufferCount did not throw ObjectDisposedException");
//
//            //////////////////////////////////
//            // 2. Test that it does not crash
//            Assert.DoesNotThrow(() => pendingCount = mono8Bits.PendingBufferCount, "PendingBufferCount crashed with valid instance");
//
//            ////////////////////////////
//            // 3. Test the default value
//            mono8Bits.Stop();
//            Assert.Equal(0, mono8Bits.PendingBufferCount, "PendingBufferCount default value is not 0");
//
//            //////////////////////////////////////////////////
//            // 4. Check the value after adding some buffers
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, 10000));
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, 10000));
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, 10000));
//            Assert.Equal(3, mono8Bits.PendingBufferCount, "PendingBufferCount value is not 3 after adding buffers");
//
//            //////////////////////////////////
//            // 5. Check the value after stop
//            mono8Bits.Stop();
//            Assert.Equal(0, mono8Bits.PendingBufferCount, "PendingBufferCount default value is not 0");
//
//            //////////////////////////////////
//            // 6 Check the value after play
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, 1000));
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, 1000));
//            mono8Bits.Play();
//            Utilities.Sleep(1000);
//            Assert.Equal(0, mono8Bits.PendingBufferCount, "PendingBufferCount value is not 0 after play");
//            mono8Bits.Stop();
//
//            mono8Bits.Dispose();
//        }
//
//        private bool bufferNeededHasBeenCalled;
//
//        private void SetBufferNeededHasBeenCalledToTrue(object sender, EventArgs e)
//        {
//            bufferNeededHasBeenCalled = true;
//            //Console.WriteLine("bufferNeededHasBeenCalled has been set to true.");
//        }
//
//        private static readonly object MultiUseLock = new object();
//
//        private void GenerateNextDataAndBlockThead(object sender, EventArgs e)
//        {
//            //Console.WriteLine("Start blocking thread");
//            lock (MultiUseLock)
//            {
//                if (!mono8Bits.IsDisposed)
//                    mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 42400f }, 1, 20000));
//            }
//            Utilities.Sleep(100);
//            //Console.WriteLine("End blocking thread");
//        }
//
//        [Fact]
//        public void TestBufferNeeded()
//        {
//            mono8Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//
//            mono8Bits.BufferNeeded += SetBufferNeededHasBeenCalledToTrue;
//
//            var sizeOfOneSubBuffer = 44100 * 200 / 1000;
//
//#if STRIDE_PLATFORM_ANDROID
//            sizeOfOneSubBuffer = mono8Bits.SubBufferSize;
//#endif
//
//            ////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that BufferNeeded is thrown when the user call plays with insufficient number of audio data
//            mono8Bits.Play();
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called when the user played without any buffers");
//            bufferNeededHasBeenCalled = false;
//            mono8Bits.Stop();
//            
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, 1000));
//            Utilities.Sleep(50);
//            bufferNeededHasBeenCalled = false;
//            mono8Bits.Play();
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called when the user played wit one buffers");
//            bufferNeededHasBeenCalled = false;
//            mono8Bits.Stop();
//            
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that BufferNeeded is thrown when the user call SubmitBuffer with insufficient number of audio data
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, sizeOfOneSubBuffer));
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called when the user submit the first buffer");
//            bufferNeededHasBeenCalled = false;
//
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, sizeOfOneSubBuffer));
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called when the user submit the second buffer");
//            bufferNeededHasBeenCalled = false;
//            mono8Bits.Stop();
//            
//            ////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that BufferNeeded is thrown when the number of buffers falls from 3 to 2, 2 to 1, 1 to 0
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, sizeOfOneSubBuffer));
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, sizeOfOneSubBuffer));
//            mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 0f }, 1, sizeOfOneSubBuffer));
//            Utilities.Sleep(50);
//            bufferNeededHasBeenCalled = false;
//            mono8Bits.Play();
//            var lastBufferCount = mono8Bits.PendingBufferCount;
//            var loopCount = 0;
//            while (true)
//            {
//                Utilities.Sleep(10);
//
//                if (lastBufferCount != mono8Bits.PendingBufferCount)
//                {
//                    lastBufferCount = mono8Bits.PendingBufferCount;
//                    Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called when number of buffer pass from "+(lastBufferCount+1)+" to "+lastBufferCount);
//                    bufferNeededHasBeenCalled = false;
//                }
//                if (lastBufferCount == 0)
//                    break;
//
//                ++loopCount;
//
//                if (loopCount>100)
//                    Assert.Fail("The test process is block in the loop.");
//            }
//            mono8Bits.Stop();
//
//            ///////////////////////////////////////////////////////////////////////////
//            // 4. Check that invocation of BufferNeeded does not block audio playback
//            mono8Bits.BufferNeeded -= SetBufferNeededHasBeenCalledToTrue;
//            mono8Bits.BufferNeeded += GenerateNextDataAndBlockThead;
//
//            mono8Bits.Play();
//            Utilities.Sleep(2000);
//            mono8Bits.Stop();
//
//            mono8Bits.BufferNeeded -= GenerateNextDataAndBlockThead;
//            mono8Bits.Dispose();
//        }
//
//        [Fact]
//        public void TestSubmitBuffer()
//        {
//            mono8Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            var mono16Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_16Bits);
//            var stereo8Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Stereo, AudioDataEncoding.PCM_8Bits);
//            var stereo16Bits = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Stereo, AudioDataEncoding.PCM_16Bits);
//            var dispInstance = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Stereo, AudioDataEncoding.PCM_16Bits);
//            dispInstance.Dispose();
//
//            ///////////////////////////////////////////////////////////////////////
//            // 1. Test that it throws ObjectDisposedException with disposed instance
//            Assert.Throws<ObjectDisposedException>(() => dispInstance.SubmitBuffer(new byte[16]), "SubmitBuffer did not throw ObjectDisposedException");
//
//            ///////////////////////////////////////////////////////////
//            // 2. Test that ArgumentNullException is correctly thrown
//            Assert.Throws<ArgumentNullException>(() => mono8Bits.SubmitBuffer(null), "SubmitBuffer did not throw ArgumentNullException");
//
//            /////////////////////////////////////////////////////////////////////////////
//            // 3. Test that ArgumentException is correctly thrown when buffer length is 0
//            Assert.Throws<ArgumentException>(() => mono8Bits.SubmitBuffer(new byte[0]), "SubmitBuffer did not throw ArgumentException with 0 length buffer");
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Test that ArgumentException is correctly thrown when buffer length do not respect alignment restrictions
//            Assert.Throws<ArgumentException>(() => mono16Bits.SubmitBuffer(new byte[3]), "SubmitBuffer did not throw ArgumentException with 3 length buffer");
//
//            ///////////////////////////////////////////////////////////////////////////
//            // 5. Test that ArgumentOutOfRangeException is thrown with negative offset
//            Assert.Throws<ArgumentOutOfRangeException>(() => mono8Bits.SubmitBuffer(new byte[16], -1, 3), "SubmitBuffer did not throw ArgumentOutOfRangeException with -1 offset");
//
//            //////////////////////////////////////////////////////////////////////////////////////////////
//            // 6. Test that ArgumentOutOfRangeException is thrown with offset greater than buffer length
//            Assert.Throws<ArgumentOutOfRangeException>(() => mono8Bits.SubmitBuffer(new byte[16], 16, 3), "SubmitBuffer did not throw ArgumentOutOfRangeException with 16 offset");
//
//            //////////////////////////////////////////////////////////////////////////////////
//            // 7. Test that ArgumentOutOfRangeException is thrown with byteCount is negative
//            Assert.Throws<ArgumentOutOfRangeException>(() => mono8Bits.SubmitBuffer(new byte[16], 16, -1), "SubmitBuffer did not throw ArgumentOutOfRangeException with -1 bytecount");
//
//            ////////////////////////////////////////////////////////////////////////////////////////////
//            // 8. Test that ArgumentOutOfRangeException is thrown with offset+byteCount is more buffer length
//            Assert.Throws<ArgumentOutOfRangeException>(() => mono8Bits.SubmitBuffer(new byte[16], 10, 7), "SubmitBuffer did not throw ArgumentOutOfRangeException with offset+bytecount greater than buffer length.");
//
//            /////////////////////////////////////////////////////////////////////////////////////////
//            // 9. Check that submitting mono-8bits signals does not crash and has the good behaviour
//            Assert.DoesNotThrow(()=>mono8Bits.SubmitBuffer(generator.Generate(44100, new[] { 40000f }, 1, 88200 )), "SubmitBuffer on mono8Bits crached.");
//            mono8Bits.Play();
//            Utilities.Sleep(2500);
//            
//            /////////////////////////////////////////////////////////////////////////////////////////
//            // 10. Check that submitting mono-16bits signals does not crash and has the good behaviour
//            Assert.DoesNotThrow(() => mono16Bits.SubmitBuffer(generator.Generate(44100, new[] { 40000f }, 2, 176400)), "SubmitBuffer on mono16Bits crached.");
//            mono16Bits.Play();
//            Utilities.Sleep(2500);
//            
//            ///////////////////////////////////////////////////////////////////////////////////////////
//            // 11. Check that submitting stereo-8bits signals does not crash and has the good behaviour
//            Assert.DoesNotThrow(() => stereo8Bits.SubmitBuffer(generator.Generate(44100, new[] { 40000f, 20000f }, 1, 176400)), "SubmitBuffer on stereo8Bits crached.");
//            stereo8Bits.Play();
//            Utilities.Sleep(2500);
//            
//            ///////////////////////////////////////////////////////////////////////////////////////////
//            // 12 Check that submitting stereo-16bits signals does not crash and has the good behaviour
//            Assert.DoesNotThrow(() => stereo16Bits.SubmitBuffer(generator.Generate(44100, new[] { 40000f, 10000f }, 2, 352800)), "SubmitBuffer on stereo16Bits crached.");
//            stereo16Bits.Play();
//            Utilities.Sleep(2500);
//
//            /////////////////////////////////////////////////////////////////////
//            // 13. Check that offset and byte count works in SubmitBuffer method
//            var buffer1 = generator.Generate(44100, new[] { 10000f }, 1, 44100);
//            var buffer2 = generator.Generate(44100, new[] { 40000f }, 1, 44100);
//            var buffer3 = generator.Generate(44100, new[] { 80000f }, 1, 44100);
//            var totalBuffer = new byte[132300];
//            Array.Copy(buffer1, totalBuffer, 44100);
//            Array.Copy(buffer2, 0, totalBuffer, 44100, 44100);
//            Array.Copy(buffer3, 0, totalBuffer, 88200, 44100);
//
//            Assert.DoesNotThrow(() => mono8Bits.SubmitBuffer(totalBuffer, 44100, 44100), "SubmitBuffer with offset and bytecount crached.");
//            mono8Bits.Play();
//            Utilities.Sleep(1500);
//
//            mono8Bits.Dispose();
//            mono16Bits.Dispose();
//            stereo8Bits.Dispose();
//            stereo16Bits.Dispose();
//        }
//
//
//        byte[] bufferData;
//        int bufferCount = 0;
//        const int BufferLenght = 8000;
//        private DynamicSoundEffectInstance dynSEInstance;
//
//        void SubmitBuffer(object o, EventArgs e)
//        {
//            var offset = bufferCount * BufferLenght;
//            if (offset >= bufferData.Length)
//                dynSEInstance.BufferNeeded -= SubmitBuffer;
//            else
//                dynSEInstance.SubmitBuffer(bufferData, offset, Math.Min(BufferLenght, bufferData.Length - offset));
//
//            ++bufferCount;
//        }
//
//        [Fact]
//        public void TestPlayableInterface()
//        {
//            WaveFormat dataFormat;
//            using (var stream = ContentManager.FileProvider.OpenStream("EffectFishLamp", VirtualFileMode.Open, VirtualFileAccess.Read))
//            {
//                var memoryStream = new MemoryStream((int)stream.Length);
//                stream.CopyTo(memoryStream);
//                memoryStream.Position = 0;
//
//                var waveStreamReader = new SoundStream(memoryStream);
//                dataFormat = waveStreamReader.Format;
//                bufferData = new byte[waveStreamReader.Length];
//
//                if (waveStreamReader.Read(bufferData, 0, (int)waveStreamReader.Length) != waveStreamReader.Length)
//                    throw new AudioSystemInternalException("The data length read in wave soundStream does not correspond to the stream's length.");
//            }
//            dynSEInstance = new DynamicSoundEffectInstance(defaultEngine, dataFormat.SampleRate, (AudioChannels)dataFormat.Channels, (AudioDataEncoding)dataFormat.BitsPerSample);
//            dynSEInstance.BufferNeeded += SubmitBuffer;
//
//            //////////////////
//            // 1. Test play
//            dynSEInstance.Play();
//            Utilities.Sleep(2000);
//            Assert.Equal(SoundPlayState.Playing, dynSEInstance.PlayState, "Music is not playing");
//
//            //////////////////
//            // 2. Test Pause
//            dynSEInstance.Pause();
//            Utilities.Sleep(600);
//            Assert.Equal(SoundPlayState.Paused, dynSEInstance.PlayState, "Music is not Paused");
//            dynSEInstance.Play();
//            Utilities.Sleep(1000);
//
//            //////////////////
//            // 2. Test Stop
//            dynSEInstance.Stop();
//            bufferCount = 0;
//            Utilities.Sleep(600);
//            Assert.Equal(SoundPlayState.Stopped, dynSEInstance.PlayState, "Music is not Stopped");
//            dynSEInstance.Play();
//            Utilities.Sleep(9000);
//
//            ///////////////////
//            // 3. Test ExitLoop
//            Assert.DoesNotThrow(dynSEInstance.ExitLoop, "ExitLoop crached");
//
//            ///////////////
//            // 4. Volume
//            var value = 1f;
//            var sign = -1f;
//            while (value <= 1f)
//            {
//                dynSEInstance.Volume = value;
//
//                value += sign * 0.01f;
//                Utilities.Sleep(30);
//
//                if (value < -0.2)
//                    sign = 1f;
//            }
//            Utilities.Sleep(2000);
//
//            //////////////////
//            // 5.Pan
//            value = 0;
//            sign = -1f;
//            while (value <= 1f)
//            {
//                dynSEInstance.Pan = value;
//
//                value += sign * 0.01f;
//                Utilities.Sleep(30);
//
//                if (value < -1.2)
//                    sign = 1f;
//            }
//            dynSEInstance.Pan = 0;
//            Utilities.Sleep(2000);
//            
//            ////////////////////////////////////////////////////////////////////////////
//            // 7. Wait until the end of the stream to check that there are not crashes
//            Utilities.Sleep(50000);
//
//            dynSEInstance.Dispose();
//        }
//
//        private class DynSoundData
//        {
//            public byte[] Data;
//
//            public int BufferCount;
//
//            public int BufferLength = 20000;
//
//            public DynamicSoundEffectInstance Instance;
//        };
//
//        private DynSoundData wave1;
//        private DynSoundData stereo;
//        private DynSoundData sayuriPart;
//        private DynamicSoundEffectInstance dynGenSound;
//
//        private DynamicSoundEffectInstance mono8Bits;
//
//        void SubmitWave1(object o, EventArgs e)
//        {
//            SubmitBuffer(wave1);
//        }
//        void SubmitStereo(object o, EventArgs e)
//        {
//            SubmitBuffer(stereo);
//        }
//        void SubmitSayuriPart(object o, EventArgs e)
//        {
//            SubmitBuffer(sayuriPart);
//        }
//        void SubmitDynGenSound(object o, EventArgs e)
//        {
//            dynGenSound.SubmitBuffer(generator.Generate(44100, new[] { 10000f }, 1, 4000));
//        }
//
//        static void SubmitBuffer(DynSoundData sound)
//        {
//            if (sound.Instance.IsDisposed)
//                return;
//
//            var offset = sound.BufferCount * sound.BufferLength;
//            if (offset < sound.Data.Length)
//            {
//                sound.Instance.SubmitBuffer(sound.Data, offset, Math.Min(sound.BufferLength, sound.Data.Length - offset));
//            }
//            else
//            {
//                sound.BufferCount = 0;
//                sound.Instance.SubmitBuffer(sound.Data, 0, Math.Min(sound.BufferLength, sound.Data.Length));
//            }
//            ++sound.BufferCount;
//        }
//
//        private void LoadWaveFileIntoBuffers(out GCHandle handle, out DynSoundData sound, string filename)
//        {
//            sound = new DynSoundData();
//
//            WaveFormat dataFormat;
//            using (var stream = ContentManager.FileProvider.OpenStream(filename, VirtualFileMode.Open, VirtualFileAccess.Read))
//            using (var memoryStream = new MemoryStream(new byte[stream.Length]))
//            {
//                stream.CopyTo(memoryStream);
//                memoryStream.Position = 0;
//                var waveStreamReader = new SoundStream(memoryStream);
//                dataFormat = waveStreamReader.Format;
//                sound.Data = new byte[waveStreamReader.Length];
//                handle = GCHandle.Alloc(sound.Data, GCHandleType.Pinned);
//
//                if (waveStreamReader.Read(sound.Data, 0, (int)waveStreamReader.Length) != waveStreamReader.Length)
//                    throw new AudioSystemInternalException("The data length read in wave soundStream does not correspond to the stream's length.");
//            }
//            sound.Instance = new DynamicSoundEffectInstance(defaultEngine, dataFormat.SampleRate, (AudioChannels)dataFormat.Channels, (AudioDataEncoding)dataFormat.BitsPerSample);
//        }
//
//        [Fact]
//        public void TestImplementationSpecific()
//        {
//            bufferNeededHasBeenCalled = false;
//
//            ////////////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that worker process buffer needed requests with the first instance of dynamic sound.
//            var instance1 = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            instance1.BufferNeeded += SetBufferNeededHasBeenCalledToTrue; 
//            instance1.Play();
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called with a first single instance.");
//            bufferNeededHasBeenCalled = false;
//            instance1.Stop();
//
//            ////////////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that worker process buffer needed requests with the second instance of dynamic sound.
//            var instance2 = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            instance2.BufferNeeded += SetBufferNeededHasBeenCalledToTrue; 
//            instance2.Play();
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called with a second instance.");
//            bufferNeededHasBeenCalled = false;
//            instance2.Stop();
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that worker process buffer needed requests of the second instance when the first is disposed.
//            instance1.Dispose();
//            instance2.Play();
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called with a second single instance.");
//            bufferNeededHasBeenCalled = false;
//            instance2.Stop();
//
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Check that the worker is correctly recreated when the number of dynamic instances have reached 0
//            instance2.Dispose();
//            instance1 = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            instance1.BufferNeeded += SetBufferNeededHasBeenCalledToTrue; 
//            instance1.Play();
//            Utilities.Sleep(50);
//            Assert.True(bufferNeededHasBeenCalled, "Buffer Needed has not been called with a single instance after destruct of all instances.");
//            bufferNeededHasBeenCalled = false;
//            instance1.Stop();
//
//            ///////////////////////////////////////////////////////////////////////////////////////////////////
//            // 5. Play several dynamic at the same time to check that there is not problem with a single worker
//
//            dynGenSound = new DynamicSoundEffectInstance(defaultEngine, 44100, AudioChannels.Mono, AudioDataEncoding.PCM_8Bits);
//            dynGenSound.BufferNeeded += SubmitDynGenSound;
//
//            GCHandle pinnedDataWave1;
//            GCHandle pinnedDataStereo;
//            GCHandle pinnedDataSayuriPart;
//
//            LoadWaveFileIntoBuffers(out pinnedDataWave1, out wave1, "EffectBip");
//            LoadWaveFileIntoBuffers(out pinnedDataStereo, out stereo, "EffectStereo");
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "EffectFishLamp");
//
//            wave1.Instance.BufferNeeded += SubmitWave1;
//            stereo.Instance.BufferNeeded += SubmitStereo;
//            sayuriPart.Instance.BufferNeeded += SubmitSayuriPart;
//
//            // plays all the instances together to see
//            wave1.Instance.Play();
//            stereo.Instance.Play();
//            sayuriPart.Instance.Play();
//            dynGenSound.Play();
//
//            Utilities.Sleep(5000);
//
//            wave1.Instance.Stop();
//            stereo.Instance.Stop();
//            sayuriPart.Instance.Stop();
//            dynGenSound.Stop();
//
//            Utilities.Sleep(100); // avoid crash due to ObjectDisposedException
//
//            dynGenSound.Dispose();
//            wave1.Instance.Dispose();
//            stereo.Instance.Dispose();
//            sayuriPart.Instance.Dispose();
//
//            pinnedDataWave1.Free();
//            pinnedDataStereo.Free();
//            pinnedDataSayuriPart.Free();
//        }
//
//        [Fact]
//        public void TestImplementationSpecificAndroid()
//        {
//            int subBufferSize = 1000;
//
//            GCHandle pinnedDataSayuriPart;
//
//#if STRIDE_PLATFORM_ANDROID
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "sayuriPart.wav");
//            subBufferSize = sayuriPart.Instance.SubBufferSize;
//            sayuriPart.Instance.Dispose();
//            pinnedDataSayuriPart.Free();
//#endif
//
//            //////////////////////////////////////////////////////////////////////////////////////////
//            // 1. Check that the there is no problems submitting buffers smallers than impl subbuffers
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "EffectFishLamp");
//            sayuriPart.BufferLength = (subBufferSize / 4) + ((subBufferSize / 4) % 2);
//            sayuriPart.Instance.BufferNeeded += SubmitSayuriPart;
//            sayuriPart.Instance.Play();
//            Utilities.Sleep(4000);
//            sayuriPart.Instance.Dispose();
//            pinnedDataSayuriPart.Free();
//            Utilities.Sleep(1000);
//
//            //////////////////////////////////////////////////////////////////////////////////////////////
//            // 2. Check that the there is no problems submitting buffers with same size as impl subbuffers
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "EffectFishLamp");
//            sayuriPart.BufferLength = subBufferSize;
//            sayuriPart.Instance.BufferNeeded += SubmitSayuriPart;
//            sayuriPart.Instance.Play();
//            Utilities.Sleep(4000);
//            sayuriPart.Instance.Dispose();
//            pinnedDataSayuriPart.Free();
//            Utilities.Sleep(1000);
//
//            //////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that the there is no problems submitting buffers with bigger size than impl subbuffers
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "EffectFishLamp");
//            sayuriPart.BufferLength = 2 * subBufferSize;
//            sayuriPart.Instance.BufferNeeded += SubmitSayuriPart;
//            sayuriPart.Instance.Play();
//            Utilities.Sleep(4000);
//            sayuriPart.Instance.Dispose();
//            pinnedDataSayuriPart.Free();
//            Utilities.Sleep(1000);
//
//            ///////////////////////////////////////////////////////////////////////////////////////////////////////
//            // 3. Check that the there is no problems submitting buffers with bigger size than impl  while buffer
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "EffectFishLamp");
//            sayuriPart.BufferLength = 5 * subBufferSize;
//            sayuriPart.Instance.BufferNeeded += SubmitSayuriPart;
//            sayuriPart.Instance.Play();
//            Utilities.Sleep(4000);
//            sayuriPart.Instance.Dispose();
//            pinnedDataSayuriPart.Free();
//            Utilities.Sleep(1000);
//
//            /////////////////////////////////////////////////////////////////////////////////////////////////
//            // 4. Test that buffer smaller than subbuffer size are played and that nbofbuffers is zero after
//            LoadWaveFileIntoBuffers(out pinnedDataSayuriPart, out sayuriPart, "EffectFishLamp");
//            sayuriPart.BufferLength = (3 * subBufferSize / 4)+ ((3 * subBufferSize / 4) % 2);
//            SubmitSayuriPart(null, null);
//            sayuriPart.Instance.Play();
//            Utilities.Sleep(1000);  // should here sound here
//            Assert.Equal(0, sayuriPart.Instance.PendingBufferCount, "The number of pending buffer is not zero after that all audio data have been consumed.");
//
//            ///////////////////////////////////////////////////////
//            // 5. Restart correctly after having consumed all data
//            sayuriPart.BufferLength = 2 * subBufferSize;
//            sayuriPart.Instance.BufferNeeded += SubmitSayuriPart;
//            SubmitSayuriPart(null, null);
//            Utilities.Sleep(3000);
//            sayuriPart.Instance.Dispose();
//            pinnedDataSayuriPart.Free();
//            Utilities.Sleep(1000);
//        }
//    }
//}
