using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Physics.Constraints;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(ConstraintComponent), false)]
    public class PhysicsConstraintGizmo : EntityGizmo<ConstraintComponent>
    {
        private static readonly Color OrangeUniformColor = new Color(0xFF, 0x98, 0x2B);
        private static readonly Color PurpleUniformColor = new Color(0xB1, 0x24, 0xF2);

        private MeshDraw CenterSphereMesh;
        private MeshDraw CyliderAxisMesh;

        private PivotMarker PivotA;
        private PivotMarker PivotB;

        public PhysicsConstraintGizmo(ConstraintComponent component) : base(component)
        {
            RenderGroup = PhysicsShapesGroup;
        }

        protected override Entity Create()
        {
            // We want to scale pivots with the zoom. There's two of them, so we'll let
            // base.Update to set scale on this empty entity and later copy it over.
            GizmoScalingEntity = new Entity($"{ContentEntity.Name}_gizmo_scale");

            return new Entity($"Physics Constraint Gizmo Root Entity (id={ContentEntity.Id})");
        }

        public override void Update()
        {
            base.Update();

            if (Component.Description == null || !IsEnabled || !IsSelected)
            {
                PivotA.Enable(false);
                PivotB.Enable(false);
                return;
            }

            // reset root entity state, because we determine world positions of pivot entities below
            GizmoRootEntity.Transform.Position = Vector3.Zero;
            GizmoRootEntity.Transform.Rotation = Quaternion.Zero;
            
            UpdateConstraintRotation();

            UpdatePivot(ref PivotA, Component.BodyA, Component.Description.PivotInA, OrangeUniformColor, $"{ContentEntity.Name}_PivotInA");
            UpdatePivot(ref PivotB, Component.BodyB, Component.Description.PivotInB, PurpleUniformColor, $"{ContentEntity.Name}_PivotInB");
        }

        private void UpdateConstraintRotation()
        {
            if (Component.Description is IRotateConstraintDesc rotateDesc)
            {
                PivotA.ConstraintRotation = rotateDesc.AxisInA;
                PivotB.ConstraintRotation = rotateDesc.AxisInB;
            }
            else
            {
                PivotA.ConstraintRotation = Quaternion.Identity;
                PivotB.ConstraintRotation = Quaternion.Identity;
            }
        }

        private void UpdatePivot(
            ref PivotMarker pivotMarker,
            RigidbodyComponent rigidbody,
            Vector3 pivot,
            Color markerColor,
            string entityName)
        {
            if (rigidbody == null)
            {
                pivotMarker.Enable(false);

                return;
            }

            // First we create a debug entity which will be reused by any rigidbody referenced
            if (pivotMarker.Entity == null)
            {
                CenterSphereMesh ??= GeometricPrimitive.Sphere.New(GraphicsDevice, 0.01f, 16).ToMeshDraw();
                CyliderAxisMesh ??= GeometricPrimitive.Cylinder.New(GraphicsDevice, 0.3f, 0.005f, 16).ToMeshDraw();

                var material = GizmoUniformColorMaterial.Create(GraphicsDevice, markerColor, false);
                var materialNeg = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Negate(markerColor), false);

                pivotMarker.Entity = new Entity(entityName);
                this.AddModelEntity(ref pivotMarker, "Center", CenterSphereMesh, material);
                this.AddModelEntity(ref pivotMarker, "Y", CyliderAxisMesh, material);
                this.AddModelEntity(ref pivotMarker, "X", CyliderAxisMesh, materialNeg, Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.PiOverTwo));
                this.AddModelEntity(ref pivotMarker, "Z", CyliderAxisMesh, material, Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo));
                this.AddModelEntity(ref pivotMarker, "Yend", CenterSphereMesh, material, position: 0.15f * Vector3.UnitY);
                this.AddModelEntity(ref pivotMarker, "Xend", CenterSphereMesh, material, position: 0.15f * Vector3.UnitX);
                this.AddModelEntity(ref pivotMarker, "Zend", CenterSphereMesh, materialNeg, position: 0.15f * Vector3.UnitZ);

                //var physicsComponent = new StaticColliderComponent
                //{
                //    Enabled = false,
                //    ColliderShape = new SphereColliderShape(false, 0.05f),
                //};
                //pivotMarker.Entity.Add(physicsComponent);

                // adding entity into the scene will cause the physics component to get picked up by the processor,
                // which we need for the AddDebugEntity call

                //EditorScene.Entities.Add(pivotMarker.Entity);
                this.GizmoRootEntity.AddChild(pivotMarker.Entity);

                //physicsComponent.AddDebugEntity(EditorScene, RenderGroup, true);
                //pivotMarker.Model = physicsComponent.DebugEntity.GetChild(0).Get<ModelComponent>();
            }

            // on each frame we'll update the transform of the entity
            // we're setting the pivot entity as a child of the rigidbody to have the correct local rotation
            //pivotMarker.Entity.SetParent(rigidbody.Entity);
            // we're dividing the pivot by the scale, because the constraint ignores it
            rigidbody.Entity.Transform.UpdateWorldMatrix();
            rigidbody.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion rotation, out var position);
            rotation.Rotate(ref pivot);
            pivotMarker.Entity.Transform.Position = position + pivot;
            pivotMarker.Entity.Transform.Rotation = pivotMarker.ConstraintRotation * rotation; // ???

            // we want the pivot marker to keep the same size irrespective of the scale of the rigidbody
            //rigidbody.Entity.Transform.WorldMatrix.Decompose(out var parentWorldScale, out _);
            var targetScale = GizmoScalingEntity.Transform.Scale;
            pivotMarker.Entity.Transform.Scale = targetScale; // / parentWorldScale;

            // and ensure the model is enabled
            pivotMarker.Enable(true);
        }

        private void AddModelEntity(ref PivotMarker pivotMarker, string suffix, MeshDraw mesh, Material material, Quaternion rotation = default, Vector3 position = default)
        {
            var entity = new Entity($"{pivotMarker.Entity.Name}_Model_{suffix}");
            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation;
            entity.Add(new ModelComponent
            {
                Model = new Model { material, new Mesh { Draw = mesh } },
                RenderGroup = RenderGroup,
            });

            pivotMarker.Entity.AddChild(entity);
        }

        private struct PivotMarker
        {
            public Entity Entity;
            public Quaternion ConstraintRotation;
            
            /// <summary>
            /// Enable components in child entities - models.
            /// </summary>
            public void Enable(bool enabled)
            {
                if (Entity == null) return;

                foreach (var child in this.Entity.GetChildren())
                    child.EnableAll(enabled);
            }
        }
    }
}
