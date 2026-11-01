// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Graphics;
using Xunit;

namespace Stride.TextureConverter.Tests
{
    public class ImageLoadTests
    {
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
