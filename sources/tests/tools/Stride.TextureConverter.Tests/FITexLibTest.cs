// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

using Xunit;
using Stride.TextureConverter.Requests;
using Stride.TextureConverter.TexLibraries;

namespace Stride.TextureConverter.Tests
{
    public class FiTexLibTest : IDisposable
    {
        private readonly FITexLib library = new FITexLib();

        public FiTexLibTest()
        {
            Assert.True(library.SupportBGRAOrder());
        }

        public void Dispose()
        {
            library.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("duck.jpg")]
        [InlineData("stones.png")]
        [InlineData("snap1.psd")]
        public void StartLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.StartLibraryTest(image, library);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("duck.jpg")]
        [InlineData("stones.png")]
        public void EndLibraryTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            library.EndLibrary(image);
            image.CurrentLibrary = null;

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("duck.jpg")]
        [InlineData("stones.png")]
        public void CorrectGammaTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.CorrectGammaTest(image, library);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("duck.jpg", Orientation.Horizontal)]
        [InlineData("stones.png", Orientation.Vertical)]
        public void FlipTest(string file, Orientation orientation)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FlipTest(image, library, orientation);

            image.Dispose();
        }

        [Theory(Skip = "Need check")]
        [InlineData("duck.jpg", Filter.Rescaling.Bicubic)]
        [InlineData("stones.png", Filter.Rescaling.Bilinear)]
        [InlineData("duck.jpg", Filter.Rescaling.Box)]
        [InlineData("stones.png", Filter.Rescaling.BSpline)]
        [InlineData("duck.jpg", Filter.Rescaling.CatmullRom)]
        [InlineData("stones.png", Filter.Rescaling.Lanczos3)]
        public void FixedRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FixedRescaleTest(image, library, filter);

            image.Dispose();
        }

        [Fact(Skip = "Need check")]
        public void FixedRescale3DTest()
        {
            DxtTexLib lib = new DxtTexLib();
            TexImage image = new TexImage();
            lib.Execute(image, new LoadingRequest(Module.PathToInputImages+"Texture3D_WMipMaps_BGRA8888.dds", false));
            image.Name = "Texture3D_WMipMaps_BGRA8888.dds";
            lib.EndLibrary(image);
            library.StartLibrary(image);
            image.CurrentLibrary = library;
            TexLibraryTest.FactorRescaleTest(image, library, Filter.Rescaling.Lanczos3);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("stones.png", Filter.Rescaling.Bicubic)]
        [InlineData("stones.png", Filter.Rescaling.Bilinear)]
        [InlineData("stones.png", Filter.Rescaling.Box)]
        [InlineData("stones.png", Filter.Rescaling.BSpline)]
        [InlineData("stones.png", Filter.Rescaling.CatmullRom)]
        [InlineData("stones.png", Filter.Rescaling.Lanczos3)]
        public void FactorRescaleTest(string file, Filter.Rescaling filter)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.FactorRescaleTest(image, library, filter);
            
            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("duck.jpg")]
        [InlineData("stones.png")]
        public void SwitchChannelsTest(string file)
        {
            TexImage image = TestTools.Load(library, file);

            TexLibraryTest.SwitchChannelsTest(image, library);

            image.Dispose();
        }


        [Theory(Skip = "Need check")]
        [InlineData("TextureArray_WMipMaps_BGRA8888", ".dds", 0)]
        [InlineData("TextureArray_WMipMaps_BGRA8888", ".dds", 16)]
        public void ExportArrayTest(string fileName, string extension, int minMipMapSize)
        {
            DxtTexLib lib = new DxtTexLib();
            TexImage image = new TexImage();
            lib.Execute(image, new LoadingRequest(Module.PathToInputImages + fileName + extension, false));
            lib.EndLibrary(image);
            library.StartLibrary(image);

            library.Execute(image, new ExportRequest(Module.PathToOutputImages + "FITexLibTest_ExportArrayTest_" + fileName + ".png", minMipMapSize));

            int ct = 0;
            for (int i = 0; i < image.ArraySize; ++i)
            {
                for (int j = 0; j < image.MipmapCount; ++j)
                {
                    if (image.SubImageArray[ct].Height < minMipMapSize || image.SubImageArray[ct].Width < minMipMapSize)
                        break;
                    string file = Module.PathToOutputImages + "FITexLibTest_ExportArrayTest_" + fileName + "-ind_" + i + "-mip_" + j + ".png";
                    Assert.True(File.Exists(file));

                    //Console.WriteLine("FITexLibTest_ExportArrayTest_" + minMipMapSize + "_" + fileName + "-ind_" + i + "-mip_" + j + ".png" + "." + TestTools.ComputeSHA1(file));
                    Assert.Equal(TestTools.GetInstance().Checksum["FITexLibTest_ExportArrayTest_" + minMipMapSize + "_" + fileName + "-ind_" + i + "-mip_" + j + ".png"], TestTools.ComputeSHA1(file));
                    File.Delete(file);
                    ++ct;
                }
            }

            image.Dispose();
        }


        [Fact(Skip = "Need check")]
        public void CanHandleRequestTest()
        {
            TexImage image = TestTools.Load(library, "stones.png");
            Assert.False(library.CanHandleRequest(image, new DecompressingRequest(false)));
            Assert.True(library.CanHandleRequest(image, new FixedRescalingRequest(0, 0, Filter.Rescaling.Bilinear)));
            Assert.True(library.CanHandleRequest(image, new SwitchingBRChannelsRequest()));
            Assert.True(library.CanHandleRequest(image, new FlippingRequest(Orientation.Vertical)));
            Assert.True(library.CanHandleRequest(image, new LoadingRequest("TextureArray_WMipMaps_BC3.png", false)));
            Assert.True(library.CanHandleRequest(image, new ExportRequest("TextureArray_WMipMaps_BC3.png", 0)));
            Assert.True(library.CanHandleRequest(image, new GammaCorrectionRequest(0)));
            image.Dispose();
        }

    }
}
