// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
#pragma warning disable SA1107 // Code must not contain multiple statements on one line
#pragma warning disable SA1117 // Parameters must be on same line or separate lines
#pragma warning disable SA1313 // Parameter names must begin with lower-case letter

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Stride.Core.Mathematics;

/// <summary>
/// The members of Stride's math API that <see cref="Matrix4x4"/> does not have.
/// </summary>
public static class Matrix4x4Extensions
{
    extension(Matrix4x4)
    {
        /// <summary>Calculates the inverse of a matrix; a matrix that has none gives the zero matrix, and <c>false</c>.</summary>
        /// <remarks>The same variable can be given as both arguments.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InvertSafe(in Matrix4x4 matrix, out Matrix4x4 result)
        {
            if (Matrix4x4.Invert(matrix, out result))
                return true;
            result = default;
            return false;
        }

        /// <summary>Creates a matrix from its 16 elements, row by row.</summary>
        public static Matrix4x4 Create(ReadOnlySpan<float> values)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(values.Length, 16);
            return MemoryMarshal.Read<Matrix4x4>(MemoryMarshal.AsBytes(values));
        }

        /// <summary>
        /// Scales a matrix by the given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix4x4 Divide(in Matrix4x4 left, float right)
        {
            Matrix4x4 result;
            float inv = 1.0f / right;

            result.M11 = left.M11 * inv;
            result.M21 = left.M21 * inv;
            result.M31 = left.M31 * inv;
            result.M41 = left.M41 * inv;
            result.M12 = left.M12 * inv;
            result.M22 = left.M22 * inv;
            result.M32 = left.M32 * inv;
            result.M42 = left.M42 * inv;
            result.M13 = left.M13 * inv;
            result.M23 = left.M23 * inv;
            result.M33 = left.M33 * inv;
            result.M43 = left.M43 * inv;
            result.M14 = left.M14 * inv;
            result.M24 = left.M24 * inv;
            result.M34 = left.M34 * inv;
            result.M44 = left.M44 * inv;
            return result;
        }

        /// <summary>
        /// Performs the exponential operation on a matrix.
        /// </summary>
        /// <param name="value">The matrix to perform the operation on.</param>
        /// <param name="exponent">The exponent to raise the matrix to.</param>
        /// <returns>The exponential matrix.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="exponent"/> is negative.</exception>
        public static Matrix4x4 Exponent(in Matrix4x4 value, int exponent)
        {
            Matrix4x4 result;
            //Source: http://rosettacode.org
            //Refrence: http://rosettacode.org/wiki/Matrix-exponentiation_operator

            ArgumentOutOfRangeException.ThrowIfNegative(exponent);

            if (exponent == 0)
            {
                result = Matrix4x4.Identity;
                return result;
            }

            if (exponent == 1)
            {
                result = value;
                return result;
            }

            Matrix4x4 identity = Matrix4x4.Identity;
            Matrix4x4 temp = value;

            while (true)
            {
                if ((exponent & 1) != 0)
                    identity *= temp;

                exponent /= 2;

                if (exponent > 0)
                    temp *= temp;
                else
                    break;
            }

            result = identity;
            return result;
        }

        /// <summary>
        /// Performs a cubic interpolation between two matrices.
        /// </summary>
        /// <param name="start">Start matrix.</param>
        /// <param name="end">End matrix.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two matrices.</returns>
        public static Matrix4x4 SmoothStep(in Matrix4x4 start, in Matrix4x4 end, float amount)
        {
            Matrix4x4 result;
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = amount * amount * (3.0f - (2.0f * amount));

            result.M11 = start.M11 + ((end.M11 - start.M11) * amount);
            result.M21 = start.M21 + ((end.M21 - start.M21) * amount);
            result.M31 = start.M31 + ((end.M31 - start.M31) * amount);
            result.M41 = start.M41 + ((end.M41 - start.M41) * amount);
            result.M12 = start.M12 + ((end.M12 - start.M12) * amount);
            result.M22 = start.M22 + ((end.M22 - start.M22) * amount);
            result.M32 = start.M32 + ((end.M32 - start.M32) * amount);
            result.M42 = start.M42 + ((end.M42 - start.M42) * amount);
            result.M13 = start.M13 + ((end.M13 - start.M13) * amount);
            result.M23 = start.M23 + ((end.M23 - start.M23) * amount);
            result.M33 = start.M33 + ((end.M33 - start.M33) * amount);
            result.M43 = start.M43 + ((end.M43 - start.M43) * amount);
            result.M14 = start.M14 + ((end.M14 - start.M14) * amount);
            result.M24 = start.M24 + ((end.M24 - start.M24) * amount);
            result.M34 = start.M34 + ((end.M34 - start.M34) * amount);
            result.M44 = start.M44 + ((end.M44 - start.M44) * amount);
            return result;
        }

