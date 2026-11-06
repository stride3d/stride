// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Stride.Editor.Avalonia.Converters;
using Stride.Graphics;

namespace Stride.Editor.Avalonia.Tests;

public sealed class ImageToBitmapTests
{
    [AvaloniaTheory]
    [InlineData(PixelFormat.R8G8B8A8_UNorm)]
    [InlineData(PixelFormat.R8G8B8A8_UNorm_SRgb)]
    [InlineData(PixelFormat.B8G8R8A8_UNorm)]
    [InlineData(PixelFormat.B8G8R8A8_UNorm_SRgb)]
    public void AnImageInADisplayableFormatBecomesABitmapOfTheSameSize(PixelFormat format)
    {
        using var image = Image.New2D(4, 2, 1, format);

        var bitmap = Assert.IsType<Bitmap>(new ImageToBitmap().Convert(image, typeof(object), null, CultureInfo.InvariantCulture));

        Assert.Equal(new PixelSize(4, 2), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public unsafe void ThePixelsAreCopiedRowByRow()
    {
        // 5 pixels wide: the row stride of the image is not a multiple of 16 bytes, so a wrong stride would shear the rows
        using var image = Image.New2D(5, 3, 1, PixelFormat.B8G8R8A8_UNorm);
        var pixels = (uint*)image.PixelBuffer[0].DataPointer;
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 5; x++)
                pixels[y * 5 + x] = 0xFF000000u | (uint)(x * 10 + y);

        var bitmap = ImageToBitmap.Convert(image);

        Assert.NotNull(bitmap);
        var copy = new uint[15];
        fixed (uint* target = copy)
            bitmap!.CopyPixels(new PixelRect(0, 0, 5, 3), (IntPtr)target, copy.Length * sizeof(uint), 5 * sizeof(uint));
        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 5; x++)
                Assert.Equal(0xFF000000u | (uint)(x * 10 + y), copy[y * 5 + x]);
    }

    [AvaloniaFact]
    public void AnImageInAnotherFormatIsNotConverted()
    {
        using var image = Image.New2D(4, 2, 1, PixelFormat.R16G16B16A16_Float);

        Assert.Null(ImageToBitmap.Convert(image));
        Assert.Same(AvaloniaProperty.UnsetValue, new ImageToBitmap().Convert(image, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [AvaloniaFact]
    public void AnythingElseIsNotConverted()
    {
        Assert.Same(AvaloniaProperty.UnsetValue, new ImageToBitmap().Convert("not an image", typeof(object), null, CultureInfo.InvariantCulture));
        Assert.Same(AvaloniaProperty.UnsetValue, new ImageToBitmap().Convert(null, typeof(object), null, CultureInfo.InvariantCulture));
    }
}
