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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.MathF;
using System.Numerics;

namespace Stride.Core.Mathematics;

/// <summary>
/// The members of Stride's math API that <see cref="Quaternion"/> does not have.
/// </summary>
public static class QuaternionExtensions
{
    extension(Quaternion)
    {
        /// <summary>Scales a quaternion to a length of one; a quaternion too short to be a rotation is returned as it is.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion NormalizeSafe(Quaternion value)
        {
            float length = value.Length();
            return length > MathUtil.ZeroTolerance ? value * (1.0f / length) : value;
        }

        /// <summary>
        /// Returns a <see cref="System.Numerics.Quaternion"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="System.Numerics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="System.Numerics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="System.Numerics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <returns>A new <see cref="System.Numerics.Quaternion"/> containing the 4D Cartesian coordinates of the specified point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Barycentric(Quaternion value1, Quaternion value2, Quaternion value3, float amount1, float amount2)
        {
            var start = Quaternion.Slerp(value1, value2, amount1 + amount2);
            var end = Quaternion.Slerp(value1, value3, amount1 + amount2);
            return Quaternion.Slerp(start, end, amount2 / (amount1 + amount2));
        }

        /// <summary>
        /// Returns the absolute angle in radians between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        public static float AngleBetween(Quaternion a, Quaternion b)
        {
            return Acos(Min(Abs(Quaternion.Dot(a, b)), 1f)) * 2f;
        }

        /// <summary>
        /// Exponentiates a quaternion.
        /// </summary>
        /// <param name="value">The quaternion to exponentiate.</param>
        /// <returns>The exponentiated quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Exponential(Quaternion value)
        {
            Quaternion result;
            float angle = MathF.Sqrt((value.X * value.X) + (value.Y * value.Y) + (value.Z * value.Z));
            float sin = MathF.Sin(angle);

            if (MathF.Abs(sin) >= MathUtil.ZeroTolerance)
            {
                float coeff = sin / angle;
                result.X = coeff * value.X;
                result.Y = coeff * value.Y;
                result.Z = coeff * value.Z;
            }
            else
            {
                result = value;
            }

            result.W = MathF.Cos(angle);
            return result;
        }

        /// <summary>
        /// Returns a rotation whose facing direction points towards <paramref name="forward"/>
        /// and whose up direction points as close as possible to <paramref name="up"/>.
        /// </summary>
        public static Quaternion CreateLookRotation(Vector3 forward, Vector3 up)
        {
            var right = Vector3.NormalizeSafe(Vector3.Cross(up, forward));
            var orthoUp = Vector3.Cross(forward, right);
            var m = new Matrix4x4
            {
                M11 = right.X, M12 = right.Y, M13 = right.Z,
                M21 = orthoUp.X, M22 = orthoUp.Y, M23 = orthoUp.Z,
                M31 = forward.X, M32 = forward.Y, M33 = forward.Z,
            };
            var lQuaternion = Quaternion.CreateFromRotationMatrix(m);
            return lQuaternion;
        }

        /// <summary>
        /// Calculates the natural logarithm of the specified quaternion.
        /// </summary>
        /// <param name="value">The quaternion whose logarithm will be calculated.</param>
        /// <returns>The natural logarithm of the quaternion.</returns>
        public static Quaternion Logarithm(Quaternion value)
        {
            Quaternion result;
            if (MathF.Abs(value.W) < 1.0f)
            {
                float angle = MathF.Acos(value.W);
                float sin = MathF.Sin(angle);

                if (MathF.Abs(sin) >= MathUtil.ZeroTolerance)
                {
                    float coeff = angle / sin;
                    result.X = value.X * coeff;
                    result.Y = value.Y * coeff;
                    result.Z = value.Z * coeff;
                }
                else
                {
                    result = value;
                }
            }
            else
            {
                result = value;
            }

            result.W = 0.0f;
            return result;
        }

        /// <summary>
        /// Creates a quaternion that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <returns>The created rotation quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion CreateRotationX(float angle)
        {
            float halfAngle = angle * 0.5f;
            return new Quaternion(MathF.Sin(halfAngle), 0.0f, 0.0f, MathF.Cos(halfAngle));
        }

        /// <summary>
        /// Creates a quaternion that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <returns>The created rotation quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion CreateRotationY(float angle)
        {
            float halfAngle = angle * 0.5f;
            return new Quaternion(0.0f, MathF.Sin(halfAngle), 0.0f, MathF.Cos(halfAngle));
        }

        /// <summary>
        /// Creates a quaternion that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <returns>The created rotation quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion CreateRotationZ(float angle)
        {
            float halfAngle = angle * 0.5f;
            return new Quaternion(0.0f, 0.0f, MathF.Sin(halfAngle), MathF.Cos(halfAngle));
        }

