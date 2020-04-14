// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public class ScaleGizmo : AxisTransformationGizmo
    {
        private readonly List<Entity>[] scaleAxes = { new List<Entity>(), new List<Entity>(), new List<Entity>() };
        private Entity scaleOrigin;

        protected override Entity Create()
        {
            base.Create();

            const float ExtremitySize = GizmoExtremitySize / 1.5f;
            const float BodyRadius = GizmoExtremitySize / 7.5f;
            const float OriginSize = GizmoOriginScale * ExtremitySize;
            const float BodyLength = 1f - ExtremitySize;

            var entity = new Entity("Scale gizmo");
            var rotations = new[] { Vector3.Zero, new Vector3(0, 0, MathUtil.Pi / 2), new Vector3(0, -MathUtil.Pi / 2f, 0) };
            var extremityMesh = GeometricPrimitive.Cube.New(GraphicsDevice, ExtremitySize).ToMeshDraw();
            var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, BodyLength, BodyRadius, GizmoTessellation).ToMeshDraw();

            for (int axis = 0; axis < 3; ++axis)
            {
                var axisMaterial = GetAxisDefaultMaterial(axis);
                var extremity = new Entity("ArrowExtremity" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = extremityMesh } }, RenderGroup = RenderGroup } };
                extremity.Transform.Position.X = BodyLength;
                scaleAxes[axis].Add(extremity);
                
                var body = new Entity("ArrowBody" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
                body.Transform.Position.X = BodyLength / 2;
                body.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;
                scaleAxes[axis].Add(body);

                // create the arrow entity composed of the cone and body
                var arrowEntity = new Entity("ArrowEntity" + axis);
                arrowEntity.Transform.Children.Add(extremity.Transform);
                arrowEntity.Transform.Children.Add(body.Transform);
                arrowEntity.Transform.RotationEulerXYZ = rotations[axis];

                // Add the arrow entity to the gizmo entity
                entity.Transform.Children.Add(arrowEntity.Transform);
            }

            // Add middle sphere
            var sphereMeshDraw = GeometricPrimitive.Cube.New(GraphicsDevice, OriginSize).ToMeshDraw();
            scaleOrigin = new Entity("OriginCube") { new ModelComponent { Model = new Model { DefaultOriginMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            entity.Transform.Children.Add(scaleOrigin.Transform);

            return entity;
        }
        
        protected override void UpdateTransformationAxis()
        {
            var newSelection = GizmoTransformationAxes.None;
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();

            // calculate the ray in the gizmo space
            var gizmoMatrix = WorldMatrix;
            var gizmoViewInverse = Matrix.Invert(gizmoMatrix * cameraService.ViewMatrix);
            var clickRay = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, Input.MousePosition, gizmoViewInverse);

            // determine gizmo world matrix and scale
            var gizmoScale = gizmoMatrix.Row1.Length();

            float hitDistance;
            var minHitDistance = float.PositiveInfinity;

            // select the axis whose intersection is the closest
            for (int i = 0; i < 3; i++)
            {
                var minimum = new Vector3(-GizmoExtremitySize / 2);
                minimum[i] = 0;
                var maximum = new Vector3(+GizmoExtremitySize / 2);
                maximum[i] = 1;

                UpdateSelectionOnCloserIntersection(new BoundingBox(minimum, maximum), clickRay, (GizmoTransformationAxes)(1 << i), ref minHitDistance, ref newSelection);
            }

            // overrides the current selection with the origin if the intersection distance are similar
            var minimumOrigin = new Vector3(-GizmoOriginScale * GizmoExtremitySize / 2);
            var maximumOrigin = new Vector3(+GizmoOriginScale * GizmoExtremitySize / 2);
            if (new BoundingBox(minimumOrigin, maximumOrigin).Intersects(ref clickRay, out hitDistance) && hitDistance - minHitDistance < gizmoScale * 0.5f)
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

            var scaleValue = translation > 0 ? (float)Math.Exp(translation) : 1 / ((float)Math.Exp(-translation));

            return Math.Max(MathUtil.ZeroTolerance, Math.Min(MaxScaleValue, scaleValue));
        }

        protected override void UpdateColors()
        {
            base.UpdateColors();

            for (int axis = 0; axis < 3; axis++)
            {
                var axisMaterial = GetAxisDefaultMaterial(axis);
                var transformationAxis = (GizmoTransformationAxes)(1 << axis);
                bool isSelected = (TransformationAxes & transformationAxis) == transformationAxis;
                scaleAxes[axis].ForEach(x => x.Get<ModelComponent>().Model.Materials[0] = isSelected ? ElementSelectedMaterial: axisMaterial);
            }
            var originSelected = TransformationAxes == GizmoTransformationAxes.XYZ;
            scaleOrigin.Get<ModelComponent>().Model.Materials[0] = originSelected ? ElementSelectedMaterial : DefaultOriginMaterial;
        }
    }
}
