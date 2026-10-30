// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts the presenter of a thumbnail (<see cref="Services.ThumbnailData.Presenter"/>) into an image that Avalonia can show.
/// </summary>
/// <remarks>
/// The thumbnails of the engine are engine images, which this layer doesn't know: the editor that builds them sets
/// <see cref="PresenterConverter"/>.
/// </remarks>
public sealed class ThumbnailToImage : OneWayValueConverter<ThumbnailToImage>
{
    /// <summary>
    /// Gets or sets the converter of a presenter that isn't an Avalonia image already.
    /// </summary>
    public static IValueConverter? PresenterConverter { get; set; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => AvaloniaProperty.UnsetValue,
            IImage image => image,
            _ => PresenterConverter?.Convert(value, targetType, parameter, culture) ?? AvaloniaProperty.UnsetValue,
        };
    }
}
