// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Audio.Tests.Engine
{
    /// <summary>
    /// Check the audio system in general.
    /// Some of them tests internal data.
    /// But most of them tests the good behavior of the audio system from the user point of view. 
    /// Those tests requires the tester to listen to the speakers output.
    /// </summary>
    public class TestAudioSystem
    {
        /// <summary>
        /// Test that the audio system retrieved from the game is correct and in a good state.
        /// </summary>
        [Fact]
        public void TestAudioEngine()
        {
            TestUtilities.CreateAndRunGame(TestInitializeAudioEngine, TestUtilities.ExitGame);
        }

        private void TestInitializeAudioEngine(Game game)
        {
            var audio = game.Audio;
            var audioEngine = audio.AudioEngine;
            Assert.NotNull(audioEngine);
            Assert.False(audioEngine.IsDisposed, "The audio engine is disposed");
        }

        /// <summary>
        /// Check that <see cref="AudioListenerComponent"/> are correctly added and removed to the <see cref="AudioSystem"/> internal structures
        /// using functions <see cref="AudioSystem.AddListener"/> and <see cref="AudioSystem.RemoveListener"/>.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddRemoveListener()
        {
            TestUtilities.CreateAndRunGame(TestAddRemoveListenerImpl, TestUtilities.ExitGame);
        }

        private void TestAddRemoveListenerImpl(Game game)
        {
            var audio = game.Audio;
            var notAddedToEntityListener = new AudioListenerComponent();
            var addedToEntityListener = new AudioListenerComponent();

            // Add a listenerComponent not present in the entity system yet and check that it is correctly added to the AudioSystem internal data structures
            audio.AddListener(notAddedToEntityListener);
            Assert.True(audio.Listeners.ContainsKey(notAddedToEntityListener), "The list of listeners of AudioSystem does not contains the notAddedToEntityListener.");

            // Add a listenerComponent already present in the entity system and check that it is correctly added to the AudioSystem internal data structures
            var entity = new Entity("Test");
            entity.Add(addedToEntityListener);
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(entity);
            audio.AddListener(addedToEntityListener);
            Assert.True(audio.Listeners.ContainsKey(addedToEntityListener), "The list of listeners of AudioSystem does not contains the addedToEntityListener.");

            // Add a listenerComponent already added to audio System and check that it does not crash
            audio.AddListener(addedToEntityListener);

            // Remove the listeners from the AudioSystem and check that they are removed from internal data structures.
            audio.RemoveListener(notAddedToEntityListener);
            Assert.False(audio.Listeners.ContainsKey(notAddedToEntityListener), "The list of listeners of AudioSystem still contains the notAddedToEntityListener.");
            audio.RemoveListener(addedToEntityListener);
            Assert.False(audio.Listeners.ContainsKey(addedToEntityListener), "The list of listeners of AudioSystem still contains the addedToEntityListener.");

            // Remove a listener not present in the AudioSystem anymore and check the thrown exception
            Assert.Throws<ArgumentException>(() => audio.RemoveListener(addedToEntityListener));
        }

        private List<AudioListenerComponent> listComps;
        private List<AudioEmitterComponent> emitComps;
        private List<Entity> listCompEntities;
        private List<Entity> emitCompEntities;

        private Entity rootEntity;
        private Entity rootSubEntity1;
        private Entity rootSubEntity2;

        private List<Sound> sounds;
        private List<AudioEmitterSoundController> soundControllers;

        // build a simple entity hierarchy as follow
        //       o root
        //      / \
        //     o   o subEntities
        //     |   | 
        //     o   o listCompEntities
        //     |   | 
        //     o   o emitCompEntities
        private void BuildEntityHierarchy()
        {
            rootEntity = new Entity();
            rootSubEntity1 = new Entity();
            rootSubEntity2 = new Entity();
            listCompEntities = new List<Entity> { new Entity(), new Entity() };
            emitCompEntities = new List<Entity> { new Entity(), new Entity() };

            rootSubEntity1.Transform.Parent = rootEntity.Transform;
            rootSubEntity2.Transform.Parent = rootEntity.Transform;
            listCompEntities[0].Transform.Parent = rootSubEntity1.Transform;
            listCompEntities[1].Transform.Parent = rootSubEntity2.Transform;
            emitCompEntities[0].Transform.Parent = listCompEntities[0].Transform;
            emitCompEntities[1].Transform.Parent = listCompEntities[1].Transform;
        }

        /// <summary>
        /// Create some <see cref="AudioListenerComponent"/> and add them to 'compEntities'.
        /// </summary>
        private void CreateAndAddListenerComponentToEntities()
        {
            listComps = new List<AudioListenerComponent> { new AudioListenerComponent(), new AudioListenerComponent() };

            for (int i = 0; i < listComps.Count; i++)
                listCompEntities[i].Add(listComps[i]);
        }

        /// <summary>
        /// Create some <see cref="AudioEmitterComponent"/> and add them to 'compEntities'.
        /// </summary>
        private void CreateAndAddEmitterComponentToEntities()
        {
            emitComps = new List<AudioEmitterComponent> { new AudioEmitterComponent(), new AudioEmitterComponent() };

            for (int i = 0; i < emitComps.Count; i++)
                emitCompEntities[i].Add(emitComps[i]);
        }

        /// <summary>
        /// Load some default random <see cref="SoundEffect"/>, attach them to the emitter components, and query their controllers.
        /// </summary>
        /// <param name="game"></param>
        private void AddSoundEffectToEmitterComponents(Game game)
        {
            sounds = new List<Sound>
                {
                    game.Content.Load<Sound>("EffectBip"),
                    game.Content.Load<Sound>("EffectToneA"),
                    game.Content.Load<Sound>("EffectToneA"),
                };

            emitComps[0].Sounds["EffectBip"] = sounds[0];
            emitComps[0].Sounds["EffectToneA"] = sounds[1];
            emitComps[1].Sounds["EffectToneA"] = sounds[2];

            soundControllers = new List<AudioEmitterSoundController>
                {
                    emitComps[0]["EffectBip"],
                    emitComps[0]["EffectToneA"],
                    emitComps[1]["EffectToneA"],
                };
        }

        /// <summary>
        /// Add the root entity to the entity system
        /// </summary>
        /// <param name="game"></param>
        private void AddRootEntityToEntitySystem(Game game)
        {
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(rootEntity);
        }

        /// <summary>
        /// Add all the <see cref="AudioListenerComponent"/> to the <see cref="AudioSystem"/>.
        /// </summary>
        /// <param name="game"></param>
        private void AddListenersToAudioSystem(Game game)
        {
            foreach (var t in listComps)
                game.Audio.AddListener(t);
        }

        /// <summary>
        /// Check the behavior of the system when adding new listeners.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddListener()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestAddListenerSetup, null, TestAddListenerLoopImpl);
        }

        private void TestAddListenerSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);
        }

        private void TestAddListenerLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check that a controller play call output nothing if there is no listener currently listening.
                soundControllers[0].Play();
            }
            // nothing should come out from the speakers during this gap
            else if (loopCount == 60)
            {
                // check that the sound is correctly outputted when there is one listener
                game.Audio.AddListener(listComps[0]);
                soundControllers[0].Play();
            }
            // here we should hear soundEffect 0
            else if (loopCount == 120)
            {
                // isolate the two listeners such that emitter 1 can be heard only by listener 1 and emitter 2 by listener 2
                // and place emitters such that emitters 1 output on left ear and emitter 2 on right ear.
                listCompEntities[0].Transform.Position = listCompEntities[0].Transform.Position - new Vector3(1000, 0, 0);
                emitCompEntities[0].Transform.Position = emitCompEntities[0].Transform.Position - new Vector3(1, 0, 0);
                emitCompEntities[1].Transform.Position = emitCompEntities[1].Transform.Position + new Vector3(1, 0, 0);

                // add a new listener not present into the entity system yet to the audio system
                listComps.Add(new AudioListenerComponent());
                listCompEntities.Add(new Entity());
                listCompEntities[2].Add(listComps[2]);
                game.Audio.AddListener(listComps[2]);

                // check that the sound is not heard by the new listener (because not added to the entity system).
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 on left ear only
            else if (loopCount == 180)
            {
                // add the new listener to the entity system
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Add(listCompEntities[2]);

                // check the sounds are heard by the two listeners.
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 on left ear and the soundEffect 2 on right ear
            else if (loopCount > 240)
            {
                game.Exit();
            }
        }

        /// <summary>
        /// Check the behavior of the system when removing old listeners.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestRemoveListener()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestRemoveListenerSetup, null, TestRemoveListenerLoopImpl);
        }

        private void TestRemoveListenerSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            // add an extra listener
            listComps.Add(new AudioListenerComponent());
            listCompEntities.Add(new Entity());
            listCompEntities[2].Add(listComps[2]);
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(listCompEntities[2]);

            // isolate the two listeners such that emitter 1 can be heard only by listener 1 and emitter 2 by listener 3
            // and place emitters such that emitters 1 output on left ear and emitter 2 on right ear.
            listCompEntities[0].Transform.Position = listCompEntities[0].Transform.Position - new Vector3(1000,0,0);
            emitCompEntities[0].Transform.Position = emitCompEntities[0].Transform.Position - new Vector3(1, 0, 0);
            emitCompEntities[1].Transform.Position = emitCompEntities[1].Transform.Position + new Vector3(1, 0, 0);

            game.Audio.AddListener(listComps[2]);
            game.Audio.AddListener(listComps[0]);

            soundControllers[0].IsLooping = true;
            soundControllers[2].IsLooping = true;
        }

        private void TestRemoveListenerLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check that initially the sound are hear via the two listeners.
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 on left ear and the soundEffect 2 on right ear
            else if (loopCount == 60)
            {
                // remove listener 3 from the entity system => check that the sounds are now heard only via listener 1
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Remove(listCompEntities[2]);
            }
            // here we should hear the soundEffect 0 on left ear only
            else if (loopCount == 120)
            {
                // remove listener 1 from the audio system  => check that the sounds are not outputted anymore.
                game.Audio.RemoveListener(listComps[0]);
            }
            // here we should hear nothing.
            else if (loopCount == 180)
            {
                game.Exit();
            }
        }

        /// <summary>
        /// Check the behavior of the system when removing old emitters.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddRemoveEmitter()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestAddRemoveEmitterSetup, null, TestAddRemoveEmitterLoopImpl);
        }

        private void TestAddRemoveEmitterSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            AddRootEntityToEntitySystem(game);
            AddListenersToAudioSystem(game);

            // isolate the two listeners such that emitter 1 can be heard only by listener 1 and emitter 2 by listener 3
            // and place emitters such that emitters 1 output on left ear and emitter 2 on right ear.
            listCompEntities[0].Transform.Position = listCompEntities[0].Transform.Position - new Vector3(1000, 0, 0);
            emitCompEntities[0].Transform.Position = emitCompEntities[0].Transform.Position - new Vector3(1, 0, 0);
            emitCompEntities[1].Transform.Position = emitCompEntities[1].Transform.Position + new Vector3(1, 0, 0);

            emitComps = new List<AudioEmitterComponent> { new AudioEmitterComponent(), new AudioEmitterComponent() };
            emitCompEntities = new List<Entity> { new Entity(), new Entity()};
            emitCompEntities[0].Add(emitComps[0]);
            emitCompEntities[1].Add(emitComps[1]);

            AddSoundEffectToEmitterComponents(game);
        }

        private void TestAddRemoveEmitterLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // check there is no sound output if the emitterComponents are not added to the entity system.
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear nothing.
            else if (loopCount == 60)
            {
                // add emitter 1 to the entity system
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Add(emitCompEntities[0]);

                // check that emitter 1 can now be heard.
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 on left ear only
            else if (loopCount == 120)
            {
                // add now emitter 2 to the entity system
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Add(emitCompEntities[1]);

                // test that now both emitters can be heard.
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 on left ear and the soundEffect 2 on right ear
            else if (loopCount == 180)
            {
                // remove emitter 2 from the entity system  
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Remove(emitCompEntities[1]);

                // test that now only emitter 1 can be heard.
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 on left ear only
            else if (loopCount == 240)
            {
                // remove emitter 1 from the entity system  
                Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
                //game.Entities.Remove(emitCompEntities[0]);

                // check that there is not more audio output at all
                soundControllers[0].Play();
                soundControllers[2].Play();
            }
            // here we should hear nothing.
            else if (loopCount == 320)
            {
                game.Exit();
            }
        }


        /// <summary>
        /// Check the behavior of the system when combining the use of <see cref="SoundMusic"/> with the use of <see cref="AudioEmitterComponent"/>.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestEffectsAndMusic()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestEffectsAndMusicSetup, null, TestEffectsAndMusicLoopImpl);
        }

        private void TestEffectsAndMusicSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            game.Audio.AddListener(listComps[0]);
        }

        private void TestEffectsAndMusicLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            if (loopCount == 0)
            {
                // start the sound music in background.
                var music = game.Content.Load<Sound>("MusicFishLampMp3").CreateInstance(game.Audio.AudioEngine.DefaultListener);
                music.Play();
            }
            // here we should hear the mp3.
            else if (loopCount == 240)
            {
                // check that AudioEmitterComponent can be played along with soundMusic.
                soundControllers[0].Play();
            }
            // here we should hear the soundEffect 0 and the mp3.
            else if (loopCount == 260)
            {
                // check that two AudioEmitterComponent can be played along with a soundMusic.
                soundControllers[2].Play();
            }
            // here we should hear the soundEffect 0 and 2 and the mp3.
            else if (loopCount == 500)
            {
                game.Exit();
            }
        }

        /// <summary>
        /// Check the behavior of the system when using several <see cref="AudioEmitterSoundController"/> for a single <see cref="AudioEmitterComponent"/>.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestSeveralControllers()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestSeveralControllersSetup, null, TestSeveralControllersLoopImpl);
        }

        private void TestSeveralControllersSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            game.Audio.AddListener(listComps[0]);

            sounds.Add(game.Content.Load<Sound>("EffectToneE"));
            emitComps[0].Sounds["EffectToneE"] = sounds[3];
            soundControllers.Add(emitComps[0]["EffectToneE"]);
        }

        private void TestSeveralControllersLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            // have the emitter turn around the listener to check that all sounds are coming from the same emitter.
            emitCompEntities[0].Transform.Position = new Vector3((float)Math.Cos(loopCount * Math.PI / 30), 0, (float)Math.Sin(loopCount * Math.PI / 30));

            if (loopCount == 0)
            {
                // start a first controller
                soundControllers[0].Play();
            }
            // here we should hear SoundEffect 0
            else if (loopCount == 30)
            {
                // start a second controller while controller 1 has not finished to play
                soundControllers[1].Play();
            }
            // here we should hear SoundEffect 0 and 1
            else if (loopCount == 60)
            {
                // start a third controller while controller 2 has not finished to play
                soundControllers[3].Play();
            }
            // here we should hear the soundEffect 1 and 2
            else if (loopCount == 120)
            {
                game.Exit();
            }
        }

        /// <summary>
        /// Simple test to check that the audio system is applying the Doppler effect as expected.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestDopplerCoherency()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestDopplerCoherencySetup, null, TestDopplerCoherencyLoopImpl);
        }

        private void TestDopplerCoherencySetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            game.Audio.AddListener(listComps[0]);

            //emitComps[0].DistanceScale = 200f;  // increase distance scale so that the sound can be heard from far away too
            soundControllers[0].IsLooping = true;
            soundControllers[0].Play();
        }

        private void TestDopplerCoherencyLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            // useless motion on the root entities just to check that is does not disturb to calculations.
            rootSubEntity1.Transform.Position += new Vector3(1, 2, 3);
            listCompEntities[0].Transform.Position += new Vector3(3, 2, -1);
            // apply a left to right motion to the emitter entity
            emitCompEntities[0].Transform.Position = new Vector3(20*(loopCount-200), 0, 2);

            // the sound should be modified in pitch depending on which side the emitter is
            // ( first pitch should be increased and then decreased)
            if (loopCount == 400)
            {
                game.Exit();
            }
        }

        /// <summary>
        /// Simple test to check that the audio system is attenuating sound signals as expected.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAttenuationCoherency()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestAttenuationCoherencySetup, null, TestAttenuationCoherencyLoopImpl);
        }

        private void TestAttenuationCoherencySetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            game.Audio.AddListener(listComps[0]);

            soundControllers[0].IsLooping = true;
            soundControllers[0].Play();
        }

        private void TestAttenuationCoherencyLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            // put away progressively the emitter.
            emitCompEntities[0].Transform.Position = new Vector3(0, 0, loopCount / 10f);

            // the sound should progressively attenuate
            if (loopCount == 800)
            {
                game.Exit();
            }
        }

        /// <summary>
        /// Simple test to check that the audio system is localizing the sounds properly.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestLocalizationCoherency()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestLocalizationCoherencySetup, null, TestLocalizationCoherencyLoopImpl);
        }

        private void TestLocalizationCoherencySetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            game.Audio.AddListener(listComps[0]);

            soundControllers[0].IsLooping = true;
            soundControllers[0].Play();
        }

        private void TestLocalizationCoherencyLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            // useless motion on the root entities just to check that is does not disturb to calculations.
            rootSubEntity1.Transform.Position += new Vector3(1, 2, 3);
            listCompEntities[0].Transform.Position += new Vector3(3, 2, -1);
            // have the emitter turn clockwise around the listener.
            emitCompEntities[0].Transform.Position = new Vector3((float)Math.Cos(loopCount * Math.PI / 100), 0, (float)Math.Sin(loopCount * Math.PI / 100));

            // the sound should turn around clockwise 
            if (loopCount == 800)
            {
                game.Exit();
            }
        }
    }
}
