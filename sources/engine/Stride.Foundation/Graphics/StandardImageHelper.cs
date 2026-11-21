// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using StbImageNext;
using StbImageNext.Encoders;
using Stride.Core;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingImage = System.Drawing.Image;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Stride.Graphics;

/// <summary>
///   Provides implementations to load and save <see cref="Image"/>s of different formats
///   (e.g., PNG, GIF, BMP, JPEG, etc.)
/// </summary>
internal static class StandardImageHelper
{
    [SupportedOSPlatform("windows6.1")]
    public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        SaveWithDrawing(CreatePixels(pixelBuffers[0], description), description.Width, description.Height, imageStream, DrawingImageFormat.Gif);
    }
    
    [SupportedOSPlatform("windows6.1")]
    public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        SaveWithDrawing(CreatePixels(pixelBuffers[0], description), description.Width, description.Height, imageStream, DrawingImageFormat.Tiff);
    }

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
    ///   The image is loaded with a pixel format of <see cref="PixelFormat.B8G8R8A8_UNorm"/>.
    /// </remarks>
    public static unsafe Image LoadFromMemory(IntPtr source, int size, bool makeACopy, GCHandle? handle, AlphaLoadMode alphaLoadMode)
    {
        try
        {
            using var stream = new UnmanagedMemoryStream((byte*)source, size);
            var span = new ReadOnlySpan<byte>((void*)source, size);
            int width, height;
            byte[] pixels;
            if (IsGif(span))
            {
                if (!OperatingSystem.IsWindows())
                    throw new PlatformNotSupportedException("Loading GIF images is supported only on Windows.");
                pixels = DecodeWithDrawing(stream, out width, out height);
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    if (pixels[i + 3] == 0)
                        pixels[i] = pixels[i + 1] = pixels[i + 2] = 0;
                }
            }
            else if (IsTiff(span))
            {
                if (!OperatingSystem.IsWindows())
                    throw new PlatformNotSupportedException("Loading TIFF images is supported only on Windows.");
                pixels = DecodeWithDrawing(stream, out width, out height);
            }
            else
            {
                var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                width = result.Width;
                height = result.Height;
                pixels = result.Data;
            }

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

    private static bool IsGif(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 && bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == '8';

    private static bool IsTiff(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 &&
        ((bytes[0] == 'I' && bytes[1] == 'I' && (bytes[2] == 42 || bytes[2] == 43) && bytes[3] == 0) ||
         (bytes[0] == 'M' && bytes[1] == 'M' && bytes[2] == 0 && (bytes[3] == 42 || bytes[3] == 43)));

    [SupportedOSPlatform("windows")]
    private static unsafe byte[] DecodeWithDrawing(Stream stream, out int width, out int height)
    {
        using var bitmap = (DrawingBitmap)DrawingImage.FromStream(stream);
        width = bitmap.Width;
        height = bitmap.Height;
        var rect = new System.Drawing.Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, DrawingPixelFormat.Format32bppArgb);
        try
        {
            var pixels = new byte[checked(width * height * 4)];
            for (int y = 0; y < height; y++)
            {
                var row = (byte*)data.Scan0 + y * data.Stride;
                for (int x = 0; x < width; x++)
                {
                    int offset = (y * width + x) * 4;
                    pixels[offset] = row[x * 4 + 2];
                    pixels[offset + 1] = row[x * 4 + 1];
                    pixels[offset + 2] = row[x * 4];
                    pixels[offset + 3] = row[x * 4 + 3];
                }
            }
            return pixels;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    [SupportedOSPlatform("windows")]
    private static unsafe void SaveWithDrawing(byte[] pixels, int width, int height, Stream stream, DrawingImageFormat imageFormat)
    {
        using var bitmap = new DrawingBitmap(width, height, DrawingPixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, DrawingPixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < height; y++)
            {
                var row = (byte*)data.Scan0 + y * data.Stride;
                for (int x = 0; x < width; x++)
                {
                    int offset = (y * width + x) * 4;
                    row[x * 4] = pixels[offset + 2];
                    row[x * 4 + 1] = pixels[offset + 1];
                    row[x * 4 + 2] = pixels[offset];
                    row[x * 4 + 3] = pixels[offset + 3];
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        bitmap.Save(stream, imageFormat);
    }
}
