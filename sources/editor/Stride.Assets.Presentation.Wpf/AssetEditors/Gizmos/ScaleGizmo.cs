// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public class ScaleGizmo : AxisTransformationGizmo
    {
        private class EntitySortInfo : IComparer<EntitySortInfo>
        {
            public Entity Entity;

            public float Depth;

            public int Compare(EntitySortInfo x, EntitySortInfo y)
            {
                return MathF.Sign(x.Depth - y.Depth);
            }
        };

        private const float AxisExtremitySize = GizmoExtremitySize / 2f;
        private const float AxisBodyRadius = GizmoExtremitySize / 9f;
        private const float AxisBodyLength = 1f - AxisExtremitySize;
        private const float OriginSize = GizmoOriginScale * AxisExtremitySize;

        private readonly Material[] planeMaterials = new Material[3];
        private readonly List<EntitySortInfo> sortedEntities = new List<EntitySortInfo>();
        private readonly List<Entity>[] scaleAxes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
        private readonly List<Entity>[] scalePlanes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
        private readonly List<Entity>[] scalePlaneEdges = { new List<Entity>(), new List<Entity>(), new List<Entity>() };

        private readonly List<Entity> scalePlaneRoots = new List<Entity>();
        private readonly List<ModelComponent>[] scaleOpositeAxes = { new List<ModelComponent>(), new List<ModelComponent>(), new List<ModelComponent>(), };
        private Entity scaleOrigin;

        protected override Entity Create()
        {
            base.Create();

            planeMaterials[0] = CreateUniformColorMaterial(Color.Red.WithAlpha(86));
            planeMaterials[1] = CreateUniformColorMaterial(Color.Green.WithAlpha(86));
            planeMaterials[2] = CreateUniformColorMaterial(Color.Blue.WithAlpha(86));

            var entity = new Entity("Scale gizmo");
            var axisRootEntities = new[] { new Entity("Root X axis"), new Entity("Root Y axis"), new Entity("Root Z axis") };
            var cubeMesh = GeometricPrimitive.Cube.New(GraphicsDevice, AxisExtremitySize).ToMeshDraw();
            var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, AxisBodyLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();

            // create the axis arrows
            for (int axis = 0; axis < 3; ++axis)
            {
                var material = GetAxisDefaultMaterial(axis);

                // the end cube
                var extremityEntity = new Entity("ArrowExtremity" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = cubeMesh } }, RenderGroup = RenderGroup } };
                extremityEntity.Transform.Position.X = AxisBodyLength + AxisExtremitySize * 0.5f;
                scaleAxes[axis].Add(extremityEntity);

                // the main body
                var bodyEntity = new Entity("ArrowBody" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
                bodyEntity.Transform.Position.X = AxisBodyLength / 2;
                bodyEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
                scaleAxes[axis].Add(bodyEntity);

                // oposite side part (cylinder shown when camera is looking oposite direction to the axis)
                var frameMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, GizmoPlaneLength, AxisBodyRadius, GizmoTessellation).ToMeshDraw();
                var opositeFrameEntity = new Entity("Oposite Frame" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
                opositeFrameEntity.Transform.Position.X = -GizmoPlaneLength / 2;
                opositeFrameEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
                scaleAxes[axis].Add(opositeFrameEntity);
                scaleOpositeAxes[axis].Add(opositeFrameEntity.Get<ModelComponent>());

                // create the arrow entity composed of the cube and body
                var arrowEntity = new Entity("ArrowEntity" + axis);
                arrowEntity.Transform.Children.Add(extremityEntity.Transform);
                arrowEntity.Transform.Children.Add(bodyEntity.Transform);
                arrowEntity.Transform.Children.Add(opositeFrameEntity.Transform);

                // Add the arrow entity to the gizmo entity
                axisRootEntities[axis].Transform.Children.Add(arrowEntity.Transform);
            }

            // create the scaling planes
            for (int axis = 0; axis < 3; ++axis)
            {
                // The skeleton material
                var axisMaterial = GetAxisDefaultMaterial(axis);

                // The 2 frame rectangles
                var frameMesh = GeometricPrimitive.Cube.New(GraphicsDevice, new Vector3(AxisBodyRadius / 2f, GizmoPlaneLength / 3f, AxisBodyRadius / 2f)).ToMeshDraw();
                var topFrameEntity = new Entity("TopFrame" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
                var leftFrameEntity = new Entity("LeftFrame" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = frameMesh } }, RenderGroup = RenderGroup } };
                topFrameEntity.Transform.Position = new Vector3(0, GizmoPlaneLength, GizmoPlaneLength - (GizmoPlaneLength / 6));
                topFrameEntity.Transform.RotationEulerXYZ = new Vector3(MathUtil.Pi / 2f, 0, 0);
                leftFrameEntity.Transform.Position = new Vector3(0, GizmoPlaneLength - (GizmoPlaneLength / 6), GizmoPlaneLength);
                scalePlaneEdges[axis].Add(topFrameEntity);
                scalePlaneEdges[axis].Add(leftFrameEntity);

                // The transparent planes (2 for correct lighting)
                var materialPlane = planeMaterials[axis];
                var planeMesh = GeometricPrimitive.Plane.New(GraphicsDevice, GizmoPlaneLength, GizmoPlaneLength).ToMeshDraw();
                var planeFrameEntityFront = new Entity("FramePlaneFront" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
                var planeFrameEntityBack = new Entity("FramePlaneBack" + axis) { new ModelComponent { Model = new Model { materialPlane, new Mesh { Draw = planeMesh } }, RenderGroup = RenderGroup } };
                planeFrameEntityFront.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
                planeFrameEntityFront.Transform.RotationEulerXYZ = new Vector3(0, MathUtil.Pi / 2f, 0);
                planeFrameEntityBack.Transform.Position = new Vector3(0, GizmoPlaneLength / 2, GizmoPlaneLength / 2);
                planeFrameEntityBack.Transform.RotationEulerXYZ = new Vector3(0, -MathUtil.Pi / 2f, 0);
                scalePlanes[axis].Add(planeFrameEntityFront);
                scalePlanes[axis].Add(planeFrameEntityBack);
                sortedEntities.Add(new EntitySortInfo { Entity = planeFrameEntityFront });
                sortedEntities.Add(new EntitySortInfo { Entity = planeFrameEntityBack });

                // Add the different parts of the plane to the plane entity
                var planeEntity = new Entity("GizmoPlane" + axis);
                planeEntity.Transform.Children.Add(topFrameEntity.Transform);
                planeEntity.Transform.Children.Add(leftFrameEntity.Transform);
                planeEntity.Transform.Children.Add(planeFrameEntityFront.Transform);
                planeEntity.Transform.Children.Add(planeFrameEntityBack.Transform);
                scalePlaneRoots.Add(planeEntity);

                // Add the plane entity to the gizmo entity
                axisRootEntities[axis].Transform.Children.Add(planeEntity.Transform);
            }

            // set the axis root entities rotation and add them to the main entity
            var axisRotations = new[] { Vector3.Zero, new Vector3(MathUtil.PiOverTwo, 0, MathUtil.PiOverTwo), new Vector3(-MathUtil.PiOverTwo, -MathUtil.PiOverTwo, 0) };
            for (int axis = 0; axis < 3; axis++)
            {
                axisRootEntities[axis].TransformValue.RotationEulerXYZ = axisRotations[axis];
                entity.TransformValue.Children.Add(axisRootEntities[axis].TransformValue);
            }

            // Add middle cube
            var cubeMeshDraw = GeometricPrimitive.Cube.New(GraphicsDevice, OriginSize).ToMeshDraw();
            scaleOrigin = new Entity("OriginCube") { new ModelComponent { Model = new Model { DefaultOriginMaterial, new Mesh { Draw = cubeMeshDraw } }, RenderGroup = RenderGroup } };
            entity.Transform.Children.Add(scaleOrigin.Transform);

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
                scalePlaneRoots[axis].TransformValue.Rotation = Quaternion.Identity;

            for (int axis = 0; axis < 3; axis++)
            {
                var isCameraBackfacing = cameraInGizmoSpace[axis] < 0;
                scaleOpositeAxes[axis].ForEach(x => x.Enabled = isCameraBackfacing);

                if (isCameraBackfacing)
                {
                    scalePlaneRoots[(axis + 1) % 3].TransformValue.Rotation *= Quaternion.RotationY(MathUtil.Pi);
                    scalePlaneRoots[(axis + 2) % 3].TransformValue.Rotation *= Quaternion.RotationZ(MathUtil.Pi);
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

            var minHitDistance = float.PositiveInfinity;

            // Select the closest intersecting translation plane
            foreach (var pair in EditorGameComponentGizmoService.PlaneToIndex)
            {
                var minimum = new Vector3(0) { [pair.Value] = -AxisBodyRadius };
                var maximum = new Vector3(GizmoPlaneLength);

                for (int axis = 0; axis < 3; axis++)
                    maximum[axis] *= MathF.Sign(gizmoViewInverse[3, axis]); // translation planes move to always face the camera.

                maximum[pair.Value] = AxisBodyRadius;

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, pair.Key, ref minHitDistance, ref newSelection);
            }

            // Overrides selection with the closest intersecting axis if any
            for (int i = 0; i < 3; i++)
            {
                // the extremity
                var minimum = new Vector3(-AxisExtremitySize) { [i] = AxisBodyLength };
                var maximum = new Vector3(+AxisExtremitySize) { [i] = 1 };

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, (GizmoTransformationAxes)(1 << i), ref minHitDistance, ref newSelection);

                // the body
                minimum = new Vector3(-AxisBodyRadius) { [i] = 0 };
                maximum = new Vector3(+AxisBodyRadius) { [i] = AxisBodyLength };
                if (gizmoViewInverse[3, i] < 0)
                    minimum[i] = -GizmoPlaneLength; // camera is backfacing the axis we should compute intersection with the oposite showing part too

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, (GizmoTransformationAxes)(1 << i), ref minHitDistance, ref newSelection);
            }

            // overrides the current selection with the origin if intersecting
            var minimumOrigin = new Vector3(-OriginSize / 2f);
            var maximumOrigin = new Vector3(+OriginSize / 2f);
            if (new BoundingBox(minimumOrigin, maximumOrigin).Intersects(ref clickRay))
            {
                newSelection = GizmoTransformationAxes.XYZ;
            }

            TransformationAxes = newSelection;
        }

        /// <summary>
        ///  For 3 axes transformation, the scale is determined the same way as the rotation. That is with the projection of the mouse translation on the screen transformation direction.
        /// </summary>
        /// <returns></returns>
        protected override InitialTransformation CalculateTransformation()
        {
            var transform = base.CalculateTransformation();

            if (TransformationAxes == GizmoTransformationAxes.XYZ) // special transformation mode
            {
                var mouseDrag = Input.MousePosition - StartMousePosition;
                var translationVector = Vector2.Dot(new Vector2(mouseDrag.X, -mouseDrag.Y), TransformationDirection) / DefaultScale;
                transform.Scale = new Vector3(ComputeScaleFactorFromTranslation(translationVector));
            }
            else // default transformation mode
            {
                for (int i = 0; i < 3; i++)
                    transform.Scale[i] = ComputeScaleFactorFromTranslation(DragTranslationWorld[i]);
            }

            return transform;
        }

        private float ComputeScaleFactorFromTranslation(float translation)
        {
            const float MaxScaleValue = 1f / MathUtil.ZeroTolerance;

            // snap the value if needed BEFORE the exp function
            if (UseSnap)
                translation = MathUtil.Snap(translation, SnapValue);

            var scaleValue = translation > 0 ? MathF.Exp(translation) : 1 / (MathF.Exp(-translation));

            return Math.Max(MathUtil.ZeroTolerance, Math.Min(MaxScaleValue, scaleValue));
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
                scaleAxes[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isAxisSelected ? ElementSelectedMaterial : axisMaterial);
                bool isPlaneSelected = (TransformationAxes ^ transformationAxis) == GizmoTransformationAxes.XYZ;
                scalePlaneEdges[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isPlaneSelected ? ElementSelectedMaterial : axisMaterial);
                scalePlanes[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isPlaneSelected ? TransparentElementSelectedMaterial : planeMaterial);
            }
            var originSelected = TransformationAxes == GizmoTransformationAxes.XYZ;
            scaleOrigin.Get<ModelComponent>().Model.Materials[0] = originSelected ? ElementSelectedMaterial : DefaultOriginMaterial;
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
