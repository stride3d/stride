// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

using NUnit.Framework;

using Xenko.Graphics;
using Xenko.TextureConverter.Requests;

namespace Xenko.TextureConverter.Tests
{
    class TexLibraryTest
    {
        /// <summary>
        /// The purpose of this test is to show that after calling the StartLibrary method on a TexImage,
        /// this image will contain in its LibraryData list the ITextureLibraryData instance corresponding
        /// to the actual state of the TexImage.
        /// An instance of ITextureLibraryData is creating at the load of the TexImage by the library so we
        /// must delete it first for the sake of this test.
        /// </summary>
        public static void StartLibraryTest(TexImage image, ITexLibrary library)
        {
            image.LibraryData.Remove(library); // deleting the LibraryData instance

            Assert.IsFalse(image.LibraryData.ContainsKey(library));

            library.StartLibrary(image);

            Assert.IsTrue(image.LibraryData.ContainsKey(library));
        }

        public static void FactorRescaleTest(TexImage image, ITexLibrary library, Filter.Rescaling filter)
        {
            var request = new FactorRescalingRequest(0.5f, 0.5f, filter);
            int width = request.ComputeWidth(image);
            int height = request.ComputeHeight(image);

            library.Execute(image, request);
            Assert.IsTrue(image.Width == width);
            Assert.IsTrue(image.Height == height);
            Assert.IsTrue(image.MipmapCount == 1);

            image.Update();

            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["FactorRescaleTest_" + filter + "_" + image.Name]));
            //Console.WriteLine("FactorRescaleTest_" + filter + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
        }

        public static void FixedRescaleTest(TexImage image, ITexLibrary library, Filter.Rescaling filter)
        {
            var request = new FixedRescalingRequest(256, 256, filter);
            int width = request.ComputeWidth(image);
            int height = request.ComputeHeight(image);

            library.Execute(image, request);
            Assert.IsTrue(image.Width == width);
            Assert.IsTrue(image.Height == height);
            Assert.IsTrue(image.MipmapCount == 1);

            image.Update();

            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["FixedRescaleTest_" + filter + "_" + image.Name]));
            //Console.WriteLine("FixedRescaleTest_" + filter + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
        }

        public static void SwitchChannelsTest(TexImage image, ITexLibrary library)
        {
            var isInRgbaOrder = image.Format.IsRGBAOrder();
            library.Execute(image, new SwitchingBRChannelsRequest());
            Assert.IsTrue(image.Format.IsRGBAOrder() != isInRgbaOrder);

            //Console.WriteLine("SwitchChannelsTest_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["SwitchChannelsTest_" + image.Name]));
        }

        public static void FlipTest(TexImage image, ITexLibrary library, Orientation orientation)
        {
            library.Execute(image, new FlippingRequest(orientation));

            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["FlipTest_" + orientation + "_" + image.Name]));
            //Console.WriteLine("FlipTest_" + orientation + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
        }

        public static void DecompressTest(TexImage image, ITexLibrary library)
        {
            Assert.IsTrue(image.Format.IsCompressed());
            library.Execute(image, new DecompressingRequest(false));
            Assert.IsTrue(image.Format == PixelFormat.R8G8B8A8_UNorm);
            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["DecompressTest_" + image.Name]));
            //Console.WriteLine("DecompressTest_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
        }

        public static void CompressTest(TexImage image, ITexLibrary library, PixelFormat format)
        {
            Assert.IsTrue(!image.Format.IsCompressed());
            library.Execute(image, new CompressingRequest(format));

            Assert.IsTrue(image.Format == format);
            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["CompressTest_"+format+"_"+image.Name]));
            //Console.WriteLine("CompressTest_" + format + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
        }

        public static void GenerateMipMapTest(TexImage image, ITexLibrary library, Filter.MipMapGeneration filter)
        {
            Assert.IsTrue(image.MipmapCount == 1);
            if (image.Format.IsCompressed()) library.Execute(image, new DecompressingRequest(false));
            library.Execute(image, new MipMapsGenerationRequest(filter));
            Assert.IsTrue(image.MipmapCount > 1);
            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["GenerateMipMapTest_" + filter + "_" + image.Name]));
            //Console.WriteLine("GenerateMipMapTest_" + filter + "_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
        }

        public static void GenerateNormalMapTest(TexImage image, ITexLibrary library)
        {
            library.Execute(image, new DecompressingRequest(false));
            var request = new NormalMapGenerationRequest(1);
            library.Execute(image, request);

            Assert.IsTrue(TestTools.ComputeSHA1(request.NormalMap.Data, request.NormalMap.DataSize).Equals(TestTools.GetInstance().Checksum["GenerateNormalMapTest_" + image.Name]));
            //Console.WriteLine("GenerateNormalMapTest_" + image.Name + "." + TestTools.ComputeSHA1(request.NormalMap.Data, request.NormalMap.DataSize));

            request.NormalMap.Dispose();
        }

        public static void ExportTest(TexImage image, ITexLibrary library, string file)
        {
            String outputFile = library.GetType().Name + "_ExportTest_" + file;
            library.Execute(image, new ExportRequest(Module.PathToOutputImages + outputFile, 0));

            //Console.WriteLine("ExportTest_" + file + "." + TestTools.ComputeSHA1(Module.PathToOutputImages + outputFile));
            Assert.IsTrue(TestTools.ComputeSHA1(Module.PathToOutputImages + outputFile).Equals(TestTools.GetInstance().Checksum["ExportTest_" + file]));
            File.Delete(Module.PathToOutputImages + outputFile);
        }

        public static void PreMultiplyAlphaTest(TexImage image, ITexLibrary library)
        {
            library.Execute(image, new PreMultiplyAlphaRequest());

            //Console.WriteLine("PreMultiplyAlphaTest_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["PreMultiplyAlphaTest_" + image.Name]));
        }

        public static void CorrectGammaTest(TexImage image, ITexLibrary library)
        {
            library.Execute(image, new GammaCorrectionRequest(1 / 2.2));

            //Console.WriteLine("CorrectGammaTest_" + image.Name + "." + TestTools.ComputeSHA1(image.Data, image.DataSize));
            Assert.IsTrue(TestTools.ComputeSHA1(image.Data, image.DataSize).Equals(TestTools.GetInstance().Checksum["CorrectGammaTest_" + image.Name]));
        }

        public static void ExportMinMipMapTest(TexImage image, ITexLibrary library, int minMipMapSize)
        {
            String outputFile = library.GetType().Name + "_ExportTest_MinMipMapSize-" + minMipMapSize + "_" + image.Name;
            library.Execute(image, new ExportRequest(Module.PathToOutputImages + outputFile, minMipMapSize));

            TexImage image2 = new TexImage();
            library.Execute(image2, new LoadingRequest(Module.PathToOutputImages + outputFile, false));
            image2.CurrentLibrary = library;

            image.Update();
            image2.Update();

            Assert.IsTrue(image.Dimension == image2.Dimension);
            Assert.IsTrue(image2.SubImageArray[image2.SubImageArray.Length - 1].Width >= minMipMapSize);
            Assert.IsTrue(image2.SubImageArray[image2.SubImageArray.Length - 1].Height >= minMipMapSize);
            Assert.IsTrue(image.Width == image2.Width);
            Assert.IsTrue(image.Height == image2.Height);
            Assert.IsTrue(image.Depth == image2.Depth);
            Assert.IsTrue(image.ArraySize == image2.ArraySize);


            //Console.WriteLine("ExportMinMipMapTest_" + minMipMapSize + "_" + image.Name + "." + TestTools.ComputeSHA1(Module.PathToOutputImages + outputFile));
            Assert.IsTrue(TestTools.ComputeSHA1(Module.PathToOutputImages + outputFile).Equals(TestTools.GetInstance().Checksum["ExportMinMipMapTest_" + minMipMapSize + "_" + image.Name]));
            File.Delete(Module.PathToOutputImages + outputFile);

            image2.Dispose();
        }

    }
}
