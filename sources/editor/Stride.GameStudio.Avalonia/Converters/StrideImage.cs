// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Graphics;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace Stride.GameStudio.Avalonia.Converters;

/// <summary>
/// Converts a <see cref="Image"/> (the presenter of the thumbnail data) into an Avalonia bitmap.
/// Only the first pixel buffer (first mip level of the first array slice) is shown.
/// </summary>
/// <remarks>
/// Each image is converted once: the bitmap is shared by every item that shows the image (the assets with the same static
/// thumbnail, the asset view and the solution explorer) and by every later evaluation of their bindings, and it lives as long as
/// the image. The bitmaps belong to the converter: the views must not dispose them.
/// </remarks>
public sealed class StrideImage : OneWayValueConverter<StrideImage>
{
    private static readonly ConditionalWeakTable<Image, AvaloniaBitmap?> Bitmaps = [];

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Image image || image.PixelBuffer.Count == 0)
            return AvaloniaProperty.UnsetValue;

        return Bitmaps.GetValue(image, CreateBitmap) ?? AvaloniaProperty.UnsetValue;
    }

    private static AvaloniaBitmap? CreateBitmap(Image image)
    {
        try
        {
            var buffer = image.PixelBuffer[0];
            if (ToAvalonia(buffer.Format) is not { } format)
                return null;

            // The bitmap copies the pixels. The last argument is the stride of a row, not the size of the whole buffer.
            return new AvaloniaBitmap(
                format,
                global::Avalonia.Platform.AlphaFormat.Unpremul,
                buffer.DataPointer,
                new PixelSize(buffer.Width, buffer.Height),
                new Vector(96, 96),
                buffer.RowStride);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static global::Avalonia.Platform.PixelFormat? ToAvalonia(PixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.R8G8B8A8_SInt or PixelFormat.R8G8B8A8_SNorm or PixelFormat.R8G8B8A8_Typeless or PixelFormat.R8G8B8A8_UInt or PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb => global::Avalonia.Platform.PixelFormats.Rgba8888,
            PixelFormat.B8G8R8A8_Typeless or PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb => global::Avalonia.Platform.PixelFormats.Bgra8888,
            _ => null,
        };
    }
}
