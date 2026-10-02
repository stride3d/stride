// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Definitions.Heightfield;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

/// <inheritdoc cref="HeightfieldShape"/>
[DataContract]
public sealed class HeightfieldCollider : ICollider
{
    private static readonly Core.Diagnostics.Logger Log = Core.Diagnostics.GlobalLogger.GetLogger(nameof(HeightfieldCollider));

    private CollidableComponent? _component;
    private HeightfieldShape _shape;

    CollidableComponent? ICollider.Component { get => _component; set => _component = value; }

    /// <summary>
    /// The definition for this colliders' heightfield
    /// </summary>
    [DataMember]
    [MemberRequired(ReportAs = MemberRequiredReportType.Error)]
    public required IHeightfieldPhysicsSource Source
    {
        get;
        set
        {
            field = value;
            _component?.TryUpdateFeatures();
        }
    }

    int ICollider.Transforms => 1;

    void ICollider.GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms)
    {
        transforms[0].PositionLocal = Vector3.Zero;
        transforms[0].RotationLocal = Quaternion.Identity;
        transforms[0].Scale = Vector3.One;
    }

    unsafe bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, bool shouldCalculateInertia, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        Debug.Assert(_component is not null);

        index = default;
        centerOfMass = Vector3.Zero;
        inertia = default;

        float size = Source.Size;
        int subdivision = Source.Subdivision;
        float minHeight = Source.MinHeight;
        float maxHeight = Source.MaxHeight;

        if (size == 0f || subdivision == 0)
        {
            Log.Error($"{nameof(HeightfieldCollider)} needs a positive {nameof(size)} and subdivision ({size}, {subdivision})");
            return false;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(subdivision, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 0);
        float cellSize = size / subdivision;

        Source.GetColliderData(out var sampler, out var coarseBlocks, out var coarseBlocksSubdivision);

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(coarseBlocksSubdivision, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(coarseBlocksSubdivision, subdivision);
        int coarseBlockWidth = subdivision / coarseBlocksSubdivision;

        ArgumentNullException.ThrowIfNull(coarseBlocks);
        ArgumentOutOfRangeException.ThrowIfLessThan(coarseBlocks.Length, coarseBlocksSubdivision * coarseBlocksSubdivision);
        // We're pinning the array provided, this provides user with the ability to change the backing coarse blocks at runtime and have the collider sync,
        // to reflect the same ability the sampler provides
        var coarseBlocksHandle = GCHandle.Alloc(coarseBlocks, GCHandleType.Pinned);
        var coarseBlocksAddress = (HeightRange*)coarseBlocksHandle.AddrOfPinnedObject();

        ArgumentNullException.ThrowIfNull(sampler);
        var sourceHandle = GCHandle.Alloc(sampler, GCHandleType.Normal);

        HeightfieldShape shape;
        shape.Sampler = sourceHandle;
        shape.SampleInterval = cellSize;
        shape.Subdivision = subdivision;
        shape.MinHeight = minHeight;
        shape.MaxHeight = maxHeight;
        shape.CoarseBlocksAddress = coarseBlocksAddress;
        shape.CoarseBlocksGCHandle = coarseBlocksHandle;
        shape.CoarseBlocksSubdivision = coarseBlocksSubdivision;
        shape.CoarseBlockInterval = coarseBlockWidth;

        _shape = shape;
        index = shapes.Add(_shape);
        return true;
    }

    unsafe void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index)
    {
        shapes.Remove(index);
        if (_shape.Sampler.IsAllocated)
            _shape.Sampler.Free();
        _shape.Sampler = default;

        if (_shape.CoarseBlocksGCHandle.IsAllocated)
            _shape.CoarseBlocksGCHandle.Free();
        _shape.CoarseBlocksGCHandle = default;
        _shape.CoarseBlocksAddress = null;
    }

    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cacheOut)
    {
        // TODO
        buffer.Add(new BasicMeshBuffers());
        cacheOut = null;
    }

    void ICollider.RayTest<TRayHitHandler>(Shapes shapes, TypedIndex shapeIndex, in NRigidPose pose, in RayData ray, ref float maximumT, ref TRayHitHandler hitHandler, BufferPool pool)
    {
        Debug.Assert(shapeIndex.Type == HeightfieldShape.TypeId);
        shapes.GetShape<HeightfieldShape>(shapeIndex.Index).RayTest(pose, in ray, ref maximumT, pool, ref hitHandler);
    }
}
