// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.TextureConverter.Tests
{
    public class TextureToolTest : IDisposable
    {
        TextureTool texTool = new TextureTool();

        public void Dispose()
        {
            texTool.Dispose();
        }

        [Fact(Skip = "Need check")]
        public void LoadTest()
        {
            TexImage img;

            img = texTool.Load(Module.PathToInputImages + "stones.png");
            Assert.True(img.ArraySize == 1);
            Assert.True(img.Width == 512);
            Assert.True(img.Height == 512);
            Assert.True(img.Depth == 1);
            Assert.True(img.Format == PixelFormat.B8G8R8A8_UNorm);
            img.Dispose();

            try
            {
                img = texTool.Load(Module.PathToInputImages + "elina.pkm");
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }
        }

        [Fact(Skip = "Need check")]
        public void DecompressTest()
        {
            TexImage img;
            int mipmapCount, arraySize, width, height, depth, subImageArrayLenght;

            // ------------------- Test with BC3 image -------------------
            img = texTool.Load(Module.PathToInputImages + "TextureArray_WMipMaps_BC3.dds");
            Assert.True(img.Format == PixelFormat.BC3_UNorm);
            mipmapCount = img.MipmapCount;
            arraySize = img.ArraySize;
            width = img.Width;
            height = img.Height;
            depth = img.Depth;
            subImageArrayLenght = img.SubImageArray.Length;

            texTool.Decompress(img, false);
            Assert.True(img.Format == PixelFormat.R8G8B8A8_UNorm);
            Assert.True(mipmapCount == img.MipmapCount);
            Assert.True(arraySize == img.ArraySize);
            Assert.True(width == img.Width);
            Assert.True(height == img.Height);
            Assert.True(depth == img.Depth);
            Assert.True(subImageArrayLenght == img.SubImageArray.Length);

            Assert.Equal(TestTools.GetInstance().Checksum["DecompressTest_TextureArray_WMipMaps_BC3.dds"], TestTools.ComputeSHA1(img.Data, img.DataSize));
            img.Dispose();

            // ------------------- Test with uncompress image -------------------
            img = texTool.Load(Module.PathToInputImages + "stones.png");
            texTool.Decompress(img, false);
            Assert.True(img.Format == PixelFormat.B8G8R8A8_UNorm); //FITexLibrary loads image in BGRA order...
            img.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("stones.png", Orientation.Horizontal)]
        [InlineData("TextureArray_WMipMaps_BC3.dds", Orientation.Horizontal)]
        [InlineData("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", Orientation.Vertical)]
        public void FlipTest(string file, Orientation orientation)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);

            texTool.Flip(image, orientation);
            image.Update();

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Flip_" + orientation + "_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_Flip_" + orientation + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WOMipMaps_BC3.dds")]
        public void PreMultiplyAlphaTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);

            texTool.PreMultiplyAlpha(image);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_PreMultiplyAlpha_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_PreMultiplyAlpha_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("stones.png", PixelFormat.BC3_UNorm)]
        [InlineData("TextureArray_WMipMaps_BC3.dds", PixelFormat.BC3_UNorm)]
        public void CompressTest(string filename, PixelFormat format)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + filename);
            texTool.Compress(image, format);
            Assert.True(image.Format == format);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Compress_" + format + "_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_Compress_" + format + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("stones.png", Filter.MipMapGeneration.Box)]
        [InlineData("TextureArray_WMipMaps_BC3.dds", Filter.MipMapGeneration.Linear)]
        public void GenerateMipMapTest(string file, Filter.MipMapGeneration filter)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);
            texTool.GenerateMipMaps(image, filter);
            Assert.True(image.MipmapCount > 1);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_GenerateMipMap_" + filter + "_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_GenerateMipMap_" + filter + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void CorrectGammaTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);
            texTool.CorrectGamma(image, 1/2.2);
            image.Update();

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_CorrectGamma_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_CorrectGamma_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void ConvertToStrideImageTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);
            var sd = texTool.ConvertToStrideImage(image);
            Assert.True(sd.TotalSizeInBytes == image.DataSize);
            Assert.True(sd.Description.MipLevels == image.MipmapCount);
            image.Dispose();
            sd.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void LoadStrideImageTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);

            var sd = texTool.ConvertToStrideImage(image);

            TexImage sdImage = texTool.Load(sd, false);

            Assert.True(image.Equals(sdImage));

            sd.Dispose();
            image.Dispose();
            sdImage.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void GenerateNormalMapTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);
            var normal = texTool.GenerateNormalMap(image, 0.5f);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_GenerateNormalMap_" + image.Name], TestTools.ComputeSHA1(normal.Data, normal.DataSize));
            //Console.WriteLine("TextureTool_GenerateNormalMap_" + image.Name + "." + TestTools.ComputeSHA1(normal.Data, normal.DataSize));

            normal.Dispose();
            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void RescaleTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);
            int width = image.Width;
            int height = image.Height;
            Assert.True(image.MipmapCount > 1);

            texTool.Rescale(image, 0.5f, 0.5f, Filter.Rescaling.Bicubic);
            Assert.True(image.Width == width / 2);
            Assert.True(image.Height == height / 2);
            Assert.True(image.MipmapCount == 1);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Rescale_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_Rescale_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void ResizeTest(string file)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + file);
            int width = image.Width;
            int height = image.Height;
            Assert.True(image.MipmapCount > 1);

            texTool.Resize(image, width/2, height/2, Filter.Rescaling.Bicubic);
            Assert.True(image.Width == width / 2);
            Assert.True(image.Height == height / 2);
            Assert.True(image.MipmapCount == 1);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Rescale_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_Rescale_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        public void SwitchChannelTest(string file)
        {
            var image = texTool.Load(Module.PathToInputImages + file);
            var isInBgraOrder = image.Format.IsBGRAOrder();

            texTool.SwitchChannel(image);
            image.Update();

            Assert.True(isInBgraOrder != image.Format.IsBGRAOrder());

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_SwitchChannel_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
            //Console.WriteLine("TextureTool_SwitchChannel_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds", ".pvr", PixelFormat.None, 16)]
        [InlineData("TextureArray_WMipMaps_BC3.dds", ".pvr", PixelFormat.ETC2_RGBA, 0)]
        [InlineData("TextureArray_WMipMaps_BC3.dds", ".pvr", PixelFormat.None, 0)]
        public void SaveTest(string input, string extension, PixelFormat compressionFormat, int minimumMipmapSize)
        {
            TexImage image = texTool.Load(Module.PathToInputImages + input);

            string output = Path.GetFileNameWithoutExtension(input) + extension;

            if (compressionFormat == PixelFormat.None)
            {
                texTool.Save(image, Module.PathToOutputImages + output, minimumMipmapSize);
            }
            else
            {
                texTool.Save(image, Module.PathToOutputImages + output, compressionFormat, minimumMipmapSize);
            }

            Assert.True(File.Exists(Module.PathToOutputImages + output));
            var loaded = texTool.Load(Module.PathToOutputImages + output);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Save_" + compressionFormat + "_" + minimumMipmapSize + "_" + loaded.Name], TestTools.ComputeSHA1(loaded.Data, loaded.DataSize));
            //Console.WriteLine("TextureTool_Save_" + compressionFormat + "_" + minimumMipmapSize + "_" + loaded.Name + "." + TestTools.ComputeSHA1(loaded.Data, loaded.DataSize));

            File.Delete(Module.PathToOutputImages + output);

            loaded.Dispose();
            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureCube_WMipMaps_BC3.dds", ".pvr", Filter.Rescaling.CatmullRom, PixelFormat.ETC2_RGBA)]
        [InlineData("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", ".dds", Filter.Rescaling.Nearest, PixelFormat.BC3_UNorm)]
        [InlineData("TextureCube_WMipMaps_ATC_RGBA_Explicit.sd", ".dds", Filter.Rescaling.Lanczos3, PixelFormat.BC3_UNorm)]
        [InlineData("duck.jpg", ".dds", Filter.Rescaling.Box, PixelFormat.BC3_UNorm)]
        public void ProcessingTest(string source, string extension, Filter.Rescaling rescaleFiler, PixelFormat format)
        {
            var image = texTool.Load(Module.PathToInputImages + source);

            texTool.CorrectGamma(image, 2.2);

            texTool.Rescale(image, 0.5f, 0.5f, rescaleFiler);

            texTool.GenerateMipMaps(image, Filter.MipMapGeneration.Box);

            var normalMap = texTool.GenerateNormalMap(image, 4);

            texTool.CorrectGamma(normalMap, 1/2.2);

            string output = "TextureTool_ProcessingTest_NormalMap" + rescaleFiler + "_" + format + "_" + source + extension;
            texTool.Save(normalMap, Module.PathToOutputImages + output, format, normalMap.Width / 2);
            normalMap.Dispose();

            Assert.Equal(TestTools.GetInstance().Checksum[output], TestTools.ComputeSHA1(Module.PathToOutputImages + output));
            //Console.WriteLine(output + "." + TestTools.ComputeSHA1(Module.PathToOutputImages + output));
            File.Delete(Module.PathToOutputImages + output);

            texTool.Flip(image, Orientation.Horizontal);

            texTool.CorrectGamma(image, 1/2.2);

            output = "TextureTool_ProcessingTest_" + rescaleFiler + "_" + format + "_" + source + extension;
            texTool.Save(image, Module.PathToOutputImages + output, format, 4);
            image.Dispose();

            Assert.Equal(TestTools.GetInstance().Checksum[output], TestTools.ComputeSHA1(Module.PathToOutputImages + output));
            //Console.WriteLine(output + "." + TestTools.ComputeSHA1(Module.PathToOutputImages + output));
            File.Delete(Module.PathToOutputImages + output);

            image.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void CreateAtlasTest()
        {
            string[] fileList = Directory.GetFiles(Module.PathToInputImages + "atlas/");
            var list = new List<TexImage>(fileList.Length);

            foreach (string filePath in fileList)
            {
                list.Add(texTool.Load(filePath));
            }

            var atlas = texTool.CreateAtlas(list);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_CreateAtlas"], TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));
            //Console.WriteLine("TextureTool_CreateAtlas." + TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));

            atlas.Dispose();

            var another = texTool.Load(fileList[fileList.Length - 1]);
            texTool.Compress(another, PixelFormat.BC3_UNorm);
            list.Add(another);

            try
            {
                atlas = texTool.CreateAtlas(list);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas_WMipMaps.dds", "stones.png")]
        public void ExtractAtlasTest(string atlasFile, string textureName)
        {
            var atlas = texTool.LoadAtlas(Module.PathToInputImages + atlasFile);
            var extracted = texTool.Extract(atlas, textureName, 16);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_ExtractAtlas_" + atlasFile + "_" + textureName], TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));
            //Console.WriteLine("TextureTool_ExtractAtlas_" + atlasFile + "_" + textureName + "." + TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));

            extracted.Dispose();
            atlas.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void ExtractAtlasFailTest()
        {
            var atlas = texTool.LoadAtlas(Module.PathToInputImages + "atlas_WMipMaps.dds");

            try
            {
                var extracted = texTool.Extract(atlas, "coucoucoucoucoucoucoucoucoucoucoucou", 16);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            atlas.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas_WMipMaps.dds")]
        public void ExtractAllAtlasTest(string atlasFile)
        {
            string[] fileList = Directory.GetFiles(Module.PathToInputImages + "atlas/");
            var list = new List<TexImage>(fileList.Length);

            foreach (string filePath in fileList)
            {
                list.Add(texTool.Load(filePath));
            }

            var atlas = texTool.CreateAtlas(list);

            var extracted = texTool.ExtractAll(atlas);

            Assert.True(extracted.Count == list.Count);

            foreach (var image in extracted)
            {
                Assert.Equal(TestTools.GetInstance().Checksum["ExtractAll_" + image.Name], TestTools.ComputeSHA1(image.Data, image.DataSize));
                image.Dispose();
            }

            atlas.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }

        [Theory(Skip = "Need check")]
        [InlineData("atlas_WMipMaps.dds", "atlas/square256_2.png")]
        public void UpdateAtlasTest(string atlasFile, string textureName)
        {
            var atlas = texTool.LoadAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages + Path.GetFileNameWithoutExtension(atlasFile) + TexAtlas.TexLayout.Extension), Module.PathToInputImages + atlasFile);
            var updateTexture = texTool.Load(Module.PathToInputImages + textureName);

            texTool.Update(atlas, updateTexture);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_UpdateAtlas_" + atlasFile + "_" + Path.GetFileName(textureName)], TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));
            //Console.WriteLine("TextureTool_UpdateAtlas_" + atlasFile + "_" + Path.GetFileName(textureName) + "." + TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));

            atlas.Dispose();
            updateTexture.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void UpdateAtlasFailTest()
        {
            var atlas = texTool.LoadAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages + Path.GetFileNameWithoutExtension("atlas_WMipMaps.dds") + TexAtlas.TexLayout.Extension), Module.PathToInputImages + "atlas_WMipMaps.dds");
            var updateTexture = texTool.Load(Module.PathToInputImages + "atlas/square256_2.png");

            try
            {
                updateTexture.Name = "coucoucoucoucoucoucoucoucoucoucoucou";
                texTool.Update(atlas, updateTexture);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            try
            {
                texTool.Update(atlas, updateTexture, "coucoucoucoucoucoucoucoucoucoucoucou");
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }
        }

        [Theory(Skip = "Need check")]
        [InlineData("atlas/stones256.png", "atlas/square256.png")]
        public void CreateArrayTest(string file1, string file2)
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 5; ++i)
            {
                list.Add(texTool.Load(Module.PathToInputImages + file1));
                list.Add(texTool.Load(Module.PathToInputImages + file2));
            }

            var array = texTool.CreateTextureArray(list);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_CreateArray_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2)], TestTools.ComputeSHA1(array.Data, array.DataSize));
            //Console.WriteLine("TextureTool_CreateArray_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2) + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));

            array.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Fact(Skip = "Need check")]
        public void CreateArrayFailTest()
        {
            var list = new List<TexImage>();
            list.Add(texTool.Load(Module.PathToInputImages + "atlas/stones256.png"));

            var other = texTool.Load(Module.PathToInputImages + "atlas/stones256.png");
            texTool.Rescale(other, 0.5f, 0.5f, Filter.Rescaling.Bilinear);
            list.Add(other);

            try
            {
                var array = texTool.CreateTextureArray(list);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas/stones256.png", "atlas/square256.png")]
        public void CreateCubeTest(string file1, string file2)
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 3; ++i)
            {
                list.Add(texTool.Load(Module.PathToInputImages + file1));
                list.Add(texTool.Load(Module.PathToInputImages + file2));
            }

            var array = texTool.CreateTextureCube(list);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_CreateCube_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2)], TestTools.ComputeSHA1(array.Data, array.DataSize));
            //Console.WriteLine("TextureTool_CreateCube_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2) + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));

            array.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Fact(Skip = "Need check")]
        public void CreateCubeFailTest()
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 7; ++i)
            {
                list.Add(texTool.Load(Module.PathToInputImages + "atlas/stones256.png"));
            }

            try
            {
                var array = texTool.CreateTextureCube(list);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Theory(Skip = "Need check")]
        [InlineData("array_WMipMaps.dds", 4)]
        public void ExtractTest(string arrayFile, int indice)
        {
            TexImage array = texTool.Load(Module.PathToInputImages + arrayFile);

            var extracted = texTool.Extract(array, indice);

            //Console.WriteLine("TextureTool_Extract_" + indice + "_" + arrayFile + "." + TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Extract_" + indice + "_" + arrayFile], TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));

            extracted.Dispose();
            array.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void ExtractFailTest()
        {
            TexImage array = texTool.Load(Module.PathToInputImages + "array_WMipMaps.dds");

            try
            {
                var extracted = texTool.Extract(array, array.ArraySize);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            array.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas/stones256.png", "atlas/square256.png")]
        public void ExtractAllTest(string file1, string file2)
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 5; ++i)
            {
                list.Add(texTool.Load(Module.PathToInputImages + file1));
                //Console.WriteLine("ArrayTexLibrary_ExtractAll_" + Path.GetFileName(file1) + "." + TestTools.ComputeSHA1(temp.Data, temp.DataSize));

                list.Add(texTool.Load(Module.PathToInputImages + file2));
                //Console.WriteLine("ArrayTexLibrary_ExtractAll_" + Path.GetFileName(file2) + "." + TestTools.ComputeSHA1(temp.Data, temp.DataSize));
            }

            var array = texTool.CreateTextureArray(list);

            var extracted = texTool.ExtractAll(array);

            Assert.True(list.Count == extracted.Count);

            for (int i = 0; i < array.ArraySize; ++i)
            {
                Assert.Equal(TestTools.GetInstance().Checksum["ExtractAll_" + list[i].Name], TestTools.ComputeSHA1(extracted[i].Data, extracted[i].DataSize));
                extracted[i].Dispose();
            }

            array.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }

        [Theory(Skip = "Need check")]
        [InlineData("array_WMipMaps.dds", "atlas/square256.png", 3)]
        public void InsertTest(string arrayFile, string newTexture, int indice)
        {
            var array = texTool.Load(Module.PathToInputImages + arrayFile);
            var texture = texTool.Load(Module.PathToInputImages + newTexture);
            texTool.Compress(texture, PixelFormat.BC3_UNorm);

            texTool.Insert(array, texture, indice);

            //Console.WriteLine("TextureTool_Insert_" + indice + "_" + Path.GetFileName(newTexture) + "_" + arrayFile + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Insert_" + indice + "_" + Path.GetFileName(newTexture) + "_" + arrayFile], TestTools.ComputeSHA1(array.Data, array.DataSize));

            try
            {
                texTool.Insert(array, texture, array.ArraySize+1);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            array.Dispose();
            texture.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("array_WMipMaps.dds", 3)]
        public void RemoveTest(string arrayFile, int indice)
        {
            var array = texTool.Load(Module.PathToInputImages + arrayFile);

            texTool.Remove(array, indice);

            //Console.WriteLine("TextureTool_Remove_" + indice + "_" + arrayFile + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_Remove_" + indice + "_" + arrayFile], TestTools.ComputeSHA1(array.Data, array.DataSize));

            try
            {
                texTool.Remove(array, array.ArraySize);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            array.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("array_WMipMaps.dds", "atlas/square256_2.png", 0)]
        public void UpdateArrayTest(string arrayFile, string textureName, int indice)
        {
            var array = texTool.Load(Module.PathToInputImages + arrayFile);
            var updateTexture = texTool.Load(Module.PathToInputImages + textureName);

            texTool.Update(array, updateTexture, indice);

            Assert.Equal(TestTools.GetInstance().Checksum["TextureTool_UpdateArray_" + arrayFile + "_" + indice + "_" + Path.GetFileName(textureName)], TestTools.ComputeSHA1(array.Data, array.DataSize));
            //Console.WriteLine("TextureTool_UpdateArray_" + arrayFile + "_" + indice + "_" + Path.GetFileName(textureName) + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));

            array.Dispose();
            updateTexture.Dispose();

            try
            {
                texTool.Update(array, updateTexture, array.ArraySize);
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void PickColorTest()
        {
            var pixelCoordinate1 = new Int2(4, 4);
            var theoreticalColor1 = new Color(0, 255, 46, 255);
            var pixelCoordinate2 = new Int2(3, 4);
            var theoreticalColor2 = new Color(222, 76, 255, 255);

            var images = new[] { "BgraSheet.dds", "RgbaSheet.dds" };

            foreach (var image in images)
            {
                using (var texTool = new TextureTool())
                using (var texImage = texTool.Load(Module.PathToInputImages + image))
                {
                    var foundColor = texTool.PickColor(texImage, pixelCoordinate1);
                    Assert.Equal(theoreticalColor1, foundColor);

                    foundColor = texTool.PickColor(texImage, pixelCoordinate2);
                    Assert.Equal(theoreticalColor2, foundColor);
                }
            }
        }

        private class AlphaLevelTest
        {
            public Rectangle Region;

            public Color? TransparencyColor;

            public AlphaLevels ExpectedResult;

            public AlphaLevelTest(Rectangle region, Color? transparencyColor, AlphaLevels expectedResult)
            {
                Region = region;
                TransparencyColor = transparencyColor;
                ExpectedResult = expectedResult;
            }
        };

        [Fact]
        public void GetAlphaLevelTests()
        {
            var testEntries = new List<AlphaLevelTest>
            {
                // transparency color tests
                new AlphaLevelTest(new Rectangle(12, 12, 18, 18), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(11, 12, 18, 18), new Color(255, 81, 237, 255), AlphaLevels.MaskAlpha),

                // last pixel test
                new AlphaLevelTest(new Rectangle(52, 54, 12, 10), new Color(255, 81, 237, 255), AlphaLevels.MaskAlpha),
                
                // region out of bound tests
                new AlphaLevelTest(new Rectangle(120, 12, 18, 18), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(12, 120, 18, 18), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(120, 120, 18, 18), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(51, 56, 10, 180), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(51, 56, 100, 7), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(51, 56, 100, 70), new Color(255, 81, 237, 255), AlphaLevels.MaskAlpha),

                // all image test
                new AlphaLevelTest(new Rectangle(0, 0, 64, 64), new Color(255, 81, 237, 255), AlphaLevels.MaskAlpha),

                // single pixel tests
                new AlphaLevelTest(new Rectangle(0, 0, 1, 1), new Color(255, 81, 237, 255), AlphaLevels.MaskAlpha),
                new AlphaLevelTest(new Rectangle(12, 12, 1, 1), new Color(255, 81, 237, 255), AlphaLevels.NoAlpha),

                // normal transparency channel tests
                new AlphaLevelTest(new Rectangle(0, 0, 5, 6), null, AlphaLevels.NoAlpha),
                new AlphaLevelTest(new Rectangle(12, 12, 18, 18), null, AlphaLevels.InterpolatedAlpha),
                new AlphaLevelTest(new Rectangle(1, 30, 5, 14), null, AlphaLevels.MaskAlpha),
                new AlphaLevelTest(new Rectangle(1, 30, 6, 14), null, AlphaLevels.InterpolatedAlpha),
                new AlphaLevelTest(new Rectangle(6, 30, 6, 14), null, AlphaLevels.InterpolatedAlpha),
                new AlphaLevelTest(new Rectangle(1, 47, 5, 14), null, AlphaLevels.MaskAlpha),
                new AlphaLevelTest(new Rectangle(1, 47, 6, 14), null, AlphaLevels.InterpolatedAlpha),
                new AlphaLevelTest(new Rectangle(6, 47, 6, 14), null, AlphaLevels.InterpolatedAlpha)
            };

            var images = new[] { "TransparentRGBA.dds", "TransparentBGRA.dds" };

            foreach (var image in images)
            {
                using (var texTool = new TextureTool())
                using (var texImage = texTool.Load(Module.PathToInputImages + image))
                {
                    foreach (var entry in testEntries)
                    {
                        var result = texTool.GetAlphaLevels(texImage, entry.Region, entry.TransparencyColor);
                        Assert.Equal(entry.ExpectedResult, result);
                    }
                }
            }
        }
    }
}
