// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Engine;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    public class TranslationGizmo : AxisTransformationGizmo
    {
        private class EntitySortInfo : IComparer<EntitySortInfo>
        {
            public Entity Entity;

            public float Depth;

            public int Compare(EntitySortInfo x, EntitySortInfo y)
            {
                return Math.Sign(x.Depth - y.Depth);
            }
        };

        private const float AxisConeRadius = GizmoExtremitySize / 2f;
        private const float AxisConeHeight = 2f * AxisConeRadius;
        private const float AxisBodyRadius = GizmoExtremitySize / 7.5f;
        private const float AxisBodyLength = 1f - AxisConeHeight;
        private const float OriginRadius = GizmoOriginScale * AxisConeRadius;

        private readonly Material[] planeMaterials = new Material[3];
        private readonly List<EntitySortInfo> sortedEntities = new List<EntitySortInfo>();
        private readonly List<Entity>[] translationAxes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
        private readonly List<Entity>[] translationPlanes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
        private readonly List<Entity>[] translationPlaneEdges = { new List<Entity>(), new List<Entity>(), new List<Entity>() };

        private readonly List<Entity> translationPlaneRoots = new List<Entity>();
        private readonly List<ModelComponent>[] translationOpositeAxes = { new List<ModelComponent>(), new List<ModelComponent>(), new List<ModelComponent>(), };
        private Entity translationOrigin;

        protected override Entity Create()
        {
            base.Create();

            planeMaterials[0] = CreateUniformColorMaterial(Color.Red.WithAlpha(86));
            planeMaterials[1] = CreateUniformColorMaterial(Color.Green.WithAlpha(86));
            planeMaterials[2] = CreateUniformColorMaterial(Color.Blue.WithAlpha(86));

            var entity = new Entity("Translation gizmo");
            var axisRootEntities = new [] { new Entity("Root X axis"), new Entity("Root Y axis"), new Entity("Root Z axis")};
            var coneMesh = GeometricPrimitive.Cone.New(GraphicsDevice, AxisConeRadius, AxisConeHeight, GizmoTessellation).ToMeshDraw();
            var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, AxisBodyLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();

            // create the axis arrows 
            for (int axis = 0; axis < 3; ++axis)
            {
                var material = GetAxisDefaultMaterial(axis);

                // the end cone 
                var coneEntity = new Entity("ArrowCone" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = coneMesh } }, RenderGroup = RenderGroup } };
                coneEntity.Transform.Rotation = Quaternion.RotationZ(-MathUtil.Pi / 2);
                coneEntity.Transform.Position.X = AxisBodyLength + AxisConeHeight * 0.5f;
                translationAxes[axis].Add(coneEntity);

                // the main body
                var bodyEntity = new Entity("ArrowBody" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
                bodyEntity.Transform.Position.X = AxisBodyLength / 2;
                bodyEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
                translationAxes[axis].Add(bodyEntity);

                // oposite side part (cylinder + end sphere shown when camera is looking oposite direction to the axis)
                var frameMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, GizmoPlaneLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
                var opositeFrameEntity = new Entity("Oposite Frame" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
                opositeFrameEntity.Transform.Position.X = - GizmoPlaneLength / 2;
                opositeFrameEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
                var articulationMesh = GeometricPrimitive.Sphere.New(GraphicsDevice, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
                var opositeSphereEntity = new Entity("FrameSphere" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = articulationMesh } }, RenderGroup = RenderGroup } };
                opositeSphereEntity.Transform.Position = new Vector3(-GizmoPlaneLength, 0, 0);
                translationAxes[axis].Add(opositeFrameEntity);
                translationAxes[axis].Add(opositeSphereEntity);
                translationOpositeAxes[axis].Add(opositeFrameEntity.Get<ModelComponent>());
                translationOpositeAxes[axis].Add(opositeSphereEntity.Get<ModelComponent>());

                // create the arrow entity composed of the cone and bode
                var arrowEntity = new Entity("ArrowEntity" + axis);
                arrowEntity.Transform.Children.Add(coneEntity.Transform);
                arrowEntity.Transform.Children.Add(bodyEntity.Transform);
                arrowEntity.Transform.Children.Add(opositeFrameEntity.Transform);
                arrowEntity.Transform.Children.Add(opositeSphereEntity.Transform);

                // Add the arrow entity to the gizmo entity
                axisRootEntities[axis].Transform.Children.Add(arrowEntity.Transform);
            }

            // create the translation planes
            for (int axis = 0; axis < 3; ++axis)
            {
                // The skeleton material
                var axisMaterial = GetAxisDefaultMaterial(axis);
                
                // The 2 frame cylinders
                var frameMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, GizmoPlaneLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
                var topFrameEntity = new Entity("TopFrame" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
                var leftFrameEntity = new Entity("LeftFrame" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
                topFrameEntity.Transform.Position = new Vector3(0, GizmoPlaneLength, GizmoPlaneLength / 2);
                topFrameEntity.Transform.RotationEulerXYZ = new Vector3(MathUtil.Pi / 2f, 0, 0);
                leftFrameEntity.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength);
                translationPlaneEdges[axis].Add(topFrameEntity);
                translationPlaneEdges[axis].Add(leftFrameEntity);

                // The articulation sphere
                var articulationMesh = GeometricPrimitive.Sphere.New(GraphicsDevice, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
                var articulationEntity = new Entity("FrameSphere" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = articulationMesh } }, RenderGroup = RenderGroup } };
                articulationEntity.Transform.Position = new Vector3(0, GizmoPlaneLength, GizmoPlaneLength);
                translationPlaneEdges[axis].Add(articulationEntity);

                // The transparent planes (2 for correct lighting)
                var materialPlane = planeMaterials[axis];
                var planeMesh = GeometricPrimitive.Plane.New(GraphicsDevice, GizmoPlaneLength, GizmoPlaneLength).ToMeshDraw();
                var planeFrameEntityFront = new Entity("FramePlaneFront" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
                var planeFrameEntityBack = new Entity("FramePlaneBack" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
                planeFrameEntityFront.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
                planeFrameEntityFront.Transform.RotationEulerXYZ = new Vector3(0, MathUtil.Pi / 2f, 0);
                planeFrameEntityBack.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
                planeFrameEntityBack.Transform.RotationEulerXYZ = new Vector3(0, -MathUtil.Pi / 2f, 0);
                translationPlanes[axis].Add(planeFrameEntityFront);
                translationPlanes[axis].Add(planeFrameEntityBack);
                sortedEntities.Add(new EntitySortInfo { Entity = planeFrameEntityFront });
                sortedEntities.Add(new EntitySortInfo { Entity = planeFrameEntityBack });

                // Add the different parts of the plane to the plane entity
                var planeEntity = new Entity("GizmoPlane" + axis);
                planeEntity.Transform.Children.Add(topFrameEntity.Transform);
                planeEntity.Transform.Children.Add(leftFrameEntity.Transform);
                planeEntity.Transform.Children.Add(articulationEntity.Transform);
                planeEntity.Transform.Children.Add(planeFrameEntityFront.Transform);
                planeEntity.Transform.Children.Add(planeFrameEntityBack.Transform);
                translationPlaneRoots.Add(planeEntity);

                // Add the plane entity to the gizmo entity
                axisRootEntities[axis].Transform.Children.Add(planeEntity.Transform);
            }

            // set the axis root entities rotation and add them to the main entityaxisRootEntities[axis]
            var axisRotations = new [] { Vector3.Zero, new Vector3(MathUtil.PiOverTwo, 0, MathUtil.PiOverTwo), new Vector3(-MathUtil.PiOverTwo, -MathUtil.PiOverTwo, 0) };
            for (int axis = 0; axis < 3; axis++)
            {
                axisRootEntities[axis].TransformValue.RotationEulerXYZ = axisRotations[axis];
                entity.TransformValue.Children.Add(axisRootEntities[axis].TransformValue);
            }

            // Add middle sphere
            var materialSphere = DefaultOriginMaterial;
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, OriginRadius, GizmoTessellation).ToMeshDraw();
            translationOrigin = new Entity("OriginCube") { new ModelComponent { Model = new Model { materialSphere, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            entity.Transform.Children.Add(translationOrigin.Transform);

            return entity;
        }

        public override async Task Update()
        {
            await base.Update();

            UpdateDrawOrder();
        }

        protected override void UpdateShape()
        {
            base.UpdateShape();
            
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();
            if (cameraService == null)
                return;

            var gizmoInverse = Matrix.Invert(WorldMatrix);
            var viewInverse = Matrix.Invert(cameraService.ViewMatrix);
            var cameraInGizmoSpace = (viewInverse * gizmoInverse).TranslationVector;

            // reset the plane rotations
            for (int axis = 0; axis < 3; axis++)
                translationPlaneRoots[axis].TransformValue.Rotation = Quaternion.Identity;

            for (int axis = 0; axis < 3; axis++)
            {
                var isCameraBackfacing = cameraInGizmoSpace[axis] < 0;
                translationOpositeAxes[axis].ForEach(x=>x.Enabled = isCameraBackfacing);

                if (isCameraBackfacing)
                {
                    translationPlaneRoots[(axis + 1) % 3].TransformValue.Rotation *= Quaternion.RotationY(MathUtil.Pi);
                    translationPlaneRoots[(axis + 2) % 3].TransformValue.Rotation *= Quaternion.RotationZ(MathUtil.Pi);
                }
            }
        }

        protected override void UpdateTransformationAxis()
        {
            var newSelection = GizmoTransformationAxes.None;
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();

            // calculate the ray in the gizmo space
            var gizmoMatrix = WorldMatrix;
            var gizmoViewInverse = Matrix.Invert(gizmoMatrix * cameraService.ViewMatrix);

            // Check if the inverted View Matrix is valid (since it will be use for mouse picking, check the translation vector only)
            if (float.IsNaN(gizmoViewInverse.TranslationVector.X)
                || float.IsNaN(gizmoViewInverse.TranslationVector.Y)
                || float.IsNaN(gizmoViewInverse.TranslationVector.Z))
            {
                return;
            }

            var clickRay = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, Input.MousePosition, gizmoViewInverse);
            
            float hitDistance;
            var minHitDistance = float.PositiveInfinity;

            // Select the closest intersecting translation plane
            foreach (var pair in EditorGameComponentGizmoService.PlaneToIndex)
            {
                var minimum = new Vector3(0) { [pair.Value] = -AxisBodyRadius };
                var maximum = new Vector3(GizmoPlaneLength);

                for (int axis = 0; axis < 3; axis++)
                    maximum[axis] *= Math.Sign(gizmoViewInverse[3, axis]); // translation planes move to always face the camera.

                maximum[pair.Value] = AxisBodyRadius;

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, pair.Key, ref minHitDistance, ref newSelection);
            }

            // Overrides selection with the closed intersecting axis if any
            minHitDistance = float.PositiveInfinity;
            for (int i = 0; i < 3; i++)
            {
                // the extremity
                var minimum = new Vector3(-AxisConeRadius) { [i] = AxisBodyLength };
                var maximum = new Vector3(+AxisConeRadius) { [i] = 1 };

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, (GizmoTransformationAxes)(1 << i), ref minHitDistance, ref newSelection);

                // the body
                minimum = new Vector3(-AxisBodyRadius) { [i] = 0 };
                maximum = new Vector3(+AxisBodyRadius) { [i] = AxisBodyLength };
                if (gizmoViewInverse[3, i] < 0) 
                    minimum[i] = -GizmoPlaneLength; // camera is backfacing the axis we should compute intersection with the oposite showing part too

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, (GizmoTransformationAxes)(1 << i), ref minHitDistance, ref newSelection);
            }
            
            // overrides the current selection with the origin if intersecting
            if (new BoundingSphere(Vector3.Zero, OriginRadius).Intersects(ref clickRay, out hitDistance))
                newSelection = GizmoTransformationAxes.XYZ;

            TransformationAxes = newSelection;
        }

        protected override void UpdateColors()
        {
            base.UpdateColors();

            for (int axis = 0; axis < 3; axis++)
            {
                var axisMaterial = GetAxisDefaultMaterial(axis);
                var planeMaterial = planeMaterials[axis];
                var transformationAxis = (GizmoTransformationAxes)(1 << axis);
                bool isAxisSelected = (TransformationAxes & transformationAxis) == transformationAxis;
                translationAxes[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isAxisSelected ? ElementSelectedMaterial: axisMaterial);
                bool isPlaneSelected = (TransformationAxes ^ transformationAxis) == GizmoTransformationAxes.XYZ;
                translationPlaneEdges[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isPlaneSelected ? ElementSelectedMaterial : axisMaterial);
                translationPlanes[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isPlaneSelected ? TransparentElementSelectedMaterial : planeMaterial);
            }
            var originSelected = TransformationAxes == GizmoTransformationAxes.XYZ;
            translationOrigin.Get<ModelComponent>().Model.Materials[0] = originSelected ? ElementSelectedMaterial : DefaultOriginMaterial;
        }

        protected override InitialTransformation CalculateTransformation()
        {
            var transformation = base.CalculateTransformation();

            // snap the translation in necessary
            if (UseSnap)
            {
                var gizmoMatrix = StartWorldMatrix;
                for (int i = 0; i < 3; i++)
                {
                    var gizmoScale = new Vector3(gizmoMatrix[i, 0], gizmoMatrix[i, 1], gizmoMatrix[i, 2]).Length();
                    var snapValue = SnapValue / gizmoScale;
                    DragTranslationWorld[i] = MathUtil.Snap(DragTranslationWorld[i], snapValue);
                }
            }

            // set the translation in the gizmo space
            transformation.Translation = DragTranslationWorld;

            return transformation;
        }

        private void UpdateDrawOrder()
        {
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();
            // calculate the depth of the entities
            var viewProjection = cameraService.ViewMatrix * cameraService.ProjectionMatrix;
            foreach (EntitySortInfo sortInfo in sortedEntities)
            {
                var worldViewProjection = sortInfo.Entity.Transform.WorldMatrix * viewProjection;
                sortInfo.Depth = worldViewProjection.M43;
                // protects against exception thrown by Sort's internals
                // TODO: investigate the actual source of the presence of nans which must come from one of the matrices. c.f. XK-4627
                if (float.IsNaN(sortInfo.Depth))
                    sortInfo.Depth = float.MaxValue;
            }

            // sort the entities by decreasing depth
            sortedEntities.Sort(sortedEntities[0]);
        }
    }
}
