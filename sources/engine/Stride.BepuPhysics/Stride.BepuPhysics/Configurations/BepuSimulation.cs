using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Definitions.Raycast;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;


namespace Stride.BepuPhysics.Configurations;

[DataContract]
public class BepuSimulation
{
    private readonly List<ISimulationUpdate> _simulationUpdateComponents = new();

    internal ThreadDispatcher ThreadDispatcher { get; private set; }
    internal BufferPool BufferPool { get; private set; }

    internal CollidableProperty<MaterialProperties> CollidableMaterials { get; private set; } = new();
    internal ContactEventsManager ContactEvents { get; private set; }

    internal Dictionary<BodyHandle, IBodyContainer> BodiesContainers { get; } = new();
    internal Dictionary<StaticHandle, IStaticContainer> StaticsContainers { get; } = new();
    internal List<BodyContainerComponent> InterpolatedBodies { get; } = new();

    internal int RemainingUpdateTimeMs { get; set; } = 0;
    internal int SoftStartRemainingDurationMs = -1;

    /// <summary>
    /// Get the bepu Simulation /!\
    /// </summary>
    [DataMemberIgnore]
    public Simulation Simulation { get; private set; }

    /// <summary>
    /// Start or Stop the simulation updating.
    /// </summary>
    [Display(0, "Enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Allows you to choose the speed of the simulation (real time multiplicator).
    /// </summary>
    [Display(1, "TimeWarp")]
    public float TimeWarp { get; set; } = 1f;

    /// <summary>
    /// This function slow down the simulation but allow to integrate per Body settings.
    /// Ignore global gravity will not work if set to false.
    /// </summary>
    [Display(11, "UsePerBodyAttributes")]
    public bool UsePerBodyAttributes 
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes = value;
    }

    /// <summary>
    /// Global gravity settings. This gravity will be applied to all bodies in the simulations that are not kinematic.
    /// </summary>
    [Display(12, "PoseGravity")]
    public Vector3 PoseGravity
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity.ToStrideVector();
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity = value.ToNumericVector();
    }

