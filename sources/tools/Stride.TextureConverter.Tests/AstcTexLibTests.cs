// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.TextureConverter.Tests
{
    public class AstcTexLibTests
    {
        private const int Size = 64;

        [Theory]
        [InlineData(PixelFormat.ASTC_6x6_UNorm, PixelFormat.R8G8B8A8_UNorm)]
        [InlineData(PixelFormat.ASTC_4x4_UNorm_SRgb, PixelFormat.R8G8B8A8_UNorm_SRgb)]
        public void CompressedImageDecompressesCloseToTheSource(PixelFormat astcFormat, PixelFormat sourceFormat)
        {
            using var source = Image.New2D(Size, Size, false, sourceFormat);
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                    source.PixelBuffer[0].SetPixel(x, y, Gradient(x, y));
            }

            using var textureTool = new TextureTool();
            using var image = textureTool.Load(source, sourceFormat.IsSRgb);

            textureTool.Compress(image, astcFormat);
            Assert.Equal(astcFormat, image.Format);

            textureTool.Decompress(image, astcFormat.IsSRgb);
            Assert.Equal(sourceFormat, image.Format);

            var maxError = 0;
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var expected = Gradient(x, y).ToArray();
                    for (var channel = 0; channel < 4; channel++)
                    {
                        var actual = Marshal.ReadByte(image.Data, (y * Size + x) * 4 + channel);
                        maxError = Math.Max(maxError, Math.Abs(actual - expected[channel]));
                    }
                }
            }
            // 6x6 blocks at Fast quality lose up to 12 on this gradient; a broken binding gives noise
            Assert.InRange(maxError, 0, 16);
        }

        private static Color Gradient(int x, int y) => new((byte)(x * 4), (byte)(y * 4), (byte)((x + y) * 2), (byte)255);
    }
}
