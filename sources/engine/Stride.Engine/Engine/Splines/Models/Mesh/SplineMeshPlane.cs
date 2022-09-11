//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
            var vertexCount = bezierPoints.Length * 2;
            var vertices = new VertexPositionNormalTexture[vertexCount];

            var indexCount = (bezierPoints.Length - 1) * 6;
            var indices = new int[indexCount];

            var halfWidth = Scale.X / 2;
            var verticesIndex = 0;
            var triangleIndex = 0;
            for (int i = 0; i < bezierPoints.Length - 1; i++)
            {
                var startPoint = bezierPoints[i];
                var targetPoint = bezierPoints[i + 1];
                var forward = (targetPoint.Position - startPoint.Position);
                forward.Normalize();
                var right = Vector3.Cross(forward, Vector3.UnitY) * halfWidth;
                var left = -right;

                // Create custom vertices, in this case just a quad facing in Y direction
                var normal = Vector3.UnitY;

                if (i == 0)
                {
                    vertices[verticesIndex] = new VertexPositionNormalTexture(startPoint.Position + (right), normal, new Vector2(0, 0));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(startPoint.Position + (left), normal, new Vector2(1, 0));
                    verticesIndex += 2;
                }

                if (i == bezierPoints.Length - 2 && Loop)
                {
                    // asdfhasdhfaskdjfkadhs
                    targetPoint = bezierPoints[0];
                }
                else
                {
                    vertices[verticesIndex] = new VertexPositionNormalTexture(targetPoint.Position + (right), normal, new Vector2(0, 1));
                    vertices[verticesIndex + 1] = new VertexPositionNormalTexture(targetPoint.Position + (left), normal, new Vector2(1, 1));

                }
                verticesIndex += 2;

                // Create custom indices
                var indiceIndex = i * 6;
                if (i == bezierPoints.Length - 1 && Loop)
                {
                    indices[indiceIndex + 0] = triangleIndex + 0;
                    indices[indiceIndex + 1] = triangleIndex + 1;
                    indices[indiceIndex + 2] = triangleIndex + 2;
                    indices[indiceIndex + 3] = triangleIndex + 1;
                    indices[indiceIndex + 4] = triangleIndex + 3;
                    indices[indiceIndex + 5] = triangleIndex + 2;
                }
                else
                {

                    indices[indiceIndex + 0] = triangleIndex + 0;
                    indices[indiceIndex + 1] = triangleIndex + 1;
                    indices[indiceIndex + 2] = triangleIndex + 2;
                    indices[indiceIndex + 3] = triangleIndex + 1;
                    indices[indiceIndex + 4] = triangleIndex + 3;
                    indices[indiceIndex + 5] = triangleIndex + 2;
                }
                triangleIndex += 2;
            }

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false);
        }
    }
}
