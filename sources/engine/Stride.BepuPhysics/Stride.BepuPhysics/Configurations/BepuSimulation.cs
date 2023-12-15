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
using static Stride.BepuPhysics.Definitions.StrideNarrowPhaseCallbacks;

namespace Stride.BepuPhysics.Configurations;

#warning Trigger HERE
//I started to implement https://github.com/bepu/bepuphysics2/blob/master/Demos/Demos/CollisionQueryDemo.cs
//But it work really well with StaticContainer, so we need to ask Norbo if worth it.
//update : it doesn't worth it from what norbo said. Since trigger in game need to be always there, it's betterr than an one time querrry per frame.

[DataContract]
public class BepuSimulation
{
    private readonly List<SimulationUpdateComponent> _simulationUpdateComponents = new();
    private RayHitHandler DefaultRayHitHandler;
    private SweepHitHandler DefaultSweepHitHandler;
    //private BatcherCallbacks DefaultBatcherCallbacks;

    internal ThreadDispatcher ThreadDispatcher { get; private set; }
    internal BufferPool BufferPool { get; private set; }

    internal CollidableProperty<MaterialProperties> CollidableMaterials { get; private set; } = new CollidableProperty<MaterialProperties>();
    internal ContactEvents ContactEvents { get; private set; }
    //internal CollisionBatcher<BatcherCallbacks> CollisionBatcher { get; private set; }

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
    public float SimulationFixedStep { get; set; } = 1000 / 60;
    [Display(32, "Max steps/frame")]
    public int MaxStepPerFrame { get; set; } = 3;


#pragma warning disable CS8618 //Done in setup to avoid 2 times the samecode.
    public BepuSimulation()
#pragma warning restore CS8618 
    {
        Setup();
        DefaultRayHitHandler = new RayHitHandler(this);
        DefaultSweepHitHandler = new SweepHitHandler(this);
        //DefaultBatcherCallbacks = new();
    }

    public HitResult RayCast(Vector3 origin, Vector3 dir, float maxT, bool stopAtFirstHit = false, byte collisionMask = 255)
    {
        DefaultRayHitHandler.Prepare(stopAtFirstHit, collisionMask);
        Simulation.RayCast(origin.ToNumericVector(), dir.ToNumericVector(), maxT, ref DefaultRayHitHandler);
        return DefaultRayHitHandler.Hit;
    }

