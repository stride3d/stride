using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Physics;
using Stride.Physics.Constraints;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(PhysicsConstraintComponent), false)]
    public class PhysicsConstraintGizmo : EntityGizmo<PhysicsConstraintComponent>
    {
        private static readonly Color OrangeUniformColor = new Color(0xFF, 0x98, 0x2B);
        private static readonly Color OrangeNegUniformColor = new Color(0xFF - 0xFF, 0xFF - 0x98, 0xFF - 0x2B);
        private static readonly Color PurpleUniformColor = new Color(0xB1, 0x24, 0xF2);
        private static readonly Color PurpleNegUniformColor = new Color(0xFF - 0xB1, 0xFF - 0x24, 0xFF - 0xF2);

        // Each time we create a primitive we allocate memory that won't be garbage collected without calling Dispose,
        // so we need a global cache of those per a graphics device (in case there's more than one).
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> SphereCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> ConeCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> CylinderCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);

        // Using the same render group as transformation gizmo which means constraint gizmo is visible while inside another mesh
        private static readonly RenderGroup GizmoRenderGroup = TransformationGizmo.TransformationGizmoGroup;

        private const float AxisConeRadius = 0.03f / 3f;
        private const float AxisConeHeight = 0.03f;
        private const float CenterSphereRadius = 0.015f;
        private const float CylinderLength = 0.3f;
        private const float CylinderRadius = 0.005f;
        private const int Tessellation = 16;

        private PivotMarker PivotA;
        private PivotMarker PivotB;

        public PhysicsConstraintGizmo(PhysicsConstraintComponent component) : base(component)
        {
            RenderGroup = GizmoRenderGroup;
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
            
            UpdatePivot(Pivot.A, ref PivotA, Component.BodyA, Component.Description.PivotInA, $"{ContentEntity.Name}_PivotInA");
            UpdatePivot(Pivot.B, ref PivotB, Component.BodyB, Component.Description.PivotInB, $"{ContentEntity.Name}_PivotInB");
        }

        private void UpdatePivot(
            Pivot pivotNum,
            ref PivotMarker pivotMarker,
            RigidbodyComponent rigidbody,
            Vector3 pivot,
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
                pivotMarker.Entity = new Entity(entityName);
                pivotMarker.ModelWrapper = ModelWrapper.Create(Component.Description, pivotNum, GraphicsDevice);
                pivotMarker.Entity.AddChild(pivotMarker.ModelWrapper.ModelEntity);

                this.GizmoRootEntity.AddChild(pivotMarker.Entity);
            }

            if (Component.Description.Type == pivotMarker.ModelWrapper.ConstraintType)
            {
                pivotMarker.ModelWrapper.Update(Component.Description);
            }
            else
            {
                // Type of descriptor has changed, we have to recreate the gizmo models
                pivotMarker.Entity.RemoveChild(pivotMarker.Entity.GetChild(0));
                pivotMarker.ModelWrapper = ModelWrapper.Create(Component.Description, pivotNum, GraphicsDevice);
                pivotMarker.Entity.AddChild(pivotMarker.ModelWrapper.ModelEntity);
            }

            // on each frame we'll update the transform of the entity
            // we compute the position of the pivot from the world position and rotation of the entity with rigidbody
            rigidbody.Entity.Transform.UpdateWorldMatrix();
            rigidbody.Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion rotation, out var position);
            rotation.Rotate(ref pivot);
            pivotMarker.Entity.Transform.Position = position + pivot;
            pivotMarker.Entity.Transform.Rotation = rotation;

            // we want the pivot marker to keep the same size irrespective of the scale of the rigidbody
            var targetScale = GizmoScalingEntity.Transform.Scale;
            pivotMarker.Entity.Transform.Scale = targetScale; // TODO: fix as this doesn't seem to be working?

            // and ensure the model is enabled
            pivotMarker.Enable(true);
        }

        private static GeometricPrimitive GetSphere(Graphics.GraphicsDevice device)
        {
            if (!SphereCache.TryGetValue(device, out var sphere))
            {
                sphere = GeometricPrimitive.Sphere.New(device, CenterSphereRadius, Tessellation);
                SphereCache.Add(device, sphere);
            }

            return sphere;
        }

        private static GeometricPrimitive GetCone(Graphics.GraphicsDevice device)
        {
            if (!ConeCache.TryGetValue(device, out var cone))
            {
                cone = GeometricPrimitive.Cone.New(device, AxisConeRadius, AxisConeHeight, Tessellation);
                ConeCache.Add(device, cone);
            }

            return cone;
        }

        private static GeometricPrimitive GetCylinder(Graphics.GraphicsDevice device)
        {
            if (!CylinderCache.TryGetValue(device, out var cylinder))
            {
                cylinder = GeometricPrimitive.Cylinder.New(device, CylinderLength, CylinderRadius, Tessellation);
                CylinderCache.Add(device, cylinder);
            }

            return cylinder;
        }

        private struct PivotMarker
        {
            public Entity Entity;
            public ModelWrapper ModelWrapper;
            
            /// <summary>
            /// Enable components in child entities - models.
            /// </summary>
            public void Enable(bool enabled)
            {
                if (Entity == null) return;

                foreach (var child in this.Entity.GetChildren())
                    child.EnableAll(enabled, applyOnChildren: true);
            }
        }

        private enum Pivot
        {
            A, B
        }

        private abstract class ModelWrapper
        {
            protected ModelWrapper(Pivot pivot) => Pivot = pivot;

            public Entity ModelEntity { get; } = new Entity("ModelWrapper");

            public Pivot Pivot { get; }

            public abstract ConstraintTypes ConstraintType { get; }

            public abstract void Update(IConstraintDesc constraintDesc);

            public static ModelWrapper Create(IConstraintDesc constraintDesc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
            {
                if (constraintDesc is Point2PointConstraintDesc p)
                    return new PointModelWrapper(p, pivot, graphicsDevice);
                else if (constraintDesc is HingeConstraintDesc h)
                    return new HingeModelWrapper(h, pivot, graphicsDevice);
                else if (constraintDesc is ConeTwistConstraintDesc ct)
                    return new ConeModelWrapper(ct, pivot, graphicsDevice);

                throw new NotSupportedException();
            }

            protected Entity AddModelEntity(string suffix, MeshDraw mesh, Material material, Quaternion rotation = default, Vector3 position = default)
            {
                var entity = new Entity($"Model_{suffix}");
                entity.Transform.Position = position;
                entity.Transform.Rotation = rotation;
                entity.Add(new ModelComponent
                {
                    Model = new Model { material, new Mesh { Draw = mesh } },
                    RenderGroup = GizmoRenderGroup,
                });

                ModelEntity.AddChild(entity);
                return entity;
            }
        }

        private sealed class PointModelWrapper : ModelWrapper
        {
            public PointModelWrapper(Point2PointConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot)
            {
                var sphere = GetSphere(graphicsDevice).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                AddModelEntity("Center", sphere, material);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.Point2Point;

            public override void Update(IConstraintDesc constraintDesc)
            {
                // model isn't changing
            }
        }

        private sealed class HingeModelWrapper : ModelWrapper
        {
            public HingeModelWrapper(HingeConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot)
            {
                // TODO: Add limit angles - create a procedular part of a disc given an angle
                var sphere = GetSphere(graphicsDevice).ToMeshDraw();
                var pipe = GetCylinder(graphicsDevice).ToMeshDraw();
                var tip = GetCone(graphicsDevice).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                var material2 = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeNegUniformColor : PurpleNegUniformColor);

                AddModelEntity("Center", sphere, material);

                var xRotation = Quaternion.RotationAxis(Vector3.UnitZ, -MathUtil.PiOverTwo); // Yup rotated towards X
                AddModelEntity("X", pipe, material, xRotation);
                AddModelEntity("Xend", tip, material, position: CylinderLength / 2f * Vector3.UnitX, rotation: xRotation);

                var zRotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo); // Yup rotated towards Z
                AddModelEntity("Zend", tip, material2, position: CylinderRadius * 4f * Vector3.UnitZ, rotation: zRotation);

                Update(desc);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.Hinge;

            public override void Update(IConstraintDesc constraintDesc)
            {
                var hingeDesc = (HingeConstraintDesc)constraintDesc;
                ModelEntity.Transform.Rotation = Pivot == Pivot.A ? hingeDesc.AxisInA : hingeDesc.AxisInB;
            }
        }

        private sealed class ConeModelWrapper : ModelWrapper
        {
            public ConeModelWrapper(ConeTwistConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot)
            {
                // TODO: Add limit angles - create a procedular part of a disc given an angle
                var sphere = GetSphere(graphicsDevice).ToMeshDraw();
                var pipe = GetCylinder(graphicsDevice).ToMeshDraw();
                var tip = GetCone(graphicsDevice).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                var material2 = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeNegUniformColor : PurpleNegUniformColor);

                AddModelEntity("Center", sphere, material);

                var xRotation = Quaternion.RotationAxis(Vector3.UnitZ, -MathUtil.PiOverTwo); // Yup rotated towards X
                AddModelEntity("X", pipe, material, xRotation);
                AddModelEntity("Xend", tip, material, position: CylinderLength / 2f * Vector3.UnitX, rotation: xRotation);

                var zRotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo); // Yup rotated towards Z
                AddModelEntity("Zend", tip, material2, position: CylinderRadius * 4f * Vector3.UnitZ, rotation: zRotation);

                Update(desc);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.ConeTwist;

            public override void Update(IConstraintDesc constraintDesc)
            {
                var hingeDesc = (ConeTwistConstraintDesc)constraintDesc;
                ModelEntity.Transform.Rotation = Pivot == Pivot.A ? hingeDesc.AxisInA : hingeDesc.AxisInB;
            }
        }
    }
}