        /// <summary>
        /// Computes a quaternion corresponding to the rotation transforming the vector <paramref name="source"/> to the vector <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source vector of the transformation.</param>
        /// <param name="target">The target vector of the transformation.</param>
        /// <returns>The resulting quaternion corresponding to the transformation of the source vector to the target vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion CreateFromDirections(Vector3 source, Vector3 target)
        {
            Quaternion result;
            var norms = MathF.Sqrt(source.LengthSquared() * target.LengthSquared());
            var real = norms + Vector3.Dot(source, target);
            if (real < MathUtil.ZeroTolerance * norms)
            {
                // If source and target are exactly opposite, rotate 180 degrees around an arbitrary orthogonal axis.
                // Axis normalisation can happen later, when we normalise the quaternion.
                result = MathF.Abs(source.X) > MathF.Abs(source.Z)
                    ? new Quaternion(-source.Y, source.X, 0.0f, 0.0f)
                    : new Quaternion(0.0f, -source.Z, source.Y, 0.0f);
            }
            else
            {
                // Otherwise, build quaternion the standard way.
                var axis = Vector3.Cross(source, target);
                result = new Quaternion(axis, real);
            }
            result = Quaternion.NormalizeSafe(result);
            return result;
        }

        /// <summary>
        /// Rotate <paramref name="current"/> towards <paramref name="target"/> by <paramref name="angle"/>.
        /// </summary>
        /// <remarks>
        /// When the angle difference between <paramref name="current"/> and <paramref name="target"/> is less than
        /// the given <paramref name="angle"/>, returns <paramref name="target"/> instead of overshooting past it.
        /// </remarks>
        public static Quaternion RotateTowards(Quaternion current, Quaternion target, float angle)
        {
            var maxAngle = Quaternion.AngleBetween(current, target);
            return maxAngle == 0f ? target : Quaternion.Slerp(current, target, Min(1f, angle / maxAngle));
        }

        /// <summary>
        /// Interpolates between quaternions, using spherical quadrangle interpolation.
        /// </summary>
        /// <param name="value1">First source quaternion.</param>
        /// <param name="value2">Second source quaternion.</param>
        /// <param name="value3">Thrid source quaternion.</param>
        /// <param name="value4">Fourth source quaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of interpolation.</param>
        /// <returns>The spherical quadrangle interpolation of the quaternions.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Squad(Quaternion value1, Quaternion value2, Quaternion value3, Quaternion value4, float amount)
        {
            var start = Quaternion.Slerp(value1, value4, amount);
            var end = Quaternion.Slerp(value2, value3, amount);
            return Quaternion.Slerp(start, end, 2.0f * amount * (1.0f - amount));
        }

        /// <summary>
        /// Sets up control points for spherical quadrangle interpolation.
        /// </summary>
        /// <param name="value1">First source quaternion.</param>
        /// <param name="value2">Second source quaternion.</param>
        /// <param name="value3">Third source quaternion.</param>
        /// <param name="value4">Fourth source quaternion.</param>
        /// <returns>An array of three quaternions that represent control points for spherical quadrangle interpolation.</returns>
        public static Quaternion[] SquadSetup(Quaternion value1, Quaternion value2, Quaternion value3, Quaternion value4)
        {
            Quaternion q0 = (value1 + value2).LengthSquared() < (value1 - value2).LengthSquared() ? -value1 : value1;
            Quaternion q2 = (value2 + value3).LengthSquared() < (value2 - value3).LengthSquared() ? -value3 : value3;
            Quaternion q3 = (value3 + value4).LengthSquared() < (value3 - value4).LengthSquared() ? -value4 : value4;
            Quaternion q1 = value2;

            var q1Exp = Quaternion.Exponential(q1);
            var q2Exp = Quaternion.Exponential(q2);

            Quaternion[] results = new Quaternion[3];
            results[0] = Quaternion.Concatenate(q1, Quaternion.Exponential(-0.25f * (Quaternion.Logarithm(Quaternion.Concatenate(q1Exp, q2)) + Quaternion.Logarithm(Quaternion.Concatenate(q1Exp, q0)))));
            results[1] = Quaternion.Concatenate(q2, Quaternion.Exponential(-0.25f * (Quaternion.Logarithm(Quaternion.Concatenate(q2Exp, q3)) + Quaternion.Logarithm(Quaternion.Concatenate(q2Exp, q1)))));
            results[2] = q2;

            return results;
        }

        /// <summary>
        /// Scales a quaternion by the given value.
        /// </summary>
        /// <param name="value">The quaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the quaternion.</param>
        /// <returns>The scaled quaternion.</returns>
        public static Quaternion operator *(float scale, Quaternion value)
        {
            var result = Quaternion.Multiply(value, scale);
            return result;
        }