    public HitResult SweepCast<TShape>(in TShape shape, in RigidPose pose, in BodyVelocity velocity, float maxT, bool stopAtFirstHit = false, byte collisionMask = 255) where TShape : unmanaged, IConvexShape //== collider "RayCast"
    {
        DefaultSweepHitHandler.Prepare(stopAtFirstHit, collisionMask);
        Simulation.Sweep(shape, pose, velocity, maxT, BufferPool, ref DefaultSweepHitHandler);
        return DefaultSweepHitHandler.Hit;
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

    #region BatcherWIP

    ///// <summary>
    ///// Provides callbacks for filtering and data collection to the CollisionBatcher we'll be using to test query shapes against the detected environment.
    ///// </summary>
    //public struct BatcherCallbacks : ICollisionCallbacks
    //{
    //    /// <summary>
    //    /// Set to true for a pair if a nonnegative depth is detected by collision testing.
    //    /// </summary>
    //    public Buffer<bool> QueryWasTouched;

    //    //These callbacks provide filtering and reporting for pairs being processed by the collision batcher.
    //    //"Pair id" refers to the identifier given to the pair when it was added to the batcher.
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public bool AllowCollisionTesting(int pairId, int childA, int childB)
    //    {
    //        //If you wanted to filter based on the children of an encountered nonconvex object, here would be the place to do it.
    //        //The pairId could be used to look up the involved objects and any metadata necessary for filtering.
    //        return true;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public void OnChildPairCompleted(int pairId, int childA, int childB, ref ConvexContactManifold manifold)
    //    {
    //        //If you need to do any processing on a child manifold before it goes back to a nonconvex processing pass, this is the place to do it.
    //        //Convex-convex pairs won't invoke this function at all.
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public void OnPairCompleted<TManifold>(int pairId, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    //    {
    //        //This function hands off the completed manifold with all postprocessing (NonconvexReduction, MeshReduction, etc.) complete.
    //        //For the purposes of this demo, we're interested in boolean collision testing.
    //        //(This process was a little overkill for a pure boolean test, but there is no pure boolean path right now because the contact manifold generators turned out fast enough.
    //        //And if you find yourself wanting contact data, well, you've got it handy!)
    //        for (int i = 0; i < manifold.Count; ++i)
    //        {
    //            //This probably looks a bit odd, but it addresses a limitation of returning references to the struct 'this' instance.
    //            //(What we really want here is either the lifting of that restriction, or allowing interfaces to require a static member so that we could call the static function and pass the instance, 
    //            //instead of invoking the function on the instance AND passing the instance.)
    //            if (manifold.GetDepth(i) >= 0)
    //            {
    //                QueryWasTouched[pairId] = true;
    //                break;
    //            }
    //        }
    //    }
    //}

    ///// <summary>
    ///// Called by the BroadPhase.GetOverlaps to collect all encountered collidables.
    ///// </summary>
    //struct BroadPhaseOverlapEnumerator : IBreakableForEach<CollidableReference>
    //{
    //    public QuickList<CollidableReference> References;
    //    //The enumerator never gets stored into unmanaged memory, so it's safe to include a reference type instance.
    //    public BufferPool Pool;
    //    public bool LoopBody(CollidableReference reference)
    //    {
    //        References.Allocate(Pool) = reference;
    //        //If you wanted to do any top-level filtering, this would be a good spot for it.
    //        //The CollidableReference tells you whether it's a body or a static object and the associated handle. You can look up metadata with that.
    //        return true;
    //    }
    //}


    //void GetPoseAndShape(CollidableReference reference, out RigidPose pose, out TypedIndex shapeIndex)
    //{
    //    //Collidables can be associated with either bodies or statics. We have to look in a different place depending on which it is.
    //    if (reference.Mobility == CollidableMobility.Static)
    //    {
    //        var collidable = Simulation.Statics[reference.StaticHandle];
    //        pose = collidable.Pose;
    //        shapeIndex = collidable.Shape;
    //    }
    //    else
    //    {
    //        var bodyReference = Simulation.Bodies[reference.BodyHandle];
    //        pose = bodyReference.Pose;
    //        shapeIndex = bodyReference.Collidable.Shape;
    //    }
    //}

    ///// <summary>
    ///// Adds a shape query to the collision batcher.
    ///// </summary>
    ///// <param name="queryShapeType">Type of the shape to test.</param>
    ///// <param name="queryShapeData">Shape data to test.</param>
    ///// <param name="queryShapeSize">Size of the shape data in bytes.</param>
    ///// <param name="queryBoundsMin">Minimum of the query shape's bounding box.</param>
    ///// <param name="queryBoundsMax">Maximum of the query shape's bounding box.</param>
    ///// <param name="queryPose">Pose of the query shape.</param>
    ///// <param name="queryId">Id to use to refer to this query when the collision batcher finishes processing it.</param>
    ///// <param name="batcher">Batcher to add the query's tests to.</param>
    //public unsafe void AddQueryToBatch(int queryShapeType, void* queryShapeData, int queryShapeSize, System.Numerics.Vector3 queryBoundsMin, System.Numerics.Vector3 queryBoundsMax, in RigidPose queryPose, int queryId, ref CollisionBatcher<BatcherCallbacks> batcher)
    //{
    //    var broadPhaseEnumerator = new BroadPhaseOverlapEnumerator { Pool = BufferPool, References = new QuickList<CollidableReference>(16, BufferPool) };
    //    Simulation.BroadPhase.GetOverlaps(queryBoundsMin, queryBoundsMax, ref broadPhaseEnumerator);
    //    for (int overlapIndex = 0; overlapIndex < broadPhaseEnumerator.References.Count; ++overlapIndex)
    //    {
    //        GetPoseAndShape(broadPhaseEnumerator.References[overlapIndex], out var pose, out var shapeIndex);
    //        Simulation.Shapes[shapeIndex.Type].GetShapeData(shapeIndex.Index, out var shapeData, out _);
    //        //In this path, we assume that the incoming shape data is ephemeral. The collision batcher may last longer than the data pointer.
    //        //To avoid undefined access, we cache the query data into the collision batcher and use a pointer to the cache instead.
    //        batcher.CacheShapeB(shapeIndex.Type, queryShapeType, queryShapeData, queryShapeSize, out var cachedQueryShapeData);
    //        batcher.AddDirectly(
    //            shapeIndex.Type, queryShapeType,
    //            shapeData, cachedQueryShapeData,
    //            //Because we're using this as a boolean query, we use a speculative margin of 0. Don't care about negative depths.
    //            queryPose.Position - pose.Position, pose.Orientation, queryPose.Orientation, 0, new PairContinuation(queryId));
    //    }
    //    broadPhaseEnumerator.References.Dispose(BufferPool);
    //}

    ///// <summary>
    ///// Adds a shape query to the collision batcher.
    ///// </summary>
    ///// <typeparam name="TShape">Type of the query shape.</typeparam>
    ///// <param name="shape">Shape to use in the query.</param>
    ///// <param name="pose">Pose of the query shape.</param>
    ///// <param name="queryId">Id to use to refer to this query when the collision batcher finishes processing it.</param>
    ///// <param name="batcher">Batcher to add the query's tests to.</param>
    //public unsafe void AddQueryToBatch<TShape>(TShape shape, in RigidPose pose, int queryId, ref CollisionBatcher<BatcherCallbacks> batcher) where TShape : IConvexShape
    //{
    //    var queryShapeData = Unsafe.AsPointer(ref shape);
    //    var queryShapeSize = Unsafe.SizeOf<TShape>();
    //    shape.ComputeBounds(pose.Orientation, out var boundingBoxMin, out var boundingBoxMax);
    //    boundingBoxMin += pose.Position;
    //    boundingBoxMax += pose.Position;
    //    var test = (IShape)shape;
    //    AddQueryToBatch((int)shape.GetType().GetField("TypeId").GetValue(null), queryShapeData, queryShapeSize, boundingBoxMin, boundingBoxMax, pose, queryId, ref batcher); ;
    //    //AddQueryToBatch(shape.TypeId, queryShapeData, queryShapeSize, boundingBoxMin, boundingBoxMax, pose, queryId, ref batcher); ;
    //}

    ////This version of the query isn't used in the demo, but shows how you could use a simulation-cached shape in a query.
    ///// <summary>
    ///// Adds a shape query to the collision batcher.
    ///// </summary>
    ///// <typeparam name="TShape">Type of the query shape.</typeparam>
    ///// <param name="shape">Shape to use in the query.</param>
    ///// <param name="pose">Pose of the query shape.</param>
    ///// <param name="queryId">Id to use to refer to this query when the collision batcher finishes processing it.</param>
    ///// <param name="batcher">Batcher to add the query's tests to.</param>
    //public unsafe void AddQueryToBatch(Shapes shapes, TypedIndex queryShapeIndex, in RigidPose queryPose, int queryId, ref CollisionBatcher<BatcherCallbacks> batcher)
    //{
    //    var shapeBatch = shapes[queryShapeIndex.Type];
    //    shapeBatch.ComputeBounds(queryShapeIndex.Index, queryPose, out var queryBoundsMin, out var queryBoundsMax);
    //    Simulation.Shapes[queryShapeIndex.Type].GetShapeData(queryShapeIndex.Index, out var queryShapeData, out _);
    //    var broadPhaseEnumerator = new BroadPhaseOverlapEnumerator { Pool = BufferPool, References = new QuickList<CollidableReference>(16, BufferPool) };
    //    Simulation.BroadPhase.GetOverlaps(queryBoundsMin, queryBoundsMax, ref broadPhaseEnumerator);
    //    for (int overlapIndex = 0; overlapIndex < broadPhaseEnumerator.References.Count; ++overlapIndex)
    //    {
    //        GetPoseAndShape(broadPhaseEnumerator.References[overlapIndex], out var pose, out var shapeIndex);
    //        //Since both involved shapes are from the simulation cache, we don't need to cache them ourselves.
    //        Simulation.Shapes[shapeIndex.Type].GetShapeData(shapeIndex.Index, out var shapeData, out _);
    //        batcher.AddDirectly(
    //            shapeIndex.Type, queryShapeIndex.Type,
    //            shapeData, queryShapeData,
    //            //Because we're using this as a boolean query, we use a speculative margin of 0. Don't care about negative depths.
    //            queryPose.Position - pose.Position, queryPose.Orientation, pose.Orientation, 0, new PairContinuation(queryId));
    //    }
    //    broadPhaseEnumerator.References.Dispose(BufferPool);
    //}

    ////For the demo, we'll use a bunch of boxes as queries.
    //struct Query
    //{
    //    public Box Box;
    //    public RigidPose Pose;
    //}

    #endregion

}
