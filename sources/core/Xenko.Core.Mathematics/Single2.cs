// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// Represents a two dimensional mathematical vector.
    /// </summary>
    [DataContract("float2")]
    [DataStyle(DataStyle.Compact)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Single2 : IEquatable<Single2>, IFormattable
    {
        /// <summary>
        /// The size of the <see cref="Xenko.Core.Mathematics.Single2"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Utilities.SizeOf<Single2>();

        /// <summary>
        /// A <see cref="Xenko.Core.Mathematics.Single2"/> with all of its components set to zero.
        /// </summary>
        public static readonly Single2 Zero = new Single2();

        /// <summary>
        /// The X unit <see cref="Xenko.Core.Mathematics.Single2"/> (1, 0).
        /// </summary>
        public static readonly Single2 UnitX = new Single2(1.0f, 0.0f);

        /// <summary>
        /// The Y unit <see cref="Xenko.Core.Mathematics.Single2"/> (0, 1).
        /// </summary>
        public static readonly Single2 UnitY = new Single2(0.0f, 1.0f);

        /// <summary>
        /// A <see cref="Xenko.Core.Mathematics.Single2"/> with all of its components set to one.
        /// </summary>
        public static readonly Single2 One = new Single2(1.0f, 1.0f);

        /// <summary>
        /// The X component of the vector.
        /// </summary>
        [DataMember(0)]
        public float X;

        /// <summary>
        /// The Y component of the vector.
        /// </summary>
        [DataMember(1)]
        public float Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Single2"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Single2(float value)
        {
            X = value;
            Y = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Single2"/> struct.
        /// </summary>
        /// <param name="x">Initial value for the X component of the vector.</param>
        /// <param name="y">Initial value for the Y component of the vector.</param>
        public Single2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Single2"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the X and Y components of the vector. This must be an array with two elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than two elements.</exception>
        public Single2(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 2)
                throw new ArgumentOutOfRangeException("values", "There must be two and only two input values for Single2.");

            X = values[0];
            Y = values[1];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Single2"/> struct.
        /// </summary>
        /// <param name="v">The Double2 to construct the Single2 from.</param>
        public Single2(Double2 v)
        {
            X = (float)v.X;
            Y = (float)v.Y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Xenko.Core.Mathematics.Single2"/> struct.
        /// </summary>
        /// <param name="v">The Vector2 to construct the Single2 from.</param>
        public Single2(Vector2 v)
        {
            X = (float)v.X;
            Y = (float)v.Y;
        }

        /// <summary>
        /// Gets a value indicting whether this instance is normalized.
        /// </summary>
        public bool IsNormalized
        {
            get { return Math.Abs((X * X) + (Y * Y) - 1f) < MathUtil.ZeroTolerance; }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the X or Y component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the X component and 1 for the Y component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 1].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Single2 run from 0 to 1, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for Single2 run from 0 to 1, inclusive.");
                }
            }
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        /// <returns>The length of the vector.</returns>
        /// <remarks>
        /// <see cref="Xenko.Core.Mathematics.Single2.LengthSquared"/> may be preferred when only the relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Length()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y));
        }

        /// <summary>
        /// Calculates the squared length of the vector.
        /// </summary>
        /// <returns>The squared length of the vector.</returns>
        /// <remarks>
        /// This method may be preferred to <see cref="Xenko.Core.Mathematics.Single2.Length"/> when only a relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float LengthSquared()
        {
            return (X * X) + (Y * Y);
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            float length = Length();
            if (length > MathUtil.ZeroTolerance)
            {
                float inv = 1.0f / length;
                X *= inv;
                Y *= inv;
            }
        }

        /// <summary>
        /// Creates an array containing the elements of the vector.
        /// </summary>
        /// <returns>A two-element array containing the components of the vector.</returns>
        public float[] ToArray()
        {
            return new float[] { X, Y };
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <param name="result">When the method completes, contains the sum of the two vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ref Single2 left, ref Single2 right, out Single2 result)
        {
            result = new Single2(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The sum of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Add(Single2 left, Single2 right)
        {
            return new Single2(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <param name="result">When the method completes, contains the difference of the two vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(ref Single2 left, ref Single2 right, out Single2 result)
        {
            result = new Single2(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <returns>The difference of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Subtract(Single2 left, Single2 right)
        {
            return new Single2(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <param name="result">When the method completes, contains the scaled vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Single2 value, float scale, out Single2 result)
        {
            result = new Single2(value.X * scale, value.Y * scale);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Multiply(Single2 value, float scale)
        {
            return new Single2(value.X * scale, value.Y * scale);
        }
        
        /// <summary>
        /// Modulates a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to modulate.</param>
        /// <param name="right">The second vector to modulate.</param>
        /// <param name="result">When the method completes, contains the modulated vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Modulate(ref Single2 left, ref Single2 right, out Single2 result)
        {
            result = new Single2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Modulates a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to modulate.</param>
        /// <param name="right">The second vector to modulate.</param>
        /// <returns>The modulated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Modulate(Single2 left, Single2 right)
        {
            return new Single2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <param name="result">When the method completes, contains the scaled vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(ref Single2 value, float scale, out Single2 result)
        {
            result = new Single2(value.X / scale, value.Y / scale);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Divide(Single2 value, float scale)
        {
            return new Single2(value.X / scale, value.Y / scale);
        }
        
        /// <summary>
        /// Demodulates a vector with another by performing component-wise division.
        /// </summary>
        /// <param name="left">The first vector to demodulate.</param>
        /// <param name="right">The second vector to demodulate.</param>
        /// <param name="result">When the method completes, contains the demodulated vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Demodulate(ref Single2 left, ref Single2 right, out Single2 result)
        {
            result = new Single2(left.X / right.X, left.Y / right.Y);
        }

        /// <summary>
        /// Demodulates a vector with another by performing component-wise division.
        /// </summary>
        /// <param name="left">The first vector to demodulate.</param>
        /// <param name="right">The second vector to demodulate.</param>
        /// <returns>The demodulated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Demodulate(Single2 left, Single2 right)
        {
            return new Single2(left.X / right.X, left.Y / right.Y);
        }

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <param name="result">When the method completes, contains a vector facing in the opposite direction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Negate(ref Single2 value, out Single2 result)
        {
            result = new Single2(-value.X, -value.Y);
        }

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>A vector facing in the opposite direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Negate(Single2 value)
        {
            return new Single2(-value.X, -value.Y);
        }

        /// <summary>
        /// Returns a <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <param name="result">When the method completes, contains the 2D Cartesian coordinates of the specified point.</param>
        public static void Barycentric(ref Single2 value1, ref Single2 value2, ref Single2 value3, float amount1, float amount2, out Single2 result)
        {
            result = new Single2((value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X)),
                (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y)));
        }

        /// <summary>
        /// Returns a <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <returns>A new <see cref="Xenko.Core.Mathematics.Single2"/> containing the 2D Cartesian coordinates of the specified point.</returns>
        public static Single2 Barycentric(Single2 value1, Single2 value2, Single2 value3, float amount1, float amount2)
        {
            Single2 result;
            Barycentric(ref value1, ref value2, ref value3, amount1, amount2, out result);
            return result;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="result">When the method completes, contains the clamped value.</param>
        public static void Clamp(ref Single2 value, ref Single2 min, ref Single2 max, out Single2 result)
        {
            float x = value.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;

            float y = value.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;

            result = new Single2(x, y);
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static Single2 Clamp(Single2 value, Single2 min, Single2 max)
        {
            Single2 result;
            Clamp(ref value, ref min, ref max, out result);
            return result;
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">When the method completes, contains the distance between the two vectors.</param>
        /// <remarks>
        /// <see cref="Xenko.Core.Mathematics.Single2.DistanceSquared(ref Single2, ref Single2, out float)"/> may be preferred when only the relative distance is needed
        /// and speed is of the essence.
        /// </remarks>
        public static void Distance(ref Single2 value1, ref Single2 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;

            result = (float)Math.Sqrt((x * x) + (y * y));
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The distance between the two vectors.</returns>
        /// <remarks>
        /// <see cref="Xenko.Core.Mathematics.Single2.DistanceSquared(Single2, Single2)"/> may be preferred when only the relative distance is needed
        /// and speed is of the essence.
        /// </remarks>
        public static float Distance(Single2 value1, Single2 value2)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;

            return (float)Math.Sqrt((x * x) + (y * y));
        }

        /// <summary>
        /// Calculates the squared distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector</param>
        /// <param name="result">When the method completes, contains the squared distance between the two vectors.</param>
        /// <remarks>Distance squared is the value before taking the square root. 
        /// Distance squared can often be used in place of distance if relative comparisons are being made. 
        /// For example, consider three points A, B, and C. To determine whether B or C is further from A, 
        /// compare the distance between A and B to the distance between A and C. Calculating the two distances 
        /// involves two square roots, which are computationally expensive. However, using distance squared 
        /// provides the same information and avoids calculating two square roots.
        /// </remarks>
        public static void DistanceSquared(ref Single2 value1, ref Single2 value2, out float result)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;

            result = (x * x) + (y * y);
        }

        /// <summary>
        /// Calculates the squared distance between two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The squared distance between the two vectors.</returns>
        /// <remarks>Distance squared is the value before taking the square root. 
        /// Distance squared can often be used in place of distance if relative comparisons are being made. 
        /// For example, consider three points A, B, and C. To determine whether B or C is further from A, 
        /// compare the distance between A and B to the distance between A and C. Calculating the two distances 
        /// involves two square roots, which are computationally expensive. However, using distance squared 
        /// provides the same information and avoids calculating two square roots.
        /// </remarks>
        public static float DistanceSquared(Single2 value1, Single2 value2)
        {
            float x = value1.X - value2.X;
            float y = value1.Y - value2.Y;

            return (x * x) + (y * y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="left">First source vector.</param>
        /// <param name="right">Second source vector.</param>
        /// <param name="result">When the method completes, contains the dot product of the two vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dot(ref Single2 left, ref Single2 right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="left">First source vector.</param>
        /// <param name="right">Second source vector.</param>
        /// <returns>The dot product of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Single2 left, Single2 right)
        {
            return (left.X * right.X) + (left.Y * right.Y);
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <param name="result">When the method completes, contains the normalized vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(ref Single2 value, out Single2 result)
        {
            result = value;
            result.Normalize();
        }

        /// <summary>
        /// Converts the vector into a unit vector.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Normalize(Single2 value)
        {
            value.Normalize();
            return value;
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="start">Start vector.</param>
        /// <param name="end">End vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two vectors.</param>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static void Lerp(ref Single2 start, ref Single2 end, float amount, out Single2 result)
        {
            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
        }

        /// <summary>
        /// Performs a linear interpolation between two vectors.
        /// </summary>
        /// <param name="start">Start vector.</param>
        /// <param name="end">End vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two vectors.</returns>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static Single2 Lerp(Single2 start, Single2 end, float amount)
        {
            Single2 result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Performs a cubic interpolation between two vectors.
        /// </summary>
        /// <param name="start">Start vector.</param>
        /// <param name="end">End vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the cubic interpolation of the two vectors.</param>
        public static void SmoothStep(ref Single2 start, ref Single2 end, float amount, out Single2 result)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.X = start.X + ((end.X - start.X) * amount);
            result.Y = start.Y + ((end.Y - start.Y) * amount);
        }

        /// <summary>
        /// Performs a cubic interpolation between two vectors.
        /// </summary>
        /// <param name="start">Start vector.</param>
        /// <param name="end">End vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two vectors.</returns>
        public static Single2 SmoothStep(Single2 start, Single2 end, float amount)
        {
            Single2 result;
            SmoothStep(ref start, ref end, amount, out result);
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
        /// <param name="result">When the method completes, contains the result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Single2 value1, ref Single2 tangent1, ref Single2 value2, ref Single2 tangent2, float amount, out Single2 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;
            float part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            float part2 = (-2.0f * cubed) + (3.0f * squared);
            float part3 = (cubed - (2.0f * squared)) + amount;
            float part4 = cubed - squared;

            result.X = (((value1.X * part1) + (value2.X * part2)) + (tangent1.X * part3)) + (tangent2.X * part4);
            result.Y = (((value1.Y * part1) + (value2.Y * part2)) + (tangent1.Y * part3)) + (tangent2.Y * part4);
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
        public static Single2 Hermite(Single2 value1, Single2 tangent1, Single2 value2, Single2 tangent2, float amount)
        {
            Single2 result;
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
            return result;
        }

        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">When the method completes, contains the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Single2 value1, ref Single2 value2, ref Single2 value3, ref Single2 value4, float amount, out Single2 result)
        {
            float squared = amount * amount;
            float cubed = amount * squared;

            result.X = 0.5f * ((((2.0f * value2.X) + ((-value1.X + value3.X) * amount)) +
            (((((2.0f * value1.X) - (5.0f * value2.X)) + (4.0f * value3.X)) - value4.X) * squared)) +
            ((((-value1.X + (3.0f * value2.X)) - (3.0f * value3.X)) + value4.X) * cubed));

            result.Y = 0.5f * ((((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount)) +
                (((((2.0f * value1.Y) - (5.0f * value2.Y)) + (4.0f * value3.Y)) - value4.Y) * squared)) +
                ((((-value1.Y + (3.0f * value2.Y)) - (3.0f * value3.Y)) + value4.Y) * cubed));
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
        public static Single2 CatmullRom(Single2 value1, Single2 value2, Single2 value3, Single2 value4, float amount)
        {
            Single2 result;
            CatmullRom(ref value1, ref value2, ref value3, ref value4, amount, out result);
            return result;
        }

        /// <summary>
        /// Returns a vector containing the smallest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <param name="result">When the method completes, contains an new vector composed of the largest components of the source vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Max(ref Single2 left, ref Single2 right, out Single2 result)
        {
            result.X = (left.X > right.X) ? left.X : right.X;
            result.Y = (left.Y > right.Y) ? left.Y : right.Y;
        }

        /// <summary>
        /// Returns a vector containing the largest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>A vector containing the largest components of the source vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Max(Single2 left, Single2 right)
        {
            Single2 result;
            Max(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Returns a vector containing the smallest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <param name="result">When the method completes, contains an new vector composed of the smallest components of the source vectors.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Min(ref Single2 left, ref Single2 right, out Single2 result)
        {
            result.X = (left.X < right.X) ? left.X : right.X;
            result.Y = (left.Y < right.Y) ? left.Y : right.Y;
        }

        /// <summary>
        /// Returns a vector containing the smallest components of the specified vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>A vector containing the smallest components of the source vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 Min(Single2 left, Single2 right)
        {
            Single2 result;
            Min(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal. 
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">Normal of the surface.</param>
        /// <param name="result">When the method completes, contains the reflected vector.</param>
        /// <remarks>Reflect only gives the direction of a reflection off a surface, it does not determine 
        /// whether the original vector was close enough to the surface to hit it.</remarks>
        public static void Reflect(ref Single2 vector, ref Single2 normal, out Single2 result)
        {
            float dot = (vector.X * normal.X) + (vector.Y * normal.Y);

            result.X = vector.X - ((2.0f * dot) * normal.X);
            result.Y = vector.Y - ((2.0f * dot) * normal.Y);
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal. 
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">Normal of the surface.</param>
        /// <returns>The reflected vector.</returns>
        /// <remarks>Reflect only gives the direction of a reflection off a surface, it does not determine 
        /// whether the original vector was close enough to the surface to hit it.</remarks>
        public static Single2 Reflect(Single2 vector, Single2 normal)
        {
            Single2 result;
            Reflect(ref vector, ref normal, out result);
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
        public static void Orthogonalize(Single2[] destination, params Single2[] source)
        {
            //Uses the modified Gram-Schmidt process.
            //q1 = m1
            //q2 = m2 - ((q1 ⋅ m2) / (q1 ⋅ q1)) * q1
            //q3 = m3 - ((q1 ⋅ m3) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m3) / (q2 ⋅ q2)) * q2
            //q4 = m4 - ((q1 ⋅ m4) / (q1 ⋅ q1)) * q1 - ((q2 ⋅ m4) / (q2 ⋅ q2)) * q2 - ((q3 ⋅ m4) / (q3 ⋅ q3)) * q3
            //q5 = ...

            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Single2 newvector = source[i];

                for (int r = 0; r < i; ++r)
                {
                    newvector -= (Single2.Dot(destination[r], newvector) / Single2.Dot(destination[r], destination[r])) * destination[r];
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
        public static void Orthonormalize(Single2[] destination, params Single2[] source)
        {
            //Uses the modified Gram-Schmidt process.
            //Because we are making unit vectors, we can optimize the math for orthogonalization
            //and simplify the projection operation to remove the division.
            //q1 = m1 / |m1|
            //q2 = (m2 - (q1 ⋅ m2) * q1) / |m2 - (q1 ⋅ m2) * q1|
            //q3 = (m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2) / |m3 - (q1 ⋅ m3) * q1 - (q2 ⋅ m3) * q2|
            //q4 = (m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3) / |m4 - (q1 ⋅ m4) * q1 - (q2 ⋅ m4) * q2 - (q3 ⋅ m4) * q3|
            //q5 = ...

            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Single2 newvector = source[i];

                for (int r = 0; r < i; ++r)
                {
                    newvector -= Single2.Dot(destination[r], newvector) * destination[r];
                }

                newvector.Normalize();
                destination[i] = newvector;
            }
        }

        /// <summary>
        /// Transforms a 2D vector by the given <see cref="Xenko.Core.Mathematics.Quaternion"/> rotation.
        /// </summary>
        /// <param name="vector">The vector to rotate.</param>
        /// <param name="rotation">The <see cref="Xenko.Core.Mathematics.Quaternion"/> rotation to apply.</param>
        /// <param name="result">When the method completes, contains the transformed <see cref="Xenko.Core.Mathematics.Single4"/>.</param>
        public static void Transform(ref Single2 vector, ref Quaternion rotation, out Single2 result)
        {
            float x = rotation.X + rotation.X;
            float y = rotation.Y + rotation.Y;
            float z = rotation.Z + rotation.Z;
            float wz = rotation.W * z;
            float xx = rotation.X * x;
            float xy = rotation.X * y;
            float yy = rotation.Y * y;
            float zz = rotation.Z * z;

            result = new Single2((vector.X * (1.0f - yy - zz)) + (vector.Y * (xy - wz)), (vector.X * (xy + wz)) + (vector.Y * (1.0f - xx - zz)));
        }

        /// <summary>
        /// Transforms a 2D vector by the given <see cref="Xenko.Core.Mathematics.Quaternion"/> rotation.
        /// </summary>
        /// <param name="vector">The vector to rotate.</param>
        /// <param name="rotation">The <see cref="Xenko.Core.Mathematics.Quaternion"/> rotation to apply.</param>
        /// <returns>The transformed <see cref="Xenko.Core.Mathematics.Single4"/>.</returns>
        public static Single2 Transform(Single2 vector, Quaternion rotation)
        {
            Single2 result;
            Transform(ref vector, ref rotation, out result);
            return result;
        }

        /// <summary>
        /// Transforms an array of vectors by the given <see cref="Xenko.Core.Mathematics.Quaternion"/> rotation.
        /// </summary>
        /// <param name="source">The array of vectors to transform.</param>
        /// <param name="rotation">The <see cref="Xenko.Core.Mathematics.Quaternion"/> rotation to apply.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.
        /// This array may be the same array as <paramref name="source"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        public static void Transform(Single2[] source, ref Quaternion rotation, Single2[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            float x = rotation.X + rotation.X;
            float y = rotation.Y + rotation.Y;
            float z = rotation.Z + rotation.Z;
            float wz = rotation.W * z;
            float xx = rotation.X * x;
            float xy = rotation.X * y;
            float yy = rotation.Y * y;
            float zz = rotation.Z * z;

            float num1 = (1.0f - yy - zz);
            float num2 = (xy - wz);
            float num3 = (xy + wz);
            float num4 = (1.0f - xx - zz);

            for (int i = 0; i < source.Length; ++i)
            {
                destination[i] = new Single2(
                    (source[i].X * num1) + (source[i].Y * num2),
                    (source[i].X * num3) + (source[i].Y * num4));
            }
        }

        /// <summary>
        /// Transforms a 2D vector by the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <param name="result">When the method completes, contains the transformed <see cref="Xenko.Core.Mathematics.Single4"/>.</param>
        public static void Transform(ref Single2 vector, ref Matrix transform, out Single4 result)
        {
            result = new Single4(
                (vector.X * transform.M11) + (vector.Y * transform.M21) + transform.M41,
                (vector.X * transform.M12) + (vector.Y * transform.M22) + transform.M42,
                (vector.X * transform.M13) + (vector.Y * transform.M23) + transform.M43,
                (vector.X * transform.M14) + (vector.Y * transform.M24) + transform.M44);
        }

        /// <summary>
        /// Transforms a 2D vector by the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <returns>The transformed <see cref="Xenko.Core.Mathematics.Single4"/>.</returns>
        public static Single4 Transform(Single2 vector, Matrix transform)
        {
            Single4 result;
            Transform(ref vector, ref transform, out result);
            return result;
        }

        /// <summary>
        /// Transforms an array of 2D vectors by the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="source">The array of vectors to transform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        public static void Transform(Single2[] source, ref Matrix transform, Single4[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                Transform(ref source[i], ref transform, out destination[i]);
            }
        }

        /// <summary>
        /// Performs a coordinate transformation using the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="coordinate">The coordinate vector to transform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <param name="result">When the method completes, contains the transformed coordinates.</param>
        /// <remarks>
        /// A coordinate transform performs the transformation with the assumption that the w component
        /// is one. The four dimensional vector obtained from the transformation operation has each
        /// component in the vector divided by the w component. This forces the wcomponent to be one and
        /// therefore makes the vector homogeneous. The homogeneous vector is often prefered when working
        /// with coordinates as the w component can safely be ignored.
        /// </remarks>
        public static void TransformCoordinate(ref Single2 coordinate, ref Matrix transform, out Single2 result)
        {
            Single4 vector = new Single4();
            vector.X = (coordinate.X * transform.M11) + (coordinate.Y * transform.M21) + transform.M41;
            vector.Y = (coordinate.X * transform.M12) + (coordinate.Y * transform.M22) + transform.M42;
            vector.Z = (coordinate.X * transform.M13) + (coordinate.Y * transform.M23) + transform.M43;
            vector.W = 1f / ((coordinate.X * transform.M14) + (coordinate.Y * transform.M24) + transform.M44);

            result = new Single2(vector.X * vector.W, vector.Y * vector.W);
        }

        /// <summary>
        /// Performs a coordinate transformation using the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="coordinate">The coordinate vector to transform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <returns>The transformed coordinates.</returns>
        /// <remarks>
        /// A coordinate transform performs the transformation with the assumption that the w component
        /// is one. The four dimensional vector obtained from the transformation operation has each
        /// component in the vector divided by the w component. This forces the wcomponent to be one and
        /// therefore makes the vector homogeneous. The homogeneous vector is often prefered when working
        /// with coordinates as the w component can safely be ignored.
        /// </remarks>
        public static Single2 TransformCoordinate(Single2 coordinate, Matrix transform)
        {
            Single2 result;
            TransformCoordinate(ref coordinate, ref transform, out result);
            return result;
        }

        /// <summary>
        /// Performs a coordinate transformation on an array of vectors using the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="source">The array of coordinate vectors to trasnform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.
        /// This array may be the same array as <paramref name="source"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        /// <remarks>
        /// A coordinate transform performs the transformation with the assumption that the w component
        /// is one. The four dimensional vector obtained from the transformation operation has each
        /// component in the vector divided by the w component. This forces the wcomponent to be one and
        /// therefore makes the vector homogeneous. The homogeneous vector is often prefered when working
        /// with coordinates as the w component can safely be ignored.
        /// </remarks>
        public static void TransformCoordinate(Single2[] source, ref Matrix transform, Single2[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                TransformCoordinate(ref source[i], ref transform, out destination[i]);
            }
        }

        /// <summary>
        /// Performs a normal transformation using the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="normal">The normal vector to transform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <param name="result">When the method completes, contains the transformed normal.</param>
        /// <remarks>
        /// A normal transform performs the transformation with the assumption that the w component
        /// is zero. This causes the fourth row and fourth collumn of the matrix to be unused. The
        /// end result is a vector that is not translated, but all other transformation properties
        /// apply. This is often prefered for normal vectors as normals purely represent direction
        /// rather than location because normal vectors should not be translated.
        /// </remarks>
        public static void TransformNormal(ref Single2 normal, ref Matrix transform, out Single2 result)
        {
            result = new Single2(
                (normal.X * transform.M11) + (normal.Y * transform.M21),
                (normal.X * transform.M12) + (normal.Y * transform.M22));
        }

        /// <summary>
        /// Performs a normal transformation using the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="normal">The normal vector to transform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <returns>The transformed normal.</returns>
        /// <remarks>
        /// A normal transform performs the transformation with the assumption that the w component
        /// is zero. This causes the fourth row and fourth collumn of the matrix to be unused. The
        /// end result is a vector that is not translated, but all other transformation properties
        /// apply. This is often prefered for normal vectors as normals purely represent direction
        /// rather than location because normal vectors should not be translated.
        /// </remarks>
        public static Single2 TransformNormal(Single2 normal, Matrix transform)
        {
            Single2 result;
            TransformNormal(ref normal, ref transform, out result);
            return result;
        }

        /// <summary>
        /// Performs a normal transformation on an array of vectors using the given <see cref="Xenko.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="source">The array of normal vectors to transform.</param>
        /// <param name="transform">The transformation <see cref="Xenko.Core.Mathematics.Matrix"/>.</param>
        /// <param name="destination">The array for which the transformed vectors are stored.
        /// This array may be the same array as <paramref name="source"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
        /// <remarks>
        /// A normal transform performs the transformation with the assumption that the w component
        /// is zero. This causes the fourth row and fourth collumn of the matrix to be unused. The
        /// end result is a vector that is not translated, but all other transformation properties
        /// apply. This is often prefered for normal vectors as normals purely represent direction
        /// rather than location because normal vectors should not be translated.
        /// </remarks>
        public static void TransformNormal(Single2[] source, ref Matrix transform, Single2[] destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destination.Length < source.Length)
                throw new ArgumentOutOfRangeException("destination", "The destination array must be of same length or larger length than the source array.");

            for (int i = 0; i < source.Length; ++i)
            {
                TransformNormal(ref source[i], ref transform, out destination[i]);
            }
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The sum of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator +(Single2 left, Single2 right)
        {
            return new Single2(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Assert a vector (return it unchanged).
        /// </summary>
        /// <param name="value">The vector to assert (unchange).</param>
        /// <returns>The asserted (unchanged) vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator +(Single2 value)
        {
            return value;
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="left">The first vector to subtract.</param>
        /// <param name="right">The second vector to subtract.</param>
        /// <returns>The difference of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator -(Single2 left, Single2 right)
        {
            return new Single2(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Reverses the direction of a given vector.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>A vector facing in the opposite direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator -(Single2 value)
        {
            return new Single2(-value.X, -value.Y);
        }

        /// <summary>
        /// Modulates a vector with another by performing component-wise multiplication.
        /// </summary>
        /// <param name="left">The first vector to multiply.</param>
        /// <param name="right">The second vector to multiply.</param>
        /// <returns>The multiplication of the two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator *(Single2 left, Single2 right)
        {
            return new Single2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator *(float scale, Single2 value)
        {
            return new Single2(value.X * scale, value.Y * scale);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator *(Single2 value, float scale)
        {
            return new Single2(value.X * scale, value.Y * scale);
        }

        /// <summary>
        /// Scales a vector by the given value.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="scale">The amount by which to scale the vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator /(Single2 value, float scale)
        {
            return new Single2(value.X / scale, value.Y / scale);
        }

        /// <summary>
        /// Divides a numerator by a vector.
        /// </summary>
        /// <param name="numerator">The numerator.</param>
        /// <param name="value">The value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator /(float numerator, Single2 value)
        {
            return new Single2(numerator / value.X, numerator / value.Y);
        }

        /// <summary>
        /// Divides a vector by the given vector, component-wise.
        /// </summary>
        /// <param name="value">The vector to scale.</param>
        /// <param name="by">The by.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Single2 operator /(Single2 value, Single2 by)
        {
            return new Single2(value.X / by.X, value.Y / by.Y);
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Single2 left, Single2 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Single2 left, Single2 right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Single2"/> to <see cref="Xenko.Core.Mathematics.Vector2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Vector2(Single2 value)
        {
            return new Vector2(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Single2"/> to <see cref="Xenko.Core.Mathematics.Double2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Double2(Single2 value)
        {
            return new Double2(value);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Xenko.Core.Mathematics.Single2"/> to <see cref="Xenko.Core.Mathematics.Single3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Single3(Single2 value)
        {
            return new Single3(value, 0.0f);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Xenko.Core.Mathematics.Single2"/> to <see cref="Xenko.Core.Mathematics.Single4"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Single4(Single2 value)
        {
            return new Single4(value, 0.0f, 0.0f);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1}", X, Y);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1}", X.ToString(format, CultureInfo.CurrentCulture), Y.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "X:{0} Y:{1}", X, Y);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                ToString(formatProvider);

            return string.Format(formatProvider, "X:{0} Y:{1}", X.ToString(format, formatProvider), Y.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Xenko.Core.Mathematics.Single2"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Xenko.Core.Mathematics.Single2"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="Xenko.Core.Mathematics.Single2"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Single2 other)
        {
            return ((float)Math.Abs(other.X - X) < MathUtil.ZeroTolerance &&
                (float)Math.Abs(other.Y - Y) < MathUtil.ZeroTolerance);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (value == null)
                return false;

            if (value.GetType() != GetType())
                return false;

            return Equals((Single2)value);
        }

#if WPFInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Single2"/> to <see cref="System.Windows.Point"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator System.Windows.Point(Single2 value)
        {
            return new System.Windows.Point(value.X, value.Y);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Windows.Point"/> to <see cref="Xenko.Core.Mathematics.Single2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Single2(System.Windows.Point value)
        {
            return new Single2((float)value.X, (float)value.Y);
        }
#endif

#if XnaInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Single2"/> to <see cref="Microsoft.Xna.Framework.Vector2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Microsoft.Xna.Framework.Vector2(Single2 value)
        {
            return new Microsoft.Xna.Framework.Vector2(value.X, value.Y);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Vector2"/> to <see cref="Xenko.Core.Mathematics.Single2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Single2(Microsoft.Xna.Framework.Vector2 value)
        {
            return new Single2(value.X, value.Y);
        }
#endif
    }
}
