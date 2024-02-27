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
            float splineDistance = 0.0f;

            for (int i = 0; i < splinePointCount - 1; i++)
            {
                var startPoint = bezierPoints[i];
                var targetPoint = bezierPoints[i + 1];
                var splineForward = targetPoint.Position - startPoint.Position;

                splineForward.Normalize();
                var left = Vector3.Cross(splineForward, Vector3.UnitY);
                var right = -left;  
                float textureY;

                for (int j = 0; j < shapePointsCount - 1; j++)
                {
                    var startShapePoint = shapePoints[j];
                    var targetShapePoint = shapePoints[j + 1];
                  
                    var shapeForward = (targetShapePoint.Position - startShapePoint.Position);
                    shapeForward.Normalize();
                    var normal = Vector3.Cross(shapeForward, Vector3.UnitY);
                    //First vertexes
                    if (j == 0)
                    {
                        var temp = right;
                        temp *= targetShapePoint.Position.X - startShapePoint.Position.X;
                        temp.Y += targetShapePoint.Position.Y - startShapePoint.Position.Y;
                        posA = startPoint.Position;
                        posB = startPoint.Position + temp;
                        posC = targetPoint.Position;
                        posD = targetPoint.Position + temp;

                        textureY = splineDistance / UvScale.Y;
                        CreateVertex(verticesIndex++, posA, normal, new Vector2(0, textureY));
                        CreateVertex(verticesIndex++, posB, normal, new Vector2(1, textureY));
                        CreateVertex(verticesIndex++, posC, normal, new Vector2(0, textureY));
                        CreateVertex(verticesIndex++, posD, normal, new Vector2(1, textureY));
                    }
                    else
                    {
                        var temp = right;
                        temp.X *= targetShapePoint.Position.X - startShapePoint.Position.X;
                        temp.Y += targetShapePoint.Position.Y - startShapePoint.Position.Y;
                        //right *= offset.X;
                        posA = vertices[verticesIndex - 3].Position;
                        posB = vertices[verticesIndex - 3].Position + temp;
                        posC = vertices[verticesIndex - 1].Position;
                        posD = vertices[verticesIndex - 1].Position + temp;
                        splineDistance += targetPoint.DistanceToPreviousPoint;
                        textureY = splineDistance / UvScale.Y;
                        CreateVertex(verticesIndex++, posA, normal, new Vector2(0, textureY));
                        CreateVertex(verticesIndex++, posB, normal, new Vector2(1, textureY));
                        CreateVertex(verticesIndex++, posC, normal, new Vector2(0, textureY));
                        CreateVertex(verticesIndex++, posD, normal, new Vector2(1, textureY));
                    }
                }
            }

            for (int i = 0; i < splinePointCount - 1; i++)
            {
                for (int j = 0; j < shapePointsCount - 1; j++)
                {
                    //if (j > 0)
                    {
                        // Indices
                        var vertexIndex = i * verticesPerShapeCount * 2; //Huidige Spline iteratie, 6, 12, 18
                        var vertexShapeIndex = j * 4;

                        var indiceIndex = i * indicesPerShapeCount;
                        var triangleIndex = j * 6;

                        indices[indiceIndex + triangleIndex + 0] = vertexIndex + vertexShapeIndex + 0;
                        indices[indiceIndex + triangleIndex + 1] = vertexIndex + vertexShapeIndex + 2;
                        indices[indiceIndex + triangleIndex + 2] = vertexIndex + vertexShapeIndex + 1;

                        indices[indiceIndex + triangleIndex + 3] = vertexIndex + vertexShapeIndex + 1;
                        indices[indiceIndex + triangleIndex + 4] = vertexIndex + vertexShapeIndex + 2;
                        indices[indiceIndex + triangleIndex + 5] = vertexIndex + vertexShapeIndex + 3;
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
