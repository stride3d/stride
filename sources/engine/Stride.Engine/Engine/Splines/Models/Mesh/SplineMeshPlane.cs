//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models
{
    [DataContract("SplineMeshPlane")]
    [Display("Plane")]
    public class SplineMeshPlane : SplineMesh
    {

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            var splinePointCount = bezierPoints.Length;
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
            var dist = bezierPoints.Select(a => a.DistanceToPreviousPoint);

            for (int i = 0; i < splinePointCount - 1; i++)
            {
                var startPoint = bezierPoints[i];
                var targetPoint = bezierPoints[i + 1];
                var forward = (targetPoint.Position - startPoint.Position);
                forward.Normalize();
                var right = Vector3.Cross(forward, Vector3.UnitY) * halfWidth;
                var left = -right;

                // Todo: deal with up normal and rotation
                var normal = Vector3.UnitY;
                float textureY;

                // Create vertices
                if (i == 0)
                {
                    vertices[verticesIndex] = new VertexPositionNormalTexture(startPoint.Position + (right), normal, new Vector2(0, 0));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(startPoint.Position + (left), normal, new Vector2(1, 0));
                    verticesIndex += 2;
                }
                
                if (i == splinePointCount - 2 && Loop) //If Loop is enabled, then the target node is the first node in the entire spline
                {
                    splineDistance += Vector3.Distance(startPoint.Position, bezierPoints[0].Position);
                    textureY = splineDistance / UvScale.Y;
                    vertices[verticesIndex] = new VertexPositionNormalTexture(vertices[0].Position, normal, new Vector2(0, textureY));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(vertices[1].Position, normal, new Vector2(1, textureY));
                }
                else
                {
                    splineDistance += targetPoint.DistanceToPreviousPoint;
                    textureY = splineDistance / UvScale.Y;
                    vertices[verticesIndex] = new VertexPositionNormalTexture(targetPoint.Position + (right), normal, new Vector2(0, textureY));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(targetPoint.Position + (left), normal, new Vector2(1, textureY));
                    verticesIndex += 2;
                }

                // Create indices
                var indiceIndex = i * 6;
                SetIndices(indices, triangleIndex, indiceIndex);
                triangleIndex += 2;

                // If this was the last loop, we do 1 additional check for closing if spline is Loop
                if (i == splinePointCount - 2 && Loop)
                {
                    SetIndices(indices, triangleIndex, indiceIndex + 6);
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
