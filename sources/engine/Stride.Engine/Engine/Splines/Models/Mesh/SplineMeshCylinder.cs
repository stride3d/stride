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
            var splinePointCount = BezierPoints.Length;
            var vertexCount = splinePointCount * Sides;
            var indicesCount = (splinePointCount - 1) * Sides * 6;

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
            var splineDistance = 0.0f;

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
                    var angle = side * MathUtil.TwoPi / Sides;
                    var x = (float)Math.Cos(angle) * Radius;
                    var z = (float)Math.Sin(angle) * Radius;

                    Vector3 perpendicular = new Vector3(-direction.Z, 0, direction.X); // Perpendicular vector on the XZ plane
                    Vector3 sideVertexPosition = point.Position + perpendicular * x + Vector3.UnitY * Scale.Y * z;
                    Vector3 normal = CalculateRadialNormal(sideVertexPosition, point.Position);

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
                    var current = i * Sides + side;
                    var next = (side + 1) % Sides + i * Sides;
                    var currentNext = (i + 1) * Sides + side;
                    var nextNext = (i + 1) * Sides + (side + 1) % Sides;

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
                CloseCylinderEnds(Sides, splinePointCount, ref indicesIndex);
            }

            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: true);
        }

        private void CloseCylinderEnds(int sides, int splinePointCount, ref int indicesIndex)
        {
            int capVertexCount = sides + 1; // +1 for the center vertex of the cap
            int newVertexCount = vertices.Length + 2 * capVertexCount; // Add space for start and end cap vertices

            if (vertices.Length != newVertexCount)
            {
                Array.Resize(ref vertices, newVertexCount);
            }

            // Generate vertices for the start cap
            int startCapCenterIndex = splinePointCount * sides; // Center vertex index for the start cap
            Vector3 startCapCenter = BezierPoints[0].Position;
            CreateVertex(startCapCenterIndex, startCapCenter, Vector3.UnitY, new Vector2(0.5f, 0.5f));

            for (int side = 0; side < sides; side++)
            {
                vertices[startCapCenterIndex + 1 + side] = vertices[side]; // Copy the first ring of vertices
            }

            // Generate indices for the start cap
            for (int side = 0; side < sides; side++)
            {
                int nextSide = (side + 1) % sides;
                indices[indicesIndex++] = startCapCenterIndex;
                indices[indicesIndex++] = startCapCenterIndex + 1 + nextSide;
                indices[indicesIndex++] = startCapCenterIndex + 1 + side;
            }

            
   
            // Generate vertices for the end cap
            int endCapCenterIndex = startCapCenterIndex + capVertexCount; // Center vertex index for the end cap
            Vector3 endCapCenter = BezierPoints[splinePointCount - 1].Position;
            CreateVertex(endCapCenterIndex, endCapCenter, -Vector3.UnitY, new Vector2(0.5f, 0.5f));

            // Calculate the direction vector of the last segment of the spline
            Vector3 lastSegmentDirection = BezierPoints[splinePointCount - 1].Position - BezierPoints[splinePointCount - 2].Position;
            lastSegmentDirection.Normalize();

            // Create a perpendicular vector to the last segment direction
            Vector3 perpendicular = Vector3.Cross(lastSegmentDirection, Vector3.UnitY);
            perpendicular.Normalize();

            for (int side = 0; side < sides; side++)
            {
                float angle = side * MathUtil.TwoPi / sides;
                float x = (float)Math.Cos(angle) * Radius;
                float z = (float)Math.Sin(angle) * Radius;
                Vector3 offset = perpendicular * x + Vector3.Cross(perpendicular, lastSegmentDirection) * z; // Correctly oriented offset
                Vector3 sideVertexPosition = endCapCenter + offset;
                Vector3 normal = CalculateRadialNormal(sideVertexPosition, endCapCenter);
                CreateVertex(endCapCenterIndex + 1 + side, sideVertexPosition, normal, new Vector2((float)side / sides, 1.0f));
            }

            // Generate indices for the end cap
            for (int side = 0; side < sides; side++)
            {
                int nextSide = (side + 1) % sides;
                indices[indicesIndex++] = endCapCenterIndex;
                indices[indicesIndex++] = endCapCenterIndex + 1 + side;
                indices[indicesIndex++] = endCapCenterIndex + 1 + nextSide;
            }
        }
    }
}
