// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;
using Xenko.TextureConverter.Requests;
using Xenko.TextureConverter.TexLibraries;

namespace Xenko.TextureConverter.Tests
{
    public class AtitcTexLibraryTest : IDisposable
    {
        private readonly AtitcTexLibrary library = new AtitcTexLibrary();
        private readonly XenkoTexLibrary paraLib = new XenkoTexLibrary();

        public AtitcTexLibraryTest()
        {
            library = new AtitcTexLibrary();
            paraLib = new XenkoTexLibrary();
            Assert.False(library.SupportBGRAOrder());
        }

        public void Dispose()
        {
            library.Dispose();
            paraLib.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        public void StartLibraryTest(string file)
        {
            TexImage image = LoadInput(file);

            TexLibraryTest.StartLibraryTest(image, library);

            AtitcTextureLibraryData libraryData = (AtitcTextureLibraryData)image.LibraryData[library];
            Assert.True(libraryData.Textures.Length == image.SubImageArray.Length);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        public void EndLibraryTest(string file)
        {
            TexImage image = LoadInput(file);

            IntPtr buffer;

            buffer = image.SubImageArray[0].Data;
            library.Execute(image, new DecompressingRequest(false));

            Assert.True(image.Format == Xenko.Graphics.PixelFormat.R8G8B8A8_UNorm); // The images features are updated with the call to Execute
            Assert.True(image.SubImageArray[0].Data == buffer); // The sub images are only updated on the call to EndLibrary

            library.EndLibrary(image);

            Assert.True(image.SubImageArray[0].Data != buffer);

            image.Dispose();
        }

        [Fact(Skip = "Need check")]
        public void CanHandleRequestTest()
        {
            TexImage image = LoadInput("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk");

            Assert.True(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.True(library.CanHandleRequest(image, new CompressingRequest(Xenko.Graphics.PixelFormat.ATC_RGBA_Explicit)));
            Assert.False(library.CanHandleRequest(image, new CompressingRequest(Xenko.Graphics.PixelFormat.BC3_UNorm)));

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureCube_WMipMaps_ATC_RGBA_Explicit.xk")]
        public void DecompressTest(string file)
        {
            TexImage image = LoadInput(file);

            TexLibraryTest.DecompressTest(image, library);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMap_RGBA8888.xk", Xenko.Graphics.PixelFormat.ATC_RGBA_Explicit)]
        [InlineData("TextureArray_WMipMaps_RGBA8888.xk", Xenko.Graphics.PixelFormat.ATC_RGBA_Interpolated)]
        [InlineData("TextureCube_WMipMaps_RGBA8888.xk", Xenko.Graphics.PixelFormat.ATC_RGBA_Explicit)]
        public void CompressTest(string file, Xenko.Graphics.PixelFormat format)
        {
            TexImage image = LoadInput(file);

            TexLibraryTest.CompressTest(image, library, format);

            image.Dispose();
        }

        private TexImage LoadInput(string file)
        {
            var image = TestTools.Load(paraLib, file);
            library.StartLibrary(image);
            return image;
        }
    }
}
