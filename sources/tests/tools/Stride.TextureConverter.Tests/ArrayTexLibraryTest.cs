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
    public class ArrayTexLibraryTest : IDisposable
    {
        private readonly ArrayTexLib library;
        private readonly FITexLib fiLib;
        private readonly DxtTexLib dxtLib;

        public ArrayTexLibraryTest()
        {
            Module.LoadLibraries();
            library = new ArrayTexLib();
            fiLib = new FITexLib();
            dxtLib = new DxtTexLib();
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
            TexImage image = TestTools.Load(dxtLib, "array_WMipMaps.dds");
            Assert.False(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.True(library.CanHandleRequest(image, new ArrayCreationRequest(new List<TexImage>())));
            Assert.True(library.CanHandleRequest(image, new ArrayExtractionRequest(0)));
            Assert.True(library.CanHandleRequest(image, new ArrayUpdateRequest(new TexImage(), 0)));
            Assert.True(library.CanHandleRequest(image, new ArrayElementRemovalRequest(0)));
            Assert.True(library.CanHandleRequest(image, new ArrayInsertionRequest(new TexImage(), 0)));
            Assert.True(library.CanHandleRequest(image, new CubeCreationRequest(new List<TexImage>())));
            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("stones256.png", "square256.png")]
        public void CreateArrayTest(string file1, string file2)
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 5; ++i)
            {
                var temp = new TexImage();
                fiLib.Execute(temp, new LoadingRequest(Module.PathToAtlasImages + file1, false));
                list.Add(temp);

                temp = new TexImage();
                fiLib.Execute(temp, new LoadingRequest(Module.PathToAtlasImages + file2, false));
                list.Add(temp);
            }

            var array = new TexImage();
            library.Execute(array, new ArrayCreationRequest(list));

            Assert.True(array.ArraySize == list.Count);

            //Console.WriteLine("ArrayTexLibrary_CreateArray_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2) + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["ArrayTexLibrary_CreateArray_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2)], TestTools.ComputeSHA1(array.Data, array.DataSize));

            array.Dispose();
            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Theory(Skip = "Need check")]
        [InlineData("array_WMipMaps.dds", 4)]
        public void ExtractTest(string arrayFile, int indice)
        {
            TexImage array = TestTools.Load(dxtLib, arrayFile);

            var request = new ArrayExtractionRequest(indice, 16);
            library.Execute(array, request);
            array.CurrentLibrary = library;

            var extracted = request.Texture;

            //Console.WriteLine("ArrayTexLibrary_Extract_" + arrayFile + "." + TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["ArrayTexLibrary_Extract_" + arrayFile], TestTools.ComputeSHA1(extracted.Data, extracted.DataSize));

            extracted.Dispose();

            array.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("atlas/stones256.png", "atlas/square256.png")]
        public void ExtractAllTest(string file1, string file2)
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 5; ++i)
            {
                var temp = new TexImage();
                fiLib.Execute(temp, new LoadingRequest(Module.PathToInputImages + file1, false));
                temp.Name = Path.GetFileName(file1);
                list.Add(temp);
                //Console.WriteLine("ExtractAll_" + Path.GetFileName(file1) + "." + TestTools.ComputeSHA1(temp.Data, temp.DataSize));

                temp = new TexImage();
                fiLib.Execute(temp, new LoadingRequest(Module.PathToInputImages + file2, false));
                temp.Name = Path.GetFileName(file2);
                list.Add(temp);
                //Console.WriteLine("ExtractAll_" + Path.GetFileName(file2) + "." + TestTools.ComputeSHA1(temp.Data, temp.DataSize));
            }

            var array = new TexImage();
            library.Execute(array, new ArrayCreationRequest(list));

            var request = new ArrayExtractionRequest(0);
            library.Execute(array, request);
            library.EndLibrary(array);

            Assert.True(list.Count == request.Textures.Count);

            for(int i = 0; i < array.ArraySize; ++i)
            {
                var temp = request.Textures[i];
                Assert.Equal(TestTools.GetInstance().Checksum["ExtractAll_" + list[i].Name], TestTools.ComputeSHA1(temp.Data, temp.DataSize));
                temp.Dispose();
            }

            array.Dispose();

            foreach (var image in list)
            {
                image.Dispose();
            }
        }


        [Theory(Skip = "Need check")]
        [InlineData("array_WOMipMaps.dds", 0, "atlas/stones256.png")]
        public void UpdateTest(string arrayFile, int indice, string newTexture)
        {
            TexImage array = TestTools.Load(dxtLib, arrayFile);
            dxtLib.EndLibrary(array);

            var updateTexture = TestTools.Load(fiLib, newTexture);

            library.Execute(array, new ArrayUpdateRequest(updateTexture, indice));
            library.EndLibrary(array);

            //Console.WriteLine("ArrayTexLibrary_Update_" + indice + "_" + arrayFile + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["ArrayTexLibrary_Update_" + indice + "_" + arrayFile], TestTools.ComputeSHA1(array.Data, array.DataSize));

            updateTexture.Dispose();
            array.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("array_WMipMaps.dds", 3)]
        public void RemoveTest(string arrayFile, int indice)
        {
            TexImage array = TestTools.Load(dxtLib, arrayFile);

            int arraySize = array.ArraySize;

            dxtLib.EndLibrary(array);
            library.StartLibrary(array); // for fun cause it's empty
            library.Execute(array, new ArrayElementRemovalRequest(indice));
            array.CurrentLibrary = library;
            array.Update();

            Assert.True(arraySize == array.ArraySize + 1);

            //Console.WriteLine("ArrayTexLibrary_Remove_" + indice + "_" + arrayFile + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["ArrayTexLibrary_Remove_" + indice + "_" + arrayFile], TestTools.ComputeSHA1(array.Data, array.DataSize));

            array.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("array_WOMipMaps.dds", "atlas/square256.png", 3)]
        public void InsertTest(string arrayFile, string newTexture, int indice)
        {
            TexImage array = TestTools.Load(dxtLib, arrayFile);

            int arraySize = array.ArraySize;

            var texture = TestTools.Load(fiLib, newTexture);

            dxtLib.EndLibrary(array);
            library.StartLibrary(array); // for fun cause it's empty
            library.Execute(array, new ArrayInsertionRequest(texture, indice));
            array.CurrentLibrary = library;
            array.Update();

            Assert.True(arraySize == array.ArraySize - 1);

            //Console.WriteLine("ArrayTexLibrary_Insert_" + Path.GetFileName(newTexture) + "_" + indice + "_" + arrayFile + "." + TestTools.ComputeSHA1(array.Data, array.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["ArrayTexLibrary_Insert_" + Path.GetFileName(newTexture) + "_" + indice + "_" + arrayFile], TestTools.ComputeSHA1(array.Data, array.DataSize));

            array.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("stones256.png", "square256.png")]
        public void CreateCubeTest(string file1, string file2)
        {
            var list = new List<TexImage>();
            for (int i = 0; i < 3; ++i)
            {
                var temp = new TexImage();
                fiLib.Execute(temp, new LoadingRequest(Module.PathToAtlasImages + file1, false));
                list.Add(temp);

                temp = new TexImage();
                fiLib.Execute(temp, new LoadingRequest(Module.PathToAtlasImages + file2, false));
                list.Add(temp);
            }

            var cube = new TexImage();
            library.Execute(cube, new CubeCreationRequest(list));

            Assert.True(cube.ArraySize == list.Count);

            //Console.WriteLine("ArrayTexLibrary_CreateCube_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2) + "." + TestTools.ComputeSHA1(cube.Data, cube.DataSize));
            Assert.Equal(TestTools.GetInstance().Checksum["ArrayTexLibrary_CreateCube_" + Path.GetFileName(file1) + "_" + Path.GetFileName(file2)], TestTools.ComputeSHA1(cube.Data, cube.DataSize));

            cube.Dispose();
            foreach (var image in list)
            {
                image.Dispose();
            }
        }
    }
}
