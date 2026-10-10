// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Regression;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.BepuPhysics.Soft.Tests;

public class SoftBodyTests : GameTestBase
{
    [Fact]
    public static void VolumetricBodyFallsAndRests()
    {
        RunScene(async scene =>
        {
            var body = new VolumetricSoftBodyComponent { Resolution = 4 };
            var entity = scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 2f, 0f));
            Assert.Equal(5 * 5 * 5, body.ParticleCount);

            await Seconds(body, 3f);

            // Resting on the floor, slightly squashed by its own weight
            Assert.InRange(entity.Transform.Position.Y, 0.4f, 0.55f);
            Assert.True(Vector3.Distance(Vector3.Transform(Vector3.UnitY, entity.Transform.Rotation), Vector3.UnitY) < 0.05f);
            for (int i = 0; i < body.ParticleCount; i++)
                Assert.True(body.GetParticlePosition(i).Y > -0.01f);
        });
    }

    [Fact]
    public static void ClothHangsFromItsPins()
    {
        RunScene(async scene =>
        {
            // Pinned along the edge at x = -0.5
            var edge = new BoundingBox(new Vector3(-0.51f, -1f, -1f), new Vector3(-0.49f, 1f, 1f));
            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = edge } } };
            scene.Add(cloth, new PlaneProceduralModel { Size = new Vector2(1f), Tessellation = new Int2(10), Normal = NormalDirection.UpY, GenerateBackFace = true }, new Vector3(0f, 2f, 0f));
            Assert.Equal(11 * 11, cloth.ParticleCount);

            // It swings like a pendulum around the pinned edge, which never moves, and the free edge comes down to about a meter below it
            var lowest = float.MaxValue;
            var simulation = cloth.Simulation!;
            for (int step = 0; step < 120; step++)
            {
                await simulation.AfterUpdate();
                for (int i = 0; i < cloth.ParticleCount; i++)
                {
                    var position = cloth.GetParticlePosition(i);
                    if (cloth.IsParticlePinned(i))
                        Assert.Equal(2f, position.Y, 3);
                    else
                        Assert.True(position.Y < 2.05f, $"Particle {i} at {position}");
                    lowest = MathF.Min(lowest, position.Y);
                }
            }
            Assert.InRange(lowest, 0.9f, 1.1f);
        });
    }

    [Fact]
    public static void RaysAndContactsReportTheSoftBody()
    {
        RunScene(async scene =>
        {
            var events = new CountingHandler();
            var body = new VolumetricSoftBodyComponent { Resolution = 3, ContactEventHandler = events };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));

            await Seconds(body, 1.5f);

            Assert.True(body.Simulation!.RayCast(new Vector3(0f, 5f, 0f), -Vector3.UnitY, 10f, out var hit));
            Assert.Same(body, hit.Collidable);
            Assert.True(body.RayCast(new Vector3(5f, 0.3f, 0f), -Vector3.UnitX, 10f, out hit));
            Assert.Same(body, hit.Collidable);

            // Touching the floor, its own particles touching each other do not count
            Assert.NotEmpty(events.Others);
            Assert.All(events.Others, other => Assert.IsType<StaticComponent>(other));
        });
    }

    [Fact]
    public static void OneTouchingEventPerStepWithNormalsTowardsTheBody()
    {
        RunScene(async scene =>
        {
            var events = new TouchingHandler();
            var body = new VolumetricSoftBodyComponent { Resolution = 3, ContactEventHandler = events, SleepThreshold = 0f };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));

            await Seconds(body, 1.5f);
            for (int step = 0; step < 10; step++)
            {
                events.Calls = 0;
                await body.Simulation!.AfterUpdate();
                Assert.Equal(1, events.Calls);
            }

            // Resting on the floor through several particles, every normal points from the floor up to the body
            Assert.True(events.Groups > 1);
            Assert.All(events.Normals, normal => Assert.True(normal.Y > 0.9f, $"Normal {normal}"));
        });
    }

    [Fact]
    public static void ParticlesRespectTheCollisionMatrix()
    {
        RunScene(async scene =>
        {
            var lower = new VolumetricSoftBodyComponent { Resolution = 3, CollisionLayer = CollisionLayer.Layer1 };
            var upper = new VolumetricSoftBodyComponent { Resolution = 3, CollisionLayer = CollisionLayer.Layer2 };
            scene.Add(lower, new CubeProceduralModel(), new Vector3(0f, 0.5f, 0f));
            var upperEntity = scene.Add(upper, new CubeProceduralModel(), new Vector3(0f, 2f, 0f));
            lower.Simulation!.CollisionMatrix.Set(CollisionLayer.Layer1, CollisionLayer.Layer2, false);

            await Seconds(upper, 2f);

            // Fell through the lower body onto the floor
            Assert.InRange(upperEntity.Transform.Position.Y, 0.35f, 0.6f);
        });
    }

    private sealed class CountingHandler : IContactHandler
    {
        public bool NoContactResponse => false;
        public readonly System.Collections.Generic.List<CollidableComponent> Others = new();

        public void OnStartedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            Others.Add(contacts.Other);
        }
    }

    private sealed class TouchingHandler : IContactHandler
    {
        public bool NoContactResponse => false;
        public int Calls, Groups;
        public readonly System.Collections.Generic.List<Vector3> Normals = new();

        public void OnTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            Calls++;
            Groups = contacts.Groups.Length;
            foreach (var contact in contacts)
                Normals.Add(contact.Normal);
        }
    }

    private static async Task Seconds(SoftBodyComponent body, float seconds)
    {
        var simulation = body.Simulation!;
        for (int steps = (int)(seconds * 60f); steps > 0; steps--)
            await simulation.AfterUpdate();
    }

    private static void RunScene(Func<TestScene, Task> test)
    {
        var game = new TestGame();
        Exception? failure = null;
        game.Script.AddTask(async () =>
        {
            try
            {
                game.ScreenShotAutomationEnabled = false;
                var floor = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(20f, 1f, 20f) } } } } };
                floor.Transform.Position = new Vector3(0f, -0.5f, 0f);
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(floor);
                await game.Script.NextFrame();

                await test(new TestScene(game));
            }
            catch (Exception e)
            {
                failure = e; // Thrown from a script it would only be logged, and the game would never exit
            }
            game.Exit();
        });
        RunGameTest(game);
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);
    }

    private sealed class TestScene(TestGame game)
    {
        /// <summary> Adds an entity with a model generated from <paramref name="shape"/> and <paramref name="body"/> </summary>
        public Entity Add(SoftBodyComponent body, PrimitiveProceduralModelBase shape, Vector3 position)
        {
            var entity = new Entity { new ModelComponent(new ProceduralModelDescriptor(shape).GenerateModel(game.Services)), body };
            entity.Transform.Position = position;
            game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            return entity;
        }
    }

    private sealed class TestGame : GameTestBase
    {
        public TestGame()
        {
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        }
    }
}
