// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Graphics.Regression;

namespace Xenko.Graphics.Tests
{
    [TestFixture]
    [Description("Check Texture")]
    public class TestTexture : GameTestBase
    {
        private static ImageFileType[] ImageFileTypes => (ImageFileType[])Enum.GetValues(typeof(ImageFileType));

        public TestTexture()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
        }

        [Test]
        public void TestCalculateMipMapCount()
        {
            Assert.AreEqual(1, Texture.CalculateMipMapCount(MipMapCount.Auto, 0));
            Assert.AreEqual(1, Texture.CalculateMipMapCount(MipMapCount.Auto, 1));
            Assert.AreEqual(2, Texture.CalculateMipMapCount(MipMapCount.Auto, 2));
            Assert.AreEqual(3, Texture.CalculateMipMapCount(MipMapCount.Auto, 4));
            Assert.AreEqual(4, Texture.CalculateMipMapCount(MipMapCount.Auto, 8));
            Assert.AreEqual(9, Texture.CalculateMipMapCount(MipMapCount.Auto, 256, 256));
            Assert.AreEqual(10, Texture.CalculateMipMapCount(MipMapCount.Auto, 1023));
            Assert.AreEqual(11, Texture.CalculateMipMapCount(MipMapCount.Auto, 1024));
            Assert.AreEqual(10, Texture.CalculateMipMapCount(MipMapCount.Auto, 615, 342));
        }

