// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2011 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Stride.Core.Mathematics;

/// <summary>
///   A half precision (16 bit) floating point value.
/// </summary>
[DataContract]
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct Half
{
    /// <summary>
    ///   Number of decimal digits of precision.
    /// </summary>
    public const int PrecisionDigits = 3;

    /// <summary>
    ///   Number of bits in the mantissa.
    /// </summary>
    public const int MantissaBits = 11;

    /// <summary>
    ///   Maximum decimal exponent.
    /// </summary>
    public const int MaximumDecimalExponent = 4;

    /// <summary>
    ///   Maximum binary exponent.
    /// </summary>
    public const int MaximumBinaryExponent = 15;

    /// <summary>
    ///   Minimum decimal exponent.
    /// </summary>
    public const int MinimumDecimalExponent = -4;

    /// <summary>
    ///   Minimum binary exponent.
    /// </summary>
    public const int MinimumBinaryExponent = -14;

    /// <summary>
    ///   Exponent radix.
    /// </summary>
    public const int ExponentRadix = 2;

    /// <summary>
    ///   Additional rounding.
    /// </summary>
    public const int AdditionRounding = 1;

    /// <summary>
    ///   Smallest such that 1.0 + epsilon != 1.0
    /// </summary>
    public static readonly float Epsilon = 0.0004887581f;

    /// <summary>
    ///   Maximum value of the number.
    /// </summary>
    public static readonly float MaxValue = 65504f;

    /// <summary>
    ///   Minimum value of the number.
    /// </summary>
    public static readonly float MinValue = 6.103516E-05f;

    /// <summary>
    /// A <see cref="Half"/> whose value is 0.0f.
    /// </summary>
    public static readonly Half Zero = (Half)0.0f;

    /// <summary>
    /// A <see cref="Half"/> whose value is 1.0f.
    /// </summary>
    public static readonly Half One = (Half)1.0f;

    /// <summary>
    ///   Initializes a new instance of the <see cref="Half"/> structure.
    /// </summary>
    /// <param name = "value">The floating point value that should be stored in 16 bit format.</param>
    public Half(float value)
    {
        RawValue = HalfUtils.Pack(value);
    }

    /// <summary>
    ///   Gets or sets the raw 16 bit value used to back this half-float.
    /// </summary>
    public ushort RawValue { get; set; }

    /// <summary>
    ///   Converts an array of half precision values into full precision values.
    /// </summary>
    /// <param name = "values">The values to be converted.</param>
    /// <returns>An array of converted values.</returns>
    public static float[] ConvertToFloat(Half[] values)
    {
        float[] results = new float[values.Length];
        for (int i = 0; i < results.Length; i++)
            results[i] = HalfUtils.Unpack(values[i].RawValue);
        return results;
    }

    /// <summary>
    ///   Converts an array of full precision values into half precision values.
    /// </summary>
    /// <param name = "values">The values to be converted.</param>
    /// <returns>An array of converted values.</returns>
    public static Half[] ConvertToHalf(float[] values)
    {
        Half[] results = new Half[values.Length];
        for (int i = 0; i < results.Length; i++)
            results[i] = new Half(values[i]);
        return results;
    }

    /// <summary>
    ///   Performs an explicit conversion from <see cref = "T:System.Single" /> to <see cref = "T:Stride.Core.Mathematics.Half" />.
    /// </summary>
    /// <param name = "value">The value to be converted.</param>
    /// <returns>The converted value.</returns>
    public static explicit operator Half(float value)
    {
        return new Half(value);
    }

    /// <summary>
    ///   Performs an implicit conversion from <see cref = "T:Stride.Core.Mathematics.Half" /> to <see cref = "T:System.Single" />.
    /// </summary>
    /// <param name = "value">The value to be converted.</param>
    /// <returns>The converted value.</returns>
    public static implicit operator float(Half value)
    {
        return HalfUtils.Unpack(value.RawValue);
    }

    /// <summary>
    ///   Tests for equality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns>
    ///   <c>true</c> if <paramref name = "left" /> has the same value as <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Half left, Half right)
    {
        return left.RawValue == right.RawValue;
    }

    /// <summary>
    ///   Tests for inequality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns>
    ///   <c>true</c> if <paramref name = "left" /> has a different value than <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Half left, Half right)
    {
        return left.RawValue != right.RawValue;
    }

    /// <summary>
    ///   Converts the value of the object to its equivalent string representation.
    /// </summary>
    /// <returns>The string representation of the value of this instance.</returns>
    public override readonly string ToString()
    {
        float num = this;
        return num.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    ///   Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(RawValue);
    }

    /// <summary>
    ///   Determines whether the specified object instances are considered equal.
    /// </summary>
    /// <param name = "value1">The first value.</param>
    /// <param name = "value2">The second value.</param>
    /// <returns>
    ///   <c>true</c> if <paramref name = "value1" /> is the same instance as <paramref name = "value2" /> or
    ///   if both are <c>null</c> references or if <c>value1.Equals(value2)</c> returns <c>true</c>; otherwise, <c>false</c>.</returns>
    public static bool Equals(ref readonly Half value1, ref readonly Half value2)
    {
        return value1.RawValue == value2.RawValue;
    }

    /// <summary>
    ///   Returns a value that indicates whether the current instance is equal to the specified object.
    /// </summary>
    /// <param name = "other">Object to make the comparison with.</param>
    /// <returns>
    ///   <c>true</c> if the current instance is equal to the specified object; <c>false</c> otherwise.</returns>
    public readonly bool Equals(Half other)
    {
        return other.RawValue == RawValue;
    }

    /// <summary>
    ///   Returns a value that indicates whether the current instance is equal to a specified object.
    /// </summary>
    /// <param name = "obj">Object to make the comparison with.</param>
    /// <returns>
    ///   <c>true</c> if the current instance is equal to the specified object; <c>false</c> otherwise.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Half half && Equals(half);
    }
}
