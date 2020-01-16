// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;
using Xenko.TextureConverter.Requests;
using Xenko.TextureConverter.TexLibraries;

namespace Xenko.TextureConverter.Tests
{
    public class DxtTexLibTest : IDisposable
    {
        private readonly DxtTexLib library = new DxtTexLib();

        public DxtTexLibTest()
        {
            library = new DxtTexLib();
            Assert.True(library.SupportBGRAOrder());
        }

        public void Dispose()
        {
            library.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        [InlineData("TextureCube_WMipMaps_BC3.dds")]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds")]
        public void StartLibraryTest(string file) 
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.StartLibraryTest(image, library);

            DxtTextureLibraryData libraryData = (DxtTextureLibraryData)image.LibraryData[library];
            Assert.True(libraryData.DxtImages.Length == image.SubImageArray.Length);
            for (int i = 0; i < libraryData.DxtImages.Length; ++i) // Checking on features
            {
                Assert.True(libraryData.DxtImages[i].RowPitch == image.SubImageArray[i].RowPitch);
            }

            image.CurrentLibrary = null; // If we don't set the CurrentLibrary to null, the Dispose() method of TexImage will try calling the EndMethod, which won't work if no operation has been made on the image since the StartLibrary call. This case can't happen.

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        [InlineData("TextureCube_WMipMaps_BC3.dds")]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds")]
        public void EndLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            library.EndLibrary(image);

            image.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "TextureArray_WMipMaps_BC3.dds");
            Assert.True(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.True(library.CanHandleRequest(image, new FixedRescalingRequest(0, 0, Filter.Rescaling.Nearest)));
            Assert.True(library.CanHandleRequest(image, new MipMapsGenerationRequest(Filter.MipMapGeneration.Nearest)));
            Assert.True(library.CanHandleRequest(image, new NormalMapGenerationRequest(1)));
            Assert.True(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_BC3.dds", false)));
            Assert.True(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_BC3.dds", 0)));
            Assert.True(library.CanHandleRequest(image, new CompressingRequest(Xenko.Graphics.PixelFormat.BC3_UNorm)));
            Assert.False(library.CanHandleRequest(image, new GammaCorrectionRequest(0)));
            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        [InlineData("TextureCube_WMipMaps_BC3.dds")]
        public void DecompressTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.DecompressTest(image, library);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds")]
        public void DecompressFailTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            Assert.True(image.Format == Xenko.Graphics.PixelFormat.B8G8R8A8_UNorm);

            try
            {
                library.Execute(image, new DecompressingRequest(false));
                Assert.True(false);
            }
            catch (TextureToolsException)
            {
                Assert.True(true);
            }

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds", Xenko.Graphics.PixelFormat.BC3_UNorm)]
        [InlineData("TextureCube_WMipMaps_BGRA8888.dds", Xenko.Graphics.PixelFormat.BC3_UNorm)]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds", Xenko.Graphics.PixelFormat.BC3_UNorm)]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds", Xenko.Graphics.PixelFormat.BC1_UNorm)]
        [InlineData("TextureCube_WMipMaps_BGRA8888.dds", Xenko.Graphics.PixelFormat.BC1_UNorm)]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds", Xenko.Graphics.PixelFormat.BC1_UNorm)]
        public void CompressTest(string file, Xenko.Graphics.PixelFormat format)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.CompressTest(image, library, format);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WOMipMaps_BC3.dds", Filter.MipMapGeneration.Box)]
        [InlineData("TextureCube_WOMipMaps_BC3.dds", Filter.MipMapGeneration.Cubic)]
        [InlineData("Texture3D_WOMipMaps_BC3.dds", Filter.MipMapGeneration.Linear)]
        [InlineData("Texture3D_WOMipMaps_BC3.dds", Filter.MipMapGeneration.Nearest)]
        public void GenerateMipMapTest(string file, Filter.MipMapGeneration filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.GenerateMipMapTest(image, library, filter);
            
            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WOMipMaps_BC3.dds", "DxtTexLib_GenerateNormalMapTest_TextureArray_WOMipMaps_BC3.dds")]
        [InlineData("TextureCube_WOMipMaps_BC3.dds", "DxtTexLib_GenerateNormalMapTest_TextureCube_WOMipMaps_BC3.dds")]
        [InlineData("Texture3D_WOMipMaps_BC3.dds", "DxtTexLib_GenerateNormalMapTest_Texture3D_WOMipMaps_BC3.dds")]
        public void GenerateNormalMapTest(string file, string outFile)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.GenerateNormalMapTest(image, library);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds", Filter.Rescaling.Bicubic)]
        [InlineData("TextureCube_WMipMaps_BGRA8888.dds", Filter.Rescaling.Bilinear)]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds", Filter.Rescaling.Nearest)]
        public void FixedRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FixedRescaleTest(image, library, filter);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds", Filter.Rescaling.Bicubic)]
        [InlineData("TextureCube_WMipMaps_BGRA8888.dds", Filter.Rescaling.Bilinear)]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds", Filter.Rescaling.Box)]
        public void FactorRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FactorRescaleTest(image, library, filter);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds")]
        [InlineData("TextureCube_WMipMaps_BC3.dds")]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds")]
        public void ExportTest(String file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportTest(image, library, file);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BC3.dds", 16)]
        [InlineData("TextureArray_WMipMaps_BC3.dds", 512)]
        [InlineData("TextureCube_WMipMaps_BC3.dds", 8)]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds", 4)]
        public void ExportMinMipMapTest(String file, int minMipMapSize)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.ExportMinMipMapTest(image, library, minMipMapSize);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888.dds")]
        [InlineData("TextureCube_WMipMaps_BGRA8888.dds")]
        [InlineData("Texture3D_WMipMaps_BGRA8888.dds")]
        public void PreMultiplyAlphaTest(String file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.PreMultiplyAlphaTest(image, library);

            image.Dispose();
        }
    }
}
