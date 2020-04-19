// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Xunit;

using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Assets.Textures.Packing;
using Stride.Engine;
using Stride.Graphics;
using Stride.TextureConverter;

namespace Stride.Assets.Tests
{
    public class TexturePackerTests
    {
        private static readonly string ApplicationPath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string TexturePackerFolder = Path.Combine(ApplicationPath, "TexturePacking") + Path.DirectorySeparatorChar;
        private static readonly string ImageOutputPath = Path.Combine(TexturePackerFolder, "OutputImages") + Path.DirectorySeparatorChar;
        private static readonly string ImageInputPath = Path.Combine(TexturePackerFolder, "InputImages") + Path.DirectorySeparatorChar;
        private static readonly string GoldImagePath = Path.Combine(TexturePackerFolder, "GoldImages") + Path.DirectorySeparatorChar;

        static TexturePackerTests()
        {
            RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);
        }

        public TexturePackerTests()
        {
            Game.InitializeAssetDatabase();
        }

        [Fact]
        public void TestMaxRectsPackWithoutRotation()
        {
            var maxRectPacker = new MaxRectanglesBinPack();
            maxRectPacker.Initialize(100, 100, false);

            // This data set remain only 1 rectangle that cant be packed
            var elementToPack = new List<AtlasTextureElement>
            {
                CreateElement(null, 80, 100),
                CreateElement(null, 100, 20),
            };

            maxRectPacker.PackRectangles(elementToPack);

            Assert.Single(elementToPack);
            Assert.Single(maxRectPacker.PackedElements);
        }

        [Fact]
        public void TestMaxRectsPackWithRotation()
        {
            var maxRectPacker = new MaxRectanglesBinPack();
            maxRectPacker.Initialize(100, 100, true);

            // This data set remain only 1 rectangle that cant be packed
            var packRectangles = new List<AtlasTextureElement>
            {
                CreateElement("A", 80, 100),
                CreateElement("B", 100, 20),
            };

            maxRectPacker.PackRectangles(packRectangles);

            Assert.Empty(packRectangles);
            Assert.Equal(2, maxRectPacker.PackedElements.Count);
            Assert.True(maxRectPacker.PackedElements.Find(e => e.Name == "B").DestinationRegion.IsRotated);
        }

        /// <summary>
        /// Test packing 7 rectangles
        /// </summary>
        [Fact]
        public void TestMaxRectsPackArbitaryRectangles()
        {
            var maxRectPacker = new MaxRectanglesBinPack();
            maxRectPacker.Initialize(100, 100, true);

            // This data set remain only 1 rectangle that cant be packed
            var packRectangles = new List<AtlasTextureElement>
            {
                CreateElement(null, 55, 70),
                CreateElement(null, 55, 30),
                CreateElement(null, 25, 30),
                CreateElement(null, 20, 30),
                CreateElement(null, 45, 30),
                CreateElement(null, 25, 40),
                CreateElement(null, 20, 40),
            };

            maxRectPacker.PackRectangles(packRectangles);

            Assert.Single(packRectangles);
            Assert.Equal(6, maxRectPacker.PackedElements.Count);
        }

