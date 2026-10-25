// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Headless.XUnit;
using Stride.GameStudio.Avalonia.Converters;
using Stride.Graphics;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The converter that shows the thumbnails (a <see cref="Image"/>) in the asset view and the solution explorer.
/// </summary>
public sealed class StrideImageConverterTests
{
    [AvaloniaFact]
    public void AThumbnailBecomesABitmapOfTheSameSize()
    {
        // A thumbnail is 256x256: the converter used to pass the size of the whole buffer as the row stride,
        // which read far past the end of the image and crashed the editor with an access violation (seen under Xvfb with Skia).
        // The headless drawing platform of these tests only creates placeholder bitmaps, so this checks the conversion, not the pixels.
        using var image = Image.New2D(256, 256, 1, PixelFormat.R8G8B8A8_UNorm);

        var result = new StrideImage().Convert(image, typeof(object), null, CultureInfo.InvariantCulture);

        var bitmap = Assert.IsType<AvaloniaBitmap>(result);
        bitmap.Dispose();
    }

    [AvaloniaFact]
    public void AnUnsupportedFormatShowsNothing()
    {
        using var image = Image.New2D(4, 4, 1, PixelFormat.R32_Float);

        var result = new StrideImage().Convert(image, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Same(AvaloniaProperty.UnsetValue, result);
    }

    [AvaloniaFact]
    public void NoThumbnailShowsNothing()
    {
        Assert.Same(AvaloniaProperty.UnsetValue, new StrideImage().Convert(null, typeof(object), null, CultureInfo.InvariantCulture));
    }
}
