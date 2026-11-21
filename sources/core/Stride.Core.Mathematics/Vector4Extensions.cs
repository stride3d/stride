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
using System.Numerics;

namespace Stride.Core.Mathematics;

/// <summary>
/// The members of Stride's math API that <see cref="Vector4"/> does not have.
/// </summary>
public static class Vector4Extensions
{
    extension(Vector4)
    {
        /// <summary>Scales a vector to a length of one; a vector too short to have a direction is returned as it is.</summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="tolerance">The length up to which a vector is too short.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 NormalizeSafe(Vector4 value, float tolerance = MathUtil.ZeroTolerance)
        {
            float length = value.Length();
            return length > tolerance ? value * (1.0f / length) : value;
        }

        /// <summary>Raises each component of a vector to a power.</summary>
        /// <param name="value">The vector.</param>
        /// <param name="exponent">The exponent.</param>
        public static Vector4 Pow(Vector4 value, float exponent)
        {
            return new Vector4(MathF.Pow(value.X, exponent), MathF.Pow(value.Y, exponent), MathF.Pow(value.Z, exponent), MathF.Pow(value.W, exponent));
        }

        /// <summary>
        /// Moves the first vector4 to the second one in a straight line.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <param name="maxTravelDistance">The rate at which the first point is going to move towards the second point.</param>
        public static Vector4 MoveTo(Vector4 from, Vector4 to, float maxTravelDistance)
        {
            Vector4 distance = to - from;

            float length = distance.Length();

            if (maxTravelDistance >= length || length == 0)
            {
                return to;
            }
            else
            {
                var v = 1f / length * maxTravelDistance;
                return new Vector4(from.X + (distance.X * v), from.Y + (distance.Y * v), from.Z + (distance.Z * v), from.W + (distance.W * v));
            }
        }

        /// <summary>
        /// Returns a <see cref="Vector4"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 4D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="Vector4"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="Vector4"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="Vector4"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <returns>A new <see cref="Vector4"/> containing the 4D Cartesian coordinates of the specified point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Barycentric(Vector4 value1, Vector4 value2, Vector4 value3, float amount1, float amount2)
        {
            return new Vector4(
                value1.X + (amount1 * (value2.X - value1.X)) + (amount2 * (value3.X - value1.X)),
                value1.Y + (amount1 * (value2.Y - value1.Y)) + (amount2 * (value3.Y - value1.Y)),
                value1.Z + (amount1 * (value2.Z - value1.Z)) + (amount2 * (value3.Z - value1.Z)),
                value1.W + (amount1 * (value2.W - value1.W)) + (amount2 * (value3.W - value1.W)));
        }

