// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Rendering
{
    /// <summary>
    /// Keys used by transformations.
    /// </summary>
    public static partial class TransformationKeys
    {
        // TODO GRAPHICS REFACTOR
        ///// <summary>
        ///// Extracts the projected screem 2d vector from the projection matrix.
        ///// </summary>
        ///// <param name="projection">The projection.</param>
        ///// <param name="projScreenRay">The proj screen ray.</param>
        //private static void ExtractProjScreenRay(ref Matrix4x4 projection, ref Vector2 projScreenRay)
        //{
        //    projScreenRay = new Vector2(-1.0f / projection.M11, 1.0f / projection.M22);
        //}
        //
        //private static void InvertMatrix(ref Matrix4x4 inMatrix, ref Matrix4x4 outMatrix)
        //{
        //    Matrix4x4.InvertSafe(inMatrix, out outMatrix);
        //}
        //
        //private static void ExtractScale(ref Matrix4x4 inMatrix, ref Vector3 outVector)
        //{
        //    outVector.X = inMatrix.X.AsVector3().Length();
        //    outVector.Y = inMatrix.Y.AsVector3().Length();
        //    outVector.Z = inMatrix.Z.AsVector3().Length();
        //}
        //
        ///// <summary>
        ///// Invert the view matrix and build an eye vector.
        ///// </summary>
        ///// <param name="view">The view matrix.</param>
        ///// <param name="eye">The eye vector.</param>
        //private static void ViewToEye(ref Matrix4x4 view, ref Vector4 eye)
        //{
        //    Matrix4x4.InvertSafe(view, out var inverseView);
        //    eye = new Vector4(inverseView.M41, inverseView.M42, inverseView.M43, 1.0f);
        //}
        //
        //private static void WorldViewToEyeMS(ref Matrix4x4 worldView, ref Vector4 eyeMS)
        //{
        //    Matrix4x4.InvertSafe(worldView, out var inverseWorldView);
        //    eyeMS = new Vector4(inverseWorldView.M41, inverseWorldView.M42, inverseWorldView.M43, 1.0f);
        //}
        //
        //private static void TransposeMatrix(ref Matrix4x4 inMatrix, ref Matrix4x4 outMatrix)
        //{
        //    outMatrix = Matrix4x4.Transpose(inMatrix);
        //}
        //
        ///// <summary>
        ///// Extracts the frustum planes from given matrix.
        ///// </summary>
        ///// <param name="matrix">The matrix.</param>
        ///// <param name="planes">The planes.</param>
        //private static void ExtractFrustumPlanes(ref Matrix4x4 matrix, ref Plane[] planes)
        //{
        //    // Left
        //    planes[0] = Plane.Normalize(new Plane(
        //        matrix.M14 + matrix.M11,
        //        matrix.M24 + matrix.M21,
        //        matrix.M34 + matrix.M31,
        //        matrix.M44 + matrix.M41));
        //
        //    // Right
        //    planes[1] = Plane.Normalize(new Plane(
        //        matrix.M14 - matrix.M11,
        //        matrix.M24 - matrix.M21,
        //        matrix.M34 - matrix.M31,
        //        matrix.M44 - matrix.M41));
        //
        //    // Top
        //    planes[2] = Plane.Normalize(new Plane(
        //        matrix.M14 - matrix.M12,
        //        matrix.M24 - matrix.M22,
        //        matrix.M34 - matrix.M32,
        //        matrix.M44 - matrix.M42));
        //
        //    // Bottom
        //    planes[3] = Plane.Normalize(new Plane(
        //        matrix.M14 + matrix.M12,
        //        matrix.M24 + matrix.M22,
        //        matrix.M34 + matrix.M32,
        //        matrix.M44 + matrix.M42));
        //
        //    // Near
        //    planes[4] = Plane.Normalize(new Plane(
        //        matrix.M13,
        //        matrix.M23,
        //        matrix.M33,
        //        matrix.M43));
        //
        //    // Far
        //    planes[5] = Plane.Normalize(new Plane(
        //        matrix.M14 - matrix.M13,
        //        matrix.M24 - matrix.M23,
        //        matrix.M34 - matrix.M33,
        //        matrix.M44 - matrix.M43));
        //}
    }
}
