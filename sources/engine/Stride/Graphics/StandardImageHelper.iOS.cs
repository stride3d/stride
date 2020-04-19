// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using UIKit;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// </summary>
    partial class StandardImageHelper
    {
        public static Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
            using (var imagedata = NSData.FromBytes(pSource, (uint) size))
            using (var cgImage = new UIImage(imagedata).CGImage)
            {
                var image = Image.New2D((int)cgImage.Width, (int)cgImage.Height, 1, PixelFormat.R8G8B8A8_UNorm, 1, (int)cgImage.BytesPerRow);

                using (var context = new CGBitmapContext(image.PixelBuffer[0].DataPointer, cgImage.Width, cgImage.Height, 8, cgImage.Width*4, cgImage.ColorSpace, CGBitmapFlags.PremultipliedLast))
                {
                    context.DrawImage(new RectangleF(0, 0, cgImage.Width, cgImage.Height), cgImage);

                    if (handle != null)
                        handle.Value.Free();
                    else if (!makeACopy)
                        Utilities.FreeMemory(pSource);

                    return image;  
                }
            }
        }

        public static void SaveGifFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        public static void SaveTiffFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        public static void SaveBmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        public static void SaveJpgFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        public static void SavePngFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }

        public static void SaveWmpFromMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
