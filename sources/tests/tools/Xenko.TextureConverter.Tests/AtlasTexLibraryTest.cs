// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.IO;

using NUnit.Framework;
using Xenko.TextureConverter.Requests;
using Xenko.TextureConverter.TexLibraries;

namespace Xenko.TextureConverter.Tests
{
    [TestFixture]
    class AtlasTexLibraryTest
    {
        AtlasTexLibrary library;
        FITexLib fiLib;
        DxtTexLib dxtLib;

        [TestFixtureSetUp]
        public void TestSetUp()
        {
            library = new AtlasTexLibrary();
            fiLib = new FITexLib();
            dxtLib = new DxtTexLib();
            Assert.IsTrue(library.SupportBGRAOrder());
            library.StartLibrary(new TexAtlas());
        }

        [TestFixtureTearDown]
        public void TestTearDown()
        {
            library.Dispose();
            fiLib.Dispose();
            dxtLib.Dispose();
        }


        [Test, Ignore("Need check")]
        public void CanHandleRequestTest()
        {
            TexAtlas atlas = new TexAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages + Path.GetFileNameWithoutExtension("atlas_WMipMaps.dds") + TexAtlas.TexLayout.Extension), TestTools.Load(dxtLib, "atlas_WMipMaps.dds"));
            Assert.IsFalse(library.CanHandleRequest(atlas, new DecompressingRequest(false)));
            Assert.IsTrue(library.CanHandleRequest(atlas, new AtlasCreationRequest(new List<TexImage>())));
            Assert.IsTrue(library.CanHandleRequest(atlas, new AtlasExtractionRequest(0)));
            Assert.IsTrue(library.CanHandleRequest(atlas, new AtlasUpdateRequest(new TexImage(), "")));
            atlas.Dispose();
        }


        [Ignore("Need check")]
        [TestCase("atlas/", false, false)]
        [TestCase("atlas/", false, true)]
        [TestCase("atlas/", true, false)]
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
            Assert.IsTrue(TestTools.ComputeSHA1(atlas.Data, atlas.DataSize).Equals(TestTools.GetInstance().Checksum["AtlasTexLibrary_CreateAtlas_" + generateMipMaps + "_" + forceSquaredAtlas]));

            if(forceSquaredAtlas) Assert.IsTrue(atlas.Width == atlas.Height);

            atlas.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Ignore("Need check")]
        [TestCase("atlas_WMipMaps.dds", "square256.png")]
        public void ExtractTest(string atlasFile, string extractedName)
        {
            TexAtlas atlas = new TexAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages+Path.GetFileNameWithoutExtension(atlasFile) + TexAtlas.TexLayout.Extension), TestTools.Load(dxtLib, atlasFile));

            var request = new AtlasExtractionRequest(extractedName, 16);
            library.Execute(atlas, request);
            atlas.CurrentLibrary = library;

            var extracted = request.Texture;

            string nameWOExtension = Path.GetFileNameWithoutExtension(extractedName);

            //Console.WriteLine("AtlasTexLibrary_Extract_" + nameWOExtension + ".dds." + TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));
            Assert.IsTrue(TestTools.ComputeSHA1(extracted.Data, extracted.DataSize).Equals(TestTools.GetInstance().Checksum["AtlasTexLibrary_Extract_" + nameWOExtension + ".dds"]));

            extracted.Dispose();

            atlas.Dispose();
        }


        [Ignore("Need check")]
        [TestCase("atlas/")]
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

            Assert.IsTrue(list.Count == request.Textures.Count);

            foreach (var image in request.Textures)
            {
                Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["ExtractAll_" + image.Name]));
                image.Dispose();
            }

            atlas.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Ignore("Need check")]
        [TestCase("atlas_WOMipMaps.png", "square256_2.png", "atlas/stones256.png")]
        public void UpdateTest(string atlasFile, string textureNameToUpdate, string newTexture)
        {
            TexAtlas atlas = new TexAtlas(TexAtlas.TexLayout.Import(Module.PathToInputImages + Path.GetFileNameWithoutExtension(atlasFile) + TexAtlas.TexLayout.Extension), TestTools.Load(fiLib, atlasFile));

            var updateTexture = TestTools.Load(fiLib, newTexture);

            library.Execute(atlas, new AtlasUpdateRequest(updateTexture, textureNameToUpdate));
            library.EndLibrary(atlas);

            //Console.WriteLine("AtlasTexLibrary_Update_" + textureNameToUpdate + "_" + atlasFile + "." + TestTools.ComputeSHA1(atlas.Data, atlas.DataSize));
            Assert.IsTrue(TestTools.ComputeSHA1(atlas.Data, atlas.DataSize).Equals(TestTools.GetInstance().Checksum["AtlasTexLibrary_Update_" + textureNameToUpdate + "_" + atlasFile]));

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
