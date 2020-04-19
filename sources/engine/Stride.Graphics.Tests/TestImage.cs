// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Engine;
using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Tests for <see cref="Texture"/>
    /// </summary>
    public class TestImage : GameTestBase
    {
        /// <summary>
        /// Tests Image 1D.
        /// </summary>
        [Fact]
        public void TestImage1D()
        {
            const int Size = 256; // must be > 256 to work
            int bufferCount = (int)Math.Log(Size, 2) + 1;
            int ExpectedSizePerArraySlice = Size * 2 - 1;

            var source = Image.New1D(Size, true, PixelFormat.R8_UNorm);

            // 9 buffers: 256 + 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1
            Assert.Equal(source.TotalSizeInBytes, ExpectedSizePerArraySlice);
            Assert.Equal(source.PixelBuffer.Count, bufferCount);

            // Check with array size
            var dest = Image.New1D(Size, true, PixelFormat.R8_UNorm, 6);
            Assert.Equal(dest.TotalSizeInBytes, ExpectedSizePerArraySlice * 6);
            Assert.Equal(dest.PixelBuffer.Count, bufferCount * 6);

            ManipulateImage(source, dest, 5, 0, 0);

            // Dispose images
            source.Dispose();
            dest.Dispose();
        }

        /// <summary>
        /// Tests Image 2D.
        /// </summary>
        [Fact]
        public void TestImage2D()
        {
            const int Size = 256; // must be > 256 to work
            int bufferCount = (int)Math.Log(Size, 2) + 1;
            int expectedSizePerArraySlice = ((int)Math.Pow(4, bufferCount) - 1) / 3;

            var source = Image.New2D(Size, Size, true, PixelFormat.R8_UNorm);

            // 9 buffers: 256 + 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1
            Assert.Equal(source.TotalSizeInBytes, expectedSizePerArraySlice);
            Assert.Equal(source.PixelBuffer.Count, bufferCount);

            // Check with array size
            var dest = Image.New2D(Size, Size, true, PixelFormat.R8_UNorm, 6);
            Assert.Equal(dest.TotalSizeInBytes, expectedSizePerArraySlice * 6);
            Assert.Equal(dest.PixelBuffer.Count, bufferCount * 6);

            ManipulateImage(source, dest, 5, 0, 0);

            // Dispose images
            source.Dispose();
            dest.Dispose();
        }

        /// <summary>
        /// Tests Image 2D.
        /// </summary>
        [Fact]
        public void TestImage3D()
        {
            const int Size = 64; // must be > 256 to work
            int bufferCount = (int)Math.Log(Size, 2) + 1;
            // BufferSize(x) = 1/7 (8^(x+1)-1)
            int expectedTotalBufferCount = (int)Math.Pow(2, bufferCount) - 1;
            int expectedSizePerArraySlice = ((int)Math.Pow(8, bufferCount) - 1) / 7;

            var source = Image.New3D(Size, Size, Size, true, PixelFormat.R8_UNorm);

            // 9 buffers: 256 + 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1
            Assert.Equal(source.TotalSizeInBytes, expectedSizePerArraySlice);
            Assert.Equal(source.PixelBuffer.Count, expectedTotalBufferCount);

            var dest = Image.New3D(Size, Size, Size, true, PixelFormat.R8_UNorm);

            ManipulateImage(source, dest, 0, 5, 0);

            // Dispose images
            source.Dispose();
            dest.Dispose();
        }

        private void ManipulateImage(Image source, Image dest, int arrayIndex, int zIndex, int mipIndex)
        {
            // Use Set Pixel
            var fromPixelBuffer = source.PixelBuffer[0];
            var toPixelBuffer = dest.PixelBuffer[arrayIndex, zIndex, mipIndex];

            fromPixelBuffer.SetPixel(0, 0, (byte)255);
            fromPixelBuffer.SetPixel(16, 0, (byte)128);
            fromPixelBuffer.CopyTo(toPixelBuffer);

            Assert.True(Utilities.CompareMemory(fromPixelBuffer.DataPointer, toPixelBuffer.DataPointer, fromPixelBuffer.BufferStride));

            // Use Get Pixels
            var fromPixels = fromPixelBuffer.GetPixels<byte>();
            Assert.Equal(fromPixels.Length, source.Description.Width * source.Description.Height);

            // Check values
            Assert.Equal(255, fromPixels[0]);
            Assert.Equal(128, fromPixels[16]);

            // Use Set Pixels
            fromPixels[0] = 1;
            fromPixels[16] = 2;
            fromPixelBuffer.SetPixels(fromPixels);

            // Use Get Pixel
            Assert.Equal(1, fromPixelBuffer.GetPixel<byte>(0, 0));
            Assert.Equal(2, fromPixelBuffer.GetPixel<byte>(16, 0));
        }

        [Fact(Skip="Ignored")]
        public void TestPerfLoadSave()
        {
            Image image;
            using (var stream = File.Open("map.bmp", FileMode.Open))
                image = Image.Load(stream);

            const int Count = 100; // Change this to perform memory benchmarks
            var types = new[]
            {
                ImageFileType.Dds,
            };
            var clock = Stopwatch.StartNew();
            foreach (var imageFileType in types)
            {
                clock.Restart();
                for (int i = 0; i < Count; i++)
                {
                    using (var stream = File.Open("map2.bin", FileMode.Create))
                        image.Save(stream, imageFileType);
                }
                Log.Info($"Save [{Count}] {imageFileType} in {clock.ElapsedMilliseconds}ms");
            }

            image.Dispose();
        }

        [Fact]
        public void TestLoadAndSave()
        {
            foreach (ImageFileType sourceFormat in Enum.GetValues(typeof(ImageFileType)))
            {
                foreach (ImageFileType intermediateFormat in Enum.GetValues(typeof(ImageFileType)))
                {
                    if (sourceFormat == ImageFileType.Wmp) // no input image of this format.
                        continue;

                    if (Platform.Type == PlatformType.Android && (
                        intermediateFormat == ImageFileType.Bmp || // TODO remove this when Save method is implemented for the bmp format
                        intermediateFormat == ImageFileType.Gif || // TODO remove this when Save method is implemented for the gif format
                        intermediateFormat == ImageFileType.Tiff || // TODO remove this when Save method is implemented for the tiff format
                        sourceFormat == ImageFileType.Bmp || // TODO remove this when Load method is fixed for the bmp format
                        sourceFormat == ImageFileType.Tiff)) // TODO remove this when Load method is fixed for the tiff format
                        continue; 

                    if (intermediateFormat == ImageFileType.Wmp || sourceFormat == ImageFileType.Wmp ||
                        intermediateFormat == ImageFileType.Tga || sourceFormat == ImageFileType.Tga) // TODO remove this when Load/Save methods are implemented for those types.
                        continue;

                    PerformTest(
                        game =>
                        {
                            ProcessFiles(game, sourceFormat, intermediateFormat);
                        });
                }
            }
        }

        [Fact]
        public void TestLoadPremultiplied()
        {
            var intermediateFormat = ImageFileType.Png;

            PerformTest(
                game =>
                {
                    // Load an image from a file and dispose it.
                    var fileName = "debug" + "Image";
                    var filePath = "ImageTypes/" + fileName;
                    Image image;

                    // Load an image from a buffer
                    byte[] buffer;
                    using (var inStream = game.Content.OpenAsStream(filePath, StreamFlags.None))
                    {
                        var bufferSize = inStream.Length;
                        buffer = new byte[bufferSize];
                        inStream.Read(buffer, 0, (int)bufferSize);
                    }

                    using (image = Image.Load(buffer))
                    {
                        // Write this image to a memory stream using DDS format.
                        var tempStream = new MemoryStream();
                        image.Save(tempStream, intermediateFormat);
                        tempStream.Position = 0;
                        
                        // Reload the image from the memory stream.
                        var image2 = Image.Load(tempStream);
                        CompareImage(image, image2, false, 0, fileName);
                        image2.Dispose();
                    }
                });
        }

        private void ProcessFiles(Game game, ImageFileType sourceFormat, ImageFileType intermediateFormat)
        {
            var testMemoryBefore = GC.GetTotalMemory(true);

            Log.Info($"Testing {intermediateFormat}");
            Console.Out.Flush();
            var imageCount = 0;
            var clock = Stopwatch.StartNew();

            // Load an image from a file and dispose it.
            var fileName = sourceFormat.ToFileExtension().Substring(1) + "Image";
            var filePath = "ImageTypes/" + fileName;
            Image image;
            using (var inStream = game.Content.OpenAsStream(filePath, StreamFlags.None))
                image = Image.Load(inStream);
            image.Dispose();

            // Load an image from a buffer
            byte[] buffer;
            using (var inStream = game.Content.OpenAsStream(filePath, StreamFlags.None))
            {
                var bufferSize = inStream.Length;
                buffer = new byte[bufferSize];
                inStream.Read(buffer, 0, (int)bufferSize);
            }

            using (image = Image.Load(buffer))
            {
                // Write this image to a memory stream using DDS format.
                var tempStream = new MemoryStream();
                image.Save(tempStream, intermediateFormat);
                tempStream.Position = 0;

                // Save to a file on disk
                var extension = intermediateFormat.ToFileExtension();
                using (var outStream = VirtualFileSystem.ApplicationCache.OpenStream(fileName + extension, VirtualFileMode.Create, VirtualFileAccess.Write))
                    image.Save(outStream, intermediateFormat);

                if (intermediateFormat == ImageFileType.Stride || intermediateFormat == ImageFileType.Dds || (sourceFormat == intermediateFormat 
                    && intermediateFormat != ImageFileType.Gif)) // TODO: remove this when Giff compression/decompression is fixed
                {
                    int allowSmallDifferences;
                    switch (intermediateFormat)
                    {
                        case ImageFileType.Tiff:// TODO: remove this when tiff encryption implementation is stable
                        case ImageFileType.Png: // TODO: remove this when png  encryption implementation is stable
                            allowSmallDifferences = 1;
                            break;
                        case ImageFileType.Jpg: // TODO: remove this when jepg encryption implementation is stable
                            allowSmallDifferences = 30;
                            break;
                        default:
                            allowSmallDifferences = 0;
                            break;
                    }

                    // Reload the image from the memory stream.
                    var image2 = Image.Load(tempStream);
                    CompareImage(image, image2, false, allowSmallDifferences, fileName);
                    image2.Dispose();
                }
            }

            imageCount++;

            var time = clock.ElapsedMilliseconds;
            clock.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            var testMemoryAfter = GC.GetTotalMemory(true);
            Log.Info($"Loaded {imageCount} and convert to (Dds, Jpg, Png, Gif, Bmp, Tiff) image from DirectXSDK test Memory: {testMemoryAfter - testMemoryBefore} bytes, in {time}ms");
        }

        internal static void CompareImage(Image from, Image to, bool ignoreAlpha, int allowedDifference = 0, string file = null)
        {
            // Check that description is identical to original image loaded from the disk.
            Assert.True(from.Description == to.Description, $"Image description is different for image [{file}]");

            // Check that number of buffers are identical.
            Assert.True(from.PixelBuffer.Count == to.PixelBuffer.Count, $"PixelBuffer size is different for image [{file}]");

            // Compare each pixel buffer
            for (int j = 0; j < from.PixelBuffer.Count; j++)
            {
                var srcPixelBuffer = from.PixelBuffer[j];
                var dstPixelBuffer = to.PixelBuffer[j];

                // Check only row and slice pitchs
                Assert.True(srcPixelBuffer.RowStride == dstPixelBuffer.RowStride, $"RowPitch are different for index [{j}], image [{file}]");
                Assert.True(srcPixelBuffer.BufferStride == dstPixelBuffer.BufferStride, $"SlicePitch are different for index [{j}], image [{file}]");

                var isSameBuffer = CompareImageData(srcPixelBuffer.DataPointer, dstPixelBuffer.DataPointer, srcPixelBuffer.BufferStride, ignoreAlpha, allowedDifference);
                //if (!isSameBuffer)
                //{
                //    var stream = new FileStream("test_from.dds", FileMode.Create, FileAccess.Write, FileShare.Write);
                //    stream.Write(srcPixelBuffer.DataPointer, 0, srcPixelBuffer.BufferStride);
                //    stream.Close();
                //    stream = new FileStream("test_to.dds", NativeFileMode.Create, NativeFileAccess.Write, NativeFileShare.Write);
                //    stream.Write(dstPixelBuffer.DataPointer, 0, dstPixelBuffer.BufferStride);
                //    stream.Close();
                //}

                Assert.True(isSameBuffer, $"Content of PixelBuffer is different for index [{j}], image [{file}]");
            }
        }

        public static unsafe bool CompareImageData(IntPtr from, IntPtr against, int sizeToCompare, bool ignoreAlpha, int allowedDifference = 0)
        {
            var pSrc = (byte*)from;
            var pDst = (byte*)against;

            // Compare remaining bytes.
            var pixelCount = sizeToCompare >> 2;
            while (pixelCount > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var originalValue = *pSrc;
                    var newValue = *pDst;
                    if ((i != 3 || !ignoreAlpha) && Math.Abs(originalValue - newValue) > allowedDifference)
                        return false;

                    pSrc++;
                    pDst++;
                }

                pixelCount -= 4;
            }

            return true;
        }
    }
}
