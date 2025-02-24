// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN

using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    public partial record struct DisplayMode
    {
        ///// <summary>
        /////   Gets a Vulkan <see cref="ModeDescription"/> from the display mode.
        ///// </summary>
        ///// <returns>Returns a <see cref="ModeDescription"/>.</returns>
        //internal ModeDescription ToDescription()
        //{
        //    return new ModeDescription(Width, Height, RefreshRate.ToSharpDX(), (SharpDX.DXGI.Format) Format);
        //}

        ///// <summary>
        /////   Gets a <see cref="DisplayMode"/> from a Vulkan <see cref="ModeDescription"/> structure.
        ///// </summary>
        ///// <param name="description">The Vulkan <see cref="ModeDescription"/> structure.</param>
        ///// <returns>A corresponding <see cref="DisplayMode"/>.</returns>
        //internal static DisplayMode FromDescription(ModeDescription description)
        //{
        //    return new DisplayMode((PixelFormat) description.Format, description.Width, description.Height, new Rational(description.RefreshRate.Numerator, description.RefreshRate.Denominator));
        //}
    }
}

#endif
