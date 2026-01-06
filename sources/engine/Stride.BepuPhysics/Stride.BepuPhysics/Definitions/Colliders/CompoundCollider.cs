// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Systems;
using Stride.Core.Annotations;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

/// <summary>
/// Represents a compound collider that combines multiple child colliders into a single collider.
/// </summary>
[DataContract]
public sealed class CompoundCollider : ICollider
{
    private readonly ListOfColliders _colliders;
    private CollidableComponent? _component;

    /// <summary>
    /// Gets the collection of child colliders that make up this compound collider.
    /// </summary>
    /// <value>A list of collider bases that form this compound shape.</value>
    [MemberCollection(NotNullItems = true)]
    [DataMember]
    public IList<ColliderBase> Colliders => _colliders;

    [DataMemberIgnore]
    public int Transforms => _colliders.Count;

    [DataMemberIgnore]
    CollidableComponent? ICollider.Component { get => _component; set => _component = value; }

#warning Norbo: What would be a good heuristic to automatically swap to big, we can provide an override for users if they know what they are doing, but I would like to have it choose automatically by default, I'm guessing it's not just a case of (child > 5) ? big : small
    public bool IsBig => false;

    public CompoundCollider()
    {
        _colliders = new() { Owner = this };
    }

    public void GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms)
    {
        for (int i = 0; i < _colliders.Count; i++)
        {
            var collider = _colliders[i];
            transforms[i].PositionLocal = collider.PositionLocal;
            transforms[i].RotationLocal = collider.RotationLocal;
            transforms[i].Scale = collider switch
            {
                BoxCollider box => box.Size,
                CapsuleCollider cap => Vector3.One,
                CylinderCollider cyl => new(cyl.Radius, cyl.Length, cyl.Radius),
                SphereCollider sph => new(sph.Radius),
                TriangleCollider tri => Vector3.One,
                ConvexHullCollider convex => Vector3.One,
                _ => throw new NotImplementedException($"Collider type {collider.GetType()} is missing in {nameof(GetLocalTransforms)}, please fill an issue or fix it"),
            };
        }
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        if (_colliders.Count == 0)
        {
            index = default;
            centerOfMass = default;
            inertia = default;
            return false;
        }

        var compoundBuilder = new CompoundBuilder(pool, shapes, _colliders.Count);
        try
        {
            foreach (var collider in _colliders)
            {
                var localTranslation = collider.PositionLocal;
                var localRotation = collider.RotationLocal;

                var compoundChildLocalPose = new NRigidPose(localTranslation.ToNumeric(), localRotation.ToNumeric());
                collider.AddToCompoundBuilder(shapeCache, pool, ref compoundBuilder, compoundChildLocalPose);
            }

            Buffer<CompoundChild> compoundChildren;
            System.Numerics.Vector3 shapeCenter;
            compoundBuilder.BuildDynamicCompound(out compoundChildren, out inertia, out shapeCenter);

            index = IsBig ? shapes.Add(new BigCompound(compoundChildren, shapes, pool)) : shapes.Add(new Compound(compoundChildren));
            centerOfMass = shapeCenter.ToStride();
        }
        finally
        {
            compoundBuilder.Dispose();
        }

        return true;
    }

    void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index)
    {
        foreach (var collider in _colliders)
            collider.OnDetach(pool);
        shapes.RemoveAndDispose(index, pool);
    }

    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cache)
    {
        cache = null;
        foreach (var collider in _colliders)
        {
            buffer.Add(collider switch
            {
                BoxCollider box => shapeCache._boxShapeData,
                CapsuleCollider cap => shapeCache.BuildCapsule(cap),
                CylinderCollider cyl => shapeCache._cylinderShapeData,
                SphereCollider sph => shapeCache._sphereShapeData,
                TriangleCollider tri => shapeCache.BuildTriangle(tri),
                ConvexHullCollider con => shapeCache.BorrowHull(con),
                _ => throw new NotImplementedException($"{nameof(ICollider.AppendModel)} could not handle '{collider.GetType()}', please file an issue or fix this"),
            });
        }
    }

    void ICollider.RayTest<TRayHitHandler>(Shapes shapes, TypedIndex shapeIndex, in NRigidPose pose, in RayData ray, ref float maximumT, ref TRayHitHandler hitHandler)
    {
        if (shapeIndex.Type == Compound.TypeId)
        {
            shapes.GetShape<Compound>(shapeIndex.Index).RayTest(pose, in ray, ref maximumT, shapes, ref hitHandler);
        }
        else
        {
            Debug.Assert(shapeIndex.Type == BigCompound.TypeId);
            shapes.GetShape<BigCompound>(shapeIndex.Index).RayTest(pose, in ray, ref maximumT, shapes, ref hitHandler);
        }
    }
}
