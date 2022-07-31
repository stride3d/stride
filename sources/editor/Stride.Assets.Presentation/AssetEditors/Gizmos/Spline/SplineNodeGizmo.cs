using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(SplineNodeComponent), false)]
    public class SplineNodeGizmo : EntityGizmo<SplineNodeComponent>
    {
        private Entity mainGizmoEntity;
        private Entity gizmoTangentOut;
        private Entity gizmoTangentIn;

        private Material outMaterial;
        private Material inMaterial;

        private readonly float tangentSphereRadius = 0.09f;

        private Vector3 previousInTangent;
        private Vector3 previousOutTangent;
        private Vector3 previousNodePosition;

        public SplineNodeGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            inMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightYellow);
            outMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightSalmon);

            RenderGroup = RenderGroup.Group4;

            mainGizmoEntity = new Entity();

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

            //Only update the tangents mesh if tangent positions have changed
            if (Component.SplineNode.TangentInLocal == previousInTangent && Component.SplineNode.TangentOutLocal == previousOutTangent
                && translation == previousNodePosition)
                return;

            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = new Vector3(1);
            GizmoRootEntity.Transform.UpdateWorldMatrix();

            gizmoTangentOut.Transform.Position = Component.SplineNode.TangentOutLocal;
            gizmoTangentIn.Transform.Position = Component.SplineNode.TangentInLocal;

            ClearChildren(gizmoTangentOut);
            ClearChildren(gizmoTangentIn);

            var tangentLineOutGoingMesh = new SplineMeshData(new Vector3[] { new Vector3(0), -Component.SplineNode.TangentOutLocal }, GraphicsDevice);
            var tangentLineOutGoing = new Entity() { new ModelComponent { Model = new Model { outMaterial, new Mesh { Draw = tangentLineOutGoingMesh.Build() } }, RenderGroup = RenderGroup } };
            gizmoTangentOut.AddChild(tangentLineOutGoing);

            var tangentLineInwardsMesh = new SplineMeshData(new Vector3[] { new Vector3(0), -Component.SplineNode.TangentInLocal }, GraphicsDevice);
            var tangentLineInwards = new Entity() { new ModelComponent { Model = new Model { inMaterial, new Mesh { Draw = tangentLineInwardsMesh.Build() } }, RenderGroup = RenderGroup } };
            gizmoTangentIn.AddChild(tangentLineInwards);

            previousInTangent = Component.SplineNode.TangentInLocal;
            previousOutTangent = Component.SplineNode.TangentOutLocal;
            previousNodePosition = translation;
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
