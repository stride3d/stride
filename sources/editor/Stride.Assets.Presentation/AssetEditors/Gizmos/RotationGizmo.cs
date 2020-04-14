// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public class RotationGizmo : TransformationGizmo
    {
        private const float RotationGizmoRadius = 1f; // the size of the radius of the torus used to rotate the objects

        private const float RotationGizmoThickness = 0.02f; // the size of the inner radius torus used to rotate the objects

        private readonly Entity[] rotationAxes = new Entity[3];

        protected override Entity Create()
        {
            base.Create();

            var entity = new Entity("Rotation Gizmo");
            const float OriginSize = GizmoOriginScale * GizmoExtremitySize;
            var rotations = new[] { new Vector3(0, 0, MathUtil.Pi / 2), new Vector3(), new Vector3(MathUtil.Pi / 2, 0, 0) };
            var bodyMesh = GeometricPrimitive.Torus.New(GraphicsDevice, RotationGizmoRadius, RotationGizmoThickness, GizmoTessellation).ToMeshDraw();

            for (int axis = 0; axis < 3; ++axis)
            {
                var axisMaterial = GetAxisDefaultMaterial(axis);
                rotationAxes[axis] = new Entity("RotationGizmo" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
                rotationAxes[axis].Transform.RotationEulerXYZ = rotations[axis];
                entity.AddChild(rotationAxes[axis]);
            }

            // Add middle sphere
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, 0.25f * OriginSize, GizmoTessellation).ToMeshDraw();
            var sphereEntity = new Entity("OriginCube") { new ModelComponent { Model = new Model { DefaultOriginMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            entity.AddChild(sphereEntity);

            return entity;
        }


        protected override void UpdateTransformationAxis()
        {
            var newSelection = GizmoTransformationAxes.None;
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();

            const int CollisionTessellation = 20;
            const float ClickThickness = 2f * RotationGizmoThickness;

            // determine the mouse position normalized, centered and correctly oriented
            var mousePosition = Input.MousePosition;
            var screenPosition = new Vector2(2f * (mousePosition.X - 0.5f), -2f * (mousePosition.Y - 0.5f));

            // calculate view parameters
            var rayOriginView = Vector3.Zero;
            Vector3 rayDirectionView; //the ray direction corresponding to the click in the view space
            if (cameraService.IsOrthographic)
            {
                var halfSize = cameraService.Component.OrthographicSize/2f;
                rayOriginView = new Vector3(screenPosition.X*cameraService.AspectRatio*halfSize, screenPosition.Y*halfSize, 0);
                rayDirectionView = -Vector3.UnitZ;
            }
            else
            {
                var halfFov = MathUtil.DegreesToRadians(cameraService.VerticalFieldOfView/2f);
                rayDirectionView = Vector3.Normalize(new Vector3(cameraService.AspectRatio*screenPosition.X, screenPosition.Y, -1/(float)Math.Tan(halfFov)));
            }

            // calculate the view to gizmo space matrix 
            var gizmoMatrix = WorldMatrix;
            var gizmoViewInverse = Matrix.Invert(gizmoMatrix * cameraService.ViewMatrix);

            var minHitDistance = float.PositiveInfinity;

            // calculate the length of the box depending to tessellation
            const float Alpha = 2f * MathUtil.Pi / CollisionTessellation;
            var length = (float)Math.Sqrt(2f * RotationGizmoRadius * RotationGizmoRadius * (1 - (float)Math.Cos(Alpha)));
            length += ClickThickness * (float)Math.Tan((MathUtil.Pi / 2 - Alpha) / 2f); // avoid small gaps between elements

            // calculate the bounding box containing the segment
            var minimum = new Vector3(-ClickThickness);
            var maximum = new Vector3(+ClickThickness);
            minimum[0] = -length / 2;
            maximum[0] = +length / 2;
            var boundingBox = new BoundingBox(minimum, maximum);

            var boxRadius = RotationGizmoRadius * (float)Math.Cos(Alpha);
            var rotationMatrix = new[] { Matrix.RotationYawPitchRoll(MathUtil.Pi / 2, 0, 0), Matrix.RotationYawPitchRoll(0, MathUtil.Pi / 2, 0), Matrix.Identity };

            // select the axis whose intersection is the closest
            for (int i = 0; i < 3; i++)
            {
                var matrix = gizmoViewInverse * rotationMatrix[i];
                for (int t = 0; t < CollisionTessellation; ++t)
                {
                    // calculate the bounding box matrix
                    var alpha = t * Alpha;
                    var positionMatrix = Matrix.RotationZ(alpha);
                    positionMatrix.M42 = -boxRadius;
                    var boundingBoxMatrix = matrix * positionMatrix;

                    // calculate the ray in the gizmo space
                    var rayDirection = Vector3.TransformNormal(rayDirectionView, boundingBoxMatrix);
                    var rayOrigin = !cameraService.IsOrthographic ? boundingBoxMatrix.TranslationVector : Vector3.TransformCoordinate(rayOriginView, boundingBoxMatrix);
                    var ray = new Ray(rayOrigin, rayDirection);

                    // update the minimum hit distance and selection
                    float hitDistance;
                    if (boundingBox.Intersects(ref ray, out hitDistance) && hitDistance < minHitDistance)
                    {
                        minHitDistance = hitDistance;
                        newSelection = (GizmoTransformationAxes)(1 << i);
                    }
                }
            }

            TransformationAxes = newSelection;
        }

        protected override void UpdateColors()
        {
            base.UpdateColors();
            for (int axis = 0; axis < 3; axis++)
            {
                var axisMaterial = GetAxisDefaultMaterial(axis);
                var transformationAxis = (GizmoTransformationAxes)(1 << axis);
                bool isSelected = (TransformationAxes & transformationAxis) == transformationAxis;
                rotationAxes[axis].Get<ModelComponent>().Model.Materials[0] = isSelected ? ElementSelectedMaterial : axisMaterial;
            }
        }

        /// <summary>
        ///  We apply a rotation angle equivalent to the projection of the mouse translation to the screen transformation direction.
        /// </summary>
        /// <returns></returns>
        protected override InitialTransformation CalculateTransformation()
        {
            var mouseDrag = Input.MousePosition - StartMousePosition;
            var transformation = new InitialTransformation { Rotation = Quaternion.Identity, Scale = Vector3.One };

            // determine the rotation angle
            var rotationAngle = Vector2.Dot(new Vector2(mouseDrag.X, -mouseDrag.Y), TransformationDirection) * 2.1f * MathUtil.Pi; // half screen size if little bit more Pi

            // snap the rotation angle if necessary
            if (UseSnap)
            {
                var snapValue = MathUtil.DegreesToRadians(SnapValue);
                rotationAngle = MathUtil.Snap(rotationAngle, snapValue);
            }

            // determine the rotation axis in the Gizmo
            var rotationAxisGizmo = Vector3.Zero;
            for (int i = 0; i < 3; i++)
            {
                if ((TransformationAxes & ((GizmoTransformationAxes)(1 << i))) != 0)
                    rotationAxisGizmo[i] = 1;
            }
            rotationAxisGizmo.Normalize();

            // set the rotation to apply in the gizmo space
            transformation.Rotation = Quaternion.RotationAxis(rotationAxisGizmo, rotationAngle);

            return transformation;
        }

        protected override void OnTransformationFinished()
        {
            base.OnTransformationFinished();

            UpdateNotSelectedAxisVisibility(false);
        }

        protected override void OnTransformationStarted(Vector2 mouseDragPixel)
        {
            base.OnTransformationStarted(mouseDragPixel);

            UpdateNotSelectedAxisVisibility(true);
        }

        /// <summary>
        /// Update the visibility of the rotation axis that are not selected
        /// </summary>
        /// <param name="shouldHide"><value>True</value> if not selected axis should be hidden</param>
        public void UpdateNotSelectedAxisVisibility(bool shouldHide)
        {
            for (var i = 0; i < 3; ++i)
            {
                var axisChild = GizmoRootEntity.Transform.Children[i].Entity;
                var shouldHideAxis = shouldHide && ((int)TransformationAxes & (1 << i)) == 0;
                axisChild.Get<ModelComponent>().Enabled = !shouldHideAxis;
            }
        }
    }
}
