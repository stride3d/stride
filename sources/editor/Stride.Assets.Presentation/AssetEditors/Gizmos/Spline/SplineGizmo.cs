// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Presentation.AssetEditors.Gizmos.Spline.Mesh;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo that displays connected spline nodes
    /// </summary>
    [GizmoComponent(typeof(SplineComponent), false)]
    public class SplineGizmo : BillboardingGizmo<SplineComponent>
    {
        private Entity mainGizmoEntity;
        private Entity gizmoNodes;
        private Entity gizmoNodeLinks;
        private Entity gizmoBeziers;
        private Entity gizmoPoints;
        private Entity gizmoBoundingBox;

        private float updateFrequency = 0.05f;
        private float updateTimer = 0.0f;
        private bool boundingIter = false;

        private Material splineMaterial;

        public SplineGizmo(EntityComponent component) : base(component, "Spline gizmo", GizmoResources.SplineGizmo)
        {
        }

        protected override Entity Create()
        {
            splineMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green);
            splineMaterial.Descriptor = new MaterialDescriptor();
            splineMaterial.Descriptor.Attributes.CullMode = CullMode.None;

            mainGizmoEntity = new Entity();
            gizmoNodes = new Entity();
            gizmoNodeLinks = new Entity();
            gizmoBeziers = new Entity();
            gizmoPoints = new Entity();
            gizmoBoundingBox = new Entity();

            mainGizmoEntity.AddChild(gizmoNodes);
            mainGizmoEntity.AddChild(gizmoNodeLinks);
            mainGizmoEntity.AddChild(gizmoBeziers);
            mainGizmoEntity.AddChild(gizmoPoints);
            mainGizmoEntity.AddChild(gizmoBoundingBox);

            Update();

            return mainGizmoEntity;
        }

        public override void Initialize(IServiceRegistry services, Scene editorScene)
        {
            base.Initialize(services, editorScene);
        }

        public override void Update()
        {
            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            updateTimer += deltaTime;

            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            if (updateTimer < updateFrequency)
            {
                return;
            }

            // calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
            // except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
            ContentEntity.Transform.WorldMatrix.Decompose(out var scale, out
            // calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
            // except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
            Quaternion rotation, out var translation);

            // Translation and Scale but no rotation on bounding boxes
            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = 1 * scale;
            GizmoRootEntity.Transform.UpdateWorldMatrix();
            var nodes = Component.Spline.splineNodes;

            if (nodes?.Count > 1 && Component.Spline.Dirty)
            {
                ClearChildren(gizmoNodes);
                ClearChildren(gizmoBeziers);
                ClearChildren(gizmoPoints);
                ClearChildren(gizmoBoundingBox);

                var totalNodesCount = nodes.Count;
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNode = nodes[i];

                    if (currentSplineNode == null)
                    {
                        break;
                    }

                    if (i == totalNodesCount - 1 && !Component.Spline.Loop) // Dont debugdraw when it is the last node and Loop is disabled
                    {
                        break;
                    }

                    if (Component.Spline.DebugInfo.Segments || Component.Spline.DebugInfo.Points || Component.Spline.DebugInfo.BoundingBox)
                    {
                        if (currentSplineNode == null) return;

                        var curvePointsInfo = currentSplineNode.GetBezierPoints();

                        if (curvePointsInfo?[0] == null)
                            break;

                        var splinePoints = new Vector3[curvePointsInfo.Length];
                        for (int j = 0; j < curvePointsInfo.Length; j++)
                        {
                            if (curvePointsInfo[j] == null)
                                break;
                            splinePoints[j] = curvePointsInfo[j].Position;
                        }

                        if (Component.Spline.DebugInfo.Segments)
                        {
                            DrawSplineSegments(splinePoints.ToList());
                        }

                        if (Component.Spline.DebugInfo.BoundingBox)
                        {
                            UpdateBoundingBox(currentSplineNode);
                        }

                        if (Component.Spline.DebugInfo.Points)
                        {
                            DrawSplinePoints(splinePoints);
                        }
                    }
                }

                Component.Spline.Dirty = false;
                GizmoRootEntity.Transform.LocalMatrix = ContentEntity.Transform.WorldMatrix;
                GizmoRootEntity.Transform.UseTRS = false;

                updateTimer = 0;
            }
        }

        private void DrawSplineSegments(List<Vector3> splinePoints)
        {
            var splineMeshData = new SplineMeshData(splinePoints, GraphicsDevice);
            var debugLine = new Entity() { new ModelComponent { Model = new Model { splineMaterial, new Mesh { Draw = splineMeshData.Build() } }, RenderGroup = RenderGroup } };
            gizmoBeziers.AddChild(debugLine);
            debugLine.Transform.Position -= mainGizmoEntity.Transform.Position - debugLine.Transform.Position;
        }

        private void UpdateBoundingBox(SplineNode splineNode)
        {
            var boundingBoxMesh = new BoundingBoxMesh(GraphicsDevice);
            boundingBoxMesh.Build(splineNode.BoundingBox);
            boundingIter = !boundingIter;

            var boundingBox = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, boundingIter?  Color.OrangeRed : Color.Green),
                        new Mesh { Draw = boundingBoxMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoBoundingBox.AddChild(boundingBox);
            boundingBox.Transform.Position -= mainGizmoEntity.Transform.Position - boundingBox.Transform.Position;
        }

        private void DrawSplinePoints(Vector3[] splinePoints)
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                var pointMesh = new BulbMesh(GraphicsDevice, 0.1f);
                pointMesh.Build();

                var point = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, Color.White),
                            new Mesh { Draw = pointMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup,
                    }
                };

                gizmoPoints.AddChild(point);
                point.Transform.Position = mainGizmoEntity.Transform.WorldToLocal(splinePoints[i]);
            }
        }

        private void ClearChildren(Entity entity)
        {
            var children = entity.GetChildren();
            foreach (var child in children)
            {
                entity?.RemoveChild(child);
            }
        }
    }
}