        /// <summary>
        /// Orthogonalizes the specified matrix.
        /// </summary>
        /// <param name="value">The matrix to orthogonalize.</param>
        /// <returns>The orthogonalized matrix.</returns>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all rows orthogonal to each other. This
        /// means that any given row in the matrix will be orthogonal to any other given row in the
        /// matrix.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static Matrix4x4 Orthogonalize(in Matrix4x4 value)
        {
            //Uses the modified Gram-Schmidt process.
            //q1 = m1
            //q2 = m2 - ((q1 ⋅ m2) / (q1 ⋅ q1)) * q1
            //q3 = m3 - ((q1 ⋅ m3) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m3) / (q2 ⋅ q2)) * q2
            //q4 = m4 - ((q1 ⋅ m4) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m4) / (q2 ⋅ q2)) * q2 - ((q3 ⋅ m4) / (q3 ⋅ q3)) * q3

            //By separating the above algorithm into multiple lines, we actually increase accuracy.
            Matrix4x4 result = value;

            var row1 = result.X;
            var row2 = result.Y;
            var row3 = result.Z;
            var row4 = result.W;

            row2 -= Vector4.Dot(row1, row2) / Vector4.Dot(row1, row1) * row1;

            row3 -= Vector4.Dot(row1, row3) / Vector4.Dot(row1, row1) * row1;
            row3 -= Vector4.Dot(row2, row3) / Vector4.Dot(row2, row2) * row2;

            row4 -= Vector4.Dot(row1, row4) / Vector4.Dot(row1, row1) * row1;
            row4 -= Vector4.Dot(row2, row4) / Vector4.Dot(row2, row2) * row2;
            row4 -= Vector4.Dot(row3, row4) / Vector4.Dot(row3, row3) * row3;

            result.Y = row2;
            result.Z = row3;
            result.W = row4;
            return result;
        }

        /// <summary>
        /// Orthonormalizes the specified matrix.
        /// </summary>
        /// <param name="value">The matrix to orthonormalize.</param>
        /// <returns>The orthonormalized matrix.</returns>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all rows and columns orthogonal to each
        /// other and making all rows and columns of unit length. This means that any given row will
        /// be orthogonal to any other given row and any given column will be orthogonal to any other
        /// given column. Any given row will not be orthogonal to any given column. Every row and every
        /// column will be of unit length.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting matrix
        /// tends to be numerically unstable. The numeric stability decreases according to the rows
        /// so that the first row is the most stable and the last row is the least stable.</para>
        /// <para>This operation is performed on the rows of the matrix rather than the columns.
        /// If you wish for this operation to be performed on the columns, first transpose the
        /// input and than transpose the output.</para>
        /// </remarks>
        public static Matrix4x4 Orthonormalize(in Matrix4x4 value)
        {
            //Uses the modified Gram-Schmidt process.
            //Because we are making unit vectors, we can optimize the math for orthogonalization
            //and simplify the projection operation to remove the division.
            //q1 = m1 / |m1|
            //q2 = (m2 - (q1 ⋅ m2) * q1) / |m2 - (q1 ⋅ m2) * q1|
            //q3 = (m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2) / |m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2|
            //q4 = (m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3) / |m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3|

            //By separating the above algorithm into multiple lines, we actually increase accuracy.
            var row1 = value.X;
            var row2 = value.Y;
            var row3 = value.Z;
            var row4 = value.W;

            row1 = Vector4.NormalizeSafe(row1);

            row2 -= Vector4.Dot(row1, row2) * row1;
            row2 = Vector4.NormalizeSafe(row2);

            row3 -= Vector4.Dot(row1, row3) * row1;
            row3 -= Vector4.Dot(row2, row3) * row2;
            row3 = Vector4.NormalizeSafe(row3);

            row4 -= Vector4.Dot(row1, row4) * row1;
            row4 -= Vector4.Dot(row2, row4) * row2;
            row4 -= Vector4.Dot(row3, row4) * row3;
            row4 = Vector4.NormalizeSafe(row4);

            Matrix4x4 result = default;
            result.X = row1;
            result.Y = row2;
            result.Z = row3;
            result.W = row4;
            return result;
        }

