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
using Stride.Engine;
using Stride.Games;
using Stride.Graphics.GeometricPrimitives;

using GeoMeshData = Stride.Graphics.GeometricMeshData<Stride.Graphics.VertexPositionNormalTexture>;

namespace Stride.BepuPhysics.Configurations;

[DataContract]
public class BepuSimulation
{
    private readonly List<SimulationUpdateComponent> _simulationUpdateComponents = new();

    internal ThreadDispatcher ThreadDispatcher { get; private set; }
    internal BufferPool BufferPool { get; private set; }

    internal CollidableProperty<MaterialProperties> CollidableMaterials { get; private set; } = new CollidableProperty<MaterialProperties>();
    internal ContactEventsManager ContactEvents { get; private set; }

    internal Dictionary<BodyHandle, IBodyContainer> BodiesContainers { get; } = new();
    internal Dictionary<StaticHandle, IStaticContainer> StaticsContainers { get; } = new();

    internal float RemainingUpdateTime { get; set; } = 0;

    /// <summary>
    /// Get the bepu Simulation /!\
    /// </summary>
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
        ContactEvents = new ContactEventsManager(ThreadDispatcher, BufferPool);

        var _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks() { CollidableMaterials = CollidableMaterials, ContactEvents = ContactEvents };
        var _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks() { CollidableMaterials = CollidableMaterials };
        var _solveDescription = new SolveDescription(1, 1);

        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);

        CollidableMaterials.Initialize(Simulation);
        ContactEvents.Initialize(Simulation);
        //CollisionBatcher = new CollisionBatcher<BatcherCallbacks>(BufferPool, Simulation.Shapes, Simulation.NarrowPhase.CollisionTaskRegistry, 0, DefaultBatcherCallbacks);
    }
    private void Clear()
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

    public List<BodyShapeData> GetShapeData(TypedIndex shapeIndex, bool toLeftHanded = true)
    {
        var shapes = new List<BodyShapeData>();
        AddShapeData(shapes, shapeIndex, toLeftHanded);
        return shapes;
    }
    private void AddShapeData(List<BodyShapeData> shapes, TypedIndex typeIndex, bool toLeftHanded = true)
    {
        var shapeType = typeIndex.Type;
        var shapeIndex = typeIndex.Index;

        switch (shapeType)
        {
            case 0:
                var sphere = Simulation.Shapes.GetShape<Sphere>(shapeIndex);
                shapes.Add(GetBodyShapeData(GetSphereVerts(sphere, toLeftHanded)));
                break;
            case 1:
                var capsule = Simulation.Shapes.GetShape<Capsule>(shapeIndex);
                shapes.Add(GetBodyShapeData(GetCapsuleVerts(capsule, toLeftHanded)));
                break;
            case 2:
                var box = Simulation.Shapes.GetShape<Box>(shapeIndex);
                shapes.Add(GetBodyShapeData(GetBoxVerts(box, toLeftHanded)));
                break;
            case 3:
                var triangle = Simulation.Shapes.GetShape<Triangle>(shapeIndex);
                var a = triangle.A.ToStrideVector();
                var b = triangle.B.ToStrideVector();
                var c = triangle.C.ToStrideVector();
                var shapeData = new BodyShapeData() { Points = new List<Vector3>() { a, b, c }, Indices = new List<int>() { 0, 1, 2 } };
                shapes.Add(shapeData);
                break;
            case 4:
                var cyliner = Simulation.Shapes.GetShape<Cylinder>(shapeIndex);
                shapes.Add(GetBodyShapeData(GetCylinderVerts(cyliner, toLeftHanded)));
                break;
            case 5:
                var convex = Simulation.Shapes.GetShape<ConvexHull>(shapeIndex);
                shapes.Add(GetConvexHullData(convex, toLeftHanded));
                break;
            case 6:
                var compound = Simulation.Shapes.GetShape<Compound>(shapeIndex);
                shapes.AddRange(GetCompoundData(compound, toLeftHanded));
                break;
            case 7:
                throw new NotImplementedException("BigCompounds are not implemented.");
            case 8:
                var mesh = Simulation.Shapes.GetShape<Mesh>(shapeIndex);
                shapes.Add(GetMeshData(mesh, toLeftHanded));
                break;
        }
    }

    private BodyShapeData GetBodyShapeData(GeoMeshData meshData, bool toLeftHanded = true)
    {
        BodyShapeData shapeData = new BodyShapeData();

        // Transform box points
        for (int i = 0; i < meshData.Vertices.Length; i++)
        {
            shapeData.Points.Add(meshData.Vertices[i].Position);
            shapeData.Normals.Add(meshData.Vertices[i].Normal);
        }

        if (meshData.IsLeftHanded)
        {
            // Copy indices with offset applied
            for (int i = 0; i < meshData.Indices.Length; i += 3)
            {
                shapeData.Indices.Add(meshData.Indices[i]);
                shapeData.Indices.Add(meshData.Indices[i + 2]);
                shapeData.Indices.Add(meshData.Indices[i + 1]);
            }
        }
        else
        {
            // Copy indices with offset applied
            for (int i = 0; i < meshData.Indices.Length; i++)
            {
                shapeData.Indices.Add(meshData.Indices[i]);
            }
        }

        return shapeData;
    }
    private List<BodyShapeData> GetCompoundData(Compound compound, bool toLeftHanded = true)
    {
        var shapeData = new List<BodyShapeData>();

        for (int i = 0; i < compound.ChildCount; i++)
        {
            var child = compound.GetChild(i);
            var startI = shapeData.Count;
            AddShapeData(shapeData, child.ShapeIndex);

            for (int ii = startI; ii < shapeData.Count; ii++)
            {
                var translatedData = shapeData[ii].Points.Select(e => Vector3.Transform(e, child.LocalOrientation.ToStrideQuaternion()) + child.LocalPosition.ToStrideVector()).ToArray();
                shapeData[ii].Points.Clear();
                shapeData[ii].Points.AddRange(translatedData);
            }
        }
        return shapeData;
    }
    private BodyShapeData GetConvexHullData(ConvexHull convex, bool toLeftHanded = true)
    {
        //Vector3 scale = Vector3.One;
        ////use Strides shape data
        //var entities = new List<Entity>();
        //entities.Add(Entity);
        //ConvexHullColliderComponent hullComponent = null;
        //do
        //{
        //    var ent = entities.First();
        //    entities.RemoveAt(0);

        //    hullComponent = ent.Get<ConvexHullColliderComponent>();
        //    if (hullComponent == null)
        //        entities.AddRange(ent.GetChildren());
        //    else
        //        scale = ent.Transform.Scale;
        //}
        //while (entities.Count != 0);

        //if (hullComponent == null)
        //    throw new Exception("A convex that doesn't have a convexHullCollider ?");

        //var shape = (ConvexHullColliderShapeDesc)hullComponent.Hull.Descriptions[0];
        //var test = shape.LocalOffset;
        BodyShapeData shapeData = new BodyShapeData();

//        for (int i = 0; i < shape.ConvexHulls[0][0].Count; i++)
//        {
//            shapeData.Points.Add(shape.ConvexHulls[0][0][i] * scale);
//            shapeData.Normals.Add(Vector3.Zero);//Edit code to get normals
//#warning scaling & normals!!
//        }

//        for (int i = 0; i < shape.ConvexHullsIndices[0][0].Count; i += 3)
//        {
//            shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i]);
//            shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i + 2]); // NOTE: Reversed winding to create left handed input
//            shapeData.Indices.Add((int)shape.ConvexHullsIndices[0][0][i + 1]);
//        }

        return shapeData;
    }
    private BodyShapeData GetMeshData(Mesh mesh, bool toLeftHanded = true)
    {
        var meshContainer = (IContainerWithMesh)this;

        if (meshContainer == null)
            throw new Exception("a mesh must be inside a MeshContainer");

        if (meshContainer.Model == null)
            return default;

        //var game = Services.GetService<IGame>();
        BodyShapeData shapeData = new(); // = GetMeshData(meshContainer.Model, game, meshContainer.Entity.Transform.Scale);

        //if (toLeftHanded)
        //    for (int i = 0; i < shapeData.Indices.Count; i += 3)
        //    {
        //        // NOTE: Reversed winding to create left handed input
        //        (shapeData.Indices[i + 1], shapeData.Indices[i + 2]) = (shapeData.Indices[i + 2], shapeData.Indices[i + 1]);
        //    }

        return shapeData;
    }

    private GeoMeshData GetBoxVerts(Box box, bool toLeftHanded = true)
    {
        var boxDescription = new Physics.BoxColliderShapeDesc()
        {
            Size = new Vector3(box.Width, box.Height, box.Length)
        };
        return GeometricPrimitive.Cube.New(boxDescription.Size, toLeftHanded: toLeftHanded);
    }
    private GeoMeshData GetCapsuleVerts(Capsule capsule, bool toLeftHanded = true)
    {
        var capsuleDescription = new Physics.CapsuleColliderShapeDesc()
        {
            Length = capsule.Length,
            Radius = capsule.Radius
        };
        return GeometricPrimitive.Capsule.New(capsuleDescription.Length, capsuleDescription.Radius, 8, toLeftHanded: toLeftHanded);
    }
    private GeoMeshData GetSphereVerts(Sphere sphere, bool toLeftHanded = true)
    {
        var sphereDescription = new Physics.SphereColliderShapeDesc()
        {
            Radius = sphere.Radius
        };
        return GeometricPrimitive.Sphere.New(sphereDescription.Radius, 16, toLeftHanded: toLeftHanded);
    }
    private GeoMeshData GetCylinderVerts(Cylinder cylinder, bool toLeftHanded = true)
    {
        var cylinderDescription = new Physics.CylinderColliderShapeDesc()
        {
            Height = cylinder.Length,
            Radius = cylinder.Radius
        };
        return GeometricPrimitive.Cylinder.New(cylinderDescription.Height, cylinderDescription.Radius, 32, toLeftHanded: toLeftHanded);
    }

    private static unsafe BodyShapeData GetStrideMeshData(Rendering.Model model, IGame game, Vector3 scale)
    {
        BodyShapeData bodyData = new BodyShapeData();
        int totalVertices = 0, totalIndices = 0;
        foreach (var meshData in model.Meshes)
        {
            totalVertices += meshData.Draw.VertexBuffers[0].Count;
            totalIndices += meshData.Draw.IndexBuffer.Count;
        }

        foreach (var meshData in model.Meshes)
        {
            var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
            var iBuffer = meshData.Draw.IndexBuffer.Buffer;
            byte[] verticesBytes = vBuffer.GetData<byte>(game.GraphicsContext.CommandList);
            byte[] indicesBytes = iBuffer.GetData<byte>(game.GraphicsContext.CommandList);

            if ((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
            {
                // returns empty lists if there is an issue
                return bodyData;
            }

            int vertMappingStart = bodyData.Points.Count;

            fixed (byte* bytePtr = verticesBytes)
            {
                var vBindings = meshData.Draw.VertexBuffers[0];
                int count = vBindings.Count;
                int stride = vBindings.Declaration.VertexStride;
                for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                {
                    var point = *(Vector3*)(bytePtr + vHead);
                    bodyData.Points.Add(point * scale);
                    bodyData.Normals.Add(Vector3.Zero);//Edit code to get normals
#warning scaling & normals
                }
            }

            fixed (byte* bytePtr = indicesBytes)
            {
                if (meshData.Draw.IndexBuffer.Is32Bit)
                {
                    foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        bodyData.Indices.Add(vertMappingStart + i);
                    }
                }
                else
                {
                    foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                    {
                        bodyData.Indices.Add(vertMappingStart + i);
                    }
                }
            }
        }

        return bodyData;
    }

}
