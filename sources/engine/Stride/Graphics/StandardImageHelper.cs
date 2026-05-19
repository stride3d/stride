// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

using SharpImage = SixLabors.ImageSharp.Image;

namespace Stride.Graphics;

/// <summary>
///   Provides implementations to load and save <see cref="Image"/>s of different formats
///   (e.g., PNG, GIF, BMP, JPEG, etc.)
/// </summary>
internal partial class StandardImageHelper
{
    public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var image = CreateImage(pixelBuffers[0], description);
        image.SaveAsGif(imageStream);
    }

    public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var image = CreateImage(pixelBuffers[0], description);
        // Note: ImageSharp 3.1.x TiffEncoder doesn't support emitting an alpha channel — the
        // output is always 24-bit RGB regardless of BitsPerPixel. TestLoadAndSave skips TIFF
        // for this reason.
        image.SaveAsTiff(imageStream);
    }

    public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var image = CreateImage(pixelBuffers[0], description);
        // 32-bit so the alpha channel survives the round-trip (default is 24-bit RGB).
        image.Save(imageStream, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32, SupportTransparency = true });
    }

    public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var image = CreateImage(pixelBuffers[0], description);
        var encoder = new JpegEncoder { Quality = 90, ColorType = JpegEncodingColor.YCbCrRatio420 };
        image.Save(imageStream, encoder);
    }

    public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var image = CreateImage(pixelBuffers[0], description);
        image.SaveAsPng(imageStream);
    }

    private static unsafe SixLabors.ImageSharp.Image<Rgba32> CreateImage(PixelBuffer buffer, ImageDescription description)
    {
        int width = description.Width;
        int height = description.Height;
        var src = (byte*)buffer.DataPointer;
        int srcStride = buffer.RowStride;
        var format = description.Format;

        var pixels = new Rgba32[width * height];

        if (format is PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb)
        {
            for (int y = 0; y < height; y++)
            {
                var srcRow = (Rgba32*)(src + y * srcStride);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = srcRow[x];
            }
        }
        else if (format is PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb)
        {
            for (int y = 0; y < height; y++)
            {
                var srcRow = (Bgra32*)(src + y * srcStride);
                for (int x = 0; x < width; x++)
                {
                    var p = srcRow[x];
                    pixels[y * width + x] = new Rgba32(p.R, p.G, p.B, p.A);
                }
            }
        }
        else if (format is PixelFormat.R8_UNorm or PixelFormat.A8_UNorm)
        {
            // SpriteBatch only renders RGBA, so expand single-channel grey into RGB with opaque alpha.
            for (int y = 0; y < height; y++)
            {
                var srcRow = src + y * srcStride;
                for (int x = 0; x < width; x++)
                {
                    byte g = srcRow[x];
                    pixels[y * width + x] = new Rgba32(g, g, g, (byte)255);
                }
            }
        }
        else
        {
            throw new ArgumentException(
                $"The pixel format {format} is not supported. Supported formats are {PixelFormat.B8G8R8A8_UNorm}, {PixelFormat.B8G8R8A8_UNorm_SRgb}, {PixelFormat.R8G8B8A8_UNorm}, {PixelFormat.R8G8B8A8_UNorm_SRgb}, {PixelFormat.R8_UNorm}, and {PixelFormat.A8_UNorm}",
                nameof(description));
        }

        return SharpImage.LoadPixelData<Rgba32>(pixels, width, height);
    }


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
