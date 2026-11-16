// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Xunit;

namespace Stride.Audio.Tests.Engine
{
    public class TestAudioEmitterZeroMatrix
    {
        private sealed class ProbeProcessor : AudioEmitterProcessor
        {
            public AudioEmitter EmitterOf(AudioEmitterComponent component) => ComponentDatas[component].AudioEmitter;

            public AudioEmitterComponent[] ComponentsInIterationOrder => ComponentDatas.Keys.ToArray();
        }

        [Fact]
        public void EmitterWithZeroWorldMatrixDoesNotStopUpdateOfFollowingEmitters()
        {
            TestUtilities.CreateAndRunGame(RunTest, TestUtilities.ExitGame);
        }

        private static void RunTest(Game game)
        {
            var scene = new Scene();
            var sceneInstance = new SceneInstance(game.Services, scene);
            var probe = new ProbeProcessor();
            sceneInstance.Processors.Add(probe);

            // An entity whose local matrix is all zero keeps a zero world matrix through the transform update at Draw.
            var zeroComponent = new AudioEmitterComponent();
            var zeroEntity = new Entity("zero") { zeroComponent };
            zeroEntity.Transform.UseTRS = false;
            zeroEntity.Transform.LocalMatrix = Matrix.Zero;

            var normalComponent = new AudioEmitterComponent();
            var normalEntity = new Entity("normal") { normalComponent };
            normalEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.PiOverTwo);

            scene.Entities.Add(zeroEntity);
            scene.Entities.Add(normalEntity);
            // Set after the entity is added: only the transform update at Draw can bring the emitter to this position.
            normalEntity.Transform.Position = new Vector3(3, 0, 0);

            // The processor iterates its emitters in insertion order: the zero-matrix one must come first.
            Assert.Equal(new[] { zeroComponent, normalComponent }, probe.ComponentsInIterationOrder);

            var normalEmitter = probe.EmitterOf(normalComponent);
            var defaultForward = normalEmitter.Forward;

            var renderContext = RenderContext.GetShared(game.Services);
            renderContext.Time = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            sceneInstance.Draw(renderContext);

            Assert.Equal(Matrix.Zero, zeroEntity.Transform.WorldMatrix);

            Assert.Equal(new Vector3(3, 0, 0), normalEmitter.Position);

            // Forward is set after the zero-matrix check: it follows the rotated world matrix.
            Assert.NotEqual(defaultForward, normalEmitter.Forward);
            Assert.True(Vector3.Distance(new Vector3(1, 0, 0), normalEmitter.Forward) < 1e-4f, $"Forward was {normalEmitter.Forward}");
        }
    }
}
