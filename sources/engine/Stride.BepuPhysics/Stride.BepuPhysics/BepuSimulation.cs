// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using BepuUtilities;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Definitions.Raycast;
using Stride.BepuPhysics.Definitions.SimTests;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.Core.Serialization;
using Stride.Engine;
using NVector3 = System.Numerics.Vector3;
using BRigidPose = BepuPhysics.RigidPose;
using SRigidPose = Stride.BepuPhysics.Definitions.RigidPose;
using SBodyVelocity = Stride.BepuPhysics.Definitions.BodyVelocity;

namespace Stride.BepuPhysics;

[DataContract]
public sealed class BepuSimulation : IDisposable
{
    private const string CategoryTime = "Time";
    private const string CategoryConstraints = "Constraints";
    private const string CategoryForces = "Forces";
    private const string MaskCategory = "Collisions";

    private TimeSpan _fixedTimeStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
    private readonly Dictionary<Type, Elider> _simulationUpdateComponents = new();
    private readonly List<BodyComponent> _interpolatedBodies = new();
    private readonly ThreadDispatcher _threadDispatcher;
    private TimeSpan _remainingUpdateTime;
    private TimeSpan _softStartRemainingDuration;
    private bool _softStartScheduled = false;
    private UrlReference<Scene>? _associatedScene = null;
    private AwaitRunner _preTickRunner = new();
    private AwaitRunner _postTickRunner = new();

    internal BufferPool BufferPool { get; }

    internal CollidableProperty<MaterialProperties> CollidableMaterials { get; } = new();
    internal ContactEventsManager ContactEvents { get; }

    internal List<BodyComponent?> Bodies { get; } = new();
    internal List<StaticComponent?> Statics { get; } = new();

    /// <summary> Required when a component is removed from the simulation and must have its contacts flushed </summary>
    internal (int value, CollidableComponent? component) TemporaryDetachedLookup { get; set; }

    /// <inheritdoc cref="Stride.BepuPhysics.Definitions.CollisionMatrix"/>
    [DataMemberIgnore]
    public CollisionMatrix CollisionMatrix = CollisionMatrix.All; // Keep this as a field, user need ref access for writes

    /// <summary>
    /// Accessing and altering this object is inherently unsupported and unsafe, this is the internal bepu simulation.
    /// </summary>
    [DataMemberIgnore]
    public Simulation Simulation { get; }

    /// <summary>
    /// The scene associated with this simulation.
    /// </summary>
    /// <remarks>
    /// When this is set, entities spawning inside this scene will be automatically associated with
    /// this simulation as long as their <see cref="CollidableComponent.SimulationSelector"/> is set to <see cref="SceneBasedSimulationSelector"/>.
    /// See <see cref="SceneBasedSimulationSelector"/> for more info.
    /// </remarks>
    public UrlReference<Scene>? AssociatedScene
    {
        get
        {
            return _associatedScene;
        }
        set
        {
            if (value?.IsEmpty == true)
                throw new ArgumentException("Url must be valid, assign null instead");

            _associatedScene = value;
        }
    }

    /// <summary>
    /// Whether to update the simulation.
    /// </summary>
    /// <remarks>
    /// False also disables contact processing but won't prevent re-synchronization of static physics bodies to their engine counterpart
    /// </remarks>
    [Display(0, "Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Allow entity synchronization to occur across multiple threads instead of just the main thread
    /// </summary>
    [Display(1, "Parallel Update")]
    public bool ParallelUpdate { get; set; } = true;

    /// <summary>
    /// Whether to use a deterministic time step when using multithreading. When set to true, additional time is spent sorting constraint additions and transfers.
    /// </summary>
    /// <remarks>
    /// This can only affect determinism locally- different processor architectures may implement instructions differently.
    /// There is also some performance cost
    /// </remarks>
    [Display(2, "Deterministic")]
    public bool Deterministic
    {
        get => Simulation.Deterministic;
        set => Simulation.Deterministic = value;
    }

    /// <summary>
    /// The number of seconds per step to simulate. Lossy, prefer <see cref="FixedTimeStep"/>.
    /// </summary>
    [Display(3, "Fixed Time Step (s)", CategoryTime)]
    public double FixedTimeStepSeconds
    {
        get => FixedTimeStep.TotalSeconds;
        set => FixedTimeStep = TimeSpan.FromSeconds(value);
    }

    /// <summary>
    /// The speed of the simulation compared to real-time.
    /// </summary>
    /// <remarks>
    /// This stacks with <see cref="Stride.Games.GameTime"/>.<see cref="Stride.Games.GameTime.Factor"/>,
    /// changing that one already affects the simulation speed.
    /// </remarks>
    [Display(4, "Time Scale", CategoryTime)]
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// Represents the maximum number of steps per frame to avoid a death loop
    /// </summary>
    [Display(5, "Max steps/frame", CategoryTime)]
    public int MaxStepPerFrame { get; set; } = 3;

    /// <summary>
    /// Allows for per-body features like <see cref="BodyComponent.Gravity"/> at a cost to the simulation's performance
    /// </summary>
    /// <remarks>
    /// <see cref="BodyComponent.Gravity"/> will be ignored if this is false.
    /// </remarks>
    [Display(6, "Per Body Attributes", CategoryForces)]
    public bool UsePerBodyAttributes
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes = value;
    }

    /// <summary>
    /// Global gravity settings. This gravity will be applied to all bodies in the simulations that are not kinematic.
    /// </summary>
    [Display(7, "Gravity", CategoryForces)]
    public Vector3 PoseGravity
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity.ToStride();
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity = value.ToNumeric();
    }

