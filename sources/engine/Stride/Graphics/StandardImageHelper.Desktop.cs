// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP

using System;
using System.IO;
using System.Runtime.InteropServices;

using FreeImageAPI;

using Stride.Core;

namespace Stride.Graphics;

internal partial class StandardImageHelper
{
    static StandardImageHelper()
    {
        NativeLibraryHelper.PreloadLibrary("freeimage", typeof(StandardImageHelper));
    }

    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// </summary>
    public static unsafe Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
    {
        using var memoryStream = new UnmanagedMemoryStream((byte*) pSource, size, capacity: size, access: FileAccess.Read);
        using var bitmap = FreeImageBitmap.FromStream(memoryStream);

        bitmap.RotateFlip(FreeImageAPI.RotateFlipType.RotateNoneFlipY);
        bitmap.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);

        var image = Image.New2D(bitmap.Width, bitmap.Height,
                                mipMapCount: 1, arraySize: 1, rowStride: bitmap.Line,
                                format: PixelFormat.B8G8R8A8_UNorm);

        try
        {
            // TODO: Test if still necessary
            // Directly load image as RGBA instead of BGRA, because OpenGL ES devices don't support it out of the box (extension).
            MemoryUtilities.CopyWithAlignmentFallback(destination: (void*) image.PixelBuffer[0].DataPointer,
                                                      source: (void*) bitmap.Bits,
                                                      byteCount: (uint) image.PixelBuffer[0].BufferStride);
        }
        finally
        {
            if (handle is not null)
                handle.Value.Free();
            else if (!makeACopy)
                MemoryUtilities.Free(pSource);
        }

        return image;
    }


    public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_GIF);
    }

    public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_TIFF);
    }

    public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_BMP);
    }

    public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);

        // Set JPEG quality to 90 and 4:2:0 subsampling by default
        var flags = (FREE_IMAGE_SAVE_FLAGS) 90
                  | FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_420;

        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_JPEG, flags);
    }

    public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_PNG);
    }

    public static void SaveWmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        throw new NotImplementedException();
    }


    private static unsafe void PrepareImageForSaving(FreeImageBitmap bitmap, PixelBuffer[] pixelBuffers, ImageDescription description)
    {
        // Ensure 32 bits per pixel
        bitmap.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);

        // Copy the image data according to the format
        var format = description.Format;
        if (format is PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb)
        {
            CopyMemoryBGRA(dest: bitmap.Bits, src: pixelBuffers[0].DataPointer,
                           sizeInBytesToCopy: pixelBuffers[0].BufferStride);
        }
        else if (format is PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb)
        {
            MemoryUtilities.CopyWithAlignmentFallback(destination: (void*) bitmap.Bits,
                                                      source: (void*) pixelBuffers[0].DataPointer,
                                                      byteCount: (uint) pixelBuffers[0].BufferStride);
        }
        else if (format is PixelFormat.R8_UNorm or PixelFormat.A8_UNorm)
        {
            // TODO: Ideally we will want to support grayscale images, but SpriteBatch can only render RGBA for now,
            //       so convert the grayscale image as RGBA and save it
            CopyMemoryRRR1(dest: bitmap.Bits, src: pixelBuffers[0].DataPointer,
                           sizeInBytesToCopy: pixelBuffers[0].BufferStride);
        }
        else
        {
            throw new ArgumentException(
                message:
                $"The pixel format {format} is not supported. Supported formats are {PixelFormat.B8G8R8A8_UNorm}, {PixelFormat.B8G8R8A8_UNorm_SRgb}, {PixelFormat.R8G8B8A8_UNorm}, {PixelFormat.R8G8B8A8_UNorm_SRgb}, {PixelFormat.R8_UNorm}, and {PixelFormat.A8_UNorm}",
                paramName: nameof(description));
        }

        // Flip the image vertically
        bitmap.RotateFlip(FreeImageAPI.RotateFlipType.RotateNoneFlipY);
    }
}

#endif
