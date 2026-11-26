// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Graphics;
using Stride.TextureConverter.Requests;
using Stride.TextureConverter.TexLibraries;
using Xunit;

namespace Stride.TextureConverter.Tests
{
    public class ImageLoadTests
    {
        private static readonly byte[] GifData = [
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 2, 0, 1, 0, 0x80, 0, 0,
            0xFF, 0xFF, 0xFF, 0xFF, 0, 0,
            0x21, 0xF9, 4, 1, 0, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 2, 0x44, 0x0A, 0, 0x3B
        ];

        [Theory]
        [InlineData(AlphaLoadMode.Preserve)]
        [InlineData(AlphaLoadMode.EnsureStraight)]
        [InlineData(AlphaLoadMode.EnsurePremultiplied)]
        public void RuntimeLoadsGif(AlphaLoadMode alphaLoadMode)
        {
            using var image = Image.Load(GifData, alphaLoadMode: alphaLoadMode);
            Assert.Equal(2, image.Description.Width);
            Assert.Equal(1, image.Description.Height);
            Assert.Equal(0u, image.PixelBuffer[0].GetPixel<uint>(0, 0));
            Assert.Equal(0xFF0000FFu, image.PixelBuffer[0].GetPixel<uint>(1, 0));
        }

        [Theory]
        [InlineData(ImageFileType.Gif)]
        [InlineData(ImageFileType.Tiff)]
        public void RuntimeDoesNotSaveGifOrTiff(ImageFileType format)
        {
            using var image = Image.New2D(1, 1, 1, PixelFormat.R8G8B8A8_UNorm);
            using var stream = new MemoryStream();
            Assert.Throws<NotSupportedException>(() => image.Save(stream, format));
        }

        [Theory]
        [InlineData(0x49, 0x49, 42, 0)]
        [InlineData(0x49, 0x49, 43, 0)]
        [InlineData(0x4D, 0x4D, 0, 42)]
        [InlineData(0x4D, 0x4D, 0, 43)]
        public void RuntimeDoesNotLoadTiff(byte first, byte second, byte third, byte fourth)
        {
            Assert.Throws<NotSupportedException>(() => Image.Load(new[] { first, second, third, fourth }));
        }

        [Fact]
        public void ManagedBackendLoadsGif()
        {
            var path = Path.Combine(Path.GetTempPath(), $"stride-{Guid.NewGuid():N}.gif");
            try
            {
                File.WriteAllBytes(path, GifData);
                using var library = new ImageSharpTexLib();
                using var image = new TexImage();
                library.Execute(image, new FileLoadingRequest(path, false));
                Assert.Equal(2, image.Width);
                Assert.Equal(1, image.Height);
                var pixels = new byte[8];
                Marshal.Copy(image.Data, pixels, 0, pixels.Length);
                Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 0, 255, 255 }, pixels);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void TextureToolLoadsGifUsingExpectedBackend()
        {
            var path = Path.Combine(Path.GetTempPath(), $"stride-{Guid.NewGuid():N}.GIF");
            try
            {
                File.WriteAllBytes(path, GifData);
                using var tool = new TextureTool();
                using var image = tool.Load(path, false);
                Assert.Equal(2, image.Width);
                Assert.Equal(1, image.Height);
                Assert.Contains(image.LibraryData.Keys, library => OperatingSystem.IsWindows() ? library is DxtTexLib : library is ImageSharpTexLib);
                Assert.True(image.OriginalAlphaDepth > 0);
                var pixels = new byte[8];
                Marshal.Copy(image.Data, pixels, 0, pixels.Length);
                Assert.Equal(0, pixels[3]);
                Assert.Equal(255, pixels[7]);
                var red = image.Format switch
                {
                    PixelFormat.R8G8B8A8_UNorm => new byte[] { 255, 0, 0 },
                    PixelFormat.B8G8R8A8_UNorm => new byte[] { 0, 0, 255 },
                    _ => throw new InvalidOperationException($"Unexpected GIF pixel format: {image.Format}"),
                };
                Assert.Equal(red, pixels[4..7]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(".tif")]
        [InlineData(".tiff")]
        public void TextureToolLoadsTiffOnWindows(string extension)
        {
            using var library = new DxtTexLib();
            Assert.Equal(OperatingSystem.IsWindows(), library.CanHandleRequest(PixelFormat.None, new FileLoadingRequest("texture" + extension, false)));
            if (!OperatingSystem.IsWindows())
                return;

            var path = Path.Combine(Path.GetTempPath(), $"stride-{Guid.NewGuid():N}{extension}");
            try
            {
                File.Copy(Path.Combine(AppContext.BaseDirectory, "InputImages", "TiffImage.tif"), path);
                using var tool = new TextureTool();
                using var image = tool.Load(path, false);
                Assert.True(image.Width > 0);
                Assert.True(image.Height > 0);
                Assert.Contains(image.LibraryData.Keys, entry => entry is DxtTexLib);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LoadReadsRemainingStreamDespiteShortReads()
        {
            using var source = Image.New2D(2, 2, false, PixelFormat.R8_UNorm);
            source.PixelBuffer[0].SetPixel(0, 0, (byte)42);
            using var encoded = new MemoryStream();
            encoded.WriteByte(0);
            source.Save(encoded, ImageFileType.Dds);
            using var stream = new ShortReadStream(encoded.ToArray());
            stream.Position = 1;

            using var result = Image.Load(stream);

            Assert.Equal(2, result.Description.Width);
            Assert.Equal(2, result.Description.Height);
            Assert.Equal(42, result.PixelBuffer[0].GetPixel<byte>(0, 0));
            Assert.Equal(stream.Length, stream.Position);
        }

        [Fact]
        public void LoadThrowsForPrematureEndOfStream()
        {
            using var stream = new ShortReadStream(new byte[] { 1, 2 }, reportedExtraLength: 1);
            Assert.Throws<EndOfStreamException>(() => Image.Load(stream));
        }

        private sealed class ShortReadStream : MemoryStream
        {
            private readonly int reportedExtraLength;

            public ShortReadStream(byte[] buffer, int reportedExtraLength = 0) : base(buffer)
            {
                this.reportedExtraLength = reportedExtraLength;
            }

            public override long Length => base.Length + reportedExtraLength;

            public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);
        }
    }
}
