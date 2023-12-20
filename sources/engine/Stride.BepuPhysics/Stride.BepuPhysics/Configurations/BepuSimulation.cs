using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Collisions;
using Stride.BepuPhysics.Definitions.Raycast;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Configurations;

[DataContract]
public class BepuSimulation
{
    private readonly List<SimulationUpdateComponent> _simulationUpdateComponents = new();

    internal ThreadDispatcher ThreadDispatcher { get; private set; }
    internal BufferPool BufferPool { get; private set; }

    internal CollidableProperty<MaterialProperties> CollidableMaterials { get; private set; } = new CollidableProperty<MaterialProperties>();
    internal ContactEvents ContactEvents { get; private set; }

    internal Dictionary<BodyHandle, BodyContainerComponent> BodiesContainers { get; } = new();
    internal Dictionary<StaticHandle, StaticContainerComponent> StaticsContainers { get; } = new();

    internal float RemainingUpdateTime { get; set; } = 0;

    [DataMemberIgnore]
    public Simulation Simulation { get; private set; }


    [Display(0, "Enabled")]
    public bool Enabled { get; set; } = true;
    [Display(1, "TimeWarp")]
    public float TimeWarp { get; set; } = 1f;

    [Display(11, "UsePerBodyAttributes")]
    public bool UsePerBodyAttributes //Warning, set this to false can disable some features used by components.
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.UsePerBodyAttributes = value;
    }

    [Display(12, "PoseGravity")]
    public Vector3 PoseGravity
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity.ToStrideVector();
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity = value.ToNumericVector();
    }
    [Display(13, "PoseLinearDamping")]
    public float PoseLinearDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping = value;
    }
    [Display(14, "PoseAngularDamping")]
    public float PoseAngularDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping = value;
    }

    [Display(15, "SolveIteration")]
    public int SolveIteration { get => Simulation.Solver.VelocityIterationCount; init => Simulation.Solver.VelocityIterationCount = value; }

    [Display(16, "SolveSubStep")]
    public int SolveSubStep { get => Simulation.Solver.SubstepCount; init => Simulation.Solver.SubstepCount = value; }

    [Display(30, "Parallel update")]
    public bool ParallelUpdate { get; set; } = true;
    [Display(31, "Simulation fixed step")]
    public float SimulationFixedStep { get; set; } = 1000f / 60;
    [Display(32, "Max steps/frame")]
    public int MaxStepPerFrame { get; set; } = 3;


#pragma warning disable CS8618 //Done in setup to avoid 2 times the samecode.
    public BepuSimulation()
#pragma warning restore CS8618 
    {
        Setup();
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
    public bool RayCast(Vector3 origin, Vector3 dir, float maxDistance, out HitInfo result, byte collisionMask = 255)
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
    /// Collect intersections between the given ray and shapes in this simulation. Hits are sorted from closest to furthest away.
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
    public void RaycastPenetrating(Vector3 origin, Vector3 dir, float maxDistance, HitInfo[] buffer, out Span<HitInfo> hits, byte collisionMask = 255)
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
    public void RaycastPenetrating(Vector3 origin, Vector3 dir, float maxDistance, ICollection<HitInfo> collection, byte collisionMask = 255)
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

    private void Setup()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);

        ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
        BufferPool = new BufferPool();
        ContactEvents = new ContactEvents(ThreadDispatcher, BufferPool);

        var _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks() { CollidableMaterials = CollidableMaterials, ContactEvents = ContactEvents };
        var _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks() { CollidableMaterials = CollidableMaterials };
        var _solveDescription = new SolveDescription(1, 1);

        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);

        CollidableMaterials.Initialize(Simulation);
        ContactEvents.Initialize(Simulation);
        //CollisionBatcher = new CollisionBatcher<BatcherCallbacks>(BufferPool, Simulation.Shapes, Simulation.NarrowPhase.CollisionTaskRegistry, 0, DefaultBatcherCallbacks);
    }
    internal void Clear()
    {
        //Warning, calling this can lead to exceptions if there are entities with Bepu components since the ref is destroyed.
        BufferPool.Clear();
        BodiesContainers.Clear();
        StaticsContainers.Clear();
        ContactEvents.Dispose();
        Setup();
    }

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
    }

    internal void Register(SimulationUpdateComponent simulationUpdateComponent)
    {
        _simulationUpdateComponents.Add(simulationUpdateComponent);
    }
    internal void Unregister(SimulationUpdateComponent simulationUpdateComponent)
    {
        _simulationUpdateComponents.Remove(simulationUpdateComponent);
    }

}
