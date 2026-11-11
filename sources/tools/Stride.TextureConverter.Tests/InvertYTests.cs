// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.TextureConverter.TexLibraries;
using Xunit;

namespace Stride.TextureConverter.Tests
{
    public class InvertYTests
    {
        [Theory]
        [InlineData(PixelFormat.R8G8B8A8_UNorm, new byte[] { 10, 20, 30, 40 }, new byte[] { 10, 235, 30, 40 })]
        [InlineData(PixelFormat.B8G8R8A8_UNorm, new byte[] { 10, 20, 30, 40 }, new byte[] { 10, 235, 30, 40 })]
        [InlineData(PixelFormat.R8G8_UNorm, new byte[] { 10, 20 }, new byte[] { 10, 235 })]
        [InlineData(PixelFormat.R8G8_SNorm, new byte[] { 10, 20 }, new byte[] { 10, 0xEC })]
        [InlineData(PixelFormat.R8G8_SNorm, new byte[] { 10, 0x80 }, new byte[] { 10, 0x7F })]
        [InlineData(PixelFormat.R8G8B8A8_SNorm, new byte[] { 10, 20, 30, 40 }, new byte[] { 10, 0xEC, 30, 40 })]
        [InlineData(PixelFormat.R16G16_UNorm, new byte[] { 1, 2, 0x34, 0x12 }, new byte[] { 1, 2, 0xCB, 0xED })]
        [InlineData(PixelFormat.R16G16B16A16_UNorm, new byte[] { 1, 2, 0x34, 0x12, 3, 4, 5, 6 }, new byte[] { 1, 2, 0xCB, 0xED, 3, 4, 5, 6 })]
        [InlineData(PixelFormat.R16G16_SNorm, new byte[] { 1, 2, 0x34, 0x12 }, new byte[] { 1, 2, 0xCC, 0xED })]
        [InlineData(PixelFormat.R16G16B16A16_SNorm, new byte[] { 1, 2, 0x00, 0x80, 3, 4, 5, 6 }, new byte[] { 1, 2, 0xFF, 0x7F, 3, 4, 5, 6 })]
        [InlineData(PixelFormat.R10G10B10A2_UNorm, new byte[] { 0x01, 0x08, 0x30, 0x40 }, new byte[] { 0x01, 0xF4, 0x3F, 0x40 })]
        public void GreenIsInverted(PixelFormat format, byte[] pixel, byte[] expected)
        {
            Assert.Equal(expected, InvertY(format, pixel));
        }

        [Fact]
        public void UnsupportedFormatLogsAnError()
        {
            var pixel = new byte[8];
            var errors = 0;
            void Count(object sender, MessageLoggedEventArgs args)
            {
                if (args.Message.Type == LogMessageType.Error && args.Message.Text.Contains(nameof(PixelFormat.R32G32_Float)))
                    errors++;
            }

            var logger = GlobalLogger.GetLogger("StrideTexLibrary");
            logger.MessageLogged += Count;
            try
            {
                Assert.Equal(pixel, InvertY(PixelFormat.R32G32_Float, pixel));
            }
            finally
            {
                logger.MessageLogged -= Count;
            }

            Assert.Equal(1, errors);
        }

        [Fact]
        public void SixteenBitPngIsInverted()
        {
            var path = Path.Combine(Path.GetTempPath(), $"stride-inverty-{Guid.NewGuid():N}.png");
            try
            {
                using (var source = new Image<Rgba64>(1, 1, new Rgba64(0x1111, 0x1234, 0x3333, 0xFFFF)))
                    source.SaveAsPng(path);

                using var textureTool = new TextureTool();
                using var image = textureTool.Load(path, false);

                // Off Windows, ImageSharpTexLib loads every PNG at 8 bits per channel
                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal(PixelFormat.R16G16B16A16_UNorm, image.Format);
                    textureTool.InvertY(image);
                    Assert.Equal((ushort)(ushort.MaxValue - 0x1234), (ushort)Marshal.ReadInt16(image.Data, sizeof(ushort)));
                }
                else
                {
                    Assert.Equal(PixelFormat.B8G8R8A8_UNorm, image.Format);
                    textureTool.InvertY(image);
                    Assert.Equal((byte)(byte.MaxValue - 0x12), Marshal.ReadByte(image.Data, 1));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static byte[] InvertY(PixelFormat format, byte[] pixel)
        {
            var data = (byte[])pixel.Clone();
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                using var library = new StrideTexLibrary();
                library.InvertY(new TexImage(handle.AddrOfPinnedObject(), data.Length, 1, 1, 1, format, 1, 1, TexImage.TextureDimension.Texture2D));
            }
            finally
            {
                handle.Free();
            }
            return data;
        }
    }
}
