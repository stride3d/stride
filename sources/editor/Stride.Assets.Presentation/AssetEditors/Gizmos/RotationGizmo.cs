// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private const float OriginRadius = GizmoOriginScale * 0.05f;

        private readonly Entity[] rotationAxes = new Entity[3];

        private Material overlaySphereDefaultMaterial;
        private Material overlaySphereSelectedMaterial;

        private Entity overlaySphere;

        protected override Entity Create()
        {
            base.Create();

            overlaySphereDefaultMaterial = CreateUniformColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.025f));
            overlaySphereSelectedMaterial = CreateUniformColorMaterial(new Color(0.4f, 0.4f, 0.4f, 0.025f));

            var entity = new Entity("Rotation Gizmo");
            var rotations = new[] { new Vector3(0, 0, -MathUtil.Pi / 2), new Vector3(), new Vector3(MathUtil.Pi / 2, 0, 0) };
            var bodyMesh = GeometricPrimitive.Torus.New(GraphicsDevice, RotationGizmoRadius, RotationGizmoThickness, GizmoTessellation).ToMeshDraw();

            for (int axis = 0; axis < 3; ++axis)
            {
                var axisMaterial = GetAxisDefaultMaterial(axis);
                rotationAxes[axis] = new Entity("RotationGizmo" + axis) { new ModelComponent { Model = new Model { axisMaterial, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
                rotationAxes[axis].Transform.RotationEulerXYZ = rotations[axis];
                entity.AddChild(rotationAxes[axis]);
            }

            // Add overlay sphere
            var overlayMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, RotationGizmoRadius, GizmoTessellation).ToMeshDraw();
            overlaySphere = new Entity("OverlaySphere") { new ModelComponent { Model = new Model { overlaySphereDefaultMaterial, new Mesh { Draw = overlayMeshDraw } }, RenderGroup = RenderGroup } };
            entity.AddChild(overlaySphere);

            // Add middle sphere
            var sphereMeshDraw = GeometricPrimitive.Sphere.New(GraphicsDevice, OriginRadius, GizmoTessellation).ToMeshDraw();
            var rotationOrigin = new Entity("OriginSphere") { new ModelComponent { Model = new Model { DefaultOriginMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            entity.Transform.Children.Add(rotationOrigin.Transform);

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

            overlaySphere.Get<ModelComponent>().Model.Materials[0] = TransformationStarted ? overlaySphereSelectedMaterial : overlaySphereDefaultMaterial;
        }

        /// <summary>
        ///  We apply a rotation angle equivalent to the projection of the mouse translation to the screen transformation direction.
        /// </summary>
        /// <returns></returns>
        protected override InitialTransformation CalculateTransformation()
        {
            // TODO: use cameraComponent.WorldToScreenPosition instead once implemented
            // determine the anchor entity's screen position
            var anchorEntityWorldPosition = AnchorEntity.Transform.WorldMatrix.TranslationVector;
            var cameraComponent = Game.EditorServices.Get<IEditorGameCameraService>().Component;
            Vector3.TransformCoordinate(ref anchorEntityWorldPosition, ref cameraComponent.ViewProjectionMatrix, out var clipSpace);
            Vector3.TransformCoordinate(ref anchorEntityWorldPosition, ref cameraComponent.ViewMatrix, out var viewSpace);
            var anchorEntityScreenPosition = new Vector2
            {
                X = (clipSpace.X + 1f) / 2f,
                Y = (clipSpace.Y + 1f) / 2f - 1f,
            };

            // determine the vectors going from the anchor entity's position to the start and current mouse positions
            var anchorEntityToMouse = new Vector2(Input.MousePosition.X, -Input.MousePosition.Y) - anchorEntityScreenPosition;
            var anchorEntityToStartMouse = new Vector2(StartMousePosition.X, -StartMousePosition.Y) - anchorEntityScreenPosition;

            anchorEntityToMouse.X *= cameraComponent.AspectRatio;
            anchorEntityToStartMouse.X *= cameraComponent.AspectRatio;
            
            var transformation = new InitialTransformation { Rotation = Quaternion.Identity, Scale = Vector3.One };

            // determine the rotation angle
            var rotationAngle = MathF.Atan2(anchorEntityToMouse.X * anchorEntityToStartMouse.Y - anchorEntityToMouse.Y * anchorEntityToStartMouse.X, Vector2.Dot(anchorEntityToStartMouse, anchorEntityToMouse));

            // snap the rotation angle if necessary
            if (UseSnap)
            {
                var snapValue = MathUtil.DegreesToRadians(SnapValue);
                rotationAngle = MathUtil.Snap(rotationAngle, snapValue);
            }

            // determine the rotation axis
            var rotationAxisWorldUp =  rotationAxes[(int)TransformationAxes / 2].Transform.WorldMatrix.Up;
            var cameraToAnchorEntity = AnchorEntity.Transform.WorldMatrix.TranslationVector - Game.EditorServices.Get<IEditorGameCameraService>().Position;
            var rotationAxis = new Vector3(0) { [(int)TransformationAxes / 2] = MathF.Sign(Vector3.Dot(cameraToAnchorEntity, rotationAxisWorldUp)) };

            // set the rotation to apply in the gizmo space
            transformation.Rotation = Quaternion.RotationAxis(rotationAxis, rotationAngle);

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
