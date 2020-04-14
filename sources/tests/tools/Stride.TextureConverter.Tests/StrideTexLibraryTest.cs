// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Xenko.TextureConverter.Requests;
using Xenko.TextureConverter.TexLibraries;

namespace Xenko.TextureConverter.Tests
{
    public class XenkoTexLibraryTest : IDisposable
    {
        XenkoTexLibrary library;

        public XenkoTexLibraryTest()
        {
            library = new XenkoTexLibrary();
            Assert.True(library.SupportBGRAOrder());
        }

        public void Dispose()
        {
            library.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureCube_WMipMaps_RGBA8888.xk")]
        public void StartLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.StartLibraryTest(image, library);

            image.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "Texture3D_WMipMaps_ATC_RGBA_Explicit.xk");
            Assert.False(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.False(library.CanHandleRequest(image, new LoadingRequest(new TexImage(), false)));
            Assert.True(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_BC3.dds", false)));
            Assert.True(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_BC3.xk", 0)));
            Assert.True(library.CanHandleRequest(image, new ExportToXenkoRequest()));
            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureCube_WMipMaps_RGBA8888.xk")]
        public void ExportTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportTest(image, library, file);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk", 4)]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk", 512)]
        [InlineData("TextureCube_WMipMaps_RGBA8888.xk", 16)]
        public void ExportTestMinMipmap(string file, int minMipMapSize)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportMinMipMapTest(image, library, minMipMapSize);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [InlineData("TextureCube_WMipMaps_RGBA8888.xk")]
        public void ExportToXenkoTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            ExportToXenkoRequest request = new ExportToXenkoRequest();
            library.Execute(image, request);

            var xk = request.XkImage;

            Assert.True(xk.TotalSizeInBytes == image.DataSize);
            Assert.True(xk.Description.MipLevels == image.MipmapCount);
            Assert.True(xk.Description.Width == image.Width);
            Assert.True(xk.Description.Height == image.Height);

            image.Dispose();
        }
    }
}
