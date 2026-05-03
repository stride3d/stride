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
    /// <returns>An <see cref="Image"/> object containing the loaded image data.</returns>
    /// <remarks>
    ///   The image is loaded with a pixel format of <see cref="PixelFormat.B8G8R8A8_UNorm"/>
    ///   and is vertically flipped to match expected orientation.
    /// </remarks>
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


    /// <summary>
    ///   Saves a GIF image to the specified stream using pixel data from memory.
    /// </summary>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="count">
    ///   The number of pixel buffers to use when saving the image.
    ///   Must be greater than zero and less than or equal to the length of <paramref name="pixelBuffers"/>.
    /// </param>
    /// <param name="description">
    ///   An <see cref="ImageDescription"/> structure that specifies the properties of the image,
    ///   such as width, height, and pixel format.
    /// </param>
    /// <param name="imageStream">
    ///   The stream to which the GIF image will be written. The stream must be writable.
    /// </param>
    public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_GIF);
    }

    /// <summary>
    ///   Saves a TIFF image to the specified stream using pixel data from memory.
    /// </summary>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="count">
    ///   The number of pixel buffers to use when saving the image.
    ///   Must be greater than zero and less than or equal to the length of <paramref name="pixelBuffers"/>.
    /// </param>
    /// <param name="description">
    ///   An <see cref="ImageDescription"/> structure that specifies the properties of the image,
    ///   such as width, height, and pixel format.
    /// </param>
    /// <param name="imageStream">
    ///   The stream to which the TIFF image will be written. The stream must be writable.
    /// </param>
    public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_TIFF);
    }

    /// <summary>
    ///   Saves a BMP image to the specified stream using pixel data from memory.
    /// </summary>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="count">
    ///   The number of pixel buffers to use when saving the image.
    ///   Must be greater than zero and less than or equal to the length of <paramref name="pixelBuffers"/>.
    /// </param>
    /// <param name="description">
    ///   An <see cref="ImageDescription"/> structure that specifies the properties of the image,
    ///   such as width, height, and pixel format.
    /// </param>
    /// <param name="imageStream">
    ///   The stream to which the BMP image will be written. The stream must be writable.
    /// </param>
    public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_BMP);
    }

    /// <summary>
    ///   Saves a JPEG image to the specified stream using pixel data from memory.
    /// </summary>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="count">
    ///   The number of pixel buffers to use when saving the image.
    ///   Must be greater than zero and less than or equal to the length of <paramref name="pixelBuffers"/>.
    /// </param>
    /// <param name="description">
    ///   An <see cref="ImageDescription"/> structure that specifies the properties of the image,
    ///   such as width, height, and pixel format.
    /// </param>
    /// <param name="imageStream">
    ///   The stream to which the JPEG image will be written. The stream must be writable.
    /// </param>
    /// <remarks>
    ///   The image is saved with a default quality of 90 and 4:2:0 chroma subsampling.
    /// </remarks>
    public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);

        // Set JPEG quality to 90 and 4:2:0 subsampling by default
        var flags = (FREE_IMAGE_SAVE_FLAGS) 90
                  | FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_420;

        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_JPEG, flags);
    }

    /// <summary>
    ///   Saves a PNG image to the specified stream using pixel data from memory.
    /// </summary>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="count">
    ///   The number of pixel buffers to use when saving the image.
    ///   Must be greater than zero and less than or equal to the length of <paramref name="pixelBuffers"/>.
    /// </param>
    /// <param name="description">
    ///   An <see cref="ImageDescription"/> structure that specifies the properties of the image,
    ///   such as width, height, and pixel format.
    /// </param>
    /// <param name="imageStream">
    ///   The stream to which the PNG image will be written. The stream must be writable.
    /// </param>
    public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        using var bitmap = new FreeImageBitmap(description.Width, description.Height);
        PrepareImageForSaving(bitmap, pixelBuffers, description);
        bitmap.Save(imageStream, FREE_IMAGE_FORMAT.FIF_PNG);
    }

    /// <summary>
    ///   Saves a WMP (Windows Media Photo) image to the specified stream using pixel data from memory.
    /// </summary>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="count">
    ///   The number of pixel buffers to use when saving the image.
    ///   Must be greater than zero and less than or equal to the length of <paramref name="pixelBuffers"/>.
    /// </param>
    /// <param name="description">
    ///   An <see cref="ImageDescription"/> structure that specifies the properties of the image,
    ///   such as width, height, and pixel format.
    /// </param>
    /// <param name="imageStream">
    ///   The stream to which the WMP image will be written. The stream must be writable.
    /// </param>
    /// <exception cref="NotImplementedException">The method is not implemented.</exception>
    public static void SaveWmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    ///   Prepares the specified bitmap for saving by converting its color depth to 32 bits per pixel,
    ///   copying pixel data from the provided buffers according to the image format,
    ///   and flipping the image vertically.
    /// </summary>
    /// <param name="bitmap">The bitmap to be prepared for saving.</param>
    /// <param name="pixelBuffers">
    ///   An array of pixel buffers containing the image data to copy into the bitmap.
    /// </param>
    /// <param name="description">The description of the image.</param>
    /// <exception cref="ArgumentException">
    ///   The pixel format specified in <paramref name="description"/> is not supported.
    ///   Supported formats are <see cref="PixelFormat.B8G8R8A8_UNorm"/>, <see cref="PixelFormat.B8G8R8A8_UNorm_SRgb"/>,
    ///   <see cref="PixelFormat.R8G8B8A8_UNorm"/>, <see cref="PixelFormat.R8G8B8A8_UNorm_SRgb"/>,
    ///   <see cref="PixelFormat.R8_UNorm"/>, and <see cref="PixelFormat.A8_UNorm"/>.
    /// </exception>
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
