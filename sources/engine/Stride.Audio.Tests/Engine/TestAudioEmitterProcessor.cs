// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Audio.Tests.Engine
{
    /// <summary>
    /// Test the <see cref="AudioEmitterComponent"/>. All the test are performed on internal members. 
    /// If the implementation of the <see cref="AudioEmitterComponent"/> is modified, those tests may not be valid anymore.
    /// </summary>
    public class TestAudioEmitterProcessor
    {
        private List<AudioListenerComponent> listComps;
        private List<AudioEmitterComponent> emitComps;
        private List<Entity> compEntities;

        private Entity rootEntity;
        private Entity rootSubEntity1;
        private Entity rootSubEntity2;

        private List<Sound> sounds;
        private List<AudioEmitterSoundController> soundControllers;

        // build a simple entity hierarchy as follow that may be used for tests.
        //       o root
        //      / \
        //     o   o subEntities
        //     |   | 
        //     o   o listCompEntities
        private void BuildEntityHierarchy()
        {
            rootEntity = new Entity { Name = "Root entity" };
            rootSubEntity1 = new Entity { Name = "Root sub entity 1" };
            rootSubEntity2 = new Entity { Name = "Root sub entity 2" };
            compEntities = new List<Entity> { new Entity { Name = "Comp entity 1" }, new Entity { Name = "Comp entity 2" } };

            rootSubEntity1.Transform.Parent = rootEntity.Transform;
            rootSubEntity2.Transform.Parent = rootEntity.Transform;
            compEntities[0].Transform.Parent = rootSubEntity1.Transform;
            compEntities[1].Transform.Parent = rootSubEntity2.Transform;
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
            throw new NotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); // game.Entities.Add(rootEntity);
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
        /// Create some <see cref="AudioListenerComponent"/> and add them to 'compEntities'.
        /// </summary>
        private void CreateAndAddListenerComponentToEntities()
        {
            listComps = new List<AudioListenerComponent> { new AudioListenerComponent(), new AudioListenerComponent() };

            for (int i = 0; i < listComps.Count; i++)
                compEntities[i].Add(listComps[i]);
        }

        /// <summary>
        /// Create some <see cref="AudioEmitterComponent"/> and add them to 'compEntities'.
        /// </summary>
        private void CreateAndAddEmitterComponentToEntities()
        {
            emitComps = new List<AudioEmitterComponent> { new AudioEmitterComponent(), new AudioEmitterComponent() };

            for (int i = 0; i < compEntities.Count; i++)
                compEntities[i].Add(emitComps[i]);
        }

        //        /// <summary>
        //        /// Check that each <see cref="AudioEmitterSoundController"/> of the scene have one <see cref="SoundEffectInstance"/> 
        //        /// associated to each <see cref="AudioListenerComponent"/> added to the <see cref="AudioSystem"/>.
        //        /// </summary>
        //        /// <param name="matchingEntities">matching entities from the <see cref="AudioEmitterProcessor"/></param>
        //        private void CheckSoundEffectExistance(Dictionary<Entity, AudioEmitterProcessor.AssociatedData> matchingEntities)
        //        {
        //            CheckSoundEffectExistance(new HashSet<AudioListenerComponent>(listComps), matchingEntities);
        //        }
        //
        //        /// <summary>
        //        /// Check that each <see cref="AudioEmitterSoundController"/> of the scene have one <see cref="SoundEffectInstance"/> 
        //        /// associated to each <see cref="AudioListenerComponent"/> of the <paramref name="shouldExistListeners"/> list.
        //        /// </summary>
        //        /// <param name="shouldExistListeners">the listener component that should exist</param>
        //        /// <param name="matchingEntities">matching entities from the <see cref="AudioEmitterProcessor"/></param>
        //        private void CheckSoundEffectExistance(HashSet<AudioListenerComponent> shouldExistListeners, Dictionary<Entity, AudioEmitterProcessor.AssociatedData> matchingEntities)
        //        {
        //            for (var i = 0; i < compEntities.Count; i++)
        //            {
        //                var data = matchingEntities[compEntities[i]];
        //                foreach (var listComp in listComps)
        //                {
        //                    foreach (var controller in emitComps[i].SoundToController.Values)
        //                    {
        //                        var currentKey = Tuple.Create(listComp, controller);
        //                        if (shouldExistListeners.Contains(listComp))
        //                            Assert.True(data.ListenerControllerToSoundInstance.ContainsKey(currentKey), "Sound Effect instance should exist for registered listener");
        //                        else
        //                            Assert.False(data.ListenerControllerToSoundInstance.ContainsKey(currentKey), "Sound Effect instance should not exist for not registered listener");
        //                    }
        //                }
        //            }
        //        }

        /// <summary>
        /// Add and remove <see cref="AudioListenerComponent"/> to the <see cref="AudioSystem"/> and check that 
        /// <see cref="SoundEffectInstance"/> dedicated to each <see cref="AudioListenerComponent"/> are correctly created.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddRemoveListeners()
        {
            TestUtilities.CreateAndRunGame(TestAddRemoveListenersHelper, TestUtilities.ExitGame);
        }

        private void TestAddRemoveListenersHelper(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddRootEntityToEntitySystem(game);

            throw new NotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer");
            //var matchingEntities = game.Entities.Processors.OfType<AudioEmitterProcessor>().First().MatchingEntitiesForDebug;

            //// check that there are initially not SoundEffectInstance created.
            //CheckSoundEffectExistance(new HashSet<AudioListenerComponent> (), matchingEntities);

            //// add one listener
            //game.Audio.AddListener(listComps[0]);

            //// check that there is now one SoundEffectInstance for each SoundController
            //CheckSoundEffectExistance(new HashSet<AudioListenerComponent> { listComps[0] }, matchingEntities);

            //// add another listener
            //game.Audio.AddListener(listComps[1]);

            //// check that there is now two SoundEffectInstance for each SoundController
            //CheckSoundEffectExistance(new HashSet<AudioListenerComponent> (listComps), matchingEntities);

            //// remove one listener
            //game.Audio.RemoveListener(listComps[1]);

            //// check that there is now only one SoundEffectInstance for each SoundController left
            //CheckSoundEffectExistance(new HashSet<AudioListenerComponent> { listComps[0] }, matchingEntities);

            //// remove the other listener
            //game.Audio.RemoveListener(listComps[0]);

            //// check that there is no SoundEffectInstance left
            //CheckSoundEffectExistance(new HashSet<AudioListenerComponent>(), matchingEntities);
        }


        /// <summary>
        /// Add entities with <see cref="AudioEmitterComponent"/> associated and check that for each <see cref="AudioListenerComponent"/> existing in the scene
        /// a <see cref="SoundEffectInstance"/> have been created. Then remove the entities and check that they are removed.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddRemoveEntityWithEmitter()
        {
            TestUtilities.CreateAndRunGame(TestAddRemoveEntityWithEmitterHelper, TestUtilities.ExitGame);
        }

        private void TestAddRemoveEntityWithEmitterHelper(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
            AddRootEntityToEntitySystem(game);

            emitComps.Add(new AudioEmitterComponent());
            emitComps[2].Sounds["EffectToneA"] = game.Content.Load<Sound>("EffectToneA");
            var extraEntity = new Entity();
            extraEntity.Add(emitComps[2]);

            throw new NotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer");
            //var matchingEntities = game.Entities.Processors.OfType<AudioEmitterProcessor>().First().MatchingEntitiesForDebug;

            //// check that initially there is no problems.
            //CheckSoundEffectExistance(matchingEntities);

            //// and an entity and check that the soundEffectInstances have been created.
            //compEntities.Add(extraEntity);
            //game.Entities.Add(extraEntity);
            //CheckSoundEffectExistance(matchingEntities);

            //// remove the entity and check that it is removed from the matchingComp list.
            //compEntities.Remove(extraEntity);
            //game.Entities.Remove(extraEntity);
            //CheckSoundEffectExistance(matchingEntities);
        }

        /// <summary>
        /// Attach and detach <see cref="SoundEffect"/> to <see cref="AudioEmitterComponent"/> and check that the 
        /// <see cref="AudioEmitterSoundController"/> and <see cref="SoundEffectInstance"/> instances are created correctly.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddRemoveSoundEffect()
        {
            TestUtilities.CreateAndRunGame(TestAddRemoveSoundEffectHelper, TestUtilities.ExitGame);
        }


        private void TestAddRemoveSoundEffectHelper(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddRootEntityToEntitySystem(game);
            AddListenersToAudioSystem(game);

            throw new NotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer");
            //var matchingEntities = game.Entities.Processors.OfType<AudioEmitterProcessor>().First().MatchingEntitiesForDebug;

            //CheckSoundEffectExistance(matchingEntities);

            //var sound1 = game.Content.Load<SoundEffect>("EffectToneA");
            //var sound2 = game.Content.Load<SoundEffect>("EffectFishLamp");
            //var sound3 = game.Content.Load<SoundEffect>("EffectBip");

            //// attach new Soundeffects and check the SoundEffectInstance creation.
            //emitComps[0].AttachSoundEffect(sound1);
            //CheckSoundEffectExistance(matchingEntities);

            //emitComps[1].AttachSoundEffect(sound2);
            //CheckSoundEffectExistance(matchingEntities);

            //emitComps[0].AttachSoundEffect(sound3);
            //CheckSoundEffectExistance(matchingEntities);

            //// detach SoundEffect and check that the controllers have been deleted.
            //emitComps[0].DetachSoundEffect(sound1);
            //CheckSoundEffectExistance(matchingEntities);

            //emitComps[1].DetachSoundEffect(sound2);
            //CheckSoundEffectExistance(matchingEntities);

            //emitComps[0].DetachSoundEffect(sound3);
            //CheckSoundEffectExistance(matchingEntities);
        }

        /// <summary>
        /// Check that the <see cref="AudioEmitter"/> associated to the <see cref="AudioEmitterComponent"/> are correctly updated
        /// when at least one of the <see cref="AudioEmitterSoundController"/> of the <see cref="AudioEmitterComponent"/> is playing.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestEmitterUpdateValues()
        {
            TestUtilities.ExecuteScriptInDrawLoop(TestEmitterUpdateValuesSetup, EntityPositionAndEmitterbfrUpdate, TestEmitterUpdateValuesAtfUpdate);
        }

        /// <summary>
        /// Setup configuration for the test.
        /// </summary>
        /// <param name="game"></param>
        private void TestEmitterUpdateValuesSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndAddListenerComponentToEntities();
            CreateAndAddEmitterComponentToEntities();
            AddRootEntityToEntitySystem(game);
            AddSoundEffectToEmitterComponents(game);
            AddListenersToAudioSystem(game);
        }

        /// <summary>
        /// Update the entities position and <see cref="AudioEmitterComponent"/> parameters at each loop turn.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="loopCount"></param>
        /// <param name="loopCountSum"></param>
        private void EntityPositionAndEmitterbfrUpdate(Game game, int loopCount, int loopCountSum)
        {
            rootSubEntity1.Transform.Position += new Vector3(loopCount, 2 * loopCount, 3 * loopCount);
            rootSubEntity2.Transform.Position += 2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount);

            compEntities[0].Transform.Position += new Vector3(loopCount + 1, 2 * loopCount + 1, 3 * loopCount + 1);
            compEntities[1].Transform.Position -= new Vector3(loopCount, 2 * loopCount, 3 * loopCount);

            //            emitComps[0].DistanceScale = loopCount;
            //            emitComps[0].DopplerScale = 2 * loopCount;
            //            emitComps[1].DistanceScale = 3 * loopCount;
            //            emitComps[1].DopplerScale = 4 * loopCount;
        }

        //        /// <summary>
        //        /// Check that the values of the <see cref="AudioEmitter"/> associated to the <see cref="AudioEmitterComponent"/> are or are not updated as they should be.
        //        /// </summary>
        //        /// <param name="emitter1ShouldBeValid">boolean indicating if emitter component 1 is supposed to be updated.</param>
        //        /// <param name="emitter2ShouldBeValid">boolean indicating if emitter component 2 is supposed to be updated.</param>
        //        /// <param name="matchingEntities">the matching entities of the <see cref="AudioEmitterProcessor"/></param>
        //        /// <param name="loopCount">the current loopCount of the game</param>
        //        private void CheckEmittersValues(bool emitter1ShouldBeValid, bool emitter2ShouldBeValid, Dictionary<Entity, AudioEmitterProcessor.AssociatedData> matchingEntities, int loopCount)
        //        {
        //            var dataComp1 = matchingEntities[compEntities[0]];
        //            var dataComp2 = matchingEntities[compEntities[1]];
        //
        //            // check that the boolean value of the AudioEmitterComponent indicating the processor if the AudioEmitter should be updated is valid.
        //            Assert.True(dataComp1.AudioEmitterComponent.ShouldBeProcessed == emitter1ShouldBeValid, "value of ShouldBeProcessed for emitter 1 is not correct at loop turn" + loopCount);
        //            Assert.True(dataComp2.AudioEmitterComponent.ShouldBeProcessed == emitter2ShouldBeValid, "value of ShouldBeProcessed for emitter 2 is not correct at loop turn" + loopCount);
        //
        //            var emitter1Velocity = 2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount) + Vector3.One;
        //            if (emitter1ShouldBeValid)
        //            {
        //                // check the AudioEmitter 1 values are updated.
        //                Assert.Equal(emitter1Velocity, dataComp1.AudioEmitter.Velocity, "The velocity of emitter 1 is not valid at loop turn" + loopCount);
        //                Assert.Equal(loopCount, dataComp1.AudioEmitter.DistanceScale, "The distance scale of emitter 1 is not valid at loop turn" + loopCount);
        //                Assert.Equal(2 * loopCount, dataComp1.AudioEmitter.DopplerScale, "The Doppler scale of emitter 1 is not valid at loop turn" + loopCount);
        //            }
        //            else
        //            {
        //                // check the AudioEmitter 1 values are not updated anymore
        //                Assert.NotEqual(emitter1Velocity, dataComp1.AudioEmitter.Velocity, "The velocity of emitter 1 is calculated for nothing at loop turn" + loopCount);
        //                Assert.NotEqual(loopCount, dataComp1.AudioEmitter.DistanceScale, "The distance scale of emitter 1 is calculated for nothing at loop turn" + loopCount);
        //                Assert.NotEqual(2 * loopCount, dataComp1.AudioEmitter.DopplerScale, "The Doppler scale of emitter 1 is calculated for nothing loop turn" + loopCount);
        //            }
        //
        //            var emitter2Velocity = new Vector3(loopCount, 2 * loopCount, 3 * loopCount);
        //            if (emitter2ShouldBeValid)
        //            {
        //                // check the AudioEmitter 2 values are updated.
        //                Assert.Equal(emitter2Velocity, dataComp2.AudioEmitter.Velocity, "The velocity of emitter 2 is not valid at loop turn" + loopCount);
        //                Assert.Equal(3 * loopCount, dataComp2.AudioEmitter.DistanceScale, "The distance scale of emitter 2 is not valid at loop turn" + loopCount);
        //                Assert.Equal(4 * loopCount, dataComp2.AudioEmitter.DopplerScale, "The Doppler scale of emitter 2 is not valid at loop turn" + loopCount);
        //            }
        //            else
        //            {
        //                // check the AudioEmitter 2 values are not updated anymore
        //                Assert.NotEqual(emitter2Velocity, dataComp2.AudioEmitter.Velocity, "The velocity of emitter 2 is calculated for nothing at loop turn" + loopCount);
        //                Assert.NotEqual(3 * loopCount, dataComp2.AudioEmitter.DistanceScale, "The distance scale of emitter 2 is calculated for nothing at loop turn" + loopCount);
        //                Assert.NotEqual(4 * loopCount, dataComp2.AudioEmitter.DopplerScale, "The Doppler scale of emitter 2 is calculated for nothing loop turn" + loopCount);
        //            }
        //        }

        //        private bool soundController0WentToStopState;
        //        private bool soundController2WentToStopState;

        private void TestEmitterUpdateValuesAtfUpdate(Game game, int loopCount, int loopCountSum)
        {
            //throw new NotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //var matchingEntities = game.Entities.Processors.OfType<AudioEmitterProcessor>().First().MatchingEntitiesForDebug;

            //var dataComp1 = matchingEntities[compEntities[0]];
            //var dataComp2 = matchingEntities[compEntities[1]];

            //// check that AudioEmitters position is always valid. (this is required to ensure that the velocity is valid from the first update).
            //Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum) + (loopCount + 1) * Vector3.One, dataComp1.AudioEmitter.Position, "Position of the emitter 1 is not correct");
            //Assert.Equal(new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), dataComp2.AudioEmitter.Position, "Position of the emitter 2 is not correct");

            //if (loopCount == 0)
            //{
            //    soundController0WentToStopState = false;
            //    soundController2WentToStopState = false;

            //    // check that initially AudioEmitter should not be updated.
            //    foreach (var data in matchingEntities.Values)
            //        Assert.False(data.AudioEmitterComponent.ShouldBeProcessed, "Initial value of ShouldBeProcessed is not correct at loop turn"+loopCount);

            //    soundControllers[0].Play();
            //}
            //else if (loopCount == 1)
            //{
            //    // check that emitter 1 is updated but not emitter 2
            //    CheckEmittersValues(true, false, matchingEntities, loopCount);

            //    soundControllers[2].Play();
            //}
            //else if (soundControllers[0].PlayState == SoundPlayState.Playing)
            //{
            //    // check that both emitters are updated.
            //    CheckEmittersValues(true, true, matchingEntities, loopCount);
            //}
            //else if (!soundController0WentToStopState)
            //{
            //    // transition state of controller 1 from play to stop 
            //    // since PlayState is updated asynchronously via callbacks, 
            //    // PlayState may have changed between the Update and this function calls
            //    // that is why we wait for next loop turn to perform the new test.
            //    soundController0WentToStopState = true;
            //}
            //else if (soundControllers[2].PlayState == SoundPlayState.Playing)
            //{
            //    // check that emitter 2 is still updated but not emitter1 anymore.
            //    CheckEmittersValues(false, true, matchingEntities, loopCount);
            //}
            //else if (!soundController2WentToStopState)
            //{
            //    // transition state of controller 2 from play to stop 
            //    // since PlayState is updated asynchronously via callbacks, 
            //    // PlayState may have changed between the Update and this function calls
            //    // that is why we wait for next loop turn to perform the new test.
            //    soundController2WentToStopState = true;
            //}
            //else
            //{
            //    // check that both emitter are not updated anymore.
            //    CheckEmittersValues(false, false, matchingEntities, loopCount);
            //    game.Exit();
            //}
        }

        /// <summary>
        /// Tests the behavior of processor when several listener are present in the scene.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestMultiListener()
        {
            TestUtilities.ExecuteScriptInDrawLoop(TestEmitterUpdateValuesSetup, TestMulteListenerUpdate);
        }

        private void TestMulteListenerUpdate(Game game, int loopCount, int loopCountSum)
        {
            throw new NotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer");
            //var matchingEntities = game.Entities.Processors.OfType<AudioEmitterProcessor>().First().MatchingEntitiesForDebug;

            //var dataComp1 = matchingEntities[compEntities[0]];

            //if (loopCount == 0)
            //{
            //    soundControllers[0].Play();
            //}
            //else if (loopCount < 10)
            //{
            //    // check that the two instances are correctly create and playing
            //    var tupple1 = Tuple.Create(listComps[0], soundControllers[0]);
            //    var tupple2 = Tuple.Create(listComps[1], soundControllers[0]);

            //    Assert.True(dataComp1.ListenerControllerToSoundInstance.ContainsKey(tupple1));
            //    Assert.True(dataComp1.ListenerControllerToSoundInstance.ContainsKey(tupple2));

            //    var instance1 = dataComp1.ListenerControllerToSoundInstance[tupple1];
            //    var instance2 = dataComp1.ListenerControllerToSoundInstance[tupple2];

            //    Assert.Equal(SoundPlayState.Playing, instance1.PlayState);
            //    Assert.Equal(SoundPlayState.Playing, instance2.PlayState);
            //}
            //else
            //{
            //    game.Exit();
            //}
        }
    }
}
