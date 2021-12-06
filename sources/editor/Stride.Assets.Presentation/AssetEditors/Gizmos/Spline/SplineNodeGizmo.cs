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
        private Material outMaterial;
        private Material inMaterial;

        protected Vector3 StartClickPoint;
        protected Matrix StartWorldMatrix = Matrix.Identity;
        private bool dragStarted;
        private float tangentSphereRadius = 0.09f;

        public Vector2 startMousePosition { get; private set; }
        public Vector2 prevTotalMouseDrag { get; private set; }

        public SplineNodeGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            whiteMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.White);
            inMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightYellow);
            outMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightSalmon);

            RenderGroup = RenderGroup.Group4;

            mainGizmoEntity = new Entity();

            // Add middle sphere
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, tangentSphereRadius, 48).ToMeshDraw();
            gizmoTangentOut = new Entity("TangentSphereOut") { new ModelComponent { Model = new Model { outMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            gizmoTangentIn = new Entity("TangentSphereIn") { new ModelComponent { Model = new Model { inMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };

            mainGizmoEntity.AddChild(gizmoTangentOut);
            mainGizmoEntity.AddChild(gizmoTangentIn);

            Update();

            return mainGizmoEntity;
        }

        public override void Initialize(IServiceRegistry services, Scene editorScene)
        {
            base.Initialize(services, editorScene);
        }

        public override void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            ContentEntity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var translation);

            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = 1 * scale;
            GizmoRootEntity.Transform.UpdateWorldMatrix();

            gizmoTangentOut.Transform.Position = Component.TangentOut;
            gizmoTangentIn.Transform.Position = Component.TangentIn;

            ClearChildren(gizmoTangentOut);
            ClearChildren(gizmoTangentIn);

            var tangentLineOutGoingMesh = new SplineMeshData(new List<Vector3> { new Vector3(0), -Component.TangentOut }, GraphicsDevice);
            var tangentLineOutGoing = new Entity() { new ModelComponent { Model = new Model { outMaterial, new Mesh { Draw = tangentLineOutGoingMesh.Build() } }, RenderGroup = RenderGroup } };
            gizmoTangentOut.AddChild(tangentLineOutGoing);

            var tangentLineInwardsMesh = new SplineMeshData(new List<Vector3> { new Vector3(0), -Component.TangentIn }, GraphicsDevice);
            var tangentLineInwards = new Entity() { new ModelComponent { Model = new Model { inMaterial, new Mesh { Draw = tangentLineInwardsMesh.Build() } }, RenderGroup = RenderGroup } };
            gizmoTangentIn.AddChild(tangentLineInwards);

            //var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            //ExperimentalStuffWithTangentTranslation(deltaTime);
        }

        private void ExperimentalStuffWithTangentTranslation(float deltaTime)
        {
            // Magic stuff copied from translation gizmo
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();
            var gizmoMatrix = GizmoRootEntity.Transform.WorldMatrix;
            var gizmoViewInverse = Matrix.Invert(gizmoMatrix * cameraService.ViewMatrix);

            var clickRay = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, Input.MousePosition, gizmoViewInverse);

            var radius = 0.35f;

            Input.UnlockMousePosition();
            Game.IsMouseVisible = true;

            if (!dragStarted && !Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            {
                gizmoTangentOut.Get<ModelComponent>().Model.Materials[0] = whiteMaterial;
            }


            if (new BoundingSphere(gizmoTangentOut.Transform.Position, radius).Intersects(ref clickRay))
            {
                gizmoTangentOut.Get<ModelComponent>().Model.Materials[0] = outMaterial;
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
                var dragMultiplier = deltaTime * 1500; // crappy dragMultiplier
                Component.TangentOut += new Vector3(-newMouseDrag.X * dragMultiplier, -newMouseDrag.Y * dragMultiplier, 0);
            }
            else if (dragStarted && !Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
            {
                prevTotalMouseDrag = new Vector2(0);
                dragStarted = false;
            }

            if (new BoundingSphere(gizmoTangentIn.Transform.Position, radius).Intersects(ref clickRay))
            {
                gizmoTangentIn.Get<ModelComponent>().Model.Materials[0] = inMaterial;
                if (Input.IsMouseButtonDown(Stride.Input.MouseButton.Left))
                {
                    Component.TangentIn += 0.01f;
                }
            }
            else
            {
                gizmoTangentIn.Get<ModelComponent>().Model.Materials[0] = whiteMaterial;
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
