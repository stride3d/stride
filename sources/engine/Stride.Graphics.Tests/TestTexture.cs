// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Xunit;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    public class TestTexture : GameTestBase
    {
        public static IEnumerable<object[]> ImageFileTypes
            => Enum.GetValues<ImageFileType>()
                .Select(fileType => new object[] { fileType });


        public TestTexture()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = [ GraphicsProfile.Level_10_0 ];
        }


        [Fact]
        public void TestCalculateMipMapCount()
        {
            Assert.Equal(1, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 0));
            Assert.Equal(1, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 1));
            Assert.Equal(2, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 2));
            Assert.Equal(3, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 4));
            Assert.Equal(4, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 8));
            Assert.Equal(9, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 256, height: 256));
            Assert.Equal(10, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 1023));
            Assert.Equal(11, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 1024));
            Assert.Equal(10, Texture.CalculateMipMapCount(MipMapCount.Auto, width: 615, height: 342));
        }

        [Fact]
        public void TestTexture1D()
        {
            PerformTest(
                game =>
                {
                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256];
                    data[0] = 255;
                    data[31] = 1;

                    var texture = Texture.New1D(game.GraphicsDevice, width: 256, PixelFormat.R8_UNorm, data, usage: GraphicsResourceUsage.Default);

                    // Perform a Texture read-back and test
                    CheckTexture(game.GraphicsContext, texture, data);

                    // Release the Texture
                    texture.Dispose();
                });
        }

        [Fact]
        public void TestTexture1DMipMap()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256];
                    data[0] = 255;
                    data[31] = 1;

                    var texture = Texture.New1D(device, width: 256, MipMapCount.Auto, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

                    // Verify the number of mip-levels
                    Assert.Equal(texture.MipLevelCount, Math.Log(data.Length, 2) + 1);

                    // Get a Render Target on the mip-level 1 (128), clear it with value 1 and get back the data
                    var renderTarget1 = texture.ToTextureView(ViewType.Single, arraySlice: 0, mipLevel: 1);
                    commandList.Clear(renderTarget1, new Color4(0xFF000001));
                    var data1 = texture.GetData<byte>(commandList, arrayIndex: 0, mipLevel: 1);
                    Assert.Equal(128, data1.Length);
                    Assert.Equal(1, data1[0]);
                    renderTarget1.Dispose();

                    // Get a Render Target on the mip-level 2 (128), clear it with value 2 and get back the data
                    var renderTarget2 = texture.ToTextureView(ViewType.Single, arraySlice: 0, mipLevel: 2);
                    commandList.Clear(renderTarget2, new Color4(0xFF000002));
                    var data2 = texture.GetData<byte>(commandList, arrayIndex: 0, mipLevel: 2);
                    Assert.Equal(64, data2.Length);
                    Assert.Equal(2, data2[0]);
                    renderTarget2.Dispose();

                    // Get a Render Target on the mip-level 3 (128), clear it with value 3 and get back the data
                    var renderTarget3 = texture.ToTextureView(ViewType.Single, arraySlice : 0, mipLevel : 3);
                    commandList.Clear(renderTarget3, new Color4(0xFF000003));
                    var data3 = texture.GetData<byte>(commandList, arrayIndex : 0, mipLevel : 3);
                    Assert.Equal(32, data3.Length);
                    Assert.Equal(3, data3[0]);
                    renderTarget3.Dispose();

                    // Release the Texture
                    texture.Dispose();
                });
        }

        [Fact]
        public void TestTexture2D()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256 * 256];
                    data[0] = 255;
                    data[31] = 1;

                    var texture = Texture.New2D(device, width: 256, height: 256, PixelFormat.R8_UNorm, data, usage: GraphicsResourceUsage.Default);

                    // Perform a Texture read-back and test
                    CheckTexture(game.GraphicsContext, texture, data);

                    // Release the Texture
                    texture.Dispose();
                },
                GraphicsProfile.Level_9_1);
        }

        [Fact]
        public void TestTexture2DArray()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256 * 256];
                    data[0] = 255;
                    data[31] = 1;

                    var texture = Texture.New2D(device, width: 256, height: 256, mipCount: 1, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget, arraySize: 4);

                    // Verify the number of mip-levels
                    Assert.Equal(1, texture.MipLevelCount);

                    // Get a Render Target on the array slice 1 (128), clear it with value 1 and get back the data
                    var renderTarget1 = texture.ToTextureView(ViewType.Single, arraySlice: 1, mipLevel: 0);
                    Assert.Equal(256, renderTarget1.ViewWidth);
                    Assert.Equal(256, renderTarget1.ViewHeight);

                    commandList.Clear(renderTarget1, new Color4(0xFF000001));
                    var data1 = texture.GetData<byte>(commandList, arrayIndex: 1);
                    Assert.Equal(data.Length, data1.Length);
                    Assert.Equal(1, data1[0]);
                    renderTarget1.Dispose();

                    // Get a Render Target on the array slice 2 (128), clear it with value 2 and get back the data
                    var renderTarget2 = texture.ToTextureView(ViewType.Single, arraySlice: 2, mipLevel: 0);
                    commandList.Clear(renderTarget2, new Color4(0xFF000002));
                    var data2 = texture.GetData<byte>(commandList, arrayIndex: 2);
                    Assert.Equal(data.Length, data2.Length);
                    Assert.Equal(2, data2[0]);
                    renderTarget2.Dispose();

                    // Get a Render Target on the array slice 3 (128), clear it with value 3 and get back the data
                    var renderTarget3 = texture.ToTextureView(ViewType.Single, arraySlice: 3, mipLevel: 0);
                    commandList.Clear(renderTarget3, new Color4(0xFF000003));
                    var data3 = texture.GetData<byte>(commandList, arrayIndex: 3);
                    Assert.Equal(data.Length, data3.Length);
                    Assert.Equal(3, data3[0]);
                    renderTarget3.Dispose();

                    // Release the Texture
                    texture.Dispose();
                });
        }

        [SkippableFact]
        public void TestTexture2DUnorderedAccess()
        {
            SkipTestForGraphicPlatform(GraphicsPlatform.OpenGL);
            SkipTestForGraphicPlatform(GraphicsPlatform.OpenGLES);

            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[256 * 256];
                    data[0] = 255;
                    data[31] = 1;

                    var texture = Texture.New2D(device, width: 256, height: 256, mipCount: 1, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, arraySize: 4);

                    // Clear array slice 1 with value 1, read back data from the Texture and check validity
                    var texture1 = texture.ToTextureView(ViewType.Single, arraySlice: 1, mipLevel: 0);
                    Assert.Equal(256, texture1.ViewWidth);
                    Assert.Equal(256, texture1.ViewHeight);
                    Assert.Equal(1, texture1.ViewDepth);

                    commandList.ClearReadWrite(texture1, new Int4(1));
                    var data1 = texture.GetData<byte>(commandList, arrayIndex: 1);
                    Assert.Equal(data.Length, data1.Length);
                    Assert.Equal(1, data1[0]);
                    texture1.Dispose();

                    // Clear array slice 2 with value 2, read back data from the Texture and check validity
                    var texture2 = texture.ToTextureView(ViewType.Single, arraySlice: 2, mipLevel: 0);
                    commandList.ClearReadWrite(texture2, new Int4(2));
                    var data2 = texture.GetData<byte>(commandList, arrayIndex: 2);
                    Assert.Equal(data.Length, data2.Length);
                    Assert.Equal(2, data2[0]);
                    texture2.Dispose();

                    texture.Dispose();
                },
                GraphicsProfile.Level_11_0); // Force to use Level11 in order to use UnorderedAccessViews
        }

        [Fact]
        public void TestTexture3D()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var data = new byte[32 * 32 * 32];
                    data[0] = 255;
                    data[31] = 1;

                    var texture = Texture.New3D(device, width: 32, height: 32, depth: 32, PixelFormat.R8_UNorm, data, usage: GraphicsResourceUsage.Default);

                    // Perform a Texture read-back and test
                    CheckTexture(game.GraphicsContext, texture, data);

                    // Release the Texture
                    texture.Dispose();
                });
        }

        /// <summary>
        ///   Validates the integrity of a Texture's data by comparing it with a provided data array
        ///   and performing a round-trip data update and verification on the GPU.
        /// </summary>
        /// <param name="graphicsContext">The graphics context used to execute GPU commands.</param>
        /// <param name="texture">The Texture to validate and update.</param>
        /// <param name="data">
        ///   A byte array containing the initial data to compare against the Texture and to use
        ///   for updating it.
        /// </param>
        private static void CheckTexture(GraphicsContext graphicsContext, Texture texture, byte[] data)
        {
            // Get back the data from the GPU
            var data2 = texture.GetData<byte>(graphicsContext.CommandList);

            // Assert that data are the same
            Assert.True(data.AsSpan().SequenceEqual(data2.AsSpan()));

            // Sets new data on the GPU
            data[0] = 1;
            data[31] = 255;
            texture.SetData(graphicsContext.CommandList, data);

            // Get back the data from the GPU
            data2 = texture.GetData<byte>(graphicsContext.CommandList);

            // Assert that data are the same
            Assert.True(data.AsSpan().SequenceEqual(data2.AsSpan()));
        }

        [Fact]
        public void TestTexture3DRenderTarget()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var texture = Texture.New3D(device, width: 32, height: 32, depth: 32, MipMapCount.Auto, PixelFormat.R8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

                    // Get a Render Target on the mip-level 1 of the Texture 3D
                    var renderTarget0 = texture.ToTextureView(ViewType.Single, arraySlice: 0, mipLevel: 0);
                    commandList.Clear(renderTarget0, new Color4(0xFF000001));
                    var data1 = texture.GetData<byte>(commandList);
                    Assert.Equal(32 * 32 * 32, data1.Length);
                    Assert.Equal(1, data1[0]);
                    renderTarget0.Dispose();

                    // Get a Render Target on the mip-level 2 of the Texture 3D
                    var renderTarget1 = texture.ToTextureView(ViewType.Single, arraySlice: 0, mipLevel: 1);

                    // Check that width/height is correctly calculated
                    Assert.Equal(32 >> 1, renderTarget1.ViewWidth);
                    Assert.Equal(32 >> 1, renderTarget1.ViewHeight);

                    commandList.Clear(renderTarget1, new Color4(0xFF000001));
                    var data2 = texture.GetData<byte>(commandList, arrayIndex: 0, mipLevel: 1);
                    Assert.Equal(16 * 16 * 16, data2.Length);
                    Assert.Equal(1, data2[0]);
                    renderTarget1.Dispose();

                    // Release the Texture
                    texture.Dispose();
                });
        }

        [SkippableFact]
        public void TestDepthStencilBuffer()
        {
            SkipTestForGraphicPlatform(GraphicsPlatform.OpenGLES);

            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    // Check that read-only is not supported for Depth-Stencil Buffer (unless on Direct3D11)
                    var supported = GraphicsDevice.Platform != GraphicsPlatform.Direct3D11;
                    Assert.Equal(supported, device.Features.HasDepthAsReadOnlyRT);

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var texture = Texture.New2D(device, width: 256, height: 256, PixelFormat.D32_Float, TextureFlags.DepthStencil);

                    // Clear the Depth-Stencil Buffer with a depth value of 0.5f
                    commandList.Clear(texture, DepthStencilClearOptions.DepthBuffer, depth: 0.5f);

                    var values = texture.GetData<float>(commandList);
                    Assert.Equal(256 * 256, values.Length);
                    Assert.True(MathUtil.WithinEpsilon(values[0], 0.5f, epsilon: 0.00001f));

                    // Create a new copy of the Depth-Stencil Buffer
                    var textureCopy = texture.CreateDepthTextureCompatible();

                    commandList.Copy(texture, textureCopy);

                    values = textureCopy.GetData<float>(commandList);
                    Assert.Equal(256 * 256, values.Length);
                    Assert.True(MathUtil.WithinEpsilon(values[0], 0.5f, epsilon: 0.00001f));

                    // Dispose the Depth-Stencil Buffer
                    textureCopy.Dispose();
                },
                GraphicsProfile.Level_10_0);
        }

        [SkippableFact(Skip = "Clear on a Read-Only Depth-Stencil Buffer should be undefined or throw exception; we should rewrite this test to do actual rendering with ReadOnly depth stencil bound?")]
        public void TestDepthStencilBufferWithNativeReadonly()
        {
            SkipTestForGraphicPlatform(GraphicsPlatform.OpenGLES);

            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    //// Without shaders, it is difficult to check this method without accessing internals

                    // Check that read-only is not supported for depth stencil buffer
                    Assert.True(Texture.IsDepthStencilReadOnlySupported(device));

                    // Check Texture creation with an array of data, with usage default to later allow SetData
                    var texture = Texture.New2D(device, width: 256, height: 256, PixelFormat.D32_Float, TextureFlags.ShaderResource | TextureFlags.DepthStencilReadOnly);

                    // Clear the Depth-Stencil Buffer with a value of 0.5f, but the depth buffer is readonly
                    commandList.Clear(texture, DepthStencilClearOptions.DepthBuffer, 0.5f);

                    var values = texture.GetData<float>(commandList);
                    Assert.Equal(256 * 256, values.Length);
                    Assert.Equal(0.0f, values[0]);

                    // Dispose the Depth-Stencil Buffer
                    texture.Dispose();
                },
                GraphicsProfile.Level_11_0);
        }

        /// <summary>
        ///   This test loads several images using <see cref="Texture.Load"/> (on the GPU) and saves them to the disk
        ///   using <see cref="Texture.Save"/>. The saved image is then compared with the original image to check that
        ///   the whole chain (CPU -> GPU, GPU -> CPU) is passing correctly the Textures.
        /// </summary>
        [SkippableTheory, MemberData(nameof(ImageFileTypes))]
        public void TestLoadSave(ImageFileType sourceFormat)
        {
            Skip.If(Platform.Type == PlatformType.Android && (
                // TODO: Remove this when mipmap copy is supported on OpenGL by the engine
                sourceFormat is ImageFileType.Stride or ImageFileType.Dds ||
                // TODO: Remove when the Tiff format is supported on Android
                sourceFormat == ImageFileType.Tiff),
                reason: "Unsupported case for Android");

            Skip.If(sourceFormat is ImageFileType.Wmp, reason: "No input image of this format");

            // TODO: Remove this when Load/Save methods are implemented for these types
            Skip.If(sourceFormat is ImageFileType.Wmp or ImageFileType.Tga, reason: "Load/Save not implemented for this format");

            PerformTest(
                game =>
                {
                    var intermediateFormat = ImageFileType.Stride;

                    var device = game.GraphicsDevice;
                    var fileName = sourceFormat.ToFileExtension()[1..] + "Image";
                    var filePath = "ImageTypes/" + fileName;

                    var testMemoryBefore = GC.GetTotalMemory(forceFullCollection: true);
                    var stopwatch = Stopwatch.StartNew();

                    // Load an image from a file and dispose it
                    Texture texture;
                    using (var inStream = game.Content.OpenAsStream(filePath))
                        texture = Texture.Load(device, inStream);

                    // Save the Texture to a memory stream in the intermediate format
                    var tempStream = new MemoryStream();
                    texture.Save(game.GraphicsContext.CommandList, tempStream, intermediateFormat);
                    tempStream.Position = 0;
                    texture.Dispose();

                    using (var inStream = game.Content.OpenAsStream(filePath))
                    {
                        using var originalImage = Image.Load(inStream);
                        using var textureImage = Image.Load(tempStream);

                        TestImage.CompareImage(originalImage, textureImage, ignoreAlpha: false, allowedDifference: 0, fileName);
                    }

                    tempStream.Dispose();

                    var time = stopwatch.ElapsedMilliseconds;
                    stopwatch.Stop();

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    var testMemoryAfter = GC.GetTotalMemory(forceFullCollection: true);

                    Log.Info($"Test loading {fileName} GPU texture / saving to {intermediateFormat} and compare with original Memory {testMemoryAfter - testMemoryBefore} delta bytes, in {time}ms");
                },
                GraphicsProfile.Level_9_1);
        }

        [SkippableTheory, MemberData(nameof(ImageFileTypes))]
        public void TestLoadDraw(ImageFileType sourceFormat)
        {
            Skip.If(sourceFormat is ImageFileType.Wmp, reason: "No input image of this format");

            // TODO: Remove this when Load/Save methods are implemented for these types
            Skip.If(sourceFormat is ImageFileType.Wmp or ImageFileType.Tga, reason: "Load/Save not implemented for this format");
            Skip.If(Platform.Type == PlatformType.Android && sourceFormat == ImageFileType.Tiff, reason: "Load/Save not implemented for this format");

            PerformDrawTest(
                (game, context) =>
                {
                    game.TestName = $"{nameof(TestLoadDraw)}({sourceFormat})";

                    var device = game.GraphicsDevice;
                    var commandList = context.CommandList;

                    commandList.Clear(commandList.RenderTarget, new Color4(Color.Green).ToColorSpace(ColorSpace.Linear));
                    commandList.Clear(commandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

                    var fileName = sourceFormat.ToFileExtension()[1..] + "Image";
                    var filePath = "ImageTypes/" + fileName;

                    // Load an image from a file and dispose it
                    Texture texture;
                    using (var inStream = game.Content.OpenAsStream(filePath))
                        texture = Texture.Load(device, inStream, loadAsSrgb: true);

                    game.GraphicsContext.DrawTexture(texture, BlendStates.AlphaBlend);
                },
                GraphicsProfile.Level_9_1);
        }

        [Theory]
        [InlineData(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Staging)]
        [InlineData(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Default)]
        [InlineData(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Staging)]
        [InlineData(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Default)]
        public void TestGetData(GraphicsProfile profile, GraphicsResourceUsage usage)
        {
            // TODO: Modify this when when supported on OpenGL
            var testArray = profile >= GraphicsProfile.Level_10_0;
            // TODO: Remove this limitation when GetData is fixed on OpenGL ES for mip-levels other than 0
            var mipmaps = GraphicsDevice.Platform == GraphicsPlatform.OpenGLES && profile < GraphicsProfile.Level_10_0 ? 1 : 3;

            PerformTest(
                game =>
                {
                    const int width = 16;
                    const int height = width;
                    var arraySize = testArray ? 2 : 1;

                    TextureFlags[] flags = usage is GraphicsResourceUsage.Default
                        ? [ TextureFlags.ShaderResource, TextureFlags.RenderTarget, TextureFlags.RenderTarget | TextureFlags.ShaderResource ]
                        : [ TextureFlags.None ];

                    var pixelFormat = PixelFormat.R8G8B8A8_UNorm;
                    var data = CreateDebugTextureData(width, height, mipmaps, arraySize, pixelFormat, DefaultColorComputer);

                    foreach (var flag in flags)
                    {
                        using var texture = CreateDebugTexture(game.GraphicsDevice, data, width, height, mipmaps, arraySize, pixelFormat, flag, usage);
                        CheckDebugTextureData(game.GraphicsContext, texture, width, height, mipmaps, arraySize, pixelFormat, flag, usage, DefaultColorComputer);
                    }
                },
                profile);
        }

        [Theory]
        [InlineData(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Staging)]
        [InlineData(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Staging)]
        [InlineData(GraphicsProfile.Level_9_1, GraphicsResourceUsage.Default)]
        [InlineData(GraphicsProfile.Level_10_0, GraphicsResourceUsage.Default)]
        public void TestCopy(GraphicsProfile profile, GraphicsResourceUsage usageSource)
        {
            // TODO: Modify this when when supported on OpenGL
            var testArray = profile >= GraphicsProfile.Level_10_0;
            // TODO: Remove this limitation when GetData is fixed on OpenGL ES for mip-levels other than 0
            var mipmaps = GraphicsDevice.Platform == GraphicsPlatform.OpenGLES && profile < GraphicsProfile.Level_10_0 ? 1 : 3;

            PerformTest(
                game =>
                {
                    const int width = 16;
                    const int height = width;
                    var arraySize = testArray ? 2 : 1;

                    PixelFormat[] pixelFormats = [ PixelFormat.R8G8B8A8_UNorm, PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.R8_UNorm ];

                    bool[] destinationIsStaged = [ true, false ];
                    foreach (var destinationStaged in destinationIsStaged)
                    {
                        foreach (var pixelFormat in pixelFormats)
                        {
                            ColorComputer colorComputer = pixelFormat.SizeInBytes == 1
                                ? ColorComputerR8
                                : DefaultColorComputer;

                            var data = CreateDebugTextureData(width, height, mipmaps, arraySize, pixelFormat, colorComputer);

                            TextureFlags[] sourceFlags = usageSource == GraphicsResourceUsage.Default
                                ? [ TextureFlags.ShaderResource, TextureFlags.RenderTarget, TextureFlags.RenderTarget | TextureFlags.ShaderResource ]
                                : [ TextureFlags.None ];

                            foreach (var flag in sourceFlags)
                            {
                                using var texture = CreateDebugTexture(game.GraphicsDevice, data, width, height, mipmaps, arraySize, pixelFormat, flag, usageSource);
                                using var copyTexture = destinationStaged ? texture.ToStaging() : texture.Clone();

                                game.GraphicsContext.CommandList.Copy(texture, copyTexture);

                                CheckDebugTextureData(game.GraphicsContext, copyTexture, width, height, mipmaps, arraySize, pixelFormat, flag, usageSource, colorComputer);
                            }
                        }
                    }
                },
                profile);
        }

        /// <summary>
        ///   Creates a byte array representing Texture data for debugging purposes by iterating over each pixel
        ///   in the specified dimensions, mipmap levels, and array slices, and using the <paramref name="dataComputer"/>
        ///   delegate to compute the value of each byte based on its position and context.
        /// </summary>
        /// <param name="width">The width of the Texture in pixels.</param>
        /// <param name="height">The height of the Texture in pixels.</param>
        /// <param name="mipmaps">The number of mipmap levels to generate. Must be greater than or equal to 1.</param>
        /// <param name="arraySize">The number of Texture array slices. Must be greater than or equal to 1.</param>
        /// <param name="format">The pixel format of the Texture.</param>
        /// <param name="dataComputer">A delegate that computes the value of each byte in the Texture data.</param>
        /// <returns>A byte array containing the generated Texture data.</returns>
        private static byte[] CreateDebugTextureData(int width, int height, int mipmaps, int arraySize, PixelFormat format, ColorComputer dataComputer)
        {
            var formatSize = format.SizeInBytes;

            var mipmapSize = 0;
            for (int i = 0; i < mipmaps; i++)
                mipmapSize += (width >> i) * (height >> i);

            var dataSize = arraySize * mipmapSize;
            var data = new byte[dataSize * formatSize];

            var offset = 0;
            for (int arraySlice = 0; arraySlice < arraySize; arraySlice++)
            for (int mipLevel = 0; mipLevel < mipmaps; mipLevel++)
            {
                var mipWidth = width >> mipLevel;
                var mipHeight = height >> mipLevel;

                for (int row = 0; row < mipHeight; row++)
                for (int col = 0; col < mipWidth; col++)
                {
                    for (int i = 0; i < formatSize; i++)
                    {
                        data[offset + (row * mipWidth + col) * formatSize + i] = dataComputer(col, row, mipLevel, arraySlice, i);
                    }
                }

                offset += mipWidth * mipHeight * formatSize;
            }

            return data;
        }

        /// <summary>
        ///   Creates a 2D debug Texture with the specified parameters and data.
        /// </summary>
        /// <remarks>
        ///   The provided <paramref name="data"/> must contain enough bytes to populate all mipmap levels
        ///   and array slices based on the specified parameters.
        /// </remarks>
        /// <param name="device">The <see cref="GraphicsDevice"/> used to create the Texture.</param>
        /// <param name="data">A byte array containing the raw Texture data.</param>
        /// <param name="width">The width of the Texture in pixels.</param>
        /// <param name="height">The height of the Texture in pixels.</param>
        /// <param name="mipmaps">The number of mipmap levels to include in the Texture. Must be greater than or equal to 1.</param>
        /// <param name="arraySize">The number of array slices in the Texture. Must be greater than or equal to 1.</param>
        /// <param name="format">The pixel format of the Texture.</param>
        /// <param name="flags">
        ///   The <see cref="TextureFlags"/> that specify additional options for the Texture,
        ///   such as whether it can be treated as a Render Target or a Shader Resource View.
        /// </param>
        /// <param name="usage">
        ///   The <see cref="GraphicsResourceUsage"/> that specifies how the Texture will be used
        ///   (e.g., static, dynamic, or staging).
        /// </param>
        /// <returns>
        ///   The created <see cref="Texture"/>.
        /// </returns>
        private unsafe Texture CreateDebugTexture(GraphicsDevice device, byte[] data, int width, int height, int mipmaps, int arraySize, PixelFormat format, TextureFlags flags, GraphicsResourceUsage usage)
        {
            var sizeInBytes = format.SizeInBytes;

            var offset = 0;
            var dataBoxes = new DataBox[arraySize * mipmaps];

            fixed (byte* ptrData = data)
            {
                for (int arraySlice = 0; arraySlice < arraySize; arraySlice++)
                for (int mipLevel = 0; mipLevel < mipmaps; mipLevel++)
                {
                    var mipWidth = width >> mipLevel;
                    var mipHeight = height >> mipLevel;
                    var rowStride = mipWidth * sizeInBytes;
                    var sliceStride = rowStride * mipHeight;

                    dataBoxes[arraySlice * mipmaps + mipLevel] = new DataBox((IntPtr) ptrData + offset, rowStride, sliceStride);

                    offset += sliceStride;
                }

                return Texture.New2D(device, width, height, mipmaps, format, dataBoxes, flags, arraySize, usage);
            }
        }

        /// <summary>
        ///   Validates the data of a debug Texture by comparing its pixel values against expected values
        ///   provided by a <see cref="ColorComputer"/> delegate.
        /// </summary>
        /// <param name="graphicsContext">The graphics context used to access the Texture data.</param>
        /// <param name="debugTexture">The Texture to validate.</param>
        /// <param name="width">The width of the Texture in pixels.</param>
        /// <param name="height">The height of the Texture in pixels.</param>
        /// <param name="mipmaps">The number of mipmap levels to include in the Texture. Must be greater than or equal to 1.</param>
        /// <param name="arraySize">The number of array slices in the Texture. Must be greater than or equal to 1.</param>
        /// <param name="format">The pixel format of the Texture.</param>
        /// <param name="flags">
        ///   The <see cref="TextureFlags"/> that specify additional options for the Texture,
        ///   such as whether it can be treated as a Render Target or a Shader Resource View.
        /// </param>
        /// <param name="usage">
        ///   The <see cref="GraphicsResourceUsage"/> that specifies how the Texture will be used
        ///   (e.g., static, dynamic, or staging).
        /// </param>
        /// <param name="dataComputer">
        ///   A delegate that computes the expected value for a given pixel given the column, row, mip-level,
        ///   array slice, and byte index of the pixel.
        /// </param>
        private void CheckDebugTextureData(GraphicsContext graphicsContext, Texture debugTexture,
            int width, int height, int mipmaps, int arraySize,
            PixelFormat format, TextureFlags flags, GraphicsResourceUsage usage, ColorComputer dataComputer)
        {
            var pixelSize = format.SizeInBytes;

            for (int arraySlice = 0; arraySlice < arraySize; arraySlice++)
            for (int mipLevel = 0; mipLevel < mipmaps; mipLevel++)
            {
                var mipWidth = width >> mipLevel;
                var mipHeight = height >> mipLevel;

                var debugMipData = debugTexture.GetData<byte>(graphicsContext.CommandList, arraySlice, mipLevel);

                for (int row = 0; row < mipHeight; row++)
                for (int col = 0; col < mipWidth; col++)
                {
                    for (int i = 0; i < pixelSize; i++)
                    {
                        var value = debugMipData[(row * mipWidth + col) * pixelSize + i];
                        var expectedValue = dataComputer(col, row, mipLevel, arraySlice, i);

                        Assert.True(expectedValue.Equals(value),
                            $"The Texture data at [{col}, {row}] for mip-level '{mipLevel}' and slice '{arraySlice}' with flags '{flags}', usage '{usage}' and format '{format}' is not valid. " +
                            $"Expected '{expectedValue}' but was '{value}' at index '{i}'");
                    }
                }
            }
        }

        /// <summary>
        ///   A default <see cref="ColorComputer"/> that computes byte values based on the pixel's parameters
        ///   just based on its position.
        /// </summary>
        private byte DefaultColorComputer(int x, int y, int mipmapSlice, int arraySlice, int index)
        {
            return index switch
            {
                0 => (byte) x,
                1 => (byte) y,
                2 => (byte) mipmapSlice,
                3 => (byte) arraySlice,
                _ => byte.MaxValue
            };
        }

        /// <summary>
        ///   A simple <see cref="ColorComputer"/> that computes an 8-bit color value based on
        ///   the provided coordinates, mipmap level, array slice, and index.
        /// </summary>
        private byte ColorComputerR8(int x, int y, int mipmapSlice, int arraySlice, int index)
        {
            return (byte)(arraySlice * 100 + mipmapSlice * 20 + (x >> 2) + (y >> 2) * 4);
        }

        private delegate byte ColorComputer(int x, int y, int mipmapSlice, int arraySlice, int index);
    }
}
