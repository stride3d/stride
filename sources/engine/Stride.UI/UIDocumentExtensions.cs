// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.UI;

public static class UIDocumentExtensions
{
    public static Matrix GetWorldViewProjection(this UIDocument uiDocument, CameraComponent camera, Texture renderTarget)
    {
        Matrix worldViewProjection = Matrix.Identity;

        // calculate the size of the virtual resolution depending on target size (UI canvas)
        var virtualResolution = uiDocument.Resolution;

        if (uiDocument.IsFullScreen)
        {
            //var targetSize = viewportSize;
            var targetSize = new Vector2(renderTarget.Width, renderTarget.Height);

            // update the virtual resolution of the renderer
            if (uiDocument.ResolutionStretch == ResolutionStretch.FixedWidthAdaptableHeight)
                virtualResolution.Y = virtualResolution.X * targetSize.Y / targetSize.X;
            if (uiDocument.ResolutionStretch == ResolutionStretch.FixedHeightAdaptableWidth)
                virtualResolution.X = virtualResolution.Y * targetSize.X / targetSize.Y;

            worldViewProjection = GetWorldViewProjection(uiDocument, virtualResolution);
        }
        else
        {
            if (camera != null)
                worldViewProjection = GetWorldViewProjection(uiDocument, camera);
        }

        return worldViewProjection;
    }
    
    private static Matrix GetWorldViewProjection(UIDocument uiDocument, Vector3 virtualResolution)
    {
        var nearPlane = virtualResolution.Z / 2;
        var farPlane = nearPlane + virtualResolution.Z;
        var zOffset = nearPlane + virtualResolution.Z / 2;
        var aspectRatio = virtualResolution.X / virtualResolution.Y;
        var verticalFov = MathF.Atan2(virtualResolution.Y / 2, zOffset) * 2;

        var cameraComponent = new CameraComponent(nearPlane, farPlane)
        {
            UseCustomAspectRatio = true,
            AspectRatio = aspectRatio,
            VerticalFieldOfView = MathUtil.RadiansToDegrees(verticalFov),
            ViewMatrix = Matrix.LookAtRH(new Vector3(0, 0, zOffset), Vector3.Zero, Vector3.UnitY),
            ProjectionMatrix = Matrix.PerspectiveFovRH(verticalFov, aspectRatio, nearPlane, farPlane),
        };

        return GetWorldViewProjection(uiDocument, cameraComponent);
    }

    private static Matrix GetWorldViewProjection(UIDocument uiDocument, CameraComponent camera)
    {
        var frustumHeight = 2 * MathF.Tan(MathUtil.DegreesToRadians(camera.VerticalFieldOfView) / 2);

        var worldMatrix = uiDocument.WorldMatrix;

        // rotate the UI element perpendicular to the camera view vector, if billboard is activated
        if (uiDocument.IsFullScreen)
        {
            worldMatrix = Matrix.Identity;
        }
        else
        {
            Matrix viewInverse;
            Matrix.Invert(ref camera.ViewMatrix, out viewInverse);
            var forwardVector = viewInverse.Forward;

            if (uiDocument.IsBillboard)
            {
                var viewInverseRow1 = viewInverse.Row1;
                var viewInverseRow2 = viewInverse.Row2;

                // remove scale of the camera
                viewInverseRow1 /= viewInverseRow1.XYZ().Length();
                viewInverseRow2 /= viewInverseRow2.XYZ().Length();

                // set the scale of the object
                viewInverseRow1 *= worldMatrix.Row1.XYZ().Length();
                viewInverseRow2 *= worldMatrix.Row2.XYZ().Length();

                // set the adjusted world matrix
                worldMatrix.Row1 = viewInverseRow1;
                worldMatrix.Row2 = viewInverseRow2;
                worldMatrix.Row3 = viewInverse.Row3;
            }

            if (uiDocument.IsFixedSize)
            {
                forwardVector.Normalize();
                var distVec = (worldMatrix.TranslationVector - viewInverse.TranslationVector);
                float distScalar;
                Vector3.Dot(ref forwardVector, ref distVec, out distScalar);
                distScalar = Math.Abs(distScalar);

                var worldScale = frustumHeight * distScalar * UIComponent.FixedSizeVerticalUnit; // FrustumHeight already is 2*Tan(FOV/2)

                worldMatrix.Row1 *= worldScale;
                worldMatrix.Row2 *= worldScale;
                worldMatrix.Row3 *= worldScale;
            }

            // If the UI component is not drawn fullscreen it should be drawn as a quad with world sizes corresponding to its actual size
            worldMatrix = Matrix.Scaling(uiDocument.Size / uiDocument.Resolution) * worldMatrix;
        }

        // Rotation of Pi along 0x to go from UI space to world space
        worldMatrix.Row2 = -worldMatrix.Row2;
        worldMatrix.Row3 = -worldMatrix.Row3;

        Matrix worldViewMatrix;
        Matrix worldViewProjectionMatrix;
        Matrix.Multiply(ref worldMatrix, ref camera.ViewMatrix, out worldViewMatrix);
        Matrix.Multiply(ref worldViewMatrix, ref camera.ProjectionMatrix, out worldViewProjectionMatrix);

        return worldViewProjectionMatrix;
    }
}
