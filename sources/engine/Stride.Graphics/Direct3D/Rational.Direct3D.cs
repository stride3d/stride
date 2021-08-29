// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D 

using System;

namespace Stride.Graphics
{
    public partial struct Rational
    {
        /// <summary>
        /// Converts from SharpDX representation.
        /// </summary>
        /// <param name="rational">The rational.</param>
        /// <returns>Rational.</returns>
        internal static Rational FromSilk(Silk.NET.DXGI.Rational rational)
        {
            return new Rational((int)rational.Numerator, (int)rational.Denominator);
        }

        /// <summary>
        /// Converts to SharpDX representation.
        /// </summary>
        /// <returns>SharpDX.DXGI.Rational.</returns>
        internal Silk.NET.DXGI.Rational ToSilk()
        {
            return new Silk.NET.DXGI.Rational((uint)Numerator, (uint)Denominator);
        }
    }
}
#endif
