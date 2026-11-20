// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Stride.Core.Mathematics;

/// <summary>
/// Represents a two dimensional mathematical vector with double-precision floats.
/// </summary>
[DataContract("double2")]
[DataStyle(DataStyle.Compact)]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Double2 : IEquatable<Double2>, ISpanFormattable
{
    /// <summary>
    /// The size of the <see cref="Double2"/> type, in bytes.
    /// </summary>
    public static readonly int SizeInBytes = Unsafe.SizeOf<Double2>();

    /// <summary>
    /// A <see cref="Double2"/> with all of its components set to zero.
    /// </summary>
    public static readonly Double2 Zero = new();

    /// <summary>
    /// The X unit <see cref="Double2"/> (1, 0).
    /// </summary>
    public static readonly Double2 UnitX = new(1.0, 0.0);

    /// <summary>
    /// The Y unit <see cref="Double2"/> (0, 1).
    /// </summary>
    public static readonly Double2 UnitY = new(0.0, 1.0);

    /// <summary>
    /// A <see cref="Double2"/> with all of its components set to one.
    /// </summary>
    public static readonly Double2 One = new(1.0, 1.0);

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
    /// Initializes a new instance of the <see cref="Double2"/> struct.
    /// </summary>
    /// <param name="value">The value that will be assigned to all components.</param>
    public Double2(double value)
    {
        X = value;
        Y = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double2"/> struct.
    /// </summary>
    /// <param name="x">Initial value for the X component of the vector.</param>
    /// <param name="y">Initial value for the Y component of the vector.</param>
    public Double2(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double2"/> struct.
    /// </summary>
    /// <param name="values">The values to assign to the X and Y components of the vector. This must be an array with two elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than two elements.</exception>
    public Double2(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(values), "There must be two and only two input values for Double2.");

        X = values[0];
        Y = values[1];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double2"/> struct.
    /// </summary>
    /// <param name="v">The Vector2 to construct the Double2 from.</param>
    public Double2(Vector2 v)
    {
        X = v.X;
        Y = v.Y;
    }

    /// <summary>
    /// Gets a value indicting whether this instance is normalized.
    /// </summary>
    public readonly bool IsNormalized
    {
        get { return Math.Abs((X * X) + (Y * Y) - 1f) < MathUtil.ZeroTolerance; }
    }

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the X or Y component, depending on the index.</value>
    /// <param name="index">The index of the component to access. Use 0 for the X component and 1 for the Y component.</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 1].</exception>
    public double this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => X,
                1 => Y,
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Indices for Double2 run from 0 to 1, inclusive."),
            };
        }

        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), "Indices for Double2 run from 0 to 1, inclusive.");
            }
        }
    }

    /// <summary>
    /// Calculates the length of the vector.
    /// </summary>
    /// <returns>The length of the vector.</returns>
    /// <remarks>
    /// <see cref="Stride.Core.Mathematics.Double2.LengthSquared"/> may be preferred when only the relative length is needed
    /// and speed is of the essence.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double Length()
    {
        return (double)Math.Sqrt((X * X) + (Y * Y));
    }

    /// <summary>
    /// Calculates the squared length of the vector.
    /// </summary>
    /// <returns>The squared length of the vector.</returns>
    /// <remarks>
    /// This method may be preferred to <see cref="Stride.Core.Mathematics.Double2.Length"/> when only a relative length is needed
    /// and speed is of the essence.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double LengthSquared()
    {
        return (X * X) + (Y * Y);
    }

    /// <summary>
    /// Creates an array containing the elements of the vector.
    /// </summary>
    /// <returns>A two-element array containing the components of the vector.</returns>
    public readonly double[] ToArray()
    {
        return [X, Y];
    }

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Add(Double2 left, Double2 right)
    {
        return new Double2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// Subtracts two vectors.
    /// </summary>
    /// <param name="left">The first vector to subtract.</param>
    /// <param name="right">The second vector to subtract.</param>
    /// <returns>The difference of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Subtract(Double2 left, Double2 right)
    {
        return new Double2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Multiply(Double2 value, double scale)
    {
        return new Double2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    /// Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    /// <param name="left">The first vector to modulate.</param>
    /// <param name="right">The second vector to modulate.</param>
    /// <returns>The modulated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Multiply(Double2 left, Double2 right)
    {
        return new Double2(left.X * right.X, left.Y * right.Y);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Divide(Double2 value, double scale)
    {
        return new Double2(value.X / scale, value.Y / scale);
    }

    /// <summary>
    /// Demodulates a vector with another by performing component-wise division.
    /// </summary>
    /// <param name="left">The first vector to demodulate.</param>
    /// <param name="right">The second vector to demodulate.</param>
    /// <returns>The demodulated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Divide(Double2 left, Double2 right)
    {
        return new Double2(left.X / right.X, left.Y / right.Y);
    }

    /// <summary>
    /// Reverses the direction of a given vector.
    /// </summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A vector facing in the opposite direction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Negate(Double2 value)
    {
        return new Double2(-value.X, -value.Y);
    }

    /// <summary>
    /// Returns a <see cref="Double2"/> containing the 2D Cartesian coordinates of a point specified in Barycentric coordinates relative to a 2D triangle.
    /// </summary>
    /// <param name="value1">A <see cref="Double2"/> containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param>
    /// <param name="value2">A <see cref="Double2"/> containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param>
    /// <param name="value3">A <see cref="Double2"/> containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param>
    /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in <paramref name="value2"/>).</param>
    /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in <paramref name="value3"/>).</param>
    /// <returns>A new <see cref="Double2"/> containing the 2D Cartesian coordinates of the specified point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Barycentric(Double2 value1, Double2 value2, Double2 value3, double amount1, double amount2)
    {
        return new Double2(value1.X + (amount1 * (value2.X - value1.X)) + (amount2 * (value3.X - value1.X)),
            value1.Y + (amount1 * (value2.Y - value1.Y)) + (amount2 * (value3.Y - value1.Y)));
    }

    /// <summary>
    /// Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Clamp(Double2 value, Double2 min, Double2 max)
    {
        double x = value.X;
        x = (x > max.X) ? max.X : x;
        x = (x < min.X) ? min.X : x;

        double y = value.Y;
        y = (y > max.Y) ? max.Y : y;
        y = (y < min.Y) ? min.Y : y;

        return new Double2(x, y);
    }

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    /// <param name="value1">The first vector.</param>
    /// <param name="value2">The second vector.</param>
    /// <returns>The distance between the two vectors.</returns>
    /// <remarks>
    /// <see cref="DistanceSquared(Double2, Double2)"/> may be preferred when only the relative distance is needed
    /// and speed is of the essence.
    /// </remarks>
    public static double Distance(Double2 value1, Double2 value2)
    {
        double x = value1.X - value2.X;
        double y = value1.Y - value2.Y;

        return (double)Math.Sqrt((x * x) + (y * y));
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
    public static double DistanceSquared(Double2 value1, Double2 value2)
    {
        double x = value1.X - value2.X;
        double y = value1.Y - value2.Y;

        return (x * x) + (y * y);
    }

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="left">First source vector.</param>
    /// <param name="right">Second source vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(Double2 left, Double2 right)
    {
        return (left.X * right.X) + (left.Y * right.Y);
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
    public static Double2 Lerp(Double2 start, Double2 end, double amount)
    {
        Double2 result;
        result.X = start.X + ((end.X - start.X) * amount);
        result.Y = start.Y + ((end.Y - start.Y) * amount);
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
    public static Double2 SmoothStep(Double2 start, Double2 end, double amount)
    {
        Double2 result;
        amount = (amount > 1.0) ? 1.0 : ((amount < 0.0) ? 0.0 : amount);
        amount = amount * amount * (3.0f - (2.0f * amount));

        result.X = start.X + ((end.X - start.X) * amount);
        result.Y = start.Y + ((end.Y - start.Y) * amount);
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
    public static Double2 Hermite(Double2 value1, Double2 tangent1, Double2 value2, Double2 tangent2, double amount)
    {
        Double2 result;
        double squared = amount * amount;
        double cubed = amount * squared;
        double part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0;
        double part2 = (-2.0f * cubed) + (3.0f * squared);
        double part3 = (cubed - (2.0f * squared)) + amount;
        double part4 = cubed - squared;

        result.X = (value1.X * part1) + (value2.X * part2) + (tangent1.X * part3) + (tangent2.X * part4);
        result.Y = (value1.Y * part1) + (value2.Y * part2) + (tangent1.Y * part3) + (tangent2.Y * part4);
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
    /// <returns>A vector that is the result of the Catmull-Rom interpolation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 CatmullRom(Double2 value1, Double2 value2, Double2 value3, Double2 value4, double amount)
    {
        Double2 result;
        double squared = amount * amount;
        double cubed = amount * squared;

        result.X = 0.5f * ((2.0f * value2.X) + ((-value1.X + value3.X) * amount) +
        (((2.0f * value1.X) - (5.0f * value2.X) + (4.0f * value3.X) - value4.X) * squared) +
        ((-value1.X + (3.0f * value2.X) - (3.0f * value3.X) + value4.X) * cubed));

        result.Y = 0.5f * ((2.0f * value2.Y) + ((-value1.Y + value3.Y) * amount) +
            (((2.0f * value1.Y) - (5.0f * value2.Y) + (4.0f * value3.Y) - value4.Y) * squared) +
            ((-value1.Y + (3.0f * value2.Y) - (3.0f * value3.Y) + value4.Y) * cubed));
        return result;
    }

    /// <summary>
    /// Returns a vector containing the largest components of the specified vectors.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>A vector containing the largest components of the source vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Max(Double2 left, Double2 right)
    {
        Double2 result;
        result.X = (left.X > right.X) ? left.X : right.X;
        result.Y = (left.Y > right.Y) ? left.Y : right.Y;
        return result;
    }

    /// <summary>
    /// Returns a vector containing the smallest components of the specified vectors.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>A vector containing the smallest components of the source vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Min(Double2 left, Double2 right)
    {
        Double2 result;
        result.X = (left.X < right.X) ? left.X : right.X;
        result.Y = (left.Y < right.Y) ? left.Y : right.Y;
        return result;
    }

    /// <summary>
    /// Returns the reflection of a vector off a surface that has the specified normal.
    /// </summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="normal">Normal of the surface.</param>
    /// <returns>The reflected vector.</returns>
    /// <remarks>Reflect only gives the direction of a reflection off a surface, it does not determine
    /// whether the original vector was close enough to the surface to hit it.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Reflect(Double2 vector, Double2 normal)
    {
        Double2 result;
        double dot = (vector.X * normal.X) + (vector.Y * normal.Y);

        result.X = vector.X - (2.0f * dot * normal.X);
        result.Y = vector.Y - (2.0f * dot * normal.Y);
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
    public static void Orthogonalize(Double2[] destination, params Double2[] source)
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
            Double2 newvector = source[i];

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
    public static void Orthonormalize(Double2[] destination, params Double2[] source)
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
            Double2 newvector = source[i];

            for (int r = 0; r < i; ++r)
            {
                newvector -= Dot(destination[r], newvector) * destination[r];
            }

            newvector = Double2.NormalizeSafe(newvector);
            destination[i] = newvector;
        }
    }

    /// <summary>
    /// Transforms a 2D vector by the given <see cref="Quaternion"/> rotation.
    /// </summary>
    /// <param name="vector">The vector to rotate.</param>
    /// <param name="rotation">The <see cref="Quaternion"/> rotation to apply.</param>
    /// <returns>The transformed <see cref="Double4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Transform(Double2 vector, Quaternion rotation)
    {
        double x = rotation.X + rotation.X;
        double y = rotation.Y + rotation.Y;
        double z = rotation.Z + rotation.Z;
        double wz = rotation.W * z;
        double xx = rotation.X * x;
        double xy = rotation.X * y;
        double yy = rotation.Y * y;
        double zz = rotation.Z * z;

        return new Double2((vector.X * (1.0 - yy - zz)) + (vector.Y * (xy - wz)), (vector.X * (xy + wz)) + (vector.Y * (1.0 - xx - zz)));
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
    public static void Transform(Double2[] source, Quaternion rotation, Double2[] destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < source.Length)
            throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

        double x = rotation.X + rotation.X;
        double y = rotation.Y + rotation.Y;
        double z = rotation.Z + rotation.Z;
        double wz = rotation.W * z;
        double xx = rotation.X * x;
        double xy = rotation.X * y;
        double yy = rotation.Y * y;
        double zz = rotation.Z * z;

        double num1 = 1.0 - yy - zz;
        double num2 = xy - wz;
        double num3 = xy + wz;
        double num4 = 1.0 - xx - zz;

        for (int i = 0; i < source.Length; ++i)
        {
            destination[i] = new Double2(
                (source[i].X * num1) + (source[i].Y * num2),
                (source[i].X * num3) + (source[i].Y * num4));
        }
    }

    /// <summary>
    /// Transforms a 2D vector by the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="vector">The source vector.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
    /// <returns>The transformed <see cref="Double4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Transform(Double2 vector, in Matrix4x4 transform)
    {
        return new Double4(
            (vector.X * transform.M11) + (vector.Y * transform.M21) + transform.M41,
            (vector.X * transform.M12) + (vector.Y * transform.M22) + transform.M42,
            (vector.X * transform.M13) + (vector.Y * transform.M23) + transform.M43,
            (vector.X * transform.M14) + (vector.Y * transform.M24) + transform.M44);
    }

    /// <summary>
    /// Transforms an array of 2D vectors by the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="source">The array of vectors to transform.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
    /// <param name="destination">The array for which the transformed vectors are stored.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destination"/> is shorter in length than <paramref name="source"/>.</exception>
    public static void Transform(Double2[] source, in Matrix4x4 transform, Double4[] destination)
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
    /// Performs a coordinate transformation using the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="coordinate">The coordinate vector to transform.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
    /// <returns>The transformed coordinates.</returns>
    /// <remarks>
    /// A coordinate transform performs the transformation with the assumption that the w component
    /// is one. The four dimensional vector obtained from the transformation operation has each
    /// component in the vector divided by the w component. This forces the wcomponent to be one and
    /// therefore makes the vector homogeneous. The homogeneous vector is often prefered when working
    /// with coordinates as the w component can safely be ignored.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 TransformCoordinate(Double2 coordinate, in Matrix4x4 transform)
    {
        Double4 vector = new Double4
        {
            X = (coordinate.X * transform.M11) + (coordinate.Y * transform.M21) + transform.M41,
            Y = (coordinate.X * transform.M12) + (coordinate.Y * transform.M22) + transform.M42,
            Z = (coordinate.X * transform.M13) + (coordinate.Y * transform.M23) + transform.M43,
            W = 1f / ((coordinate.X * transform.M14) + (coordinate.Y * transform.M24) + transform.M44)
        };

        return new Double2(vector.X * vector.W, vector.Y * vector.W);
    }

    /// <summary>
    /// Performs a coordinate transformation on an array of vectors using the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="source">The array of coordinate vectors to trasnform.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
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
    public static void TransformCoordinate(Double2[] source, in Matrix4x4 transform, Double2[] destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < source.Length)
            throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

        for (int i = 0; i < source.Length; ++i)
        {
            destination[i] = TransformCoordinate(source[i], transform);
        }
    }

    /// <summary>
    /// Performs a normal transformation using the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="normal">The normal vector to transform.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
    /// <returns>The transformed normal.</returns>
    /// <remarks>
    /// A normal transform performs the transformation with the assumption that the w component
    /// is zero. This causes the fourth row and fourth collumn of the matrix to be unused. The
    /// end result is a vector that is not translated, but all other transformation properties
    /// apply. This is often prefered for normal vectors as normals purely represent direction
    /// rather than location because normal vectors should not be translated.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 TransformNormal(Double2 normal, in Matrix4x4 transform)
    {
        return new Double2(
            (normal.X * transform.M11) + (normal.Y * transform.M21),
            (normal.X * transform.M12) + (normal.Y * transform.M22));
    }

    /// <summary>
    /// Performs a normal transformation on an array of vectors using the given <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="source">The array of normal vectors to transform.</param>
    /// <param name="transform">The transformation <see cref="Matrix4x4"/>.</param>
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
    public static void TransformNormal(Double2[] source, in Matrix4x4 transform, Double2[] destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < source.Length)
            throw new ArgumentOutOfRangeException(nameof(destination), "The destination array must be of same length or larger length than the source array.");

        for (int i = 0; i < source.Length; ++i)
        {
            destination[i] = TransformNormal(source[i], transform);
        }
    }

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator +(Double2 left, Double2 right)
    {
        return new Double2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// Assert a vector (return it unchanged).
    /// </summary>
    /// <param name="value">The vector to assert (unchange).</param>
    /// <returns>The asserted (unchanged) vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator +(Double2 value)
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
    public static Double2 operator -(Double2 left, Double2 right)
    {
        return new Double2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    /// Reverses the direction of a given vector.
    /// </summary>
    /// <param name="value">The vector to negate.</param>
    /// <returns>A vector facing in the opposite direction.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator -(Double2 value)
    {
        return new Double2(-value.X, -value.Y);
    }

    /// <summary>
    /// Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    /// <param name="left">The first vector to multiply.</param>
    /// <param name="right">The second vector to multiply.</param>
    /// <returns>The multiplication of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator *(Double2 left, Double2 right)
    {
        return new Double2(left.X * right.X, left.Y * right.Y);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <param name="value">The vector to scale.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator *(double scale, Double2 value)
    {
        return new Double2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator *(Double2 value, double scale)
    {
        return new Double2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator /(Double2 value, double scale)
    {
        return new Double2(value.X / scale, value.Y / scale);
    }

    /// <summary>
    /// Divides a numerator by a vector.
    /// </summary>
    /// <param name="numerator">The numerator.</param>
    /// <param name="value">The value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator /(double numerator, Double2 value)
    {
        return new Double2(numerator / value.X, numerator / value.Y);
    }

    /// <summary>
    /// Divides a vector by the given vector, component-wise.
    /// </summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="by">The by.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator /(Double2 value, Double2 by)
    {
        return new Double2(value.X / by.X, value.Y / by.Y);
    }

    /// <summary>
    /// Tests for equality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Double2 left, Double2 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Tests for inequality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Double2 left, Double2 right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Stride.Core.Mathematics.Double2"/> to <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Vector2(Double2 value)
    {
        return new Vector2((float)value.X, (float)value.Y);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="System.Numerics.Vector2"/> to <see cref="Stride.Core.Mathematics.Double2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Double2(Vector2 value)
    {
        return new Double2(value);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Double2"/> to <see cref="Half2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Half2(Double2 value)
    {
        return new Half2((Half)value.X, (Half)value.Y);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Half2"/> to <see cref="Double2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double2(Half2 value)
    {
        return new Double2((float)value.X, (float)value.Y);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Stride.Core.Mathematics.Double2"/> to <see cref="Stride.Core.Mathematics.Double3"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double3(Double2 value)
    {
        return new Double3(value, 0.0);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Stride.Core.Mathematics.Double2"/> to <see cref="Stride.Core.Mathematics.Double4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double4(Double2 value)
    {
        return new Double4(value, 0.0, 0.0);
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
        var handler = new DefaultInterpolatedStringHandler(5, 2, formatProvider);
        handler.AppendLiteral("X:");
        handler.AppendFormatted(X, format);
        handler.AppendLiteral(" Y:");
        handler.AppendFormatted(Y, format);
        return handler.ToStringAndClear();
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var format1 = format.Length > 0 ? format.ToString() : null;
        var handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(5, 2, destination, provider, out _);
        handler.AppendLiteral("X:");
        handler.AppendFormatted(X, format1);
        handler.AppendLiteral(" Y:");
        handler.AppendFormatted(Y, format1);
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
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Stride.Core.Mathematics.Double2"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="Stride.Core.Mathematics.Double2"/> to compare with this instance.</param>
    /// <returns>
    /// 	<c>true</c> if the specified <see cref="Stride.Core.Mathematics.Double2"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool Equals(Double2 other)
    {
        return (double)Math.Abs(other.X - X) < MathUtil.ZeroTolerance &&
            (double)Math.Abs(other.Y - Y) < MathUtil.ZeroTolerance;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>
    /// 	<c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override readonly bool Equals(object? value)
    {
        return value is Double2 d && Equals(d);
    }

    /// <summary>
    /// Deconstructs the vector's components into named variables.
    /// </summary>
    /// <param name="x">The X component</param>
    /// <param name="y">The Y component</param>
    public readonly void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

#if WPFInterop
    /// <summary>
    /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Double2"/> to <see cref="System.Windows.Point"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator System.Windows.Point(Double2 value)
    {
        return new System.Windows.Point(value.X, value.Y);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="System.Windows.Point"/> to <see cref="Stride.Core.Mathematics.Double2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Double2(System.Windows.Point value)
    {
        return new Double2(value.X, value.Y);
    }
#endif

#if XnaInterop
    /// <summary>
    /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.Double2"/> to <see cref="Microsoft.Xna.Framework.Vector2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Microsoft.Xna.Framework.Vector2(Double2 value)
    {
        return new Microsoft.Xna.Framework.Vector2(value.X, value.Y);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="Microsoft.Xna.Framework.Vector2"/> to <see cref="Stride.Core.Mathematics.Double2"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator Double2(Microsoft.Xna.Framework.Vector2 value)
    {
        return new Double2(value.X, value.Y);
    }
#endif
}
