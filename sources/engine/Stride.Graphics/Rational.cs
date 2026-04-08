// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Represents a rational number.
/// </summary>
/// <param name="numerator">The numerator (top) of the rational number.</param>
/// <param name="denominator">The denominator (bottom) of the rational number.</param>
/// <remarks>
///   The <strong><see cref="Rational"/></strong> structure operates under the following rules:
///   <list type="bullet">
///     <item>0/0 is legal and will be interpreted as 0/1.</item>
///     <item>0/anything is interpreted as zero.</item>
///     <item>If you are representing a whole number, the denominator should be 1.</item>
///   </list>
/// </remarks>
public partial struct Rational(int numerator, int denominator) : IEquatable<Rational>
{
    /// <summary>
    ///   An value representing the top of the rational number.
    /// </summary>
    public int Numerator = numerator;

    /// <summary>
    ///   An value representing the bottom of the rational number.
    /// </summary>
    public int Denominator = denominator;


    /// <summary>
    ///   Returns a new <see cref="Rational"/> from an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The integer <paramref name="value"/> as a rational number with denominator 1.</returns>
    public static implicit operator Rational(int value)
    {
        return new Rational(value, denominator: 1);
    }


    /// <summary>
    ///   Returns the string representation of this <see cref="Rational"/>, including its approximate real value.
    /// </summary>
    /// <returns>The string representation of this <see cref="Rational"/>.</returns>
    public override string ToString()
    {
        return Denominator == 1
            ? Numerator.ToString()
            : string.Format("{0}/{1} = {2}", Numerator, Denominator, (float) Numerator / Denominator);
    }

    /// <inheritdoc/>
    public readonly bool Equals(Rational other)
    {
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        return obj is Rational rational && Equals(rational);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    public static bool operator ==(Rational left, Rational right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rational left, Rational right)
    {
        return !left.Equals(right);
    }
}
