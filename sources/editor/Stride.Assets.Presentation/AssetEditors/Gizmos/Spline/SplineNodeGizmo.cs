using System;
using System.Collections.Generic;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(SplineNodeComponent), false)]
    public class SplineNodeGizmo : EntityGizmo<SplineNodeComponent>
    {
        private Entity mainGizmoEntity;
        private Entity gizmoTangentOut;
        private Entity gizmoTangentIn;

        private Material whiteMaterial;
        private Material redMaterial;
        private Material greenMaterial;
        private Material pinkMaterial;
        private Material blueMaterial;

        protected Vector3 StartClickPoint;
        protected Matrix StartWorldMatrix = Matrix.Identity;
        private bool dragStarted;

        public TranslationGizmo TranslationGizmo { get; private set; }
        public Vector2 startMousePosition { get; private set; }
        public Vector2 prevTotalMouseDrag { get; private set; }

        public SplineNodeGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            whiteMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.White);
            redMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Red);
            greenMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green);
            pinkMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Pink);
            blueMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Blue);
            RenderGroup = RenderGroup.Group4;

            mainGizmoEntity = new Entity();

            gizmoTangentOut = new Entity();
            gizmoTangentIn = new Entity();


            // Add middle sphere
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, 0.18f, 48).ToMeshDraw();
            gizmoTangentOut = new Entity("TangentSphereOut") { new ModelComponent { Model = new Model { redMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            gizmoTangentIn = new Entity("TangentSphereIn") { new ModelComponent { Model = new Model { greenMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };


            mainGizmoEntity.AddChild(gizmoTangentOut);


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


            if (ContentEntity == null || GizmoRootEntity == null)
                return;


            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);

            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = 1 * scale;
            GizmoRootEntity.Transform.UpdateWorldMatrix();

            gizmoTangentOut.Transform.Position = Component.TangentOut;
            gizmoTangentIn.Transform.Position = Component.TangentIn;

            if (Component.Dirty)
            {

                
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

            if (dragStarted && Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            {
                var mousePosition = Input.MousePosition;
                var totalMouseDrag = mousePosition - startMousePosition;
                var newMouseDrag = totalMouseDrag - prevTotalMouseDrag;
                prevTotalMouseDrag = totalMouseDrag;

                //var viewProjection = cameraService.ViewMatrix * cameraService.ProjectionMatrix;


                var dragMultiplier = deltaTime * 1500; //dragMultiplier
                Component.TangentOut += new Vector3(-newMouseDrag.X * dragMultiplier, -newMouseDrag.Y * dragMultiplier, 0);
            }
            else if (dragStarted && !Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            {
                prevTotalMouseDrag = new Vector2(0);
                dragStarted = false;
            }

            if (new BoundingSphere(gizmoTangentIn.Transform.Position, radius).Intersects(ref clickRay))
            {
                gizmoTangentIn.Get<ModelComponent>().Model.Materials[0] = blueMaterial;
                if (Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
                {
                    Component.TangentIn += 0.01f;
                }
            }
            else
            {
                gizmoTangentIn.Get<ModelComponent>().Model.Materials[0] = greenMaterial;
            }
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