        [Fact]
        public void TestTexturePackerFitAllElements()
        {
            var textureElements = CreateFakeTextureElements();

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);
        }

        public List<AtlasTextureElement> CreateFakeTextureElements()
        {
            return new List<AtlasTextureElement>
            {
                CreateElement("A", 100, 200),
                CreateElement("B", 400, 300),
            };
        }

        [Fact]
        public void TestTexturePackerEmptyList()
        {
            var textureElements = new List<AtlasTextureElement>();

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxHeight = 300,
                MaxWidth = 300,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.Empty(textureElements);
            Assert.Empty(texturePacker.AtlasTextureLayouts);
            Assert.True(canPackAllTextures);
        }

        [Fact]
        public void TestRotationElement1()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("imageRotated0", 0, TextureAddressMode.Clamp, TextureAddressMode.Clamp)
            };
            textureElements[0].SourceRegion.IsRotated = true;

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxWidth = 128,
                MaxHeight = 256,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);
            Assert.Single(texturePacker.AtlasTextureLayouts);

            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(texturePacker.AtlasTextureLayouts[0], false);

            SaveAndCompareTexture(atlasTexture, "TestRotationElement1");
        }

        [Fact]
        public void TestRotationElement2()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("imageRotated1", 0, TextureAddressMode.Clamp, TextureAddressMode.Clamp)
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxWidth = 256,
                MaxHeight = 128,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);
            Assert.Single(texturePacker.AtlasTextureLayouts);

            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(texturePacker.AtlasTextureLayouts[0], false);

            SaveAndCompareTexture(atlasTexture, "TestRotationElement2");
        }

        [Fact]
        public void TestTexturePackerWithMultiPack()
        {
            var textureElements = CreateFakeTextureElements();

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxHeight = 300,
                MaxWidth = 300,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.Equal(2, textureElements.Count);
            Assert.Empty(texturePacker.AtlasTextureLayouts);
            Assert.False(canPackAllTextures);

            texturePacker.Reset();
            texturePacker.MaxWidth = 1500;
            texturePacker.MaxHeight = 800;

            canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);
            Assert.Single(texturePacker.AtlasTextureLayouts);
            Assert.Equal(textureElements.Count, texturePacker.AtlasTextureLayouts[0].Textures.Count);

            Assert.True(MathUtil.IsPow2(texturePacker.AtlasTextureLayouts[0].Width));
            Assert.True(MathUtil.IsPow2(texturePacker.AtlasTextureLayouts[0].Height));
        }

        [Fact]
        public void TestTexturePackerWithBorder()
        {
            var textureAtlases = new List<AtlasTextureLayout>();

            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("A", 100, 200, 2),
                CreateElement("B", 57, 22, 2),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = true,
                AllowRotation = true,
                MaxHeight = 512,
                MaxWidth = 512
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);
            textureAtlases.AddRange(texturePacker.AtlasTextureLayouts);

            Assert.True(canPackAllTextures);
            Assert.Equal(2, textureElements.Count);
            Assert.Single(textureAtlases);

            Assert.True(MathUtil.IsPow2(textureAtlases[0].Width));
            Assert.True(MathUtil.IsPow2(textureAtlases[0].Height));

            // Test if border is applied in width and height
            var textureA = textureAtlases[0].Textures.Find(rectangle => rectangle.Name == "A");
            var textureB = textureAtlases[0].Textures.Find(rectangle => rectangle.Name == "B");

            Assert.Equal(textureA.SourceRegion.Width + 2 * textureA.BorderSize, textureA.DestinationRegion.Width);
            Assert.Equal(textureA.SourceRegion.Height + 2 * textureA.BorderSize, textureA.DestinationRegion.Height);

            Assert.Equal(textureB.SourceRegion.Width + 2 * textureB.BorderSize,
                (!textureB.DestinationRegion.IsRotated) ? textureB.DestinationRegion.Width : textureB.DestinationRegion.Height);
            Assert.Equal(textureB.SourceRegion.Height + 2 * textureB.BorderSize,
                (!textureB.DestinationRegion.IsRotated) ? textureB.DestinationRegion.Height : textureB.DestinationRegion.Width);
        }

        private AtlasTextureElement CreateElement(string name, int width, int height, int borderSize = 0, Color? color = null)
        {
            return CreateElement(name, width, height, borderSize, TextureAddressMode.Clamp, color);
        }

        private AtlasTextureElement CreateElement(string name, int width, int height, int borderSize, TextureAddressMode borderMode, Color? color = null, Color? borderColor = null)
        {
            Image image = null;
            if (color != null)
                image = CreateMockTexture(width, height, color.Value);

            return new AtlasTextureElement(name, image, new RotableRectangle(0, 0, width, height), borderSize, borderMode, borderMode, borderColor);
        }

        private AtlasTextureElement CreateElementFromFile(string name, int borderSize, TextureAddressMode borderModeU, TextureAddressMode borderModeV, RotableRectangle? imageRegion = null)
        {
            using (var texTool = new TextureTool())
            {
                var image = LoadImage(texTool, new UFile(ImageInputPath + "/" + name + ".png"));
                var region = imageRegion ?? new RotableRectangle(0, 0, image.Description.Width, image.Description.Height);

                return new AtlasTextureElement(name, image, region, borderSize, borderModeU, borderModeV, Color.SteelBlue);
            }
        }

        private Image CreateMockTexture(int width, int height, Color color)
        {
            var texture = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);

            unsafe
            {
                var ptr = (Color*)texture.DataPointer;

                // Fill in mock data
                for (var y = 0; y < height; ++y)
                    for (var x = 0; x < width; ++x)
                    {
                        ptr[y * width + x] = y < height / 2 ? color : Color.White;
                    }
            }

            return texture;
        }

        [Fact]
        public void TestTextureAtlasFactory()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("A", 100, 200, 0, Color.MediumPurple),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);
            Assert.True(MathUtil.IsPow2(textureAtlases[0].Width));
            Assert.True(MathUtil.IsPow2(textureAtlases[0].Height));

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.Equal(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.Equal(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactory");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Fact]
        public void TestTextureAtlasFactoryRotation()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("image9", 25, TextureAddressMode.Clamp, TextureAddressMode.Clamp),
                CreateElementFromFile("image10", 25, TextureAddressMode.Mirror, TextureAddressMode.Mirror),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxWidth = 306,
                MaxHeight = 356,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);
            Assert.Equal(texturePacker.MaxWidth, textureAtlases[0].Width);
            Assert.Equal(texturePacker.MaxHeight, textureAtlases[0].Height);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.Equal(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.Equal(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactoryRotation");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Fact]
        public void TestTextureAtlasFactoryRotation2()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("image9", 25, TextureAddressMode.Clamp, TextureAddressMode.Clamp),
                CreateElementFromFile("image10", 25, TextureAddressMode.Mirror, TextureAddressMode.Mirror),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxWidth = 356,
                MaxHeight = 306,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);
            Assert.Equal(texturePacker.MaxWidth, textureAtlases[0].Width);
            Assert.Equal(texturePacker.MaxHeight, textureAtlases[0].Height);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.Equal(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.Equal(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactoryRotation2");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Fact]
        public void TestRegionOutOfTexture()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("image9", 10, TextureAddressMode.Mirror, TextureAddressMode.Clamp, new RotableRectangle(-100, 30, 400, 250)),
                CreateElementFromFile("image10", 10, TextureAddressMode.Wrap, TextureAddressMode.Border, new RotableRectangle(-50, -30, 300, 400, true)),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                AllowNonPowerOfTwo = true,
                MaxWidth = 1024,
                MaxHeight = 1024,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.Equal(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.Equal(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestRegionOutOfTexture");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        [Fact]
        public void TestTextureAtlasFactoryImageParts()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElementFromFile("imagePart0", 26, TextureAddressMode.Border, TextureAddressMode.Mirror, new RotableRectangle(0, 0, 128, 128)),
                CreateElementFromFile("imagePart0", 26, TextureAddressMode.Clamp, TextureAddressMode.Clamp, new RotableRectangle(128, 128, 128, 128)),
                CreateElementFromFile("imagePart0", 26, TextureAddressMode.MirrorOnce, TextureAddressMode.Wrap, new RotableRectangle(128, 0, 128, 128)),
                CreateElementFromFile("imagePart1", 26, TextureAddressMode.Clamp, TextureAddressMode.Mirror, new RotableRectangle(376, 0, 127, 256)),
                CreateElementFromFile("imagePart1", 26, TextureAddressMode.Mirror, TextureAddressMode.Clamp, new RotableRectangle(10, 10, 254, 127)),
                CreateElement("empty", 0, 0, 26),
                CreateElementFromFile("imagePart2", 26, TextureAddressMode.Clamp, TextureAddressMode.Clamp, new RotableRectangle(0, 0, 128, 64)),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxWidth = 2048,
                MaxHeight = 2048,
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);
            Assert.Equal(texturePacker.MaxWidth/2, textureAtlases[0].Width);
            Assert.Equal(texturePacker.MaxHeight/4, textureAtlases[0].Height);

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            Assert.Equal(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.Equal(textureAtlases[0].Height, atlasTexture.Description.Height);

            SaveAndCompareTexture(atlasTexture, "TestTextureAtlasFactoryImageParts");

            textureElements[0].Texture.Dispose();
            atlasTexture.Dispose();
        }

        private void SaveAndCompareTexture(Image outputImage, string fileName, ImageFileType extension = ImageFileType.Png)
        {
            // save
            Directory.CreateDirectory(ImageOutputPath);
            outputImage.Save(new FileStream(ImageOutputPath + fileName + extension.ToFileExtension(), FileMode.Create), extension); 

            // Compare
            using(var texTool = new TextureTool())
            {
                var referenceImage = LoadImage(texTool, new UFile(GoldImagePath + "/" + fileName + extension.ToFileExtension()));
                Assert.True(CompareImages(outputImage, referenceImage), "The texture outputted differs from the gold image.");
            }
        }

        // Note: this comparison function is not very robust and might have to be improved at some point (does not take in account RowPitch, etc...)
        private bool CompareImages(Image outputImage, Image referenceImage)
        {
            if (outputImage.Description != referenceImage.Description)
                return false;
            
            unsafe
            {
                var ptr1 = (Color*)outputImage.DataPointer;
                var ptr2 = (Color*)referenceImage.DataPointer;

                // Fill in mock data
                for (var i = 0; i < outputImage.Description.Height * outputImage.Description.Width; ++i)
                {
                    if (*ptr1 != *ptr2)
                        return false;

                    ++ptr1;
                    ++ptr2;
                }
            }

            return true;
        }

        [Fact]
        public void TestNullSizeTexture()
        {
            var textureElements = new List<AtlasTextureElement> { CreateElement("A", 0, 0) };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Empty(textureAtlases);
        }

        [Fact]
        public void TestNullSizeElements()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("A", 10, 10, 5),
                CreateElement("B", 11, 0, 6),
                CreateElement("C", 12, 13, 7),
                CreateElement("D", 0, 14, 8),
                CreateElement("E", 14, 15, 9),
                CreateElement("F", 0, 0, 10),
                CreateElement("G", 16, 17, 11),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 2000,
                MaxWidth = 2000
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);
            Assert.Equal(4, textureAtlases[0].Textures.Count);
            Assert.Null(textureAtlases[0].Textures.Find(e => e.Name == "B"));
            Assert.Null(textureAtlases[0].Textures.Find(e => e.Name == "D"));
            Assert.Null(textureAtlases[0].Textures.Find(e => e.Name == "F"));
            Assert.NotNull(textureAtlases[0].Textures.Find(e => e.Name == "A"));
            Assert.NotNull(textureAtlases[0].Textures.Find(e => e.Name == "C"));
            Assert.NotNull(textureAtlases[0].Textures.Find(e => e.Name == "E"));
            Assert.NotNull(textureAtlases[0].Textures.Find(e => e.Name == "G"));
        }

        [Fact]
        public void TestWrapBorderMode()
        {
            // Positive sets
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.Wrap));
            Assert.Equal(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.Wrap));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.Wrap));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.Wrap));
            Assert.Equal(5, AtlasTextureFactory.GetSourceTextureCoordinate(15, 10, TextureAddressMode.Wrap));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(19, 10, TextureAddressMode.Wrap));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.Wrap));
            Assert.Equal(5, AtlasTextureFactory.GetSourceTextureCoordinate(25, 10, TextureAddressMode.Wrap));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(29, 10, TextureAddressMode.Wrap));

            // Negative sets
            Assert.Equal(6, AtlasTextureFactory.GetSourceTextureCoordinate(-4, 10, TextureAddressMode.Wrap));
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.Wrap));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.Wrap));
            Assert.Equal(6, AtlasTextureFactory.GetSourceTextureCoordinate(-14, 10, TextureAddressMode.Wrap));
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-19, 10, TextureAddressMode.Wrap));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.Wrap));
            Assert.Equal(6, AtlasTextureFactory.GetSourceTextureCoordinate(-24, 10, TextureAddressMode.Wrap));
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-29, 10, TextureAddressMode.Wrap));
        }

        [Fact]
        public void TestClampBorderMode()
        {
            // Positive sets
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.Clamp));
            Assert.Equal(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.Clamp));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.Clamp));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.Clamp));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(15, 10, TextureAddressMode.Clamp));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(19, 10, TextureAddressMode.Clamp));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.Clamp));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(25, 10, TextureAddressMode.Clamp));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(29, 10, TextureAddressMode.Clamp));

            // Negative sets
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-4, 10, TextureAddressMode.Clamp));
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.Clamp));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.Clamp));
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-14, 10, TextureAddressMode.Clamp));
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-19, 10, TextureAddressMode.Clamp));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.Clamp));
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-24, 10, TextureAddressMode.Clamp));
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-29, 10, TextureAddressMode.Clamp));
        }

        [Fact]
        public void TestMirrorBorderMode()
        {
            // Positive sets
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.Mirror));
            Assert.Equal(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.Mirror));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.Mirror));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.Mirror));
            Assert.Equal(8, AtlasTextureFactory.GetSourceTextureCoordinate(11, 10, TextureAddressMode.Mirror));
            Assert.Equal(7, AtlasTextureFactory.GetSourceTextureCoordinate(12, 10, TextureAddressMode.Mirror));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.Mirror));
            Assert.Equal(8, AtlasTextureFactory.GetSourceTextureCoordinate(21, 10, TextureAddressMode.Mirror));

            // Negative Sets
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-1, 10, TextureAddressMode.Mirror));
            Assert.Equal(2, AtlasTextureFactory.GetSourceTextureCoordinate(-2, 10, TextureAddressMode.Mirror));
            Assert.Equal(3, AtlasTextureFactory.GetSourceTextureCoordinate(-3, 10, TextureAddressMode.Mirror));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.Mirror));
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.Mirror));
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-11, 10, TextureAddressMode.Mirror));

            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.Mirror));
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-21, 10, TextureAddressMode.Mirror));
        }

        [Fact]
        public void TestMirrorOnceBorderMode()
        {
            // Positive sets
            Assert.Equal(0, AtlasTextureFactory.GetSourceTextureCoordinate(0, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(5, AtlasTextureFactory.GetSourceTextureCoordinate(5, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(9, 10, TextureAddressMode.MirrorOnce));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(10, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(11, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(12, 10, TextureAddressMode.MirrorOnce));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(20, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(21, 10, TextureAddressMode.MirrorOnce));

            // Negative Sets
            Assert.Equal(1, AtlasTextureFactory.GetSourceTextureCoordinate(-1, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(2, AtlasTextureFactory.GetSourceTextureCoordinate(-2, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(3, AtlasTextureFactory.GetSourceTextureCoordinate(-3, 10, TextureAddressMode.MirrorOnce));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(-9, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(-10, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(-11, 10, TextureAddressMode.MirrorOnce));

            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(-20, 10, TextureAddressMode.MirrorOnce));
            Assert.Equal(9, AtlasTextureFactory.GetSourceTextureCoordinate(-21, 10, TextureAddressMode.MirrorOnce));
        }

        [Fact]
        public void TestImageCreationGetAndSet()
        {
            const int width = 256;
            const int height = 128;

            var source = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);

            Assert.Equal(source.TotalSizeInBytes, PixelFormat.R8G8B8A8_UNorm.SizeInBytes() * width * height);
            Assert.Equal(1, source.PixelBuffer.Count);

            Assert.Equal(1, source.Description.MipLevels);
            Assert.Equal(1, source.Description.ArraySize);

            Assert.Equal(width * height * 4,
                source.PixelBuffer[0].Width * source.PixelBuffer[0].Height * source.PixelBuffer[0].PixelSize);

            // Set Pixel
            var pixelBuffer = source.PixelBuffer[0];
            pixelBuffer.SetPixel(0, 0, (byte)255);

            // Get Pixel
            var fromPixels = pixelBuffer.GetPixels<byte>();
            Assert.Equal(255, fromPixels[0]);

            // Dispose images
            source.Dispose();
        }

        [Fact]
        public void TestImageDataPointerManipulation()
        {
            const int width = 256;
            const int height = 128;

            var source = Image.New2D(width, height, 1, PixelFormat.R8G8B8A8_UNorm);

            Assert.Equal(source.TotalSizeInBytes, PixelFormat.R8G8B8A8_UNorm.SizeInBytes() * width * height);
            Assert.Equal(1, source.PixelBuffer.Count);

            Assert.Equal(1, source.Description.MipLevels);
            Assert.Equal(1, source.Description.ArraySize);

            Assert.Equal(width * height * 4,
                source.PixelBuffer[0].Width * source.PixelBuffer[0].Height * source.PixelBuffer[0].PixelSize);

            unsafe
            {
                var ptr = (Color*)source.DataPointer;

                // Clean the data
                for (var i = 0; i < source.PixelBuffer[0].Height * source.PixelBuffer[0].Width; ++i)
                    ptr[i] = Color.Transparent;

                // Set a specific pixel to red
                ptr[0] = Color.Red;
            }

            var pixelBuffer = source.PixelBuffer[0];

            // Get Pixel
            var fromPixels = pixelBuffer.GetPixels<Color>();
            Assert.Equal(Color.Red, fromPixels[0]);

            // Dispose images
            source.Dispose();
        }

        [Fact]
        public void TestCreateTextureAtlasToOutput()
        {
            var textureElements = new List<AtlasTextureElement>
            {
                CreateElement("MediumPurple", 130, 158, 10, TextureAddressMode.Border, Color.MediumPurple, Color.SteelBlue),
                CreateElement("Red", 127, 248, 10, TextureAddressMode.Border, Color.Red, Color.SteelBlue),
                CreateElement("Blue", 212, 153, 10, TextureAddressMode.Border, Color.Blue, Color.SteelBlue),
                CreateElement("Gold", 78, 100, 10, TextureAddressMode.Border, Color.Gold, Color.SteelBlue),
                CreateElement("RosyBrown", 78, 100, 10, TextureAddressMode.Border, Color.RosyBrown, Color.SteelBlue),
                CreateElement("SaddleBrown", 400, 100, 10, TextureAddressMode.Border, Color.SaddleBrown, Color.SteelBlue),
                CreateElement("Salmon", 400, 200, 10, TextureAddressMode.Border, Color.Salmon, Color.SteelBlue),
                CreateElement("PowderBlue", 190, 200, 10, TextureAddressMode.Border, Color.PowderBlue, Color.SteelBlue),
                CreateElement("Orange", 200, 230, 10, TextureAddressMode.Border, Color.Orange, Color.SteelBlue),
                CreateElement("Silver", 100, 170, 10, TextureAddressMode.Border, Color.Silver, Color.SteelBlue),
                CreateElement("SlateGray", 100, 170, 10, TextureAddressMode.Border, Color.SlateGray, Color.SteelBlue),
                CreateElement("Tan", 140, 110, 10, TextureAddressMode.Border, Color.Tan, Color.SteelBlue),
            };

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = true,
                MaxHeight = 1024,
                MaxWidth = 1024
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            Assert.Single(textureAtlases);

            if (!texturePacker.AllowNonPowerOfTwo)
            {
                Assert.True(MathUtil.IsPow2(textureAtlases[0].Width));
                Assert.True(MathUtil.IsPow2(textureAtlases[0].Height));
            }

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);
            SaveAndCompareTexture(atlasTexture, "TestCreateTextureAtlasToOutput");

            Assert.Equal(textureAtlases[0].Width, atlasTexture.Description.Width);
            Assert.Equal(textureAtlases[0].Height, atlasTexture.Description.Height);

            atlasTexture.Dispose();

            foreach (var texture in textureAtlases.SelectMany(textureAtlas => textureAtlas.Textures))
            {
                texture.Texture.Dispose();
            }
        }

        [Fact]
        public void TestLoadImagesToCreateAtlas()
        {
            var textureElements = new List<AtlasTextureElement>();

            for (var i = 0; i < 8; ++i)
                textureElements.Add(CreateElementFromFile("image" + i, 100, TextureAddressMode.Wrap, TextureAddressMode.Border));

            for (var i = 0; i < 8; ++i)
                textureElements.Add(CreateElementFromFile("image" + i, 100, TextureAddressMode.Mirror, TextureAddressMode.Clamp));

            var texturePacker = new TexturePacker
            {
                AllowMultipack = false,
                AllowRotation = false,
                MaxHeight = 2048,
                MaxWidth = 2048
            };

            var canPackAllTextures = texturePacker.PackTextures(textureElements);

            Assert.True(canPackAllTextures);

            // Obtain texture atlases
            var textureAtlases = texturePacker.AtlasTextureLayouts;

            // Create atlas texture
            var atlasTexture = AtlasTextureFactory.CreateTextureAtlas(textureAtlases[0], false);

            SaveAndCompareTexture(atlasTexture, "TestLoadImagesToCreateAtlas", ImageFileType.Dds);
            atlasTexture.Dispose();

            foreach (var texture in textureAtlases.SelectMany(textureAtlas => textureAtlas.Textures))
                texture.Texture.Dispose();
        }

        private Image LoadImage(TextureTool texTool, UFile sourcePath)
        {
            using (var texImage = texTool.Load(sourcePath, false))
            {
                // Decompresses the specified texImage
                texTool.Decompress(texImage, false);

                if (texImage.Format == PixelFormat.B8G8R8A8_UNorm)
                    texTool.SwitchChannel(texImage);

                return texTool.ConvertToStrideImage(texImage);
            }
        }
    }
}
