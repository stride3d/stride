// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Extensions;

using AvaloniaColor = Avalonia.Media.Color;

namespace Stride.Core.Presentation.Avalonia.Converters;

using Color = Mathematics.Color;

public sealed class ColorConverter : ValueConverterBase<ColorConverter>
{
    public Type? SourceType { get; set; }

    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return DoConvert(value, targetType);
    }

    /// <inheritdoc/>
    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return targetType == typeof(object) ? value : DoConvert(value, targetType);
    }

    private static object DoConvert(object? value, Type targetType)
    {
        if (value is ISolidColorBrush brush)
            value = brush.Color;

        switch (value)
        {
            case Color color:
            {
                if (targetType == typeof(AvaloniaColor))
                    return color.ToAvaloniaColor();
                if (targetType == typeof(Color))
                    return color;
                if (targetType == typeof(Color3))
                    return color.ToColor3();
                if (targetType == typeof(Color4))
                    return color.ToColor4();
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(color.ToAvaloniaColor());
                if (targetType == typeof(string))
                    return ColorExtensions.RgbaToString(color.ToRgba());
                break;
            }
            case Color3 color3:
            {
                if (targetType == typeof(AvaloniaColor))
                    return color3.ToAvaloniaColor();
                if (targetType == typeof(Color))
                    return (Color)color3;
                if (targetType == typeof(Color3))
                    return color3;
                if (targetType == typeof(Color4))
                    return color3.ToColor4();
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(color3.ToAvaloniaColor());
                if (targetType == typeof(string))
                    return ColorExtensions.RgbToString(color3.ToRgb());
                break;
            }
            case Color4 color4:
            {
                if (targetType == typeof(AvaloniaColor))
                    return color4.ToAvaloniaColor();
                if (targetType == typeof(Color))
                    return (Color)color4;
                if (targetType == typeof(Color3))
                    return color4.ToColor3();
                if (targetType == typeof(Color4))
                    return color4;
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(color4.ToAvaloniaColor());
                if (targetType == typeof(string))
                    return ColorExtensions.RgbaToString(color4.ToRgba());
                break;
            }
            case AvaloniaColor aColor:
            {
                if (targetType == typeof(AvaloniaColor))
                    return aColor;
                var colorValue = new Color(aColor.R, aColor.G, aColor.B, aColor.A);
                if (targetType == typeof(Color))
                    return colorValue;
                if (targetType == typeof(Color3))
                    return colorValue.ToColor3();
                if (targetType == typeof(Color4))
                    return colorValue.ToColor4();
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(aColor);
                if (targetType == typeof(string))
                    return ColorExtensions.RgbaToString(colorValue.ToRgba());
                break;
            }
            case string stringColor:
            {
                var intValue = ColorExtensions.StringToRgba(stringColor);
                if (targetType == typeof(AvaloniaColor))
                    return AvaloniaColor.FromArgb(
                        (byte)((intValue >> 24) & 255),
                        (byte)(intValue & 255),
                        (byte)((intValue >> 8) & 255),
                        (byte)((intValue >> 16) & 255));
                if (targetType == typeof(Color))
                    return Color.FromRgba(intValue);
                if (targetType == typeof(Color3))
                    return new Color3(intValue);
                if (targetType == typeof(Color4))
                    return new Color4(intValue);
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(AvaloniaColor.FromArgb(
                        (byte)((intValue >> 24) & 255),
                        (byte)(intValue & 255),
                        (byte)((intValue >> 8) & 255),
                        (byte)((intValue >> 16) & 255)));
                if (targetType == typeof(string))
                    return stringColor;
                break;
            }
        }

#if DEBUG
        if (value == null || value == AvaloniaProperty.UnsetValue)
            return AvaloniaProperty.UnsetValue;

        throw new NotSupportedException("Requested conversion is not supported.");
#else
        return AvaloniaProperty.UnsetValue;
#endif
    }
}
