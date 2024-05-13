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
        /// Generate geometry for endings
        /// </summary>
        public bool CloseEnd;

        /// <summary>
        /// The amount of sids 
        /// </summary>
        public int Sides = 16;

        private VertexPositionNormalTexture[] vertices;
        private int[] indices;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            int splinePointCount = bezierPoints.Length;
            int vertexCount = splinePointCount * Sides;
            int indicesCount = (splinePointCount - 1) * Sides * 6;

            if (Loop)
            {
                indicesCount += Sides * 6;
            }

            if (CloseEnd)
            {
                vertexCount += 2 * Sides; // Additional vertices for the start and end caps
                indicesCount += 3 * Sides; // Additional triangles for the caps
            }

            var vertices = new VertexPositionNormalTexture[vertexCount];
            var indices = new int[indicesCount];

            var verticesIndex = 0;
            var indicesIndex = 0;
            float splineDistance = 0.0f;

            for (int i = 0; i < splinePointCount; i++)
            {
                var point = bezierPoints[i];
                var nextPoint = bezierPoints[(i + 1) % splinePointCount];
                Vector3 direction = (nextPoint.Position - point.Position);
                direction.Normalize();

                float textureY = splineDistance / UvScale.Y;

                // Generate vertices around the spline point
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * MathUtil.TwoPi / Sides;
                    float x = (float)Math.Cos(angle) * Scale.X / 2;
                    float z = (float)Math.Sin(angle) * Scale.X / 2;

                    Vector3 perpendicular = new Vector3(-direction.Z, 0, direction.X); // Perpendicular vector on the XZ plane
                    Vector3 sideVertexPosition = point.Position + perpendicular * x + Vector3.UnitY * Scale.Y * z;
                    Vector3 normal = CalculateNormal(sideVertexPosition, point.Position);

                    vertices[verticesIndex++] = new VertexPositionNormalTexture(sideVertexPosition, normal, new Vector2((float)side / Sides, textureY));
                }

                if (i < splinePointCount - 1)
                {
                    splineDistance += Vector3.Distance(point.Position, bezierPoints[i + 1].Position);
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
            if (CloseEnd)
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
            //TODO 
        }
    }
}
