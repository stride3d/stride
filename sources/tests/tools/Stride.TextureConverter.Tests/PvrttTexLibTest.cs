// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;
using Stride.TextureConverter.Requests;
using Stride.TextureConverter.TexLibraries;

namespace Stride.TextureConverter.Tests
{
    public class PvrttTexLibTest : IDisposable
    {
        private readonly PvrttTexLib library = new PvrttTexLib();

        public PvrttTexLibTest()
        {
            Assert.False(library.SupportBGRAOrder());
        }

        public void Dispose()
        {
            library.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds")]
        [InlineData("TextureCube_WMipMaps_BGRA8888.dds")]
        public void StartLibraryTest(string file)
        {
            TexImage image = new TexImage();

            var dxtLib = new DxtTexLib();
            dxtLib.Execute(image, new LoadingRequest(Module.PathToInputImages + file, false));
            image.CurrentLibrary = dxtLib;
            dxtLib.EndLibrary(image);

            TexLibraryTest.StartLibraryTest(image, library);

            image.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "TextureArray_WMipMaps_PVRTC2_4bpp.pvr");
            Assert.True(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.True(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", false)));
            Assert.True(library.CanHandleRequest(image, new MipMapsGenerationRequest(Filter.MipMapGeneration.Linear)));
            Assert.True(library.CanHandleRequest(image, new NormalMapGenerationRequest(0.5f)));
            Assert.True(library.CanHandleRequest(image, new SwitchingBRChannelsRequest()));
            Assert.True(library.CanHandleRequest(image, new FlippingRequest(Orientation.Horizontal)));
            Assert.True(library.CanHandleRequest(image, new FixedRescalingRequest(512, 512, Filter.Rescaling.Nearest)));
            Assert.True(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", 0)));
            Assert.False(library.CanHandleRequest(image, new GammaCorrectionRequest(1)));
            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_PVRTC2_4bpp.pvr")]
        [InlineData("TextureCube_WMipMaps_PVRTC2_4bpp.pvr")]
        public void DecompressTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.DecompressTest(image, library);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr", Stride.Graphics.PixelFormat.ETC2_RGBA)]
        [InlineData("TextureCube_WMipMaps_RGBA8888.pvr", Stride.Graphics.PixelFormat.ETC2_RGBA)]
        public void CompressTest(string file, Stride.Graphics.PixelFormat format)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.CompressTest(image, library, format);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WOMipMaps_PVRTC2_4bpp.pvr", Filter.MipMapGeneration.Box)]
        [InlineData("TextureCube_WOMipMaps_PVRTC2_4bpp.pvr", Filter.MipMapGeneration.Cubic)]
        public void GenerateMipMapTest(string file, Filter.MipMapGeneration filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.GenerateMipMapTest(image, library, filter);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", "PvrttTexLib_GenerateNormalMapTest_TextureArray_WOMipMaps_PVRTC2_4bpp.pvr")]
        [InlineData("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", "PvrttTexLib_GenerateNormalMapTest_TextureCube_WOMipMaps_PVRTC2_4bpp.pvr")]
        public void GenerateNormalMapTest(string file, string outFile)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.GenerateNormalMapTest(image, library);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bicubic)]
        [InlineData("TextureCube_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bilinear)]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Nearest)]
        public void FixedRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FixedRescaleTest(image, library, filter);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bicubic)]
        [InlineData("TextureCube_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Bilinear)]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr", Filter.Rescaling.Box)]
        public void FactorRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FactorRescaleTest(image, library, filter);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_PVRTC2_4bpp.pvr")]
        [InlineData("TextureCube_WMipMaps_PVRTC2_4bpp.pvr")]
        public void ExportTest(String file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportTest(image, library, file);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", 16)]
        [InlineData("TextureArray_WMipMaps_PVRTC2_4bpp.pvr", 512)]
        [InlineData("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", 8)]
        [InlineData("TextureCube_WMipMaps_PVRTC2_4bpp.pvr", 4)]
        public void ExportMinMipMapTest(String file, int minMipMapSize)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportMinMipMapTest(image, library, minMipMapSize);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr")]
        [InlineData("TextureCube_WMipMaps_RGBA8888.pvr")]
        public void SwitchChannelsTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.SwitchChannelsTest(image, library);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr", Orientation.Horizontal)]
        [InlineData("TextureCube_WMipMaps_RGBA8888.pvr", Orientation.Vertical)]
        public void FlipTest(String file, Orientation orientation)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FlipTest(image, library, orientation);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_RGBA8888.pvr")]
        [InlineData("TextureCube_WMipMaps_RGBA8888.pvr")]
        public void PreMultiplyAlphaTest(String file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.PreMultiplyAlphaTest(image, library);

            image.Dispose();
        }

    }
}
