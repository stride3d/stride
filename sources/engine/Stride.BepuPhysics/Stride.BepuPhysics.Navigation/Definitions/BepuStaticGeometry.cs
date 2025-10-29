// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core.Mathematics;
using Stride.DotRecast.Definitions;
using Stride.Engine;

namespace Stride.BepuPhysics.Navigation.Definitions;

public class BepuStaticGeometry : DotRecastGeometryProvider
{
    public CollisionMask CollidersToInclude { get; set; } = CollisionMask.Everything;

    public override bool TryGetTransformedShapeInfo(Entity entity, out DotRecastShapeData shapeData)
    {
        var shapeCache = Services.GetService<ShapeCacheSystem>();
        var collidable = entity.Get<CollidableComponent>();
        List<BasicMeshBuffers> meshBuffer = [];
        List<ShapeTransform> transforms = [];
        List<(Matrix entity, int count)> matrices = [];

        // Only use StaticColliders for the nav mesh build.
        if (collidable is not StaticComponent)
        {
            shapeData = null;
            return false;
        }

        if (!CollidersToInclude.IsSet(collidable.CollisionLayer))
        {
            shapeData = null;
            return false;
        }

        collidable.Collider.AppendModel(meshBuffer, shapeCache, out object? _);
        int shapeCount = collidable.Collider.Transforms;
        for (int i = shapeCount - 1; i >= 0; i--)
            transforms.Add(default);
        collidable.Collider.GetLocalTransforms(collidable, CollectionsMarshal.AsSpan(transforms)[^shapeCount..]);
        matrices.Add((collidable.Entity.Transform.WorldMatrix, shapeCount));


        var verts = new List<Vector3>();
        var indices = new List<int>();
        for (int collidableI = 0, shapeI = 0; collidableI < matrices.Count; collidableI++)
        {
            var (collidableMatrix, _) = matrices[collidableI];
            collidableMatrix.Decompose(out _, out Matrix worldMatrix, out var translation);
            worldMatrix.TranslationVector = translation;

            for (int j = 0; j < shapeCount; j++, shapeI++)
            {
                var transform = transforms[shapeI];
                Matrix.Transformation(ref transform.Scale, ref transform.RotationLocal, ref transform.PositionLocal, out var localMatrix);
                var finalMatrix = localMatrix * worldMatrix;

                var shape = meshBuffer[shapeI];
                verts.EnsureCapacity(verts.Count + shape.Vertices.Length);
                indices.EnsureCapacity(indices.Count + shape.Indices.Length);

                int vertexBufferStart = verts.Count;

                for (int i = 0; i < shape.Indices.Length; i += 3)
                {
                    var index0 = shape.Indices[i];
                    var index1 = shape.Indices[i + 1];
                    var index2 = shape.Indices[i + 2];
                    indices.Add(vertexBufferStart + index0);
                    indices.Add(vertexBufferStart + index2);
                    indices.Add(vertexBufferStart + index1);
                }

                for (int l = 0; l < shape.Vertices.Length; l++)
                {
                    var vertex = shape.Vertices[l].Position;
                    Vector3.Transform(ref vertex, ref finalMatrix, out Vector3 transformedVertex);
                    verts.Add(transformedVertex);
                }
            }
        }

        shapeData = new DotRecastShapeData
        {
            Points = verts,
            Indices = indices
        };

        return true;
    }
}