        /// <summary>
        /// Brings the matrix into upper triangular form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into upper triangular form.</param>
        /// <returns>The upper triangular matrix.</returns>
        /// <remarks>
        /// If the matrix is not invertable (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the matrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static Matrix4x4 UpperTriangularForm(in Matrix4x4 value)
        {
            //Adapted from the row echelon code.
            Matrix4x4 result = value;
            int lead = 0;
            const int rowcount = 4;
            const int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return result;

                int i = r;

                while (MathF.Abs(result[i, lead]) < MathUtil.ZeroTolerance)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return result;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * multiplier * result[i, lead];
                        result[i, 1] -= result[r, 1] * multiplier * result[i, lead];
                        result[i, 2] -= result[r, 2] * multiplier * result[i, lead];
                        result[i, 3] -= result[r, 3] * multiplier * result[i, lead];
                    }
                }

                lead++;
            }
            return result;
        }

        /// <summary>
        /// Brings the matrix into lower triangular form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into lower triangular form.</param>
        /// <returns>The lower triangular matrix.</returns>
        /// <remarks>
        /// If the matrix is not invertable (i.e. its determinant is zero) than the result of this
        /// method may produce Single.Nan and Single.Inf values. When the matrix represents a system
        /// of linear equations, than this often means that either no solution exists or an infinite
        /// number of solutions exist.
        /// </remarks>
        public static Matrix4x4 LowerTriangularForm(in Matrix4x4 value)
        {
            //Adapted from the row echelon code.
            Matrix4x4 temp = value;
            Matrix4x4 result = Matrix4x4.Transpose(temp);

            int lead = 0;
            const int rowcount = 4;
            const int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return result;

                int i = r;

                while (MathF.Abs(result[i, lead]) < MathUtil.ZeroTolerance)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return result;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * multiplier * result[i, lead];
                        result[i, 1] -= result[r, 1] * multiplier * result[i, lead];
                        result[i, 2] -= result[r, 2] * multiplier * result[i, lead];
                        result[i, 3] -= result[r, 3] * multiplier * result[i, lead];
                    }
                }

                lead++;
            }

            result = Matrix4x4.Transpose(result);
            return result;
        }

        /// <summary>
        /// Brings the matrix into row echelon form using elementry row operations;
        /// </summary>
        /// <param name="value">The matrix to put into row echelon form.</param>
        /// <returns>When the method completes, contains the row echelon form of the matrix.</returns>
        public static Matrix4x4 RowEchelonForm(in Matrix4x4 value)
        {
            //Source: Wikipedia psuedo code
            //Reference: http://en.wikipedia.org/wiki/Row_echelon_form#Pseudocode

            Matrix4x4 result = value;
            int lead = 0;
            const int rowcount = 4;
            const int columncount = 4;

            for (int r = 0; r < rowcount; ++r)
            {
                if (columncount <= lead)
                    return result;

                int i = r;

                while (MathF.Abs(result[i, lead]) < MathUtil.ZeroTolerance)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (lead == columncount)
                            return result;
                    }
                }

                if (i != r)
                {
                    result.ExchangeRows(i, r);
                }

                float multiplier = 1f / result[r, lead];
                result[r, 0] *= multiplier;
                result[r, 1] *= multiplier;
                result[r, 2] *= multiplier;
                result[r, 3] *= multiplier;

                for (; i < rowcount; ++i)
                {
                    if (i != r)
                    {
                        result[i, 0] -= result[r, 0] * result[i, lead];
                        result[i, 1] -= result[r, 1] * result[i, lead];
                        result[i, 2] -= result[r, 2] * result[i, lead];
                        result[i, 3] -= result[r, 3] * result[i, lead];
                    }
                }

                lead++;
            }
            return result;
        }

        /// <summary>
        /// Brings the matrix into reduced row echelon form using elementry row operations.
        /// </summary>
        /// <param name="value">The matrix to put into reduced row echelon form.</param>
        /// <param name="augment">The fifth column of the matrix.</param>
        /// <param name="result">When the method completes, contains the resultant matrix after the operation.</param>
        /// <param name="augmentResult">When the method completes, contains the resultant fifth column of the matrix.</param>
        /// <remarks>
        /// <para>The fifth column is often called the agumented part of the matrix. This is because the fifth
        /// column is really just an extension of the matrix so that there is a place to put all of the
        /// non-zero components after the operation is complete.</para>
        /// <para>Often times the resultant matrix will the identity matrix or a matrix similar to the identity
        /// matrix. Sometimes, however, that is not possible and numbers other than zero and one may appear.</para>
        /// <para>This method can be used to solve systems of linear equations. Upon completion of this method,
        /// the <paramref name="augmentResult"/> will contain the solution for the system. It is up to the user
        /// to analyze both the input and the result to determine if a solution really exists.</para>
        /// </remarks>
        public static void ReducedRowEchelonForm(in Matrix4x4 value, in Vector4 augment, out Matrix4x4 result, out Vector4 augmentResult)
        {
            //Source: http://rosettacode.org
            //Reference: http://rosettacode.org/wiki/Reduced_row_echelon_form

            float[,] matrix = new float[4, 5];

            matrix[0, 0] = value[0, 0];
            matrix[0, 1] = value[0, 1];
            matrix[0, 2] = value[0, 2];
            matrix[0, 3] = value[0, 3];
            matrix[0, 4] = augment[0];

            matrix[1, 0] = value[1, 0];
            matrix[1, 1] = value[1, 1];
            matrix[1, 2] = value[1, 2];
            matrix[1, 3] = value[1, 3];
            matrix[1, 4] = augment[1];

            matrix[2, 0] = value[2, 0];
            matrix[2, 1] = value[2, 1];
            matrix[2, 2] = value[2, 2];
            matrix[2, 3] = value[2, 3];
            matrix[2, 4] = augment[2];

            matrix[3, 0] = value[3, 0];
            matrix[3, 1] = value[3, 1];
            matrix[3, 2] = value[3, 2];
            matrix[3, 3] = value[3, 3];
            matrix[3, 4] = augment[3];

            int lead = 0;
            const int rowcount = 4;
            const int columncount = 5;

            for (int r = 0; r < rowcount; r++)
            {
                if (columncount <= lead)
                    break;

                int i = r;

                while (matrix[i, lead] == 0)
                {
                    i++;

                    if (i == rowcount)
                    {
                        i = r;
                        lead++;

                        if (columncount == lead)
                            break;
                    }
                }

                for (int j = 0; j < columncount; j++)
                {
                    (matrix[r, j], matrix[i, j]) = (matrix[i, j], matrix[r, j]);
                }

                float div = matrix[r, lead];

                for (int j = 0; j < columncount; j++)
                {
                    matrix[r, j] /= div;
                }

                for (int j = 0; j < rowcount; j++)
                {
                    if (j != r)
                    {
                        float sub = matrix[j, lead];
                        for (int k = 0; k < columncount; k++) matrix[j, k] -= (sub * matrix[r, k]);
                    }
                }

                lead++;
            }

            result.M11 = matrix[0, 0];
            result.M12 = matrix[0, 1];
            result.M13 = matrix[0, 2];
            result.M14 = matrix[0, 3];

            result.M21 = matrix[1, 0];
            result.M22 = matrix[1, 1];
            result.M23 = matrix[1, 2];
            result.M24 = matrix[1, 3];

            result.M31 = matrix[2, 0];
            result.M32 = matrix[2, 1];
            result.M33 = matrix[2, 2];
            result.M34 = matrix[2, 3];

            result.M41 = matrix[3, 0];
            result.M42 = matrix[3, 1];
            result.M43 = matrix[3, 2];
            result.M44 = matrix[3, 3];

            augmentResult.X = matrix[0, 4];
            augmentResult.Y = matrix[1, 4];
            augmentResult.Z = matrix[2, 4];
            augmentResult.W = matrix[3, 4];
        }

        /// <summary>
        /// Creates a matrix that flattens geometry into a shadow.
        /// </summary>
        /// <param name="light">The light direction. If the W component is 0, the light is directional light; if the
        /// W component is 1, the light is a point light.</param>
        /// <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
        /// <returns>The shadow matrix.</returns>
        public static Matrix4x4 CreateShadow(Vector4 light, Plane plane)
        {
            Matrix4x4 result;
            float dot = (plane.Normal.X * light.X) + (plane.Normal.Y * light.Y) + (plane.Normal.Z * light.Z) + (plane.D * light.W);
            float x = -plane.Normal.X;
            float y = -plane.Normal.Y;
            float z = -plane.Normal.Z;
            float d = -plane.D;

            result.M11 = (x * light.X) + dot;
            result.M21 = y * light.X;
            result.M31 = z * light.X;
            result.M41 = d * light.X;
            result.M12 = x * light.Y;
            result.M22 = (y * light.Y) + dot;
            result.M32 = z * light.Y;
            result.M42 = d * light.Y;
            result.M13 = x * light.Z;
            result.M23 = y * light.Z;
            result.M33 = (z * light.Z) + dot;
            result.M43 = d * light.Z;
            result.M14 = x * light.W;
            result.M24 = y * light.W;
            result.M34 = z * light.W;
            result.M44 = (d * light.W) + dot;
            return result;
        }

        /// <summary>
        /// Creates a matrix that contains both the X, Y and Z rotation, as well as scaling and translation. Note: This function is NOT thead safe.
        /// </summary>
        /// <param name="scaling">The scaling.</param>
        /// <param name="rotation">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="translation">The translation.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void CreateTransformation(in Vector3 scaling, in Quaternion rotation, in Vector3 translation, out Matrix4x4 result)
        {
            // Equivalent to:
            //result =
            //    Matrix.Scaling(scaling)
            //    *Matrix.RotationX(rotation.X)
            //    *Matrix.RotationY(rotation.Y)
            //    *Matrix.RotationZ(rotation.Z)
            //    *Matrix.Position(translation);

            // Rotation
            float xx = rotation.X * rotation.X;
            float yy = rotation.Y * rotation.Y;
            float zz = rotation.Z * rotation.Z;
            float xy = rotation.X * rotation.Y;
            float zw = rotation.Z * rotation.W;
            float zx = rotation.Z * rotation.X;
            float yw = rotation.Y * rotation.W;
            float yz = rotation.Y * rotation.Z;
            float xw = rotation.X * rotation.W;

            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));

            // Position
            result.M41 = translation.X;
            result.M42 = translation.Y;
            result.M43 = translation.Z;

            // Scale
            if (scaling.X != 1.0f)
            {
                result.M11 *= scaling.X;
                result.M12 *= scaling.X;
                result.M13 *= scaling.X;
            }
            if (scaling.Y != 1.0f)
            {
                result.M21 *= scaling.Y;
                result.M22 *= scaling.Y;
                result.M23 *= scaling.Y;
            }
            if (scaling.Z != 1.0f)
            {
                result.M31 *= scaling.Z;
                result.M32 *= scaling.Z;
                result.M33 *= scaling.Z;
            }

            result.M14 = 0.0f;
            result.M24 = 0.0f;
            result.M34 = 0.0f;
            result.M44 = 1.0f;
        }

        /// <summary>
        /// Creates a 3D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateAffineTransformation(float scaling, Quaternion rotation, Vector3 translation)
        {
            return Matrix4x4.CreateScale(scaling) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        }

        /// <summary>
        /// Creates a 3D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateAffineTransformation(float scaling, Vector3 rotationCenter, Quaternion rotation, Vector3 translation)
        {
            return Matrix4x4.CreateScale(scaling) * Matrix4x4.CreateTranslation(-rotationCenter) * Matrix4x4.CreateFromQuaternion(rotation) *
                Matrix4x4.CreateTranslation(rotationCenter) * Matrix4x4.CreateTranslation(translation);
        }

        /// <summary>
        /// Creates a 2D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateAffineTransformation2D(float scaling, float rotation, Vector2 translation)
        {
            return Matrix4x4.CreateScale(scaling, scaling, 1.0f) * Matrix4x4.CreateRotationZ(rotation) * Matrix4x4.CreateTranslation(translation.AsVector3());
        }

        /// <summary>
        /// Creates a 2D affine transformation matrix.
        /// </summary>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created affine transformation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateAffineTransformation2D(float scaling, Vector2 rotationCenter, float rotation, Vector2 translation)
        {
            return Matrix4x4.CreateScale(scaling, scaling, 1.0f) * Matrix4x4.CreateTranslation((-rotationCenter).AsVector3()) * Matrix4x4.CreateRotationZ(rotation) *
                Matrix4x4.CreateTranslation(rotationCenter.AsVector3()) * Matrix4x4.CreateTranslation(translation.AsVector3());
        }

        /// <summary>
        /// Creates a transformation matrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created transformation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateTransformation(Vector3 scalingCenter, Quaternion scalingRotation, Vector3 scaling, Vector3 rotationCenter, Quaternion rotation, Vector3 translation)
        {
            Matrix4x4 sr = Matrix4x4.CreateFromQuaternion(scalingRotation);

            return Matrix4x4.CreateTranslation(-scalingCenter) * Matrix4x4.Transpose(sr) * Matrix4x4.CreateScale(scaling) * sr * Matrix4x4.CreateTranslation(scalingCenter) * Matrix4x4.CreateTranslation(-rotationCenter) *
                Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(rotationCenter) * Matrix4x4.CreateTranslation(translation);
        }

        /// <summary>
        /// Creates a 2D transformation matrix.
        /// </summary>
        /// <param name="scalingCenter">Center point of the scaling operation.</param>
        /// <param name="scalingRotation">Scaling rotation amount.</param>
        /// <param name="scaling">Scaling factor.</param>
        /// <param name="rotationCenter">The center of the rotation.</param>
        /// <param name="rotation">The rotation of the transformation.</param>
        /// <param name="translation">The translation factor of the transformation.</param>
        /// <returns>The created transformation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 CreateTransformation2D(Vector2 scalingCenter, float scalingRotation, Vector2 scaling, Vector2 rotationCenter, float rotation, Vector2 translation)
        {
            Matrix4x4 result = Matrix4x4.CreateTranslation((-scalingCenter).AsVector3()) * Matrix4x4.CreateRotationZ(-scalingRotation) * Matrix4x4.CreateScale(scaling.AsVector3()) * Matrix4x4.CreateRotationZ(scalingRotation) * Matrix4x4.CreateTranslation(scalingCenter.AsVector3()) *
                Matrix4x4.CreateTranslation((-rotationCenter).AsVector3()) * Matrix4x4.CreateRotationZ(rotation) * Matrix4x4.CreateTranslation(rotationCenter.AsVector3()) * Matrix4x4.CreateTranslation(translation.AsVector3());

            result.M33 = 1f;
            result.M44 = 1f;
            return result;
        }

        /// <summary>
        /// Scales a matrix by a given value.
        /// </summary>
        /// <param name="left">The amount by which to scale.</param>
        /// <param name="right">The matrix to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix4x4 operator *(float left, in Matrix4x4 right)
        {
            var result = Matrix4x4.Multiply(right, left);
            return result;
        }

        /// <summary>
        /// Scales a matrix by a given value.
        /// </summary>
        /// <param name="left">The matrix to scale.</param>
        /// <param name="right">The amount by which to scale.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix4x4 operator /(in Matrix4x4 left, float right)
        {
            var result = Matrix4x4.Divide(left, right);
            return result;
        }

    }

    extension(in Matrix4x4 matrix)
    {
        /// <summary>Returns the text System.Numerics gives for a matrix, with each number formatted as asked.</summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            string Row(char row, Vector4 values)
                => $"{{M{row}1:{values.X.ToString(format, formatProvider)} M{row}2:{values.Y.ToString(format, formatProvider)} M{row}3:{values.Z.ToString(format, formatProvider)} M{row}4:{values.W.ToString(format, formatProvider)}}}";

            return $"{{ {Row('1', matrix.X)} {Row('2', matrix.Y)} {Row('3', matrix.Z)} {Row('4', matrix.W)} }}";
        }

        /// <summary>
        /// Gets or sets the first column in the matrix; that is M11, M21, M31, and M41.
        /// </summary>
        public Vector4 Column1
        {
            get { return new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41); }
        }

        /// <summary>
        /// Gets or sets the second column in the matrix; that is M12, M22, M32, and M42.
        /// </summary>
        public Vector4 Column2
        {
            get { return new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42); }
        }

        /// <summary>
        /// Gets or sets the third column in the matrix; that is M13, M23, M33, and M43.
        /// </summary>
        public Vector4 Column3
        {
            get { return new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43); }
        }

        /// <summary>
        /// Gets or sets the fourth column in the matrix; that is M14, M24, M34, and M44.
        /// </summary>
        public Vector4 Column4
        {
            get { return new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44); }
        }

        /// <summary>
        /// Gets or sets the up <see cref="Vector3"/> of the matrix; that is M21, M22, and M23.
        /// </summary>
        public Vector3 Up
        {
            get { return new Vector3(matrix.M21, matrix.M22, matrix.M23); }
        }

        /// <summary>
        /// Gets or sets the down <see cref="Vector3"/> of the matrix; that is -M21, -M22, and -M23.
        /// </summary>
        public Vector3 Down
        {
            get { return new Vector3(-matrix.M21, -matrix.M22, -matrix.M23); }
        }

        /// <summary>
        /// Gets or sets the right <see cref="Vector3"/> of the matrix; that is M11, M12, and M13.
        /// </summary>
        public Vector3 Right
        {
            get { return new Vector3(matrix.M11, matrix.M12, matrix.M13); }
        }

        /// <summary>
        /// Gets or sets the left <see cref="Vector3"/> of the matrix; that is -M11, -M12, and -M13.
        /// </summary>
        public Vector3 Left
        {
            get { return new Vector3(-matrix.M11, -matrix.M12, -matrix.M13); }
        }

        /// <summary>
        /// Gets or sets the forward <see cref="Vector3"/> of the matrix; that is -M31, -M32, and -M33.
        /// </summary>
        public Vector3 Forward
        {
            get { return new Vector3(-matrix.M31, -matrix.M32, -matrix.M33); }
        }

        /// <summary>
        /// Gets or sets the backward <see cref="Vector3"/> of the matrix; that is M31, M32, and M33.
        /// </summary>
        public Vector3 Backward
        {
            get { return new Vector3(matrix.M31, matrix.M32, matrix.M33); }
        }

        /// <summary>
        /// Decomposes a matrix into an orthonormalized matrix Q and a right triangular matrix R.
        /// </summary>
        /// <param name="Q">When the method completes, contains the orthonormalized matrix of the decomposition.</param>
        /// <param name="R">When the method completes, contains the right triangular matrix of the decomposition.</param>
        public void DecomposeQR(out Matrix4x4 Q, out Matrix4x4 R)
        {
            Matrix4x4 temp = matrix;
            temp = Matrix4x4.Transpose(temp);
            Q = Matrix4x4.Orthonormalize(temp);
            Q = Matrix4x4.Transpose(Q);

            R = new Matrix4x4
            {
                M11 = Vector4.Dot(Q.Column1, matrix.Column1),
                M12 = Vector4.Dot(Q.Column1, matrix.Column2),
                M13 = Vector4.Dot(Q.Column1, matrix.Column3),
                M14 = Vector4.Dot(Q.Column1, matrix.Column4),

                M22 = Vector4.Dot(Q.Column2, matrix.Column2),
                M23 = Vector4.Dot(Q.Column2, matrix.Column3),
                M24 = Vector4.Dot(Q.Column2, matrix.Column4),

                M33 = Vector4.Dot(Q.Column3, matrix.Column3),
                M34 = Vector4.Dot(Q.Column3, matrix.Column4),

                M44 = Vector4.Dot(Q.Column4, matrix.Column4)
            };
        }

        /// <summary>
        /// Decomposes a matrix into a lower triangular matrix L and an orthonormalized matrix Q.
        /// </summary>
        /// <param name="L">When the method completes, contains the lower triangular matrix of the decomposition.</param>
        /// <param name="Q">When the method completes, contains the orthonormalized matrix of the decomposition.</param>
        public void DecomposeLQ(out Matrix4x4 L, out Matrix4x4 Q)
        {
            Q = Matrix4x4.Orthonormalize(matrix);

            L = new Matrix4x4
            {
                M11 = Vector4.Dot(Q.X, matrix.X),

                M21 = Vector4.Dot(Q.X, matrix.Y),
                M22 = Vector4.Dot(Q.Y, matrix.Y),

                M31 = Vector4.Dot(Q.X, matrix.Z),
                M32 = Vector4.Dot(Q.Y, matrix.Z),
                M33 = Vector4.Dot(Q.Z, matrix.Z),

                M41 = Vector4.Dot(Q.X, matrix.W),
                M42 = Vector4.Dot(Q.Y, matrix.W),
                M43 = Vector4.Dot(Q.Z, matrix.W),
                M44 = Vector4.Dot(Q.W, matrix.W)
            };
        }

        /// <summary>
        /// Decomposes a rotation matrix with the specified yaw, pitch, roll value (angles in radians).
        /// </summary>
        /// <param name="yaw">The yaw component in radians.</param>
        /// <param name="pitch">The pitch component in radians.</param>
        /// <param name="roll">The roll component in radians.</param>
        /// <remarks>
        /// This rotation matrix can be represented by <b>intrinsic</b> rotations in the order <paramref name="yaw"/>, <paramref name="pitch"/>, then <paramref name="roll"/>.
        /// <br/>
        /// Therefore, the <b>extrinsic</b> rotations to achieve this matrix is the reversed order of operations,
        /// i.e. Matrix.RotationZ(roll) * Matrix.RotationX(pitch) * Matrix.RotationY(yaw)
        /// </remarks>
        public void Decompose(out float yaw, out float pitch, out float roll)
        {
            // Adapted from 'Euler Angle Formulas' by David Eberly - https://www.geometrictools.com/Documentation/EulerAngles.pdf
            // 2.3 Factor as Ry Rx Rz
            // License under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/)
            //
            // Note the Stride's matrix row/column ordering is swapped, indices starts at one,
            // and the if-statement ordering is written to minimize the number of operations to get to
            // the common case, and made to handle the +/- 1 cases better due to low precision in floats
            if (MathUtil.IsOne(Math.Abs(matrix.M32)))
            {
                if (matrix.M32 >= 0)
                {
                    // Edge case where M32 == +1
                    pitch = -MathUtil.PiOverTwo;
                    yaw = MathF.Atan2(-matrix.M21, matrix.M11);
                    roll = 0;
                }
                else
                {
                    // Edge case where M32 == -1
                    pitch = MathUtil.PiOverTwo;
                    yaw = -MathF.Atan2(-matrix.M21, matrix.M11);
                    roll = 0;
                }
            }
            else
            {
                // Common case
                pitch = MathF.Asin(-matrix.M32);
                yaw = MathF.Atan2(matrix.M31, matrix.M33);
                roll = MathF.Atan2(matrix.M12, matrix.M22);
            }
        }

        /// <summary>
        /// Decomposes a rotation matrix with the specified X, Y and Z euler angles in radians.
        /// Matrix.RotationX(rotation.X) * Matrix.RotationY(rotation.Y) * Matrix.RotationZ(rotation.Z) should represent the same rotation.
        /// </summary>
        /// <param name="rotation">The vector containing the 3 rotations angles to be applied in order.</param>
        public void DecomposeXYZ(out Vector3 rotation)
        {
            // Adapted from 'Euler Angle Formulas' by David Eberly - https://www.geometrictools.com/Documentation/EulerAngles.pdf
            // 2.6 Factor as Rz Ry Rx
            // License under CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/)
            //
            // Note the Stride's matrix row/column ordering is swapped, indices starts at one,
            // and the if-statement ordering is written to minimize the number of operations to get to
            // the common case, and made to handle the +/- 1 cases better due to low precision in floats.
            // The above documentation implies the *extrinsic* rotation order is X-Y-Z,
            // so the *intrinsic* rotation is Z-Y-X which is the formula to use here
            if (MathUtil.IsOne(Math.Abs(matrix.M13)))
            {
                if (matrix.M13 >= 0)
                {
                    // Edge case where M13 == +1
                    rotation.Y = -MathUtil.PiOverTwo;
                    rotation.Z = MathF.Atan2(-matrix.M32, matrix.M22);
                    rotation.X = 0;
                }
                else
                {
                    // Edge case where M13 == -1
                    rotation.Y = MathUtil.PiOverTwo;
                    rotation.Z = -MathF.Atan2(-matrix.M32, matrix.M22);
                    rotation.X = 0;
                }
            }
            else
            {
                // Common case
                rotation.Y = MathF.Asin(-matrix.M13);
                rotation.Z = MathF.Atan2(matrix.M12, matrix.M11);
                rotation.X = MathF.Atan2(matrix.M23, matrix.M33);
            }
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <returns><c>true</c> if a rotation exist for this matrix, <c>false</c> otherwise.</returns>
        /// <remarks>This method is designed to decompose an SRT transformation matrix only.</remarks>
        public bool Decompose(out Vector3 scale, out Vector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            //Get the translation.
            translation.X = matrix.M41;
            translation.Y = matrix.M42;
            translation.Z = matrix.M43;

            //Scaling is the length of the rows.
            scale.X = MathF.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12) + (matrix.M13 * matrix.M13));
            scale.Y = MathF.Sqrt((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22) + (matrix.M23 * matrix.M23));
            scale.Z = MathF.Sqrt((matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32) + (matrix.M33 * matrix.M33));

            //If any of the scaling factors are zero, than the rotation matrix can not exist.
            return MathF.Abs(scale.X) >= MathUtil.ZeroTolerance &&
                MathF.Abs(scale.Y) >= MathUtil.ZeroTolerance &&
                MathF.Abs(scale.Z) >= MathUtil.ZeroTolerance;
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="rotation">When the method completes, contains the rotation component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <remarks>
        /// This method is designed to decompose an SRT transformation matrix only.
        /// </remarks>
        public bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            matrix.Decompose(out scale, out Matrix4x4 rotationMatrix, out translation);
            rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
            return true;
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="rotation">When the method completes, contains the rotation component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <remarks>
        /// This method is designed to decompose an SRT transformation matrix only.
        /// </remarks>
        public bool Decompose(out Vector3 scale, out Matrix4x4 rotation, out Vector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            //Get the translation.
            translation.X = matrix.M41;
            translation.Y = matrix.M42;
            translation.Z = matrix.M43;

            //Scaling is the length of the rows.
            scale.X = MathF.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12) + (matrix.M13 * matrix.M13));
            scale.Y = MathF.Sqrt((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22) + (matrix.M23 * matrix.M23));
            scale.Z = MathF.Sqrt((matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32) + (matrix.M33 * matrix.M33));

            //If any of the scaling factors are zero, then the rotation matrix can not exist.
            if (MathF.Abs(scale.X) < MathUtil.ZeroTolerance ||
                MathF.Abs(scale.Y) < MathUtil.ZeroTolerance ||
                MathF.Abs(scale.Z) < MathUtil.ZeroTolerance)
            {
                rotation = Matrix4x4.Identity;
                return false;
            }

            // Calculate a perfect orthonormal matrix (no reflections)
            var at = new Vector3(matrix.M31 / scale.Z, matrix.M32 / scale.Z, matrix.M33 / scale.Z);
            var up = Vector3.Cross(at, new Vector3(matrix.M11 / scale.X, matrix.M12 / scale.X, matrix.M13 / scale.X));
            var right = Vector3.Cross(up, at);

            rotation = Matrix4x4.Identity;
            rotation.SetRight(right);
            rotation.SetUp(up);
            rotation.SetBackward(at);

            // In case of reflexions
            scale.X = Vector3.Dot(right, matrix.Right) > 0.0f ? scale.X : -scale.X;
            scale.Y = Vector3.Dot(up, matrix.Up) > 0.0f ? scale.Y : -scale.Y;
            scale.Z = Vector3.Dot(at, matrix.Backward) > 0.0f ? scale.Z : -scale.Z;

            return true;
        }

        /// <summary>
        /// Creates an array containing the elements of the matrix.
        /// </summary>
        /// <returns>A sixteen-element array containing the components of the matrix.</returns>
        public float[] ToArray()
        {
            return [matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44];
        }

        /// <summary>Determines whether every element of a matrix differs from the same element of this one by less than a tolerance.</summary>
        /// <param name="other">The matrix to compare with this instance.</param>
        /// <param name="tolerance">The difference of an element from which the two are not near.</param>
        public bool NearEquals(in Matrix4x4 other, float tolerance = MathUtil.ZeroTolerance)
        {
            return MathF.Abs(other.M11 - matrix.M11) < tolerance &&
                MathF.Abs(other.M12 - matrix.M12) < tolerance &&
                MathF.Abs(other.M13 - matrix.M13) < tolerance &&
                MathF.Abs(other.M14 - matrix.M14) < tolerance &&

                MathF.Abs(other.M21 - matrix.M21) < tolerance &&
                MathF.Abs(other.M22 - matrix.M22) < tolerance &&
                MathF.Abs(other.M23 - matrix.M23) < tolerance &&
                MathF.Abs(other.M24 - matrix.M24) < tolerance &&

                MathF.Abs(other.M31 - matrix.M31) < tolerance &&
                MathF.Abs(other.M32 - matrix.M32) < tolerance &&
                MathF.Abs(other.M33 - matrix.M33) < tolerance &&
                MathF.Abs(other.M34 - matrix.M34) < tolerance &&

                MathF.Abs(other.M41 - matrix.M41) < tolerance &&
                MathF.Abs(other.M42 - matrix.M42) < tolerance &&
                MathF.Abs(other.M43 - matrix.M43) < tolerance &&
                MathF.Abs(other.M44 - matrix.M44) < tolerance;
        }

    }

    extension(ref Matrix4x4 matrix)
    {
        /// <summary>
        /// Exchanges two rows in the matrix.
        /// </summary>
        /// <param name="firstRow">The first row to exchange. This is an index of the row starting at zero.</param>
        /// <param name="secondRow">The second row to exchange. This is an index of the row starting at zero.</param>
        public void ExchangeRows(int firstRow, int secondRow)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(firstRow, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(firstRow, 3);
            ArgumentOutOfRangeException.ThrowIfLessThan(secondRow, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(secondRow, 3);

            if (firstRow == secondRow)
                return;

            float temp0 = matrix[secondRow, 0];
            float temp1 = matrix[secondRow, 1];
            float temp2 = matrix[secondRow, 2];
            float temp3 = matrix[secondRow, 3];

            matrix[secondRow, 0] = matrix[firstRow, 0];
            matrix[secondRow, 1] = matrix[firstRow, 1];
            matrix[secondRow, 2] = matrix[firstRow, 2];
            matrix[secondRow, 3] = matrix[firstRow, 3];

            matrix[firstRow, 0] = temp0;
            matrix[firstRow, 1] = temp1;
            matrix[firstRow, 2] = temp2;
            matrix[firstRow, 3] = temp3;
        }

        /// <summary>
        /// Exchange columns.
        /// </summary>
        /// <param name="firstColumn">The first column to exchange.</param>
        /// <param name="secondColumn">The second column to exchange.</param>
        public void ExchangeColumns(int firstColumn, int secondColumn)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(firstColumn, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(firstColumn, 3);
            ArgumentOutOfRangeException.ThrowIfLessThan(secondColumn, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(secondColumn, 3);

            if (firstColumn == secondColumn)
                return;

            float temp0 = matrix[0, secondColumn];
            float temp1 = matrix[1, secondColumn];
            float temp2 = matrix[2, secondColumn];
            float temp3 = matrix[3, secondColumn];

            matrix[0, secondColumn] = matrix[0, firstColumn];
            matrix[1, secondColumn] = matrix[1, firstColumn];
            matrix[2, secondColumn] = matrix[2, firstColumn];
            matrix[3, secondColumn] = matrix[3, firstColumn];

            matrix[0, firstColumn] = temp0;
            matrix[1, firstColumn] = temp1;
            matrix[2, firstColumn] = temp2;
            matrix[3, firstColumn] = temp3;
        }

        /// <summary>
        /// Copies a nxm matrix to this instance.
        /// </summary>
        /// <param name="src">The source matrix.</param>
        /// <param name="columns">The number of columns.</param>
        /// <param name="rows">The number of rows.</param>
        public unsafe void CopyMatrixFrom(float* src, int columns, int rows)
        {
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; ++j)
                {
                    matrix[j, i] = src[(i * rows) + j];
                }
            }
        }

        /// <summary>
        /// Transposes a nmx matrix to this instance.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="columns">The columns.</param>
        /// <param name="rows">The rows.</param>
        public unsafe void TransposeMatrixFrom(float* src, int columns, int rows)
        {
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < columns; ++j)
                {
                    matrix[j, i] = src[i + (j * rows)];
                }
            }
        }

        /// <summary>Sets what <c>Column1</c> returns.</summary>
        public void SetColumn1(Vector4 value)
        {
            matrix.M11 = value.X;
            matrix.M21 = value.Y;
            matrix.M31 = value.Z;
            matrix.M41 = value.W;
        }

        /// <summary>Sets what <c>Column2</c> returns.</summary>
        public void SetColumn2(Vector4 value)
        {
            matrix.M12 = value.X;
            matrix.M22 = value.Y;
            matrix.M32 = value.Z;
            matrix.M42 = value.W;
        }

        /// <summary>Sets what <c>Column3</c> returns.</summary>
        public void SetColumn3(Vector4 value)
        {
            matrix.M13 = value.X;
            matrix.M23 = value.Y;
            matrix.M33 = value.Z;
            matrix.M43 = value.W;
        }

        /// <summary>Sets what <c>Column4</c> returns.</summary>
        public void SetColumn4(Vector4 value)
        {
            matrix.M14 = value.X;
            matrix.M24 = value.Y;
            matrix.M34 = value.Z;
            matrix.M44 = value.W;
        }

        /// <summary>Sets what <c>Up</c> returns.</summary>
        public void SetUp(Vector3 value)
        {
            matrix.M21 = value.X;
            matrix.M22 = value.Y;
            matrix.M23 = value.Z;
        }

        /// <summary>Sets what <c>Down</c> returns.</summary>
        public void SetDown(Vector3 value)
        {
            matrix.M21 = -value.X;
            matrix.M22 = -value.Y;
            matrix.M23 = -value.Z;
        }

        /// <summary>Sets what <c>Right</c> returns.</summary>
        public void SetRight(Vector3 value)
        {
            matrix.M11 = value.X;
            matrix.M12 = value.Y;
            matrix.M13 = value.Z;
        }

        /// <summary>Sets what <c>Left</c> returns.</summary>
        public void SetLeft(Vector3 value)
        {
            matrix.M11 = -value.X;
            matrix.M12 = -value.Y;
            matrix.M13 = -value.Z;
        }

        /// <summary>Sets what <c>Forward</c> returns.</summary>
        public void SetForward(Vector3 value)
        {
            matrix.M31 = -value.X;
            matrix.M32 = -value.Y;
            matrix.M33 = -value.Z;
        }

        /// <summary>Sets what <c>Backward</c> returns.</summary>
        public void SetBackward(Vector3 value)
        {
            matrix.M31 = value.X;
            matrix.M32 = value.Y;
            matrix.M33 = value.Z;
        }

    }
}
