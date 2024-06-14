// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
public sealed class MeshCollider : ICollider
{
    private float _mass = 1f;
    private bool _closed = true;
    private Model _model = null!; // We have a 'required' guard making sure it is assigned

    private CollidableComponent? _component;
    private ShapeCacheSystem.Cache? _cache;
    CollidableComponent? ICollider.Component { get => _component; set => _component = value; }

    [MemberRequired(ReportAs = MemberRequiredReportType.Error)]
    public required Model Model
    {
        get => _model;
        set
        {
            _model = value;
            _component?.TryUpdateFeatures();
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
                _component?.TryUpdateFeatures();
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
                _component?.TryUpdateFeatures();
            }
        }
    }

    public int Transforms => 1;

    public void GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms)
    {
        transforms[0].PositionLocal = Vector3.Zero;
        transforms[0].RotationLocal = Quaternion.Identity;
        transforms[0].Scale = ComputeMeshScale(collidable);
    }

    public static Vector3 ComputeMeshScale(CollidableComponent collidable)
    {
        collidable.Entity.Transform.UpdateWorldMatrix();
        return ShapeCacheSystem.GetClosestToDecomposableScale(collidable.Entity.Transform.WorldMatrix);
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        Debug.Assert(_component is not null);

        shapeCache.GetModelCache(Model, out _cache);
        var mesh = _cache.GetBepuMesh(ComputeMeshScale(_component));

        index = shapes.Add(mesh);
        inertia = Closed ? mesh.ComputeClosedInertia(Mass) : mesh.ComputeOpenInertia(Mass);
        centerOfMass = Vector3.Zero;
        //if (_containerComponent is BodyMeshContainerComponent _b)
        //{
        //    CenterOfMass = (_b.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStride();
        //}
        //else if (_containerComponent is StaticMeshContainerComponent _s)
        //{
        //    CenterOfMass = (_s.Closed ? mesh.ComputeClosedCenterOfMass() : mesh.ComputeOpenCenterOfMass()).ToStride();
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
