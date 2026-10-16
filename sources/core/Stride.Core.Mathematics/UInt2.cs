// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Core.Mathematics;

/// <summary>
///   Represents a two dimensional mathematical vector.
/// </summary>
[DataContract]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct UInt2 : IEquatable<UInt2>, ISpanFormattable
{
    /// <summary>
    ///   The size of the <see cref = "UInt2" /> type, in bytes.
    /// </summary>
    public static readonly int SizeInBytes = Unsafe.SizeOf<UInt2>();

    /// <summary>
    ///   A <see cref = "UInt2" /> with all of its components set to zero.
    /// </summary>
    public static readonly UInt2 Zero = new();

    /// <summary>
    ///   The X unit <see cref = "UInt2" /> (1, 0).
    /// </summary>
    public static readonly UInt2 UnitX = new(1, 0);

    /// <summary>
    ///   The Y unit <see cref = "UInt2" /> (0, 1).
    /// </summary>
    public static readonly UInt2 UnitY = new(0, 1);

    /// <summary>
    ///   A <see cref = "UInt2" /> with all of its components set to one.
    /// </summary>
    public static readonly UInt2 One = new(1, 1);

    /// <summary>
    ///   The X component of the vector.
    /// </summary>
    public uint X;

    /// <summary>
    ///   The Y component of the vector.
    /// </summary>
    public uint Y;

    /// <summary>
    ///   Initializes a new instance of the <see cref = "UInt2" /> struct.
    /// </summary>
    /// <param name = "value">The value that will be assigned to all components.</param>
    public UInt2(uint value)
    {
        X = value;
        Y = value;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref = "UInt2" /> struct.
    /// </summary>
    /// <param name = "x">Initial value for the X component of the vector.</param>
    /// <param name = "y">Initial value for the Y component of the vector.</param>
    public UInt2(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref = "UInt2" /> struct.
    /// </summary>
    /// <param name = "values">The values to assign to the X and Y components of the vector. This must be an array with two elements.</param>
    /// <exception cref = "ArgumentNullException">Thrown when <paramref name = "values" /> is <c>null</c>.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <paramref name = "values" /> contains more or less than two elements.</exception>
    public UInt2(uint[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (values.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(values), "There must be two and only two input values for UInt2.");

        X = values[0];
        Y = values[1];
    }

    /// <summary>
    ///   Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the X or Y component, depending on the index.</value>
    /// <param name = "index">The index of the component to access. Use 0 for the X component and 1 for the Y component.</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the <paramref name = "index" /> is out of the range [0, 1].</exception>
    public uint this[uint index]
    {
        get
        {
            switch (index)
            {
                case 0:
                    return X;
                case 1:
                    return Y;
            }

            throw new ArgumentOutOfRangeException(nameof(index), "Indices for UInt2 run from 0 to 1, inclusive.");
        }

        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), "Indices for UInt2 run from 0 to 1, inclusive.");
            }
        }
    }

    /// <summary>
    /// Casts from System.Numerics to Stride.Maths vectors
    /// </summary>
    /// <param name="v">Value to cast</param>
    public static explicit operator UInt2(System.Numerics.Vector2 v) => new((uint)v.X, (uint)v.Y);

    /// <summary>
    /// Casts from Stride.Maths to System.Numerics vectors
    /// </summary>
    /// <param name="v">Value to cast</param>
    public static explicit operator System.Numerics.Vector2(UInt2 v) => new(v.X, v.Y);

    /// <summary>
    ///   Creates an array containing the elements of the vector.
    /// </summary>
    /// <returns>A two-element array containing the components of the vector.</returns>
    public uint[] ToArray()
    {
        return [X, Y];
    }

    /// <summary>
    ///   Adds two vectors.
    /// </summary>
    /// <param name = "left">The first vector to add.</param>
    /// <param name = "right">The second vector to add.</param>
    /// <param name = "result">When the method completes, contains the sum of the two vectors.</param>
    public static void Add(ref readonly UInt2 left, ref readonly UInt2 right, out UInt2 result)
    {
        result = new UInt2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    ///   Adds two vectors.
    /// </summary>
    /// <param name = "left">The first vector to add.</param>
    /// <param name = "right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    public static UInt2 Add(UInt2 left, UInt2 right)
    {
        return new UInt2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    ///   Subtracts two vectors.
    /// </summary>
    /// <param name = "left">The first vector to subtract.</param>
    /// <param name = "right">The second vector to subtract.</param>
    /// <param name = "result">When the method completes, contains the difference of the two vectors.</param>
    public static void Subtract(ref readonly UInt2 left, ref readonly UInt2 right, out UInt2 result)
    {
        result = new UInt2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    ///   Subtracts two vectors.
    /// </summary>
    /// <param name = "left">The first vector to subtract.</param>
    /// <param name = "right">The second vector to subtract.</param>
    /// <returns>The difference of the two vectors.</returns>
    public static UInt2 Subtract(UInt2 left, UInt2 right)
    {
        return new UInt2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <param name = "result">When the method completes, contains the scaled vector.</param>
    public static void Multiply(ref readonly UInt2 value, uint scale, out UInt2 result)
    {
        result = new UInt2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    public static UInt2 Multiply(UInt2 value, uint scale)
    {
        return new UInt2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    ///   Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    /// <param name = "left">The first vector to modulate.</param>
    /// <param name = "right">The second vector to modulate.</param>
    /// <param name = "result">When the method completes, contains the modulated vector.</param>
    public static void Modulate(ref readonly UInt2 left, ref readonly UInt2 right, out UInt2 result)
    {
        result = new UInt2(left.X * right.X, left.Y * right.Y);
    }

    /// <summary>
    ///   Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    /// <param name = "left">The first vector to modulate.</param>
    /// <param name = "right">The second vector to modulate.</param>
    /// <returns>The modulated vector.</returns>
    public static UInt2 Modulate(UInt2 left, UInt2 right)
    {
        return new UInt2(left.X * right.X, left.Y * right.Y);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <param name = "result">When the method completes, contains the scaled vector.</param>
    public static void Divide(ref readonly UInt2 value, uint scale, out UInt2 result)
    {
        result = new UInt2(value.X / scale, value.Y / scale);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    public static UInt2 Divide(UInt2 value, uint scale)
    {
        return new UInt2(value.X / scale, value.Y / scale);
    }

    /// <summary>
    ///   Restricts a value to be within a specified range.
    /// </summary>
    /// <param name = "value">The value to clamp.</param>
    /// <param name = "min">The minimum value.</param>
    /// <param name = "max">The maximum value.</param>
    /// <param name = "result">When the method completes, contains the clamped value.</param>
    public static void Clamp(ref readonly UInt2 value, ref readonly UInt2 min, ref readonly UInt2 max, out UInt2 result)
    {
        uint x = value.X;
        x = (x > max.X) ? max.X : x;
        x = (x < min.X) ? min.X : x;

        uint y = value.Y;
        y = (y > max.Y) ? max.Y : y;
        y = (y < min.Y) ? min.Y : y;

        result = new UInt2(x, y);
    }

    /// <summary>
    ///   Restricts a value to be within a specified range.
    /// </summary>
    /// <param name = "value">The value to clamp.</param>
    /// <param name = "min">The minimum value.</param>
    /// <param name = "max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static UInt2 Clamp(UInt2 value, UInt2 min, UInt2 max)
    {
        Clamp(ref value, ref min, ref max, out var result);
        return result;
    }

    /// <summary>
    ///   Returns a vector containing the largest components of the specified vectors.
    /// </summary>
    /// <param name = "left">The first source vector.</param>
    /// <param name = "right">The second source vector.</param>
    /// <param name = "result">When the method completes, contains an new vector composed of the largest components of the source vectors.</param>
    public static void Max(ref readonly UInt2 left, ref readonly UInt2 right, out UInt2 result)
    {
        result.X = (left.X > right.X) ? left.X : right.X;
        result.Y = (left.Y > right.Y) ? left.Y : right.Y;
    }

    /// <summary>
    ///   Returns a vector containing the largest components of the specified vectors.
    /// </summary>
    /// <param name = "left">The first source vector.</param>
    /// <param name = "right">The second source vector.</param>
    /// <returns>A vector containing the largest components of the source vectors.</returns>
    public static UInt2 Max(UInt2 left, UInt2 right)
    {
        Max(ref left, ref right, out var result);
        return result;
    }

    /// <summary>
    ///   Returns a vector containing the smallest components of the specified vectors.
    /// </summary>
    /// <param name = "left">The first source vector.</param>
    /// <param name = "right">The second source vector.</param>
    /// <param name = "result">When the method completes, contains an new vector composed of the smallest components of the source vectors.</param>
    public static void Min(ref readonly UInt2 left, ref readonly UInt2 right, out UInt2 result)
    {
        result.X = (left.X < right.X) ? left.X : right.X;
        result.Y = (left.Y < right.Y) ? left.Y : right.Y;
    }

    /// <summary>
    ///   Returns a vector containing the smallest components of the specified vectors.
    /// </summary>
    /// <param name = "left">The first source vector.</param>
    /// <param name = "right">The second source vector.</param>
    /// <returns>A vector containing the smallest components of the source vectors.</returns>
    public static UInt2 Min(UInt2 left, UInt2 right)
    {
        Min(ref left, ref right, out var result);
        return result;
    }

    /// <summary>
    ///   Adds two vectors.
    /// </summary>
    /// <param name = "left">The first vector to add.</param>
    /// <param name = "right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    public static UInt2 operator +(UInt2 left, UInt2 right)
    {
        return new UInt2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    ///   Assert a vector (return it unchanged).
    /// </summary>
    /// <param name = "value">The vector to assert (unchange).</param>
    /// <returns>The asserted (unchanged) vector.</returns>
    public static UInt2 operator +(UInt2 value)
    {
        return value;
    }

    /// <summary>
    ///   Subtracts two vectors.
    /// </summary>
    /// <param name = "left">The first vector to subtract.</param>
    /// <param name = "right">The second vector to subtract.</param>
    /// <returns>The difference of the two vectors.</returns>
    public static UInt2 operator -(UInt2 left, UInt2 right)
    {
        return new UInt2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    public static UInt2 operator *(uint scale, UInt2 value)
    {
        return new UInt2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    public static UInt2 operator *(UInt2 value, uint scale)
    {
        return new UInt2(value.X * scale, value.Y * scale);
    }

    /// <summary>
    ///   Scales a vector by the given value.
    /// </summary>
    /// <param name = "value">The vector to scale.</param>
    /// <param name = "scale">The amount by which to scale the vector.</param>
    /// <returns>The scaled vector.</returns>
    public static UInt2 operator /(UInt2 value, uint scale)
    {
        return new UInt2(value.X / scale, value.Y / scale);
    }

    /// <summary>
    ///   Tests for equality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name = "left" /> has the same value as <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator ==(UInt2 left, UInt2 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///   Tests for inequality between two objects.
    /// </summary>
    /// <param name = "left">The first value to compare.</param>
    /// <param name = "right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name = "left" /> has a different value than <paramref name = "right" />; otherwise, <c>false</c>.</returns>
    public static bool operator !=(UInt2 left, UInt2 right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///   Performs an explicit conversion from <see cref = "UInt2" /> to <see cref = "Vector2" />.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static explicit operator Vector2(UInt2 value)
    {
        return new Vector2(value.X, value.Y);
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
    ///   Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///   A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    ///   Determines whether the specified <see cref = "UInt2" /> is equal to this instance.
    /// </summary>
    /// <param name = "other">The <see cref = "UInt2" /> to compare with this instance.</param>
    /// <returns>
    ///   <c>true</c> if the specified <see cref = "UInt2" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool Equals(UInt2 other)
    {
        return other.X == X && other.Y == Y;
    }

    /// <summary>
    ///   Determines whether the specified <see cref = "object" /> is equal to this instance.
    /// </summary>
    /// <param name = "value">The <see cref = "object" /> to compare with this instance.</param>
    /// <returns>
    ///   <c>true</c> if the specified <see cref = "object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override readonly bool Equals(object? value)
    {
        return value is UInt2 u && Equals(u);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="uint"/> array to <see cref="Stride.Core.Mathematics.UInt2"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator UInt2(uint[] input)
    {
        return new UInt2(input);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="Stride.Core.Mathematics.UInt2"/> to <see cref="uint"/> array.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator uint[](UInt2 input)
    {
        return input.ToArray();
    }

    /// <summary>
    /// Deconstructs the vector's components into named variables.
    /// </summary>
    /// <param name="x">The X component</param>
    /// <param name="y">The Y component</param>
    public readonly void Deconstruct(out uint x, out uint y)
    {
        x = X;
        y = Y;
    }
}