    /// <summary>
    /// Controls linear damping (how fast object loose it's linear velocity)
    /// </summary>
    [Display(13, "PoseLinearDamping")]
    public float PoseLinearDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping = value;
    }

    /// <summary>
    /// Controls angular damping (how fast object loose it's angular velocity)
    /// </summary>
    [Display(14, "PoseAngularDamping")]
    public float PoseAngularDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping = value;
    }

    /// <summary>
    /// Controls the number of iterations for the solver
    /// Can be heavy performance wise, consider update solveSubStep first.
    /// </summary>
    [Display(15, "SolveIteration")]
    public int SolveIteration { get => Simulation.Solver.VelocityIterationCount; init => Simulation.Solver.VelocityIterationCount = value; }

    /// <summary>
    /// Specifies the number of sub-steps for solving 
    /// </summary>
    [Display(16, "SolveSubStep")]
    public int SolveSubStep { get => Simulation.Solver.SubstepCount; init => Simulation.Solver.SubstepCount = value; }

    /// <summary>
    /// Specifies the number of milliseconds per step to simulate
    /// </summary>
    [Display(30, "Simulation fixed step")]
    public int SimulationFixedStep { get; set; } = 1000 / 60;

    /// <summary>
    /// Represents the maximum number of steps per frame to avoid a death loop
    /// </summary>
    [Display(31, "Max steps/frame")]
    public int MaxStepPerFrame { get; set; } = 3;

    /// <summary>
    /// Allow entity synchronization to occur across multiple threads instead of just the main thread
    /// </summary>
    [Display(35, "Parallel update")]
    public bool ParallelUpdate { get; set; } = true;

    /// <summary>
    /// The duration in millisec of the SoftStart.
    /// Negative or 0 disable the feature.
    /// </summary>
    [Display(36, "SoftStart duration (ms)")]
    public int SoftStartDuration { get; set; } = 1000;

    /// <summary>
    /// How much we should soften the simulation during softStart ?
    /// </summary>
    [Display(37, "SoftStart softness")]
    public int SoftStartSoftness { get; set; } = 4;

    /// <summary>
    /// Whether to use a deterministic time step when using multithreading. When set to true, additional time is spent sorting constraint additions and transfers.
    /// Note that this can only affect determinism locally- different processor architectures may implement instructions differently.
    /// </summary>
    [Display(38, "Deterministic")]
    public bool Deterministic
    {
        get => Simulation.Deterministic;
        set => Simulation.Deterministic = value;
    }



    /// <summary>
    /// Reset the SoftStart to SoftStartDuration.
    /// </summary>
    public void ResetSoftStart()
    {
        if (SoftStartDuration > 0)
            SoftStartRemainingDurationMs = SoftStartDuration;
    }


    public BepuSimulation()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);

        #warning Consider wrapping stride's threadpool/dispatcher into an IThreadDispatcher and passing that over to bepu instead of using their dispatcher
        ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
        BufferPool = new BufferPool();
        ContactEvents = new ContactEventsManager(ThreadDispatcher, BufferPool);

        var _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks() { CollidableMaterials = CollidableMaterials, ContactEvents = ContactEvents };
        var _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks() { CollidableMaterials = CollidableMaterials };
        var _solveDescription = new SolveDescription(1, 1);

        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);

        CollidableMaterials.Initialize(Simulation);
        ContactEvents.Initialize(Simulation);
        //CollisionBatcher = new CollisionBatcher<BatcherCallbacks>(BufferPool, Simulation.Shapes, Simulation.NarrowPhase.CollisionTaskRegistry, 0, DefaultBatcherCallbacks);
    }

    /// <summary>
    /// Finds the closest intersection between this ray and shapes in the simulation.
    /// </summary>
    /// <param name="origin">The start position for this ray</param>
    /// <param name="dir">The normalized direction the ray is facing</param>
    /// <param name="maxDistance">The maximum from the origin that hits will be collected</param>
    /// <param name="result">An intersection in the world when this method returns true, an undefined value when this method returns false</param>
    /// <param name="collisionMask"></param>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public bool RayCast(in Vector3 origin, in Vector3 dir, float maxDistance, out HitInfo result, byte collisionMask = 255)
    {
        var handler = new RayClosestHitHandler(this, collisionMask);
        Simulation.RayCast(origin.ToNumericVector(), dir.ToNumericVector(), maxDistance, ref handler);
        if (handler.HitInformation.HasValue)
        {
            result = handler.HitInformation.Value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Collect intersections between the given ray and shapes in this simulation. Hits are NOT sorted.
    /// </summary>
    /// <param name="origin">The start position for this ray</param>
    /// <param name="dir">The normalized direction the ray is facing</param>
    /// <param name="maxDistance">The maximum from the origin that hits will be collected</param>
    /// <param name="buffer">
    /// The collection used to store hits into,
    /// feel free to rent it from <see cref="System.Buffers.ArrayPool{T}"/> and return it after you processed <paramref name="hits"/>
    /// </param>
    /// <param name="hits">Intersections are pushed to <see cref="buffer"/>, this is the subset of <paramref name="buffer"/> that contains valid/assigned values</param>
    /// <param name="collisionMask"></param>
    public void RaycastPenetrating(in Vector3 origin, in Vector3 dir, float maxDistance, HitInfo[] buffer, out Span<HitInfo> hits, byte collisionMask = 255)
    {
        var handler = new RayHitsArrayHandler(this, buffer, collisionMask);
        Simulation.RayCast(origin.ToNumericVector(), dir.ToNumericVector(), maxDistance, ref handler);
        hits = new(buffer, 0, handler.Count);
    }

    /// <summary>
    /// Collect intersections between the given ray and shapes in this simulation. Hits are NOT sorted.
    /// </summary>
    /// <param name="origin">The start position for this ray</param>
    /// <param name="dir">The normalized direction the ray is facing</param>
    /// <param name="maxDistance">The maximum from the origin that hits will be collected</param>
    /// <param name="collection">The collection used to store hits into, the collection is not cleared before usage, hits are appended to it</param>
    /// <param name="collisionMask"></param>
    public void RaycastPenetrating(in Vector3 origin, in Vector3 dir, float maxDistance, ICollection<HitInfo> collection, byte collisionMask = 255)
    {
        var handler = new RayHitsCollectionHandler(this, collection, collisionMask);
        Simulation.RayCast(origin.ToNumericVector(), dir.ToNumericVector(), maxDistance, ref handler);
    }

    /// <summary>
    /// Finds the closest contact between <paramref name="shape"/> and other shapes in the simulation when thrown in <paramref name="velocity"/> direction.
    /// </summary>
    /// <param name="shape">The shape thrown at the scene</param>
    /// <param name="pose">Initial position for the shape</param>
    /// <param name="velocity">Velocity used to throw the shape</param>
    /// <param name="maxDistance">The maximum distance, or amount of time along the path of the <paramref name="velocity"/></param>
    /// <param name="result">The resulting contact when this method returns true, an undefined value when this method returns false</param>
    /// <param name="collisionMask"></param>
    /// <typeparam name="TShape"></typeparam>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public bool SweepCast<TShape>(in TShape shape, in RigidPose pose, in BodyVelocity velocity, float maxDistance, out HitInfo result, byte collisionMask = 255) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        var handler = new RayClosestHitHandler(this, collisionMask);
        Simulation.Sweep(shape, pose, velocity, maxDistance, BufferPool, ref handler);
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
    /// <param name="shape">The shape thrown at the scene</param>
    /// <param name="pose">Initial position for the shape</param>
    /// <param name="velocity">Velocity used to throw the shape</param>
    /// <param name="maxDistance">The maximum distance, or amount of time along the path of the <paramref name="velocity"/></param>
    /// <param name="buffer">
    /// The collection used to store hits into,
    /// feel free to rent it from <see cref="System.Buffers.ArrayPool{T}"/> and return it after you processed <paramref name="contacts"/>
    /// </param>
    /// <param name="contacts">Contacts are pushed to <see cref="buffer"/>, this is the subset of <paramref name="buffer"/> that contains valid/assigned values</param>
    /// <param name="collisionMask"></param>
    /// <typeparam name="TShape"></typeparam>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public void SweepCastPenetrating<TShape>(in TShape shape, in RigidPose pose, in BodyVelocity velocity, float maxDistance, HitInfo[] buffer, out Span<HitInfo> contacts, byte collisionMask = 255) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        var handler = new RayHitsArrayHandler(this, buffer, collisionMask);
        Simulation.Sweep(shape, pose, velocity, maxDistance, BufferPool, ref handler);
        contacts = new(buffer, 0, handler.Count);
    }

    /// <summary>
    /// Finds contacts between <paramref name="shape"/> and other shapes in the simulation when thrown in <paramref name="velocity"/> direction.
    /// </summary>
    /// <param name="shape">The shape thrown at the scene</param>
    /// <param name="pose">Initial position for the shape</param>
    /// <param name="velocity">Velocity used to throw the shape</param>
    /// <param name="maxDistance">The maximum distance, or amount of time along the path of the <paramref name="velocity"/></param>
    /// <param name="collection">The collection used to store hits into, the collection is not cleared before usage, hits are appended to it</param>
    /// <param name="collisionMask"></param>
    /// <typeparam name="TShape"></typeparam>
    /// <returns>True when the given ray intersects with a shape, false otherwise</returns>
    public void SweepCastPenetrating<TShape>(in TShape shape, in RigidPose pose, in BodyVelocity velocity, float maxDistance, ICollection<HitInfo> collection, byte collisionMask = 255) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        var handler = new RayHitsCollectionHandler(this, collection, collisionMask);
        Simulation.Sweep(shape, pose, velocity, maxDistance, BufferPool, ref handler);
    }

    /// <summary>
    /// Returns true when this shape overlaps with any physics object in this simulation
    /// </summary>
    /// <param name="shape">The shape used to test for overlap</param>
    /// <param name="pose">Position the shape is on for this test</param>
    /// <param name="collisionMask"></param>
    /// <typeparam name="TShape"></typeparam>
    /// <returns>True when the given shape overlaps with any physics object in the simulation</returns>
    public bool Overlap<TShape>(in TShape shape, in RigidPose pose, byte collisionMask = 255) where TShape : unmanaged, IConvexShape
    {
        var handler = new OverlapAnyHandler(this, collisionMask);
        Simulation.Sweep(shape, pose, default, 0f, BufferPool, ref handler);
        return handler.Any;
    }

    /// <summary>
    /// Fills <paramref name="buffer"/> with any physics object in the simulation that overlaps with this shape
    /// </summary>
    /// <param name="shape">The shape used to test for overlap</param>
    /// <param name="pose">Position the shape is on for this test</param>
    /// <param name="buffer">
    /// The collection used to store hits into,
    /// feel free to rent it from <see cref="System.Buffers.ArrayPool{T}"/> and return it after you processed <paramref name="overlaps"/>
    /// </param>
    /// <param name="overlaps">Containers are pushed to <see cref="buffer"/>, this is the subset of <paramref name="buffer"/> that contains valid/assigned containers</param>
    /// <param name="collisionMask"></param>
    /// <typeparam name="TShape"></typeparam>
    public void Overlap<TShape>(in TShape shape, in RigidPose pose, IContainer[] buffer, out Span<IContainer> overlaps, byte collisionMask = 255) where TShape : unmanaged, IConvexShape
    {
        var handler = new OverlapArrayHandler(this, buffer, collisionMask);
        Simulation.Sweep(shape, pose, default, 0f, BufferPool, ref handler);
        overlaps = new(buffer, 0, handler.Count);
    }

    /// <summary>
    /// Fills <paramref name="collection"/> with any physics object in the simulation that overlaps with this shape
    /// </summary>
    /// <param name="shape">The shape used to test for overlap</param>
    /// <param name="pose">Position the shape is on for this test</param>
    /// <param name="collection">The collection used to store containers into, the collection is not cleared before usage, containers are appended to it</param>
    /// <param name="collisionMask"></param>
    /// <typeparam name="TShape"></typeparam>
    public void Overlap<TShape>(in TShape shape, in RigidPose pose, ICollection<IContainer> collection, byte collisionMask = 255) where TShape : unmanaged, IConvexShape
    {
        var handler = new OverlapCollectionHandler(this, collection, collisionMask);
        Simulation.Sweep(shape, pose, default, 0f, BufferPool, ref handler);
    }

    //private void Setup()
    //{
       
    //}
    //private void Clear()
    //{
    //    //Warning, calling this can lead to exceptions if there are entities with Bepu components since the ref is destroyed.
    //    BufferPool.Clear();
    //    BodiesContainers.Clear();
    //    StaticsContainers.Clear();
    //    ContactEvents.Dispose();
    //    Setup();
    //}

    internal void CallSimulationUpdate(float simTimeStep)
    {
        foreach (var updateComponent in _simulationUpdateComponents)
        {
            updateComponent.SimulationUpdate(simTimeStep);
        }
    }
    internal void CallAfterSimulationUpdate(float simTimeStep)
    {
        foreach (var updateComponent in _simulationUpdateComponents)
        {
            updateComponent.AfterSimulationUpdate(simTimeStep);
        }

        foreach (var body in InterpolatedBodies)
        {
            body.PreviousPose = body.CurrentPos;
            Debug.Assert(body.ContainerData is not null);
            body.CurrentPos = Simulation.Bodies[body.ContainerData.BHandle].Pose;
        }
    }

    internal void Register(ISimulationUpdate simulationUpdateComponent)
    {
        _simulationUpdateComponents.Add(simulationUpdateComponent);
    }
    internal void Unregister(ISimulationUpdate simulationUpdateComponent)
    {
        _simulationUpdateComponents.Remove(simulationUpdateComponent);
    }

    internal void RegisterInterpolated(BodyContainerComponent body)
    {
        InterpolatedBodies.Add(body);
    }
    internal void UnregisterInterpolated(BodyContainerComponent body)
    {
        InterpolatedBodies.Remove(body);
    }
}
