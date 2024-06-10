//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models.Mesh
{
    [DataContract("SplineMeshCylinder")]
    [Display("Cylinder")]
    public class SplineMeshCylinder : SplineMesh
    {
        /// <summary>
        /// The amount of sides 
        /// </summary>
        public int Sides = 16;

        /// <summary>
        /// The radius of the cylinder 
        /// </summary>
        public float Radius = 1.0f;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            int splinePointCount = BezierPoints.Length;
            int vertexCount = splinePointCount * Sides;
            int indicesCount = (splinePointCount - 1) * Sides * 6;

            if (Loop)
            {
                indicesCount += Sides * 6;
            }
            else if (CloseEnds && !Loop)
            {
                vertexCount += 2 * Sides; // Additional vertices for the start and end caps
                indicesCount += 6 * Sides; // Additional triangles for the caps
            }

            vertices = new VertexPositionNormalTexture[vertexCount];
            indices = new int[indicesCount];

            var verticesIndex = 0;
            var indicesIndex = 0;
            float splineDistance = 0.0f;

            for (int i = 0; i < splinePointCount; i++)
            {
                var point = BezierPoints[i];
                var nextPoint = BezierPoints[(i + 1) % splinePointCount];
                Vector3 direction = (nextPoint.Position - point.Position);
                direction.Normalize();

                float textureY = splineDistance / UvScale.Y;

                // Generate vertices around the spline point
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * MathUtil.TwoPi / Sides;
                    float x = (float)Math.Cos(angle) * Radius;
                    float z = (float)Math.Sin(angle) * Radius;

                    Vector3 perpendicular = new Vector3(-direction.Z, 0, direction.X); // Perpendicular vector on the XZ plane
                    Vector3 sideVertexPosition = point.Position + perpendicular * x + Vector3.UnitY * Scale.Y * z;
                    Vector3 normal = CalculateNormal(sideVertexPosition, point.Position);

                    CreateVertex(verticesIndex++, sideVertexPosition, normal, new Vector2((float)side / Sides, textureY));
                }

                if (i < splinePointCount - 1)
                {
                    splineDistance += Vector3.Distance(point.Position, BezierPoints[i + 1].Position);
                }
            }

            // Generating indices for each cylinder segment
            for (int i = 0; i < splinePointCount - 1; i++)
            {
                for (int side = 0; side < Sides; side++)
                {
                    int current = i * Sides + side;
                    int next = (side + 1) % Sides + i * Sides;
                    int currentNext = (i + 1) * Sides + side;
                    int nextNext = (i + 1) * Sides + (side + 1) % Sides;

                    indices[indicesIndex++] = current;
                    indices[indicesIndex++] = nextNext;
                    indices[indicesIndex++] = currentNext;

                    indices[indicesIndex++] = current;
                    indices[indicesIndex++] = next;
                    indices[indicesIndex++] = nextNext;
                }
            }

            // Close the cylinder ends 
            if (CloseEnds && !Loop)
            {
                CloseCylinderEnds(Sides, splinePointCount, vertices, indices, ref indicesIndex);
            }

            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: true);
        }

        private Vector3 CalculateNormal(Vector3 vertexPosition, Vector3 centerPosition)
        {
            Vector3 radialVector = vertexPosition - centerPosition;
            radialVector.Normalize();
            return radialVector;
        }

        private void CloseCylinderEnds(int sides, int splinePointCount, VertexPositionNormalTexture[] vertices, int[] indices, ref int indicesIndex)
        {
            // int startCapVertexOffset = vertices.Length - 2 * sides;
            // int endCapVertexOffset = vertices.Length - sides;
            //
            // Vector3 startCenter = BezierPoints[0].Position;
            // Vector3 endCenter = BezierPoints[splinePointCount - 1].Position;
            //
            // // Generate vertices for the caps
            // for (int side = 0; side < sides; side++)
            // {
            //     float angle = side * MathUtil.TwoPi / sides;
            //     float x = (float)Math.Cos(angle) * Scale.X / 2;
            //     float z = (float)Math.Sin(angle) * Scale.X / 2;
            //
            //     // Start cap vertices
            //     Vector3 startCapPosition = startCenter + new Vector3(x, 0, z);
            //     Vector3 startNormal = -Vector3.UnitY; // Normal pointing inward for the cap
            //     vertices[startCapVertexOffset + side] = new VertexPositionNormalTexture(startCapPosition, startNormal, new Vector2((float)side / sides, 0));
            //
            //     // End cap vertices
            //     Vector3 endCapPosition = endCenter + new Vector3(x, 0, z);
            //     Vector3 endNormal = Vector3.UnitY; // Normal pointing outward for the cap
            //     vertices[endCapVertexOffset + side] = new VertexPositionNormalTexture(endCapPosition, endNormal, new Vector2((float)side / sides, 1));
            // }
            //
            // // Generate indices for the start cap
            // int startCenterVertexIndex = startCapVertexOffset + sides;
            // vertices[startCenterVertexIndex] = new VertexPositionNormalTexture(startCenter, -Vector3.UnitY, new Vector2(0.5f, 0.5f));
            // for (int side = 0; side < sides; side++)
            // {
            //     int nextSide = (side + 1) % sides;
            //
            //     indices[indicesIndex++] = startCenterVertexIndex; // Center vertex of the start cap
            //     indices[indicesIndex++] = startCapVertexOffset + nextSide;
            //     indices[indicesIndex++] = startCapVertexOffset + side;
            // }
            //
            // // Generate indices for the end cap
            // int endCenterVertexIndex = endCapVertexOffset + sides;
            // vertices[endCenterVertexIndex] = new VertexPositionNormalTexture(endCenter, Vector3.UnitY, new Vector2(0.5f, 0.5f));
            // for (int side = 0; side < sides; side++)
            // {
            //     int nextSide = (side + 1) % sides;
            //
            //     indices[indicesIndex++] = endCenterVertexIndex; // Center vertex of the end cap
            //     indices[indicesIndex++] = endCapVertexOffset + side;
            //     indices[indicesIndex++] = endCapVertexOffset + nextSide;
            // }
        }
    }
}
