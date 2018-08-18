// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xunit;

using Xenko.Engine;
using Xenko.Graphics.Regression;
using Xenko.Media;

namespace Xenko.Audio.Tests.Engine
{
    /// <summary>
    /// Test the <see cref="AudioEmitterSoundController"/> class namely its implementation of the IPlayableSound interface.
    /// </summary>
    public class TestController
    {
        private List<AudioListenerComponent> listComps;
        private List<AudioEmitterComponent> emitComps;
        private List<Entity> listCompEntities;
        private List<Entity> emitCompEntities;

        private Entity rootEntity;

        private List<Sound> sounds;
        private List<AudioEmitterSoundController> soundControllers;
        private AudioEmitterSoundController mainController;

        // build a simple entity hierarchy as follow
        //       o root
        //       |    
        //       o  listCompEntity
        //       |   
        //       o  emitCompEntity
        private void BuildEntityHierarchy()
        {
            rootEntity = new Entity();
            listCompEntities = new List<Entity> { new Entity(), new Entity() };
            emitCompEntities = new List<Entity> { new Entity(), new Entity() };

            listCompEntities[0].Transform.Parent = rootEntity.Transform;
            emitCompEntities[0].Transform.Parent = listCompEntities[0].Transform;
        }

        private void CreateAndAddListenerComponentToEntities()
        {
            listComps = new List<AudioListenerComponent> { new AudioListenerComponent(), new AudioListenerComponent() };

            for (int i = 0; i < listComps.Count; i++)
                listCompEntities[i].Add(listComps[i]);
        }

        private void CreateAndAddEmitterComponentToEntities()
        {
            emitComps = new List<AudioEmitterComponent> { new AudioEmitterComponent(), new AudioEmitterComponent() };

            for (int i = 0; i < emitComps.Count; i++)
                emitCompEntities[i].Add(emitComps[i]);
        }

        private void AddSoundEffectToEmitterComponents(Game game)
        {
            sounds = new List<Sound>
                {
                    game.Content.Load<Sound>("EffectBip"),
                    game.Content.Load<Sound>("EffectToneA"),
                };

            emitComps[0].Sounds["EffectBip"] = sounds[0];
            emitComps[0].Sounds["EffectToneA"] = sounds[1];

            soundControllers = new List<AudioEmitterSoundController>
                {
                    emitComps[0]["EffectBip"],
                    emitComps[0]["EffectToneA"],
                };

            mainController = soundControllers[0];
        }

        private void AddRootEntityToEntitySystem(Game game)
        {
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(rootEntity);
        }

        private void AddListenersToAudioSystem(Game game)
        {
            foreach (var t in listComps)
                game.Audio.AddListener(t);
        }

        /// <summary>
        /// Test the default values and states of the <see cref="AudioEmitterSoundController"/> class.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestDefaultValues()
        {
            TestUtilities.CreateAndRunGame(TestDefaultValuesImpl, TestUtilities.ExitGame);
        }

        private void TestDefaultValuesImpl(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
            
            // test before the creation of any soundEffectInstances
            Assert.False(mainController.IsLooping, "Controller is looped status was true by default.");
            Assert.True(PlayState.Stopped == mainController.PlayState, "Controller play state was not set to stopped by default.");
            Assert.True(1 == mainController.Volume, "The volume of the controller was not 1 by default.");

            AddRootEntityToEntitySystem(game);

            // test after the creation of any soundEffectInstances
            Assert.False(mainController.IsLooping, "Controller is looped status was true by default.");
            Assert.True(PlayState.Stopped == mainController.PlayState, "Controller play state was not set to stopped by default.");
            Assert.True(1 == mainController.Volume, "The volume of the controller was not 1 by default.");
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestVolume()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestVolumeSetup, null, TestVolumeLoopImpl);
        }

