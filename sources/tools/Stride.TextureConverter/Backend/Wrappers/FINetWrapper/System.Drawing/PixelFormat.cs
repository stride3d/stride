// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace FreeImageAPI;

//
// Summary:
//     Specifies the format of the color data for each pixel in the image.
public enum PixelFormat
{
    //
    // Summary:
    //     The pixel format is undefined.
    Undefined = 0,
    //
    // Summary:
    //     No pixel format is specified.
    DontCare = 0,
    //
    // Summary:
    //     The maximum value for this enumeration.
    Max = 15,
    //
    // Summary:
    //     The pixel data contains color-indexed values, which means the values are an index
    //     to colors in the system color table, as opposed to individual color values.
    Indexed = 65536,
    //
    // Summary:
    //     The pixel data contains GDI colors.
    Gdi = 131072,
    //
    // Summary:
    //     Specifies that the format is 16 bits per pixel; 5 bits each are used for the
    //     red, green, and blue components. The remaining bit is not used.
    Format16bppRgb555 = 135173,
    //
    // Summary:
    //     Specifies that the format is 16 bits per pixel; 5 bits are used for the red component,
    //     6 bits are used for the green component, and 5 bits are used for the blue component.
    Format16bppRgb565 = 135174,
    //
    // Summary:
    //     Specifies that the format is 24 bits per pixel; 8 bits each are used for the
    //     red, green, and blue components.
    Format24bppRgb = 137224,
    //
    // Summary:
    //     Specifies that the format is 32 bits per pixel; 8 bits each are used for the
    //     red, green, and blue components. The remaining 8 bits are not used.
    Format32bppRgb = 139273,
    //
    // Summary:
    //     Specifies that the pixel format is 1 bit per pixel and that it uses indexed color.
    //     The color table therefore has two colors in it.
    Format1bppIndexed = 196865,
    //
    // Summary:
    //     Specifies that the format is 4 bits per pixel, indexed.
    Format4bppIndexed = 197634,
    //
    // Summary:
    //     Specifies that the format is 8 bits per pixel, indexed. The color table therefore
    //     has 256 colors in it.
    Format8bppIndexed = 198659,
    //
    // Summary:
    //     The pixel data contains alpha values that are not premultiplied.
    Alpha = 262144,
    //
    // Summary:
    //     The pixel format is 16 bits per pixel. The color information specifies 32,768
    //     shades of color, of which 5 bits are red, 5 bits are green, 5 bits are blue,
    //     and 1 bit is alpha.
    Format16bppArgb1555 = 397319,
    //
    // Summary:
    //     The pixel format contains premultiplied alpha values.
    PAlpha = 524288,
    //
    // Summary:
    //     Specifies that the format is 32 bits per pixel; 8 bits each are used for the
    //     alpha, red, green, and blue components. The red, green, and blue components are
    //     premultiplied, according to the alpha component.
    Format32bppPArgb = 925707,
    //
    // Summary:
    //     Reserved.
    Extended = 1048576,
    //
    // Summary:
    //     The pixel format is 16 bits per pixel. The color information specifies 65536
    //     shades of gray.
    Format16bppGrayScale = 1052676,
    //
    // Summary:
    //     Specifies that the format is 48 bits per pixel; 16 bits each are used for the
    //     red, green, and blue components.
    Format48bppRgb = 1060876,
    //
    // Summary:
    //     Specifies that the format is 64 bits per pixel; 16 bits each are used for the
    //     alpha, red, green, and blue components. The red, green, and blue components are
    //     premultiplied according to the alpha component.
    Format64bppPArgb = 1851406,
    //
    // Summary:
    //     The default pixel format of 32 bits per pixel. The format specifies 24-bit color
    //     depth and an 8-bit alpha channel.
    Canonical = 2097152,
    //
    // Summary:
    //     Specifies that the format is 32 bits per pixel; 8 bits each are used for the
    //     alpha, red, green, and blue components.
    Format32bppArgb = 2498570,
    //
    // Summary:
    //     Specifies that the format is 64 bits per pixel; 16 bits each are used for the
    //     alpha, red, green, and blue components.
    Format64bppArgb = 3424269
}