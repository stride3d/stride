using BepuPhysics.Collidables;
using BepuPhysics;
using BepuUtilities.Memory;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core;
using Stride.Rendering;
using System.Diagnostics;
using Stride.BepuPhysics.Systems;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public class MeshCollider : ICollider
{
    private float _mass = 1f;
    private bool _closed = true;
    private Model _model = null!; // We have a 'required' guard making sure it is assigned

    private ContainerComponent? _container;
    private ShapeCacheSystem.Cache? _cache;
    ContainerComponent? ICollider.Container { get => _container; set => _container = value; }

    [MemberRequired(ReportAs = MemberRequiredReportType.Error)]
    public required Model Model
    {
        get => _model;
        set
        {
            _model = value;
            OnEditCallBack();
        }
    }

    public float Mass
    {
        get => _mass;
        set
        {
            if (_mass != value)
            {
                _mass = value;
                OnEditCallBack();
            }
        }
    }

    /// <summary>
    /// Physics assume that the mesh's surface doesn't have any holes. That all edges of the mesh are shared between two triangles.
    /// </summary>
    public bool Closed
    {
        get => _closed;
        set
        {
            if (_closed != value)
            {
                _closed = value;
                OnEditCallBack();
            }
        }
    }

    public int Transforms => 1;

    [DataMemberIgnore]
    public Action OnEditCallBack { get; set; }

    public MeshCollider()
    {
        OnEditCallBack = () => _container?.TryUpdateContainer();
    }

    public void GetLocalTransforms(ContainerComponent container, Span<ShapeTransform> transforms)
    {
        transforms[0].PositionLocal = Vector3.Zero;
        transforms[0].RotationLocal = Quaternion.Identity;
        transforms[0].Scale = ComputeMeshScale(container);
    }

    public static Vector3 ComputeMeshScale(ContainerComponent container)
    {
        container.Entity.Transform.UpdateWorldMatrix();
        return ShapeCacheSystem.GetClosestToDecomposableScale(container.Entity.Transform.WorldMatrix);
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        Debug.Assert(_container is not null);

        shapeCache.GetModelCache(Model, out _cache);
        var mesh = _cache.GetBepuMesh(ComputeMeshScale(_container));

        index = shapes.Add(mesh);
        inertia = Closed ? mesh.ComputeClosedInertia(Mass) : mesh.ComputeOpenInertia(Mass);
        centerOfMass = Vector3.Zero;
        //if (_containerComponent is BodyMeshContainerComponent _b)
        //{
        //    CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
        //}
        //else if (_containerComponent is StaticMeshContainerComponent _s)
        //{
        //    CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStrideVector();
        //}
        return true;
    }

    void ICollider.Detach(Shapes shapeCollection, BufferPool pool, TypedIndex index)
    {
        shapeCollection.Remove(index);
        _cache = null; // Let GC collect the cache
    }

    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cacheOut)
    {
        shapeCache.GetModelCache(Model, out var cache);
        BasicMeshBuffers data;
        cache.GetBuffers(out data.Vertices, out data.Indices);
        buffer.Add(data);
        cacheOut = cache;
    }
}