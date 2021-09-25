// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos.Spline.Mesh;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Extensions;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
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
        private Entity gizmoBeziers;
        private Entity gizmoPoints;
        private Entity gizmoTangentOut;
        private Entity gizmoTangentIn;
        private Entity gizmoBoundingBox;

        private float updateFrequency = 1.2f;
        private float updateTimer = 0.0f;
        private bool boundingIter = false;

        private Material whiteMaterial;
        private Material redMaterial;
        private Material greenMaterial;
        private Material pinkMaterial;
        private Material blueMaterial;
        private Material boundingBoxMaterial;

        protected Vector3 StartClickPoint;
        protected Matrix StartWorldMatrix = Matrix.Identity;
        private bool dragStarted;

        public TranslationGizmo TranslationGizmo { get; private set; }
        public Vector2 startMousePosition { get; private set; }
        public Vector2 prevTotalMouseDrag { get; private set; }

        public SplineGizmo(EntityComponent component) : base(component, "Spline gizmo", GizmoResources.SplineGizmo)
        {
        }

        protected override Entity Create()
        {
            whiteMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.White);
            redMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Red);
            greenMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green);
            pinkMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Pink);
            blueMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Blue);
            boundingBoxMaterial = GizmoEmissiveColorMaterial.Create(GraphicsDevice, Color.Green);
            RenderGroup = RenderGroup.Group4;

            mainGizmoEntity = new Entity();
            gizmoNodes = new Entity();
            gizmoNodeLinks = new Entity();
            gizmoBeziers = new Entity();
            gizmoPoints = new Entity();
            gizmoTangentOut = new Entity();
            gizmoTangentIn = new Entity();
            gizmoBoundingBox = new Entity();



            // Add middle sphere
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, 0.3f, 48).ToMeshDraw();
            gizmoTangentOut = new Entity("TangentSphereOut") { new ModelComponent { Model = new Model { redMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            gizmoTangentIn = new Entity("TangentSphereIn") { new ModelComponent { Model = new Model { greenMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };

            mainGizmoEntity.AddChild(gizmoNodes);
            mainGizmoEntity.AddChild(gizmoNodeLinks);
            mainGizmoEntity.AddChild(gizmoBeziers);
            mainGizmoEntity.AddChild(gizmoPoints);
            mainGizmoEntity.AddChild(gizmoTangentOut);
            mainGizmoEntity.AddChild(gizmoTangentIn);
            mainGizmoEntity.AddChild(gizmoBoundingBox);



            TranslationGizmo = new TranslationGizmo();
            TranslationGizmo.IsEnabled = true;
            TranslationGizmo.AnchorEntity = gizmoTangentIn;



            Update();

            return mainGizmoEntity;
        }

        public override void Initialize(IServiceRegistry services, Scene editorScene)
        {
            base.Initialize(services, editorScene);
        }

        public override void Update()
        {
            //StartMousePosition = Input.MousePosition;
            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            updateTimer += deltaTime;

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
                ClearChildren(gizmoBeziers);
                ClearChildren(gizmoPoints);
                ClearChildren(gizmoTangentOut);
                ClearChildren(gizmoTangentIn);
                ClearChildren(gizmoBoundingBox);

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

                    if (i == totalNodesCount - 1 && !Component.Loop) //Dont debugdraw when it is the last node and Loop is disabled
                    {
                        break;
                    }

                    if (Component.DebugInfo.Segments || Component.DebugInfo.Points || Component.DebugInfo.BoundingBox)
                    {
                        var curve = curNode.GetBezierCurve();
                        if (curve == null) return;

                        var splinePointsInfo = curve.GetBezierPoints();

                        if (splinePointsInfo[0] == null)
                            break;

                        var splinePoints = new Vector3[splinePointsInfo.Length];
                        for (int j = 0; j < splinePointsInfo.Length; j++)
                        {
                            if (splinePointsInfo[j] == null)
                                break;
                            splinePoints[j] = splinePointsInfo[j].Position;
                        }

                        if (Component.DebugInfo.BoundingBox)
                        {
                            UpdateBoundingBox(curNode);
                        }

                        if (Component.DebugInfo.Points)
                        {
                            DrawSplinePoints(splinePoints);
                        }

                        if (Component.DebugInfo.Segments)
                        {
                            DrawSplineSegments(splinePoints.ToList());
                        }
                    }
                }
                Component.Dirty = false;
                GizmoRootEntity.Transform.LocalMatrix = ContentEntity.Transform.WorldMatrix;
                GizmoRootEntity.Transform.UseTRS = false;
            }

            ExperimentalStuffWithTangentTranslation(deltaTime);
        }

        private void ExperimentalStuffWithTangentTranslation(float deltaTime)
        {
            //magic stuff copied from translation gizmo
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();
            var gizmoMatrix = GizmoRootEntity.Transform.WorldMatrix;
            var gizmoViewInverse = Matrix.Invert(gizmoMatrix * cameraService.ViewMatrix);

            var clickRay = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, Input.MousePosition, gizmoViewInverse);

            var radius = 0.35f;

            Input.UnlockMousePosition();
            Game.IsMouseVisible = true;

            if (!dragStarted && !Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            { 
                gizmoTangentOut.Get<ModelComponent>().Model.Materials[0] = redMaterial;
            }


            if (new BoundingSphere(gizmoTangentOut.Transform.Position, radius).Intersects(ref clickRay))
            {
                gizmoTangentOut.Get<ModelComponent>().Model.Materials[0] = pinkMaterial;
                if (Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
                {
                    if (!dragStarted)
                    {
                        dragStarted = true;
                        startMousePosition = Input.MousePosition;
                    }
                }
            }

            if(dragStarted && Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            {
                var mousePosition = Input.MousePosition;
                var totalMouseDrag = mousePosition - startMousePosition;
                var newMouseDrag = totalMouseDrag - prevTotalMouseDrag;
                prevTotalMouseDrag = totalMouseDrag;

                var dragMultiplier = deltaTime * 500; //dragMultiplier
                Component.Nodes[0].TangentOut += new Vector3(-newMouseDrag.X * dragMultiplier, -newMouseDrag.Y * dragMultiplier, 0);
            }
            else if(dragStarted && !Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            {
                prevTotalMouseDrag = new Vector2(0);
                dragStarted = false;
            }

            if (new BoundingSphere(gizmoTangentIn.Transform.Position, radius).Intersects(ref clickRay))
            {
                gizmoTangentIn.Get<ModelComponent>().Model.Materials[0] = blueMaterial;
                if (Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
                {
                    Component.Nodes[0].TangentIn += 0.01f;
                }
            }
            else
            {
                gizmoTangentIn.Get<ModelComponent>().Model.Materials[0] = greenMaterial;
            }
        }

        private void UpdateBoundingBox(SplineNodeComponent curNode)
        {
            var boundingBoxMesh = new BoundingBoxMesh(GraphicsDevice);
            boundingBoxMesh.Build(curNode.BoundingBox);
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

        private void DrawSplineSegments(List<Vector3> splinePoints)
        {
            var splineMeshData = new SplineMeshData(splinePoints, GraphicsDevice);
            var debugLine = new Entity() { new ModelComponent { Model = new Model { greenMaterial, new Mesh { Draw = splineMeshData.Build() } }, RenderGroup = RenderGroup } };
            gizmoBeziers.AddChild(debugLine);
            debugLine.Transform.Position -= mainGizmoEntity.Transform.Position - debugLine.Transform.Position;
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

        private void DrawNodes(SplineNodeComponent splineNodeComponent)
        {
            var nodeMesh = new BulbMesh(GraphicsDevice, 0.2f);
            nodeMesh.Build();

            var node = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.White),
                        new Mesh { Draw = nodeMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoNodes.AddChild(node);
            node.Transform.Position += splineNodeComponent.Entity.Transform.Position;
        }

        private void DrawTangentOutwards(SplineNodeComponent splineNodeComponent)
        {
            gizmoTangentOut.Transform.Position = splineNodeComponent.TangentOut;
        }

        private void DrawTangentInwards(SplineNodeComponent splineNodeComponent)
        {
            gizmoTangentIn.Transform.Position = splineNodeComponent.TangentIn;
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
