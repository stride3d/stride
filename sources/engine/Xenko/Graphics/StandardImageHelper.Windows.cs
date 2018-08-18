// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX
using System;
#if (XENKO_UI_WINFORMS || XENKO_UI_WPF)
using System.Drawing;
using System.Drawing.Imaging;
#endif
#if XENKO_UI_SDL
using SDL2;
#endif
using System.IO;
using System.Runtime.InteropServices;
using Xenko.Core;

namespace Xenko.Graphics
{
#if (XENKO_UI_OPENTK || XENKO_UI_SDL) && (!XENKO_UI_WINFORMS && !XENKO_UI_WPF)
    public sealed class ImageFormat
    {
        // Format IDs
        // private static ImageFormat undefined = new ImageFormat(new Guid("{b96b3ca9-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat memoryBMP = new ImageFormat(new Guid("{b96b3caa-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat bmp = new ImageFormat(new Guid("{b96b3cab-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat emf = new ImageFormat(new Guid("{b96b3cac-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat wmf = new ImageFormat(new Guid("{b96b3cad-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat jpeg = new ImageFormat(new Guid("{b96b3cae-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat png = new ImageFormat(new Guid("{b96b3caf-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat gif = new ImageFormat(new Guid("{b96b3cb0-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat tiff = new ImageFormat(new Guid("{b96b3cb1-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat exif = new ImageFormat(new Guid("{b96b3cb2-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat photoCD = new ImageFormat(new Guid("{b96b3cb3-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat flashPIX = new ImageFormat(new Guid("{b96b3cb4-0728-11d3-9d7b-0000f81ef32e}"));
        private static ImageFormat icon = new ImageFormat(new Guid("{b96b3cb5-0728-11d3-9d7b-0000f81ef32e}"));


        private Guid guid;

        public ImageFormat(Guid guid)
        {
            this.guid = guid;
        }

        public Guid Guid
        {
            get { return guid; }
        }

        public static ImageFormat MemoryBmp { get { return memoryBMP; } }
        public static ImageFormat Bmp { get { return bmp; } }
        public static ImageFormat Emf { get { return emf; } }
        public static ImageFormat Wmf { get { return wmf; } }
        public static ImageFormat Gif { get { return gif; } }
        public static ImageFormat Jpeg { get { return jpeg; } }
        public static ImageFormat Png { get { return png; } }
        public static ImageFormat Tiff { get { return tiff; } }
        public static ImageFormat Exif { get { return exif; } }
        public static ImageFormat Icon { get { return icon; } }

        public override bool Equals(object o)
        {
            ImageFormat format = o as ImageFormat;
            if (format == null)
                return false;
            return this.guid == format.guid;
        }

        public override int GetHashCode()
        {
            return this.guid.GetHashCode();
        }

        public override string ToString()
        {
            if (this == memoryBMP) return "MemoryBMP";
            if (this == bmp) return "Bmp";
            if (this == emf) return "Emf";
            if (this == wmf) return "Wmf";
            if (this == gif) return "Gif";
            if (this == jpeg) return "Jpeg";
            if (this == png) return "Png";
            if (this == tiff) return "Tiff";
            if (this == exif) return "Exif";
            if (this == icon) return "Icon";
            return "[ImageFormat: " + guid + "]";
        }
    }
#endif

    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// TODO: Replace using System.Drawing, as it is not available on all platforms (not on Windows 8/WP8).
    /// </summary>
    partial class StandardImageHelper
    {
        public static unsafe Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
#if XENKO_UI_WINFORMS || XENKO_UI_WPF
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
#if XENKO_GRAPHICS_API_OPENGLES && XENKO_PLATFORM_WINDOWS_DESKTOP
                    // Directly load image as RGBA instead of BGRA, because OpenGL ES devices don't support it out of the box (extension).
                    image.Description.Format = PixelFormat.R8G8B8A8_UNorm;
                    CopyMemoryBGRA(image.PixelBuffer[0].DataPointer, bitmapData.Scan0, image.PixelBuffer[0].BufferStride);
#else
                    Utilities.CopyMemory(image.PixelBuffer[0].DataPointer, bitmapData.Scan0, image.PixelBuffer[0].BufferStride);
#endif
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
#else
    #if XENKO_UI_SDL
            // FIXME: Manu: The following beginning of code shows that we can read images using SDL.
            // FIXME: We will do the implementation logic later.
            //Core.NativeLibrary.PreloadLibrary("SDL2_image.dll");
            //IntPtr rw = SDL.SDL_RWFromMemNative((byte *)pSource, size);
            //IntPtr image = SDL_image.IMG_Load_RW(rw, 1);
    #elif XENKO_UI_OPENTK
    #endif
            return null;
#endif

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
#if (XENKO_UI_WINFORMS || XENKO_UI_WPF)
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
#else
            // FIXME: Manu: Currently SDL can only save to BMP or PNG.
#endif
        }
    }
}
#endif
