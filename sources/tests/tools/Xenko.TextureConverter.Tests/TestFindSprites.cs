// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Core.Mathematics;

namespace Xenko.TextureConverter.Tests
{
    public class TestFindSprites
    {
        private readonly Color transparencyColorKey = new Color(222, 76, 255, 255);

        private void CheckEmptyRegion(TextureTool tool, TexImage image, Int2 position)
        {
            var theoreticalRegion = new Rectangle(position.X, position.Y, 0, 0);
            var foundRegion = tool.FindSpriteRegion(image, position);
            Assert.AreEqual(theoreticalRegion, foundRegion);
        }

        [Test]
        public void EmptyRegionTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                CheckEmptyRegion(texTool, texImage, new Int2(-1, 5));
                CheckEmptyRegion(texTool, texImage, new Int2(1000, 5));
                CheckEmptyRegion(texTool, texImage, new Int2(4, -5));
                CheckEmptyRegion(texTool, texImage, new Int2(4, 5000));
                CheckEmptyRegion(texTool, texImage, new Int2(2, 2));
            }
        }

        [Test]
        public void SinglePixelTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(4, 5, 1, 1);
                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(4,5));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }

        [Test]
        public void TransversalLineTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(1, 8, 13, 15);

                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(3, 18));
                Assert.AreEqual(theoreticalRegion, foundRegion);

                foundRegion = texTool.FindSpriteRegion(texImage, new Int2(13, 8));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }

        [Test]
        public void ConvexShapeTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(10, 4, 36, 38);

                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(29, 23));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }

        [Test]
        public void ConcavShapeTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(65, 60, 55, 61);

                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(89, 67));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }

        [Test]
        public void IncludedShapeTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(70, 83, 32, 14);

                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(85, 85));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }
        
        [Test]
        public void ComplexShapeTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(0, 52, 59, 65);

                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(32, 74));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }

        [Test]
        public void ShapeWithHoleTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "TransparentSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(49, 1, 76, 53);

                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(52, 29));
                Assert.AreEqual(theoreticalRegion, foundRegion);

                foundRegion = texTool.FindSpriteRegion(texImage, new Int2(58, 30));
                Assert.AreEqual(theoreticalRegion, foundRegion);

                foundRegion = texTool.FindSpriteRegion(texImage, new Int2(75, 32));
                Assert.AreEqual(theoreticalRegion, foundRegion);

                foundRegion = texTool.FindSpriteRegion(texImage, new Int2(84, 28));
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }

        [Test]
        public void BgraRgbaTest()
        {
            var images = new[] { "BgraSheet.dds", "RgbaSheet.dds" };
            foreach (var image in images)
            {
                using (var texTool = new TextureTool())
                using (var texImage = texTool.Load(Module.PathToInputImages + image))
                {
                    var theoreticalRegion = new Rectangle(4, 4, 1, 1);
                    var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(4, 4), transparencyColorKey, 0xffffffff);
                    Assert.AreEqual(theoreticalRegion, foundRegion);

                    theoreticalRegion = new Rectangle(6, 7, 30, 20);
                    foundRegion = texTool.FindSpriteRegion(texImage, new Int2(23, 13), transparencyColorKey, 0xffffffff);
                    Assert.AreEqual(theoreticalRegion, foundRegion);

                    theoreticalRegion = new Rectangle(16, 28, 45, 31);
                    foundRegion = texTool.FindSpriteRegion(texImage, new Int2(42, 45), transparencyColorKey, 0xffffffff);
                    Assert.AreEqual(theoreticalRegion, foundRegion);
                }
            }
        }

        [Test]
        public void TransparencyKeyTest()
        {
            using (var texTool = new TextureTool())
            using (var texImage = texTool.Load(Module.PathToInputImages + "BgraSheet.dds"))
            {
                var theoreticalRegion = new Rectangle(25, 25, 0, 0);
                var foundRegion = texTool.FindSpriteRegion(texImage, new Int2(25, 25), transparencyColorKey, 0xffffffff);
                Assert.AreEqual(theoreticalRegion, foundRegion);
            }
        }
    }
}
