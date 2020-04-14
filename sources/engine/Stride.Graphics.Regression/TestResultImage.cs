// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Stride.Core.LZ4;
using Stride.Graphics;

namespace Stride.Graphics.Regression
{
    public class TestResultImage
    {
        public string TestName;
        public string Frame;

        // Image
        public Image Image;

        public void Read(BinaryReader reader)
        {
            TestName = reader.ReadString();
            Frame = reader.ReadString();

            // Read image header
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var format = (PixelFormat)reader.ReadInt32();
            var textureSize = reader.ReadInt32();

            // Read image data
            var imageData = new byte[textureSize];
            using (var lz4Stream = new LZ4Stream(reader.BaseStream, CompressionMode.Decompress, false, textureSize))
            {
                if (lz4Stream.Read(imageData, 0, textureSize) != textureSize)
                    throw new EndOfStreamException("Unexpected end of stream");
            }

            var pinnedImageData = GCHandle.Alloc(imageData, GCHandleType.Pinned);
            var description = new ImageDescription
            {
                Dimension = TextureDimension.Texture2D,
                Width = width,
                Height = height,
                ArraySize = 1,
                Depth = 1,
                Format = format,
                MipLevels = 1,
            };

            Image = Image.New(description, pinnedImageData.AddrOfPinnedObject(), 0, pinnedImageData, false);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(TestName);
            writer.Write(Frame);

            // This call returns the pixels without any extra stride
            var pixels = Image.PixelBuffer[0].GetPixels<byte>();

            writer.Write(Image.PixelBuffer[0].Width);
            writer.Write(Image.PixelBuffer[0].Height);
            writer.Write((int)Image.PixelBuffer[0].Format);
            writer.Write(pixels.Length);

            // Write image data
            var lz4Stream = new LZ4Stream(writer.BaseStream, CompressionMode.Compress, false, pixels.Length);
            lz4Stream.Write(pixels, 0, pixels.Length);
            lz4Stream.Flush();
            writer.Flush();
        }
    }
}
