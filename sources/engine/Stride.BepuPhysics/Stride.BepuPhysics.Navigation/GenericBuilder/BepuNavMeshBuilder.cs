// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Navigation.Definitions;
using Stride.DotRecast;
using Stride.DotRecast.Definitions;

namespace Stride.BepuPhysics.Navigation.GenericBuilder;
public class BepuNavMeshBuilder
{

    /// <summary>
    /// Used for Bepu specific mesh building.
    /// </summary>
    /// <param name="navSettings"></param>
    /// <param name="input"></param>
    /// <param name="threads"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    internal static DtNavMesh CreateBepuNavMesh(RcNavMeshBuildSettings navSettings, AsyncMeshInput input, int threads, CancellationToken cancelToken)
    {
        // /!\ THIS IS NOT RUNNING ON THE MAIN THREAD /!\

        var verts = new List<VertexPosition3>();
        var indices = new List<int>();
        for (int collidableI = 0, shapeI = 0; collidableI < input.Matrices.Count; collidableI++)
        {
            var (collidableMatrix, shapeCount) = input.Matrices[collidableI];
            collidableMatrix.Decompose(out _, out Matrix worldMatrix, out var translation);
            worldMatrix.TranslationVector = translation;

            for (int j = 0; j < shapeCount; j++, shapeI++)
            {
                var transform = input.TransformsOut[shapeI];
                Matrix.Transformation(ref transform.Scale, ref transform.RotationLocal, ref transform.PositionLocal, out var localMatrix);
                var finalMatrix = localMatrix * worldMatrix;

                var shape = input.ShapeData[shapeI];
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
                    verts.Add(new(transformedVertex));
                }
            }
        }

        // Get the backing array of this list,
        // get a span to that backing array,
        var spanToPoints = CollectionsMarshal.AsSpan(verts);
        // cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
        var reinterpretedPoints = MemoryMarshal.Cast<VertexPosition3, float>(spanToPoints);
        SimpleGeomProvider geom = new(reinterpretedPoints.ToArray(), [.. indices]);

        return NavMeshBuilder.CreateNavMeshFromGeometry(navSettings, geom, threads, cancelToken);
    }
}
