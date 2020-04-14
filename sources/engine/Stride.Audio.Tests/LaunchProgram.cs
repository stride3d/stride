// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Audio.Tests
{
    public class LaunchProgram
    {
        public static void Main()
        {
            TestAll();
        }

        public static void TestAll()
        {
//            TestAudioEngine();
//            TestWaveStreamReader();
//            TestSoundEffect();
            TestAudioListener();
            TestAudioEmitter();
//            TestSoundEffectInstance();
//            TestSoundMusic();
//            TestDynamicSoundEffectInstance();
//            TestInvalidationAudioContext();
        }

//        public static void TestAudioEngine()
//        {
//            var test = new TestAudioEngine();
//            test.Initialize();
//            test.ContextInitialization();
//            test.TestGetLeastSignificativeSoundEffect();
//            test.TestState();
//            test.TestPauseAudio();
//            test.TestResumeAudio();
//            test.TestDispose();
//        }

//        public static void TestWaveStreamReader()
//        {
//            var test = new TestWaveStreamReader();
//            test.Initialize();
//            test.WaveHeaderTest();
//            test.WaveDataTest();
//        }

//        public static void TestSoundEffect()
//        {
//            var test = new TestSoundEffect();
//            test.Initialize();
//            test.TestLoad();
//            test.TestCreateInstance();
//            test.TestDispose();
//            test.TestDefaultInstance();
//            test.Uninitialize();
//        }

        public static void TestAudioListener()
        {
            var test = new TestAudioListener();
            test.TestPosition();
            test.TestVelocity();
            test.TestForward();
            test.TestUp();
        }

        public static void TestAudioEmitter()
        {
            var test = new TestAudioEmitter();
            test.TestPosition();
            test.TestVelocity();
//            test.TestDistanceScale();
//            test.TestDopplerScale();
        }
//        public static void TestSoundEffectInstance()
//        {
//            var test = new TestSoundEffectInstance();
//            test.Initialize();
//            test.TestMultipleCreations();
//            test.TestInstanceConcurrency();
//            test.TestDispose();
//            test.TestPlay();
//            test.TestPause();
//            test.TestStop();
//            test.TestExitLoop();
//            test.TestVolume();
//            test.TestIsLooped();
//            test.TestPlayState();
//            test.TestPan();
//            test.TestApply3D();
//            test.Uninitialize();
//        }

//        public static void TestSoundMusic()
//        {
//            var test = new TestSoundMusic();
//            test.Initialize();
//            test.TestDispose();
//            test.TestLoad();
//            test.TestLoadAndPlayValid();
//            test.TestPlay();
//            test.TestPause();
//            test.TestStop();
//            test.TestPlayState();
//            test.TestIsLooped();
//            test.TestExitLoop();
//            test.TestVolume();
//            test.TestVarious();
//            test.TestImplSpecific();
//
//            // does not work on all platforms yet.
//            //test.TestLoadAndPlayInvalid1();
//            //test.TestLoadAndPlayInvalid2();
//            //test.TestLoadAndPlayInvalid3();
//            //test.TestLoadAndPlayInvalid4();
//
//            test.Uninitialize();
//        }

//        public static void TestDynamicSoundEffectInstance()
//        {
//            var test = new TestDynamicSoundEffectInstance();
//            test.Initialize();
//            test.TestIsLooped();
//            test.TestConstructor();
//            test.TestPendingBufferCount();
//            test.TestBufferNeeded();
//            test.TestSubmitBuffer();
//            test.TestPlayableInterface();
//            test.TestImplementationSpecific();
//            test.TestImplementationSpecificAndroid();
//            test.Uninitialize();
//        }

//        public static void TestInvalidationAudioContext()
//        {
//            var test = new TestInvalidationAudioContext();
//            test.Initialize();
//            test.TestInvalidationDuringSoundEffectPlay();
//            test.TestInvalidationDuringSoundMusicPlay();
//            test.Unititiliaze();
//        }
    }
}
