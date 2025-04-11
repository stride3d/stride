// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace FreeImageAPI;

//
// Summary:
//     Specifies the attributes of the pixel data contained in an System.Drawing.Image
//     object. The System.Drawing.Image.Flags property returns a member of this enumeration.
[Flags]
public enum ImageFlags
{
    //
    // Summary:
    //     There is no format information.
    None = 0,
    //
    // Summary:
    //     The pixel data is scalable.
    Scalable = 1,
    //
    // Summary:
    //     The pixel data contains alpha information.
    HasAlpha = 2,
    //
    // Summary:
    //     Specifies that the pixel data has alpha values other than 0 (transparent) and
    //     255 (opaque).
    HasTranslucent = 4,
    //
    // Summary:
    //     The pixel data is partially scalable, but there are some limitations.
    PartiallyScalable = 8,
    //
    // Summary:
    //     The pixel data uses an RGB color space.
    ColorSpaceRgb = 16,
    //
    // Summary:
    //     The pixel data uses a CMYK color space.
    ColorSpaceCmyk = 32,
    //
    // Summary:
    //     The pixel data is grayscale.
    ColorSpaceGray = 64,
    //
    // Summary:
    //     Specifies that the image is stored using a YCBCR color space.
    ColorSpaceYcbcr = 128,
    //
    // Summary:
    //     Specifies that the image is stored using a YCCK color space.
    ColorSpaceYcck = 256,
    //
    // Summary:
    //     Specifies that dots per inch information is stored in the image.
    HasRealDpi = 4096,
    //
    // Summary:
    //     Specifies that the pixel size is stored in the image.
    HasRealPixelSize = 8192,
    //
    // Summary:
    //     The pixel data is read-only.
    ReadOnly = 65536,
    //
    // Summary:
    //     The pixel data can be cached for faster access.
    Caching = 131072
}