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
        private static readonly Color LimitColor = new Color(0xac, 0xf4, 0xa4, 0x96);

        // Each time we create a primitive we allocate memory that won't be garbage collected without calling Dispose,
        // so we need a global cache of those per a graphics device (in case there's more than one).
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> SphereCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> ConeCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> CylinderCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);
        private static Dictionary<Graphics.GraphicsDevice, Dictionary<float, GeometricPrimitive>> LimitDiscCache = new Dictionary<Graphics.GraphicsDevice, Dictionary<float,GeometricPrimitive>>(1);

        // Using the same render group as transformation gizmo which means constraint gizmo is visible while inside another mesh
        private static readonly RenderGroup GizmoRenderGroup = TransformationGizmo.TransformationGizmoGroup;

        private const float AxisConeRadius = 0.03f / 3f;
        private const float AxisConeHeight = 0.03f;
        private const float CenterSphereRadius = 0.01f;
        private const float CylinderLength = 0.2f;
        private const float CylinderRadius = 0.005f;
        private const float LimitDiscRadius = 0.06f;
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
            
            // the constraint pivot is local to the center of rigidbody body mass
            var shapePosition = rigidbody.ColliderShape?.LocalOffset ?? Vector3.Zero;
            var shapeRotation = rigidbody.ColliderShape?.LocalRotation ?? Quaternion.Identity;
            var shapeLocalMatrix = Matrix.RotationQuaternion(shapeRotation) * Matrix.Translation(shapePosition);
            var shapeWorldMatrix = shapeLocalMatrix * rigidbody.Entity.Transform.WorldMatrix;
            // we don't want to scale the pivot model, or the pivot translation
            ResetScale(ref shapeWorldMatrix);

            // we want the pivot marker to receive scaling from the gizmo system
            var targetScale = GizmoScalingEntity.Transform.Scale;

            var pivotPosition = Matrix.Scaling(targetScale) * Matrix.Translation(pivot) * shapeWorldMatrix;

            pivotMarker.Entity.Transform.UseTRS = false;
            pivotMarker.Entity.Transform.LocalMatrix = pivotPosition;

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

        private static GeometricPrimitive GetLimitDisc(Graphics.GraphicsDevice device, float angle)
        {
            if (!LimitDiscCache.TryGetValue(device, out var cache))
            {
                cache = new Dictionary<float, GeometricPrimitive>();
                LimitDiscCache.Add(device, cache);
            }

            if (!cache.TryGetValue(angle, out var disc))
            {
                disc = GeometricPrimitive.Disc.New(device, LimitDiscRadius, angle, Tessellation);
                cache.Add(angle, disc);
            }

            return disc;
        }

        /// <summary>
        /// This method normalizes rows within top left 3x3 sub matrix, which causes the scale to become (1,1,1).
        /// </summary>
        private static void ResetScale(ref Matrix matrix)
        {
            var row1 = matrix.Right;
            row1.Normalize();
            matrix.Right = row1;

            var row2 = matrix.Up;
            row2.Normalize();
            matrix.Up = row2;

            var row3 = matrix.Backward;
            row3.Normalize();
            matrix.Backward = row3;
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
            protected ModelWrapper(Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
            {
                Pivot = pivot;
                GraphicsDevice = graphicsDevice;
            }

            protected Graphics.GraphicsDevice GraphicsDevice { get; }

            public Entity ModelEntity { get; } = new Entity("ModelWrapper");

            public Pivot Pivot { get; }

            public abstract ConstraintTypes ConstraintType { get; }

            public abstract void Update(IConstraintDesc constraintDesc);

            public static ModelWrapper Create(IConstraintDesc constraintDesc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
            {
                switch (constraintDesc)
                {
                    case Point2PointConstraintDesc p:
                        return new PointModelWrapper(p, pivot, graphicsDevice);
                    case HingeConstraintDesc h:
                        return new HingeModelWrapper(h, pivot, graphicsDevice);
                    case ConeTwistConstraintDesc ct:
                        return new ConeModelWrapper(ct, pivot, graphicsDevice);
                    case GearConstraintDesc g:
                        return new GearModelWrapper(g, pivot, graphicsDevice);
                    case SliderConstraintDesc s:
                        return new SliderModelWrapper(s, pivot, graphicsDevice);
                }

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
                : base(pivot, graphicsDevice)
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
            private Material limitMaterial;
            private ModelComponent lowerLimit;
            private ModelComponent upperLimit;
            private float lastLowerLimit;
            private float lastUpperLimit;

            public HingeModelWrapper(HingeConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot, graphicsDevice)
            {
                var sphere = GetSphere(graphicsDevice).ToMeshDraw();
                var pipe = GetCylinder(graphicsDevice).ToMeshDraw();
                var tip = GetCone(graphicsDevice).ToMeshDraw();
                lastLowerLimit = desc.Limit.SetLimit ? -desc.Limit.LowerLimit : MathF.PI;
                lastUpperLimit = desc.Limit.SetLimit ? desc.Limit.UpperLimit : MathF.PI;
                var limitLower = GetLimitDisc(graphicsDevice, lastLowerLimit).ToMeshDraw();
                var limitUpper = GetLimitDisc(graphicsDevice, lastUpperLimit).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                var material2 = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeNegUniformColor : PurpleNegUniformColor);
                limitMaterial = GizmoUniformColorMaterial.Create(graphicsDevice, LimitColor);

                AddModelEntity("Center", sphere, material);

                var xRotation = Quaternion.RotationZ(-MathUtil.PiOverTwo); // Yup rotated towards X
                AddModelEntity("X", pipe, material, xRotation);
                AddModelEntity("Xend", tip, material, position: CylinderLength / 2f * Vector3.UnitX, rotation: xRotation);

                var zRotation = Quaternion.RotationX(MathUtil.PiOverTwo); // Yup rotated towards Z
                AddModelEntity("Zend", tip, material2, position: CylinderRadius * 4f * Vector3.UnitZ, rotation: zRotation);

                lowerLimit = AddModelEntity("LowerLimit", limitLower, limitMaterial, Quaternion.RotationZ(MathUtil.PiOverTwo) * Quaternion.RotationX(MathUtil.PiOverTwo)).Get<ModelComponent>();
                upperLimit = AddModelEntity("UpperLimit", limitUpper, limitMaterial, Quaternion.RotationZ(-MathUtil.PiOverTwo) * Quaternion.RotationX(-MathUtil.PiOverTwo)).Get<ModelComponent>();

                Update(desc);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.Hinge;

            public override void Update(IConstraintDesc constraintDesc)
            {
                var hingeDesc = (HingeConstraintDesc)constraintDesc;
                ModelEntity.Transform.Rotation = Pivot == Pivot.A ? hingeDesc.AxisInA : hingeDesc.AxisInB;

                var newLowerLimit = hingeDesc.Limit.SetLimit ? -hingeDesc.Limit.LowerLimit : MathF.PI;
                var newUpperLimit = hingeDesc.Limit.SetLimit ? hingeDesc.Limit.UpperLimit : MathF.PI;

                if(newLowerLimit != lastLowerLimit)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newLowerLimit).ToMeshDraw();
                    lowerLimit.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    lastLowerLimit = newLowerLimit;
                }

                if (newUpperLimit != lastUpperLimit)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newUpperLimit).ToMeshDraw();
                    upperLimit.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    lastUpperLimit = newUpperLimit;
                }
            }
        }

        private sealed class ConeModelWrapper : ModelWrapper
        {
            private Material limitMaterial;
            private ModelComponent limitZ;
            private ModelComponent limitY;
            private ModelComponent limitT;
            private float lastLimitZ;
            private float lastLimitY;
            private float lastLimitT;

            public ConeModelWrapper(ConeTwistConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot, graphicsDevice)
            {
                var sphere = GetSphere(graphicsDevice).ToMeshDraw();
                var pipe = GetCylinder(graphicsDevice).ToMeshDraw();
                var tip = GetCone(graphicsDevice).ToMeshDraw();
                lastLimitZ = desc.Limit.SetLimit ? desc.Limit.SwingSpanZ : MathF.PI;
                lastLimitY = desc.Limit.SetLimit ? desc.Limit.SwingSpanY : MathF.PI;
                lastLimitT = desc.Limit.SetLimit ? 2 * desc.Limit.TwistSpan : 2 * MathF.PI;
                var limitZ = GetLimitDisc(graphicsDevice, lastLimitZ).ToMeshDraw();
                var limitY = GetLimitDisc(graphicsDevice, lastLimitY).ToMeshDraw();
                var limitT = GetLimitDisc(graphicsDevice, lastLimitT).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                var material2 = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeNegUniformColor : PurpleNegUniformColor);
                limitMaterial = GizmoUniformColorMaterial.Create(graphicsDevice, LimitColor);

                AddModelEntity("Center", sphere, material);

                var xRotation = Quaternion.RotationAxis(Vector3.UnitZ, -MathUtil.PiOverTwo); // Yup rotated towards X
                AddModelEntity("X", pipe, material, xRotation);
                AddModelEntity("Xend", tip, material, position: CylinderLength / 2f * Vector3.UnitX, rotation: xRotation);

                var zRotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo); // Yup rotated towards Z
                AddModelEntity("Zend", tip, material2, position: CylinderRadius * 4f * Vector3.UnitZ, rotation: zRotation);

                this.limitZ = AddModelEntity("LimitZ", limitZ, limitMaterial, Quaternion.RotationY(MathF.PI + (lastLimitZ / 2f)), position: new Vector3(CylinderLength / 2, 0, 0)).Get<ModelComponent>();
                this.limitY = AddModelEntity("LimitY", limitY, limitMaterial, Quaternion.RotationY(MathF.PI + (lastLimitY / 2f)) * Quaternion.RotationX(-MathUtil.PiOverTwo), position: new Vector3(CylinderLength / 2, 0, 0)).Get<ModelComponent>();
                this.limitT = AddModelEntity("LimitT", limitT, limitMaterial, Quaternion.RotationZ(MathUtil.PiOverTwo) * Quaternion.RotationX(MathUtil.PiOverTwo - lastLimitT / 2f)).Get<ModelComponent>();

                Update(desc);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.ConeTwist;

            public override void Update(IConstraintDesc constraintDesc)
            {
                var coneDesc = (ConeTwistConstraintDesc)constraintDesc;
                ModelEntity.Transform.Rotation = Pivot == Pivot.A ? coneDesc.AxisInA : coneDesc.AxisInB;

                var newLimitZ = coneDesc.Limit.SetLimit ? coneDesc.Limit.SwingSpanZ : MathF.PI;
                var newLimitY = coneDesc.Limit.SetLimit ? coneDesc.Limit.SwingSpanY : MathF.PI;
                var newLimitT = coneDesc.Limit.SetLimit ? 2 * coneDesc.Limit.TwistSpan : 2 * MathF.PI;

                if (newLimitZ != lastLimitZ)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newLimitZ).ToMeshDraw();
                    limitZ.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    limitZ.Entity.Transform.Rotation = Quaternion.RotationY(MathF.PI + (newLimitZ / 2f));
                    lastLimitZ = newLimitZ;
                }

                if (newLimitY != lastLimitY)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newLimitY).ToMeshDraw();
                    limitY.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    limitY.Entity.Transform.Rotation = Quaternion.RotationY(MathF.PI + (newLimitY / 2f)) * Quaternion.RotationX(-MathUtil.PiOverTwo);
                    lastLimitY = newLimitY;
                }

                if (newLimitT != lastLimitT)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newLimitT).ToMeshDraw();
                    limitT.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    limitT.Entity.Transform.Rotation = Quaternion.RotationZ(MathUtil.PiOverTwo) * Quaternion.RotationX(MathUtil.PiOverTwo - newLimitT / 2f);
                    lastLimitT = newLimitT;
                }
            }
        }

        private sealed class GearModelWrapper : ModelWrapper
        {
            public GearModelWrapper(GearConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot, graphicsDevice)
            {
                var pipe = GetCylinder(graphicsDevice).ToMeshDraw();
                var tip = GetCone(graphicsDevice).ToMeshDraw();
                var disc = GetLimitDisc(graphicsDevice, 2 * MathF.PI).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                var discMaterial = GizmoUniformColorMaterial.Create(graphicsDevice, LimitColor);

                var xRotation = Quaternion.RotationZ(-MathUtil.PiOverTwo); // Yup rotated towards X
                AddModelEntity("X", pipe, material, xRotation);
                AddModelEntity("Xend", tip, material, position: CylinderLength / 2f * Vector3.UnitX, rotation: xRotation);

                AddModelEntity("Disc", disc, discMaterial, Quaternion.RotationZ(MathUtil.PiOverTwo)).Get<ModelComponent>();

                Update(desc);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.Gear;

            public override void Update(IConstraintDesc constraintDesc)
            {
                var gearDesc = (GearConstraintDesc)constraintDesc;
                ModelEntity.Transform.Rotation = Pivot == Pivot.A ? gearDesc.AxisInA : gearDesc.AxisInB;
            }
        }

        private sealed class SliderModelWrapper : ModelWrapper
        {
            private Material limitMaterial;
            private ModelComponent lowerAngulerLimit;
            private ModelComponent upperAngularLimit;
            private float lastLowerAngularLimit;
            private float lastUpperAngularLimit;
            private Entity lowerLinearLimit;
            private Entity upperLinearLimit;

            public SliderModelWrapper(SliderConstraintDesc desc, Pivot pivot, Graphics.GraphicsDevice graphicsDevice)
                : base(pivot, graphicsDevice)
            {
                var sphere = GetSphere(graphicsDevice).ToMeshDraw();
                var pipe = GetCylinder(graphicsDevice).ToMeshDraw();
                var tip = GetCone(graphicsDevice).ToMeshDraw();
                lastLowerAngularLimit = -desc.Limit.LowerAngularLimit;
                lastUpperAngularLimit = desc.Limit.UpperAngularLimit;
                var limitLower = GetLimitDisc(graphicsDevice, lastLowerAngularLimit).ToMeshDraw();
                var limitUpper = GetLimitDisc(graphicsDevice, lastUpperAngularLimit).ToMeshDraw();
                var material = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeUniformColor : PurpleUniformColor);
                var material2 = GizmoUniformColorMaterial.Create(graphicsDevice, pivot == Pivot.A ? OrangeNegUniformColor : PurpleNegUniformColor);
                limitMaterial = GizmoUniformColorMaterial.Create(graphicsDevice, LimitColor);

                AddModelEntity("Center", sphere, material);

                var xRotation = Quaternion.RotationZ(-MathUtil.PiOverTwo); // Yup rotated towards X
                AddModelEntity("X", pipe, material, xRotation);
                AddModelEntity("Xend", tip, material, position: CylinderLength / 2f * Vector3.UnitX, rotation: xRotation);

                var zRotation = Quaternion.RotationX(MathUtil.PiOverTwo); // Yup rotated towards Z
                AddModelEntity("Zend", tip, material2, position: CylinderRadius * 4f * Vector3.UnitZ, rotation: zRotation);

                lowerAngulerLimit = AddModelEntity("LowerAngularLimit", limitLower, limitMaterial, Quaternion.RotationZ(MathUtil.PiOverTwo) * Quaternion.RotationX(MathUtil.PiOverTwo)).Get<ModelComponent>();
                upperAngularLimit = AddModelEntity("UpperAngularLimit", limitUpper, limitMaterial, Quaternion.RotationZ(-MathUtil.PiOverTwo) * Quaternion.RotationX(-MathUtil.PiOverTwo)).Get<ModelComponent>();

                lowerLinearLimit = AddModelEntity("LowerLinearLimit", sphere, limitMaterial);
                upperLinearLimit = AddModelEntity("UpperLinearLimit", sphere, limitMaterial);

                Update(desc);
            }

            public override ConstraintTypes ConstraintType => ConstraintTypes.Slider;

            public override void Update(IConstraintDesc constraintDesc)
            {
                var sliderDesc = (SliderConstraintDesc)constraintDesc;
                ModelEntity.Transform.Rotation = Pivot == Pivot.A ? sliderDesc.AxisInA : sliderDesc.AxisInB;

                lowerLinearLimit.Transform.Position = new Vector3(sliderDesc.Limit.LowerLinearLimit, 0, 0);
                upperLinearLimit.Transform.Position = new Vector3(sliderDesc.Limit.UpperLinearLimit, 0, 0);

                var newLowerLimit = -sliderDesc.Limit.LowerAngularLimit;
                var newUpperLimit = sliderDesc.Limit.UpperAngularLimit;

                if (newLowerLimit != lastLowerAngularLimit)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newLowerLimit).ToMeshDraw();
                    lowerAngulerLimit.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    lastLowerAngularLimit = newLowerLimit;
                }

                if (newUpperLimit != lastUpperAngularLimit)
                {
                    var limitMesh = GetLimitDisc(GraphicsDevice, newUpperLimit).ToMeshDraw();
                    upperAngularLimit.Model = new Model { limitMaterial, new Mesh { Draw = limitMesh } };
                    lastUpperAngularLimit = newUpperLimit;
                }
            }
        }
    }
}
