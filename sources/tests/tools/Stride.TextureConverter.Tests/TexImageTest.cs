// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

using Xunit;
using Xenko.TextureConverter;

namespace Xenko.TextureConverter.Tests
{
    public class TexImageTest : IDisposable
    {
        private readonly TexImage image = new TexImage(Marshal.AllocHGlobal(699104), 699104, 512, 512, 1, Xenko.Graphics.PixelFormat.BC3_UNorm, 10, 2, TexImage.TextureDimension.Texture2D);

        public void Dispose()
        {
            Marshal.FreeHGlobal(image.Data);
        }

        [Fact(Skip = "Need check")]
        public void TestEquals()
        {
            TexImage image2 = new TexImage(new IntPtr(), 699104, 512, 512, 1, Xenko.Graphics.PixelFormat.BC3_UNorm, 10, 2, TexImage.TextureDimension.Texture2D);
            Assert.True(image.Equals(image2));

            image2 = new TexImage(new IntPtr(), 699104, 512, 256, 1, Xenko.Graphics.PixelFormat.BC3_UNorm, 10, 2, TexImage.TextureDimension.Texture2D);
            Assert.False(image.Equals(image2));
        }

        [Fact(Skip = "Need check")]
        public void TestClone()
        {
            TexImage clone = (TexImage)image.Clone();

            Assert.True(image.Equals(clone));

            clone.Dispose();
        }
    }
}
