// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using DotRecast.Core;
using DotRecast.Recast;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.DotRecast.Definitions;
using Stride.Engine;

namespace Stride.BepuPhysics.Navigation.Definitions;
public class BepuNavigationStaticCollider : BaseNavigationCollider
{
    private float[] _vertices;
    private int[] _triangles;

    public override float[] Bounds()
    {
        float[] bounds = [_vertices[0], _vertices[1], _vertices[2], _vertices[0], _vertices[1], _vertices[2]];
        for (int i = 3; i < _vertices.Length; i += 3)
        {
            bounds[0] = Math.Min(bounds[0], _vertices[i]);
            bounds[1] = Math.Min(bounds[1], _vertices[i + 1]);
            bounds[2] = Math.Min(bounds[2], _vertices[i + 2]);
            bounds[3] = Math.Max(bounds[3], _vertices[i]);
            bounds[4] = Math.Max(bounds[4], _vertices[i + 1]);
            bounds[5] = Math.Max(bounds[5], _vertices[i + 2]);
        }

        return bounds;
    }

    public override void Rasterize(RcHeightfield hf, RcContext context)
    {
        // TODO: check if volume matters for Dotrecase. If it does then we may want to check the sape types and determine the volume.

        for (int i = 0; i < _triangles.Length; i += 3)
        {
            RcRasterizations.RasterizeTriangle(context, _vertices, _triangles[i], _triangles[i + 1], _triangles[i + 2], area,
                hf, (int)MathF.Floor(flagMergeThreshold / hf.ch));
        }
    }

    public override void Initialize(Entity entity, IServiceRegistry services)
    {
        var shapeCache = services.GetService<ShapeCacheSystem>();
        var collidable = entity.Get<CollidableComponent>();
        List<BasicMeshBuffers> meshBuffer = [];
        List<ShapeTransform> transforms = [];
        List<(Matrix entity, int count)> matrices = [];

        // Only use StaticColliders for the nav mesh build.
        if (collidable is not StaticComponent)
        {
            throw new InvalidOperationException($"Entity ({entity.Name}) does not have a valid {nameof(StaticComponent)} attached");
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

        Span<Vector3> spanToPoints = CollectionsMarshal.AsSpan(verts);
        Span<float> reinterpretedPoints = MemoryMarshal.Cast<Vector3, float>(spanToPoints);

        _vertices = reinterpretedPoints.ToArray();
        _triangles = [.. indices];
    }
}
