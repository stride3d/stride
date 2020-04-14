// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics
{
    /// <summary>
    /// <p>Represents a rational number.</p>
    /// </summary>
    /// <remarks>
    /// <p>The <strong><see cref="SharpDX.DXGI.Rational"/></strong> structure operates under the following rules:</p><ul> <li>0/0 is legal and will be interpreted as 0/1.</li> <li>0/anything is interpreted as zero.</li> <li>If you are representing a whole number, the denominator should be 1.</li> </ul>
    /// </remarks>
    public partial struct Rational : IEquatable<Rational>
    {
        public Rational(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// <dd> <p>An unsigned integer value representing the top of the rational number.</p> </dd>
        /// </summary>
        public int Numerator;

        /// <summary>
        /// <dd> <p>An unsigned integer value representing the bottom of the rational number.</p> </dd>
        /// </summary>
        public int Denominator;

        public override string ToString()
        {
            return string.Format("{0}/{1} = {2}", Numerator, Denominator, (float)Numerator / Denominator);
        }

        public bool Equals(Rational other)
        {
            return Numerator == other.Numerator && Denominator == other.Denominator;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Rational && Equals((Rational)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Numerator * 397) ^ Denominator;
            }
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
}