        /// <summary>
        /// Return the vector rotated by the quaternion.
        /// </summary>
        /// <remarks>
        /// Shorthand for <see cref="Rotate"/>
        /// </remarks>
        public static Vector3 operator *(Quaternion left, Vector3 right)
        {
            var pureQuaternion = new Quaternion(right, 0);
            pureQuaternion = Quaternion.Concatenate(Quaternion.Concatenate(Quaternion.Conjugate(left), pureQuaternion), left);
            return new Vector3(pureQuaternion.X, pureQuaternion.Y, pureQuaternion.Z);
        }

    }

    extension(Quaternion quaternion)
    {
        /// <summary>Returns the text System.Numerics gives for a quaternion, with each number formatted as asked.</summary>
        public string ToString(string format, IFormatProvider formatProvider)
            => $"{{X:{quaternion.X.ToString(format, formatProvider)} Y:{quaternion.Y.ToString(format, formatProvider)} Z:{quaternion.Z.ToString(format, formatProvider)} W:{quaternion.W.ToString(format, formatProvider)}}}";

        /// <summary>
        /// Computes the angle of the rotation, in radians.
        /// </summary>
        public float GetAngle()
        {
            float length = (quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z);
            if (length < MathUtil.ZeroTolerance)
                return 0.0f;

            return 2.0f * MathF.Acos(quaternion.W);
        }

        /// <summary>
        /// Computes the axis of the rotation; a quaternion that has no rotation gives the X axis.
        /// </summary>
        public Vector3 GetAxis()
        {
            float length = (quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z);
            if (length < MathUtil.ZeroTolerance)
                return Vector3.UnitX;

            float inv = 1.0f / length;
            return new Vector3(quaternion.X * inv, quaternion.Y * inv, quaternion.Z * inv);
        }

        /// <summary>
        /// Computes the yaw, pitch and roll of the rotation, in radians, as the X, Y and Z of a vector.
        /// </summary>
        public Vector3 GetYawPitchRoll()
        {
            // Equivalent to:
            //  Matrix4x4.CreateFromQuaternion(quaternion).Decompose(out yaw, out pitch, out roll);

            var xx = quaternion.X * quaternion.X;
            var yy = quaternion.Y * quaternion.Y;
            var zz = quaternion.Z * quaternion.Z;
            var xy = quaternion.X * quaternion.Y;
            var zw = quaternion.Z * quaternion.W;
            var zx = quaternion.Z * quaternion.X;
            var yw = quaternion.Y * quaternion.W;
            var yz = quaternion.Y * quaternion.Z;
            var xw = quaternion.X * quaternion.W;

            var M11 = 1.0f - (2.0f * (yy + zz));
            var M12 = 2.0f * (xy + zw);
            var M21 = 2.0f * (xy - zw);
            var M22 = 1.0f - (2.0f * (zz + xx));
            var M31 = 2.0f * (zx + yw);
            var M32 = 2.0f * (yz - xw);
            var M33 = 1.0f - (2.0f * (yy + xx));

            /*** Refer to Matrix4x4.Decompose(out float yaw, out float pitch, out float roll) for code and license ***/
            if (MathUtil.IsOne(Math.Abs(M32)))
            {
                // Edge cases where M32 == +1 and M32 == -1
                return M32 >= 0
                    ? new Vector3(MathF.Atan2(-M21, M11), -MathUtil.PiOverTwo, 0)
                    : new Vector3(-MathF.Atan2(-M21, M11), MathUtil.PiOverTwo, 0);
            }

            // Common case
            return new Vector3(MathF.Atan2(M31, M33), MathF.Asin(-M32), MathF.Atan2(M12, M22));
        }

        /// <summary>
        /// Gets a value indicting whether this instance is normalized.
        /// </summary>
        public bool IsNormalized
        {
            get { return MathF.Abs((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z) + (quaternion.W * quaternion.W) - 1f) < MathUtil.ZeroTolerance; }
        }

        /// <summary>
        /// Creates an array containing the elements of the quaternion.
        /// </summary>
        /// <returns>A four-element array containing the components of the quaternion.</returns>
        public float[] ToArray()
        {
            return [quaternion.X, quaternion.Y, quaternion.Z, quaternion.W];
        }

        /// <summary>Determines whether every component of a quaternion differs from the same component of this one by less than a tolerance.</summary>
        /// <param name="other">The quaternion to compare with this instance.</param>
        /// <param name="tolerance">The difference of a component from which the two are not near.</param>
        public bool NearEquals(Quaternion other, float tolerance = MathUtil.ZeroTolerance)
        {
            return (Abs(other.X - quaternion.X) < tolerance &&
                Abs(other.Y - quaternion.Y) < tolerance &&
                Abs(other.Z - quaternion.Z) < tolerance &&
                Abs(other.W - quaternion.W) < tolerance);
        }

    }
}
