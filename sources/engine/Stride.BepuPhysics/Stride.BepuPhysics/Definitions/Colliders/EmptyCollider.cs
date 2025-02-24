// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class EmptyCollider : ICollider
{
    CollidableComponent? ICollider.Component { get; set; }

    [DataMemberIgnore]
    public Action OnEditCallBack { get; set; } = () => { };

    int ICollider.Transforms => 1; //We don't have collider, but we have a debug render (a sphere of a radius of 0.1 for now)

    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cache)
    {
        cache = null;
        buffer.Add(shapeCache._sphereShapeData);
    }

    void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index)
    {
    }

    void ICollider.GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms)
    {
        transforms[0].PositionLocal = Vector3.Zero;
        transforms[0].RotationLocal = Quaternion.Identity;
        transforms[0].Scale = new Vector3(0.1f);
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        index = new();
        centerOfMass = new();
        inertia = new Sphere(1).ComputeInertia(1);
        return true;
    }
}