        [Test]
        public void TestTexture1D()
        {
            PerformTest(
                game =>
                {
                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256];
                    data[0] = 255;
                    data[31] = 1;
                    var texture = Texture.New1D(game.GraphicsDevice, 256, PixelFormat.R8_UNorm, data, usage: GraphicsResourceUsage.Default);

                    // Perform texture op
                    CheckTexture(game.GraphicsContext, texture, data);

                    // Release the texture
                    texture.Dispose();
                });
        }

        [Test]
        public void TestTexture1DMipMap()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256];
                    data[0] = 255;
                    data[31] = 1;
                    var texture = Texture.New1D(device, 256, true, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

                    // Verify the number of mipmap levels
                    Assert.That(texture.MipLevels, Is.EqualTo(Math.Log(data.Length, 2) + 1));

                    // Get a render target on the mipmap 1 (128) with value 1 and get back the data
                    var renderTarget1 = texture.ToTextureView(ViewType.Single, 0, 1);
                    commandList.Clear(renderTarget1, new Color4(0xFF000001));
                    var data1 = texture.GetData<byte>(commandList, 0, 1);
                    Assert.That(data1.Length, Is.EqualTo(128));
                    Assert.That(data1[0], Is.EqualTo(1));
                    renderTarget1.Dispose();

                    // Get a render target on the mipmap 2 (128) with value 2 and get back the data
                    var renderTarget2 = texture.ToTextureView(ViewType.Single, 0, 2);
                    commandList.Clear(renderTarget2, new Color4(0xFF000002));
                    var data2 = texture.GetData<byte>(commandList, 0, 2);
                    Assert.That(data2.Length, Is.EqualTo(64));
                    Assert.That(data2[0], Is.EqualTo(2));
                    renderTarget2.Dispose();

                    // Get a render target on the mipmap 3 (128) with value 3 and get back the data
                    var renderTarget3 = texture.ToTextureView(ViewType.Single, 0, 3);
                    commandList.Clear(renderTarget3, new Color4(0xFF000003));
                    var data3 = texture.GetData<byte>(commandList, 0, 3);
                    Assert.That(data3.Length, Is.EqualTo(32));
                    Assert.That(data3[0], Is.EqualTo(3));
                    renderTarget3.Dispose();

                    // Release the texture
                    texture.Dispose();
                });
        }

        [Test]
        public void TestTexture2D()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256 * 256];
                    data[0] = 255;
                    data[31] = 1;
                    var texture = Texture.New2D(device, 256, 256, PixelFormat.R8_UNorm, data, usage: GraphicsResourceUsage.Default);

                    // Perform texture op
                    CheckTexture(game.GraphicsContext, texture, data);

                    // Release the texture
                    texture.Dispose();
                },
                GraphicsProfile.Level_9_1);
        }

        [Test]
        public void TestTexture2DArray()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256 * 256];
                    data[0] = 255;
                    data[31] = 1;
                    var texture = Texture.New2D(device, 256, 256, 1, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 4);

                    // Verify the number of mipmap levels
                    Assert.That(texture.MipLevels, Is.EqualTo(1));

                    // Get a render target on the array 1 (128) with value 1 and get back the data
                    var renderTarget1 = texture.ToTextureView(ViewType.Single, 1, 0);
                    Assert.That(renderTarget1.ViewWidth, Is.EqualTo(256));
                    Assert.That(renderTarget1.ViewHeight, Is.EqualTo(256));

                    commandList.Clear(renderTarget1, new Color4(0xFF000001));
                    var data1 = texture.GetData<byte>(commandList, 1);
                    Assert.That(data1.Length, Is.EqualTo(data.Length));
                    Assert.That(data1[0], Is.EqualTo(1));
                    renderTarget1.Dispose();

                    // Get a render target on the array 2 (128) with value 2 and get back the data
                    var renderTarget2 = texture.ToTextureView(ViewType.Single, 2, 0);
                    commandList.Clear(renderTarget2, new Color4(0xFF000002));
                    var data2 = texture.GetData<byte>(commandList, 2);
                    Assert.That(data2.Length, Is.EqualTo(data.Length));
                    Assert.That(data2[0], Is.EqualTo(2));
                    renderTarget2.Dispose();

                    // Get a render target on the array 3 (128) with value 3 and get back the data
                    var renderTarget3 = texture.ToTextureView(ViewType.Single, 3, 0);
                    commandList.Clear(renderTarget3, new Color4(0xFF000003));
                    var data3 = texture.GetData<byte>(commandList, 3);
                    Assert.That(data3.Length, Is.EqualTo(data.Length));
                    Assert.That(data3[0], Is.EqualTo(3));
                    renderTarget3.Dispose();

                    // Release the texture
                    texture.Dispose();
                });
        }

        [Test]
        public void TestTexture2DUnorderedAccess()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGL);
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);

            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256 * 256];
                    data[0] = 255;
                    data[31] = 1;
                    var texture = Texture.New2D(device, 256, 256, 1, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, 4);

                    // Clear slice array[1] with value 1, read back data from texture and check validity
                    var texture1 = texture.ToTextureView(ViewType.Single, 1, 0);
                    Assert.That(texture1.ViewWidth, Is.EqualTo(256));
                    Assert.That(texture1.ViewHeight, Is.EqualTo(256));
                    Assert.That(texture1.ViewDepth, Is.EqualTo(1));

                    commandList.ClearReadWrite(texture1, new Int4(1));
                    var data1 = texture.GetData<byte>(commandList, 1);
                    Assert.That(data1.Length, Is.EqualTo(data.Length));
                    Assert.That(data1[0], Is.EqualTo(1));
                    texture1.Dispose();

                    // Clear slice array[2] with value 2, read back data from texture and check validity
                    var texture2 = texture.ToTextureView(ViewType.Single, 2, 0);
                    commandList.ClearReadWrite(texture2, new Int4(2));
                    var data2 = texture.GetData<byte>(commandList, 2);
                    Assert.That(data2.Length, Is.EqualTo(data.Length));
                    Assert.That(data2[0], Is.EqualTo(2));
                    texture2.Dispose();

                    texture.Dispose();
                },
                GraphicsProfile.Level_11_0); // Force to use Level11 in order to use UnorderedAccessViews
        }

        [Test]
        public void TestTexture3D()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[32 * 32 * 32];
                    data[0] = 255;
                    data[31] = 1;
                    var texture = Texture.New3D(device, 32, 32, 32, PixelFormat.R8_UNorm, data, usage: GraphicsResourceUsage.Default);

                    // Perform generate texture checking
                    CheckTexture(game.GraphicsContext, texture, data);

                    // Release the texture
                    texture.Dispose();
                });
        }

        [Test]
        public void TestTexture3DRenderTarget()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var texture = Texture.New3D(device, 32, 32, 32, true, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

                    // Get a render target on the 1st mipmap of this texture 3D
                    var renderTarget0 = texture.ToTextureView(ViewType.Single, 0, 0);
                    commandList.Clear(renderTarget0, new Color4(0xFF000001));
                    var data1 = texture.GetData<byte>(commandList);
                    Assert.That(data1.Length, Is.EqualTo(32 * 32 * 32));
                    Assert.That(data1[0], Is.EqualTo(1));
                    renderTarget0.Dispose();

                    // Get a render target on the 2nd mipmap of this texture 3D
                    var renderTarget1 = texture.ToTextureView(ViewType.Single, 0, 1);

                    // Check that width/height is correctly calculated 
                    Assert.That(renderTarget1.ViewWidth, Is.EqualTo(32 >> 1));
                    Assert.That(renderTarget1.ViewHeight, Is.EqualTo(32 >> 1));

                    commandList.Clear(renderTarget1, new Color4(0xFF000001));
                    var data2 = texture.GetData<byte>(commandList, 0, 1);
                    Assert.That(data2.Length, Is.EqualTo(16 * 16 * 16));
                    Assert.That(data2[0], Is.EqualTo(1));
                    renderTarget1.Dispose();

                    // Release the texture
                    texture.Dispose();
                });
        }

        [Test]
        public void TestDepthStencilBuffer()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);

            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check that read-only is not supported for depth stencil buffer
                    var supported = GraphicsDevice.Platform != GraphicsPlatform.Direct3D11;
                    Assert.AreEqual(supported, Texture.IsDepthStencilReadOnlySupported(device));

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var texture = Texture.New2D(device, 256, 256, PixelFormat.D32_Float, TextureFlags.DepthStencil);

                    // Clear the depth stencil buffer with a value of 0.5f
                    commandList.Clear(texture, DepthStencilClearOptions.DepthBuffer, 0.5f);

                    var values = texture.GetData<float>(commandList);
                    Assert.That(values.Length, Is.EqualTo(256*256));
                    Assert.That(MathUtil.WithinEpsilon(values[0], 0.5f, 0.00001f));

                    // Create a new copy of the depth stencil buffer
                    var textureCopy = texture.CreateDepthTextureCompatible();

                    commandList.Copy(texture, textureCopy);

                    values = textureCopy.GetData<float>(commandList);
                    Assert.That(values.Length, Is.EqualTo(256 * 256));
                    Assert.That(MathUtil.WithinEpsilon(values[0], 0.5f, 0.00001f));

                    // Dispose the depth stencil buffer
                    textureCopy.Dispose();
                }, 
                GraphicsProfile.Level_10_0);
        }

        [Test, Ignore("Clear on a ReadOnly depth buffer should be undefined or throw exception; should rewrite this test to do actual rendering with ReadOnly depth stencil bound?")]
        public void TestDepthStencilBufferWithNativeReadonly()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);

            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    //// Without shaders, it is difficult to check this method without accessing internals

                    // Check that read-only is not supported for depth stencil buffer
                    Assert.That(Texture.IsDepthStencilReadOnlySupported(device), Is.True);

                    // Check texture creation with an array of data, with usage default to later allow SetData
                    var texture = Texture.New2D(device, 256, 256, PixelFormat.D32_Float, TextureFlags.ShaderResource | TextureFlags.DepthStencilReadOnly);

                    // Clear the depth stencil buffer with a value of 0.5f, but the depth buffer is readonly
                    commandList.Clear(texture, DepthStencilClearOptions.DepthBuffer, 0.5f);

                    var values = texture.GetData<float>(commandList);
                    Assert.That(values.Length, Is.EqualTo(256 * 256));
                    Assert.That(values[0], Is.EqualTo(0.0f));

                    // Dispose the depth stencil buffer
                    texture.Dispose();
                },
                GraphicsProfile.Level_11_0);
        }

        /// <summary>
        /// Tests the load save.
        /// </summary>
        /// <remarks>
        /// This test loads several images using <see cref="Texture.Load"/> (on the GPU) and save them to the disk using <see cref="Texture.Save(Stream,ImageFileType)"/>.
        /// The saved image is then compared with the original image to check that the whole chain (CPU -> GPU, GPU -> CPU) is passing correctly
        /// the textures.
        /// </remarks>
        [Test, TestCaseSource(nameof(ImageFileTypes))]
        public void TestLoadSave(ImageFileType sourceFormat)
        {
            if(Platform.Type == PlatformType.Android && (
                sourceFormat == ImageFileType.Xenko || sourceFormat == ImageFileType.Dds || // TODO remove this when mipmap copy is supported on OpenGL by the engine.
                sourceFormat == ImageFileType.Tiff)) // TODO remove when the tiff format is supported on android.
                Assert.Ignore();

            PerformTest(
                game =>
                {
                    var intermediateFormat = ImageFileType.Xenko;

                    if (sourceFormat == ImageFileType.Wmp) // no input image of this format.
                        return;

                    if (sourceFormat == ImageFileType.Wmp || sourceFormat == ImageFileType.Tga) // TODO remove this when Load/Save methods are implemented for those types.
                        return;

                    var device = game.GraphicsDevice;
                    var fileName = sourceFormat.ToFileExtension().Substring(1) + "Image";
                    var filePath = "ImageTypes/" + fileName;

                    var testMemoryBefore = GC.GetTotalMemory(true);
                    var clock = Stopwatch.StartNew();

                    // Load an image from a file and dispose it.
                    Texture texture;
                    using (var inStream = game.Content.OpenAsStream(filePath, StreamFlags.None))
                        texture = Texture.Load(device, inStream);
                            
                    var tempStream = new MemoryStream();
                    texture.Save(game.GraphicsContext.CommandList, tempStream, intermediateFormat);
                    tempStream.Position = 0;
                    texture.Dispose();

                    using (var inStream = game.Content.OpenAsStream(filePath, StreamFlags.None))
                    using (var originalImage = Image.Load(inStream))
                    {
                        using (var textureImage = Image.Load(tempStream))
                        {
                            TestImage.CompareImage(originalImage, textureImage, false, 0, fileName);
                        }
                    }
                    tempStream.Dispose();
                    var time = clock.ElapsedMilliseconds;
                    clock.Stop();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    var testMemoryAfter = GC.GetTotalMemory(true);
                    Log.Info($"Test loading {fileName} GPU texture / saving to {intermediateFormat} and compare with original Memory {testMemoryAfter - testMemoryBefore} delta bytes, in {time}ms");
                }, 
                GraphicsProfile.Level_9_1);
        }

        [Test, TestCaseSource(nameof(ImageFileTypes))]
        public void TestLoadDraw(ImageFileType sourceFormat)
        {
            if (sourceFormat == ImageFileType.Wmp) // no input image of this format.
                Assert.Ignore();

            if (sourceFormat == ImageFileType.Wmp || sourceFormat == ImageFileType.Tga) // TODO remove this when Load/Save methods are implemented for those types.
                Assert.Ignore();

            if (Platform.Type == PlatformType.Android && sourceFormat == ImageFileType.Tiff)// TODO remove this when Load/Save methods are implemented for this type.
                Assert.Ignore();

            PerformDrawTest(
                (game, context) =>
                {
                    context.CommandList.Clear(context.CommandList.RenderTarget, new Color4(Color.Green).ToColorSpace(ColorSpace.Linear));
                    context.CommandList.Clear(context.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

                    var device = game.GraphicsDevice;
                    var fileName = sourceFormat.ToFileExtension().Substring(1) + "Image";
                    var filePath = "ImageTypes/" + fileName;
                        
                    // Load an image from a file and dispose it.
                    Texture texture;
                    using (var inStream = game.Content.OpenAsStream(filePath, StreamFlags.None))
                        texture = Texture.Load(device, inStream, loadAsSRGB: true);
                        
                    game.GraphicsContext.DrawTexture(texture, BlendStates.AlphaBlend);
                },
                GraphicsProfile.Level_9_1);
        }

        private void CheckTexture(GraphicsContext graphicsContext, Texture texture, byte[] data)
        {
            // Get back the data from the gpu
            var data2 = texture.GetData<byte>(graphicsContext.CommandList);

            // Assert that data are the same
            Assert.That(Utilities.Compare(data, data2), Is.True);

            // Sets new data on the gpu
            data[0] = 1;
            data[31] = 255;
            texture.SetData(graphicsContext.CommandList, data);

            // Get back the data from the gpu
            data2 = texture.GetData<byte>(graphicsContext.CommandList);

            // Assert that data are the same
            Assert.That(Utilities.Compare(data, data2), Is.True);
        }

        [TestCase(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Staging)]
        [TestCase(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Default)]
        [TestCase(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Staging)]
        [TestCase(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Default)]
        public void TestGetData(GraphicsProfile profile, GraphicsResourceUsage usage)
        {
            var testArray = profile >= GraphicsProfile.Level_10_0; // TODO modify this when when supported on openGL
            var mipmaps = GraphicsDevice.Platform == GraphicsPlatform.OpenGLES && profile < GraphicsProfile.Level_10_0 ? 1 : 3; // TODO remove this limitation when GetData is fixed on OpenGl ES for mipmap levels other than 0

            PerformTest(
                game =>
                {
                    const int width = 16;
                    const int height = width;
                    var arraySize = testArray ? 2 : 1;
                    var flags = usage == GraphicsResourceUsage.Default?
                        new[] { TextureFlags.ShaderResource, TextureFlags.RenderTarget, TextureFlags.RenderTarget | TextureFlags.ShaderResource }:
                        new[] { TextureFlags.None };

                    var pixelFormat = PixelFormat.R8G8B8A8_UNorm;
                    var data = CreateDebugTextureData(width, height, mipmaps, arraySize, pixelFormat, DefaultColorComputer);

                    foreach (var flag in flags)
                    {
                        using (var texture = CreateDebugTexture(game.GraphicsDevice, data, width, height, mipmaps, arraySize, pixelFormat, flag, usage))
                            CheckDebugTextureData(game.GraphicsContext, texture, width, height, mipmaps, arraySize, pixelFormat, flag, usage, DefaultColorComputer);
                    }
                },
                profile);
        }

        [TestCase(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Staging)]
        [TestCase(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Staging)]
        [TestCase(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Default)]
        [TestCase(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Default)]
        public void TestCopy(GraphicsProfile profile, GraphicsResourceUsage usageSource)
        {
            var testArray = profile >= GraphicsProfile.Level_10_0; // TODO modify this when when supported on openGL
            var mipmaps = GraphicsDevice.Platform == GraphicsPlatform.OpenGLES && profile < GraphicsProfile.Level_10_0 ? 1 : 3; // TODO remove this limitation when GetData is fixed on OpenGl ES for mipmap levels other than 0

            PerformTest(
                game =>
                {
                    const int width = 16;
                    const int height = width;
                    var arraySize = testArray ? 2 : 1;

                    var destinationIsStaged = new[] { true, false };

                    foreach (var destinationStaged in destinationIsStaged)
                    {
                        var pixelFormats = new List<PixelFormat> { PixelFormat.R8G8B8A8_UNorm, PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.R8_UNorm };
#if XENKO_GRAPHICS_API_OPENGLES
                        if(!game.GraphicsDevice.HasTextureRG)
                            pixelFormats.Remove(PixelFormat.R8_UNorm);
#endif

                        foreach (var pixelFormat in pixelFormats)
                        {
                            var computer = pixelFormat.SizeInBytes() == 1 ? (Func<int, int, int, int, int, byte>)ColorComputerR8 : DefaultColorComputer;
                            var data = CreateDebugTextureData(width, height, mipmaps, arraySize, pixelFormat, computer);

                            var sourceFlags = usageSource == GraphicsResourceUsage.Default ?
                                new[] { TextureFlags.ShaderResource, TextureFlags.RenderTarget, TextureFlags.RenderTarget | TextureFlags.ShaderResource } :
                                new[] { TextureFlags.None };

                            foreach (var flag in sourceFlags)
                            {
                                using (var texture = CreateDebugTexture(game.GraphicsDevice, data, width, height, mipmaps, arraySize, pixelFormat, flag, usageSource))
                                using (var copyTexture = destinationStaged ? texture.ToStaging(): texture.Clone())
                                {
                                    game.GraphicsContext.CommandList.Copy(texture, copyTexture);

                                    CheckDebugTextureData(game.GraphicsContext, copyTexture, width, height, mipmaps, arraySize, pixelFormat, flag, usageSource, computer);
                                }
                            }
                        }
                    }
                },
                profile);
        }

        private byte[] CreateDebugTextureData(int width, int height, int mipmaps, int arraySize, PixelFormat format, Func<int, int, int, int, int, byte> dataComputer)
        {
            var formatSize = format.SizeInBytes();

            var mipmapSize = 0;
            for (int i = 0; i < mipmaps; i++)
                mipmapSize += (width >> i) * (height >> i);

            var dataSize = arraySize * mipmapSize;
            var data = new byte[dataSize * formatSize];
            {
                var offset = 0;
                for (int array = 0; array < arraySize; array++)
                {
                    for (int mip = 0; mip < mipmaps; mip++)
                    {
                        var w = width >> mip;
                        var h = height >> mip;

                        for (int r = 0; r < h; r++)
                        {
                            for (int c = 0; c < w; c++)
                            {
                                for (int i = 0; i < formatSize; i++)
                                {
                                    data[offset + (r * w + c) * formatSize + i] = dataComputer(c, r, mip, array, i);
                                }
                            }
                        }
                        offset += w * h * formatSize;
                    }
                }
            }

            return data;
        }

        private unsafe Texture CreateDebugTexture(GraphicsDevice device, byte[] data, int width, int height, int mipmaps, int arraySize, PixelFormat format, TextureFlags flags, GraphicsResourceUsage usage)
        {
            fixed (byte* pData = data)
            {
                var sizeInBytes = format.SizeInBytes();

                var offset = 0;
                var dataBoxes = new DataBox[arraySize * mipmaps];
                for (int array = 0; array < arraySize; array++)
                {
                    for (int mip = 0; mip < mipmaps; mip++)
                    {
                        var w = width >> mip;
                        var h = height >> mip;
                        var rowStride = w * sizeInBytes;
                        var sliceStride = rowStride * h;

                        dataBoxes[array * mipmaps + mip] = new DataBox((IntPtr)pData + offset, rowStride, sliceStride);

                        offset += sliceStride;
                    }
                }

                return Texture.New2D(device, width, height, mipmaps, format, dataBoxes, flags, arraySize, usage);
            }
        }

        private void CheckDebugTextureData(GraphicsContext graphicsContext, Texture debugTexture, int width, int height, int mipmaps, int arraySize,
            PixelFormat format, TextureFlags flags, GraphicsResourceUsage usage, Func<int, int, int, int, int, byte> dataComputer)
        {
            var pixelSize = format.SizeInBytes();

            for (int arraySlice = 0; arraySlice < arraySize; arraySlice++)
            {
                for (int mipSlice = 0; mipSlice < mipmaps; mipSlice++)
                {
                    var w = width >> mipSlice;
                    var h = height >> mipSlice;

                    var readData = debugTexture.GetData<byte>(graphicsContext.CommandList, arraySlice, mipSlice);

                    for (int r = 0; r < h; r++)
                    {
                        for (int c = 0; c < w; c++)
                        {
                            for (int i = 0; i < pixelSize; i++)
                            {
                                var value = readData[(r * w + c) * pixelSize + i];
                                var expectedValue = dataComputer(c, r, mipSlice, arraySlice, i);

                                if (!expectedValue.Equals(value))
                                    Assert.Fail("The texture data get at [{0}, {1}] for mipmap level '{2}' and slice '{3}' with flags '{4}', usage '{5}' and format '{6}' is not valid. " +
                                                "Expected '{7}' but was '{8}' at index '{9}'",
                                                c, r, mipSlice, arraySlice, flags, usage, format, expectedValue, value, i);
                            }
                        }
                    }
                }
            }
        }

        private byte DefaultColorComputer(int x, int y, int mipmapSlice, int arraySlice, int index)
        {
            switch (index)
            {
                case 0:
                    return (byte)x;
                case 1:
                    return (byte)y;
                case 2:
                    return (byte)mipmapSlice;
                case 3:
                    return (byte)arraySlice;
            }

            return byte.MaxValue;
        }

        private byte ColorComputerR8(int x, int y, int mipmapSlice, int arraySlice, int index)
        {
            return (byte)(arraySlice*100 + mipmapSlice*20 + (x >> 2) + (y >> 2) * 4);
        }
    }
}
