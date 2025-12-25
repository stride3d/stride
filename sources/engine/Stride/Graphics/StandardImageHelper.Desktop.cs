// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_DESKTOP
using System;
using System.IO;
using System.Runtime.InteropServices;
using FreeImageAPI;
using Stride.Core;
using RotateFlipType = FreeImageAPI.RotateFlipType;

namespace Stride.Graphics
{
    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// </summary>
    partial class StandardImageHelper
    {
        static StandardImageHelper()
        {
            NativeLibraryHelper.PreloadLibrary("freeimage", typeof(StandardImageHelper));
        }

        public static unsafe Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
            using var memoryStream = new UnmanagedMemoryStream((byte*)pSource, size, capacity: size, access: FileAccess.Read);
            using var bitmap = FreeImageBitmap.FromStream(memoryStream);
            
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bitmap.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);
            
            var image = Image.New2D(bitmap.Width, bitmap.Height, 1, PixelFormat.B8G8R8A8_UNorm, 1, bitmap.Line);

            try
            {
                // TODO: Test if still necessary
                // Directly load image as RGBA instead of BGRA, because OpenGL ES devices don't support it out of the box (extension).
                Utilities.CopyWithAlignmentFallback((void*)image.PixelBuffer[0].DataPointer, (void*)bitmap.Bits, (uint)image.PixelBuffer[0].BufferStride);
            }
            finally
            {
                if (handle != null)
                    handle.Value.Free();
                else if (!makeACopy)
                    Utilities.FreeMemory(pSource);
            }

            return image;
        }

        public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, description, imageStream, FREE_IMAGE_FORMAT.FIF_GIF);
        }

        public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, description, imageStream, FREE_IMAGE_FORMAT.FIF_TIFF);
        }

        public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, description, imageStream, FREE_IMAGE_FORMAT.FIF_BMP);
        }

        public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, description, imageStream, FREE_IMAGE_FORMAT.FIF_JPEG);
        }

        public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, description, imageStream, FREE_IMAGE_FORMAT.FIF_PNG);
        }

        public static void SaveWmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        private static unsafe void SaveFromMemory(PixelBuffer[] pixelBuffers, ImageDescription description, Stream imageStream, FREE_IMAGE_FORMAT imageFormat)
        {
            using var bitmap = new FreeImageBitmap(description.Width, description.Height);
            bitmap.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);

            // Copy memory
            var format = description.Format;
            if (format is PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb)
            {
                CopyMemoryBGRA(bitmap.Bits, pixelBuffers[0].DataPointer, pixelBuffers[0].BufferStride);
            }
            else if (format is PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb)
            {
                Utilities.CopyWithAlignmentFallback((void*)bitmap.Bits, (void*)pixelBuffers[0].DataPointer, (uint)pixelBuffers[0].BufferStride);
            }
            else if (format is PixelFormat.R8_UNorm or PixelFormat.A8_UNorm)
            {
                // TODO Ideally we will want to support grayscale images, but the SpriteBatch can only render RGBA for now
                //  so convert the grayscale image as an RGBA and save it
                CopyMemoryRRR1(bitmap.Bits, pixelBuffers[0].DataPointer, pixelBuffers[0].BufferStride);
            }
            else
            {
                throw new ArgumentException(
                    message:
                    $"The pixel format {format} is not supported. Supported formats are {PixelFormat.B8G8R8A8_UNorm}, {PixelFormat.B8G8R8A8_UNorm_SRgb}, {PixelFormat.R8G8B8A8_UNorm}, {PixelFormat.R8G8B8A8_UNorm_SRgb}, {PixelFormat.R8_UNorm}, {PixelFormat.A8_UNorm}",
                    paramName: nameof(description));
            }

            // Save
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bitmap.Save(imageStream, imageFormat);
        }
    }
}
#endif
