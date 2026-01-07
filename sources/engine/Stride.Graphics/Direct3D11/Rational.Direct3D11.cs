// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

namespace Stride.Graphics
{
    public partial struct Rational
    {
        /// <summary>
        ///   Converts from a Silk.NET <see cref="Silk.NET.DXGI.Rational"/> representation.
        /// </summary>
        /// <param name="rational">The rational to convert.</param>
        /// <returns>The converted <see cref="Rational"/>.</returns>
        internal static Rational FromSilk(Silk.NET.DXGI.Rational rational)
        {
            return new Rational((int) rational.Numerator, (int) rational.Denominator);
        }

        /// <summary>
        ///   Converts to a Silk.NET <see cref="Silk.NET.DXGI.Rational"/> representation.
        /// </summary>
        /// <returns>The converted <see cref="Silk.NET.DXGI.Rational"/>.</returns>
        internal readonly Silk.NET.DXGI.Rational ToSilk()
        {
            return new Silk.NET.DXGI.Rational((uint) Numerator, (uint) Denominator);
        }
    }
}

#endif
