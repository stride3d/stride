// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Xunit;

namespace Stride.Audio.Tests.Engine
{
    /// <summary>
    /// The velocity the processors give to listeners and emitters is in world units per second, whatever the frame time.
    /// </summary>
    /// <remarks>
    /// The tests call <see cref="SceneInstance.Draw"/> themselves with a <see cref="GameTime"/> they build,
    /// so the elapsed time of each frame is exact. Frame times are powers of two so that the results are exact floats.
    /// </remarks>
    public class TestDopplerVelocity
    {
        private TimeSpan total;

        private void DrawFrame(Game game, SceneInstance scene, double elapsedSeconds)
        {
            total += TimeSpan.FromSeconds(elapsedSeconds);
            var context = RenderContext.GetShared(game.Services);
            context.Time = new GameTime(total, TimeSpan.FromSeconds(elapsedSeconds));
            scene.Draw(context);
        }

        private static void Move(Entity entity, Vector3 position)
        {
            entity.Transform.Position = position;
            entity.Transform.UpdateWorldMatrix();
        }

        [Fact]
        public void ListenerVelocityIsUnitsPerSecond()
        {
            TestUtilities.CreateAndRunGame(ListenerVelocityIsUnitsPerSecondImpl, TestUtilities.ExitGame);
        }

        private void ListenerVelocityIsUnitsPerSecondImpl(Game game)
        {
            var component = new AudioListenerComponent();
            var entity = new Entity { component };
            Move(entity, new Vector3(10, 0, 0));
            var scene = new SceneInstance(game.Services, new Scene { Entities = { entity } });

            // No previous position yet: no velocity, not the distance to the origin.
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(10, 0, 0), component.Listener.Position);
            Assert.Equal(Vector3.Zero, component.Listener.Velocity);

            // 1 unit in a quarter of a second.
            Move(entity, new Vector3(11, 0, 0));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(4, 0, 0), component.Listener.Velocity);

            // 2 units in half a second.
            Move(entity, new Vector3(11, 0, 2));
            DrawFrame(game, scene, 0.5);
            Assert.Equal(new Vector3(0, 0, 4), component.Listener.Velocity);

            // A frame without elapsed time has no velocity, and still takes the new position.
            Move(entity, new Vector3(11, 1, 2));
            DrawFrame(game, scene, 0);
            Assert.Equal(Vector3.Zero, component.Listener.Velocity);
            Assert.Equal(new Vector3(11, 1, 2), component.Listener.Position);

            Move(entity, new Vector3(11, 2, 2));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(0, 4, 0), component.Listener.Velocity);

            // Moving while disabled, then enabled again: the first frame has no velocity.
            component.Enabled = false;
            Move(entity, new Vector3(20, 2, 2));
            DrawFrame(game, scene, 0.25);
            component.Enabled = true;
            Move(entity, new Vector3(21, 2, 2));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(Vector3.Zero, component.Listener.Velocity);
            Move(entity, new Vector3(22, 2, 2));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(4, 0, 0), component.Listener.Velocity);
        }

        [Fact]
        public void EmitterVelocityIsUnitsPerSecond()
        {
            TestUtilities.CreateAndRunGame(EmitterVelocityIsUnitsPerSecondImpl, TestUtilities.ExitGame);
        }

        private void EmitterVelocityIsUnitsPerSecondImpl(Game game)
        {
            var component = new AudioEmitterComponent();
            var entity = new Entity { component };
            Move(entity, new Vector3(10, 0, 0));

            var scene = new SceneInstance(game.Services, new Scene());
            var probe = new EmitterProbe();
            scene.Processors.Add(probe);
            scene.RootScene.Entities.Add(entity);
            var emitter = probe.GetEmitter(component);

            // The entity moves before the first frame: there is still no previous frame to measure the speed against.
            Move(entity, new Vector3(11, 0, 0));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(11, 0, 0), emitter.Position);
            Assert.Equal(Vector3.Zero, emitter.Velocity);

            // 1 unit in a quarter of a second.
            Move(entity, new Vector3(12, 0, 0));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(4, 0, 0), emitter.Velocity);

            // 2 units in half a second.
            Move(entity, new Vector3(12, 0, 2));
            DrawFrame(game, scene, 0.5);
            Assert.Equal(new Vector3(0, 0, 4), emitter.Velocity);

            // A frame without elapsed time has no velocity, and still takes the new position.
            Move(entity, new Vector3(12, 1, 2));
            DrawFrame(game, scene, 0);
            Assert.Equal(Vector3.Zero, emitter.Velocity);
            Assert.Equal(new Vector3(12, 1, 2), emitter.Position);

            Move(entity, new Vector3(12, 2, 2));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(0, 4, 0), emitter.Velocity);

            // Moving while disabled, then enabled again: the first frame has no velocity.
            component.Enabled = false;
            Move(entity, new Vector3(20, 2, 2));
            DrawFrame(game, scene, 0.25);
            component.Enabled = true;
            Move(entity, new Vector3(21, 2, 2));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(Vector3.Zero, emitter.Velocity);
            Move(entity, new Vector3(22, 2, 2));
            DrawFrame(game, scene, 0.25);
            Assert.Equal(new Vector3(4, 0, 0), emitter.Velocity);
        }

        /// <summary>
        /// Gives the test access to the <see cref="AudioEmitter"/> the processor keeps for a component.
        /// </summary>
        private class EmitterProbe : AudioEmitterProcessor
        {
            public AudioEmitter GetEmitter(AudioEmitterComponent component) => ComponentDatas[component].AudioEmitter;
        }
    }
}
