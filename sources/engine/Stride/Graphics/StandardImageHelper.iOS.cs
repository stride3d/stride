// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SharpImage = SixLabors.ImageSharp.Image;

namespace Stride.Graphics
{
    // We deliberately go through ImageSharp on iOS rather than ImageIO. ImageIO premultiplies
    // during decode at 8 bpc, which collapses low-alpha channels (e.g. straight (3,0,0,13) →
    // premul (0,0,0,13)) — the original RGB is unrecoverable. ImageSharp returns straight bytes
    // directly, matching Desktop verbatim. The same dependency is already linked here for the
    // Save* helpers, so this costs no extra binary size.
    partial class StandardImageHelper
    {
        public static unsafe Image LoadFromMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle, AlphaLoadMode alphaLoadMode)
        {
            try
            {
                using var memoryStream = new UnmanagedMemoryStream((byte*)pSource, size, capacity: size, access: FileAccess.Read);
                using var sharpImage = SharpImage.Load<Rgba32>(memoryStream);

                // PNG/JPG/BMP/GIF/TIFF decode straight; flip to premul only if the caller asked.
                if (alphaLoadMode == AlphaLoadMode.EnsurePremultiplied)
                {
                    sharpImage.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            var row = accessor.GetRowSpan(y);
                            for (int i = 0; i < row.Length; i++)
                            {
                                ref var px = ref row[i];
                                px.R = (byte)((px.R * px.A + 127) / 255);
                                px.G = (byte)((px.G * px.A + 127) / 255);
                                px.B = (byte)((px.B * px.A + 127) / 255);
                            }
                        }
                    });
                }

                int width = sharpImage.Width;
                int height = sharpImage.Height;
                int bytesPerRow = width * 4;
                var image = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm, 1, bytesPerRow);

                var dst = (byte*)image.PixelBuffer[0].DataPointer;
                sharpImage.CopyPixelDataTo(new Span<byte>(dst, bytesPerRow * height));

                return image;
            }
            finally
            {
                if (handle != null)
                    handle.Value.Free();
                else if (!makeACopy)
                    Stride.Core.MemoryUtilities.Free(pSource);
            }
        }
    }
}
#endif
