// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FreeImageAPI;

//
// Summary:
//     Specifies how much an image is rotated and the axis used to flip the image.
public enum RotateFlipType
{
    //
    // Summary:
    //     Specifies no clockwise rotation and no flipping.
    RotateNoneFlipNone = 0,
    //
    // Summary:
    //     Specifies a 180-degree clockwise rotation followed by a horizontal and vertical
    //     flip.
    Rotate180FlipXY = RotateNoneFlipNone,
    //
    // Summary:
    //     Specifies a 90-degree clockwise rotation without flipping.
    Rotate90FlipNone = 1,
    //
    // Summary:
    //     Specifies a 270-degree clockwise rotation followed by a horizontal and vertical
    //     flip.
    Rotate270FlipXY = Rotate90FlipNone,
    //
    // Summary:
    //     Specifies a 180-degree clockwise rotation without flipping.
    Rotate180FlipNone = 2,
    //
    // Summary:
    //     Specifies no clockwise rotation followed by a horizontal and vertical flip.
    RotateNoneFlipXY = Rotate180FlipNone,
    //
    // Summary:
    //     Specifies a 270-degree clockwise rotation without flipping.
    Rotate270FlipNone = 3,
    //
    // Summary:
    //     Specifies a 90-degree clockwise rotation followed by a horizontal and vertical
    //     flip.
    Rotate90FlipXY = Rotate270FlipNone,
    //
    // Summary:
    //     Specifies no clockwise rotation followed by a horizontal flip.
    RotateNoneFlipX = 4,
    //
    // Summary:
    //     Specifies a 180-degree clockwise rotation followed by a vertical flip.
    Rotate180FlipY = RotateNoneFlipX,
    //
    // Summary:
    //     Specifies a 90-degree clockwise rotation followed by a horizontal flip.
    Rotate90FlipX = 5,
    //
    // Summary:
    //     Specifies a 270-degree clockwise rotation followed by a vertical flip.
    Rotate270FlipY = Rotate90FlipX,
    //
    // Summary:
    //     Specifies a 180-degree clockwise rotation followed by a horizontal flip.
    Rotate180FlipX = 6,
    //
    // Summary:
    //     Specifies no clockwise rotation followed by a vertical flip.
    RotateNoneFlipY = Rotate180FlipX,
    //
    // Summary:
    //     Specifies a 270-degree clockwise rotation followed by a horizontal flip.
    Rotate270FlipX = 7,
    //
    // Summary:
    //     Specifies a 90-degree clockwise rotation followed by a vertical flip.
    Rotate90FlipY = Rotate270FlipX
}