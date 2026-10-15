// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Soft.Colliders;

/// <summary>
/// The sphere every particle of a <see cref="SoftBodyComponent"/> collides with.
/// </summary>
[DataContract]
public sealed class ParticleCollider : ICollider
{
    private float _radius;

    /// <summary>
    /// The radius of each particle, zero picks one from the spacing between particles.
    /// </summary>
    /// <remarks>
    /// Larger particles close the gaps other objects could slip through, but keep them further from the surface.
    /// </remarks>
    [DefaultValue(0f)]
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = MathF.Max(0f, value);
            Component?.TryUpdateFeatures();
        }
    }

    /// <summary> The radius the particles were created with </summary>
    [DataMemberIgnore]
    public float ActualRadius { get; private set; }

    internal CollidableComponent? Component { get; private set; }

    CollidableComponent? ICollider.Component
    {
        get => Component;
        set => Component = value;
    }

    // The particles move independently of the entity, debug views fixed to it cannot show them
    int ICollider.Transforms => 0;

    void ICollider.GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms)
    {
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, bool shouldCalculateInertia, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        centerOfMass = Vector3.Zero;
        inertia = default;
        if (Component is not SoftBodyComponent softBody || softBody.TryPrepareTopology(out var spacing) == false)
        {
            index = default;
            return false;
        }

        ActualRadius = Radius > 0f ? Radius : spacing * softBody.DefaultRadiusFraction;
        index = shapes.Add(new Sphere(ActualRadius));
        return true;
    }

    void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index)
    {
        shapes.Remove(index);
    }

    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cache)
    {
        cache = null;
    }

    void ICollider.RayTest<TRayHitHandler>(Shapes shapes, TypedIndex shapeIndex, in NRigidPose pose, in RayData ray, ref float maximumT, ref TRayHitHandler hitHandler, BufferPool pool)
    {
        if (hitHandler.AllowTest(0)
            && shapes.GetShape<Sphere>(shapeIndex.Index).RayTest(pose, ray.Origin, ray.Direction, out var t, out var normal)
            && t <= maximumT)
        {
            hitHandler.OnRayHit(ray, ref maximumT, t, normal, 0);
        }
    }
}
