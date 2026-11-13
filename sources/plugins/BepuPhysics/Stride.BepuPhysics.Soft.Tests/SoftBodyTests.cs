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
using Stride.Rendering;
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
    public static void OneTouchingEventPerStepWithABodyLargeEnoughToSplitAcrossWorkers()
    {
        RunScene(async scene =>
        {
            var events = new TouchingHandler();
            // Resolution 10 gives 11^3 = 1331 particles, enough that the narrow phase hands pairs to several workers
            // regardless of the machine's core count.
            var body = new VolumetricSoftBodyComponent { Resolution = 10, ContactEventHandler = events, SleepThreshold = 0f };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));

            await Seconds(body, 1.5f);
            for (int step = 0; step < 10; step++)
            {
                events.Calls = 0;
                await body.Simulation!.AfterUpdate();
                Assert.Equal(1, events.Calls);
            }

            // Resting on the floor through many particles; the merged groups all carry the same up-pointing normal
            Assert.True(events.Groups > 10);
            Assert.All(events.Normals, normal => Assert.True(normal.Y > 0.9f, $"Normal {normal}"));
        });
    }

    /// <summary>
    /// Half on the floor and half on a plate, the particles of the body alternate between the two in the order the narrow phase
    /// reports them: each of the two collidables still gets a single event per step, with one worker as with many.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public static void OneTouchingEventPerStepAndPerCollidableTouched(int threadCount)
    {
        RunScene(async scene =>
        {
            var events = new PerOtherHandler();
            var body = new VolumetricSoftBodyComponent { Resolution = 3, ContactEventHandler = events, SleepThreshold = 0f };
            var plate = new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(1f, 0.04f, 2f) } } } };
            scene.Add(plate, new Vector3(0.5f, 0.02f, 0f));
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));
            body.Simulation!.ThreadCount = threadCount;

            await Seconds(body, 1.5f);
            for (int step = 0; step < 10; step++)
            {
                events.Calls.Clear();
                await body.Simulation!.AfterUpdate();
                Assert.Equal(2, events.Calls.Count);
                Assert.All(events.Calls, call => Assert.True(call.Value.Count == 1, $"{call.Value.Count} events with {call.Value.Groups} groups for {call.Key.Entity}"));
                Assert.All(events.Calls, call => Assert.True(call.Value.Groups > 1));
            }
        });
    }

    /// <summary>
    /// The particles of a soft body have more constraints than the solver has batches for: the rest goes to its fallback batch,
    /// whose slots are not all filled. The contact recorder of the debug view goes through every batch after each step.
    /// </summary>
    [Fact]
    public static void DebugContactRecorderGoesThroughTheFallbackBatch()
    {
        RunScene(async scene =>
        {
            var lower = new VolumetricSoftBodyComponent { Resolution = 4, SleepThreshold = 0f };
            var upper = new VolumetricSoftBodyComponent { Resolution = 4, SleepThreshold = 0f };
            scene.Add(lower, new CubeProceduralModel(), new Vector3(0f, 0.8f, 0f));
            scene.Add(upper, new CubeProceduralModel(), new Vector3(0.3f, 2.2f, 0.2f));

            var simulation = lower.Simulation!.Simulation;
            using var recorder = new Stride.BepuPhysics.Debug.ContactRecorder(simulation);
            bool emptySlot = false;
            for (int step = 0; step < 150; step++)
            {
                await lower.Simulation!.AfterUpdate();

                // What the recorder must skip: a slot without a constraint among the contacts of the fallback batch
                ref var active = ref simulation.Solver.Sets[0];
                if (active.Batches.Count <= simulation.Solver.FallbackBatchThreshold)
                    continue;
                ref var fallback = ref active.Batches[simulation.Solver.FallbackBatchThreshold];
                for (int t = 0; t < fallback.TypeBatches.Count && emptySlot == false; t++)
                {
                    ref var typeBatch = ref fallback.TypeBatches[t];
                    if (global::BepuPhysics.CollisionDetection.NarrowPhase.IsContactConstraintType(typeBatch.TypeId) == false)
                        continue;
                    for (int i = 0; i < typeBatch.ConstraintCount; i++)
                        emptySlot |= typeBatch.IndexToHandle[i].Value < 0;
                }
            }

            Assert.True(emptySlot, "The fallback batch of the solver never had an empty contact slot, this scene no longer covers what it is for");
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

    /// <summary>
    /// A model made of two cubes apart from each other is a body in two pieces, which sleep and wake on their own in the solver:
    /// the body is awake as long as one of them is, and is put to sleep or woken up as a whole.
    /// </summary>
    [Fact]
    public static void ABodyInSeveralPiecesWakesAndSleepsAsOne()
    {
        RunScene(async scene =>
        {
            var model = new ProceduralModelDescriptor(new CubeProceduralModel()).GenerateModel(scene.Game.Services);
            model.Skeleton = new Skeleton
            {
                Nodes =
                [
                    new ModelNodeDefinition { Name = "Root", ParentIndex = -1, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Rotation = Quaternion.Identity, Scale = Vector3.One } },
                    new ModelNodeDefinition { Name = "Far", ParentIndex = 0, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Position = new Vector3(3f, 0f, 0f), Rotation = Quaternion.Identity, Scale = Vector3.One } },
                ],
            };
            model.Meshes.Add(new Mesh(model.Meshes[0]) { NodeIndex = 1 });

            var body = new VolumetricSoftBodyComponent { Resolution = 8 };
            scene.Add(body, model, new Vector3(0f, 0.6f, 0f));
            await body.Simulation!.AfterUpdate();

            int near = 0, far = 0;
            for (int i = 0; i < body.ParticleCount; i++)
            {
                if (body.GetParticlePosition(i).X < body.GetParticlePosition(near).X)
                    near = i;
                if (body.GetParticlePosition(i).X > body.GetParticlePosition(far).X)
                    far = i;
            }
            Assert.True(body.GetParticlePosition(far).X - body.GetParticlePosition(near).X > 3f);

            body.Awake = false;
            Assert.False(body.Particle(near).Awake);
            Assert.False(body.Particle(far).Awake);
            Assert.False(body.Awake);

            foreach (int piece in new[] { near, far })
            {
                body.ApplyParticleImpulse(piece, Vector3.UnitY * 0.01f);
                Assert.True(body.Awake);
                body.Awake = false;
            }

            body.Awake = true;
            Assert.True(body.Particle(near).Awake);
            Assert.True(body.Particle(far).Awake);
        });
    }

    /// <summary> What a soft body shows in the debug view: three lines for the cross on each particle, then one for each spring </summary>
    [Fact]
    public static void DebugViewGetsTheParticlesAndTheSprings()
    {
        RunScene(async scene =>
        {
            var body = new VolumetricSoftBodyComponent { Resolution = 2 };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));
            await body.Simulation!.AfterUpdate();

            var lines = new LineCollector();
            body.AddDebugLines(lines);

            var topology = body.Topology!;
            Assert.Equal(body.ParticleCount * 3 + topology.Edges.Length, lines.Segments.Count);
            for (int i = 0; i < body.ParticleCount; i++)
            {
                var (start, end, _) = lines.Segments[i * 3];
                Assert.True(Vector3.Distance((start + end) * 0.5f, body.GetParticlePosition(i)) < 1e-4f, $"Cross of particle {i}");
            }
            for (int i = 0; i < topology.Edges.Length; i++)
            {
                var (start, end, _) = lines.Segments[body.ParticleCount * 3 + i];
                Assert.Equal(body.GetParticlePosition(topology.Edges[i].A), start);
                Assert.Equal(body.GetParticlePosition(topology.Edges[i].B), end);
            }

            // Each of the two is a setting of the soft bodies
            var settings = scene.Game.Services.GetOrCreate<SoftBodyConfiguration>();
            settings.ShowDebugParticles = false;
            lines.Segments.Clear();
            body.AddDebugLines(lines);
            Assert.Equal(topology.Edges.Length, lines.Segments.Count);

            settings.ShowDebugParticles = true;
            settings.ShowDebugSprings = false;
            settings.DebugParticleSize = 0.5f;
            lines.Segments.Clear();
            body.AddDebugLines(lines);
            Assert.Equal(body.ParticleCount * 3, lines.Segments.Count);
            Assert.All(lines.Segments, segment => Assert.Equal(1f, Vector3.Distance(segment.Start, segment.End), 4));

            settings.ShowDebugParticles = false;
            lines.Segments.Clear();
            body.AddDebugLines(lines);
            Assert.Empty(lines.Segments);
        });
    }

    private sealed class LineCollector : IDebugLineCollector
    {
        public readonly System.Collections.Generic.List<(Vector3 Start, Vector3 End, Color Color)> Segments = new();

        public void AddLine(Vector3 start, Vector3 end, Color color) => Segments.Add((start, end, color));
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

    private sealed class PerOtherHandler : IContactHandler
    {
        public bool NoContactResponse => false;
        public readonly System.Collections.Generic.Dictionary<CollidableComponent, (int Count, int Groups)> Calls = new();

        public void OnTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            Calls.TryGetValue(contacts.Other, out var call);
            Calls[contacts.Other] = (call.Count + 1, contacts.Groups.Length);
        }
    }

    internal static async Task Seconds(SoftBodyComponent body, float seconds)
    {
        var simulation = body.Simulation!;
        for (int steps = (int)(seconds * 60f); steps > 0; steps--)
            await simulation.AfterUpdate();
    }

    internal static void RunScene(Func<TestScene, Task> test)
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

    internal sealed class TestScene(TestGame game)
    {
        public Game Game => game;

        /// <summary> Adds an entity with a model generated from <paramref name="shape"/> and <paramref name="body"/> </summary>
        public Entity Add(SoftBodyComponent body, PrimitiveProceduralModelBase shape, Vector3 position)
        {
            return Add(body, new ProceduralModelDescriptor(shape).GenerateModel(game.Services), position);
        }

        /// <summary> Adds an entity with <paramref name="model"/> and <paramref name="body"/> </summary>
        public Entity Add(SoftBodyComponent body, Model model, Vector3 position)
        {
            var entity = new Entity { new ModelComponent(model), body };
            entity.Transform.Position = position;
            game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            return entity;
        }

        /// <summary> Adds an entity with <paramref name="component"/> </summary>
        public Entity Add(EntityComponent component, Vector3 position)
        {
            var entity = new Entity { component };
            entity.Transform.Position = position;
            game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            return entity;
        }
    }

    internal sealed class TestGame : GameTestBase
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
