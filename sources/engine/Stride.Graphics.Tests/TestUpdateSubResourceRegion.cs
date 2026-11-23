// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
///   Uploading a region of a 2D texture reads its rows a row pitch apart, whatever the slice pitch is.
/// </summary>
public class TestUpdateSubResourceRegion : GraphicTestGameBase
{
    private const int BytesPerPixel = 4;

    [Fact]
    public void SubRegionWithoutSlicePitch()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 64, 32, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
            UploadAndCompare(game, texture, arraySlice: 0, new ResourceRegion(8, 4, 0, 8 + 20, 4 + 10, 1));
        });
    }

    /// <summary>
    ///   The upload is larger than the 4 MiB the device's upload buffer starts with.
    /// </summary>
    [Fact]
    public void UploadLargerThanTheUploadBuffer()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 6636, 200, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
            UploadAndCompare(game, texture, arraySlice: 0, new ResourceRegion(0, 0, 0, 6636, 200, 1));
        });
    }

    [Fact]
    public void ArraySliceWithoutSlicePitch()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 32, 32, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, arraySize: 3);
            UploadAndCompare(game, texture, arraySlice: 2, new ResourceRegion(3, 5, 0, 3 + 17, 5 + 9, 1));
        });
    }

    /// <summary>
    ///   The source rows are padded: the copy reads the row pitch apart, and only the width of the region on the last row.
    /// </summary>
    [Fact]
    public void PaddedRowPitch()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 64, 32, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
            UploadAndCompare(game, texture, arraySlice: 0, new ResourceRegion(5, 2, 0, 5 + 13, 2 + 7, 1), rowPadding: 12);
        });
    }

    private static unsafe void UploadAndCompare(Stride.Graphics.Regression.GameTestBase game, Texture texture, int arraySlice, ResourceRegion region, int rowPadding = 0)
    {
        var commandList = game.GraphicsContext.CommandList;

        var width = region.Right - region.Left;
        var height = region.Bottom - region.Top;
        var rowBytes = width * BytesPerPixel;
        var rowPitch = rowBytes + rowPadding;

        // No padding after the last row, so reading past it would read outside the array
        var source = new byte[rowPitch * (height - 1) + rowBytes];
        for (int i = 0; i < source.Length; i++)
            source[i] = (byte) (i * 31 + i / 251 + 1);

        fixed (byte* sourcePtr = source)
        {
            commandList.UpdateSubResource(texture, arraySlice * texture.MipLevelCount, new DataBox((nint) sourcePtr, rowPitch, slicePitch: 0), region);
        }

        var data = texture.GetData<byte>(commandList, arraySlice);
        var textureRowPitch = texture.Width * BytesPerPixel;

        for (int y = 0; y < height; y++)
        {
            var expected = source.AsSpan(y * rowPitch, rowBytes);
            var actual = data.AsSpan((region.Top + y) * textureRowPitch + region.Left * BytesPerPixel, rowBytes);
            Assert.True(expected.SequenceEqual(actual), $"Row {y} of the region differs from the uploaded data.");
        }
    }
}
