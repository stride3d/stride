// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using Stride.Core.Mathematics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;

namespace Stride.BepuPhysics.Definitions.Colliders;

public interface ICollider
{
    internal CollidableComponent? Component { get; set; }

    public int Transforms { get; }
    /// <summary>
    /// Fills in a span to transform <see cref="ICollider.AppendModel"/> from their neutral transform into the one specified by its collidable.
    /// </summary>
    /// <remarks>
    /// You must still transform this further into worldspace by using the world position and rotation the collidable's entity.
    /// </remarks>
    public void GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms);
    internal bool TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia);
    internal void Detach(Shapes shapes, BufferPool pool, TypedIndex index);
    internal void AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cache);
}
