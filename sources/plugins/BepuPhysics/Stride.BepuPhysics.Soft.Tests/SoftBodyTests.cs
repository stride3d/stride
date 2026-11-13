// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Soft.Colliders;
using Stride.BepuPhysics.Soft.Topology;
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

    /// <summary>
    /// Each constraint of a particle needs a batch of the solver of its own, and the particles of a lattice are held by many:
    /// with the contacts of a body at rest on top, they still fit in the batches the solver has before its fallback one.
    /// </summary>
    [Fact]
    public static void LatticeKeepsTheSolverBatchesBelowTheFallbackThreshold()
    {
        RunScene(async scene =>
        {
            var body = new VolumetricSoftBodyComponent { Resolution = 6, SleepThreshold = 0f };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 0.6f, 0f));
            await Seconds(body, 0.3f);

            var solver = body.Simulation!.Simulation.Solver;
            int most = 0;
            for (int i = 0; i < body.ParticleCount; i++)
                most = Math.Max(most, body.Particle(i).Constraints.Count);
            Assert.True(most <= 40, $"A particle is in {most} constraints");
            Assert.True(solver.ActiveSet.Batches.Count <= solver.FallbackBatchThreshold, $"{solver.ActiveSet.Batches.Count} batches");
        });
    }

    /// <summary>
    /// The collider of the particles is not a public type, so no other collidable can be given one. A soft body refuses any other
    /// collider as it is given, and comes back from the binary form of a compiled scene with its own and the radius of its particles.
    /// </summary>
    [Fact]
    public static void SoftBodiesMakeTheirOwnCollider()
    {
        Assert.False(typeof(ParticleCollider).IsVisible);

        var soft = new VolumetricSoftBodyComponent { ParticleRadius = 0.07f };
        Assert.Throws<NotSupportedException>(() => soft.Collider = new CompoundCollider { Colliders = { new SphereCollider() } });
        Assert.Throws<NotSupportedException>(() => ((CollidableComponent)soft).Collider = new CompoundCollider { Colliders = { new SphereCollider() } });
        Assert.IsType<ParticleCollider>(soft.Collider);

        var stream = new System.IO.MemoryStream();
        var writer = new Stride.Core.Serialization.BinarySerializationWriter(stream);
        Stride.Core.Serialization.SerializerExtensions.Write(writer, soft);
        writer.Flush();
        stream.Seek(0, System.IO.SeekOrigin.Begin);
        VolumetricSoftBodyComponent? copy = null;
        Stride.Core.Serialization.SerializerExtensions.Serialize(new Stride.Core.Serialization.BinarySerializationReader(stream), ref copy, Stride.Core.Serialization.ArchiveMode.Deserialize);
        Assert.IsType<ParticleCollider>(copy!.Collider);
        Assert.Equal(0.07f, copy.ParticleRadius);
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

            // It swings like a pendulum around the pinned edge, which stays in place, and the free edge comes down to about a meter below it
            var lowest = float.MaxValue;
            var simulation = cloth.Simulation!;
            for (int step = 0; step < 120; step++)
            {
                await simulation.AfterUpdate();
                for (int i = 0; i < cloth.ParticleCount; i++)
                {
                    var position = cloth.GetParticlePosition(i);
                    // Pinned particles are held by a stiff spring, they give a little under the weight of the cloth
                    if (cloth.IsParticlePinned(i))
                        Assert.InRange(position.Y, 1.97f, 2.03f);
                    else
                        Assert.True(position.Y < 2.05f, $"Particle {i} at {position}");
                    lowest = MathF.Min(lowest, position.Y);
                }
            }
            Assert.InRange(lowest, 0.9f, 1.1f);
        });
    }

    /// <summary> A pin on a node of the anchor's model is carried by that node, not by the entity </summary>
    [Fact]
    public static void PinsFollowAnAnchorNode()
    {
        RunScene(async scene =>
        {
            // A skeleton whose second node sits at the cloth's pinned edge
            var anchorModel = new ProceduralModelDescriptor(new CubeProceduralModel { Size = new Vector3(0.1f) }).GenerateModel(scene.Game.Services);
            anchorModel.Skeleton = new Skeleton { Nodes = [Node("Root", -1, Vector3.Zero), Node("Hand", 0, new Vector3(-0.5f, 2f, 0f))] };
            var anchor = new Entity { new ModelComponent(anchorModel) };
            scene.Add(anchor);

            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new WorldAnchor { Entity = anchor, NodeName = "Hand" } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));
            await Seconds(cloth, 0.1f);

            var pinned = Enumerable.Range(0, cloth.ParticleCount).Where(cloth.IsParticlePinned).ToArray();
            Assert.NotEmpty(pinned);
            var before = pinned.Select(cloth.GetParticlePosition).ToArray();

            // Moving the node, not the entity, carries the pinned particles along
            anchor.Get<ModelComponent>()!.Skeleton.NodeTransformations[1].Transform.Position += new Vector3(1f, 0.5f, 0f);
            await Seconds(cloth, 0.5f);

            for (int i = 0; i < pinned.Length; i++)
                Assert.True(Vector3.Distance(cloth.GetParticlePosition(pinned[i]), before[i] + new Vector3(1f, 0.5f, 0f)) < 0.02f, $"Pinned particle {pinned[i]} at {cloth.GetParticlePosition(pinned[i])}");
        });
    }

    /// <summary> The particles pinned to a body pull on it, whether the body was there before the cloth or came after </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void PinnedParticlesPullTheirBodyAnchor(bool anchorFirst)
    {
        RunScene(async scene =>
        {
            // A floating block above the pinned edge of a cloth
            var block = new BodyComponent { Gravity = false, Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(0.1f), Mass = 1f } } } };
            var anchor = new Entity { block };
            anchor.Transform.Position = new Vector3(-0.5f, 2.3f, 0f);
            if (anchorFirst)
                scene.Add(anchor);

            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new BodyAnchor { Body = block } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));
            if (anchorFirst == false)
                scene.Add(anchor);

            // The weight of the cloth drags the block down
            await Seconds(cloth, 1f);
            Assert.True(block.Position.Y < 2f, $"Block at {block.Position}");
        });
    }

    /// <summary> A cloth pinned to a kinematic body follows it when it starts moving, also when the cloth was asleep by then </summary>
    [Fact]
    public static void PinsFollowAKinematicBodyAndWakeUp()
    {
        RunScene(async scene =>
        {
            var rail = new BodyComponent { Kinematic = true, Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(0.1f, 0.1f, 1.2f) } } } };
            var anchor = new Entity { rail };
            anchor.Transform.Position = new Vector3(-0.5f, 2f, 0f);
            scene.Add(anchor);
            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new BodyAnchor { Body = rail } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));
            await Seconds(cloth, 2f);

            int pinned = 0;
            while (cloth.IsParticlePinned(pinned) == false)
                pinned++;
            var before = cloth.GetParticlePosition(pinned);
            cloth.Awake = false;
            Assert.False(cloth.Awake);

            rail.LinearVelocity = new Vector3(0f, 0f, 1f);
            rail.Awake = true;
            await Seconds(cloth, 1f);

            Assert.True(cloth.Awake);
            Assert.True(Vector3.Distance(cloth.GetParticlePosition(pinned), before + new Vector3(0f, 0f, 1f)) < 0.1f, $"Pinned particle went from {before} to {cloth.GetParticlePosition(pinned)}");
        });
    }

    /// <summary>
    /// Out of a simulation, as in the editor, the model of the entity stands for the body in what draws and picks colliders;
    /// in one the particles move on their own, and nothing fixed to the entity is given.
    /// </summary>
    [Fact]
    public static void TheModelStandsForTheBodyOutOfASimulation()
    {
        RunScene(async scene =>
        {
            var cloth = new ClothComponent();
            var entity = new Entity { new ModelComponent(new ProceduralModelDescriptor(ClothModel).GenerateModel(scene.Game.Services)), cloth };
            entity.Transform.Scale = new Vector3(2f);
            var collider = cloth.Collider;

            Assert.Equal(1, collider.Transforms);
            var transforms = new ShapeTransform[1];
            collider.GetLocalTransforms(cloth, transforms);
            Assert.Equal(new Vector3(2f), transforms[0].Scale);

            scene.Add(entity);
            await cloth.Simulation!.AfterUpdate();
            Assert.Equal(0, collider.Transforms);
        });
    }

    /// <summary> A soft body without a model cannot be built: it stays out of the simulation, and says so once </summary>
    [Fact]
    public static void ASoftBodyWithoutAModelIsReported()
    {
        RunScene(async scene =>
        {
            var warnings = new System.Collections.Generic.List<string>();
            void Collect(Stride.Core.Diagnostics.ILogMessage message)
            {
                if (message.Type >= Stride.Core.Diagnostics.LogMessageType.Warning)
                    warnings.Add(message.Text);
            }
            Stride.Core.Diagnostics.GlobalLogger.GlobalMessageLogged += Collect;
            try
            {
                var body = new VolumetricSoftBodyComponent();
                scene.Add(body, new Vector3(0f, 2f, 0f));
                for (int frame = 0; frame < 3; frame++)
                    await scene.Game.Script.NextFrame();

                Assert.Null(body.Simulation);
                Assert.Single(warnings, warning => warning.Contains(nameof(VolumetricSoftBodyComponent)) && warning.Contains(nameof(ModelComponent)));
            }
            finally
            {
                Stride.Core.Diagnostics.GlobalLogger.GlobalMessageLogged -= Collect;
            }
        });
    }

    /// <summary>
    /// The collider and the collision group of a soft body are its own business: neither the editor nor a scene file sees them,
    /// while they see the ones of the other collidables.
    /// </summary>
    [Fact]
    public static void SoftBodiesKeepTheirColliderAndCollisionGroupToThemselves()
    {
        static System.Collections.Generic.List<string> MembersOf(Type type) => Stride.Core.Reflection.TypeDescriptorFactory.Default.Find(type).Members.Select(member => member.Name).ToList();

        Assert.Contains("Collider", MembersOf(typeof(BodyComponent)));
        Assert.Contains("CollisionGroup", MembersOf(typeof(BodyComponent)));
        foreach (var type in new[] { typeof(ClothComponent), typeof(VolumetricSoftBodyComponent) })
        {
            var members = MembersOf(type);
            Assert.DoesNotContain("Collider", members);
            Assert.DoesNotContain("CollisionGroup", members);
            Assert.Contains("ParticleRadius", members);
            Assert.Contains("CollisionLayer", members);
        }
    }

    [Fact]
    public static void PinWithoutRegionHoldsTheParticlesClosestToItsAnchor()
    {
        RunScene(async scene =>
        {
            // Hooks at two corners of a cloth, and a point between two vertices of its far edge
            var hooks = new[] { new Vector3(-0.5f, 2f, -0.5f), new Vector3(0.5f, 2f, -0.5f) };
            var pins = hooks.Select(hook => new SoftBodyPin { Anchor = new WorldAnchor { Entity = new Entity { Transform = { Position = hook } } } }).ToList();
            pins.Add(new SoftBodyPin { Region = new BoundingBox(new Vector3(0.05f, 0f, 0.5f), new Vector3(0.05f, 0f, 0.5f)) });
            foreach (var pin in pins.Take(2))
                scene.Add(((WorldAnchor)pin.Anchor!).Entity!);
            var cloth = new ClothComponent();
            cloth.Pins.AddRange(pins);
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));
            await Seconds(cloth, 0.1f);

            var pinned = Enumerable.Range(0, cloth.ParticleCount).Where(cloth.IsParticlePinned).Select(i => cloth.Topology!.RestPositions[i]).ToList();
            Assert.Equal(4, pinned.Count);
            Assert.Contains(new Vector3(-0.5f, 0f, -0.5f), pinned);
            Assert.Contains(new Vector3(0.5f, 0f, -0.5f), pinned);
            Assert.Contains(pinned, p => MathUtil.NearEqual(p.Z, 0.5f) && MathUtil.NearEqual(p.X, 0f));
            Assert.Contains(pinned, p => MathUtil.NearEqual(p.Z, 0.5f) && MathUtil.NearEqual(p.X, 0.1f));
        });
    }

    [Fact]
    public static void PinnedParticlesDoNotPushTheirBodyAnchor()
    {
        RunScene(async scene =>
        {
            // The pinned edge of a weightless cloth runs through a floating block
            var block = new BodyComponent { Gravity = false, Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(0.08f), Mass = 1f } } } };
            scene.Add(block, new Vector3(-0.5f, 2f, 0f));

            var cloth = new ClothComponent { Gravity = false, Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new BodyAnchor { Body = block } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));

            // Contacts between the pinned particles and the block would push it, then the particles after it, forever
            await Seconds(cloth, 2f);
            Assert.True(Vector3.Distance(block.Position, new Vector3(-0.5f, 2f, 0f)) < 0.05f, $"Block at {block.Position}");
            Assert.True(block.LinearVelocity.Length() < 0.05f, $"Block moving at {block.LinearVelocity}");
        });
    }

    /// <summary> The pinned edge of a cloth runs through the static post it is tied to: its particles are held inside of it, not pushed out </summary>
    [Fact]
    public static void PinnedParticlesDoNotTouchTheirStaticAnchor()
    {
        RunScene(async scene =>
        {
            var post = new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(0.08f, 0.3f, 2f) } } } };
            scene.Add(post, new Vector3(-0.5f, 2f, 0f));

            var cloth = new ClothComponent { Gravity = false, Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new BodyAnchor { Body = post } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));
            await Seconds(cloth, 1f);

            for (int i = 0; i < cloth.ParticleCount; i++)
            {
                if (cloth.IsParticlePinned(i))
                    Assert.True(Vector3.Distance(cloth.GetParticlePosition(i), cloth.Topology!.RestPositions[i] + new Vector3(0f, 2f, 0f)) < 0.01f, $"Pinned particle {i} at {cloth.GetParticlePosition(i)}");
            }
        });
    }

    /// <summary> A cloth tied to a soft body hangs from its particles </summary>
    [Fact]
    public static void PinnedParticlesHoldOnToASoftBody()
    {
        RunScene(async scene =>
        {
            // A soft cube held in the air by its upper half, the edge of the cloth lies against its side
            var upperHalf = new BoundingBox(new Vector3(-1f, 0f, -1f), new Vector3(1f, 1f, 1f));
            var cube = new VolumetricSoftBodyComponent { Resolution = 3, Pins = { new SoftBodyPin { Region = upperHalf } } };
            scene.Add(cube, new CubeProceduralModel(), new Vector3(-1f, 3f, 0f));

            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new BodyAnchor { Body = cube } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 3f, 0f));
            await Seconds(cloth, 2f);

            float lowest = float.MaxValue;
            for (int i = 0; i < cloth.ParticleCount; i++)
            {
                var position = cloth.GetParticlePosition(i);
                if (cloth.IsParticlePinned(i))
                    Assert.InRange(position.Y, 2.7f, 3.1f);
                lowest = MathF.Min(lowest, position.Y);
            }
            Assert.InRange(lowest, 1.7f, 2.3f);
        });
    }

    /// <summary>
    /// Each particle joined to a body is one more constraint on it, and each constraint of a body needs a batch of the solver of its own:
    /// a pin holding two layers of a body only joins some of their particles, the body hangs from those.
    /// </summary>
    [Fact]
    public static void PinsJoinABoundedNumberOfParticlesToABody()
    {
        RunScene(async scene =>
        {
            var block = new BodyComponent { Kinematic = true, Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(1f, 0.2f, 1f) } } } };
            scene.Add(block, new Vector3(0f, 3.3f, 0f));

            var upperLayers = new BoundingBox(new Vector3(-1f, 0.3f, -1f), new Vector3(1f, 1f, 1f));
            var body = new VolumetricSoftBodyComponent { Resolution = 8, Pins = { new SoftBodyPin { Region = upperLayers, Anchor = new BodyAnchor { Body = block } } } };
            var entity = scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 2.5f, 0f));
            await body.Simulation!.AfterUpdate();

            // What is left of the constraints once those holding the lattice together are counted out
            var topology = body.Topology!;
            int ownConstraints = topology.Edges.Length + topology.Edges.Count(edge => edge.Kind == EdgeKind.Stretch) + topology.Tetrahedra.Length;
            int joints = body.Simulation.Simulation.Solver.CountConstraints() - ownConstraints;
            int pinned = Enumerable.Range(0, body.ParticleCount).Count(body.IsParticlePinned);
            Assert.True(pinned > 64, $"{pinned} pinned particles");
            Assert.InRange(joints, 1, 32);

            await Seconds(body, 1f);
            Assert.InRange(entity.Transform.Position.Y, 2.3f, 2.6f);
        });
    }

    /// <summary> The joints of a pin go with the body of its anchor when the anchor is rebuilt: the pin makes them again </summary>
    [Fact]
    public static void PinsAreJoinedAgainWhenTheirAnchorIsRebuilt()
    {
        RunScene(async scene =>
        {
            var box = new BoxCollider { Size = new Vector3(0.1f) };
            var block = new BodyComponent { Kinematic = true, Collider = new CompoundCollider { Colliders = { box } } };
            scene.Add(block, new Vector3(-0.5f, 2.3f, 0f));

            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = PinnedEdge, Anchor = new BodyAnchor { Body = block } } } };
            scene.Add(cloth, ClothModel, new Vector3(0f, 2f, 0f));
            await Seconds(cloth, 0.5f);

            box.Size = new Vector3(0.12f);
            await Seconds(cloth, 1.5f);

            for (int i = 0; i < cloth.ParticleCount; i++)
            {
                if (cloth.IsParticlePinned(i))
                    Assert.InRange(cloth.GetParticlePosition(i).Y, 1.9f, 2.1f);
            }
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

    /// <summary> A teleported body is found where it now is by a ray cast before the next step, as a rigid body is </summary>
    [Fact]
    public static void ATeleportedBodyIsWhereRaysLookForIt()
    {
        RunScene(async scene =>
        {
            var body = new VolumetricSoftBodyComponent { Resolution = 3, Gravity = false };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 2f, 0f));
            await body.Simulation!.AfterUpdate();

            // Above the floor of the scene, which the ray reaches if it misses the body
            body.Teleport(new Vector3(8f, 2f, 0f), Quaternion.Identity);
            Assert.True(body.Simulation!.RayCast(new Vector3(8f, 6f, 0f), -Vector3.UnitY, 10f, out var hit));
            Assert.Same(body, hit.Collidable);
        });
    }

    /// <summary> Ray hits and contacts say which particle of the soft body they are on, through their child index </summary>
    [Fact]
    public static void RaysAndContactsReportTheParticle()
    {
        RunScene(async scene =>
        {
            var events = new ParticleHandler();
            var body = new VolumetricSoftBodyComponent { Resolution = 3, ContactEventHandler = events, SleepThreshold = 0f };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));
            await Seconds(body, 1.5f);

            // A ray stops on the sphere of the particle it reports, cast through the simulation or at the body alone
            float radius = ((ParticleCollider)body.Collider).ActualRadius;
            Assert.True(body.Simulation!.RayCast(new Vector3(0.1f, 5f, 0.2f), -Vector3.UnitY, 10f, out var hit));
            Assert.Equal(radius, Vector3.Distance(body.GetParticlePosition(hit.ChildIndex), hit.Point), 3);
            Assert.True(body.RayCast(new Vector3(5f, 0.3f, 0.1f), -Vector3.UnitX, 10f, out hit));
            Assert.Equal(radius, Vector3.Distance(body.GetParticlePosition(hit.ChildIndex), hit.Point), 3);
            var hits = new System.Collections.Generic.List<HitInfo>();
            CollectThroughABuffer(body, new Vector3(5f, 0.3f, 0.1f), hits);
            Assert.NotEmpty(hits);
            Assert.All(hits, each => Assert.Equal(radius, Vector3.Distance(body.GetParticlePosition(each.ChildIndex), each.Point), 3));

            // The floor is touched by the bottom layer of the lattice, by more than one of its particles
            events.Particles.Clear();
            await body.Simulation!.AfterUpdate();
            Assert.True(events.Particles.Count > 1, $"{events.Particles.Count} particles in contact");
            Assert.All(events.Particles, particle => Assert.True(body.GetParticlePosition(particle).Y < radius * 1.5f, $"Particle {particle} at {body.GetParticlePosition(particle)}"));
        });

        static void CollectThroughABuffer(SoftBodyComponent body, Vector3 origin, System.Collections.Generic.List<HitInfo> hits)
        {
            Span<HitInfoStack> buffer = stackalloc HitInfoStack[16];
            foreach (var hit in body.RayCastPenetrating(origin, -Vector3.UnitX, 10f, buffer))
                hits.Add(hit);
        }
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
            // Resolution 10 gives 11^3 = 1331 particles, whose pairs the narrow phase hands to the four workers asked for
            var body = new VolumetricSoftBodyComponent { Resolution = 10, ContactEventHandler = events, SleepThreshold = 0f };
            scene.Add(body, new CubeProceduralModel(), new Vector3(0f, 1f, 0f));
            body.Simulation!.ThreadCount = 4;

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
    /// A plate lying on a soft body touches many of its particles, more contacts than the solver has batches for: the rest goes
    /// to its fallback batch, whose slots are not all filled. The contact recorder of the debug view goes through every batch after each step.
    /// </summary>
    [Fact]
    public static void DebugContactRecorderGoesThroughTheFallbackBatch()
    {
        RunScene(async scene =>
        {
            var lower = new VolumetricSoftBodyComponent { Resolution = 9, SleepThreshold = 0f };
            var plate = new BodyComponent { SleepThreshold = 0f, Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(1.4f, 0.1f, 1.4f) } } } };
            scene.Add(lower, new CubeProceduralModel(), new Vector3(0f, 0.6f, 0f));
            scene.Add(plate, new Vector3(0f, 1.3f, 0f));

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

    /// <summary> The edge at x = -0.5 of <see cref="ClothModel"/> </summary>
    private static BoundingBox PinnedEdge => new(new Vector3(-0.51f, -1f, -1f), new Vector3(-0.49f, 1f, 1f));

    /// <summary> A square of side 1 on XZ, eleven particles along each side </summary>
    private static PlaneProceduralModel ClothModel => new() { Size = new Vector2(1f), Tessellation = new Int2(10), Normal = NormalDirection.UpY, GenerateBackFace = true };

    private static ModelNodeDefinition Node(string name, int parent, Vector3 position)
    {
        return new ModelNodeDefinition { Name = name, ParentIndex = parent, Flags = ModelNodeFlags.Default, Transform = new TransformTRS { Position = position, Rotation = Quaternion.Identity, Scale = Vector3.One } };
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

    private sealed class ParticleHandler : IContactHandler
    {
        public bool NoContactResponse => false;
        public readonly System.Collections.Generic.HashSet<int> Particles = new();

        public void OnTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            foreach (var contact in contacts)
                Particles.Add(contact.SourceChildIndex);
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

        public void Add(Entity entity) => game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);

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
