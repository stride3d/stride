// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core;
using Android.Graphics;

namespace Stride.Graphics
{
    // Android-native LoadFromMemory uses BitmapFactory to get hardware-accelerated image
    // decoding. Save* methods are implemented in the shared StandardImageHelper.cs using
    // ImageSharp.
    partial class StandardImageHelper
    {
        public static unsafe Image LoadFromMemory(nint pSource, int size, bool makeACopy, GCHandle? handle, AlphaLoadMode alphaLoadMode)
        {
            using (var memoryStream = new UnmanagedMemoryStream((byte*)pSource, size, capacity: size, access: FileAccess.Read))
            {
                var options = new BitmapFactory.Options
                {
                    InPreferredConfig = Bitmap.Config.Argb8888,
                    InPremultiplied = alphaLoadMode == AlphaLoadMode.EnsurePremultiplied,
                };
                var bitmap = BitmapFactory.DecodeStream(memoryStream, new Rect(), options);

                // fix the format of the bitmap if not supported
                if (bitmap.GetConfig() != Bitmap.Config.Argb8888)
                {
                    var temp = bitmap.Copy(Bitmap.Config.Argb8888, false);
                    bitmap.Dispose();
                    bitmap = temp;
                }

                var bitmapData = bitmap.LockPixels();
                var image = Image.New2D(bitmap.Width, bitmap.Height, 1, PixelFormat.B8G8R8A8_UNorm, 1, bitmap.RowBytes);
                // Directly load image as RGBA instead of BGRA, because OpenGL ES devices don't support it out of the box (extension).
                image.Description.Format = PixelFormat.R8G8B8A8_UNorm;
                CopyMemoryBGRA(image.PixelBuffer[0].DataPointer, bitmapData, image.PixelBuffer[0].BufferStride);
                bitmap.UnlockPixels();
                bitmap.Dispose();

                if (handle != null)
                    handle.Value.Free();
                else if (!makeACopy)
                    MemoryUtilities.Free(pSource);

                return image;
            }
        }
    }
}
#endif
