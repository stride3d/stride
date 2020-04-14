// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// TODO: Replace using System.Drawing, as it is not available on all platforms (not on Windows 8/WP8).
    /// </summary>
    partial class StandardImageHelper
    {
        public static unsafe Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
            using (var memoryStream = new UnmanagedMemoryStream((byte*)pSource, size))
            using (var bitmap = (Bitmap)System.Drawing.Image.FromStream(memoryStream))
            {
                var sourceArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                // Lock System.Drawing.Bitmap

                var bitmapData = bitmap.LockBits(sourceArea, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                var image = Image.New2D(bitmap.Width, bitmap.Height, 1, PixelFormat.B8G8R8A8_UNorm, 1, bitmapData.Stride);
                // var dataRect = new DataRectangle(bitmapData.Stride, bitmapData.Scan0);

                try
                {
                    // TODO: Test if still necessary
                    // Directly load image as RGBA instead of BGRA, because OpenGL ES devices don't support it out of the box (extension).
                    //image.Description.Format = PixelFormat.R8G8B8A8_UNorm;
                    //CopyMemoryBGRA(image.PixelBuffer[0].DataPointer, bitmapData.Scan0, image.PixelBuffer[0].BufferStride);
                    Utilities.CopyMemory(image.PixelBuffer[0].DataPointer, bitmapData.Scan0, image.PixelBuffer[0].BufferStride);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);

                    if (handle != null)
                        handle.Value.Free();
                    else if (!makeACopy)
                        Utilities.FreeMemory(pSource);
                }

                return image;
            }
        }

        public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, count, description, imageStream, ImageFormat.Gif);
        }

        public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, count, description, imageStream, ImageFormat.Tiff);
        }

        public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, count, description, imageStream, ImageFormat.Bmp);
        }

        public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, count, description, imageStream, ImageFormat.Jpeg);
        }

        public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            SaveFromMemory(pixelBuffers, count, description, imageStream, ImageFormat.Png);
        }

        public static void SaveWmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        private static void SaveFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream, ImageFormat imageFormat)
        {
            using (var bitmap = new Bitmap(description.Width, description.Height))
            {
                var sourceArea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

                // Lock System.Drawing.Bitmap
                var bitmapData = bitmap.LockBits(sourceArea, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                try
                {
                    // Copy memory
                    if (description.Format == PixelFormat.R8G8B8A8_UNorm || description.Format == PixelFormat.R8G8B8A8_UNorm_SRgb)
                    {
                        CopyMemoryBGRA(bitmapData.Scan0, pixelBuffers[0].DataPointer, pixelBuffers[0].BufferStride);
                    }
                    else if (description.Format == PixelFormat.B8G8R8A8_UNorm || description.Format == PixelFormat.B8G8R8A8_UNorm_SRgb)
                    {
                        Utilities.CopyMemory(bitmapData.Scan0, pixelBuffers[0].DataPointer, pixelBuffers[0].BufferStride);
                    }
                    else
                    {
                        // TODO Ideally we will want to support grayscale images, but the SpriteBatch can only render RGBA for now
                        //  so convert the grayscale image as an RGBA and save it
                        CopyMemoryRRR1(bitmapData.Scan0, pixelBuffers[0].DataPointer, pixelBuffers[0].BufferStride);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // Save
                bitmap.Save(imageStream, imageFormat);
            }
        }
    }
}
#endif
