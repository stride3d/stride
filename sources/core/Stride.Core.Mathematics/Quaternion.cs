// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

namespace Stride.Core.Mathematics
{
    /// <summary>
    /// Represents a four dimensional mathematical quaternion.
    /// </summary>
    [DataContract("quaternion")]
    [DataStyle(DataStyle.Compact)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Quaternion : IEquatable<Quaternion>, IFormattable
    {
        /// <summary>
        /// The size of the <see cref="Stride.Core.Mathematics.Quaternion"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Utilities.SizeOf<Quaternion>();

        /// <summary>
        /// A <see cref="Stride.Core.Mathematics.Quaternion"/> with all of its components set to zero.
        /// </summary>
        public static readonly Quaternion Zero = new Quaternion();

        /// <summary>
        /// A <see cref="Stride.Core.Mathematics.Quaternion"/> with all of its components set to one.
        /// </summary>
        public static readonly Quaternion One = new Quaternion(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        /// The identity <see cref="Stride.Core.Mathematics.Quaternion"/> (0, 0, 0, 1).
        /// </summary>
        public static readonly Quaternion Identity = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// The X component of the quaternion.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y component of the quaternion.
        /// </summary>
        public float Y;

        /// <summary>
        /// The Z component of the quaternion.
        /// </summary>
        public float Z;

        /// <summary>
        /// The W component of the quaternion.
        /// </summary>
        public float W;

        /// <summary>
        /// Initializes a new instance of the <see cref="Stride.Core.Mathematics.Quaternion"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Quaternion(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stride.Core.Mathematics.Quaternion"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the components.</param>
        public Quaternion(Vector4 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = value.W;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stride.Core.Mathematics.Quaternion"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the X, Y, and Z components.</param>
        /// <param name="w">Initial value for the W component of the quaternion.</param>
        public Quaternion(Vector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stride.Core.Mathematics.Quaternion"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the X and Y components.</param>
        /// <param name="z">Initial value for the Z component of the quaternion.</param>
        /// <param name="w">Initial value for the W component of the quaternion.</param>
        public Quaternion(Vector2 value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stride.Core.Mathematics.Quaternion"/> struct.
        /// </summary>
        /// <param name="x">Initial value for the X component of the quaternion.</param>
        /// <param name="y">Initial value for the Y component of the quaternion.</param>
        /// <param name="z">Initial value for the Z component of the quaternion.</param>
        /// <param name="w">Initial value for the W component of the quaternion.</param>
        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stride.Core.Mathematics.Quaternion"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the X, Y, Z, and W components of the quaternion. This must be an array with four elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public Quaternion(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 4)
                throw new ArgumentOutOfRangeException("values", "There must be four and only four input values for Quaternion.");

            X = values[0];
            Y = values[1];
            Z = values[2];
            W = values[3];
        }

        /// <summary>
        /// Gets a value indicating whether this instance is equivalent to the identity quaternion.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is an identity quaternion; otherwise, <c>false</c>.
        /// </value>
        public bool IsIdentity
        {
            get { return this.Equals(Identity); }
        }

        /// <summary>
        /// Gets a value indicting whether this instance is normalized.
        /// </summary>
        public bool IsNormalized
        {
            get { return Math.Abs((X * X) + (Y * Y) + (Z * Z) + (W * W) - 1f) < MathUtil.ZeroTolerance; }
        }

        /// <summary>
        /// Gets the angle of the quaternion.
        /// </summary>
        /// <value>The quaternion's angle.</value>
        public float Angle
        {
            get
            {
                float length = (X * X) + (Y * Y) + (Z * Z);
                if (length < MathUtil.ZeroTolerance)
                    return 0.0f;

                return (float)(2.0 * Math.Acos(W));
            }
        }

        /// <summary>
        /// Gets the axis components of the quaternion.
        /// </summary>
        /// <value>The axis components of the quaternion.</value>
        public Vector3 Axis
        {
            get
            {
                float length = (X * X) + (Y * Y) + (Z * Z);
                if (length < MathUtil.ZeroTolerance)
                    return Vector3.UnitX;

                float inv = 1.0f / length;
                return new Vector3(X * inv, Y * inv, Z * inv);
            }
        }

        /// <summary>
        /// Gets yaw/pitch/roll equivalent of the quaternion
        /// </summary>
        public Vector3 YawPitchRoll
        {
            get
            {
                Vector3 yawPitchRoll;
                RotationYawPitchRoll(ref this, out yawPitchRoll.X, out yawPitchRoll.Y, out yawPitchRoll.Z);
                return yawPitchRoll;
            }
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the X, Y, Z, or W component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the X component, 1 for the Y component, 2 for the Z component, and 3 for the W component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 3].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    case 3: return W;
                }

                throw new ArgumentOutOfRangeException("index", "Indices for Quaternion run from 0 to 3, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    case 3: W = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Indices for Quaternion run from 0 to 3, inclusive.");
                }
            }
        }

        /// <summary>
        /// Conjugates the quaternion.
        /// </summary>
        public void Conjugate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        /// <summary>
        /// Conjugates and renormalizes the quaternion.
        /// </summary>
        public void Invert()
        {
            float lengthSq = LengthSquared();
            if (lengthSq > MathUtil.ZeroTolerance)
            {
                lengthSq = 1.0f / lengthSq;

                X = -X * lengthSq;
                Y = -Y * lengthSq;
                Z = -Z * lengthSq;
                W = W * lengthSq;
            }
        }

        /// <summary>
        /// Calculates the length of the quaternion.
        /// </summary>
        /// <returns>The length of the quaternion.</returns>
        /// <remarks>
        /// <see cref="Stride.Core.Mathematics.Quaternion.LengthSquared"/> may be preferred when only the relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        public float Length()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
        }