        /// <summary>
        /// Performs a cubic interpolation between two vectors.
        /// </summary>
        /// <param name="start">Start vector.</param>
        /// <param name="end">End vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 SmoothStep(Vector4 start, Vector4 end, float amount)
        {
            Vector4 result;
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = amount * amount * (3.0f - (2.0f * amount));

            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
            result.Z = start.Z + ((end.Z - start.Z) * amount);
            result.W = start.W + ((end.W - start.W) * amount);
            return result;
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">First source position vector.</param>
        /// <param name="tangent1">First source tangent vector.</param>
        /// <param name="value2">Second source position vector.</param>
        /// <param name="tangent2">Second source tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The result of the Hermite spline interpolation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float amount)
        {
            float squared = amount * amount;
            float cubed = amount * squared;
            float part1 = (2.0f * cubed) - (3.0f * squared) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = cubed - (2.0f * squared) + amount;
            float part4 = cubed - squared;

            return new Vector4(
                (value1.X * part1) + (value2.X * part2) + (tangent1.X * part3) + (tangent2.X * part4),
                (value1.Y * part1) + (value2.Y * part2) + (tangent1.Y * part3) + (tangent2.Y * part4),
                (value1.Z * part1) + (value2.Z * part2) + (tangent1.Z * part3) + (tangent2.Z * part4),
                (value1.W * part1) + (value2.W * part2) + (tangent1.W * part3) + (tangent2.W * part4));
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>A vector that is the result of the Catmull-Rom interpolation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float amount)
        {
            Vector4 result;
            float squared = amount * amount;
            float cubed = amount * squared;

            result.X = 0.5f * ((2.0f * value2.X) + ((-value1.X + value3.X) * amount) + (((((2.0f * value1.X) - (5.0f * value2.X)) + (4.0f * value3.X)) - value4.X) * squared) + ((((-value1.X + (3.0f * value2.X)) - (3.0f * value3.X)) + value4.X) * cubed));
            result.Y = 0.5f * ((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount) + (((((2.0f * value1.Y) - (5.0f * value2.Y)) + (4.0f * value3.Y)) - value4.Y) * squared) + ((((-value1.Y + (3.0f * value2.Y)) - (3.0f * value3.Y)) + value4.Y) * cubed));
            result.Z = 0.5f * ((2.0f * value2.Z) + ((-value1.Z + value3.Z) * amount) + (((((2.0f * value1.Z) - (5.0f * value2.Z)) + (4.0f * value3.Z)) - value4.Z) * squared) + ((((-value1.Z + (3.0f * value2.Z)) - (3.0f * value3.Z)) + value4.Z) * cubed));
            result.W = 0.5f * ((2.0f * value2.W) + ((-value1.W + value3.W) * amount) + (((((2.0f * value1.W) - (5.0f * value2.W)) + (4.0f * value3.W)) - value4.W) * squared) + ((((-value1.W + (3.0f * value2.W)) - (3.0f * value3.W)) + value4.W) * cubed));
            return result;
        }

        /// <summary>
        /// Orthogonalizes a list of vectors.
        /// </summary>
        /// <param name="destination">The list of orthogonalized vectors.</param>
        /// <param name="source">The list of vectors to orthogonalize.</param>
        /// <remarks>
        /// <para>Orthogonalization is the process of making all vectors orthogonal to each other. This
        /// means that any given vector in the list will be orthogonal to any other given vector in the
        /// list.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting vectors
        /// tend to be numerically unstable. The numeric stability decreases according to the vectors
        /// position in the list so that the first vector is the most stable and the last vector is the
        /// least stable.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        public static void Orthogonalize(Vector4[] destination, params Vector4[] source)
        {
            //Uses the modified Gram-Schmidt process.
            //q1 = m1
            //q2 = m2 - ((q1 ⋅ m2) / (q1 ⋅ q1)) * q1
            //q3 = m3 - ((q1 ⋅ m3) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m3) / (q2 ⋅ q2)) * q2
            //q4 = m4 - ((q1 ⋅ m4) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m4) / (q2 ⋅ q2)) * q2 - ((q3 ⋅ m4) / (q3 ⋅ q3)) * q3
            //q5 = ...

            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Vector4 newvector = source[i];

                for (int r = 0; r < i; ++r)
                {
                    newvector -= Vector4.Dot(destination[r], newvector) / Vector4.Dot(destination[r], destination[r]) * destination[r];
                }

                destination[i] = newvector;
            }
        }

        /// <summary>
        /// Orthonormalizes a list of vectors.
        /// </summary>
        /// <param name="destination">The list of orthonormalized vectors.</param>
        /// <param name="source">The list of vectors to orthonormalize.</param>
        /// <remarks>
        /// <para>Orthonormalization is the process of making all vectors orthogonal to each
        /// other and making all vectors of unit length. This means that any given vector will
        /// be orthogonal to any other given vector in the list.</para>
        /// <para>Because this method uses the modified Gram-Schmidt process, the resulting vectors
        /// tend to be numerically unstable. The numeric stability decreases according to the vectors
        /// position in the list so that the first vector is the most stable and the last vector is the
        /// least stable.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        public static void Orthonormalize(Vector4[] destination, params Vector4[] source)
        {
            //Uses the modified Gram-Schmidt process.
            //Because we are making unit vectors, we can optimize the math for orthogonalization
            //and simplify the projection operation to remove the division.
            //q1 = m1 / |m1|
            //q2 = (m2 - (q1 ⋅ m2) * q1) / |m2 - (q1 ⋅ m2) * q1|
            //q3 = (m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2) / |m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2|
            //q4 = (m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3) / |m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3|
            //q5 = ...

            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Vector4 newvector = source[i];

                for (int r = 0; r < i; ++r)
                {
                    newvector -= Vector4.Dot(destination[r], newvector) * destination[r];
                }

                newvector = Vector4.NormalizeSafe(newvector);
                destination[i] = newvector;
            }
        }

