// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D

using Silk.NET.DXGI;

namespace Stride.Graphics
{
    public partial class DisplayMode
    {
        internal ModeDesc ToDescription()
        {
            return new ModeDesc((uint)Width, (uint)Height, RefreshRate.ToSilk(), format: (Format)Format);
        }

        internal static DisplayMode FromDescription(ModeDesc description)
        {
            return new DisplayMode((PixelFormat)description.Format, (int)description.Width, (int)description.Height, new Rational((int)description.RefreshRate.Numerator, (int)description.RefreshRate.Denominator));
        }
    }
}
#endif
