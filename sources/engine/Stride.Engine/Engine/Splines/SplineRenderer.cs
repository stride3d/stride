// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Engine.Splines.Models;
using Stride.Graphics;
using Stride.Core.Mathematics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Extensions;
using Stride.Rendering;

namespace Stride.Engine.Splines;

public class SplineRenderer
{
    public Entity Create([CanBeNull] Entity splineMeshEntity, Spline spline, SplineRenderSettings splineRenderSettings, GraphicsDevice graphicsDevice, Entity entity)
    {
        splineMeshEntity ??= new Entity("SplineRenderer");

        if (graphicsDevice == null || splineRenderSettings.SegmentsMaterial == null || spline == null)
            return splineMeshEntity;

        var nodes = spline.SplineNodes;

        if (nodes?.Count > 1)
        {
            var totalNodesCount = nodes.Count;
            for (var i = 0; i < totalNodesCount; i++)
            {
                var currentSplineNode = nodes[i];

                if (currentSplineNode == null)
                {
                    break;
                }

                // Show spline node meshes
                if (splineRenderSettings.ShowNodes)
                {
                    CreateSplineNodeMesh(i.ToString(), graphicsDevice, currentSplineNode.WorldPosition, entity, splineMeshEntity, splineRenderSettings);
                }

                // Don't create a mesh when it is the last node and Loop is disabled
                if (i == totalNodesCount - 1 && !spline.Loop)
                {
                    break;
                }

                if (!splineRenderSettings.ShowSegments && !splineRenderSettings.ShowBoundingBox)
                {
                    continue;
                }

                var curvePointsInfo = currentSplineNode.GetBezierPoints();

                if (curvePointsInfo?[0] == null)
                    break;

                var splinePoints = new Vector3[curvePointsInfo.Length];
                for (var j = 0; j < curvePointsInfo.Length; j++)
                {
                    if (curvePointsInfo[j] == null)
                        break;
                    splinePoints[j] = curvePointsInfo[j].Position;
                }

                // Show spline segment mesh
                if (splineRenderSettings.ShowSegments)
                {
                    CreateSegmentLineMesh(i.ToString(), splinePoints, graphicsDevice, entity, splineMeshEntity, splineRenderSettings);
                }

                // Show bounding box of spline segment
                if (splineRenderSettings.ShowBoundingBox)
                {
                    UpdateSegmentBoundingBoxMesh(i.ToString(), currentSplineNode.BoundingBox, graphicsDevice, entity, splineMeshEntity, splineRenderSettings);
                }
            }
        }

        // Show bounding box of entire splines
        if (splineRenderSettings.ShowBoundingBox)
        {
            UpdateSegmentBoundingBoxMesh("Spline", spline.BoundingBox, graphicsDevice, entity, splineMeshEntity, splineRenderSettings);
        }

        return splineMeshEntity;
    }

        private static void CreateSegmentLineMesh(string description, Vector3[] splinePoints, GraphicsDevice graphicsDevice, Entity splineEntity, Entity splineMeshEntity, SplineRenderSettings renderSettings)
        {
            var splineMeshData = new SplineMeshData();
            var segmentsEntity = new Entity($"Segment_{description}")
            {
                new ModelComponent { Model = new Model { renderSettings.SegmentsMaterial, new Mesh { Draw = splineMeshData.Build(splinePoints, graphicsDevice) }, }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false }
            };
            splineMeshEntity.AddChild(segmentsEntity);
            segmentsEntity.Transform.Position -= splineEntity.Transform.Position;
        }

        private static void UpdateSegmentBoundingBoxMesh(string description, BoundingBox boundingBox, GraphicsDevice graphicsDevice, Entity splineEntity, Entity splineMeshEntity,
            SplineRenderSettings renderSettings)
        {
            var boundingBoxMesh = new BoundingBoxMesh(graphicsDevice);
            boundingBoxMesh.Build(boundingBox);

            var boundingBoxMaterial = renderSettings.BoundingBoxMaterial ?? renderSettings.BoundingBoxMaterial ?? renderSettings.SegmentsMaterial;
            var boundingBoxEntity = new Entity($"BoundingBox_{description}")
            {
                new ModelComponent { Model = new Model { boundingBoxMaterial, new Mesh { Draw = boundingBoxMesh.MeshDraw } }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false }
            };
            splineMeshEntity.AddChild(boundingBoxEntity);
            boundingBoxEntity.Transform.Position -= splineEntity.Transform.Position;
        }

        private static void CreateSplineNodeMesh(string description, GraphicsDevice graphicsDevice, Vector3 splineNodeWorldPosition, Entity splineEntity, Entity splineMeshEntity,
            SplineRenderSettings renderSettings)
        {
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(graphicsDevice, 0.15f, 8).ToMeshDraw();
            var splineNodeMaterial = renderSettings.NodesMaterial ?? renderSettings.NodesMaterial ?? renderSettings.SegmentsMaterial;
            var splineNodeEntity = new Entity($"SplineNode_{description}")
            {
                new ModelComponent { Model = new Model { splineNodeMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup.Group4, IsShadowCaster = false }
            };
            splineMeshEntity.AddChild(splineNodeEntity);
            splineNodeEntity.Transform.Position -= splineEntity.Transform.WorldMatrix.TranslationVector - splineNodeWorldPosition;
        }
}