        /// <summary>
        /// Transforms an array of vectors by the given <see cref="Quaternion"/> rotation.
        /// </summary>
        /// <param name="source">The array of vectors to transform.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> rotation to apply.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.
        /// This array may be the same array as <paramref name="source"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        public static void Transform(Vector4[] source, Quaternion rotation, Vector4[] destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

            float x = rotation.X + rotation.X;
            float y = rotation.Y + rotation.Y;
            float z = rotation.Z + rotation.Z;
            float wx = rotation.W * x;
            float wy = rotation.W * y;
            float wz = rotation.W * z;
            float xx = rotation.X * x;
            float xy = rotation.X * y;
            float xz = rotation.X * z;
            float yy = rotation.Y * y;
            float yz = rotation.Y * z;
            float zz = rotation.Z * z;

            float num1 = 1.0f - yy - zz;
            float num2 = xy - wz;
            float num3 = xz + wy;
            float num4 = xy + wz;
            float num5 = 1.0f - xx - zz;
            float num6 = yz - wx;
            float num7 = xz - wy;
            float num8 = yz + wx;
            float num9 = 1.0f - xx - yy;

            for (int i = 0; i < source.Length; ++i)
            {
                destination[i] = new Vector4(
                    (source[i].X * num1) + (source[i].Y * num2) + (source[i].Z * num3),
                    (source[i].X * num4) + (source[i].Y * num5) + (source[i].Z * num6),
                    (source[i].X * num7) + (source[i].Y * num8) + (source[i].Z * num9),
                    source[i].W);
            }
        }

        /// <summary>
        /// Transforms an array of 4D vectors by the given <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="source">The array of vectors to transform.</param>
        /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.
        /// This array may be the same array as <paramref name="source"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        public static void Transform(Vector4[] source, in Matrix4x4 transform, Vector4[] destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                destination[i] = Vector4.Transform(source[i], transform);
            }
        }

        /// <summary>
        /// Divides a numerator by a vector.
        /// </summary>
        /// <param name="numerator">The numerator.</param>
        /// <param name="value">The value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(float numerator, Vector4 value)
        {
            return new Vector4(numerator / value.X, numerator / value.Y, numerator / value.Z, numerator / value.W);
        }

    }

    extension(Vector4 vector4)
    {
        /// <summary>
        /// Gets a value indicting whether this instance is normalized.
        /// </summary>
        public bool IsNormalized
        {
            get { return MathF.Abs((vector4.X * vector4.X) + (vector4.Y * vector4.Y) + (vector4.Z * vector4.Z) + (vector4.W * vector4.W) - 1f) < MathUtil.ZeroTolerance; }
        }

        /// <summary>
        /// Creates an array containing the elements of the vector.
        /// </summary>
        /// <returns>A four-element array containing the components of the vector.</returns>
        public float[] ToArray()
        {
            return [vector4.X, vector4.Y, vector4.Z, vector4.W];
        }

        /// <summary>Determines whether every component of a vector differs from the same component of this one by less than a tolerance.</summary>
        /// <param name="other">The vector to compare with this instance.</param>
        /// <param name="tolerance">The difference of a component from which the two are not near.</param>
        public bool NearEquals(Vector4 other, float tolerance = MathUtil.ZeroTolerance)
        {
            return MathF.Abs(other.X - vector4.X) < tolerance &&
                MathF.Abs(other.Y - vector4.Y) < tolerance &&
                MathF.Abs(other.Z - vector4.Z) < tolerance &&
                MathF.Abs(other.W - vector4.W) < tolerance;
        }

        /// <summary>
        /// Deconstructs the vector's components into named variables.
        /// </summary>
        /// <param name="x">The X component</param>
        /// <param name="y">The Y component</param>
        /// <param name="z">The Z component</param>
        /// <param name="w">The W component</param>
        public void Deconstruct(out float x, out float y, out float z, out float w)
        {
            x = vector4.X;
            y = vector4.Y;
            z = vector4.Z;
            w = vector4.W;
        }

    }
}
