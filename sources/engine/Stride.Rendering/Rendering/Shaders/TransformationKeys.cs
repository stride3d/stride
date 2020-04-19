// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Rendering
{
    /// <summary>
    /// Keys used by transformations.
    /// </summary>
    public static partial class TransformationKeys
    {
        static TransformationKeys()
        {
            View = ParameterKeys.NewValue(Matrix.Identity);
            Projection = ParameterKeys.NewValue(Matrix.Identity);
            World = ParameterKeys.NewValue(Matrix.Identity);
            WorldView = ParameterKeys.NewValue(Matrix.Identity);
            WorldViewProjection = ParameterKeys.NewValue(Matrix.Identity);
            WorldInverse = ParameterKeys.NewValue(Matrix.Identity);
            WorldInverseTranspose = ParameterKeys.NewValue(Matrix.Identity);
            ViewInverse = ParameterKeys.NewValue(Matrix.Identity);
            ProjectionInverse = ParameterKeys.NewValue(Matrix.Identity);
            WorldViewInverse = ParameterKeys.NewValue(Matrix.Identity);
            WorldScale = ParameterKeys.NewValue(Vector3.One);
        }

        // TODO GRAPHICS REFACTOR
        ///// <summary>
        ///// Extracts the projected screem 2d vector from the projection matrix.
        ///// </summary>
        ///// <param name="projection">The projection.</param>
        ///// <param name="projScreenRay">The proj screen ray.</param>
        //private static void ExtractProjScreenRay(ref Matrix projection, ref Vector2 projScreenRay)
        //{
        //    projScreenRay = new Vector2(-1.0f / projection.M11, 1.0f / projection.M22);
        //}
        //
        //private static void InvertMatrix(ref Matrix inMatrix, ref Matrix outMatrix)
        //{
        //    Matrix.Invert(ref inMatrix, out outMatrix);
        //}
        //
        //private static void ExtractScale(ref Matrix inMatrix, ref Vector3 outVector)
        //{
        //    outVector.X = ((Vector3)inMatrix.Row1).Length();
        //    outVector.Y = ((Vector3)inMatrix.Row2).Length();
        //    outVector.Z = ((Vector3)inMatrix.Row3).Length();
        //}
        //
        ///// <summary>
        ///// Invert the view matrix and build an eye vector.
        ///// </summary>
        ///// <param name="view">The view matrix.</param>
        ///// <param name="eye">The eye vector.</param>
        //private static void ViewToEye(ref Matrix view, ref Vector4 eye)
        //{
        //    Matrix inverseView;
        //    Matrix.Invert(ref view, out inverseView);
        //    eye = new Vector4(inverseView.M41, inverseView.M42, inverseView.M43, 1.0f);
        //}
        //
        //private static void WorldViewToEyeMS(ref Matrix worldView, ref Vector4 eyeMS)
        //{
        //    Matrix inverseWorldView;
        //    Matrix.Invert(ref worldView, out inverseWorldView);
        //    eyeMS = new Vector4(inverseWorldView.M41, inverseWorldView.M42, inverseWorldView.M43, 1.0f);
        //}
        //
        //private static void TransposeMatrix(ref Matrix inMatrix, ref Matrix outMatrix)
        //{
        //    Matrix.Transpose(ref inMatrix, out outMatrix);
        //}
        //
        ///// <summary>
        ///// Extracts the frustum planes from given matrix.
        ///// </summary>
        ///// <param name="matrix">The matrix.</param>
        ///// <param name="planes">The planes.</param>
        //private static void ExtractFrustumPlanes(ref Matrix matrix, ref Plane[] planes)
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
