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
using Stride.Core.Threading;

namespace Stride.BepuPhysics.Configurations;

[DataContract]
public class BepuSimulation
{
    const string CATEGORY_TIME = "Time";
    const string CATEGORY_CONSTRAINTS = "Constraints";
    const string CATEGORY_FORCES = "Forces";

    private TimeSpan _fixedTimeStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
    private readonly List<ISimulationUpdate> _simulationUpdateComponents = new();
    private readonly List<BodyContainerComponent> _interpolatedBodies = new();
    private readonly ThreadDispatcher _threadDispatcher;
    private TimeSpan _remainingUpdateTime;
    private TimeSpan _softStartRemainingDuration;
    private bool _softStartScheduled = false;

    internal BufferPool BufferPool { get; }

    internal CollidableProperty<MaterialProperties> CollidableMaterials { get; } = new();
    internal ContactEventsManager ContactEvents { get; }

    internal List<IBodyContainer?> Bodies { get; } = new();
    internal List<IStaticContainer?> Statics { get; } = new();

    /// <summary>
    /// Get the bepu Simulation /!\
    /// </summary>
    [DataMemberIgnore]
    public Simulation Simulation { get; }

    /// <summary>
    /// Whether to update the simulation
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
    [Display(3, "Fixed Time Step (s)", CATEGORY_TIME)]
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
    [Display(4, "Time Scale", CATEGORY_TIME)]
    public float TimeScale { get; set; } = 1f;

    /// <summary>
    /// Represents the maximum number of steps per frame to avoid a death loop
    /// </summary>
    [Display(5, "Max steps/frame", CATEGORY_TIME)]
    public int MaxStepPerFrame { get; set; } = 3;

