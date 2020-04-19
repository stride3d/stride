// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Audio.Tests.Engine
{
    public class LaunchProgram
    {
        public static void Main()
        {
            {
                var test = new TestController();
                test.TestExitLoop();
                test.TestPlayState();
                test.TestStop();
                test.TestPause();
                test.TestDefaultValues();
                test.TestVolume();
                test.TestIsLooped();
                test.TestPlay();
            }
            {
                var test = new TestAudioSystem();
                test.TestDopplerCoherency();
                test.TestAttenuationCoherency();
                test.TestLocalizationCoherency();
                test.TestSeveralControllers();
                test.TestEffectsAndMusic();
                test.TestAddRemoveEmitter();
                test.TestRemoveListener();
                test.TestAddListener();
                test.TestAudioEngine();
                test.TestAddRemoveListener();
            }
            {
                var test = new TestAudioEmitterProcessor();
                test.TestEmitterUpdateValues();
                test.TestAddRemoveListeners();
                test.TestAddRemoveEntityWithEmitter();
                test.TestAddRemoveSoundEffect();
            }

//            {
//                var test = new TestAudioEmitterComponent();
//                test.TestInitialization();
//                test.TestAttachDetachSounds();
//                test.TestGetController();
//            }

            {
                var test = new TestAudioListenerProcessor();
                test.TestAddAudioSysThenEntitySys();
                test.TestAddEntitySysThenAudioSys();
                test.TestRemoveListenerFromAudioSystem();
                test.TestRemoveListenerFromEntitySystem();
            }
            {
                var test = new TestGame();
                test.TestAccessToAudio();
                test.TestCreationDestructionOfTheGame();
            }
            {
                var test = new TestAssetLoading();
                test.TestSoundEffectLoading();
                test.TestSoundMusicLoading();
            }
        }
    }
}
