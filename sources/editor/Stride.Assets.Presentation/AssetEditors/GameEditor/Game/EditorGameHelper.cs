// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    internal static class EditorGameHelper
    {
        public static Ray CalculateRayFromMousePosition([NotNull] CameraComponent camera, Vector2 mousePosition, Matrix worldView)
        {
            // determine the mouse position normalized, centered and correctly oriented
            var screenPosition = new Vector2(2f * (mousePosition.X - 0.5f), -2f * (mousePosition.Y - 0.5f));

            if (camera.Projection == CameraProjectionMode.Perspective)
            {
                // calculate the ray direction corresponding to the click in the view space
                var verticalFov = MathUtil.DegreesToRadians(camera.VerticalFieldOfView);
                var rayDirectionView = Vector3.Normalize(new Vector3(camera.AspectRatio * screenPosition.X, screenPosition.Y, -1 / (float)Math.Tan(verticalFov / 2f)));

                // calculate the direction of the ray in the gizmo space
                var rayDirectionGizmo = Vector3.Normalize(Vector3.TransformNormal(rayDirectionView, worldView));

                return new Ray(worldView.TranslationVector, rayDirectionGizmo);
            }
            else
            {
                // calculate the direction of the ray in the gizmo space
                var rayDirectionGizmo = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, worldView));

                // calculate the position of the ray in the gizmo space
                var halfSize = camera.OrthographicSize / 2f;
                var rayOriginOffset = new Vector3(screenPosition.X * camera.AspectRatio * halfSize, screenPosition.Y * halfSize, 0);
                var rayOrigin = Vector3.TransformCoordinate(rayOriginOffset, worldView);

                return new Ray(rayOrigin, rayDirectionGizmo);
            }
        }

        public static Vector3 ProjectOnPlaneWithLimitAngle(Ray ray, Plane projectionPlane, float limitAngle)
        {
            //gridMaterial.Parameters.Set(MaterialParameters.AlbedoDiffuse, diffuse);
            Vector3 endPointGizmo;

            // Ensures a ray angle with projection plane of at least 'limitAngle' to avoid the object to go to infinity.
            var dotProductValue = Vector3.Dot(ray.Direction, projectionPlane.Normal);
            var comparisonSign = Math.Sign(Vector3.Dot(ray.Position, projectionPlane.Normal) + projectionPlane.D);
            if (comparisonSign * (Math.Acos(dotProductValue) - MathUtil.PiOverTwo) < limitAngle)
            {
                var rotationAxis = Vector3.Normalize(Vector3.Cross(projectionPlane.Normal, ray.Direction));
                var initialDirection = Vector3.Normalize(Vector3.Cross(rotationAxis, projectionPlane.Normal));
                ray.Direction = Vector3.Transform(initialDirection, Quaternion.RotationAxis(rotationAxis, comparisonSign * limitAngle));
            }
            projectionPlane.Intersects(ref ray, out endPointGizmo);

            return endPointGizmo;
        }
    }
}
