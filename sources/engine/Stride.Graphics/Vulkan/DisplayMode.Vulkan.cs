// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN

using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    public partial class DisplayMode
    {
        //internal ModeDescription ToDescription()
        //{
        //    return new ModeDescription(Width, Height, RefreshRate.ToSharpDX(), format: (SharpDX.DXGI.Format)Format);
        //}

        //internal static DisplayMode FromDescription(ModeDescription description)
        //{
        //    return new DisplayMode((PixelFormat)description.Format, description.Width, description.Height, new Rational(description.RefreshRate.Numerator, description.RefreshRate.Denominator));
        //}
    }
}
#endif
