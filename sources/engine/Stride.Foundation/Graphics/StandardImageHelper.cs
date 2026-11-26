// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using StbImageNext;
using StbImageNext.Encoders;
using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Provides implementations to load and save <see cref="Image"/>s of different formats
///   (e.g., PNG, GIF, BMP, JPEG, etc.)
/// </summary>
internal static class StandardImageHelper
{
    public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        BmpEncoder.EncodeToStream(imageStream, CreatePixels(pixelBuffers[0], description), description.Width, description.Height, ColorComponents.RedGreenBlueAlpha);
    }

    public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        JpgEncoder.EncodeToStream(imageStream, CreatePixels(pixelBuffers[0], description), description.Width, description.Height, ColorComponents.RedGreenBlueAlpha, 90);
    }

    public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        PngEncoder.EncodeToStream(imageStream, CreatePixels(pixelBuffers[0], description), description.Width, description.Height, ColorComponents.RedGreenBlueAlpha);
    }

    private static unsafe byte[] CreatePixels(PixelBuffer buffer, ImageDescription description)
    {
        const int BytesPerPixel = 4;
        int width = description.Width;
        int height = description.Height;
        var src = (byte*)buffer.DataPointer;
        int srcStride = buffer.RowStride;
        var format = description.Format;

        var pixels = new byte[checked(width * height * BytesPerPixel)];

        if (format is PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb)
        {
            for (int y = 0; y < height; y++)
            {
                var srcRow = src + y * srcStride;
                new ReadOnlySpan<byte>(srcRow, width * BytesPerPixel).CopyTo(pixels.AsSpan(y * width * BytesPerPixel, width * BytesPerPixel));
            }
        }
        else if (format is PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb)
        {
            for (int y = 0; y < height; y++)
            {
                var srcRow = src + y * srcStride;
                for (int x = 0; x < width; x++)
                {
                    int offset = (y * width + x) * BytesPerPixel;
                    pixels[offset] = srcRow[x * BytesPerPixel + 2];
                    pixels[offset + 1] = srcRow[x * BytesPerPixel + 1];
                    pixels[offset + 2] = srcRow[x * BytesPerPixel];
                    pixels[offset + 3] = srcRow[x * BytesPerPixel + 3];
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
                    int offset = (y * width + x) * 4;
                    pixels.AsSpan(offset, 3).Fill(srcRow[x]);
                    pixels[offset + 3] = 255;
                }
            }
        }
        else
        {
            throw new ArgumentException(
                $"The pixel format {format} is not supported. Supported formats are {PixelFormat.B8G8R8A8_UNorm}, {PixelFormat.B8G8R8A8_UNorm_SRgb}, {PixelFormat.R8G8B8A8_UNorm}, {PixelFormat.R8G8B8A8_UNorm_SRgb}, {PixelFormat.R8_UNorm}, and {PixelFormat.A8_UNorm}",
                nameof(description));
        }

        return pixels;
    }

    /// <summary>
    ///   Loads an image from a block of unmanaged memory.
    /// </summary>
    /// <param name="pSource">
    ///   A pointer to the beginning of the unmanaged memory block containing the image data.
    /// </param>
    /// <param name="size">
    ///   The size, in bytes, of the memory block pointed to by <paramref name="pSource"/>.
    /// </param>
    /// <param name="makeACopy">
    ///   A value indicating whether to make a copy of the image data (<see langword="true"/>),
    ///   or to use the provided memory directly (<see langword="false"/>).
    ///   If <see langword="false"/>, the method may free the memory after loading.
    /// </param>
    /// <param name="handle">
    ///   An optional <see cref="GCHandle"/> associated with the memory block.
    ///   If provided, the handle will be freed after loading the image.
    /// </param>
    /// <param name="alphaLoadMode">The requested alpha conversion mode.</param>
    /// <returns>An <see cref="Image"/> object containing the loaded image data.</returns>
    /// <remarks>
    ///   The image is loaded with a pixel format of <see cref="PixelFormat.R8G8B8A8_UNorm"/>.
    /// </remarks>
    public static unsafe Image LoadFromMemory(IntPtr source, int size, bool makeACopy, GCHandle? handle, AlphaLoadMode alphaLoadMode)
    {
        try
        {
            using var stream = new UnmanagedMemoryStream((byte*)source, size);
            var span = new ReadOnlySpan<byte>((void*)source, size);
            if (IsTiff(span))
                throw new NotSupportedException("Loading TIFF images is not supported.");

            var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            int width = result.Width;
            int height = result.Height;
            byte[] pixels = result.Data;

            if (alphaLoadMode == AlphaLoadMode.EnsurePremultiplied)
            {
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    int alpha = pixels[i + 3];
                    pixels[i] = (byte)((pixels[i] * alpha + 127) / 255);
                    pixels[i + 1] = (byte)((pixels[i + 1] * alpha + 127) / 255);
                    pixels[i + 2] = (byte)((pixels[i + 2] * alpha + 127) / 255);
                }
            }

            var image = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);
            try
            {
                var buffer = image.PixelBuffer[0];
                fixed (byte* src = pixels)
                {
                    for (int y = 0; y < height; y++)
                        Buffer.MemoryCopy(src + y * width * 4, (byte*)buffer.DataPointer + y * buffer.RowStride, buffer.RowStride, width * 4);
                }
                return image;
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
        finally
        {
            if (handle.HasValue)
                handle.Value.Free();
            else if (!makeACopy)
                MemoryUtilities.Free(source);
        }
    }

    private static bool IsTiff(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 &&
        ((bytes[0] == 'I' && bytes[1] == 'I' && (bytes[2] == 42 || bytes[2] == 43) && bytes[3] == 0) ||
         (bytes[0] == 'M' && bytes[1] == 'M' && bytes[2] == 0 && (bytes[3] == 42 || bytes[3] == 43)));

}
