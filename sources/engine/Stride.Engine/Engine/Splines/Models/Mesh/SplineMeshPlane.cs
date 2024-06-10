//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models.Mesh
{
    [DataContract("SplineMeshPlane")]
    [Display("Plane")]
    public class SplineMeshPlane : SplineMesh
    {
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            var splinePointCount = BezierPoints.Length;
            var vertexCount = splinePointCount * 2;
            var indexCount = (splinePointCount - 1) * 6;
            if (Loop)
            {
                vertexCount += 2;
                indexCount += 6;
            }

            var vertices = new VertexPositionNormalTexture[vertexCount];
            var indices = new int[indexCount];

            var halfWidth = Scale.X / 2;
            var verticesIndex = 0;
            var triangleIndex = 0;
            float splineDistance = 0.0f;

            for (int i = 0; i < splinePointCount - 1; i++)
            {
                var startPoint = BezierPoints[i];
                var targetPoint = BezierPoints[i + 1];
                var forward = (targetPoint.Position - startPoint.Position);
                forward.Normalize();
                var left = Vector3.Cross(forward, Vector3.UnitY) * halfWidth;
                var right = -left;
                var normal = Vector3.UnitY;
                float textureY;

                // Create vertices
                if (i == 0)
                {
                    vertices[verticesIndex] = new VertexPositionNormalTexture(startPoint.Position + left, normal, new Vector2(0, 0));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(startPoint.Position + right, normal, new Vector2(1, 0));
                    verticesIndex += 2;
                }

                splineDistance += targetPoint.DistanceToPreviousPoint;
                textureY = splineDistance / UvScale.Y;
                vertices[verticesIndex] = new VertexPositionNormalTexture(targetPoint.Position + left, normal, new Vector2(0, textureY));
                vertices[verticesIndex + 1] = new VertexPositionNormalTexture(targetPoint.Position + right, normal, new Vector2(1, textureY));
                verticesIndex += 2;

                // Create indices
                var indiceIndex = i * 6;
                SetIndices(indices, triangleIndex, indiceIndex);
                triangleIndex += 2;

                // If this was the last loop, we do 1 additional check for closing if spline is Loop
                if (i == splinePointCount - 2 && Loop)
                {
                    // Create vertices for closing the loop
                    splineDistance += Vector3.Distance(targetPoint.Position, BezierPoints[0].Position);
                    textureY = splineDistance / UvScale.Y;
                    vertices[verticesIndex] = new VertexPositionNormalTexture(BezierPoints[0].Position + left, normal, new Vector2(0, textureY));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(BezierPoints[0].Position + right, normal, new Vector2(1, textureY));

                    // Create indices for closing the loop
                    var loopIndicesIndex = (splinePointCount - 1) * 6;
                    SetIndices(indices, triangleIndex, loopIndicesIndex);
                }
            }

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false);
        }

        private static void SetIndices(int[] indices, int triangleIndex, int indiceIndex)
        {
            indices[indiceIndex + 0] = triangleIndex + 0;
            indices[indiceIndex + 1] = triangleIndex + 1;
            indices[indiceIndex + 2] = triangleIndex + 2;
            indices[indiceIndex + 3] = triangleIndex + 1;
            indices[indiceIndex + 4] = triangleIndex + 3;
            indices[indiceIndex + 5] = triangleIndex + 2;
        }
    }
}
