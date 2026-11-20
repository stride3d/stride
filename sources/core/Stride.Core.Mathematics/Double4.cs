// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Stride.Core.Mathematics;

/// <summary>
/// Represents a four dimensional mathematical vector with double-precision floats.
/// </summary>
[DataContract("double4")]
[DataStyle(DataStyle.Compact)]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Double4 : IEquatable<Double4>, ISpanFormattable
{
    /// <summary>
    /// The size of the <see cref="Double4"/> type, in bytes.
    /// </summary>
    public static readonly int SizeInBytes = Unsafe.SizeOf<Double4>();

    /// <summary>
    /// A <see cref="Double4"/> with all of its components set to zero.
    /// </summary>
    public static readonly Double4 Zero = new();

    /// <summary>
    /// The X unit <see cref="Double4"/> (1, 0, 0, 0).
    /// </summary>
    public static readonly Double4 UnitX = new(1.0, 0.0, 0.0, 0.0);

    /// <summary>
    /// The Y unit <see cref="Double4"/> (0, 1, 0, 0).
    /// </summary>
    public static readonly Double4 UnitY = new(0.0, 1.0, 0.0, 0.0);

    /// <summary>
    /// The Z unit <see cref="Double4"/> (0, 0, 1, 0).
    /// </summary>
    public static readonly Double4 UnitZ = new(0.0, 0.0, 1.0, 0.0);

    /// <summary>
    /// The W unit <see cref="Double4"/> (0, 0, 0, 1).
    /// </summary>
    public static readonly Double4 UnitW = new(0.0, 0.0, 0.0, 1.0);

    /// <summary>
    /// A <see cref="Double4"/> with all of its components set to one.
    /// </summary>
    public static readonly Double4 One = new(1.0, 1.0, 1.0, 1.0);

    /// <summary>
    /// The X component of the vector.
    /// </summary>
    [DataMember(0)]
    public double X;

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    [DataMember(1)]
    public double Y;

    /// <summary>
    /// The Z component of the vector.
    /// </summary>
    [DataMember(2)]
    public double Z;

    /// <summary>
    /// The W component of the vector.
    /// </summary>
    [DataMember(3)]
    public double W;

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> struct.
    /// </summary>
    /// <param name="value">The value that will be assigned to all components.</param>
    public Double4(double value)
    {
        X = value;
        Y = value;
        Z = value;
        W = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> struct.
    /// </summary>
    /// <param name="x">Initial value for the X component of the vector.</param>
    /// <param name="y">Initial value for the Y component of the vector.</param>
    /// <param name="z">Initial value for the Z component of the vector.</param>
    /// <param name="w">Initial value for the W component of the vector.</param>
    public Double4(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> struct.
    /// </summary>
    /// <param name="value">A vector containing the values with which to initialize the X, Y, and Z components.</param>
    /// <param name="w">Initial value for the W component of the vector.</param>
    public Double4(Double3 value, double w)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
        W = w;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> struct.
    /// </summary>
    /// <param name="value">A vector containing the values with which to initialize the X and Y components.</param>
    /// <param name="z">Initial value for the Z component of the vector.</param>
    /// <param name="w">Initial value for the W component of the vector.</param>
    public Double4(Double2 value, double z, double w)
    {
        X = value.X;
        Y = value.Y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> struct.
    /// </summary>
    /// <param name="values">The values to assign to the X, Y, Z, and W components of the vector. This must be an array with four elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
    public Double4(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length != 4)
            throw new ArgumentOutOfRangeException(nameof(values), "There must be four and only four input values for Double4.");

        X = values[0];
        Y = values[1];
        Z = values[2];
        W = values[3];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> struct.
    /// </summary>
    /// <param name="v">The Vector4 to construct the Double4 from.</param>
    public Double4(Vector4 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
        W = v.W;
    }

    /// <summary>
    /// Gets a value indicting whether this instance is normalized.
    /// </summary>
    public readonly bool IsNormalized
    {
        get { return Math.Abs((X * X) + (Y * Y) + (Z * Z) + (W * W) - 1f) < MathUtil.ZeroTolerance; }
    }

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the X, Y, Z, or W component, depending on the index.</value>
    /// <param name="index">The index of the component to access. Use 0 for the X component, 1 for the Y component, 2 for the Z component, and 3 for the W component.</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 3].</exception>
    public double this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                3 => W,
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Indices for Double4 run from 0 to 3, inclusive."),
            };
        }

        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                case 3: W = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), "Indices for Double4 run from 0 to 3, inclusive.");
            }
        }
    }

    /// <summary>
    /// Calculates the length of the vector.
    /// </summary>
    /// <returns>The length of the vector.</returns>
    /// <remarks>
    /// <see cref="LengthSquared"/> may be preferred when only the relative length is needed
    /// and speed is of the essence.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double Length()
    {
        return (double)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
    }

    /// <summary>
    /// Calculates the squared length of the vector.
    /// </summary>
    /// <returns>The squared length of the vector.</returns>
    /// <remarks>
    /// This method may be preferred to <see cref="Length"/> when only a relative length is needed
    /// and speed is of the essence.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double LengthSquared()
    {
        return (X * X) + (Y * Y) + (Z * Z) + (W * W);
    }

    /// <summary>
    /// Creates an array containing the elements of the vector.
    /// </summary>
    /// <returns>A four-element array containing the components of the vector.</returns>
    public readonly double[] ToArray()
    {
        return [X, Y, Z, W];
    }

    /// <summary>Scales a vector to a length of one. A vector of length zero gives NaN, as the vectors of System.Numerics do.</summary>
    /// <param name="value">The vector to scale.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Normalize(Double4 value)
    {
        return value / value.Length();
    }

    /// <summary>Scales a vector to a length of one; a vector too short to have a direction is returned as it is.</summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="tolerance">The length up to which a vector is too short.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 NormalizeSafe(Double4 value, double tolerance = MathUtil.ZeroTolerance)
    {
        double length = value.Length();
        return length > tolerance ? value * (1.0 / length) : value;
    }

    /// <summary>Raises each component of a vector to a power.</summary>
    /// <param name="value">The vector.</param>
    /// <param name="exponent">The exponent.</param>
    public static Double4 Pow(Double4 value, double exponent)
    {
        return new Double4(Math.Pow(value.X, exponent), Math.Pow(value.Y, exponent), Math.Pow(value.Z, exponent), Math.Pow(value.W, exponent));
    }

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Add(Double4 left, Double4 right)
    {
        return new Double4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    /// <summary>
    /// Subtracts two vectors.
    /// </summary>
    /// <param name="left">The first vector to subtract.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>The difference of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Subtract(Double4 left, Double4 right)
    {
        return new Double4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Multiply(Double4 value, double scale)
    {
        return new Double4(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale);
    }

    /// <summary>
    /// Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    /// <param name="left">The first vector to modulate.</param>
    /// <param name="right">The second vector to modulate.</param>
    /// <returns>The modulated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Multiply(Double4 left, Double4 right)
    {
        return new Double4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Divide(Double4 value, double scale)
    {
        return new Double4(value.X / scale, value.Y / scale, value.Z / scale, value.W / scale);
    }

    /// <summary>
    /// Demodulates a vector with another by performing component-wise division.
    /// </summary>
    /// <param name="left">The first vector to demodulate.</param>
    /// <param name="right">The second vector to demodulate.</param>
    /// <returns>The demodulated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Divide(Double4 left, Double4 right)
    {
        return new Double4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
    }

    /// <summary>
    /// Reverses the direction of a given vector.
    /// </summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A vector facing in the opposite direction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Negate(Double4 value)
    {
        return new Double4(-value.X, -value.Y, -value.Z, -value.W);
    }

    /// <summary>
    /// Returns a <see cref="Double4"/> containing the 4D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 4D triangle.
    /// </summary>
    /// <param name="value1">A <see cref="Double4"/> containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
    /// <param name="value2">A <see cref="Double4"/> containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
    /// <param name="value3">A <see cref="Double4"/> containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
    /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
    /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
    /// <returns>A new <see cref="Double4"/> containing the 4D Cartesian coordinates of the specified point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Barycentric(Double4 value1, Double4 value2, Double4 value3, double amount1, double amount2)
    {
        return new Double4(value1.X + (amount1 * (value2.X - value1.X)) + (amount2 * (value3.X - value1.X)),
            value1.Y + (amount1 * (value2.Y - value1.Y)) + (amount2 * (value3.Y - value1.Y)),
            value1.Z + (amount1 * (value2.Z - value1.Z)) + (amount2 * (value3.Z - value1.Z)),
            value1.W + (amount1 * (value2.W - value1.W)) + (amount2 * (value3.W - value1.W)));
    }

    /// <summary>
    /// Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static Double4 Clamp(Double4 value, Double4 min, Double4 max)
    {
        double x = value.X;
        x = (x > max.X) ? max.X : x;
        x = (x < min.X) ? min.X : x;

        double y = value.Y;
        y = (y > max.Y) ? max.Y : y;
        y = (y < min.Y) ? min.Y : y;

        double z = value.Z;
        z = (z > max.Z) ? max.Z : z;
        z = (z < min.Z) ? min.Z : z;

        double w = value.W;
        w = (w > max.W) ? max.W : w;
        w = (w < min.W) ? min.W : w;

        return new Double4(x, y, z, w);
    }

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    /// <param name="value1">The first vector.</param>
    /// <param name="value2">The second vector.</param>
    /// <returns>The distance between the two vectors.</returns>
    /// <remarks>
    /// <see cref="DistanceSquared(Double4, Double4)"/> may be preferred when only the relative distance is needed
    /// and speed is of the essence.
    /// </remarks>
    public static double Distance(Double4 value1, Double4 value2)
    {
        double x = value1.X - value2.X;
        double y = value1.Y - value2.Y;
        double z = value1.Z - value2.Z;
        double w = value1.W - value2.W;

        return (double)Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));
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
    public static double DistanceSquared(Double4 value1, Double4 value2)
    {
        double x = value1.X - value2.X;
        double y = value1.Y - value2.Y;
        double z = value1.Z - value2.Z;
        double w = value1.W - value2.W;

        return (x * x) + (y * y) + (z * z) + (w * w);
    }

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="left">First source vector.</param>
    /// <param name="right">Second source vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(Double4 left, Double4 right)
    {
        return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z) + (left.W * right.W);
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
    /// <c>start + (end - start) * amount</c>
    /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Lerp(Double4 start, Double4 end, double amount)
    {
        Double4 result;
        result.X = start.X + ((end.X - start.X) * amount);
        result.Y = start.Y + ((end.Y - start.Y) * amount);
        result.Z = start.Z + ((end.Z - start.Z) * amount);
        result.W = start.W + ((end.W - start.W) * amount);
        return result;
    }

    /// <summary>
    /// Performs a cubic interpolation between two vectors.
    /// </summary>
    /// <param name="start">Start vector.</param>
    /// <param name="end">End vector.</param>
    /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
    /// <returns>The cubic interpolation of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 SmoothStep(Double4 start, Double4 end, double amount)
    {
        Double4 result;
        amount = (amount > 1.0) ? 1.0 : ((amount < 0.0) ? 0.0 : amount);
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
    public static Double4 Hermite(Double4 value1, Double4 tangent1, Double4 value2, Double4 tangent2, double amount)
    {
        double squared = amount * amount;
        double cubed = amount * squared;
        double part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0;
        double part2 = (-2.0f * cubed) + (3.0f * squared);
        double part3 = (cubed - (2.0f * squared)) + amount;
        double part4 = cubed - squared;

        return new Double4((value1.X * part1) + (value2.X * part2) + (tangent1.X * part3) + (tangent2.X * part4),
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
    public static Double4 CatmullRom(Double4 value1, Double4 value2, Double4 value3, Double4 value4, double amount)
    {
        Double4 result;
        double squared = amount * amount;
        double cubed = amount * squared;

        result.X = 0.5f * ((2.0f * value2.X) + ((-value1.X + value3.X) * amount) + (((2.0f * value1.X) - (5.0f * value2.X) + (4.0f * value3.X) - value4.X) * squared) + ((-value1.X + (3.0f * value2.X) - (3.0f * value3.X) + value4.X) * cubed));
        result.Y = 0.5f * ((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount) + (((2.0f * value1.Y) - (5.0f * value2.Y) + (4.0f * value3.Y) - value4.Y) * squared) + ((-value1.Y + (3.0f * value2.Y) - (3.0f * value3.Y) + value4.Y) * cubed));
        result.Z = 0.5f * ((2.0f * value2.Z) + ((-value1.Z + value3.Z) * amount) + (((2.0f * value1.Z) - (5.0f * value2.Z) + (4.0f * value3.Z) - value4.Z) * squared) + ((-value1.Z + (3.0f * value2.Z) - (3.0f * value3.Z) + value4.Z) * cubed));
        result.W = 0.5f * ((2.0f * value2.W) + ((-value1.W + value3.W) * amount) + (((2.0f * value1.W) - (5.0f * value2.W) + (4.0f * value3.W) - value4.W) * squared) + ((-value1.W + (3.0f * value2.W) - (3.0f * value3.W) + value4.W) * cubed));
        return result;
    }

    /// <summary>
    /// Returns a vector containing the largest components of the specified vectors.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>A vector containing the largest components of the source vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Max(Double4 left, Double4 right)
    {
        Double4 result;
        result.X = (left.X > right.X) ? left.X : right.X;
        result.Y = (left.Y > right.Y) ? left.Y : right.Y;
        result.Z = (left.Z > right.Z) ? left.Z : right.Z;
        result.W = (left.W > right.W) ? left.W : right.W;
        return result;
    }

    /// <summary>
    /// Returns a vector containing the smallest components of the specified vectors.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>A vector containing the smallest components of the source vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Min(Double4 left, Double4 right)
    {
        Double4 result;
        result.X = (left.X < right.X) ? left.X : right.X;
        result.Y = (left.Y < right.Y) ? left.Y : right.Y;
        result.Z = (left.Z < right.Z) ? left.Z : right.Z;
        result.W = (left.W < right.W) ? left.W : right.W;
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
    public static void Orthogonalize(Double4[] destination, params Double4[] source)
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
            Double4 newvector = source[i];

            for (int r = 0; r < i; ++r)
            {
                newvector -= Dot(destination[r], newvector) / Dot(destination[r], destination[r]) * destination[r];
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
    public static void Orthonormalize(Double4[] destination, params Double4[] source)
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
            Double4 newvector = source[i];

            for (int r = 0; r < i; ++r)
            {
                newvector -= Dot(destination[r], newvector) * destination[r];
            }

            newvector = Double4.NormalizeSafe(newvector);
            destination[i] = newvector;
        }
    }

    /// <summary>
    /// Transforms a 4D vector by the given <see cref="Quaternion"/> rotation.
    /// </summary>
    /// <param name="vector">The vector to rotate.</param>
    /// <param name="rotation">The <see cref="Quaternion"/> rotation to apply.</param>
    /// <returns>The transformed <see cref="Double4"/>.</returns>
    public static Double4 Transform(Double4 vector, Quaternion rotation)
    {
        double x = rotation.X + rotation.X;
        double y = rotation.Y + rotation.Y;
        double z = rotation.Z + rotation.Z;
        double wx = rotation.W * x;
        double wy = rotation.W * y;
        double wz = rotation.W * z;
        double xx = rotation.X * x;
        double xy = rotation.X * y;
        double xz = rotation.X * z;
        double yy = rotation.Y * y;
        double yz = rotation.Y * z;
        double zz = rotation.Z * z;

        return new Double4(
            (vector.X * (1.0 - yy - zz)) + (vector.Y * (xy - wz)) + (vector.Z * (xz + wy)),
            (vector.X * (xy + wz)) + (vector.Y * (1.0 - xx - zz)) + (vector.Z * (yz - wx)),
            (vector.X * (xz - wy)) + (vector.Y * (yz + wx)) + (vector.Z * (1.0 - xx - yy)),
            vector.W);
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
    public static void Transform(Double4[] source, Quaternion rotation, Double4[] destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < source.Length)
            throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

        double x = rotation.X + rotation.X;
        double y = rotation.Y + rotation.Y;
        double z = rotation.Z + rotation.Z;
        double wx = rotation.W * x;
        double wy = rotation.W * y;
        double wz = rotation.W * z;
        double xx = rotation.X * x;
        double xy = rotation.X * y;
        double xz = rotation.X * z;
        double yy = rotation.Y * y;
        double yz = rotation.Y * z;
        double zz = rotation.Z * z;

        double num1 = 1.0 - yy - zz;
        double num2 = xy - wz;
        double num3 = xz + wy;
        double num4 = xy + wz;
        double num5 = 1.0 - xx - zz;
        double num6 = yz - wx;
        double num7 = xz - wy;
        double num8 = yz + wx;
        double num9 = 1.0 - xx - yy;

        for (int i = 0; i < source.Length; ++i)
        {
            destination[i] = new Double4(
                (source[i].X * num1) + (source[i].Y * num2) + (source[i].Z * num3),
                (source[i].X * num4) + (source[i].Y * num5) + (source[i].Z * num6),
                (source[i].X * num7) + (source[i].Y * num8) + (source[i].Z * num9),
                source[i].W);
        }
    }

    /// <summary>
    /// Transforms a 4D vector by the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
    /// <returns>The transformed <see cref="Double4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Transform(Double4 vector, in Matrix4x4 transform)
    {
        return new Double4(
            (vector.X * transform.M11) + (vector.Y * transform.M21) + (vector.Z * transform.M31) + (vector.W * transform.M41),
            (vector.X * transform.M12) + (vector.Y * transform.M22) + (vector.Z * transform.M32) + (vector.W * transform.M42),
            (vector.X * transform.M13) + (vector.Y * transform.M23) + (vector.Z * transform.M33) + (vector.W * transform.M43),
            (vector.X * transform.M14) + (vector.Y * transform.M24) + (vector.Z * transform.M34) + (vector.W * transform.M44));
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
    public static void Transform(Double4[] source, in Matrix4x4 transform, Double4[] destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < source.Length)
            throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

        for (int i = 0; i < source.Length; ++i)
        {
            destination[i] = Transform(source[i], transform);
        }
    }

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator +(Double4 left, Double4 right)
    {
        return new Double4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    /// <summary>
    /// Assert a vector (return it unchanged).
    /// </summary>
    /// <param name="value">The vector to assert (unchange).</param>
    /// <returns>The asserted (unchanged) vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator +(Double4 value)
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
    public static Double4 operator -(Double4 left, Double4 right)
    {
        return new Double4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    /// <summary>
    /// Reverses the direction of a given vector.
    /// </summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A vector facing in the opposite direction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator -(Double4 value)
    {
        return new Double4(-value.X, -value.Y, -value.Z, -value.W);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <param name="value">The vector to scale.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator *(double scale, Double4 value)
    {
        return new Double4(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator *(Double4 value, double scale)
    {
        return new Double4(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale);
    }

    /// <summary>
    /// Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    /// <param name="left">The first vector to multiply.</param>
    /// <param name="right">The second vector to multiply.</param>
    /// <returns>The multiplication of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator *(Double4 left, Double4 right)
    {
        return new Double4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator /(Double4 value, double scale)
    {
        return new Double4(value.X / scale, value.Y / scale, value.Z / scale, value.W / scale);
    }

    /// <summary>
    /// Divides a numerator by a vector.
    /// </summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="value">The value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator /(double numerator, Double4 value)
    {
        return new Double4(numerator / value.X, numerator / value.Y, numerator / value.Z, numerator / value.W);
    }

    /// <summary>
    /// Divides a vector by the given vector, component-wise.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="by">The by.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator /(Double4 value, Double4 by)
    {
        return new Double4(value.X / by.X, value.Y / by.Y, value.Z / by.Z, value.W / by.W);
    }

    /// <summary>
    /// Tests for equality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Double4 left, Double4 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Tests for inequality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Double4 left, Double4 right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Double4"/> to <see cref="Vector4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Vector4(Double4 value)
    {
        return new Vector4((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="Vector4"/> to <see cref="Double4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Double4(Vector4 value)
    {
        return new Double4(value);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Double4"/> to <see cref="Half4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Half4(Double4 value)
    {
        return new Half4((Half)value.X, (Half)value.Y, (Half)value.Z, (Half)value.W);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Half4"/> to <see cref="Double4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double4(Half4 value)
    {
        return new Double4((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Double4"/> to <see cref="Double2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double2(Double4 value)
    {
        return new Double2(value.X, value.Y);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Double4"/> to <see cref="Double3"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double3(Double4 value)
    {
        return new Double3(value.X, value.Y, value.Z);
    }
    
    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public override readonly string ToString() => $"{this}";

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        var handler = new DefaultInterpolatedStringHandler(8, 4, formatProvider);
        handler.AppendLiteral("<");
        handler.AppendFormatted(X, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Y, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Z, format);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(W, format);
        handler.AppendLiteral(">");
        return handler.ToStringAndClear();
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var format1 = format.Length > 0 ? format.ToString() : null;
        var separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;
        var handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(8, 4, destination, provider, out _);
        handler.AppendLiteral("<");
        handler.AppendFormatted(X, format1);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Y, format1);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(Z, format1);
        handler.AppendLiteral(separator);
        handler.AppendLiteral(" ");
        handler.AppendFormatted(W, format1);
        handler.AppendLiteral(">");
        return destination.TryWrite(ref handler, out charsWritten);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Double4"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="Double4"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="Double4"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool Equals(Double4 other)
    {
        return (double)Math.Abs(other.X - X) < MathUtil.ZeroTolerance &&
            (double)Math.Abs(other.Y - Y) < MathUtil.ZeroTolerance &&
            (double)Math.Abs(other.Z - Z) < MathUtil.ZeroTolerance &&
            (double)Math.Abs(other.W - W) < MathUtil.ZeroTolerance;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override readonly bool Equals(object? value)
    {
        return value is Double4 d && Equals(d);
    }

    /// <summary>
    /// Deconstructs the vector's components into named variables.
    /// </summary>
    /// <param name="x">The X component</param>
    /// <param name="y">The Y component</param>
    /// <param name="z">The Z component</param>
    /// <param name="w">The W component</param>
    public readonly void Deconstruct(out double x, out double y, out double z, out double w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

#if WPFInterop
    /// <summary>
    /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Double4"/> to <see cref="System.Windows.Media.Media3D.Point4D"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator System.Windows.Media.Media3D.Point4D(Double4 value)
    {
        return new System.Windows.Media.Media3D.Point4D(value.X, value.Y, value.Z, value.W);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="System.Windows.Media.Media3D.Point4D"/> to <see cref="Stride.Core.Mathematics.Double4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double4(System.Windows.Media.Media3D.Point4D value)
    {
        return new Double4((double)value.X, (double)value.Y, (double)value.Z, (double)value.W);
    }
#endif

#if XnaInterop
    /// <summary>
    /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Double4"/> to <see cref="Microsoft.Xna.Framework.Vector4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Microsoft.Xna.Framework.Vector4(Double4 value)
    {
        return new Microsoft.Xna.Framework.Vector4(value.X, value.Y, value.Z, value.W);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Vector4"/> to <see cref="Stride.Core.Mathematics.Double4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Double4(Microsoft.Xna.Framework.Vector4 value)
    {
        return new Double4(value.X, value.Y, value.Z, value.W);
    }
#endif
}
