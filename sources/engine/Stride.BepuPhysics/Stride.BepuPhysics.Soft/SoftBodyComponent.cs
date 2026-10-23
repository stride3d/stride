// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Soft.Colliders;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// A deformable body made of particles held together by springs, its shape and look come from the <see cref="ModelComponent"/> on the same entity.
/// </summary>
/// <remarks>
/// The model is replaced at runtime by a copy whose vertices follow the particles, the entity's transform follows the overall motion of the body.
/// Each particle is a body of the simulation, raycasts, overlaps and contact events report this component for any of them.
/// </remarks>
[DataContract(Inherited = true)]
[DefaultEntityComponentProcessor(typeof(SoftBodyProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Soft")]
public abstract class SoftBodyComponent : CollidableComponent
{
    /// <summary> The category of the soft body settings in the property grid </summary>
    public const string CategorySoftBody = "Soft Body";

    private static readonly ConditionalWeakTable<Model, SourceMesh> SourceMeshes = new();
    private static readonly Logger Logger = GlobalLogger.GetLogger(nameof(SoftBodyComponent));
    private static int _nextGroupId = ushort.MaxValue;

    private float _mass = 1f;
    private bool _gravity = true;
    private bool _selfCollision = true;
    private float _sleepThreshold = 0.01f;

    private BodyHandle[] _particles = [];
    private bool[] _pinned = [];
    private (int Particle, SoftBodyPin Pin, Vector3 Offset)[] _pinTargets = [];
    private Vector3[] _restOffsets = [];
    private Vector3 _restCentroid;
    private Quaternion _rotation = Quaternion.Identity;
    private ushort _groupId;
    private int _islandProbe; // A particle which is not pinned, kinematic bodies are not part of the island
    private StepListener? _stepListener;

    [SetsRequiredMembers]
    protected SoftBodyComponent()
    {
        Collider = new ParticleCollider();
    }

    /// <summary>
    /// The mass of the whole body, spread evenly over its particles.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [DefaultValue(1f)]
    public float Mass
    {
        get => _mass;
        set
        {
            _mass = MathF.Max(1e-6f, value);
            TryUpdateFeatures();
        }
    }

    /// <inheritdoc cref="BodyComponent.Gravity"/>
    [Display(category: CategorySoftBody)]
    [DefaultValue(true)]
    public bool Gravity
    {
        get => _gravity;
        set
        {
            _gravity = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// Whether parts of this body collide with each other when it folds onto itself, neighboring particles never do.
    /// </summary>
    /// <remarks>
    /// Particles are told apart through <see cref="CollidableComponent.CollisionGroup"/>: its indices are replaced by the position of each particle,
    /// and when its id is zero this body picks an id of its own, counting down from 65535.
    /// </remarks>
    [Display(category: CategorySoftBody)]
    [DefaultValue(true)]
    public bool SelfCollision
    {
        get => _selfCollision;
        set
        {
            _selfCollision = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <inheritdoc cref="BodyComponent.SleepThreshold"/>
    [Display(category: CategoryActivity)]
    [DefaultValue(0.01f)]
    public float SleepThreshold
    {
        get => _sleepThreshold;
        set
        {
            _sleepThreshold = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// Particles held in place or following other entities, changes are applied when the body is created.
    /// </summary>
    [Display(category: CategorySoftBody)]
    public List<SoftBodyPin> Pins { get; } = new();

    /// <summary> The amount of particles, zero until the body is part of a simulation </summary>
    [DataMemberIgnore]
    public int ParticleCount => _particles.Length;

    /// <summary> Whether the particles are being simulated, a body at rest falls asleep after a while </summary>
    [DataMemberIgnore]
    public bool Awake
    {
        get => _particles.Length > 0 && Simulation is not null && Particle(_islandProbe).Awake;
        set
        {
            if (_particles.Length > 0 && Simulation is not null)
            {
                var body = Particle(_islandProbe); // Every particle is in the same island
                body.Awake = value;
            }
        }
    }

    /// <summary> The average velocity of the particles, setting it changes the velocity of every particle by the same amount </summary>
    [DataMemberIgnore]
    public Vector3 LinearVelocity
    {
        get
        {
            if (_particles.Length == 0 || Simulation is null)
                return default;

            var sum = System.Numerics.Vector3.Zero;
            foreach (var handle in _particles)
                sum += Simulation.Simulation.Bodies[handle].Velocity.Linear;
            return (sum / _particles.Length).ToStride();
        }
        set
        {
            var delta = (value - LinearVelocity).ToNumeric();
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_pinned[i] == false)
                {
                    var body = Particle(i);
                    body.Velocity.Linear += delta;
                }
            }
            Awake = true;
        }
    }

    /// <summary> The model of the <see cref="ModelComponent"/> this body was built from </summary>
    [DataMemberIgnore]
    public Model? SourceModel { get; private set; }

    internal SoftBodyTopology? Topology { get; private set; }

    internal DeformableModel? RenderModel { get; set; }

    /// <summary> The particle radius when <see cref="ParticleCollider.Radius"/> is zero, as a fraction of the spacing between particles </summary>
    internal abstract float DefaultRadiusFraction { get; }

    internal override int CollidableCount => Simulation is null ? 0 : _particles.Length;

    internal override CollidableReference GetCollidableReference(int index) => Particle(index).CollidableReference;

    internal override bool ShouldCalculateInertia => false;

    protected internal override CollidableReference? CollidableReference => _particles.Length > 0 && Simulation is not null ? Particle(_islandProbe).CollidableReference : null;

    protected override ref MaterialProperties MaterialProperties => ref Simulation!.CollidableMaterials[_particles[0]];

    protected internal override NRigidPose? Pose
    {
        get
        {
            if (_particles.Length == 0 || Simulation is null)
                return null;
            return new NRigidPose(ComputeCentroid().ToNumeric(), _rotation.ToNumeric());
        }
    }

    /// <summary> The current position of a particle in world space </summary>
    /// <param name="particle"> Index of the particle, below <see cref="ParticleCount"/> </param>
    public Vector3 GetParticlePosition(int particle) => Particle(particle).Pose.Position.ToStride();

    /// <summary> The current velocity of a particle </summary>
    /// <param name="particle"> Index of the particle, below <see cref="ParticleCount"/> </param>
    public Vector3 GetParticleVelocity(int particle) => Particle(particle).Velocity.Linear.ToStride();

    /// <summary> Replaces the velocity of a particle, pinned particles keep following their pin </summary>
    /// <param name="particle"> Index of the particle, below <see cref="ParticleCount"/> </param>
    /// <param name="velocity"> The new velocity, in world space </param>
    public void SetParticleVelocity(int particle, Vector3 velocity)
    {
        if (_pinned[particle])
            return;
        var body = Particle(particle);
        body.Velocity.Linear = velocity.ToNumeric();
        body.Awake = true;
    }

    /// <summary> Whether a particle is held by one of the <see cref="Pins"/> </summary>
    /// <param name="particle"> Index of the particle, below <see cref="ParticleCount"/> </param>
    public bool IsParticlePinned(int particle) => _pinned[particle];

    /// <summary> Changes the velocity of a single particle by <paramref name="impulse"/> divided by its mass </summary>
    /// <param name="particle"> Index of the particle, below <see cref="ParticleCount"/> </param>
    /// <param name="impulse"> The impulse, in world space </param>
    public void ApplyParticleImpulse(int particle, Vector3 impulse)
    {
        var body = Particle(particle);
        body.ApplyLinearImpulse(impulse.ToNumeric());
        body.Awake = true;
    }

    /// <summary> Spreads <paramref name="impulse"/> over every particle, the whole body accelerates without deforming </summary>
    /// <param name="impulse"> The impulse, in world space </param>
    public void ApplyLinearImpulse(Vector3 impulse)
    {
        LinearVelocity += impulse / _mass;
    }

    /// <summary> Moves the body to a new pose in its rest shape, at rest </summary>
    /// <remarks> Pinned particles move with it for this step, then go back to their pins </remarks>
    /// <param name="position"> The new world position of the entity </param>
    /// <param name="orientation"> The new world orientation of the entity </param>
    public void Teleport(Vector3 position, Quaternion orientation)
    {
        if (Simulation is null)
            return;

        var scale = WorldScale();
        for (int i = 0; i < _particles.Length; i++)
        {
            var body = Particle(i);
            body.Pose = new NRigidPose((position + Vector3.Transform(Topology!.RestPositions[i] * scale, orientation)).ToNumeric());
            body.Velocity = default;
        }
        _rotation = orientation;
        UpdateTransformationComponent(position, orientation);
        Awake = true;
    }

    internal abstract SoftBodyTopology BuildTopology(SourceMesh mesh);

    internal abstract void AddConstraints(global::BepuPhysics.Simulation simulation, ReadOnlySpan<BodyHandle> particles, ReadOnlySpan<bool> pinned, SoftBodyTopology topology);

    /// <summary> Called before each simulation step </summary>
    internal virtual void BeforeStep(float deltaTime) { }

    internal BodyReference Particle(int index) => Simulation!.Simulation.Bodies[_particles[index]];

    /// <summary> Builds the topology for the current model if it changed, false when this body cannot be created yet </summary>
    internal bool TryPrepareTopology(out float spacing)
    {
        spacing = 0f;
        var services = Entity?.EntityManager?.Services;
        if (Entity?.Get<ModelComponent>()?.Model is not { } model || services is null)
            return false;

        model = DeformableModel.SourceOf(model);
        if (Topology is null || ReferenceEquals(model, SourceModel) == false)
        {
            try
            {
                var mesh = SourceMeshes.GetValue(model, m => SourceMesh.FromModel(m, services));
                var topology = BuildTopology(mesh);
                if (topology.ParticleCount == 0)
                    throw new InvalidOperationException("The model has no vertices");
                Topology = topology;
                SourceModel = model;
            }
            catch (Exception e) when (e is InvalidOperationException or NotSupportedException)
            {
                Logger.Error($"{GetType().Name} on '{Entity.Name}' cannot be built from its model: {e.Message}");
                return false;
            }
        }

        spacing = Topology.Spacing * WorldScale().Length() / MathF.Sqrt(3f);
        return true;
    }

    /// <summary> Throws away the topology, it is rebuilt the next time this body is created </summary>
    internal void InvalidateTopology()
    {
        Topology = null;
        TryUpdateFeatures();
    }

    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        Debug.Assert(Simulation is not null);

        // Done by the ParticleCollider, a collider of another kind gives every particle its own shape instead
        if (Topology is null && TryPrepareTopology(out _) == false)
            throw new InvalidOperationException($"{GetType().Name} on '{Entity?.Name}' needs a {nameof(ModelComponent)} with a model on the same entity");
        var topology = Topology!;
        var bodies = Simulation.Simulation.Bodies;

        var world = Entity.Transform.WorldMatrix;
        world.Decompose(out var scale, out Quaternion rotation, out _);
        _rotation = rotation;

        int count = topology.ParticleCount;
        _particles = new BodyHandle[count];
        _pinned = new bool[count];
        _restOffsets = new Vector3[count];
        _restCentroid = Vector3.Zero;
        foreach (var rest in topology.RestPositions)
            _restCentroid += rest;
        _restCentroid /= count;

        var pinTargets = new List<(int, SoftBodyPin, Vector3)>();
        for (int i = 0; i < count; i++)
        {
            var rest = topology.RestPositions[i];
            foreach (var pin in Pins)
            {
                var region = pin.Region;
                if (region.Contains(ref rest) != ContainmentType.Disjoint)
                {
                    _pinned[i] = true;
                    var worldPosition = Vector3.TransformCoordinate(topology.RestPositions[i], world);
                    var offset = pin.Anchor is { } anchor ? Vector3.TransformCoordinate(worldPosition, Matrix.Invert(anchor.Transform.WorldMatrix)) : worldPosition;
                    pinTargets.Add((i, pin, offset));
                    break;
                }
            }
        }
        _pinTargets = pinTargets.ToArray();
        _islandProbe = Math.Max(0, Array.IndexOf(_pinned, false));

        // Particles do not rotate, they only carry mass
        var inertia = new BodyInertia { InverseMass = count / _mass };
        var activity = new BodyActivityDescription(_sleepThreshold);
        var collidable = new CollidableDescription(shapeIndex);
        for (int i = 0; i < count; i++)
        {
            _restOffsets[i] = (topology.RestPositions[i] - _restCentroid) * scale;
            var position = Vector3.TransformCoordinate(topology.RestPositions[i], world).ToNumeric();
            var description = BodyDescription.CreateDynamic(new NRigidPose(position), _pinned[i] ? default : inertia, collidable, activity);
            var handle = bodies.Add(description);
            _particles[i] = handle;
            Simulation.SetBodyOwner(handle, this);
            Simulation.CollidableMaterials.Allocate(handle) = new();
        }

        AddConstraints(Simulation.Simulation, _particles, _pinned, topology);

        _stepListener = new StepListener(this);
        Simulation.Register(_stepListener);
    }

    protected override void DetachInner()
    {
        Debug.Assert(Simulation is not null);

        if (_stepListener is not null)
        {
            Simulation.Unregister(_stepListener);
            _stepListener = null;
        }

        foreach (var handle in _particles)
        {
            Simulation.Simulation.Bodies.Remove(handle); // Also removes the constraints between particles
            Simulation.SetBodyOwner(handle, null);
        }

        _particles = [];
        _pinned = [];
        _pinTargets = [];
    }

    internal override void OnMaterialPropertiesUpdated()
    {
        var materials = Simulation!.CollidableMaterials;
        var material = materials[_particles[0]];
        material.Gravity = _gravity;

        // Particles closer than a step on every axis share an id and never collide, see StrideNarrowPhaseCallbacks
        if (material.CollisionGroup.Id == 0)
        {
            if (_groupId == 0)
                _groupId = (ushort)Math.Max(1, Interlocked.Decrement(ref _nextGroupId) & ushort.MaxValue);
            material.CollisionGroup.Id = _groupId;
        }

        var cells = Topology!.SelfCollisionCells;
        for (int i = 0; i < _particles.Length; i++)
        {
            if (_selfCollision)
            {
                material.CollisionGroup.IndexA = (ushort)Math.Clamp(cells[i].X, 0, ushort.MaxValue);
                material.CollisionGroup.IndexB = (ushort)Math.Clamp(cells[i].Y, 0, ushort.MaxValue);
                material.CollisionGroup.IndexC = (ushort)Math.Clamp(cells[i].Z, 0, ushort.MaxValue);
            }
            materials[_particles[i]] = material;
        }
    }

    internal override void RayTest<TRayHitHandler>(in Vector3 origin, in Vector3 dir, ref float maximumT, ref TRayHitHandler hitHandler)
    {
        if (ShapeIndex.Exists == false || Simulation is null)
            return;

        var ray = new global::BepuPhysics.Trees.RayData { Origin = origin.ToNumeric(), Direction = dir.ToNumeric() };
        var shapes = Simulation.Simulation.Shapes;
        for (int i = 0; i < _particles.Length; i++)
            Collider.RayTest(shapes, ShapeIndex, Particle(i).Pose, ray, ref maximumT, ref hitHandler, Simulation.BufferPool);
    }

    /// <summary> Reads the current position of every particle </summary>
    internal void CopyParticlePositions(Span<Vector3> positions)
    {
        var bodies = Simulation!.Simulation.Bodies;
        for (int i = 0; i < _particles.Length; i++)
            positions[i] = bodies[_particles[i]].Pose.Position.ToStride();
    }

    internal static SpringSettings Spring(float frequency, float dampingRatio) => new(MathF.Max(1e-3f, frequency), MathF.Max(0f, dampingRatio));

    private Vector3 ComputeCentroid()
    {
        var sum = System.Numerics.Vector3.Zero;
        var bodies = Simulation!.Simulation.Bodies;
        foreach (var handle in _particles)
            sum += bodies[handle].Pose.Position;
        return (sum / _particles.Length).ToStride();
    }

    private Vector3 WorldScale()
    {
        Entity.Transform.UpdateWorldMatrix();
        Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion _, out _);
        return scale;
    }

    private void DrivePins(float deltaTime)
    {
        foreach (var (particle, pin, offset) in _pinTargets)
        {
            var target = pin.Anchor is { } anchor ? Vector3.TransformCoordinate(offset, anchor.Transform.WorldMatrix) : offset;
            var body = Particle(particle);
            var velocity = (target - body.Pose.Position.ToStride()) / deltaTime;
            body.Velocity.Linear = velocity.ToNumeric();
            if (velocity != Vector3.Zero)
                body.Awake = true;
        }
    }

    /// <summary> Moves the entity with the body: its origin follows the centroid of the particles, its rotation the best fit of their rest shape </summary>
    private void SyncTransform()
    {
        var centroid = ComputeCentroid();
        Vector3 column0 = default, column1 = default, column2 = default;
        var bodies = Simulation!.Simulation.Bodies;
        for (int i = 0; i < _particles.Length; i++)
        {
            var current = bodies[_particles[i]].Pose.Position.ToStride() - centroid;
            var rest = _restOffsets[i];
            column0 += current * rest.X;
            column1 += current * rest.Y;
            column2 += current * rest.Z;
        }

        _rotation = ShapeMatching.ExtractRotation(column0, column1, column2, _rotation);
        var scaledCentroid = _restCentroid * WorldScale();
        UpdateTransformationComponent(centroid - Vector3.Transform(scaledCentroid, _rotation), _rotation);
    }

    private sealed class StepListener(SoftBodyComponent owner) : ISimulationUpdate
    {
        public Entity Entity => owner.Entity;

        public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            if (owner._pinTargets.Length > 0)
                owner.DrivePins(simTimeStep);
            owner.BeforeStep(simTimeStep);
        }

        public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            if (owner._particles.Length > 0 && owner.Particle(owner._islandProbe).Awake)
                owner.SyncTransform();
        }
    }
}
