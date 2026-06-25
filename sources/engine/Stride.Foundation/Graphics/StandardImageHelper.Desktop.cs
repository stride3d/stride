// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP

using System;
using System.IO;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Stride.Core;

using SharpImage = SixLabors.ImageSharp.Image;

namespace Stride.Graphics;

internal partial class StandardImageHelper
{
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
    ///   The image is loaded with a pixel format of <see cref="PixelFormat.B8G8R8A8_UNorm"/>.
    /// </remarks>
    public static unsafe Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle, AlphaLoadMode alphaLoadMode)
    {
        using var memoryStream = new UnmanagedMemoryStream((byte*)pSource, size, capacity: size, access: FileAccess.Read);

        // Bgra32 matches the in-memory layout of B8G8R8A8_UNorm so we can blit row-by-row.
        using var sharpImage = SharpImage.Load<Bgra32>(memoryStream);

        // PNG/JPG/BMP/GIF/TIFF decode as straight alpha; convert if caller asked for premul.
        if (alphaLoadMode == AlphaLoadMode.EnsurePremultiplied)
        {
            sharpImage.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
            {
                for (int i = 0; i < row.Length; i++)
                {
                    ref var px = ref row[i];
                    px.X *= px.W; px.Y *= px.W; px.Z *= px.W;
                }
            }));
        }

        int width = sharpImage.Width;
        int height = sharpImage.Height;

        var image = Image.New2D(width, height,
                                mipMapCount: 1, arraySize: 1,
                                format: PixelFormat.B8G8R8A8_UNorm);

        try
        {
            var pixelBuffer = image.PixelBuffer[0];
            int dstStride = pixelBuffer.RowStride;
            int srcRowBytes = width * sizeof(Bgra32);
            var pixelBytes = new byte[width * height * sizeof(Bgra32)];
            sharpImage.CopyPixelDataTo(pixelBytes);

            fixed (byte* srcPtr = pixelBytes)
            {
                var dstPtr = (byte*)pixelBuffer.DataPointer;
                for (int y = 0; y < height; y++)
                    Buffer.MemoryCopy(srcPtr + y * srcRowBytes, dstPtr + y * dstStride, dstStride, srcRowBytes);
            }
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

}

#endif