        private void TestVolumeSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
            AddRootEntityToEntitySystem(game);
        }

        private void TestVolumeLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check that setting the volume before play works.
                mainController.Volume = 0.1f;
                mainController.Play();
            }
            // should hear low volume
            else if (loopCount == 60)
            {
               mainController.Volume = 1f;
               mainController.Play();
            }
            // should hear normal volume
            else if (loopCount == 120)
            {
                // check that setting the volume without listener works too
                game.Audio.RemoveListener(listComps[0]);
                game.Audio.RemoveListener(listComps[1]);
                mainController.Volume = 0.1f;
                game.Audio.AddListener(listComps[0]);
                game.Audio.AddListener(listComps[1]);
                mainController.Play();
            }
            // should hear low volume
            else if (loopCount == 180)
            {
                mainController.Volume = 1f;
                mainController.Play();
            }
            // should hear normal volume
            else if (loopCount == 240)
            {
                mainController.Volume = 0f;
                mainController.IsLooping = true;
                mainController.Play();
            }
            else if (loopCount > 240)
            {
                // check that sound goes smoothly from nothing to full intensity
                mainController.Volume = Math.Abs((loopCount - 440) / 200f);
            }
            // the sound should go up and down
            if (loopCount == 640)
            {
                game.Exit();
            }
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestIsLooped()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestIsLoopedSetup, null, TestIsLoopedLoopImpl);
        }

        private void TestIsLoopedSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
            AddRootEntityToEntitySystem(game);
        }

        private void TestIsLoopedLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check that the sound loops as it should.
                mainController.IsLooping = true;
                mainController.Play();
            }
            // should hear looped sound
            else if (loopCount == 100)
            {
                Assert.True(PlayState.Playing == mainController.PlayState, "The sound play status was stopped but the sound is supposed to be looped.");
                mainController.Stop();
            }
            // should hear nothing
            else if (loopCount == 150)
            {
                // check that the sound does not loop  as it should.
                mainController.IsLooping = false;
                mainController.Play();
            }
            // should hear not a looped sound
            else if (loopCount == 250)
            {
                Assert.True(PlayState.Stopped == mainController.PlayState, "The sound play status was playing but the sound is supposed to do so.");
                mainController.Stop();
            }
            // should hear not a looped sound
            else if (loopCount == 300)
            {
                // check that setting the isLooped without listener works too
                game.Audio.RemoveListener(listComps[0]);
                game.Audio.RemoveListener(listComps[1]);
                mainController.IsLooping = true;
                game.Audio.AddListener(listComps[0]);
                game.Audio.AddListener(listComps[1]);
                mainController.Play();
            }
            // should hear looped sound
            else if (loopCount == 400)
            {
                Assert.True(PlayState.Playing == mainController.PlayState, "The sound play status was stopped but the sound is supposed to be looped.");
                mainController.Stop();
            }
            // should hear looped sound
            else if (loopCount == 450)
            {
                mainController.Play();
            }
            else if (loopCount == 475)
            {
                // check that IsLooped throws InvalidOperationException when modified while playing.
                Assert.Throws<InvalidOperationException>(() => mainController.IsLooping = true);
                game.Exit();
            }
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestPlay()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestPlaySetup, null, TestPlayLoopImpl);
        }

        private void TestPlaySetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
        }

        private void TestPlayLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // test that the sound does not play when playing a sound associated to an entity which has not been added to the system yet.
                mainController.Play();
            }
            // should hear nothing
            else if (loopCount == 100)
            {
                Assert.True(PlayState.Stopped == mainController.PlayState, "The sound play status was playing eventhough the entity was not added.");

                // check the behavious of a basic call to play.
                AddRootEntityToEntitySystem(game);
                mainController.Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 120)
            {
                Assert.True(PlayState.Playing == mainController.PlayState, "The sound play status was not playing eventhough the entity was added.");
            }
            // should hear the end of the sound
            else if (loopCount == 160)
            {
                // add a longuer sound
                sounds.Add(game.Content.Load<Sound>("EffectFishLamp"));
                emitComps[0].Sounds["EffectFishLamp"] = sounds[2];
                soundControllers.Add(emitComps[0]["EffectFishLamp"]);
                soundControllers[2].Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 300)
            {
                // check that the sound is stopped when removing the listeners.
                game.Audio.RemoveListener(listComps[0]);
                game.Audio.RemoveListener(listComps[1]);
                Assert.True(PlayState.Stopped == soundControllers[2].PlayState, "The sound has not been stopped when the listeners have been removed.");
            }
            // should hear nothing
            else if (loopCount == 360)
            {
                game.Audio.AddListener(listComps[0]);
                game.Audio.AddListener(listComps[1]);
                soundControllers[2].Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 500)
            {
                // check that the sound is stopped when removing the sound Entity from the system.
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Remove(rootEntity);
                Assert.True(PlayState.Stopped == soundControllers[2].PlayState, "The sound has not been stopped when the emitter's entities have been removed.");
            }
            // should hear nothing
            else if (loopCount == 560)
            {
                game.Exit();
            }
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestPause()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestPauseSetup, null, TestPauseLoopImpl);
        }

        private void TestPauseSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
            AddRootEntityToEntitySystem(game);

            // add a longuer sound
            sounds.Add(game.Content.Load<Sound>("EffectFishLamp"));
            emitComps[0].Sounds["EffectFishLamp"] = sounds[2];
            soundControllers.Add(emitComps[0]["EffectFishLamp"]);
        }

        private void TestPauseLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check that a call to pause works even though the call to play has not been comitted yet (next AudioSystem.Update).
                soundControllers[2].Play();
                soundControllers[2].Pause();
            }
            // should hear nothing
            else if (loopCount == 60)
            {
                Assert.True(PlayState.Paused == soundControllers[2].PlayState, "The sound play status was not paused just after play.");
                soundControllers[2].Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 150)
            {
                // test that a basic call the pause works correctly.
                soundControllers[2].Pause();
                Assert.True(PlayState.Paused == soundControllers[2].PlayState, "The sound play status was not paused.");
            }
            // should hear nothing
            else if (loopCount == 200)
            {
                soundControllers[2].Play();
            }
            // should hear the end of the sound
            else if (loopCount == 400)
            {
                game.Exit();
            }
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestStop()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestStopSetup, null, TestStopLoopImpl);
        }

        private void TestStopSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
            AddRootEntityToEntitySystem(game);

            // add a longuer sound
            sounds.Add(game.Content.Load<Sound>("EffectFishLamp"));
            emitComps[0].Sounds["EffectFishLamp"] = sounds[2];
            soundControllers.Add(emitComps[0]["EffectFishLamp"]);
        }

        private void TestStopLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check that a call to stop works even though the call to play has not been comitted yet (next AudioSystem.Update).
                soundControllers[2].Play();
                soundControllers[2].Stop();
            }
            // should hear nothing
            else if (loopCount == 60)
            {
                Assert.True(PlayState.Stopped == soundControllers[2].PlayState, "The sound play status was not stopped just after play.");
                soundControllers[2].Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 150)
            {
                // test the basic call to stop.
                soundControllers[2].Stop();
                Assert.True(PlayState.Stopped == soundControllers[2].PlayState, "The sound play status was not stopped.");
            }
            // should hear nothing
            else if (loopCount == 200)
            {
                soundControllers[2].Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 400)
            {
                game.Exit();
            }
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestPlayState()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestPlayStateSetup, null, TestPlayStateLoopImpl);
        }

        private void TestPlayStateSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            // add an extra listener.
            var extraList = new AudioListenerComponent();
            var extraListEntity = new Entity();
            extraListEntity.Add(extraList);
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(extraListEntity);

            // check that PlayState always returns 'PlayState.Stopped' when there are no listeners
            mainController.Play();
            Assert.True(PlayState.Stopped == mainController.PlayState, "Value of playState without listeners is not valid after call to play.");
            mainController.Pause();
            Assert.True(PlayState.Stopped == mainController.PlayState, "Value of playState without listeners is not valid after call to Pause.");
            mainController.Stop();
            Assert.True(PlayState.Stopped == mainController.PlayState, "Value of playState without listeners is not valid after call to Stop.");
            
            // check values with listeners

            AddListenersToAudioSystem(game);

            mainController.Play(); 
            Assert.True(PlayState.Playing == mainController.PlayState, "Value of playState with listeners is not valid after call to play.");
            mainController.Pause();
            Assert.True(PlayState.Paused == mainController.PlayState, "Value of playState with listeners is not valid after call to Pause.");
            mainController.Stop();
            Assert.True(PlayState.Stopped == mainController.PlayState, "Value of playState with listeners is not valid after call to Stop.");
            mainController.Play();
            Assert.True(PlayState.Playing == mainController.PlayState, "Value of playState with listeners is not valid after a second call to play.");
            mainController.Pause();
            Assert.True(PlayState.Paused == mainController.PlayState, "Value of playState with listeners is not valid after a second call to Pause.");
            mainController.Play();
            Assert.True(PlayState.Playing == mainController.PlayState, "Value of playState with listeners is not valid after a third call to play.");
        }

        private void TestPlayStateLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 60)
            {
                // check that PlayState is automatically reset to 'Stopped' when all the subInstances have finished to play.
                Assert.True(PlayState.Stopped == mainController.PlayState, "Value of playState with listeners is not valid after the end of the track.");
                game.Exit();
            }
        }

        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestExitLoop()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestExitLoopSetup, null, TestExitLoopLoopImpl);
        }

        private void TestExitLoopSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);
            AddListenersToAudioSystem(game);

            mainController.IsLooping = true;
        }

        private void TestExitLoopLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                mainController.Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 10)
            {
                // test a basic utilisation of exitLoop.
                //mainController.ExitLoop();
            }
            // should hear the end of the sound
            else if (loopCount == 60)
            {
                Assert.True(mainController.IsLooping, "The value of isLooped has been modified by ExitLoop.");
                Assert.True(PlayState.Stopped == mainController.PlayState, "The sound did not stopped after exitloop.");
            }
            // should hear nothing
            else if (loopCount == 80)
            {
                // check that performing an exitLoop before the play commit does not fail (bfr next AudioSystem.Uptade).
                mainController.Play();
                //mainController.ExitLoop();
            }
            // should hear a sound not looped
            else if (loopCount == 140)
            {
                Assert.True(mainController.IsLooping, "The value of isLooped has been modified by ExitLoop just after play.");
                Assert.True(PlayState.Stopped == mainController.PlayState, "The sound did not stopped after Exitloop just after play.");
            }
            // should hear nothing
            else if (loopCount == 160)
            {
                mainController.Play();
            }
            // should hear the beginning of the sound
            else if (loopCount == 170)
            {
                // check that performing an ExitLoop while the sound is paused does not fails.
                mainController.Pause();
                //mainController.ExitLoop();
            }
            // should hear nothing
            else if (loopCount == 220)
            {
                mainController.Play();
            }
            // should hear the end of the sound (not looped)
            else if (loopCount == 270)
            {
                Assert.True(mainController.IsLooping, "The value of isLooped has been modified by ExitLoop after pause.");
                Assert.True(PlayState.Stopped == mainController.PlayState, "The sound did not stopped after exitloop after pause.");
            }
            // should hear nothing
            else if (loopCount == 320)
            {
                // check that performing an exitLoop while the sound is stopped is ignored.
                //mainController.ExitLoop();
                mainController.Play();
            }
            // should hear the sound looping
            else if (loopCount == 500)
            {
                Assert.True(mainController.IsLooping, "The value of isLooped has been modified by ExitLoop before play.");
                Assert.True(PlayState.Playing == mainController.PlayState, "The sound did not looped.");
                game.Exit();
            }
        }
    }
}
