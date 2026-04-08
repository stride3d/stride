// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using Silk.NET.DXGI;

namespace Stride.Graphics;

public partial record struct DisplayMode
{
    /// <summary>
    ///   Gets a DXGI <see cref="ModeDesc"/> from the display mode.
    /// </summary>
    /// <returns>Returns a <see cref="ModeDesc"/>.</returns>
    internal ModeDesc ToDescription()
    {
        return new ModeDesc((uint) Width, (uint) Height, RefreshRate.ToSilk(), format: (Format) Format);
    }

    /// <summary>
    ///   Gets a DXGI <see cref="ModeDesc1"/> from the display mode.
    /// </summary>
    /// <returns>Returns a <see cref="ModeDesc1"/>.</returns>
    internal ModeDesc1 ToDescription1()
    {
        return new ModeDesc1((uint) Width, (uint) Height, RefreshRate.ToSilk(), format: (Format) Format);
    }

    /// <summary>
    ///   Gets a <see cref="DisplayMode"/> from a DXGI <see cref="ModeDesc"/> structure.
    /// </summary>
    /// <param name="description">The DXGI <see cref="ModeDesc"/> structure.</param>
    /// <returns>A corresponding <see cref="DisplayMode"/>.</returns>
    internal static DisplayMode FromDescription(ref readonly ModeDesc description)
    {
        return new DisplayMode((PixelFormat) description.Format,
                               (int) description.Width,
                               (int) description.Height,
                               new Rational((int) description.RefreshRate.Numerator, (int) description.RefreshRate.Denominator));
    }

    /// <summary>
    ///   Gets a <see cref="DisplayMode"/> from a DXGI <see cref="ModeDesc1"/> structure.
    /// </summary>
    /// <param name="description">The DXGI <see cref="ModeDesc1"/> structure.</param>
    /// <returns>A corresponding <see cref="DisplayMode"/>.</returns>
    internal static DisplayMode FromDescription(ref readonly ModeDesc1 description)
    {
        return new DisplayMode((PixelFormat) description.Format,
                               (int) description.Width,
                               (int) description.Height,
                               new Rational((int) description.RefreshRate.Numerator, (int) description.RefreshRate.Denominator));
    }
}

#endif
