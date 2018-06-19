// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.TextureConverter.Requests;
using Xenko.TextureConverter.TexLibraries;

namespace Xenko.TextureConverter.Tests
{
    [TestFixture]
    class XenkoTexLibraryTest
    {
        XenkoTexLibrary library;

        [TestFixtureSetUp]
        public void TestSetUp()
        {
            library = new XenkoTexLibrary();
            Assert.IsTrue(library.SupportBGRAOrder());
        }

        [TestFixtureTearDown]
        public void TestTearDown()
        {
            library.Dispose();
        }


        [Ignore("Need check")]
        [TestCase("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [TestCase("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [TestCase("TextureCube_WMipMaps_RGBA8888.xk")]
        public void StartLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.StartLibraryTest(image, library);

            image.Dispose();
        }


        [Test, Ignore("Need check")]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "Texture3D_WMipMaps_ATC_RGBA_Explicit.xk");
            Assert.IsFalse(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.IsFalse(library.CanHandleRequest(image, new LoadingRequest(new TexImage(), false)));
            Assert.IsTrue(library.CanHandleRequest(image, new LoadingRequest(Xenko.Graphics.Image.New1D(5, 0, Xenko.Graphics.PixelFormat.ATC_RGBA_Explicit), false)));
            Assert.IsTrue(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_BC3.dds", false)));
            Assert.IsTrue(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_BC3.xk", 0)));
            Assert.IsTrue(library.CanHandleRequest(image, new ExportToXenkoRequest()));
            image.Dispose();
        }


        [Ignore("Need check")]
        [TestCase("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [TestCase("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [TestCase("TextureCube_WMipMaps_RGBA8888.xk")]
        public void ExportTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportTest(image, library, file);

            image.Dispose();
        }

        [Ignore("Need check")]
        [TestCase("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk", 4)]
        [TestCase("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk", 512)]
        [TestCase("TextureCube_WMipMaps_RGBA8888.xk", 16)]
        public void ExportTest(string file, int mipMipMapSize)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportMinMipMapTest(image, library, mipMipMapSize);

            image.Dispose();
        }

        [Ignore("Need check")]
        [TestCase("Texture3D_WMipMaps_ATC_RGBA_Explicit.xk")]
        [TestCase("TextureArray_WMipMaps_ATC_RGBA_Explicit.xk")]
        [TestCase("TextureCube_WMipMaps_RGBA8888.xk")]
        public void ExportToXenkoTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            ExportToXenkoRequest request = new ExportToXenkoRequest();
            library.Execute(image, request);

            var xk = request.XkImage;

            Assert.IsTrue(xk.TotalSizeInBytes == image.DataSize);
            Assert.IsTrue(xk.Description.MipLevels == image.MipmapCount);
            Assert.IsTrue(xk.Description.Width == image.Width);
            Assert.IsTrue(xk.Description.Height == image.Height);

            image.Dispose();
        }
    }
}
