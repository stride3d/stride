
using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Graphics;

namespace Stride.GameStudio.Avalonia.Converters;

public sealed class StrideImage : OneWayValueConverter<StrideImage>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return AvaloniaProperty.UnsetValue;

        try
        {
            // FIXME xplat-editor this is a first attempt ut it's very likely broken
            if (value is Image image)
            {
                var desc = image.Description;
                global::Avalonia.Media.Imaging.Bitmap bitmap = new(
                    ToAvalonia(desc.Format),
                    global::Avalonia.Platform.AlphaFormat.Opaque,
                    image.DataPointer, new(desc.Width, desc.Height), new(96, 96), image.TotalSizeInBytes);

                return bitmap;
            }
        }
        catch (Exception)
        {
        }

        return AvaloniaProperty.UnsetValue;
    }

    private static global::Avalonia.Platform.PixelFormat ToAvalonia(PixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.R8G8B8A8_SInt or PixelFormat.R8G8B8A8_SNorm or PixelFormat.R8G8B8A8_Typeless or PixelFormat.R8G8B8A8_UInt or PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb => global::Avalonia.Platform.PixelFormats.Rgba8888,
            PixelFormat.B8G8R8A8_Typeless or PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb => global::Avalonia.Platform.PixelFormats.Bgra8888,
            _ => default
        };
    }
}
