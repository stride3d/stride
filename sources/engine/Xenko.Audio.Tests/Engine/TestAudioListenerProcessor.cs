// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using Xunit;

using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Audio.Tests.Engine
{
    /// <summary>
    /// Test the <see cref="AudioListenerProcessor"/>. All the test are performed on internal members. 
    /// If the implementation of the <see cref="AudioListenerProcessor"/> is modified, those tests may not be valid anymore.
    /// </summary>
    public class TestAudioListenerProcessor
    {
        private AudioListenerComponent listComp1;
        private AudioListenerComponent listComp2;

        private Entity rootEntity;
        private Entity rootSubEntity1;
        private Entity rootSubEntity2;
        private Entity listComp1Entity;
        private Entity listComp2Entity;

        // build a simple entity hierarchy as follow
        //       o root
        //      / \
        //     o   o subEntities
        //     |   | 
        //     o   o listCompEntities
        private void BuildEntityHierarchy()
        {
            rootEntity = new Entity();
            rootSubEntity1 = new Entity();
            rootSubEntity2 = new Entity();
            listComp1Entity = new Entity();
            listComp2Entity = new Entity();

            rootSubEntity1.Transform.Parent = rootEntity.Transform;
            rootSubEntity2.Transform.Parent = rootEntity.Transform;
            listComp1Entity.Transform.Parent = rootSubEntity1.Transform;
            listComp2Entity.Transform.Parent = rootSubEntity2.Transform;
        }

        private void CreateAndComponentToEntities()
        {
            listComp1 = new AudioListenerComponent();
            listComp2 = new AudioListenerComponent();

            listComp1Entity.Add(listComp1);
            listComp2Entity.Add(listComp2);
        }

        /// <summary>
        /// Check component data is correctly updated when the component is first added to the audio System and then to the Entity system.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddAudioSysThenEntitySys()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestAddAudioSysThenEntitySysSetup, EntityPositionUpdate, TestAddAudioSysThenEntitySysLoopImpl);
        }

        private void TestAddAudioSysThenEntitySysSetup(Game game)
        {
            var audio = game.Audio;

            BuildEntityHierarchy();
            CreateAndComponentToEntities();

            audio.AddListener(listComp1);
            audio.AddListener(listComp2);

            listComp2Entity.Transform.RotationEulerXYZ = new Vector3((float)Math.PI/2,0,0);
        }

        private void EntityPositionUpdate(Game game, int loopCount, int loopCountSum)
        {
            rootSubEntity1.Transform.Position += new Vector3(loopCount, 2 * loopCount, 3 * loopCount);
            rootSubEntity2.Transform.Position += new Vector3(loopCount, 2 * loopCount, 3 * loopCount);

            listComp1Entity.Transform.Position += new Vector3(loopCount, 2 * loopCount, 3 * loopCount);
            listComp2Entity.Transform.Position -= new Vector3(loopCount, 2 * loopCount, 3 * loopCount);
        }

        private void TestAddAudioSysThenEntitySysLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //var listenerProcessor = game.Entities.Processors.OfType<AudioListenerProcessor>().First();

            //if (loopCount == 1)
            //{
            //    // add the listeners entities to the entity system.
            //    game.Entities.Add(rootEntity);
            //    AudioListenerProcessor.AssociatedData list1Data = null;
            //    AudioListenerProcessor.AssociatedData list2Data = null;
                
            //    // check that the entities are immediately present in the matching entity list after addition to the Entity system.
            //    Assert.DoesNotThrow(() => list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity], "Listener Component 1 entity is not present in listener processor matching entities");
            //    Assert.DoesNotThrow(() => list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity], "Listener Component 2 entity is not present in listener processor matching entities");
                
            //    // check that the entities' position are immediately computed after addition to the Entity system.
            //    Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), list1Data.AudioListener.Position, "The Position of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(Vector3.Zero, list2Data.AudioListener.Position, "The Position of the listener2 is not valid at loop turn " + loopCount);
                
            //    // check that the listener components are marked for update immediately after addition to the Entity system.
            //    Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //}
            //else if (loopCount > 2 && loopCount < 5)
            //{
            //    var list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity];
            //    var list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity];

            //    // check the values of the Positions after update
            //    Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), list1Data.AudioListener.Position, "The Position of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(Vector3.Zero, list2Data.AudioListener.Position, "The Position of the listener2 is not valid at loop turn " + loopCount);

            //    // check the values of the Velocities after update
            //    Assert.Equal(2 * new Vector3(loopCount, 2*loopCount, 3*loopCount), list1Data.AudioListener.Velocity, "The velocity of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(Vector3.Zero, list2Data.AudioListener.Velocity, "The velocity of the listener2 is not valid at loop turn " + loopCount);

            //    // check the values of the Forward vectors after update
            //    Assert.True((list2Data.AudioListener.Forward - new Vector3(0, -1, 0)).Length() < 1e-7, "The forward vector of listener2 is not valid at loop turn " + loopCount);
            //    Assert.Equal(new Vector3(0, 0, 1), list1Data.AudioListener.Forward, "The forward vector of the listener1 is not valid at loop turn " + loopCount);

            //    // check the values of the Up vectors after update
            //    Assert.True((list2Data.AudioListener.Up - new Vector3(0, 0, 1)).Length() < 1e-7, "The Up vector of the listener2 is not valid at loop turn " + loopCount);
            //    Assert.Equal(new Vector3(0, 1, 0), list1Data.AudioListener.Up, "The Up vector of listener1 is not valid at loop turn " + loopCount);

            //    // check that component is still marked for update for next turn.
            //    Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //}
            //else
            //{
            //    game.Exit();
            //}
        }

        /// <summary>
        /// Check component data is correctly updated when the component is first added to the Entity system and then to the audio System.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestAddEntitySysThenAudioSys()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(TestAddEntitySysThenAudioSysSetup, EntityPositionUpdate, TestAddEntitySysThenAudioSysLoopImpl);
        }

        private void TestAddEntitySysThenAudioSysSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndComponentToEntities();
            
            listComp2Entity.Transform.RotationEulerXYZ = new Vector3((float)Math.PI / 2, 0, 0);

            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            // game.Entities.Add(rootEntity);
        }

        private void TestAddEntitySysThenAudioSysLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            var audio = game.Audio;
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //var listenerProcessor = game.Entities.Processors.OfType<AudioListenerProcessor>().First();

            //if (loopCount == 1)
            //{
            //    AudioListenerProcessor.AssociatedData list1Data = null;
            //    AudioListenerProcessor.AssociatedData list2Data = null;

            //    // check that the entities are present in the processor matching list even though they have not been added to the audio system
            //    Assert.DoesNotThrow(() => list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity], "Listener Component 1 entity is not present in listener processor matching entities");
            //    Assert.DoesNotThrow(() => list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity], "Listener Component 2 entity is not present in listener processor matching entities");

            //    // check that the entities are not marked for update when they have not been added the audio System.
            //    Assert.False(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //}
            //else if (loopCount == 2)
            //{
            //    // add listener 1 to the audio system.
            //    audio.AddListener(listComp1);
            //    AudioListenerProcessor.AssociatedData list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity];
            //    AudioListenerProcessor.AssociatedData list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity];

            //    // check that listener 1 is marked for update but not listener 2.
            //    Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.False(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);

            //    // check that the listener 1's position is valid immediately after its addition to the audio system.
            //    Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), list1Data.AudioListener.Position, "The Position of the listener1 is not valid at loop turn " + loopCount);
            //}
            //else if (loopCount == 3)
            //{
            //    // add listener 2 to the audio system.
            //    audio.AddListener(listComp2);
            //    AudioListenerProcessor.AssociatedData list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity];
            //    AudioListenerProcessor.AssociatedData list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity];

            //    // check that both listeners are marked for update.
            //    Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.True(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);

            //    // check that the listener 2's position is valid directly after its addition to the system.
            //    Assert.Equal(Vector3.Zero, list2Data.AudioListener.Position, "The Position of the listener2 is not valid at loop turn " + loopCount);

            //    // check the listener 1's position, velocity, up and forward values have been correctly updated.
            //    Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), list1Data.AudioListener.Position, "The Position of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount), list1Data.AudioListener.Velocity, "The velocity of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(new Vector3(0, 0, 1), list1Data.AudioListener.Forward, "The forward vector of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(new Vector3(0, 1, 0), list1Data.AudioListener.Up, "The Up vector of listener1 is not valid at loop turn " + loopCount);
            //}
            //else if (loopCount > 3 && loopCount < 6)
            //{
            //    var list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity];
            //    var list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity];

            //    // check that both listeners' position are correctly updated.
            //    Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), list1Data.AudioListener.Position, "The Position of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(Vector3.Zero, list2Data.AudioListener.Position, "The Position of the listener2 is not valid at loop turn " + loopCount);

            //    // check that both listeners' velocity are correctly updated.
            //    Assert.Equal(2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount), list1Data.AudioListener.Velocity, "The velocity of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.Equal(Vector3.Zero, list2Data.AudioListener.Velocity, "The velocity of the listener2 is not valid at loop turn " + loopCount);

            //    // check that both listeners' up vector are correctly updated.
            //    Assert.Equal(new Vector3(0, 1, 0), list1Data.AudioListener.Up, "The Up vector of listener1 is not valid at loop turn " + loopCount);
            //    Assert.True((list2Data.AudioListener.Up - new Vector3(0, 0, 1)).Length() < 1e-7, "The Up vector of the listener2 is not valid at loop turn " + loopCount);

            //    // check that both listeners' forward vector are correctly updated.
            //    Assert.Equal(new Vector3(0, 0, 1), list1Data.AudioListener.Forward, "The forward vector of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.True((list2Data.AudioListener.Forward - new Vector3(0, -1, 0)).Length() < 1e-7, "The forward vector of listener2 is not valid at loop turn " + loopCount);

            //    // check that both listeners are still marked for update for next turn.
            //    Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //}
            //else
            //{
            //    game.Exit();
            //}
        }

        /// <summary>
        /// Check that <see cref="AudioEmitterComponent"/> are not updated anymore when removed from the audio system.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestRemoveListenerFromAudioSystem()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(AddListeners, EntityPositionUpdate, TestRemoveListenerFromAudioSystemLoopImpl);
        }

        private void AddListeners(Game game)
        {
            var audio = game.Audio;

            BuildEntityHierarchy();
            CreateAndComponentToEntities();

            audio.AddListener(listComp1);
            audio.AddListener(listComp2);

            listComp2Entity.Transform.RotationEulerXYZ = new Vector3((float)Math.PI / 2, 0, 0);

            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(rootEntity);
        }

        private void TestRemoveListenerFromAudioSystemLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            var audio = game.Audio;
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //var listenerProcessor = game.Entities.Processors.OfType<AudioListenerProcessor>().First();

            //var list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity];
            //var list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity];

            //if (loopCount == 1)
            //{
            //    // check that the listeners were marked for update before removal from the audioSystem.
            //    Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.True(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);

            //    // remove listener 1 only 
            //    audio.RemoveListener(listComp1);

            //    // check that listener 1 is not marked for update but listener 2 still is.
            //    Assert.False(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.True(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);
            //}
            //else if (loopCount == 2)
            //{
            //    // check that listener 1 is not marked for update but listener 2 still is after and update call
            //    Assert.False(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.True(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);

            //    // check that the listener 1's position, velocity, up and forward vectors are not updated anymore.
            //    Assert.NotEqual(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), list1Data.AudioListener.Position, "The Position of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.NotEqual(2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount), list1Data.AudioListener.Velocity, "The velocity of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.NotEqual(new Vector3(0, 0, 1), list1Data.AudioListener.Forward, "The forward vector of the listener1 is not valid at loop turn " + loopCount);
            //    Assert.NotEqual(new Vector3(0, 1, 0), list1Data.AudioListener.Up, "The Up vector of listener1 is not valid at loop turn " + loopCount);

            //    // remove listener 2
            //    audio.RemoveListener(listComp2);

            //    // check that both listener are not marked for update anymore.
            //    Assert.False(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.False(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);
            //}
            //else if (loopCount == 3)
            //{
            //    // check that both listener are still marked not for update after a call to AudioSystem.Update.
            //    Assert.False(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //    Assert.False(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);
            //}
            //else
            //{
            //    game.Exit();
            //}
        }

        /// <summary>
        /// Check that <see cref="AudioListenerComponent"/> are removed from the matching list of the processor 
        /// when removed from the entity system and that <see cref="AudioListener"/> associated value is put to null in the <see cref="AudioSystem"/>.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestRemoveListenerFromEntitySystem()
        {
            TestUtilities.ExecuteScriptInUpdateLoop(AddListeners, EntityPositionUpdate, TestRemoveListenerFromEntitySystemLoopImpl);
        }

        private void TestRemoveListenerFromEntitySystemLoopImpl(Game game, int loopCount, int loopCountSum)
        {
            var audio = game.Audio;
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //var listenerProcessor = game.Entities.Processors.OfType<AudioListenerProcessor>().First();

            //var list1Data = listenerProcessor.MatchingEntitiesForDebug[listComp1Entity];
            //var list2Data = listenerProcessor.MatchingEntitiesForDebug[listComp2Entity];

            //// check that the listeners were initially marked for update.
            //Assert.True(list1Data.ShouldBeComputed, "The value of should be computed for listener 1 is not valid at loop turn " + loopCount);
            //Assert.True(list2Data.ShouldBeComputed, "The value of should be computed for listener 2 is not valid at loop turn " + loopCount);

            //// remove the listeners from the entity system.
            //game.Entities.Remove(rootEntity);

            //// check that they are not present in the processor matching list anymore.
            //Assert.False(listenerProcessor.MatchingEntitiesForDebug.ContainsKey(listComp1Entity), "The matching list of the processor still contains an entity that have been removed.");
            //Assert.False(listenerProcessor.MatchingEntitiesForDebug.ContainsKey(listComp2Entity), "The matching list of the processor still contains an entity that have been removed.");

            //// check that they are still present in the AudioSystem list.
            //Assert.True(audio.Listeners.ContainsKey(listComp1), "The audioSystem does not contain any more the listener component 1");
            //Assert.True(audio.Listeners.ContainsKey(listComp1), "The audioSystem does not contain any more the listener component 2");

            //// check that the component associated AudioListener value has been set to null since not calculable anymore.
            //Assert.Null(audio.Listeners[listComp1], "The audioEmitter associated to the listener component 1 has not been set to null");
            //Assert.Null(audio.Listeners[listComp2], "The audioEmitter associated to the listener component 2 has not been set to null");

            //game.Exit();
        }
        
        /// <summary>
        /// Check that the <see cref="AudioListener"/> associated to the <see cref="AudioListenerComponent"/> are correctly updated
        /// when at least one of the <see cref="AudioEmitter"/> is added to the system.
        /// </summary>
        [Fact(Skip = "TODO: UPDATE TO USE Scene and Graphics Composer")]
        public void TestEmitterUpdateValues()
        {
            TestUtilities.ExecuteScriptInDrawLoop(TestListenerUpdateValuesSetup, UpdateEntityPositionBfrUpdate, UpdateListenerTestValues);
        }


        /// <summary>
        /// Setup configuration for the test.
        /// </summary>
        /// <param name="game"></param>
        private void TestListenerUpdateValuesSetup(Game game)
        {
            BuildEntityHierarchy();
            CreateAndComponentToEntities();
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //game.Entities.Add(rootEntity);
            game.Audio.AddListener(listComp1);
            game.Audio.AddListener(listComp2);
        }

        /// <summary>
        /// Update the entities position and <see cref="AudioEmitterComponent"/> parameters at each loop turn.
        /// </summary>
        /// <param name="game">the instance of the game</param>
        /// <param name="loopCount">the current loop count</param>
        /// <param name="loopCountSum">the current loop count sum</param>
        private void UpdateEntityPositionBfrUpdate(Game game, int loopCount, int loopCountSum)
        {
            rootSubEntity1.Transform.Position += new Vector3(loopCount, 2 * loopCount, 3 * loopCount);
            rootSubEntity2.Transform.Position += 2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount);

            listComp1Entity.Transform.Position += new Vector3(loopCount + 1, 2 * loopCount + 1, 3 * loopCount + 1);
            listComp2Entity.Transform.Position -= new Vector3(loopCount, 2 * loopCount, 3 * loopCount);

            if (loopCount >= 10)
            {
                listComp1Entity.Transform.RotationEulerXYZ = new Vector3((float)Math.PI / 2, 0, 0);
                listComp2Entity.Transform.RotationEulerXYZ = new Vector3(0, (float)Math.PI / 2, 0);
            }
        }

        private void UpdateListenerTestValues(Game game, int loopCount, int loopCountSum)
        {
            Internal.Refactor.ThrowNotImplementedException("TODO: UPDATE TO USE Scene and Graphics Composer"); 
            //var matchingEntities = game.Entities.Processors.OfType<AudioListenerProcessor>().First().MatchingEntitiesForDebug;

            //var dataComp1 = matchingEntities[listComp1Entity];
            //var dataComp2 = matchingEntities[listComp2Entity];
            
            //// check that AudioEmitters position is always valid. (this is required to ensure that the velocity is valid from the first update).
            //Assert.Equal(2 * new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum) + (loopCount + 1) * Vector3.One, dataComp1.AudioListener.Position, "Position of the listener 1 is not correct");
            //Assert.Equal(new Vector3(loopCountSum, 2 * loopCountSum, 3 * loopCountSum), dataComp2.AudioListener.Position, "Position of the listener 2 is not correct");

            //Assert.Equal(2 * new Vector3(loopCount, 2 * loopCount, 3 * loopCount) + Vector3.One, dataComp1.AudioListener.Velocity, "Velocity of the listener 1 is not correct");
            //Assert.Equal(new Vector3(loopCount, 2 * loopCount, 3 * loopCount), dataComp2.AudioListener.Velocity, "Velocity of the listener 2 is not correct");

            //if (loopCount < 10)
            //{
            //    Assert.Equal(new Vector3(0, 1, 0), dataComp1.AudioListener.Up, "Up of the listener 1 is not correct");
            //    Assert.Equal(new Vector3(0, 1, 0), dataComp2.AudioListener.Up, "Up of the listener 2 is not correct");

            //    Assert.Equal(new Vector3(0, 0, 1), dataComp1.AudioListener.Forward, "Forward of the listener 1 is not correct");
            //    Assert.Equal(new Vector3(0, 0, 1), dataComp2.AudioListener.Forward, "Forward of the listener 2 is not correct");
            //}
            //else if (loopCount == 10)
            //{
            //    Assert.Equal(new Vector3(0, 0, 1), dataComp1.AudioListener.Up, "Up of the listener 1 is not correct");
            //    Assert.Equal(new Vector3(0, 1, 0), dataComp2.AudioListener.Up, "Up of the listener 2 is not correct");

            //    Assert.Equal(new Vector3(0, -1, 0), dataComp1.AudioListener.Forward, "Forward of the listener 1 is not correct");
            //    Assert.Equal(new Vector3(1,  0, 0), dataComp2.AudioListener.Forward, "Forward of the listener 2 is not correct");
            //}
            //else
            //{
            //    game.Exit();
            //}
        }
    }
}