        /// <summary>
        /// Calculates the squared length of the quaternion.
        /// </summary>
        /// <returns>The squared length of the quaternion.</returns>
        /// <remarks>
        /// This method may be preferred to <see cref="Stride.Core.Mathematics.Quaternion.Length"/> when only a relative length is needed
        /// and speed is of the essence.
        /// </remarks>
        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        /// <summary>
        /// Converts the quaternion into a unit quaternion.
        /// </summary>
        public void Normalize()
        {
            float length = Length();
            if (length > MathUtil.ZeroTolerance)
            {
                float inverse = 1.0f / length;
                X *= inverse;
                Y *= inverse;
                Z *= inverse;
                W *= inverse;
            }
        }

        /// <summary>
        /// Creates an array containing the elements of the quaternion.
        /// </summary>
        /// <returns>A four-element array containing the components of the quaternion.</returns>
        public float[] ToArray()
        {
            return new float[] { X, Y, Z, W };
        }

        /// <summary>
        /// Adds two quaternions.
        /// </summary>
        /// <param name="left">The first quaternion to add.</param>
        /// <param name="right">The second quaternion to add.</param>
        /// <param name="result">When the method completes, contains the sum of the two quaternions.</param>
        public static void Add(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result.X = left.X + right.X;
            result.Y = left.Y + right.Y;
            result.Z = left.Z + right.Z;
            result.W = left.W + right.W;
        }

