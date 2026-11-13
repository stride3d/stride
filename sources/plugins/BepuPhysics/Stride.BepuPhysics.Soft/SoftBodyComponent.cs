// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Soft.Colliders;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core;
using Stride.Core.Annotations;
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
/// Each particle is a body of the simulation, raycasts, overlaps and contact events report this component for any of them,
/// ray hits and contacts with the index of the particle as their child index.
/// </remarks>
[DataContract(Inherited = true)]
[DefaultEntityComponentProcessor(typeof(SoftBodyProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Soft")]
[CategoryOrder(1, CategorySoftBody, Expand = ExpandRule.Always)]
public abstract class SoftBodyComponent : CollidableComponent, IDebugLineSource
{
    /// <summary> The category of the soft body settings in the property grid </summary>
    public const string CategorySoftBody = "Soft Body";

    private static readonly ConditionalWeakTable<Model, SourceMesh> SourceMeshes = new();
    private static readonly Logger Logger = GlobalLogger.GetLogger(nameof(SoftBodyComponent));
    private static readonly Stack<ushort> FreeGroupIds = new(); // Given back by the bodies that left their simulation, also the lock of the ids
    private static int _nextGroupId = ushort.MaxValue;
    private const int SoftAnchorParticles = 3; // Particles of a soft body anchor each pinned particle keeps its distance to
    private const int MaximumJoinedParticles = 32; // Particles of a pin joined to the bodies of its anchor, each one a constraint on them

    private float _mass = 1f;
    private bool _gravity = true;
    private bool _selfCollision = true;
    private float _sleepThreshold = 0.01f;

    private BodyHandle[] _particles = [];
    private readonly Dictionary<int, int> _particleOfBody = new(); // By body handle
    private bool[] _pinned = [];
    private WorldPin[] _worldPins = [];
    private BodyPin[] _bodyPins = [];
    private Entity[] _nodeAnchors = []; // Anchors whose node matrices are refreshed before each step
    private SkeletonUpdater?[] _nodeSkeletons = []; // The skeleton of each of them, as of the last refresh
    private Entity?[] _anchorOfParticle = []; // The anchor entity whose collidables a pinned particle does not touch
    private Vector3[] _restOffsets = [];
    private Vector3 _restCentroid;
    private Quaternion _rotation = Quaternion.Identity;
    private ushort _groupId;
    private int[] _islandProbes = [0]; // A particle of each piece of the body
    private StepListener? _stepListener;

    [SetsRequiredMembers]
    protected SoftBodyComponent()
    {
        Collider = new ParticleCollider();
    }

    /// <summary>
    /// The collider of the particles, which the soft body makes for itself. Their radius is <see cref="ParticleRadius"/>.
    /// </summary>
    /// <exception cref="NotSupportedException"> A soft body cannot be given another collider. </exception>
    [DataMemberIgnore]
    public override required ICollider Collider
    {
        get => base.Collider;
        set
        {
            if (value is not ParticleCollider)
                throw new NotSupportedException($"{GetType().Name} makes the collider of its particles itself, it cannot be given a {value?.GetType().Name}; the radius of the particles is {nameof(ParticleRadius)}");
            base.Collider = value;
        }
    }

    /// <summary>
    /// Not used: a soft body tells its particles apart with collision groups of its own.
    /// Use <see cref="CollidableComponent.CollisionLayer"/> to have it ignore other collidables.
    /// </summary>
    [DataMemberIgnore]
    public new CollisionGroup CollisionGroup => default;

    /// <summary>
    /// The radius of each particle, zero picks one from the spacing between particles.
    /// </summary>
    /// <remarks>
    /// Larger particles close the gaps other objects could slip through, but keep them further from the surface.
    /// </remarks>
    [Display(category: CategorySoftBody)]
    [DataMemberRange(minimum: 0, decimalPlaces: 3)]
    [DefaultValue(0f)]
    public float ParticleRadius
    {
        get => (Collider as ParticleCollider)?.Radius ?? 0f;
        set { if (Collider is ParticleCollider p) p.Radius = value; }
    }

    /// <summary>
    /// The mass of the whole body, spread evenly over its particles.
    /// </summary>
    /// <remarks>
    /// The solver converges poorly when light particles carry a much heavier body: the soft body gives way past its limits,
    /// and more iterations do not help much. Give it a mass closer to what it carries, or fewer particles.
    /// </remarks>
    [Display(category: CategorySoftBody)]
    [DataMemberRange(minimum: 0.001, decimalPlaces: 3)]
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
    /// Particles are told apart through collision groups of their own: each soft body takes an id no other one has, counting down from 65534,
    /// and the indices of a particle are its place in the body. Keep those ids free on other collidables,
    /// and use <see cref="CollidableComponent.CollisionLayer"/> to have this body ignore another collidable.
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
    [DataMemberRange(minimum: 0, decimalPlaces: 3)]
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
    /// Particles held in place or tied to other entities, changes are applied when the body is created.
    /// </summary>
    [Display(category: CategorySoftBody)]
    [MemberCollection(NotNullItems = true)]
    public List<SoftBodyPin> Pins { get; } = new();

    /// <summary> The amount of particles, zero until the body is part of a simulation </summary>
    [DataMemberIgnore]
    public int ParticleCount => _particles.Length;

    /// <summary> Whether the particles are being simulated, a body at rest falls asleep after a while </summary>
    [DataMemberIgnore]
    public bool Awake
    {
        get
        {
            if (_particles.Length == 0 || Simulation is null)
                return false;

            // The particles of a piece are in the same island
            foreach (int probe in _islandProbes)
            {
                if (Particle(probe).Awake)
                    return true;
            }
            return false;
        }
        set
        {
            if (_particles.Length == 0 || Simulation is null)
                return;

            foreach (int probe in _islandProbes)
            {
                var body = Particle(probe);
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

    /// <summary> Hits and contacts report the particle they are on </summary>
    internal override int GetChildIndex(CollidableReference collidable, int shapeChildIndex)
    {
        return collidable.Mobility != CollidableMobility.Static && _particleOfBody.TryGetValue(collidable.BodyHandle.Value, out int particle) ? particle : shapeChildIndex;
    }

    internal override bool ShouldCalculateInertia => false;

    protected internal override CollidableReference? CollidableReference => _particles.Length > 0 && Simulation is not null ? Particle(_islandProbes[0]).CollidableReference : null;

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
    /// <remarks> Pinned particles move with it, then their anchors pull them back </remarks>
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

    internal abstract void AddConstraints(global::BepuPhysics.Simulation simulation, ReadOnlySpan<BodyHandle> particles, SoftBodyTopology topology);

    /// <summary> Called before each simulation step </summary>
    internal virtual void BeforeStep(float deltaTime) { }

    internal BodyReference Particle(int index) => Simulation!.Simulation.Bodies[_particles[index]];

    /// <summary> Adds a cross on every particle and a line along every spring, as the <see cref="SoftBodyConfiguration"/> asks for them </summary>
    /// <param name="lines"> Receives the lines, in world space </param>
    public void AddDebugLines(IDebugLineCollector lines)
    {
        if (_particles.Length == 0 || Simulation is null || Topology is not { } topology)
            return;

        var settings = Entity.EntityManager.Services.GetOrCreate<SoftBodyConfiguration>();
        if (settings.ShowDebugParticles == false && settings.ShowDebugSprings == false)
            return;

        var positions = ArrayPool<Vector3>.Shared.Rent(_particles.Length);
        CopyParticlePositions(positions);

        if (settings.ShowDebugParticles)
        {
            float size = settings.DebugParticleSize;
            Vector3 x = new(size, 0f, 0f), y = new(0f, size, 0f), z = new(0f, 0f, size);
            foreach (var position in positions.AsSpan(0, _particles.Length))
            {
                lines.AddLine(position - x, position + x, Color.Red);
                lines.AddLine(position - y, position + y, Color.Red);
                lines.AddLine(position - z, position + z, Color.Red);
            }
        }

        if (settings.ShowDebugSprings)
        {
            foreach (var edge in topology.Edges)
                lines.AddLine(positions[edge.A], positions[edge.B], edge.Kind switch { EdgeKind.Shear => Color.Cyan, EdgeKind.Bend => Color.Yellow, _ => Color.White });
        }

        ArrayPool<Vector3>.Shared.Return(positions);
    }

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

        Debug.Assert(Topology is not null, "Built by ParticleCollider.TryAttach");
        var topology = Topology;
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

        var anchorNodes = ResolveAnchorNodes();
        var pinned = SelectPinnedParticles(topology, world, anchorNodes);
        _islandProbes = FindIslandProbes(topology.Edges, count);

        // Particles do not rotate, they only carry mass; pinned ones too, so they pull on what they are tied to
        var inertia = new BodyInertia { InverseMass = count / _mass };
        var activity = new BodyActivityDescription(_sleepThreshold);
        var collidable = new CollidableDescription(shapeIndex);
        for (int i = 0; i < count; i++)
        {
            _restOffsets[i] = (topology.RestPositions[i] - _restCentroid) * scale;
            var position = Vector3.TransformCoordinate(topology.RestPositions[i], world).ToNumeric();
            var description = BodyDescription.CreateDynamic(new NRigidPose(position), inertia, collidable, activity);
            var handle = bodies.Add(description);
            _particles[i] = handle;
            _particleOfBody[handle.Value] = i;
            Simulation.SetBodyOwner(handle, this);
            Simulation.CollidableMaterials.Allocate(handle) = new();
        }

        AddConstraints(Simulation.Simulation, _particles, topology);
        AddPins(pinned, anchorNodes);

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
            Simulation.Simulation.Bodies.Remove(handle); // Also removes the constraints between particles, and the ones of the pins
            Simulation.SetBodyOwner(handle, null);
        }

        _particles = [];
        _particleOfBody.Clear();
        _pinned = [];
        _worldPins = [];
        _bodyPins = [];
        _nodeAnchors = [];
        _nodeSkeletons = [];
        _anchorOfParticle = [];
        FiltersContactsWithOthers = false;
        ReleaseGroupId(ref _groupId);
    }

    /// <summary> Pinned particles do not touch the collidables of the entity they are tied to </summary>
    internal override bool AllowContactWith(BodyHandle body, CollidableComponent other)
    {
        return _particleOfBody.TryGetValue(body.Value, out int particle) == false || ReferenceEquals(_anchorOfParticle[particle], other.Entity) == false;
    }

    internal override void OnMaterialPropertiesUpdated()
    {
        var materials = Simulation!.CollidableMaterials;
        var material = materials[_particles[0]];
        material.Gravity = _gravity;

        // Particles closer than a step on every axis share an id and never collide, see StrideNarrowPhaseCallbacks
        if (_groupId == 0)
            _groupId = AcquireGroupId();
        material.CollisionGroup = new CollisionGroup { Id = _groupId };

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
        {
            var handler = new ParticleRayHitHandler<TRayHitHandler>(hitHandler, i);
            Collider.RayTest(shapes, ShapeIndex, Particle(i).Pose, ray, ref maximumT, ref handler, Simulation.BufferPool);
            hitHandler = handler.Inner;
        }
    }

    /// <summary> Reads the current position of every particle </summary>
    internal void CopyParticlePositions(Span<Vector3> positions)
    {
        var bodies = Simulation!.Simulation.Bodies;
        for (int i = 0; i < _particles.Length; i++)
            positions[i] = bodies[_particles[i]].Pose.Position.ToStride();
    }

    internal static SpringSettings Spring(float frequency, float dampingRatio) => new(MathF.Max(1e-3f, frequency), MathF.Max(0f, dampingRatio));

    /// <summary> An id for the collision group of the particles of a body, which no other soft body holds </summary>
    private static ushort AcquireGroupId()
    {
        lock (FreeGroupIds)
        {
            if (FreeGroupIds.TryPop(out ushort id))
                return id;

            // Past 65534 soft bodies at once the ids start over
            if (--_nextGroupId < 1)
                _nextGroupId = ushort.MaxValue - 1;
            return (ushort)_nextGroupId;
        }
    }

    private static void ReleaseGroupId(ref ushort id)
    {
        if (id == 0)
            return;

        lock (FreeGroupIds)
            FreeGroupIds.Push(id);
        id = 0;
    }

    /// <summary> Picks a particle in each group of particles that the edges link </summary>
    private static int[] FindIslandProbes(ReadOnlySpan<Edge> edges, int particleCount)
    {
        var group = new int[particleCount];
        for (int i = 0; i < group.Length; i++)
            group[i] = i;

        foreach (var edge in edges)
        {
            int a = GroupOf(group, edge.A), b = GroupOf(group, edge.B);
            group[Math.Max(a, b)] = Math.Min(a, b);
        }

        var probes = new List<int>();
        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == i)
                probes.Add(i);
        }
        return probes.ToArray();

        static int GroupOf(int[] group, int particle)
        {
            while (group[particle] != particle)
                particle = group[particle] = group[group[particle]];
            return particle;
        }
    }

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

    /// <summary> Per pin, the index of the anchor node it follows, or -1 for the anchor entity itself; refreshes the anchors' node matrices </summary>
    private int[] ResolveAnchorNodes()
    {
        var nodes = new int[Pins.Count];
        var anchors = new List<Entity>();
        for (int p = 0; p < Pins.Count; p++)
        {
            nodes[p] = -1;
            var pin = Pins[p];
            if (pin.Anchor?.AnchorEntity is not { } anchor)
                continue;
            anchor.Transform.UpdateWorldMatrix(); // It may have been added or moved this frame, before the transform processor ran
            if (string.IsNullOrEmpty(pin.Anchor.NodeName))
                continue;

            if (ModelNodes.Refresh(anchor) is { } skeleton && ModelNodes.Find(skeleton, pin.Anchor.NodeName) is var node and >= 0)
            {
                nodes[p] = node;
                if (anchors.Contains(anchor) == false)
                    anchors.Add(anchor);
            }
            else
            {
                Logger.Warning($"A pin of {GetType().Name} on '{Entity?.Name}' follows the node '{pin.Anchor.NodeName}', which the model of '{anchor.Name}' does not have; it follows the entity instead");
            }
        }
        _nodeAnchors = anchors.ToArray();
        _nodeSkeletons = new SkeletonUpdater?[_nodeAnchors.Length];
        return nodes;
    }

    /// <summary> The particles of each pin, null for a pin holding none; a particle belongs to the first pin holding it </summary>
    private List<int>?[] SelectPinnedParticles(SoftBodyTopology topology, in Matrix world, int[] anchorNodes)
    {
        var pinned = new List<int>?[Pins.Count];
        var rests = topology.RestPositions;
        for (int p = 0; p < Pins.Count; p++)
        {
            var region = Pins[p].Region;
            if (region.Minimum != region.Maximum)
            {
                for (int i = 0; i < rests.Length; i++)
                {
                    if (_pinned[i] == false && region.Contains(ref rests[i]) != ContainmentType.Disjoint)
                        Pin(p, i);
                }
                continue;
            }

            // A point: the particles closest to it, those of a whole cell or edge when it lies between them
            var point = region.Minimum;
            if (point == Vector3.Zero && Pins[p].Anchor?.AnchorEntity is { } anchor)
                point = Vector3.TransformCoordinate(AnchorMatrix(anchor, anchorNodes[p]).TranslationVector, Matrix.Invert(world));
            float nearest = float.MaxValue;
            for (int i = 0; i < rests.Length; i++)
            {
                if (_pinned[i] == false)
                    nearest = MathF.Min(nearest, Vector3.Distance(rests[i], point));
            }
            float reach = nearest + topology.Spacing * 0.5f;
            for (int i = 0; i < rests.Length; i++)
            {
                if (_pinned[i] == false && Vector3.Distance(rests[i], point) <= reach)
                    Pin(p, i);
            }
        }

        for (int p = 0; p < Pins.Count; p++)
        {
            if (pinned[p] is null)
                Logger.Warning($"Pin {p} of {GetType().Name} on '{Entity?.Name}' holds no particle: its region is outside of the model, or taken by an earlier pin");
        }
        return pinned;

        void Pin(int pin, int particle)
        {
            _pinned[particle] = true;
            (pinned[pin] ??= new()).Add(particle);
        }
    }

    /// <summary> Ties the particles of each pin to its anchor, <paramref name="pinned"/> holding the particles of each pin </summary>
    private void AddPins(List<int>?[] pinned, int[] anchorNodes)
    {
        var solver = Simulation!.Simulation.Solver;
        var worldPins = new List<WorldPin>();
        var bodyPins = new List<BodyPin>();
        _anchorOfParticle = new Entity?[_particles.Length];
        bool filtersContacts = false;
        for (int p = 0; p < Pins.Count; p++)
        {
            if (pinned[p] is not { } particles)
                continue;

            var pin = Pins[p];
            var anchor = pin.Anchor?.AnchorEntity;
            if (pin.Anchor is BodyAnchor bodyAnchor)
            {
                if (ReferenceEquals(bodyAnchor.Body, this))
                {
                    Logger.Error($"A pin of {GetType().Name} on '{Entity?.Name}' is joined to the soft body itself, its particles are held in place instead");
                    anchor = null;
                }
                else if (bodyAnchor.Body is null)
                {
                    Logger.Warning($"A pin of {GetType().Name} on '{Entity?.Name}' has a {nameof(BodyAnchor)} without a body, its particles are held in place");
                }
                else if (bodyAnchor.Body is not StaticComponent)
                {
                    foreach (int particle in particles)
                        _anchorOfParticle[particle] = anchor;
                    filtersContacts = true;
                    var all = particles.ToArray();
                    var bodyPin = new BodyPin(pin, bodyAnchor.Body, all, SpreadOver(all), anchorNodes[p]);
                    bodyPins.Add(bodyPin);
                    TryBind(bodyPin);
                    continue;
                }
            }

            if (anchor is not null)
            {
                foreach (int particle in particles)
                    _anchorOfParticle[particle] = anchor;
                filtersContacts = true;
            }

            var spring = Spring(pin.SpringFrequency, pin.SpringDampingRatio);
            var servo = new ServoSettings(float.MaxValue, 0f, pin.ServoMaximumForce);
            int nodeAnchor = anchorNodes[p] < 0 ? -1 : Array.IndexOf(_nodeAnchors, anchor);
            foreach (int particle in particles)
            {
                var position = Particle(particle).Pose.Position;
                var worldPin = new WorldPin { Anchor = anchor, Node = anchorNodes[p], NodeAnchor = nodeAnchor, Target = position.ToStride(), Spring = spring, Servo = servo };
                worldPin.Offset = anchor is null ? worldPin.Target : Vector3.TransformCoordinate(worldPin.Target, Matrix.Invert(AnchorMatrix(anchor, worldPin.Node)));
                worldPin.Handle = solver.Add(_particles[particle], new OneBodyLinearServo { Target = position, SpringSettings = spring, ServoSettings = servo });
                worldPins.Add(worldPin);
            }
        }

        _worldPins = worldPins.ToArray();
        _bodyPins = bodyPins.ToArray();
        FiltersContactsWithOthers = filtersContacts;
    }

    /// <summary> Joins the particles of <paramref name="pin"/> to its anchor once the anchor is part of the same simulation </summary>
    private void TryBind(BodyPin pin)
    {
        var anchor = pin.Anchor;
        if (anchor.Simulation is null || anchor.CollidableCount == 0)
            return;

        if (ReferenceEquals(anchor.Simulation, Simulation) == false)
        {
            if (pin.Reported == false)
                Logger.Warning($"A pin of {GetType().Name} on '{Entity?.Name}' is joined to '{anchor.Entity?.Name}', which is in another simulation; its particles are left free");
            pin.Reported = true;
            return;
        }

        var simulation = Simulation!.Simulation;
        var spring = Spring(pin.Settings.SpringFrequency, pin.Settings.SpringDampingRatio);
        var servo = new ServoSettings(float.MaxValue, 0f, pin.Settings.ServoMaximumForce);
        Span<(float Distance, int Index)> nearest = stackalloc (float, int)[SoftAnchorParticles];
        if (anchor is SoftBodyComponent)
        {
            // Particles do not rotate: distances to a few of them hold the pinned particle in place whichever way the soft body turns
            foreach (int particle in pin.Particles)
            {
                var handle = _particles[particle];
                int found = FindNearest(anchor, simulation.Bodies[handle].Pose.Position, nearest);
                for (int n = 0; n < found; n++)
                {
                    var other = anchor.GetCollidableReference(nearest[n].Index).BodyHandle;
                    if (MathUtil.WithinEpsilon(nearest[n].Distance, 0f, 1e-4f))
                    {
                        simulation.Solver.Add(other, handle, new BallSocketServo { SpringSettings = spring, ServoSettings = servo });
                        break;
                    }
                    simulation.Solver.Add(other, handle, new CenterDistanceConstraint(nearest[n].Distance, spring));
                }
            }
        }
        else
        {
            int node = FollowedAncestor(anchor, pin.Node);
            foreach (int particle in pin.Joined)
            {
                var handle = _particles[particle];
                var position = simulation.Bodies[handle].Pose.Position;
                if (FindNearest(anchor, position, nearest[..1], node) == 0)
                    continue;

                var body = simulation.Bodies[anchor.GetCollidableReference(nearest[0].Index).BodyHandle];
                var offset = System.Numerics.Vector3.Transform(position - body.Pose.Position, System.Numerics.Quaternion.Conjugate(body.Pose.Orientation));
                simulation.Solver.Add(body.Handle, handle, new BallSocketServo { LocalOffsetA = offset, SpringSettings = spring, ServoSettings = servo });
            }
        }

        pin.Version = anchor.Versioning;
        pin.Bound = true;
        Awake = true;
    }

    /// <summary> The bodies of <paramref name="anchor"/> closest to <paramref name="position"/>, nearest first, as many as <paramref name="nearest"/> holds or fewer </summary>
    private static int FindNearest(CollidableComponent anchor, System.Numerics.Vector3 position, Span<(float Distance, int Index)> nearest, int node = -1)
    {
        var bodies = anchor.Simulation!.Simulation.Bodies;
        int found = 0;
        for (int i = 0; i < anchor.CollidableCount; i++)
        {
            var reference = anchor.GetCollidableReference(i);
            if (reference.Mobility == CollidableMobility.Static || (node >= 0 && anchor.GetFollowedNode(i) != node))
                continue;

            // Insertion into the sorted list, the farthest one drops out when it is full
            float distance = System.Numerics.Vector3.Distance(bodies[reference.BodyHandle].Pose.Position, position);
            int slot = found < nearest.Length ? found++ : nearest.Length;
            for (; slot > 0 && nearest[slot - 1].Distance > distance; slot--)
            {
                if (slot < nearest.Length)
                    nearest[slot] = nearest[slot - 1];
            }
            if (slot < nearest.Length)
                nearest[slot] = (distance, i);
        }
        return found;
    }

    /// <summary> <paramref name="node"/> or its closest ancestor with a body of <paramref name="anchor"/> following it, -1 for any body </summary>
    private static int FollowedAncestor(CollidableComponent anchor, int node)
    {
        var nodes = anchor.Entity.Get<ModelComponent>()?.Skeleton?.Nodes;
        for (; node >= 0 && nodes is not null; node = nodes[node].ParentIndex)
        {
            for (int i = 0; i < anchor.CollidableCount; i++)
            {
                if (anchor.GetFollowedNode(i) == node)
                    return node;
            }
        }
        return -1;
    }

    /// <summary> The world matrix of <paramref name="anchor"/>, or of its node when <paramref name="node"/> is not -1 </summary>
    private static Matrix AnchorMatrix(Entity anchor, int node)
    {
        if (node < 0)
            return anchor.Transform.WorldMatrix;
        return anchor.Get<ModelComponent>()!.Skeleton.NodeTransformations[node].WorldMatrix;
    }

    /// <summary> Up to <see cref="MaximumJoinedParticles"/> of <paramref name="particles"/>, spread over them: each next one is the farthest from those already picked </summary>
    private int[] SpreadOver(int[] particles)
    {
        if (particles.Length <= MaximumJoinedParticles)
            return particles;

        var rests = Topology!.RestPositions;
        var picked = new int[MaximumJoinedParticles];
        var distances = new float[particles.Length];
        Array.Fill(distances, float.MaxValue);
        int next = 0;
        for (int count = 0; count < picked.Length; count++)
        {
            picked[count] = particles[next];
            var from = rests[particles[next]];
            float farthest = -1f;
            for (int i = 0; i < particles.Length; i++)
            {
                distances[i] = MathF.Min(distances[i], Vector3.DistanceSquared(rests[particles[i]], from));
                if (distances[i] > farthest)
                {
                    farthest = distances[i];
                    next = i;
                }
            }
        }
        return picked;
    }

    /// <summary> Binds the pins whose anchor was added or rebuilt, moves the targets of the pins following an entity </summary>
    private void UpdatePins()
    {
        foreach (var pin in _bodyPins)
        {
            // The constraints went with the bodies of the anchor when it was removed or rebuilt
            if (pin.Bound && pin.Anchor.Versioning != pin.Version)
                pin.Bound = false;
            if (pin.Bound == false)
                TryBind(pin);
        }

        if (_worldPins.Length == 0)
            return;
        for (int i = 0; i < _nodeAnchors.Length; i++)
            _nodeSkeletons[i] = ModelNodes.Refresh(_nodeAnchors[i]);

        var solver = Simulation!.Simulation.Solver;
        bool moved = false;
        for (int i = 0; i < _worldPins.Length; i++)
        {
            ref var pin = ref _worldPins[i];
            if (pin.Anchor is null)
                continue;

            // The entity itself when its model, or the node, is no longer there
            var matrix = pin.NodeAnchor >= 0 && _nodeSkeletons[pin.NodeAnchor] is { } skeleton && pin.Node < skeleton.NodeTransformations.Length
                ? skeleton.NodeTransformations[pin.Node].WorldMatrix
                : pin.Anchor.Transform.WorldMatrix;
            var target = Vector3.TransformCoordinate(pin.Offset, matrix);
            if (target == pin.Target)
                continue;
            pin.Target = target;
            solver.ApplyDescription(pin.Handle, new OneBodyLinearServo { Target = target.ToNumeric(), SpringSettings = pin.Spring, ServoSettings = pin.Servo });
            moved = true;
        }

        // A sleeping body would not follow its anchor
        if (moved)
            Awake = true;
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

    /// <summary> Reports the hits on the shape of a particle with the index of that particle as their child </summary>
    private struct ParticleRayHitHandler<T>(T inner, int particle) : IShapeRayHitHandler where T : struct, IShapeRayHitHandler
    {
        public T Inner = inner;

        public bool AllowTest(int childIndex) => Inner.AllowTest(particle);

        public void OnRayHit(in global::BepuPhysics.Trees.RayData ray, ref float maximumT, float t, System.Numerics.Vector3 normal, int childIndex) => Inner.OnRayHit(ray, ref maximumT, t, normal, particle);
    }

    private sealed class StepListener(SoftBodyComponent owner) : ISimulationUpdate
    {
        public Entity Entity => owner.Entity;

        public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            owner.UpdatePins();
            owner.BeforeStep(simTimeStep);
        }

        public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            if (owner.Awake)
                owner.SyncTransform();
        }
    }

    /// <summary> A particle pulled toward a point: <see cref="Offset"/> in the space of the anchor's node, of the anchor, or of the world without an anchor </summary>
    private struct WorldPin
    {
        public Entity? Anchor;
        public int Node;
        public int NodeAnchor; // The index of the anchor among the ones whose nodes are refreshed, -1 when it follows the entity
        public Vector3 Offset;
        public Vector3 Target;
        public SpringSettings Spring;
        public ServoSettings Servo;
        public ConstraintHandle Handle;
    }

    /// <summary> The particles of a pin joined to the bodies of another collidable, once <see cref="Bound"/> </summary>
    private sealed class BodyPin(SoftBodyPin settings, CollidableComponent anchor, int[] particles, int[] joined, int node)
    {
        /// <summary> The node of the anchor's model the particles are joined to the bodies of, -1 for any body </summary>
        public int Node { get; } = node;
        public SoftBodyPin Settings { get; } = settings;
        public CollidableComponent Anchor { get; } = anchor;
        public int[] Particles { get; } = particles;

        /// <summary> The ones of <see cref="Particles"/> joined to a rigid anchor, the others follow through the soft body </summary>
        public int[] Joined { get; } = joined;
        public uint Version { get; set; }
        public bool Bound { get; set; }
        public bool Reported { get; set; }
    }
}
