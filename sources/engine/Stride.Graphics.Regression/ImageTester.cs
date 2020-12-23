// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Stride.Engine.Network;
using Sockets.Plugin;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Regression
{
    public static class ImageTester
    {
        public enum ComparisonMode
        {
            /// <summary>
            /// Comparison will fails if image doesn't exist or is different.
            /// </summary>
            CompareOnly,
            /// <summary>
            /// Comparison will fails if image is different. If no version of it exist yet, it will be created.
            /// </summary>
            CompareOrCreate,
            CompareOrCreateAlternative,
        }

        public enum ComparisonResult
        {
            ReferenceCreated,
            Success,
            Failed,
        }

        public static void SaveImage(Image image, string testFilename)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(testFilename));
            using (var stream = File.Open(testFilename, FileMode.Create))
            {
                image.Save(stream, ImageFileType.Png);
            }
        }

        /// <summary>
        /// Send the data of the test to the server.
        /// </summary>
        /// <param name="image">The image to send.</param>
        /// <param name="testFilename">The expected filename.</param>
        public static bool CompareImage(Image image, string testFilename)
        {
            // Compare
            using (var stream = File.OpenRead(testFilename))
            using (var referenceImage = Image.Load(stream))
            {
                // Start comparison
                if (image.PixelBuffer.Count != referenceImage.PixelBuffer.Count)
                {
                    return false;
                }

                for (int i = 0; i < image.PixelBuffer.Count; ++i)
                {
                    var buffer = image.PixelBuffer[i];
                    var referenceBuffer = referenceImage.PixelBuffer[i];

                    if (buffer.Width != referenceBuffer.Width
                        || buffer.Height != referenceBuffer.Height
                        || buffer.RowStride != referenceBuffer.RowStride)
                        return false;

                    var swapBGR = buffer.Format.IsBGRAOrder() != referenceBuffer.Format.IsBGRAOrder();
                    // For now, we handle only those specific cases
                    if ((buffer.Format != PixelFormat.R8G8B8A8_UNorm_SRgb && buffer.Format != PixelFormat.B8G8R8A8_UNorm_SRgb)
                        || referenceBuffer.Format != PixelFormat.B8G8R8A8_UNorm)
                    {
                        // TODO: support more formats
                        return false;
                    }

                    bool checkAlpha = buffer.Format.AlphaSizeInBits() > 0;

                    // Compare remaining bytes.
                    int allowedDiff = 2;
                    int differentPixels = 0;
                    unsafe
                    {
                        for (int y = 0; y < buffer.Height; ++y)
                        {
                            var pSrc = (Color*)(buffer.DataPointer + y * buffer.RowStride);
                            var pDst = (Color*)(referenceBuffer.DataPointer + y * referenceBuffer.RowStride);
                            for (int x = 0; x < buffer.Width; ++x, ++pSrc, ++pDst)
                            {
                                var src = *pSrc;
                                if (swapBGR)
                                {
                                    var tmp = src.B;
                                    src.B = src.R;
                                    src.R = tmp;
                                }

                                var r = Math.Abs((int)src.R - (int)pDst->R);
                                var g = Math.Abs((int)src.G - (int)pDst->G);
                                var b = Math.Abs((int)src.B - (int)pDst->B);
                                var a = Math.Abs((int)src.A - (int)pDst->A);
                                if (r > allowedDiff || g > allowedDiff || b > allowedDiff || (a > allowedDiff && checkAlpha))
                                {
                                    // Too big difference
                                    differentPixels++;
                                }
                            }
                        }
                    }

                    if (differentPixels > 0)
                        return false;
                }

                return true;
            }
        }
    }
}