    /// <summary>
    /// Controls linear damping, how fast object loose their linear velocity
    /// </summary>
    [Display(8, "Linear Damping", CategoryForces)]
    public float PoseLinearDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping = value;
    }

    /// <summary>
    /// Controls angular damping, how fast object loose their angular velocity
    /// </summary>
    [Display(9, "Angular Damping", CategoryForces)]
    public float PoseAngularDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping = value;
    }

    /// <summary>
    /// The number of iterations for the solver
    /// </summary>
    /// <remarks>
    /// Smaller values improve performance at the cost of stability and precision.
    /// </remarks>
    [Display(10, "Solver Iteration", CategoryConstraints)]
    public int SolverIteration { get => Simulation.Solver.VelocityIterationCount; init => Simulation.Solver.VelocityIterationCount = value; }

    /// <summary>
    /// The number of sub-steps used when solving constraints
    /// </summary>
    /// <remarks>
    /// Smaller values improve performance at the cost of stability and precision.
    /// </remarks>
    [Display(11, "Solver SubStep", CategoryConstraints)]
    public int SolverSubStep { get => Simulation.Solver.SubstepCount; init => Simulation.Solver.SubstepCount = value; }

    /// <summary>
    /// The duration for the SoftStart; when the simulation starts up, more <see cref="SolverSubStep"/>
    /// run to improve constraints stability and let them come to rest sooner.
    /// </summary>
    /// <remarks>
    /// Negative or 0 disables this feature.
    /// </remarks>
    [Display(12, "SoftStart Duration", CategoryConstraints)]
    public TimeSpan SoftStartDuration { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Multiplier over <see cref="SolverSubStep"/> during Soft Start
    /// </summary>
    [Display(13, "SoftStart Substep factor", CategoryConstraints)]
    public int SoftStartSubstepFactor { get; set; } = 4;

    /// <summary>
    /// The amount of time between individual simulation steps/ticks, by default ~16.67 milliseconds which would run 60 ticks per second
    /// </summary>
    /// <remarks>
    /// Larger values improve performance at the cost of stability and precision.
    /// </remarks>
    [DataMemberIgnore]
    public TimeSpan FixedTimeStep
    {
        get => _fixedTimeStep;
        set
        {
            if (value.Ticks <= 0)
                throw new ArgumentException("Duration provided must be greater than zero");
            _fixedTimeStep = value;
        }
    }

    #region UglyEditorWorkaroundForMasks
    // This nonsense is temporary, we need a specific editor UI for this
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer0 { get => CollisionMatrix.Get(CollisionLayer.Layer0); set => CollisionMatrix.Set(CollisionLayer.Layer0, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer1 { get => CollisionMatrix.Get(CollisionLayer.Layer1); set => CollisionMatrix.Set(CollisionLayer.Layer1, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer2 { get => CollisionMatrix.Get(CollisionLayer.Layer2); set => CollisionMatrix.Set(CollisionLayer.Layer2, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer3 { get => CollisionMatrix.Get(CollisionLayer.Layer3); set => CollisionMatrix.Set(CollisionLayer.Layer3, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer4 { get => CollisionMatrix.Get(CollisionLayer.Layer4); set => CollisionMatrix.Set(CollisionLayer.Layer4, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer5 { get => CollisionMatrix.Get(CollisionLayer.Layer5); set => CollisionMatrix.Set(CollisionLayer.Layer5, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer6 { get => CollisionMatrix.Get(CollisionLayer.Layer6); set => CollisionMatrix.Set(CollisionLayer.Layer6, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer7 { get => CollisionMatrix.Get(CollisionLayer.Layer7); set => CollisionMatrix.Set(CollisionLayer.Layer7, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer8 { get => CollisionMatrix.Get(CollisionLayer.Layer8); set => CollisionMatrix.Set(CollisionLayer.Layer8, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer9 { get => CollisionMatrix.Get(CollisionLayer.Layer9); set => CollisionMatrix.Set(CollisionLayer.Layer9, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer10 { get => CollisionMatrix.Get(CollisionLayer.Layer10); set => CollisionMatrix.Set(CollisionLayer.Layer10, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer11 { get => CollisionMatrix.Get(CollisionLayer.Layer11); set => CollisionMatrix.Set(CollisionLayer.Layer11, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer12 { get => CollisionMatrix.Get(CollisionLayer.Layer12); set => CollisionMatrix.Set(CollisionLayer.Layer12, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer13 { get => CollisionMatrix.Get(CollisionLayer.Layer13); set => CollisionMatrix.Set(CollisionLayer.Layer13, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer14 { get => CollisionMatrix.Get(CollisionLayer.Layer14); set => CollisionMatrix.Set(CollisionLayer.Layer14, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer15 { get => CollisionMatrix.Get(CollisionLayer.Layer15); set => CollisionMatrix.Set(CollisionLayer.Layer15, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer16 { get => CollisionMatrix.Get(CollisionLayer.Layer16); set => CollisionMatrix.Set(CollisionLayer.Layer16, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer17 { get => CollisionMatrix.Get(CollisionLayer.Layer17); set => CollisionMatrix.Set(CollisionLayer.Layer17, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer18 { get => CollisionMatrix.Get(CollisionLayer.Layer18); set => CollisionMatrix.Set(CollisionLayer.Layer18, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer19 { get => CollisionMatrix.Get(CollisionLayer.Layer19); set => CollisionMatrix.Set(CollisionLayer.Layer19, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer20 { get => CollisionMatrix.Get(CollisionLayer.Layer20); set => CollisionMatrix.Set(CollisionLayer.Layer20, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer21 { get => CollisionMatrix.Get(CollisionLayer.Layer21); set => CollisionMatrix.Set(CollisionLayer.Layer21, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer22 { get => CollisionMatrix.Get(CollisionLayer.Layer22); set => CollisionMatrix.Set(CollisionLayer.Layer22, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer23 { get => CollisionMatrix.Get(CollisionLayer.Layer23); set => CollisionMatrix.Set(CollisionLayer.Layer23, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer24 { get => CollisionMatrix.Get(CollisionLayer.Layer24); set => CollisionMatrix.Set(CollisionLayer.Layer24, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer25 { get => CollisionMatrix.Get(CollisionLayer.Layer25); set => CollisionMatrix.Set(CollisionLayer.Layer25, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer26 { get => CollisionMatrix.Get(CollisionLayer.Layer26); set => CollisionMatrix.Set(CollisionLayer.Layer26, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer27 { get => CollisionMatrix.Get(CollisionLayer.Layer27); set => CollisionMatrix.Set(CollisionLayer.Layer27, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer28 { get => CollisionMatrix.Get(CollisionLayer.Layer28); set => CollisionMatrix.Set(CollisionLayer.Layer28, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer29 { get => CollisionMatrix.Get(CollisionLayer.Layer29); set => CollisionMatrix.Set(CollisionLayer.Layer29, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer30 { get => CollisionMatrix.Get(CollisionLayer.Layer30); set => CollisionMatrix.Set(CollisionLayer.Layer30, value); }
    [DefaultValue(CollisionMask.Everything), Display(category:MaskCategory)] public CollisionMask Layer31 { get => CollisionMatrix.Get(CollisionLayer.Layer31); set => CollisionMatrix.Set(CollisionLayer.Layer31, value); }
    #endregion

    public BepuSimulation()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);

        #warning Consider wrapping stride's threadpool/dispatcher into an IThreadDispatcher and passing that over to bepu instead of using their dispatcher
        _threadDispatcher = new ThreadDispatcher(targetThreadCount);
        BufferPool = new BufferPool();
        ContactEvents = new ContactEventsManager(_threadDispatcher, BufferPool);

        var strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks(this, ContactEvents, CollidableMaterials);
        var stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks(CollidableMaterials);
        var solveDescription = new SolveDescription(1, 1);

        Simulation = Simulation.Create(BufferPool, strideNarrowPhaseCallbacks, stridePoseIntegratorCallbacks, solveDescription);
        Simulation.Solver.VelocityIterationCount = 8;
        Simulation.Solver.SubstepCount = 1;

        CollidableMaterials.Initialize(Simulation);
        ContactEvents.Initialize(this);
        //CollisionBatcher = new CollisionBatcher<BatcherCallbacks>(BufferPool, Simulation.Shapes, Simulation.NarrowPhase.CollisionTaskRegistry, 0, DefaultBatcherCallbacks);
    }

    public void Dispose()
    {
        _threadDispatcher.Dispose();
        BufferPool.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CollidableComponent GetComponent(CollidableReference collidable)
    {
        return collidable.Mobility == CollidableMobility.Static ? GetComponent(collidable.StaticHandle) : GetComponent(collidable.BodyHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BodyComponent GetComponent(BodyHandle handle)
    {
        if (TemporaryDetachedLookup.component is BodyComponent detachedBody && handle.Value == TemporaryDetachedLookup.value)
            return detachedBody;

        var body = Bodies[handle.Value];
        Debug.Assert(body is not null, "Handle is invalid, Bepu's array indexing strategy might have changed under us");
        return body;
    }

    public StaticComponent GetComponent(StaticHandle handle)
    {
        if (TemporaryDetachedLookup.component is StaticComponent detachedStatic && handle.Value == TemporaryDetachedLookup.value)
            return detachedStatic;

        var statics = Statics[handle.Value];
        Debug.Assert(statics is not null, "Handle is invalid, Bepu's array indexing strategy might have changed under us");
        return statics;
    }

    /// <summary>
    /// Yields execution until right before the next physics tick
    /// </summary>
    /// <returns>Task that will resume next tick.</returns>
    public TickAwaiter NextUpdate() => new TickAwaiter(_preTickRunner);

    /// <summary>
    /// Yields execution until right after the next physics tick
    /// </summary>
    /// <returns>Task that will resume next tick.</returns>
    public TickAwaiter AfterUpdate() => new TickAwaiter(_postTickRunner);

    /// <summary>
    /// Whether a physics test with <paramref name="mask"/> against <paramref name="collidable"/> should be performed or entirely ignored
    /// </summary>
    /// <returns>True when it should be performed, false when it should be ignored</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldPerformPhysicsTest(CollisionMask mask, CollidableReference collidable)
    {
        var component = GetComponent(collidable);
        return mask.IsSet(component.CollisionLayer);
    }

    /// <summary>
    /// Finds the closest intersection between this ray and shapes in the simulation.
    /// </summary>
    /// <param name="origin">The start position for this ray</param>
    /// <param name="dir">The normalized direction the ray is facing</param>
    /// <param name="maxDistance">The maximum from the origin that hits will be collected</param>
    /// <param name="result">An intersection in the world when this method returns true, an undefined value when this method returns false</param>
    /// <param name="collisionMask">Which layer should be hit</param>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public bool RayCast(in Vector3 origin, in Vector3 dir, float maxDistance, out HitInfo result, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var handler = new RayClosestHitHandler(this, collisionMask);
        Simulation.RayCast(origin.ToNumeric(), dir.ToNumeric(), maxDistance, ref handler);
        if (handler.HitInformation.HasValue)
        {
            result = handler.HitInformation.Value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Collect intersections between the given ray and shapes in this simulation.
    /// </summary>
    /// <remarks>
    /// When there are more hits than <paramref name="buffer"/> can accomodate, returns only the closest hits.<br/>
    /// There are no guarantees as to the order hits are returned in.
    /// </remarks>
    /// <param name="origin">The start position for this ray</param>
    /// <param name="dir">The normalized direction the ray is facing</param>
    /// <param name="maxDistance">The maximum from the origin that hits will be collected</param>
    /// <param name="buffer">
    /// A temporary buffer which is used as a backing array to write to, its length defines the maximum amount of info you want to read.
    /// It is used by the returned enumerator as its backing array from which you read
    /// </param>
    /// <param name="collisionMask">Which layer should be hit</param>
    public unsafe ConversionEnum<ManagedConverter, HitInfoStack, HitInfo> RayCastPenetrating(in Vector3 origin, in Vector3 dir, float maxDistance, Span<HitInfoStack> buffer, CollisionMask collisionMask = CollisionMask.Everything)
    {
        fixed (HitInfoStack* ptr = &buffer[0])
        {
            var handler = new RayHitsStackHandler(ptr, buffer.Length, this, collisionMask);
            Simulation.RayCast(origin.ToNumeric(), dir.ToNumeric(), maxDistance, ref handler);
            return new (buffer[..handler.Head], new ManagedConverter(this));
        }
    }

    /// <summary>
    /// Collect intersections between the given ray and shapes in this simulation. Hits are NOT sorted.
    /// </summary>
    /// <remarks> There are no guarantees as to the order hits are returned in. </remarks>
    /// <param name="origin">The start position for this ray</param>
    /// <param name="dir">The normalized direction the ray is facing</param>
    /// <param name="maxDistance">The maximum from the origin that hits will be collected</param>
    /// <param name="collection">The collection used to store hits into, the collection is not cleared before usage, hits are appended to it</param>
    /// <param name="collisionMask">Which layer should be hit</param>
    public void RayCastPenetrating(in Vector3 origin, in Vector3 dir, float maxDistance, ICollection<HitInfo> collection, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var handler = new RayHitsCollectionHandler(this, collection, collisionMask);
        Simulation.RayCast(origin.ToNumeric(), dir.ToNumeric(), maxDistance, ref handler);
    }

    /// <summary>
    /// Finds the closest contact between <paramref name="shape"/> and other shapes in the simulation when thrown in <paramref name="velocity"/> direction.
    /// </summary>
    /// <param name="shape">The shape thrown at the scene</param>
    /// <param name="pose">Initial position for the shape</param>
    /// <param name="velocity">Velocity used to throw the shape</param>
    /// <param name="maxDistance">The maximum distance, or amount of time along the path of the <paramref name="velocity"/></param>
    /// <param name="result">The resulting contact when this method returns true, an undefined value when this method returns false</param>
    /// <param name="collisionMask">Which layer should be hit</param>
    /// <typeparam name="TShape"></typeparam>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public bool SweepCast<TShape>(in TShape shape, in SRigidPose pose, in SBodyVelocity velocity, float maxDistance, out HitInfo result, CollisionMask collisionMask = CollisionMask.Everything) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        var handler = new RayClosestHitHandler(this, collisionMask);
        Simulation.Sweep(shape, pose.ToBepu(), velocity.ToBepu(), maxDistance, BufferPool, ref handler);
        if (handler.HitInformation.HasValue)
        {
            result = handler.HitInformation.Value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Finds contacts between <paramref name="shape"/> and other shapes in the simulation when thrown in <paramref name="velocity"/> direction.
    /// </summary>
    /// <remarks>
    /// When there are more hits than <paramref name="buffer"/> can accomodate, returns only the closest hits <br/>
    /// There are no guarantees as to the order hits are returned in.
    /// </remarks>
    /// <param name="shape">The shape thrown at the scene</param>
    /// <param name="pose">Initial position for the shape</param>
    /// <param name="velocity">Velocity used to throw the shape</param>
    /// <param name="maxDistance">The maximum distance, or amount of time along the path of the <paramref name="velocity"/></param>
    /// <param name="buffer">
    /// A temporary buffer which is used as a backing array to write to, its length defines the maximum amount of info you want to read.
    /// It is used by the returned enumerator as its backing array from which you read
    /// </param>
    /// <param name="collisionMask">Which layer should be hit</param>
    /// <typeparam name="TShape"></typeparam>
    public unsafe ConversionEnum<ManagedConverter, HitInfoStack, HitInfo> SweepCastPenetrating<TShape>(in TShape shape, in SRigidPose pose, in SBodyVelocity velocity, float maxDistance, Span<HitInfoStack> buffer, CollisionMask collisionMask = CollisionMask.Everything) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        fixed (HitInfoStack* ptr = &buffer[0])
        {
            var handler = new RayHitsStackHandler(ptr, buffer.Length, this, collisionMask);
            Simulation.Sweep(shape, pose.ToBepu(), velocity.ToBepu(), maxDistance, BufferPool, ref handler);
            return new (buffer[..handler.Head], new(this));
        }
    }

    /// <summary>
    /// Finds contacts between <paramref name="shape"/> and other shapes in the simulation when thrown in <paramref name="velocity"/> direction.
    /// </summary>
    /// <remarks> There are no guarantees as to the order hits are returned in. </remarks>
    /// <param name="shape">The shape thrown at the scene</param>
    /// <param name="pose">Initial position for the shape</param>
    /// <param name="velocity">Velocity used to throw the shape</param>
    /// <param name="maxDistance">The maximum distance, or amount of time along the path of the <paramref name="velocity"/></param>
    /// <param name="collection">The collection used to store hits into, the collection is not cleared before usage, hits are appended to it</param>
    /// <param name="collisionMask">Which layer should be hit</param>
    /// <typeparam name="TShape"></typeparam>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public void SweepCastPenetrating<TShape>(in TShape shape, in SRigidPose pose, in SBodyVelocity velocity, float maxDistance, ICollection<HitInfo> collection, CollisionMask collisionMask = CollisionMask.Everything) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        var handler = new RayHitsCollectionHandler(this, collection, collisionMask);
        Simulation.Sweep(shape, pose.ToBepu(), velocity.ToBepu(), maxDistance, BufferPool, ref handler);
    }

    /// <summary>
    /// Appends any physics object into <paramref name="collection"/> if it was found to be overlapping with <paramref name="shape"/>
    /// </summary>
    /// <remarks>The collection is not cleared before appending items into it</remarks>
    /// <param name="shape">The shape used to test for overlap</param>
    /// <param name="pose">Position the shape is on for this test</param>
    /// <param name="collection">The collection used to store overlapped shapes into. Note that the collection is not cleared before items are added to it</param>
    /// <param name="collisionMask">Which layer should be hit</param>
    public void Overlap<TShape>(in TShape shape, in SRigidPose pose, ICollection<OverlapInfo> collection, CollisionMask collisionMask = CollisionMask.Everything) where TShape : unmanaged, IConvexShape
    {
        var collector = new CollectionCollector(collection);
        OverlapInner(shape, pose, collisionMask, ref collector);
    }

    /// <summary>
    /// Enumerates any physics object found to be overlapping with <paramref name="shape"/>
    /// </summary>
    /// <typeparam name="TShape">A bepu <see cref="IConvexShape"/> representing the shape that will be used when testing for overlap</typeparam>
    /// <param name="shape">The shape used to test for overlap</param>
    /// <param name="pose">Position the shape is on for this test</param>
    /// <param name="buffer">
    /// A temporary buffer which is used as a backing array to write to, its length defines the maximum amount of info you want to read.
    /// It is used by the returned enumerator as its backing array from which you read
    /// </param>
    /// <param name="collisionMask">Which layer should be hit</param>
    public ConversionEnum<ManagedConverter, CollidableStack, CollidableComponent> Overlap<TShape>(in TShape shape, in SRigidPose pose, Span<CollidableStack> buffer, CollisionMask collisionMask = CollisionMask.Everything) where TShape : unmanaged, IConvexShape
    {
        unsafe
        {
            fixed (CollidableStack* ptr = buffer)
            {
                var collector = new SpanCollidableCollector(ptr, buffer.Length, this);
                OverlapInner(shape, pose, collisionMask, ref collector);
                buffer = buffer[..collector.Head]; // Only include data that has been written to
            }

            return new(buffer, new(this));
        }
    }

    /// <summary>
    /// Enumerates all overlap info for any shape and sub-shapes found to be overlapping with <paramref name="shape"/>
    /// </summary>
    /// <remarks> Multiple info may come from the same <see cref="CollidableComponent"/> when it is a compound shape </remarks>
    /// <typeparam name="TShape">A bepu <see cref="IConvexShape"/> representing the shape that will be used when testing for overlap</typeparam>
    /// <param name="shape">The shape used to test for overlap</param>
    /// <param name="pose">Position the shape is on for this test</param>
    /// <param name="buffer">
    /// A temporary buffer which is used as a backing array to write to, its length defines the maximum amount of info you want to read.
    /// It is used by the returned enumerator as its backing array from which you read
    /// </param>
    /// <param name="collisionMask">Which layer should be hit</param>
    public ConversionEnum<ManagedConverter, OverlapInfoStack, OverlapInfo> OverlapInfo<TShape>(in TShape shape, in SRigidPose pose, Span<OverlapInfoStack> buffer, CollisionMask collisionMask = CollisionMask.Everything) where TShape : unmanaged, IConvexShape
    {
        unsafe
        {
            fixed (OverlapInfoStack* ptr = buffer)
            {
                var collector = new SpanManifoldCollector(ptr, buffer.Length, this);
                OverlapInner(shape, pose, collisionMask, ref collector);
                buffer = buffer[..collector.Head]; // Only include data that has been written to
            }

            return new(buffer, new(this));
        }
    }

    /// <summary>
    /// Called by the BroadPhase.GetOverlaps to collect all encountered collidables.
    /// </summary>
    struct BroadPhaseOverlapEnumerator : IBreakableForEach<CollidableReference>
    {
        public QuickList<CollidableReference> References;
        //The enumerator never gets stored into unmanaged memory, so it's safe to include a reference type instance.
        public BufferPool Pool;
        public bool LoopBody(CollidableReference reference)
        {
            References.Allocate(Pool) = reference;
            //If you wanted to do any top-level filtering, this would be a good spot for it.
            //The CollidableReference tells you whether it's a body or a static object and the associated handle. You can look up metadata with that.
            return true;
        }
    }

    /// <summary>
    /// Provides callbacks for filtering and data collection to the CollisionBatcher we'll be using to test query shapes against the detected environment.
    /// </summary>
    struct BatcherCallbacks<T> : ICollisionCallbacks where T : IOverlapCollector
    {
        public required CollisionMask CollisionMask;
        public required QuickList<CollidableReference> References;
        public required CollidableProperty<MaterialProperties> CollidableMaterials;
        public required T Collector;
        public required BepuSimulation Simulation;

        //These callbacks provide filtering and reporting for pairs being processed by the collision batcher.
        //"Pair id" refers to the identifier given to the pair when it was added to the batcher.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowCollisionTesting(int pairId, int childA, int childB)
        {
            var matA = CollidableMaterials[References[pairId]];
            return CollisionMask.IsSet(matA.Layer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnChildPairCompleted(int pairId, int childA, int childB, ref ConvexContactManifold manifold)
        {
            //If you need to do any processing on a child manifold before it goes back to a nonconvex processing pass, this is the place to do it.
            //Convex-convex pairs won't invoke this function at all.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnPairCompleted<TManifold>(int pairId, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            Collector.OnPairCompleted(Simulation, References[pairId], ref manifold);
        }
    }

    unsafe void OverlapInner<TShape, TCollector>(in TShape shape, in SRigidPose pose, CollisionMask collisionMask, ref TCollector collector) where TShape : unmanaged, IConvexShape where TCollector : IOverlapCollector
    {
        fixed (TShape* queryShapeData = &shape)
        {
            var queryShapeSize = Unsafe.SizeOf<TShape>();
            var bepuPose = pose.ToBepu();
            queryShapeData->ComputeBounds(bepuPose.Orientation, out var boundingBoxMin, out var boundingBoxMax);
            boundingBoxMin += bepuPose.Position;
            boundingBoxMax += bepuPose.Position;
            var broadPhaseEnumerator = new BroadPhaseOverlapEnumerator
            {
                Pool = BufferPool,
                References = new QuickList<CollidableReference>(16, BufferPool)
            };

            try
            {
                Simulation.BroadPhase.GetOverlaps(boundingBoxMin, boundingBoxMax, ref broadPhaseEnumerator);

                var batcher = new CollisionBatcher<BatcherCallbacks<TCollector>>(BufferPool, Simulation.Shapes, Simulation.NarrowPhase.CollisionTaskRegistry, 0, new()
                {
                    CollisionMask = collisionMask,
                    References = broadPhaseEnumerator.References,
                    CollidableMaterials = CollidableMaterials,
                    Collector = collector,
                    Simulation = this
                });

                int i = 0;
                foreach (CollidableReference reference in broadPhaseEnumerator.References)
                {
                    BRigidPose poseOther;
                    TypedIndex shapeIndexOther;
                    //Collidables can be associated with either bodies or statics. We have to look in a different place depending on which it is.
                    if (reference.Mobility == CollidableMobility.Static)
                    {
                        var collidable = Simulation.Statics[reference.StaticHandle];
                        poseOther = collidable.Pose;
                        shapeIndexOther = collidable.Shape;
                    }
                    else
                    {
                        var bodyReference = Simulation.Bodies[reference.BodyHandle];
                        poseOther = bodyReference.Pose;
                        shapeIndexOther = bodyReference.Collidable.Shape;
                    }

                    Simulation.Shapes[shapeIndexOther.Type].GetShapeData(shapeIndexOther.Index, out var shapeData, out _);
                    //In this path, we assume that the incoming shape data is ephemeral. The collision batcher may last longer than the data pointer.
                    //To avoid undefined access, we cache the query data into the collision batcher and use a pointer to the cache instead.
                    batcher.Callbacks.References.Add(reference, BufferPool);
                    batcher.CacheShapeB(shapeIndexOther.Type, TShape.TypeId, queryShapeData, queryShapeSize, out var cachedQueryShapeData);
                    batcher.AddDirectly(shapeIndexOther.Type, TShape.TypeId, shapeData, cachedQueryShapeData,
                        //Because we're using this as a boolean query, we use a speculative margin of 0. Don't care about negative depths.
                        bepuPose.Position - poseOther.Position, poseOther.Orientation, bepuPose.Orientation, 0, new PairContinuation(i));
                    i++;
                }

                //While the collision batcher may flush batches here and there when a new test is added if it fills a batch,
                //it's likely that there remain leftover pairs that didn't fill up a batch completely. Force a complete flush.
                //Note that this also returns all resources used by the batcher to the BufferPool.
                batcher.Flush();
                collector = batcher.Callbacks.Collector;
            }
            finally
            {
                broadPhaseEnumerator.References.Dispose(BufferPool);
            }
        }
    }

    /// <summary>
    /// Reset the SoftStart to SoftStartDuration.
    /// </summary>
    public void ResetSoftStart()
    {
        _softStartScheduled = true;
    }

    internal void Update(TimeSpan elapsed)
    {
        if (!Enabled)
            return;

        // TimeSpan multiplication is lossy, skipping mult when we can
        _remainingUpdateTime += TimeScale == 1f ? elapsed : elapsed * TimeScale;

        for (int stepCount = 0; _remainingUpdateTime >= FixedTimeStep && (stepCount < MaxStepPerFrame || MaxStepPerFrame != -1); stepCount++, _remainingUpdateTime -= FixedTimeStep)
        {
            if (_softStartScheduled)
            {
                _softStartScheduled = false;
                if (SoftStartDuration > TimeSpan.Zero)
                {
                    _softStartRemainingDuration = SoftStartDuration;
                    Simulation.Solver.SubstepCount = SolverSubStep * SoftStartSubstepFactor;
                }
            }

            bool turnOffSoftStart = false;
            if (_softStartRemainingDuration > TimeSpan.Zero)
            {
                turnOffSoftStart = _softStartRemainingDuration <= FixedTimeStep;
                _softStartRemainingDuration -= FixedTimeStep;
            }

            float simTimeStepInSec = (float)FixedTimeStep.TotalSeconds;

            _preTickRunner.Run();

            Elider.SimulationUpdate(_simulationUpdateComponents, this, simTimeStepInSec);

            Simulation.Timestep(simTimeStepInSec, _threadDispatcher); //perform physic simulation using SimulationFixedStep
            ContactEvents.Flush(); //Fire event handler stuff.

            if (turnOffSoftStart)
            {
                Simulation.Solver.SubstepCount = SolverSubStep / SoftStartSubstepFactor;
                _softStartRemainingDuration = TimeSpan.Zero;
            }

            SyncActiveTransformsWithPhysics();

            Elider.AfterSimulationUpdate(_simulationUpdateComponents, this, simTimeStepInSec);

            _postTickRunner.Run();

            foreach (var body in _interpolatedBodies)
            {
                body.PreviousPose = body.CurrentPose;
                if (body.BodyReference is {} bRef)
                    body.CurrentPose = bRef.Pose;
            }
        }

        InterpolateTransforms();
    }

    private void SyncActiveTransformsWithPhysics()
    {
        if (ParallelUpdate)
        {
            Dispatcher.For(0, Simulation.Bodies.ActiveSet.Count, (i) => SyncTransformsWithPhysics(Simulation.Bodies.GetBodyReference(Simulation.Bodies.ActiveSet.IndexToHandle[i]), this));
        }
        else
        {
            for (int i = 0; i < Simulation.Bodies.ActiveSet.Count; i++)
            {
                SyncTransformsWithPhysics(Simulation.Bodies.GetBodyReference(Simulation.Bodies.ActiveSet.IndexToHandle[i]), this);
            }
        }

        static void SyncTransformsWithPhysics(in BodyReference body, BepuSimulation bepuSim)
        {
            var collidable = bepuSim.GetComponent(body.Handle);

            for (var item = collidable.Parent; item != null; item = item.Parent)
            {
                if (item.BodyReference is { } bRef)
                {
                    // Have to go through our parents to make sure they're up to date since we're reading from the parent's world matrix
                    // This means that we're potentially updating bodies that are not part of the active set but checking that may be more costly than just doing the thing
                    SyncTransformsWithPhysics(bRef, bepuSim);
                    // This can be slower than expected when we have multiple collidables as parents recursively since we would recompute the topmost collidable n times, the second topmost n-1 etc.
                    // It's not that likely but should still be documented as suboptimal somewhere
                    item.Entity.Transform.Parent.UpdateWorldMatrix();
                }
            }

            var localPosition = body.Pose.Position.ToStride();
            var localRotation = body.Pose.Orientation.ToStride();

            var entityTransform = collidable.Entity.Transform;
            if (entityTransform.Parent is { } parent)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
                var iRotation = Quaternion.Invert(parentEntityRotation);
                localPosition = Vector3.Transform(localPosition - parentEntityPosition, iRotation);
                localRotation = localRotation * iRotation;
            }

            entityTransform.Rotation = localRotation;
            entityTransform.Position = localPosition - Vector3.Transform(collidable.CenterOfMass, localRotation);
        }
    }

    private void InterpolateTransforms()
    {
        // Find the interpolation factor, a value [0,1] which represents the ratio of the current time relative to the previous and the next physics step,
        // a value of 0.5 means that we're halfway to the next physics update, just have to wait for the same amount of time.
        var interpolationFactor = (float)(_remainingUpdateTime.TotalSeconds / FixedTimeStep.TotalSeconds);
        interpolationFactor = MathF.Min(interpolationFactor, 1f);
        if (ParallelUpdate)
        {
            Dispatcher.For(0, _interpolatedBodies.Count, i => InterpolateBody(_interpolatedBodies[i], interpolationFactor));
        }
        else
        {
            foreach (var body in _interpolatedBodies)
            {
                InterpolateBody(body, interpolationFactor);
            }
        }

        static void InterpolateBody(BodyComponent body, float interpolationFactor)
        {
            // Have to go through our parents to make sure they're up-to-date since we're reading from the parent's world matrix
            // This means that we're potentially updating bodies that are not part of the active set but checking that may be more costly than just doing the thing
            for (var item = body.Parent; item != null; item = item.Parent)
            {
                if (item is BodyComponent parentBody && parentBody.InterpolationMode != InterpolationMode.None)
                {
                    InterpolateBody(parentBody, interpolationFactor); // This one will take care of his parents too.
                    // This can be slower than expected when we have multiple collidables as parents recursively since we would recompute the topmost collidable n times, the second topmost n-1 etc.
                    // It's not that likely but should still be documented as suboptimal somewhere
                    parentBody.Entity.Transform.Parent.UpdateWorldMatrix();
                    break;
                }
            }

            if (body.InterpolationMode == InterpolationMode.Extrapolated)
                interpolationFactor += 1f;

            var interpolatedPosition = System.Numerics.Vector3.Lerp(body.PreviousPose.Position, body.CurrentPose.Position, interpolationFactor).ToStride();
            // We may be able to get away with just a Lerp instead of Slerp, not sure if it needs to be normalized though at which point it may not be that much faster
            var interpolatedRotation = System.Numerics.Quaternion.Slerp(body.PreviousPose.Orientation, body.CurrentPose.Orientation, interpolationFactor).ToStride();

            body.WorldToLocal(ref interpolatedPosition, ref interpolatedRotation);
            body.Entity.Transform.Position = interpolatedPosition;
            body.Entity.Transform.Rotation = interpolatedRotation;
        }
    }

    internal void Register(ISimulationUpdate simulationUpdateComponent)
    {
        Elider.AddToHandlers(simulationUpdateComponent, _simulationUpdateComponents);
    }

    internal bool Unregister(ISimulationUpdate simulationUpdateComponent)
    {
        return Elider.RemoveFromHandlers(simulationUpdateComponent, _simulationUpdateComponents);
    }

    internal void RegisterInterpolated(BodyComponent body)
    {
        _interpolatedBodies.Add(body);

        body.Entity.Transform.UpdateWorldMatrix();
        body.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion collidableWorldRotation, out Vector3 collidableWorldTranslation);
        body.CurrentPose.Position = (collidableWorldTranslation + body.CenterOfMass).ToNumeric();
        body.CurrentPose.Orientation = collidableWorldRotation.ToNumeric();
        body.PreviousPose = body.CurrentPose;
    }

    internal void UnregisterInterpolated(BodyComponent body)
    {
        _interpolatedBodies.Remove(body);
    }

    /// <summary>
    /// Used to JIT specialized classes handling each type implementing <see cref="ISimulationUpdate"/> individually
    /// to elide and inline the virtual calls.
    /// </summary>
    internal abstract class Elider
    {
        protected abstract void Add(ISimulationUpdate obj);
        protected abstract bool Remove(ISimulationUpdate obj);
        protected abstract void SimulationUpdate(BepuSimulation sim, float deltaTime);
        protected abstract void AfterSimulationUpdate(BepuSimulation sim, float deltaTime);

        public static void AddToHandlers(ISimulationUpdate item, Dictionary<Type, Elider> handlers)
        {
            var concreteType = item.GetType();
            if (handlers.TryGetValue(concreteType, out var batcher) == false)
            {
                var gen = typeof(Handler<>).MakeGenericType(concreteType); // Create one specific Handler<> for this concrete type
                batcher = (Elider)Activator.CreateInstance(gen)!; // Create an instance of this specialized type to pass our items to it
                handlers.Add(concreteType, batcher);
            }
            batcher.Add(item);
        }

        public static bool RemoveFromHandlers(ISimulationUpdate item, Dictionary<Type, Elider> handlers)
        {
            return handlers.TryGetValue(item.GetType(), out var handler) && handler.Remove(item);
        }

        public static void SimulationUpdate(Dictionary<Type, Elider> handlers, BepuSimulation sim, float deltaTime)
        {
            foreach (var kvp in handlers)
                kvp.Value.SimulationUpdate(sim, deltaTime);
        }

        public static void AfterSimulationUpdate(Dictionary<Type, Elider> handlers, BepuSimulation sim, float deltaTime)
        {
            foreach (var kvp in handlers)
                kvp.Value.AfterSimulationUpdate(sim, deltaTime);
        }

        private sealed class Handler<T> : Elider where T : ISimulationUpdate // This class get specialized to a concrete type
        {
            private List<T> _abstraction = [];
            protected override void Add(ISimulationUpdate obj) => _abstraction.Add((T)obj);
            protected override bool Remove(ISimulationUpdate obj) => _abstraction.Remove((T)obj);
            protected override void SimulationUpdate(BepuSimulation sim, float deltaTime)
            {
                foreach (var abstraction in _abstraction)
                    abstraction.SimulationUpdate(sim, deltaTime);
            }
            protected override void AfterSimulationUpdate(BepuSimulation sim, float deltaTime)
            {
                foreach (var abstraction in _abstraction)
                    abstraction.AfterSimulationUpdate(sim, deltaTime);
            }
        }
    }

    internal class AwaitRunner
    {
        private object _addLock = new();
        private List<Action> _scheduled = new();
        private List<Action> _processed = new();

        public void Add(Action a)
        {
            lock (_addLock)
            {
                _scheduled.Add(a);
            }
        }

        public void Run()
        {
            lock (_addLock)
            {
                (_processed, _scheduled) = (_scheduled, _processed);
            }

            foreach (var item in _processed)
                item.Invoke();

            _processed.Clear();
        }
    }

    /// <summary>
    /// Await this struct to continue during a physics tick
    /// </summary>
    public struct TickAwaiter : INotifyCompletion
    {
        private AwaitRunner _runner;

        internal TickAwaiter(AwaitRunner runner)
        {
            _runner = runner;
        }

        public bool IsCompleted => false; // Forces the awaiter to call OnCompleted() right away to schedule asynchronous method continuation with our runner

        public void OnCompleted(Action continuation) => _runner.Add(continuation);

        public void GetResult() { }

        public TickAwaiter GetAwaiter() => this;
    }
}