    /// <summary>
    /// Allows for per-body features like <see cref="ContainerComponent.IgnoreGlobalGravity"/> at a cost to the simulation's performance
    /// </summary>
    /// <remarks>
    /// <see cref="ContainerComponent.IgnoreGlobalGravity"/> will be ignored if this is false.
    /// </remarks>
    [Display(6, "Per Body Attributes", CATEGORY_FORCES)]
    public bool UsePerBodyAttributes
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes = value;
    }

    /// <summary>
    /// Global gravity settings. This gravity will be applied to all bodies in the simulations that are not kinematic.
    /// </summary>
    [Display(7, "Gravity", CATEGORY_FORCES)]
    public Vector3 PoseGravity
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity.ToStrideVector();
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity = value.ToNumericVector();
    }

    /// <summary>
    /// Controls linear damping, how fast object loose their linear velocity
    /// </summary>
    [Display(8, "Linear Damping", CATEGORY_FORCES)]
    public float PoseLinearDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping = value;
    }

    /// <summary>
    /// Controls angular damping, how fast object loose their angular velocity
    /// </summary>
    [Display(9, "Angular Damping", CATEGORY_FORCES)]
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
    [Display(10, "Solver Iteration", CATEGORY_CONSTRAINTS)]
    public int SolverIteration { get => Simulation.Solver.VelocityIterationCount; init => Simulation.Solver.VelocityIterationCount = value; }

    /// <summary>
    /// The number of sub-steps used when solving constraints
    /// </summary>
    /// <remarks>
    /// Smaller values improve performance at the cost of stability and precision.
    /// </remarks>
    [Display(11, "Solver SubStep", CATEGORY_CONSTRAINTS)]
    public int SolverSubStep { get => Simulation.Solver.SubstepCount; init => Simulation.Solver.SubstepCount = value; }

    /// <summary>
    /// The duration for the SoftStart; when the simulation starts up, more <see cref="SolverSubStep"/>
    /// run to improve constraints stability and let them come to rest sooner.
    /// </summary>
    /// <remarks>
    /// Negative or 0 disables this feature.
    /// </remarks>
    [Display(12, "SoftStart Duration", CATEGORY_CONSTRAINTS)]
    public TimeSpan SoftStartDuration { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Multiplier over <see cref="SolverSubStep"/> during Soft Start
    /// </summary>
    [Display(13, "SoftStart Substep factor", CATEGORY_CONSTRAINTS)]
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

    public BepuSimulation()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);

        #warning Consider wrapping stride's threadpool/dispatcher into an IThreadDispatcher and passing that over to bepu instead of using their dispatcher
        _threadDispatcher = new ThreadDispatcher(targetThreadCount);
        BufferPool = new BufferPool();
        ContactEvents = new ContactEventsManager(_threadDispatcher, BufferPool);

        var _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks() { CollidableMaterials = CollidableMaterials, ContactEvents = ContactEvents };
        var _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks() { CollidableMaterials = CollidableMaterials };
        var _solveDescription = new SolveDescription(1, 1);

        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);
        Simulation.Solver.VelocityIterationCount = 8;
        Simulation.Solver.SubstepCount = 1;

        CollidableMaterials.Initialize(Simulation);
        ContactEvents.Initialize(Simulation);
        //CollisionBatcher = new CollisionBatcher<BatcherCallbacks>(BufferPool, Simulation.Shapes, Simulation.NarrowPhase.CollisionTaskRegistry, 0, DefaultBatcherCallbacks);
    }

    public IBodyContainer GetContainer(BodyHandle handle)
    {
        var body = Bodies[handle.Value];
        Debug.Assert(body is not null, "Handle is invalid, Bepu's array indexing strategy might have changed under us");
        return body;
    }

    public IStaticContainer GetContainer(StaticHandle handle)
    {
        var statics = Statics[handle.Value];
        Debug.Assert(statics is not null, "Handle is invalid, Bepu's array indexing strategy might have changed under us");
        return statics;
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

            var simTimeStepInSec = (float)FixedTimeStep.TotalSeconds;
            foreach (var updateComponent in _simulationUpdateComponents)
            {
                updateComponent.SimulationUpdate(simTimeStepInSec);
            }

            Simulation.Timestep(simTimeStepInSec, _threadDispatcher); //perform physic simulation using SimulationFixedStep
            ContactEvents.Flush(); //Fire event handler stuff.

            if (turnOffSoftStart)
            {
                Simulation.Solver.SubstepCount = SolverSubStep / SoftStartSubstepFactor;
                _softStartRemainingDuration = TimeSpan.Zero;
            }

            SyncActiveTransformsWithPhysics();

            foreach (var updateComponent in _simulationUpdateComponents)
            {
                updateComponent.AfterSimulationUpdate(simTimeStepInSec);
            }

            foreach (var body in _interpolatedBodies)
            {
                body.PreviousPose = body.CurrentPos;
                Debug.Assert(body.ContainerData is not null);
                body.CurrentPos = Simulation.Bodies[body.ContainerData.BHandle].Pose;
            }
        }

        InterpolateTransforms();
    }

    private void SyncActiveTransformsWithPhysics()
    {
        if (ParallelUpdate)
        {
            Dispatcher.For(0, Simulation.Bodies.ActiveSet.Count, (i) => SyncTransformsWithPhysics(Simulation.Bodies.ActiveSet.IndexToHandle[i], this));
        }
        else
        {
            for (int i = 0; i < Simulation.Bodies.ActiveSet.Count; i++)
            {
                SyncTransformsWithPhysics(Simulation.Bodies.ActiveSet.IndexToHandle[i], this);
            }
        }

        static void SyncTransformsWithPhysics(BodyHandle handle, BepuSimulation bepuSim)
        {
            var bodyContainer = bepuSim.GetContainer(handle);
            Debug.Assert(bodyContainer.ContainerData is not null);

            if (bodyContainer.ContainerData.Parent is {} containerParent)
            {
                Debug.Assert(containerParent.ContainerData is not null);
                // Have to go through our parents to make sure they're up to date since we're reading from the parent's world matrix
                // This means that we're potentially updating bodies that are not part of the active set but checking that may be more costly than just doing the thing
                SyncTransformsWithPhysics(containerParent.ContainerData.BHandle, bepuSim);
                // This can be slower than expected when we have multiple containers as parents recursively since we would recompute the topmost container n times, the second topmost n-1 etc.
                // It's not that likely but should still be documented as suboptimal somewhere
                containerParent.Entity.Transform.Parent.UpdateWorldMatrix();
            }

            var body = bepuSim.Simulation.Bodies[handle];
            var localPosition = body.Pose.Position.ToStrideVector();
            var localRotation = body.Pose.Orientation.ToStrideQuaternion();

            var entityTransform = bodyContainer.Entity.Transform;
            if (entityTransform.Parent is { } parent)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
                var iRotation = Quaternion.Invert(parentEntityRotation);
                localPosition = Vector3.Transform(localPosition - parentEntityPosition, iRotation);
                localRotation = localRotation * iRotation;
            }

            entityTransform.Rotation = localRotation;
            entityTransform.Position = localPosition - Vector3.Transform(bodyContainer.CenterOfMass, localRotation);
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
            Dispatcher.For(0, _interpolatedBodies.Count, (i) => InterpolateContainer(_interpolatedBodies[i], interpolationFactor));
        }
        else
        {
            foreach (var body in _interpolatedBodies)
            {
                InterpolateContainer(body, interpolationFactor);
            }
        }

        static void InterpolateContainer(BodyContainerComponent body, float interpolationFactor)
        {
            Debug.Assert(body.ContainerData is not null);

            // Have to go through our parents to make sure they're up to date since we're reading from the parent's world matrix
            // This means that we're potentially updating bodies that are not part of the active set but checking that may be more costly than just doing the thing
            for (var containerParent = body.ContainerData.Parent; containerParent != null; containerParent = containerParent.ContainerData!.Parent)
            {
                if (containerParent is BodyContainerComponent parentBody && parentBody.Interpolation != Interpolation.None)
                {
                    InterpolateContainer(parentBody, interpolationFactor); // That guy will take care of his parents too
                    // This can be slower than expected when we have multiple containers as parents recursively since we would recompute the topmost container n times, the second topmost n-1 etc.
                    // It's not that likely but should still be documented as suboptimal somewhere
                    containerParent.Entity.Transform.Parent.UpdateWorldMatrix();
                    break;
                }
            }

            if (body.Interpolation == Interpolation.Extrapolated)
                interpolationFactor += 1f;

            var interpolatedPosition = System.Numerics.Vector3.Lerp(body.PreviousPose.Position, body.CurrentPos.Position, interpolationFactor).ToStrideVector();
            // We may be able to get away with just a Lerp instead of Slerp, not sure if it needs to be normalized though at which point it may not be that much faster
            var interpolatedRotation = System.Numerics.Quaternion.Slerp(body.PreviousPose.Orientation, body.CurrentPos.Orientation, interpolationFactor).ToStrideQuaternion();

            var entityTransform = body.Entity.Transform;
            if (entityTransform.Parent is { } parent)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
                var iRotation = Quaternion.Invert(parentEntityRotation);
                interpolatedPosition = Vector3.Transform(interpolatedPosition - parentEntityPosition, iRotation);
                interpolatedRotation = interpolatedRotation * iRotation;
            }

            entityTransform.Rotation = interpolatedRotation;
            entityTransform.Position = interpolatedPosition - Vector3.Transform(body.CenterOfMass, interpolatedRotation);
        }
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
        _interpolatedBodies.Add(body);
    }
    internal void UnregisterInterpolated(BodyContainerComponent body)
    {
        _interpolatedBodies.Remove(body);
    }
}
