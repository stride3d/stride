//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;
using Stride.Graphics;

namespace Stride.Engine.Splines.Models
{
    [DataContract("SplineMeshSpline")]
    [Display("Spline")]
    public class SplineMeshSpline : SplineMesh
    {
        private VertexPositionNormalTexture[] vertices;
        private int[] indices;
        private BezierPoint[] shapePoints;

        public SplineComponent SplineComponent;

        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            if (SplineComponent == null)
            {
                return null;
            }
            else
            {
                shapePoints = RetrieveShapePoints();
            }

            var splinePointCount = bezierPoints.Length;
            var shapePointsCount = shapePoints.Length;
            var totalVertexCount = 4 * (shapePointsCount - 1) * (splinePointCount - 1);
            var totalIndicesCount = (totalVertexCount / 4) * 6;

            var verticesPerShapeCount = (shapePointsCount - 1) * 2;
            var indicesPerShapeCount = (shapePointsCount - 1) * 6;

            vertices = new VertexPositionNormalTexture[totalVertexCount];
            indices = new int[totalIndicesCount];

            var verticesIndex = 0;
            Vector3 posA, posB, posC, posD;

            for (int i = 0; i < splinePointCount; i++)
            {
                var startPoint = bezierPoints[i];
             
                for (int j = 0; j < shapePointsCount - 1; j++)
                {
                    var startShapePoint = shapePoints[j];
                    var targetShapePoint = shapePoints[j + 1];
                    var offset = targetShapePoint.Position - startShapePoint.Position;

                    ////First vertexes
                    if (j == 0)
                    {
                        posA = startPoint.Position;
                        posB = startPoint.Position + offset;
                        CreateVertex(verticesIndex++, posA, Vector3.UnitY, new Vector2(0, 0));
                        CreateVertex(verticesIndex++, posB, Vector3.UnitY, new Vector2(0, 0));
                    }
                    else
                    {
                        posA = vertices[verticesIndex-1].Position;
                        posB = vertices[verticesIndex-1].Position + offset;
                        CreateVertex(verticesIndex++, posA, Vector3.UnitY, new Vector2(0, 0));
                        CreateVertex(verticesIndex++, posB, Vector3.UnitY, new Vector2(0, 0));
                    }

                    if (i < splinePointCount - 1)
                    {
                        // Indices
                        var vertexIndex = i * verticesPerShapeCount * 2; //Huidige Spline iteratie, 6, 12, 18
                        var vertexShapeIndex = j * 2;

                        var indiceIndex = i * indicesPerShapeCount;
                        var triangleIndex = j * 6;

                        indices[indiceIndex + triangleIndex + 0] = vertexIndex + vertexShapeIndex + 0;
                        indices[indiceIndex + triangleIndex + 1] = vertexIndex + vertexShapeIndex + verticesPerShapeCount;
                        indices[indiceIndex + triangleIndex + 2] = vertexIndex + vertexShapeIndex + 1;

                        indices[indiceIndex + triangleIndex + 3] = vertexIndex + vertexShapeIndex + 1;
                        indices[indiceIndex + triangleIndex + 4] = vertexIndex + vertexShapeIndex + verticesPerShapeCount;
                        indices[indiceIndex + triangleIndex + 5] = vertexIndex + vertexShapeIndex + verticesPerShapeCount + 1;
                    }
                }
            }

            // Create the primitive object for further processing by the base class
            return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false);
        }

        private BezierPoint[] RetrieveShapePoints()
        {
            var totalNodesCount = SplineComponent.Spline?.SplineNodes.Count;
            var splineBezierPoints = new List<BezierPoint>();
            for (int i = 0; i < totalNodesCount; i++)
            {
                var currentSplineNodeComponent = SplineComponent.Spline.SplineNodes[i];
                var bezierPoints = currentSplineNodeComponent?.GetBezierPoints();
                if (bezierPoints == null)
                {
                    break;
                }

                splineBezierPoints.AddRange(i == 0 ? bezierPoints : bezierPoints[1..]);
            }
            return splineBezierPoints.ToArray();
        }

        private void CreateVertex(int verticesIndex, Vector3 position, Vector3 normal, Vector2 texture)
        {
            vertices[verticesIndex] = new VertexPositionNormalTexture(position, normal, texture);
        }
    }
}
