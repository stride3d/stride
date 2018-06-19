// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

using NUnit.Framework;
using Xenko.TextureConverter;

namespace Xenko.TextureConverter.Tests
{
    [TestFixture]
    class TexImageTest
    {
        TexImage image;

        [SetUp]
        public void SetUp()
        {
            image = new TexImage(Marshal.AllocHGlobal(699104), 699104, 512, 512, 1, Xenko.Graphics.PixelFormat.BC3_UNorm, 10, 2, TexImage.TextureDimension.Texture2D);
        }

        [TearDown]
        public void TearDown()
        {
            Marshal.FreeHGlobal(image.Data);
        }

        [Test, Ignore("Need check")]
        public void TestEquals()
        {
            TexImage image2 = new TexImage(new IntPtr(), 699104, 512, 512, 1, Xenko.Graphics.PixelFormat.BC3_UNorm, 10, 2, TexImage.TextureDimension.Texture2D);
            Assert.IsTrue(image.Equals(image2));

            image2 = new TexImage(new IntPtr(), 699104, 512, 256, 1, Xenko.Graphics.PixelFormat.BC3_UNorm, 10, 2, TexImage.TextureDimension.Texture2D);
            Assert.IsFalse(image.Equals(image2));
        }

        [Test, Ignore("Need check")]
        public void TestClone()
        {
            TexImage clone = (TexImage)image.Clone();

            Assert.IsTrue(image.Equals(clone));

            clone.Dispose();
        }
    }
}
