// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core;
using Android.Graphics;
using SixLabors.ImageSharp.Processing;

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
                if (bitmap is null)
                {
                    // BitmapFactory returns null for formats it can't decode natively (e.g. TIFF on
                    // most Android versions). Fall back to the cross-platform ImageSharp path so we
                    // still load successfully, at the cost of slower decode.
                    memoryStream.Position = 0;
                    return LoadFromMemoryFallback(memoryStream, alphaLoadMode, handle, makeACopy, pSource);
                }

                // fix the format of the bitmap if not supported
                if (bitmap.GetConfig() != Bitmap.Config.Argb8888)
                {
                    var temp = bitmap.Copy(Bitmap.Config.Argb8888, false);
                    bitmap.Dispose();
                    bitmap = temp;
                }

                var bitmapData = bitmap.LockPixels();
                // Bitmap.Config.Argb8888 stores pixels as RGBA in memory on Android (per the NDK:
                // ANDROID_BITMAP_FORMAT_RGBA_8888 — bytes in order R, G, B, A). Match that format
                // for the destination image so a straight memcpy preserves channel ordering.
                var image = Image.New2D(bitmap.Width, bitmap.Height, 1, PixelFormat.R8G8B8A8_UNorm, 1, bitmap.RowBytes);
                MemoryUtilities.CopyWithAlignmentFallback((void*)image.PixelBuffer[0].DataPointer, (void*)bitmapData, (uint)image.PixelBuffer[0].BufferStride);
                bitmap.UnlockPixels();
                bitmap.Dispose();

                if (handle != null)
                    handle.Value.Free();
                else if (!makeACopy)
                    MemoryUtilities.Free(pSource);

                return image;
            }
        }

        private static unsafe Image LoadFromMemoryFallback(Stream stream, AlphaLoadMode alphaLoadMode, GCHandle? handle, bool makeACopy, nint pSource)
        {
            using var sharpImage = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream);
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

            var image = Image.New2D(sharpImage.Width, sharpImage.Height, mipMapCount: 1, format: PixelFormat.R8G8B8A8_UNorm, arraySize: 1);
            var pixelBuffer = image.PixelBuffer[0];
            int srcRowBytes = sharpImage.Width * 4;
            var pixelBytes = new byte[srcRowBytes * sharpImage.Height];
            sharpImage.CopyPixelDataTo(pixelBytes);
            fixed (byte* srcPtr = pixelBytes)
            {
                var dstPtr = (byte*)pixelBuffer.DataPointer;
                for (int y = 0; y < sharpImage.Height; y++)
                    Buffer.MemoryCopy(srcPtr + y * srcRowBytes, dstPtr + y * pixelBuffer.RowStride, pixelBuffer.RowStride, srcRowBytes);
            }

            if (handle != null)
                handle.Value.Free();
            else if (!makeACopy)
                MemoryUtilities.Free(pSource);

            return image;
        }
    }
}
#endif
