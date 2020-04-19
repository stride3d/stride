// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

using Xunit;
using Stride.TextureConverter.Requests;
using Stride.TextureConverter.TexLibraries;

namespace Stride.TextureConverter.Tests
{
    public class AtlasTexLibraryTest : IDisposable
    {
        private readonly AtlasTexLibrary library = new AtlasTexLibrary();
        private readonly FITexLib fiLib = new FITexLib();
        private readonly DxtTexLib dxtLib = new DxtTexLib();

        public AtlasTexLibraryTest()
        {
            Assert.True(library.SupportBGRAOrder());
            library.StartLibrary(new TexAtlas());
        }

        public void Dispose()
        {
            library.Dispose();
            fiLib.Dispose();
            dxtLib.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void CanHandleRequestTest()
        {
            TexAtlas atlas = new TexAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages + Path.GetFileNameWithoutExtension("atlas_WMipMaps.dds") + TexAtlas.TexLayout.Extension), TestTools.Load(dxtLib, "atlas_WMipMaps.dds"));
            Assert.False(library.CanHandleRequest(atlas, new DecompressingRequest(false)));
            Assert.True(library.CanHandleRequest(atlas, new AtlasCreationRequest(new List<TexImage>())));
            Assert.True(library.CanHandleRequest(atlas, new AtlasExtractionRequest(0)));
            Assert.True(library.CanHandleRequest(atlas, new AtlasUpdateRequest(new TexImage(), "")));
            atlas.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas/", false, false)]
        [InlineData("atlas/", false, true)]
        [InlineData("atlas/", true, false)]
        public void CreateAtlasTest(string directory, bool generateMipMaps, bool forceSquaredAtlas)
        {
            string path = Module.PathToInputImages + directory;
            string[] fileList = Directory.GetFiles(path);
            var list = new List<TexImage>(fileList.Length);

            foreach(string filePath in fileList)
            {
                var temp = Load(fiLib, filePath);
                list.Add(temp);
                if (generateMipMaps)
                {
                    fiLib.EndLibrary(temp);
                    dxtLib.StartLibrary(temp);
                    dxtLib.Execute(temp, new MipMapsGenerationRequest(Filter.MipMapGeneration.Cubic));
                    temp.CurrentLibrary = dxtLib;
                }
            }

            var atlas = new TexAtlas();

            library.Execute(atlas, new AtlasCreationRequest(list, forceSquaredAtlas));

            //Console.WriteLine("AtlasTexLibrary_CreateAtlas_" + generateMipMaps + "_" + forceSquaredAtlas + "." + TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["AtlasTexLibrary_CreateAtlas_" + generateMipMaps + "_" + forceSquaredAtlas], TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));

            if (forceSquaredAtlas) Assert.True(atlas.Width == atlas.Height);

            atlas.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas_WMipMaps.dds", "square256.png")]
        public void ExtractTest(string atlasFile, string extractedName)
        {
            TexAtlas atlas = new TexAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages+Path.GetFileNameWithoutExtension(atlasFile) + TexAtlas.TexLayout.Extension), TestTools.Load(dxtLib, atlasFile));

            var request = new AtlasExtractionRequest(extractedName, 16);
            library.Execute(atlas, request);
            atlas.CurrentLibrary = library;

            var extracted = request.Texture;

            string nameWOExtension = Path.GetFileNameWithoutExtension(extractedName);

            //Console.WriteLine("AtlasTexLibrary_Extract_" + nameWOExtension + ".dds." + TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["AtlasTexLibrary_Extract_" + nameWOExtension + ".dds"], TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));

            extracted.Dispose();

            atlas.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas/")]
        public void ExtractAllTest(string directory)
        {
            string path = Module.PathToInputImages + directory;
            string[] fileList = Directory.GetFiles(path);
            var list = new List<TexImage>(fileList.Length);

            foreach (string filePath in fileList)
            {
                var temp = Load(fiLib, filePath);
                list.Add(temp);
                //Console.WriteLine("ExtractAll_" + Path.GetFileName(filePath) + "." + TestTools.ComputeSHA1(temp.Data, temp.DataSize));
            }

            var atlas = new TexAtlas();

            library.Execute(atlas, new AtlasCreationRequest(list));

            var request = new AtlasExtractionRequest(0);
            library.Execute(atlas, request);
            library.EndLibrary(atlas);

            Assert.True(list.Count == request.Textures.Count);

            foreach (var image in request.Textures)
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
        [InlineData("atlas_WOMipMaps.png", "square256_2.png", "atlas/stones256.png")]
        public void UpdateTest(string atlasFile, string textureNameToUpdate, string newTexture)
        {
            TexAtlas atlas = new TexAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages + Path.GetFileNameWithoutExtension(atlasFile) + TexAtlas.TexLayout.Extension), TestTools.Load(fiLib, atlasFile));

            var updateTexture = TestTools.Load(fiLib, newTexture);

            library.Execute(atlas, new AtlasUpdateRequest(updateTexture, textureNameToUpdate));
            library.EndLibrary(atlas);

            //Console.WriteLine("AtlasTexLibrary_Update_" + textureNameToUpdate + "_" + atlasFile + "." + TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["AtlasTexLibrary_Update_" + textureNameToUpdate + "_" + atlasFile], TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));

            updateTexture.Dispose();
            atlas.Dispose();
        }


        private TexImage Load(ITexLibrary library, string filePath)
        {
            var image = new TexImage();
            library.Execute(image, new LoadingRequest(filePath, false));
            image.Name = Path.GetFileName(filePath);
            image.CurrentLibrary = library;
            return image;
        }
    }
}
