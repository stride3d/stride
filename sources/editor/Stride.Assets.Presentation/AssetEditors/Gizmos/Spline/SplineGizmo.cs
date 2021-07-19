// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Presentation.AssetEditors.Gizmos.Spline.Mesh;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Rendering;

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
        private Entity gizmoSegments;
        private Entity gizmoPoints;
        private Entity gizmoTangentOut;
        private Entity gizmoTangentIn;
        private float updateFrequency = 1.2f;
        private float updateTimer = 0.0f;

        public SplineGizmo(EntityComponent component) : base(component, "Spline gizmo", GizmoResources.SplineGizmo)
        {
        }

        protected override Entity Create()
        {
            mainGizmoEntity = new Entity();
            gizmoNodes = new Entity();
            gizmoNodeLinks = new Entity();
            gizmoSegments = new Entity();
            gizmoPoints = new Entity();
            gizmoTangentOut = new Entity();
            gizmoTangentIn = new Entity();
            mainGizmoEntity.AddChild(gizmoNodes);
            mainGizmoEntity.AddChild(gizmoNodeLinks);
            mainGizmoEntity.AddChild(gizmoSegments);
            mainGizmoEntity.AddChild(gizmoPoints);
            mainGizmoEntity.AddChild(gizmoTangentOut);
            mainGizmoEntity.AddChild(gizmoTangentIn);
            return mainGizmoEntity;
        }

        public override void Update()
        {
            updateTimer += (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            if (updateTimer > updateFrequency)
            {
                updateTimer = 0;
                return;
            }

            // calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
            // except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);

            // Translation and Scale but no rotation on bounding boxes
            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = 1 * scale;
            GizmoRootEntity.Transform.UpdateWorldMatrix();
                        
            if (Component.Nodes?.Count > 1 && Component.Dirty)
            {
                ClearChildren(gizmoNodes);
                ClearChildren(gizmoNodeLinks);
                ClearChildren(gizmoSegments);
                ClearChildren(gizmoPoints);
                ClearChildren(gizmoTangentOut);
                ClearChildren(gizmoTangentIn);

                var totalNodesCount = Component.Nodes.Count;
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var curNode = Component.Nodes[i];
                    if (curNode == null)
                    {
                        break;
                    }

                    //allways draw
                    if (Component.DebugInfo.Nodes)
                    {
                        DrawNodes(curNode);
                    }

                    if (Component.DebugInfo.TangentOutwards)
                    {
                        DrawTangentOutwards(curNode);
                    }

                    if (Component.DebugInfo.TangentInwards)
                    {
                        DrawTangentInwards(curNode);
                    }

                    //Draw all, but last node
                    if (i < totalNodesCount - 1)
                    {
                        //if (Component.DebugInfo.NodesLink)
                        //{
                        //    DrawNodeLinks(curNode, Component.spline.BezierCurves[i+1]);
                        //}

                        if (Component.DebugInfo.Segments || Component.DebugInfo.Points)
                        {
                            var splinePointsInfo = curNode.GetBezierCurve().GetBezierPoints();
                            var splinePoints = new Vector3[splinePointsInfo.Length];
                            for (int j = 0; j < splinePointsInfo.Length; j++)
                            {
                                splinePoints[j] = splinePointsInfo[j].Position;
                            }

                            if (Component.DebugInfo.Points)
                            {
                                DrawSplinePoints(splinePoints);
                            }

                            if (Component.DebugInfo.Segments)
                            {
                                DrawSplineSegments(splinePoints);
                            }
                        }
                    }
                }

                Component.Dirty = false;
            }
        }

        private void DrawSplineSegments(Vector3[] splinePoints)
        {
            var localPoints = new Vector3[splinePoints.Length];
            for (int i = 0; i < splinePoints.Length; i++)
            {
                localPoints[i] = mainGizmoEntity.Transform.WorldToLocal(splinePoints[i]);
            }

            for (int i = 0; i < localPoints.Length - 1; i++)
            {
                var lineMesh = new LineMesh(GraphicsDevice);
                lineMesh.Build(new Vector3[2] { localPoints[i], localPoints[i + 1] - localPoints[i] });

                var segment = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, i % 2 == 0 ? Color.LightGoldenrodYellow: Color.LightCyan),
                            new Mesh { Draw = lineMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup,
                    }
                };

                gizmoSegments.AddChild(segment);
                segment.Transform.Position += localPoints[i];
            }
        }

        private void DrawSplinePoints(Vector3[] splinePoints)
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                var pointMesh = new BulbMesh(GraphicsDevice, 0.2f);
                pointMesh.Build();

                var point = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, Color.PeachPuff),
                            new Mesh { Draw = pointMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup,
                    }
                };

                point.Transform.Position = mainGizmoEntity.Transform.WorldToLocal(splinePoints[i]);
                gizmoPoints.AddChild(point);
            }
        }


        private void DrawNodes(SplineNodeComponent splineNodeComponent)
        {
            var nodeMesh = new BulbMesh(GraphicsDevice, 0.4f);
            nodeMesh.Build();

            var node = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightSkyBlue),
                        new Mesh { Draw = nodeMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoNodes.AddChild(node);
            node.Transform.Position += splineNodeComponent.Entity.Transform.Position;
        }

        private void DrawNodeLinks(SplineNodeComponent currentSplineNodeComponent, SplineNodeComponent nextSplineNodeComponent)
        {
            var nodeLinkLineMesh = new LineMesh(GraphicsDevice);
            nodeLinkLineMesh.Build(new Vector3[2] { currentSplineNodeComponent.Entity.Transform.Position, nextSplineNodeComponent.Entity.Transform.Position - currentSplineNodeComponent.Entity.Transform.Position });
            var nodeLink = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green),
                            new Mesh { Draw = nodeLinkLineMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup
                    }
                };
            gizmoNodeLinks.AddChild(nodeLink);
            nodeLink.Transform.Position += currentSplineNodeComponent.Entity.Transform.Position;
        }

        private void DrawTangentOutwards(SplineNodeComponent splineNodeComponent)
        {
            var outMesh = new BulbMesh(GraphicsDevice, 0.3f);
            outMesh.Build();

            var outEntity = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightGray),
                        new Mesh { Draw = outMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoTangentOut.AddChild(outEntity);
            outEntity.Transform.Position += splineNodeComponent.TangentOut;
        }

        private void DrawTangentInwards(SplineNodeComponent splineNodeComponent)
        {
            var inMesh = new BulbMesh(GraphicsDevice, 0.3f);
            inMesh.Build();

            var inEntity = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.HotPink),
                        new Mesh { Draw = inMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoTangentOut.AddChild(inEntity);
            inEntity.Transform.Position += splineNodeComponent.TangentIn;
        }

        private void ClearChildren(Entity entity)
        {
            var children = entity.GetChildren();
            foreach (var child in children)
            {
                entity.RemoveChild(child);
            }
        }
    }
}
