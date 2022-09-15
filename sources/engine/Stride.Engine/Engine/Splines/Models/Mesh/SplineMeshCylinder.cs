//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models
{
    [DataContract("SplineMeshCylinder")]
    [Display("Cylinder")]
    public class SplineMeshCylinder : SplineMesh
    {
        /// <summary>
        /// Generate geometry for endings
        /// </summary>
        public bool CloseEnd;

        private VertexPositionNormalTexture[] vertices;
        private int[] indices;
        private readonly Vector3[] normals = new Vector3[4]
        {
            -Vector3.UnitY, //Down
            -Vector3.UnitX, //Right
            Vector3.UnitY, //Up
            Vector3.UnitX // Left
        };

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
                vertexCount += 8;
                indicesCount += 12;
            }

            vertices = new VertexPositionNormalTexture[vertexCount];
            indices = new int[indicesCount];

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
                float textureY;

                // Create vertices
                var sides = new Vector3[4]
                {
                    left - halfHeigth, //Bottom left
                    right - halfHeigth, //Bottom right
                    right + halfHeigth, //Top right
                    left + halfHeigth // Top Left
                };

                if (i == 0) //First vertexes
                {
                    // Loop over each side in following order: Bottom, Right, Top, Left
                    for (int side = 0; side < sides.Length; side++)
                    {
                        CreateVertex(verticesIndex + 0, startPoint.Position + sides[side], normals[side], new Vector2(0, 0));
                        CreateVertex(verticesIndex + 1, startPoint.Position + sides[(side + 1) % 4], normals[side], new Vector2(1, 0));
                        verticesIndex += 2;
                    }
                }

                if (i == splinePointCount - 2 && Loop) //If Loop is enabled, then the target node is the first node in the entire spline
                {
                    splineDistance += Vector3.Distance(startPoint.Position, bezierPoints[0].Position);
                    textureY = splineDistance / UvScale.Y;

                    for (int side = 0; side < sides.Length; side++)
                    {
                        CreateVertex(verticesIndex + 0, vertices[side * 2 + 0].Position, normals[side], new Vector2(0, textureY));
                        CreateVertex(verticesIndex + 1, vertices[side * 2 + 1].Position, normals[side], new Vector2(1, textureY));
                        verticesIndex += 2;
                    }
                }
                else
                {
                    splineDistance += targetPoint.DistanceToPreviousPoint;
                    textureY = splineDistance / UvScale.Y;
                    for (int side = 0; side < sides.Length; side++)
                    {
                        CreateVertex(verticesIndex + 0, targetPoint.Position + sides[side + 0], normals[side], new Vector2(0, textureY));
                        CreateVertex(verticesIndex + 1, targetPoint.Position + sides[(side + 1) % 4], normals[side], new Vector2(1, textureY));
                        verticesIndex += 2;
                    }
                }


                // Create indices
                var indiceIndex = i * 24;

                //Bottom
                indices[indiceIndex + 0] = 0 + triangleIndex;
                indices[indiceIndex + 1] = 1 + triangleIndex;
                indices[indiceIndex + 2] = 8 + triangleIndex;

                indices[indiceIndex + 3] = 1 + triangleIndex;
                indices[indiceIndex + 4] = 9 + triangleIndex;
                indices[indiceIndex + 5] = 8 + triangleIndex;

                //Right
                indices[indiceIndex + 6] = 2 + triangleIndex;
                indices[indiceIndex + 7] = 3 + triangleIndex;
                indices[indiceIndex + 8] = 10 + triangleIndex;

                indices[indiceIndex + 9] = 3 + triangleIndex;
                indices[indiceIndex + 10] = 11 + triangleIndex;
                indices[indiceIndex + 11] = 10 + triangleIndex;

                //Top
                indices[indiceIndex + 12] = 4 + triangleIndex;
                indices[indiceIndex + 13] = 5 + triangleIndex;
                indices[indiceIndex + 14] = 12 + triangleIndex;

                indices[indiceIndex + 15] = 5 + triangleIndex;
                indices[indiceIndex + 16] = 13 + triangleIndex;
                indices[indiceIndex + 17] = 12 + triangleIndex;

                //Left
                indices[indiceIndex + 18] = 6 + triangleIndex;
                indices[indiceIndex + 19] = 7 + triangleIndex;
                indices[indiceIndex + 20] = 15 + triangleIndex;

                indices[indiceIndex + 21] = 6 + triangleIndex;
                indices[indiceIndex + 22] = 15 + triangleIndex;
                indices[indiceIndex + 23] = 14 + triangleIndex;

                triangleIndex += 8;

                // If this was the last loop, we do 1 additional check for Closing of the sides or looping the geometry
                if (i == splinePointCount - 2 && !Loop && CloseEnd)
                {
                    var backIndex = verticesIndex;
                    //Front face vertices
                    CreateVertex(verticesIndex + 0, vertices[0].Position, -Vector3.UnitZ, new Vector2(0, 0));
                    CreateVertex(verticesIndex + 1, vertices[1].Position, -Vector3.UnitZ, new Vector2(1, 0));
                    CreateVertex(verticesIndex + 2, vertices[4].Position, -Vector3.UnitZ, new Vector2(0, 1));
                    CreateVertex(verticesIndex + 3, vertices[5].Position, -Vector3.UnitZ, new Vector2(1, 1));

                    ////Back face vertices            
                    CreateVertex(verticesIndex + 4, vertices[backIndex - 8].Position, Vector3.UnitZ, new Vector2(0, 0));
                    CreateVertex(verticesIndex + 5, vertices[backIndex - 7].Position, Vector3.UnitZ, new Vector2(1, 0));
                    CreateVertex(verticesIndex + 6, vertices[backIndex - 4].Position, Vector3.UnitZ, new Vector2(0, 1));
                    CreateVertex(verticesIndex + 7, vertices[backIndex - 3].Position, Vector3.UnitZ, new Vector2(1, 1));

                    var closeIndicesIndex = indicesCount - 12;
                    var vertextCountIndex = vertexCount - 8;
                    //Front
                    indices[closeIndicesIndex + 0] = vertextCountIndex + 0;
                    indices[closeIndicesIndex + 1] = vertextCountIndex + 3;
                    indices[closeIndicesIndex + 2] = vertextCountIndex + 1;

                    indices[closeIndicesIndex + 3] = vertextCountIndex + 1;
                    indices[closeIndicesIndex + 4] = vertextCountIndex + 3;
                    indices[closeIndicesIndex + 5] = vertextCountIndex + 2;
                    closeIndicesIndex += 6;

                    //Back
                    var closeVerticesIndex = vertexCount - 4;
                    indices[closeIndicesIndex + 0] = closeVerticesIndex + 0;
                    indices[closeIndicesIndex + 1] = closeVerticesIndex + 1;
                    indices[closeIndicesIndex + 2] = closeVerticesIndex + 3;

                    indices[closeIndicesIndex + 3] = closeVerticesIndex + 1;
                    indices[closeIndicesIndex + 4] = closeVerticesIndex + 2;
                    indices[closeIndicesIndex + 5] = closeVerticesIndex + 3;
                }
            }

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false);
        }

        private void CreateVertex(int verticesIndex, Vector3 position, Vector3 normal, Vector2 texture)
        {
            vertices[verticesIndex] = new VertexPositionNormalTexture(position, normal, texture);
        }
    }
}