        /// <summary>
        /// Adds two quaternions.
        /// </summary>
        /// <param name="left">The first quaternion to add.</param>
        /// <param name="right">The second quaternion to add.</param>
        /// <returns>The sum of the two quaternions.</returns>
        public static Quaternion Add(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Subtracts two quaternions.
        /// </summary>
        /// <param name="left">The first quaternion to subtract.</param>
        /// <param name="right">The second quaternion to subtract.</param>
        /// <param name="result">When the method completes, contains the difference of the two quaternions.</param>
        public static void Subtract(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result.X = left.X - right.X;
            result.Y = left.Y - right.Y;
            result.Z = left.Z - right.Z;
            result.W = left.W - right.W;
        }

        /// <summary>
        /// Subtracts two quaternions.
        /// </summary>
        /// <param name="left">The first quaternion to subtract.</param>
        /// <param name="right">The second quaternion to subtract.</param>
        /// <returns>The difference of the two quaternions.</returns>
        public static Quaternion Subtract(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Scales a quaternion by the given value.
        /// </summary>
        /// <param name="value">The quaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the quaternion.</param>
        /// <param name="result">When the method completes, contains the scaled quaternion.</param>
        public static void Multiply(ref Quaternion value, float scale, out Quaternion result)
        {
            result.X = value.X * scale;
            result.Y = value.Y * scale;
            result.Z = value.Z * scale;
            result.W = value.W * scale;
        }

        /// <summary>
        /// Scales a quaternion by the given value.
        /// </summary>
        /// <param name="value">The quaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the quaternion.</param>
        /// <returns>The scaled quaternion.</returns>
        public static Quaternion Multiply(Quaternion value, float scale)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        /// <summary>
        /// Modulates a quaternion by another.
        /// </summary>
        /// <param name="left">The first quaternion to modulate.</param>
        /// <param name="right">The second quaternion to modulate.</param>
        /// <param name="result">When the moethod completes, contains the modulated quaternion.</param>
        public static void Multiply(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            float lx = left.X;
            float ly = left.Y;
            float lz = left.Z;
            float lw = left.W;
            float rx = right.X;
            float ry = right.Y;
            float rz = right.Z;
            float rw = right.W;

            result.X = (rx * lw + lx * rw + ry * lz) - (rz * ly);
            result.Y = (ry * lw + ly * rw + rz * lx) - (rx * lz);
            result.Z = (rz * lw + lz * rw + rx * ly) - (ry * lx);
            result.W = (rw * lw) - (rx * lx + ry * ly + rz * lz);
        }

        /// <summary>
        /// Modulates a quaternion by another.
        /// </summary>
        /// <param name="left">The first quaternion to modulate.</param>
        /// <param name="right">The second quaternion to modulate.</param>
        /// <returns>The modulated quaternion.</returns>
        public static Quaternion Multiply(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }
        
        /// <summary>
        /// Reverses the direction of a given quaternion.
        /// </summary>
        /// <param name="value">The quaternion to negate.</param>
        /// <param name="result">When the method completes, contains a quaternion facing in the opposite direction.</param>
        public static void Negate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        /// <summary>
        /// Reverses the direction of a given quaternion.
        /// </summary>
        /// <param name="value">The quaternion to negate.</param>
        /// <returns>A quaternion facing in the opposite direction.</returns>
        public static Quaternion Negate(Quaternion value)
        {
            Quaternion result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Returns a <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <param name="result">When the method completes, contains a new <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of the specified point.</param>
        public static void Barycentric(ref Quaternion value1, ref Quaternion value2, ref Quaternion value3, float amount1, float amount2, out Quaternion result)
        {
            Quaternion start, end;
            Slerp(ref value1, ref value2, amount1 + amount2, out start);
            Slerp(ref value1, ref value3, amount1 + amount2, out end);
            Slerp(ref start, ref end, amount2 / (amount1 + amount2), out result);
        }

        /// <summary>
        /// Returns a <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
        /// </summary>
        /// <param name="value1">A <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
        /// <returns>A new <see cref="Stride.Core.Mathematics.Quaternion"/> containing the 4D Cartesian coordinates of the specified point.</returns>
        public static Quaternion Barycentric(Quaternion value1, Quaternion value2, Quaternion value3, float amount1, float amount2)
        {
            Quaternion result;
            Barycentric(ref value1, ref value2, ref value3, amount1, amount2, out result);
            return result;
        }

        /// <summary>
        /// Conjugates a quaternion.
        /// </summary>
        /// <param name="value">The quaternion to conjugate.</param>
        /// <param name="result">When the method completes, contains the conjugated quaternion.</param>
        public static void Conjugate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        /// <summary>
        /// Conjugates a quaternion.
        /// </summary>
        /// <param name="value">The quaternion to conjugate.</param>
        /// <returns>The conjugated quaternion.</returns>
        public static Quaternion Conjugate(Quaternion value)
        {
            Quaternion result;
            Conjugate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Calculates the dot product of two quaternions.
        /// </summary>
        /// <param name="left">First source quaternion.</param>
        /// <param name="right">Second source quaternion.</param>
        /// <param name="result">When the method completes, contains the dot product of the two quaternions.</param>
        public static void Dot(ref Quaternion left, ref Quaternion right, out float result)
        {
            result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        /// <summary>
        /// Calculates the dot product of two quaternions.
        /// </summary>
        /// <param name="left">First source quaternion.</param>
        /// <param name="right">Second source quaternion.</param>
        /// <returns>The dot product of the two quaternions.</returns>
        public static float Dot(Quaternion left, Quaternion right)
        {
            return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
        }

        /// <summary>
        /// Exponentiates a quaternion.
        /// </summary>
        /// <param name="value">The quaternion to exponentiate.</param>
        /// <param name="result">When the method completes, contains the exponentiated quaternion.</param>
        public static void Exponential(ref Quaternion value, out Quaternion result)
        {
            float angle = (float)Math.Sqrt((value.X * value.X) + (value.Y * value.Y) + (value.Z * value.Z));
            float sin = (float)Math.Sin(angle);

            if (Math.Abs(sin) >= MathUtil.ZeroTolerance)
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

            result.W = (float)Math.Cos(angle);
        }

        /// <summary>
        /// Exponentiates a quaternion.
        /// </summary>
        /// <param name="value">The quaternion to exponentiate.</param>
        /// <returns>The exponentiated quaternion.</returns>
        public static Quaternion Exponential(Quaternion value)
        {
            Quaternion result;
            Exponential(ref value, out result);
            return result;
        }

        /// <summary>
        /// Conjugates and renormalizes the quaternion.
        /// </summary>
        /// <param name="value">The quaternion to conjugate and renormalize.</param>
        /// <param name="result">When the method completes, contains the conjugated and renormalized quaternion.</param>
        public static void Invert(ref Quaternion value, out Quaternion result)
        {
            result = value;
            result.Invert();
        }

        /// <summary>
        /// Conjugates and renormalizes the quaternion.
        /// </summary>
        /// <param name="value">The quaternion to conjugate and renormalize.</param>
        /// <returns>The conjugated and renormalized quaternion.</returns>
        public static Quaternion Invert(Quaternion value)
        {
            Quaternion result;
            Invert(ref value, out result);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between two quaternions.
        /// </summary>
        /// <param name="start">Start quaternion.</param>
        /// <param name="end">End quaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two quaternions.</param>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static void Lerp(ref Quaternion start, ref Quaternion end, float amount, out Quaternion result)
        {
            float inverse = 1.0f - amount;

            if (Dot(start, end) >= 0.0f)
            {
                result.X = (inverse * start.X) + (amount * end.X);
                result.Y = (inverse * start.Y) + (amount * end.Y);
                result.Z = (inverse * start.Z) + (amount * end.Z);
                result.W = (inverse * start.W) + (amount * end.W);
            }
            else
            {
                result.X = (inverse * start.X) - (amount * end.X);
                result.Y = (inverse * start.Y) - (amount * end.Y);
                result.Z = (inverse * start.Z) - (amount * end.Z);
                result.W = (inverse * start.W) - (amount * end.W);
            }

            result.Normalize();
        }

        /// <summary>
        /// Performs a linear interpolation between two quaternion.
        /// </summary>
        /// <param name="start">Start quaternion.</param>
        /// <param name="end">End quaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two quaternions.</returns>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static Quaternion Lerp(Quaternion start, Quaternion end, float amount)
        {
            Quaternion result;
            Lerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Calculates the natural logarithm of the specified quaternion.
        /// </summary>
        /// <param name="value">The quaternion whose logarithm will be calculated.</param>
        /// <param name="result">When the method completes, contains the natural logarithm of the quaternion.</param>
        public static void Logarithm(ref Quaternion value, out Quaternion result)
        {
            if (Math.Abs(value.W) < 1.0)
            {
                float angle = (float)Math.Acos(value.W);
                float sin = (float)Math.Sin(angle);

                if (Math.Abs(sin) >= MathUtil.ZeroTolerance)
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
        }

        /// <summary>
        /// Calculates the natural logarithm of the specified quaternion.
        /// </summary>
        /// <param name="value">The quaternion whose logarithm will be calculated.</param>
        /// <returns>The natural logarithm of the quaternion.</returns>
        public static Quaternion Logarithm(Quaternion value)
        {
            Quaternion result;
            Logarithm(ref value, out result);
            return result;
        }

        /// <summary>
        /// Converts the quaternion into a unit quaternion.
        /// </summary>
        /// <param name="value">The quaternion to normalize.</param>
        /// <param name="result">When the method completes, contains the normalized quaternion.</param>
        public static void Normalize(ref Quaternion value, out Quaternion result)
        {
            Quaternion temp = value;
            result = temp;
            result.Normalize();
        }

        /// <summary>
        /// Converts the quaternion into a unit quaternion.
        /// </summary>
        /// <param name="value">The quaternion to normalize.</param>
        /// <returns>The normalized quaternion.</returns>
        public static Quaternion Normalize(Quaternion value)
        {
            value.Normalize();
            return value;
        }

        /// <summary>
        /// Rotates a Vector3 by the specified quaternion rotation.
        /// </summary>
        /// <param name="vector">The vector to rotate.</param>
        public void Rotate(ref Vector3 vector)
        {
            var pureQuaternion = new Quaternion(vector, 0);
            pureQuaternion = Conjugate(this) * pureQuaternion * this;

            vector.X = pureQuaternion.X;
            vector.Y = pureQuaternion.Y;
            vector.Z = pureQuaternion.Z;
        }

        /// <summary>
        /// Creates a quaternion given a rotation and an axis.
        /// </summary>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationAxis(ref Vector3 axis, float angle, out Quaternion result)
        {
            Vector3 normalized;
            Vector3.Normalize(ref axis, out normalized);

            float half = angle * 0.5f;
            float sin = (float)Math.Sin(half);
            float cos = (float)Math.Cos(half);

            result.X = normalized.X * sin;
            result.Y = normalized.Y * sin;
            result.Z = normalized.Z * sin;
            result.W = cos;
        }

        /// <summary>
        /// Creates a quaternion given a rotation and an axis.
        /// </summary>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation.</param>
        /// <returns>The newly created quaternion.</returns>
        public static Quaternion RotationAxis(Vector3 axis, float angle)
        {
            Quaternion result;
            RotationAxis(ref axis, angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a quaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationMatrix(ref Matrix matrix, out Quaternion result)
        {
            float sqrt;
            float half;
            float scale = matrix.M11 + matrix.M22 + matrix.M33;

            if (scale > 0.0f)
            {
                sqrt = (float)Math.Sqrt(scale + 1.0f);
                result.W = sqrt * 0.5f;
                sqrt = 0.5f / sqrt;

                result.X = (matrix.M23 - matrix.M32) * sqrt;
                result.Y = (matrix.M31 - matrix.M13) * sqrt;
                result.Z = (matrix.M12 - matrix.M21) * sqrt;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = 0.5f * sqrt;
                result.Y = (matrix.M12 + matrix.M21) * half;
                result.Z = (matrix.M13 + matrix.M31) * half;
                result.W = (matrix.M23 - matrix.M32) * half;
            }
            else if (matrix.M22 > matrix.M33)
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = (matrix.M21 + matrix.M12) * half;
                result.Y = 0.5f * sqrt;
                result.Z = (matrix.M32 + matrix.M23) * half;
                result.W = (matrix.M31 - matrix.M13) * half;
            }
            else
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                half = 0.5f / sqrt;

                result.X = (matrix.M31 + matrix.M13) * half;
                result.Y = (matrix.M32 + matrix.M23) * half;
                result.Z = 0.5f * sqrt;
                result.W = (matrix.M12 - matrix.M21) * half;
            }
        }

        /// <summary>
        /// Creates a quaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <returns>The newly created quaternion.</returns>
        public static Quaternion RotationMatrix(Matrix matrix)
        {
            Quaternion result;
            RotationMatrix(ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Creates a quaternion that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationX(float angle, out Quaternion result)
        {
            float halfAngle = angle * 0.5f;
            result = new Quaternion((float)Math.Sin(halfAngle), 0.0f, 0.0f, (float)Math.Cos(halfAngle));
        }

        /// <summary>
        /// Creates a quaternion that rotates around the x-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <returns>The created rotation quaternion.</returns>
        public static Quaternion RotationX(float angle)
        {
            Quaternion result;
            RotationX(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a quaternion that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationY(float angle, out Quaternion result)
        {
            float halfAngle = angle * 0.5f;
            result = new Quaternion(0.0f, (float)Math.Sin(halfAngle), 0.0f, (float)Math.Cos(halfAngle));
        }

        /// <summary>
        /// Creates a quaternion that rotates around the y-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <returns>The created rotation quaternion.</returns>
        public static Quaternion RotationY(float angle)
        {
            Quaternion result;
            RotationY(angle, out result);
            return result;
        }

        /// <summary>
        /// Creates a quaternion that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationZ(float angle, out Quaternion result)
        {
            float halfAngle = angle * 0.5f;
            result = new Quaternion(0.0f, 0.0f, (float)Math.Sin(halfAngle), (float)Math.Cos(halfAngle));
        }

        /// <summary>
        /// Creates a quaternion that rotates around the z-axis.
        /// </summary>
        /// <param name="angle">Angle of rotation in radians.</param>
        /// <returns>The created rotation quaternion.</returns>
        public static Quaternion RotationZ(float angle)
        {
            Quaternion result;
            RotationZ(angle, out result);
            return result;
        }

        /// <summary>
        /// Calculate the yaw/pitch/roll rotation equivalent to the provided quaternion.
        /// </summary>
        /// <param name="rotation">The input quaternion</param>
        /// <param name="yaw">The yaw component</param>
        /// <param name="pitch">The pitch component</param>
        /// <param name="roll">The roll component</param>
        public static void RotationYawPitchRoll(ref Quaternion rotation, out float yaw, out float pitch, out float roll)
        {
            // Equivalent to:
            //  Matrix rotationMatrix;
            //  Matrix.Rotation(ref cachedRotation, out rotationMatrix);
            //  rotationMatrix.DecomposeXYZ(out rotationEuler);

            var xx = rotation.X * rotation.X;
            var yy = rotation.Y * rotation.Y;
            var zz = rotation.Z * rotation.Z;
            var xy = rotation.X * rotation.Y;
            var zw = rotation.Z * rotation.W;
            var zx = rotation.Z * rotation.X;
            var yw = rotation.Y * rotation.W;
            var yz = rotation.Y * rotation.Z;
            var xw = rotation.X * rotation.W;

            pitch = (float)Math.Asin(2.0f * (xw - yz));
            double test = Math.Cos(pitch);
            if (test > MathUtil.ZeroTolerance)
            {
                roll = (float)Math.Atan2(2.0f * (xy + zw), 1.0f - (2.0f * (zz + xx)));
                yaw = (float)Math.Atan2(2.0f * (zx + yw), 1.0f - (2.0f * (yy + xx)));
            }
            else
            {
                roll = (float)Math.Atan2(-2.0f * (xy - zw), 1.0f - (2.0f * (yy + zz)));
                yaw = 0.0f;
            }
        }

        /// <summary>
        /// Creates a quaternion given a yaw, pitch, and roll value (angles in radians).
        /// </summary>
        /// <param name="yaw">The yaw of rotation in radians.</param>
        /// <param name="pitch">The pitch of rotation in radians.</param>
        /// <param name="roll">The roll of rotation in radians.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        public static void RotationYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
        {
            var halfRoll = roll * 0.5f;
            var halfPitch = pitch * 0.5f;
            var halfYaw = yaw * 0.5f;
            
            var sinRoll = (float)Math.Sin(halfRoll);
            var cosRoll = (float)Math.Cos(halfRoll);
            var sinPitch = (float)Math.Sin(halfPitch);
            var cosPitch = (float)Math.Cos(halfPitch);
            var sinYaw = (float)Math.Sin(halfYaw);
            var cosYaw = (float)Math.Cos(halfYaw);

            var cosYawPitch = cosYaw * cosPitch;
            var sinYawPitch = sinYaw * sinPitch;

            result.X = (cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll);
            result.Y = (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll);
            result.Z = (cosYawPitch * sinRoll) - (sinYawPitch * cosRoll);
            result.W = (cosYawPitch * cosRoll) + (sinYawPitch * sinRoll);
        }

        /// <summary>
        /// Creates a quaternion given a yaw, pitch, and roll value.
        /// </summary>
        /// <param name="yaw">The yaw of rotation.</param>
        /// <param name="pitch">The pitch of rotation.</param>
        /// <param name="roll">The roll of rotation.</param>
        /// <returns>The newly created quaternion.</returns>
        public static Quaternion RotationYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion result;
            RotationYawPitchRoll(yaw, pitch, roll, out result);
            return result;
        }

        /// <summary>
        /// Computes a quaternion corresponding to the rotation transforming the vector <paramref name="source"/> to the vector <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source vector of the transformation.</param>
        /// <param name="target">The target vector of the transformation.</param>
        /// <returns>The resulting quaternion corresponding to the transformation of the source vector to the target vector.</returns>
        public static Quaternion BetweenDirections(Vector3 source, Vector3 target)
        {
            Quaternion result;
            BetweenDirections(ref source, ref target, out result);
            return result;
        }

        /// <summary>
        /// Computes a quaternion corresponding to the rotation transforming the vector <paramref name="source"/> to the vector <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source vector of the transformation.</param>
        /// <param name="target">The target vector of the transformation.</param>
        /// <param name="result">The resulting quaternion corresponding to the transformation of the source vector to the target vector.</param>
        public static void BetweenDirections(ref Vector3 source, ref Vector3 target, out Quaternion result)
        {
            var norms = (float)Math.Sqrt(source.LengthSquared() * target.LengthSquared());
            var real = norms + Vector3.Dot(source, target);
            if (real < MathUtil.ZeroTolerance * norms)
            {
                // If source and target are exactly opposite, rotate 180 degrees around an arbitrary orthogonal axis.
                // Axis normalisation can happen later, when we normalise the quaternion.
                result = Math.Abs(source.X) > Math.Abs(source.Z)
                    ? new Quaternion(-source.Y, source.X, 0.0f, 0.0f)
                    : new Quaternion(0.0f, -source.Z, source.Y, 0.0f);
            }
            else
            {
                // Otherwise, build quaternion the standard way.
                var axis = Vector3.Cross(source, target);
                result = new Quaternion(axis, real);
            }
            result.Normalize();
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="start">Start quaternion.</param>
        /// <param name="end">End quaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the spherical linear interpolation of the two quaternions.</param>
        public static void Slerp(ref Quaternion start, ref Quaternion end, float amount, out Quaternion result)
        {
            float opposite;
            float inverse;
            float dot = Dot(start, end);

            if (Math.Abs(dot) > 1.0f - MathUtil.ZeroTolerance)
            {
                inverse = 1.0f - amount;
                opposite = amount * Math.Sign(dot);
            }
            else
            {
                float acos = (float)Math.Acos(Math.Abs(dot));
                float invSin = (float)(1.0 / Math.Sin(acos));

                inverse = (float)Math.Sin((1.0f - amount) * acos) * invSin;
                opposite = (float)Math.Sin(amount * acos) * invSin * Math.Sign(dot);
            }

            result.X = (inverse * start.X) + (opposite * end.X);
            result.Y = (inverse * start.Y) + (opposite * end.Y);
            result.Z = (inverse * start.Z) + (opposite * end.Z);
            result.W = (inverse * start.W) + (opposite * end.W);
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="start">Start quaternion.</param>
        /// <param name="end">End quaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The spherical linear interpolation of the two quaternions.</returns>
        public static Quaternion Slerp(Quaternion start, Quaternion end, float amount)
        {
            Quaternion result;
            Slerp(ref start, ref end, amount, out result);
            return result;
        }

        /// <summary>
        /// Interpolates between quaternions, using spherical quadrangle interpolation.
        /// </summary>
        /// <param name="value1">First source quaternion.</param>
        /// <param name="value2">Second source quaternion.</param>
        /// <param name="value3">Thrid source quaternion.</param>
        /// <param name="value4">Fourth source quaternion.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of interpolation.</param>
        /// <param name="result">When the method completes, contains the spherical quadrangle interpolation of the quaternions.</param>
        public static void Squad(ref Quaternion value1, ref Quaternion value2, ref Quaternion value3, ref Quaternion value4, float amount, out Quaternion result)
        {
            Quaternion start, end;
            Slerp(ref value1, ref value4, amount, out start);
            Slerp(ref value2, ref value3, amount, out end);
            Slerp(ref start, ref end, 2.0f * amount * (1.0f - amount), out result);
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
        public static Quaternion Squad(Quaternion value1, Quaternion value2, Quaternion value3, Quaternion value4, float amount)
        {
            Quaternion result;
            Squad(ref value1, ref value2, ref value3, ref value4, amount, out result);
            return result;
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

            Quaternion q1Exp, q2Exp;
            Exponential(ref q1, out q1Exp);
            Exponential(ref q2, out q2Exp);

            Quaternion[] results = new Quaternion[3];
            results[0] = q1 * Exponential(-0.25f * (Logarithm(q1Exp * q2) + Logarithm(q1Exp * q0)));
            results[1] = q2 * Exponential(-0.25f * (Logarithm(q2Exp * q3) + Logarithm(q2Exp * q1)));
            results[2] = q2;

            return results;
        }

        /// <summary>
        /// Adds two quaternions.
        /// </summary>
        /// <param name="left">The first quaternion to add.</param>
        /// <param name="right">The second quaternion to add.</param>
        /// <returns>The sum of the two quaternions.</returns>
        public static Quaternion operator +(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Add(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Subtracts two quaternions.
        /// </summary>
        /// <param name="left">The first quaternion to subtract.</param>
        /// <param name="right">The second quaternion to subtract.</param>
        /// <returns>The difference of the two quaternions.</returns>
        public static Quaternion operator -(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Subtract(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Reverses the direction of a given quaternion.
        /// </summary>
        /// <param name="value">The quaternion to negate.</param>
        /// <returns>A quaternion facing in the opposite direction.</returns>
        public static Quaternion operator -(Quaternion value)
        {
            Quaternion result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Scales a quaternion by the given value.
        /// </summary>
        /// <param name="value">The quaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the quaternion.</param>
        /// <returns>The scaled quaternion.</returns>
        public static Quaternion operator *(float scale, Quaternion value)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        /// <summary>
        /// Scales a quaternion by the given value.
        /// </summary>
        /// <param name="value">The quaternion to scale.</param>
        /// <param name="scale">The amount by which to scale the quaternion.</param>
        /// <returns>The scaled quaternion.</returns>
        public static Quaternion operator *(Quaternion value, float scale)
        {
            Quaternion result;
            Multiply(ref value, scale, out result);
            return result;
        }

        /// <summary>
        /// Multiplies a quaternion by another.
        /// </summary>
        /// <param name="left">The first quaternion to multiply.</param>
        /// <param name="right">The second quaternion to multiply.</param>
        /// <returns>The multiplied quaternion.</returns>
        public static Quaternion operator *(Quaternion left, Quaternion right)
        {
            Quaternion result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Quaternion left, Quaternion right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Quaternion left, Quaternion right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", X, Y, Z, W);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            if (format == null)
                return ToString();

            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Z:{2} W:{3}", X.ToString(format, CultureInfo.CurrentCulture),
                Y.ToString(format, CultureInfo.CurrentCulture), Z.ToString(format, CultureInfo.CurrentCulture), W.ToString(format, CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", X, Y, Z, W);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider, "X:{0} Y:{1} Z:{2} W:{3}", X.ToString(format, formatProvider),
                Y.ToString(format, formatProvider), Z.ToString(format, formatProvider), W.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Stride.Core.Mathematics.Quaternion"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Stride.Core.Mathematics.Quaternion"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Stride.Core.Mathematics.Quaternion"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Quaternion other)
        {
            return ((float)Math.Abs(other.X - X) < MathUtil.ZeroTolerance &&
                (float)Math.Abs(other.Y - Y) < MathUtil.ZeroTolerance &&
                (float)Math.Abs(other.Z - Z) < MathUtil.ZeroTolerance &&
                (float)Math.Abs(other.W - W) < MathUtil.ZeroTolerance);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (value == null)
                return false;

            if (value.GetType() != GetType())
                return false;

            return Equals((Quaternion)value);
        }

#if SlimDX1xInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Quaternion"/> to <see cref="SlimDX.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator SlimDX.Quaternion(Quaternion value)
        {
            return new SlimDX.Quaternion(value.X, value.Y, value.Z, value.W);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SlimDX.Quaternion"/> to <see cref="Stride.Core.Mathematics.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Quaternion(SlimDX.Quaternion value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W);
        }
#endif

#if WPFInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Quaternion"/> to <see cref="System.Windows.Media.Media3D.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator System.Windows.Media.Media3D.Quaternion(Quaternion value)
        {
            return new System.Windows.Media.Media3D.Quaternion(value.X, value.Y, value.Z, value.W);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Windows.Media.Media3D.Quaternion"/> to <see cref="Stride.Core.Mathematics.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Quaternion(System.Windows.Media.Media3D.Quaternion value)
        {
            return new Quaternion((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
        }
#endif

#if XnaInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Quaternion"/> to <see cref="Microsoft.Xna.Framework.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Microsoft.Xna.Framework.Quaternion(Quaternion value)
        {
            return new Microsoft.Xna.Framework.Quaternion(value.X, value.Y, value.Z, value.W);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Quaternion"/> to <see cref="Stride.Core.Mathematics.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Quaternion(Microsoft.Xna.Framework.Quaternion value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W);
        }
#endif
    }
}
