// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using UIKit;
using Stride.Core;

namespace Stride.Graphics
{
    // iOS-native LoadFromMemory uses UIImage / CGBitmapContext to get hardware-accelerated
    // image decoding. Save* methods are implemented in the shared StandardImageHelper.cs
    // using ImageSharp.
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
                        MemoryUtilities.Free(pSource);

                    return image;
                }
            }
        }
    }
}
#endif
