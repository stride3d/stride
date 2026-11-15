// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Graphics;
using PixelFormat = Stride.Graphics.PixelFormat;

namespace Stride.Editor.Avalonia.Converters;

/// <summary>
/// Converts a <see cref="Image"/> (the presenter of the thumbnails of <c>Stride.Editor</c>) to an Avalonia <see cref="Bitmap"/>.
/// Images in a format Avalonia cannot display convert to <see cref="AvaloniaProperty.UnsetValue"/>.
/// </summary>
public sealed class ImageToBitmap : OneWayValueConverter<ImageToBitmap>
{
    /// <inheritdoc />
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Image image ? Convert(image) ?? AvaloniaProperty.UnsetValue : AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Copies the first mip level of the image into a bitmap.
    /// </summary>
    /// <returns>The bitmap, or <c>null</c> when the format of the image is not one Avalonia displays.</returns>
    public static Bitmap? Convert(Image image)
    {
        if (image.PixelBuffer.Count == 0 || ToAvalonia(image.Description.Format) is not { } format)
            return null;

        var buffer = image.PixelBuffer[0];
        return new Bitmap(format, AlphaFormat.Unpremul, buffer.DataPointer, new PixelSize(buffer.Width, buffer.Height), new Vector(96, 96), buffer.RowStride);
    }

    private static global::Avalonia.Platform.PixelFormat? ToAvalonia(PixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb or PixelFormat.R8G8B8A8_Typeless => PixelFormats.Rgba8888,
            PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb or PixelFormat.B8G8R8A8_Typeless => PixelFormats.Bgra8888,
            _ => null,
        };
    }
}
