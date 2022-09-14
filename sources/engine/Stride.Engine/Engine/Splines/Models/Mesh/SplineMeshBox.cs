//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models
{
    [DataContract("SplineMeshBox")]
    [Display("Box")]
    public class SplineMeshBox : SplineMesh
    {
        /// <summary>
        /// Generate geometry for endings
        /// </summary>
        public bool CloseEnd;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            var splinePointCount = bezierPoints.Length;
            var vertexCount = splinePointCount * 4 * 2; // 4 sides * 2 per corner
            var indicesCount = (splinePointCount - 1) * 24;
            if (Loop)
            {
                vertexCount += 4;
                indicesCount += 24;
            }
            else if (CloseEnd)
            {
                indicesCount += 12;
            }

            var vertices = new VertexPositionNormalTexture[vertexCount];
            var indices = new int[indicesCount];

            var halfWidth = Scale.X / 2;
            var halfHeigth = new Vector3(0, Scale.Y / 2, 0);
            var verticesIndex = 0;
            var triangleIndex = 0;
            float splineDistance = 0.0f;

            for (int i = 0; i < splinePointCount - 1; i++)
            {
                var startPoint = bezierPoints[i];
                var targetPoint = bezierPoints[i + 1];
                var forward = (targetPoint.Position - startPoint.Position);
                forward.Normalize();
                var right = Vector3.Cross(forward, Vector3.UnitY) * halfWidth;
                var left = -right;

                // Todo: deal with up normal and rotation
                var upNormal = Vector3.UnitY;
                var leftNormal = Vector3.UnitX;
                float textureY;

                // Create vertices
                var a = left - halfHeigth;
                var b = right - halfHeigth;
                var c = right + halfHeigth;
                var d = left + halfHeigth;

                for (int k = 0; k < 4; k++)
                {

                    if (i == 0)
                    {
                        vertices[verticesIndex] = new VertexPositionNormalTexture(startPoint.Position + a, -upNormal, new Vector2(0, 0));
                        vertices[verticesIndex + 1] = new VertexPositionNormalTexture(startPoint.Position + b, -leftNormal, new Vector2(1, 0));
                        vertices[verticesIndex + 2] = new VertexPositionNormalTexture(startPoint.Position + c, upNormal, new Vector2(0, 0));
                        vertices[verticesIndex + 3] = new VertexPositionNormalTexture(startPoint.Position + d, leftNormal, new Vector2(1, 0));
                        verticesIndex += 4;
                    }

                    if (i == splinePointCount - 2 && Loop) //If Loop is enabled, then the target node is the first node in the entire spline
                    {
                        splineDistance += Vector3.Distance(startPoint.Position, bezierPoints[0].Position);
                        textureY = splineDistance / UvScale.Y;
                        vertices[verticesIndex] = new VertexPositionNormalTexture(vertices[0].Position, -upNormal, new Vector2(0, textureY));
                        vertices[verticesIndex + 1] = new VertexPositionNormalTexture(vertices[1].Position, -leftNormal, new Vector2(1, textureY));
                        vertices[verticesIndex + 2] = new VertexPositionNormalTexture(vertices[2].Position, upNormal, new Vector2(1, textureY));
                        vertices[verticesIndex + 3] = new VertexPositionNormalTexture(vertices[3].Position, leftNormal, new Vector2(1, textureY));
                    }
                    else
                    {
                        splineDistance += targetPoint.DistanceToPreviousPoint;
                        textureY = splineDistance / UvScale.Y;
                        vertices[verticesIndex] = new VertexPositionNormalTexture(targetPoint.Position + a, -upNormal, new Vector2(0, textureY));
                        vertices[verticesIndex + 1] = new VertexPositionNormalTexture(targetPoint.Position + b, -leftNormal, new Vector2(1, textureY));
                        vertices[verticesIndex + 2] = new VertexPositionNormalTexture(targetPoint.Position + c, upNormal, new Vector2(1, textureY));
                        vertices[verticesIndex + 3] = new VertexPositionNormalTexture(targetPoint.Position + d, leftNormal, new Vector2(1, textureY));
                        verticesIndex += 4;
                    }
                }

                // Create indices
                var indiceIndex = i * 24;

                //A
                indices[indiceIndex + 0] = 0 + triangleIndex;
                indices[indiceIndex + 1] = 1 + triangleIndex;
                indices[indiceIndex + 2] = 4 + triangleIndex;

                indices[indiceIndex + 3] = 1 + triangleIndex;
                indices[indiceIndex + 4] = 5 + triangleIndex;
                indices[indiceIndex + 5] = 4 + triangleIndex;

                //b
                indices[indiceIndex + 6] = 1 + triangleIndex;
                indices[indiceIndex + 7] = 2 + triangleIndex;
                indices[indiceIndex + 8] = 5 + triangleIndex;

                indices[indiceIndex + 9] = 2 + triangleIndex;
                indices[indiceIndex + 10] = 6 + triangleIndex;
                indices[indiceIndex + 11] = 5 + triangleIndex;

                //c
                indices[indiceIndex + 12] = 3 + triangleIndex;
                indices[indiceIndex + 13] = 6 + triangleIndex;
                indices[indiceIndex + 14] = 2 + triangleIndex;

                indices[indiceIndex + 15] = 3 + triangleIndex;
                indices[indiceIndex + 16] = 7 + triangleIndex;
                indices[indiceIndex + 17] = 6 + triangleIndex;

                //d
                indices[indiceIndex + 18] = 3 + triangleIndex;
                indices[indiceIndex + 19] = 0 + triangleIndex;
                indices[indiceIndex + 20] = 7 + triangleIndex;

                indices[indiceIndex + 21] = 0 + triangleIndex;
                indices[indiceIndex + 22] = 4 + triangleIndex;
                indices[indiceIndex + 23] = 7 + triangleIndex;

                triangleIndex += 4;

                // If this was the last loop, we do 1 additional check for Closing of the sides or looping the geometry
                if (i == splinePointCount - 2)
                {
                    if (Loop)
                    {

                    }
                    else if (CloseEnd)
                    {
                        var closeIndicesIndex = indicesCount - 12;

                        //Front
                        indices[closeIndicesIndex + 0] = 0;
                        indices[closeIndicesIndex + 1] = 3;
                        indices[closeIndicesIndex + 2] = 1;

                        indices[closeIndicesIndex + 3] = 1;
                        indices[closeIndicesIndex + 4] = 3;
                        indices[closeIndicesIndex + 5] = 2;
                        closeIndicesIndex += 6;

                        //Back
                        var closeVerticesIndex = vertexCount - 4;
                        indices[closeIndicesIndex + 0] = closeVerticesIndex;
                        indices[closeIndicesIndex + 1] = closeVerticesIndex + 1;
                        indices[closeIndicesIndex + 2] = closeVerticesIndex + 3;

                        indices[closeIndicesIndex + 3] = closeVerticesIndex + 1;
                        indices[closeIndicesIndex + 4] = closeVerticesIndex + 2;
                        indices[closeIndicesIndex + 5] = closeVerticesIndex + 3;
                    }


                }
            }

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false);
        }


    }
}
