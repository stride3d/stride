// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

public partial struct Rational(int numerator, int denominator) : IEquatable<Rational>
{
    /// <summary>
    /// <p>Represents a rational number.</p>
    /// </summary>
    /// <remarks>
    /// <p>The <strong><see cref="SharpDX.DXGI.Rational"/></strong> structure operates under the following rules:</p><ul> <li>0/0 is legal and will be interpreted as 0/1.</li> <li>0/anything is interpreted as zero.</li> <li>If you are representing a whole number, the denominator should be 1.</li> </ul>
    /// </remarks>
    public int Numerator = numerator;

    public int Denominator = denominator;


    public static implicit operator Rational(int value)
    {
        return new Rational(value, denominator: 1);
    }

        /// <summary>
        /// <dd> <p>An unsigned integer value representing the top of the rational number.</p> </dd>
        /// </summary>

        /// <summary>
        /// <dd> <p>An unsigned integer value representing the bottom of the rational number.</p> </dd>
        /// </summary>
    public override string ToString()
    {
        return Denominator == 1
            ? Numerator.ToString()
            : string.Format("{0}/{1} = {2}", Numerator, Denominator, (float) Numerator / Denominator);
    }

    public readonly bool Equals(Rational other)
    {
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public override readonly bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        return obj is Rational rational && Equals(rational);
    }

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
