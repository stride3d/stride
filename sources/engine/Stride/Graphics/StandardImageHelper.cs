// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Numerics;

namespace Stride.Graphics;

/// <summary>
///   Provides implementations to load and save <see cref="Image"/>s of different formats
///   (e.g., PNG, GIF, BMP, JPEG, etc.)
/// </summary>
internal partial class StandardImageHelper
{
    /// <summary>
    ///   Copies a block of memory from a source buffer to a destination buffer,
    ///   converting each 32-bit pixel from RGBA to BGRA format.
    /// </summary>
    /// <param name="dest">A pointer to the destination buffer that will receive the converted BGRA pixel data.</param>
    /// <param name="src">A pointer to the source buffer containing the RGBA pixel data to copy and convert.</param>
    /// <param name="sizeInBytesToCopy">
    ///   The number of bytes to copy and convert. Must be a multiple of 4, as each pixel is represented by 4 bytes.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="sizeInBytesToCopy"/> is not a multiple of 4.</exception>
    /// <remarks>
    ///   The conversion swaps the red and blue channels for each pixel, effectively transforming the format
    ///   from RGBA to BGRA.
    /// </remarks>
    private static unsafe void CopyMemoryBGRA(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
    {
        if ((sizeInBytesToCopy & 3) != 0)
            throw new ArgumentException("Should be a multiple of 4.", nameof(sizeInBytesToCopy));

        var bufferSize = sizeInBytesToCopy / 4;
        var srcPtr = (uint*) src;
        var destPtr = (uint*) dest;
        for (int i = 0; i < bufferSize; ++i)
        {
            var value = *srcPtr++;
            // value: 0xAARRGGBB or in reverse 0xAABBGGRR
            value = BinaryPrimitives.ReverseEndianness(value);
            // value: 0xBBGGRRAA or in reverse 0xRRGGBBAA
            value = BitOperations.RotateRight(value, 8);
            // value: 0xAABBGGRR or in reverse 0xAARRGGBB
            *destPtr++ = value;
        }
    }

    /// <summary>
    ///   Copies a block of memory from a source buffer to a destination buffer,
    ///   converting each source byte representing a red channel value into a 32-bit RGBA pixel
    ///   with full opacity and equal red, green, and blue channels.
    /// </summary>
    /// <param name="dest">A pointer to the destination buffer where the converted RGBA pixel data will be written.</param>
    /// <param name="src">A pointer to the source buffer containing the red channel byte values to copy and convert.</param>
    /// <param name="sizeInBytesToCopy">
    ///   The number of bytes to copy and convert from the source buffer.
    ///   Each byte is treated as a single red channel value and converted to one 32-bit RGBA pixel.
    /// </param>
    private static unsafe void CopyMemoryRRR1(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
    {
        var bufferSize = sizeInBytesToCopy;
        var srcPtr = (byte*)src;
        var destPtr = (uint*)dest;
        for (int i = 0; i < bufferSize; ++i)
        {
            uint value = *srcPtr++;
            // R => RGBA
            value = 0xFF000000u | (value * 0x010101u);
            *destPtr++ = value;
        }
    }
}
