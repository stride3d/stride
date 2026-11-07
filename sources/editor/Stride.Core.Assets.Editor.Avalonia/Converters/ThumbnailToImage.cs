// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Gives the image of a thumbnail (<see cref="Stride.Core.Assets.Presentation.Services.ThumbnailData.Presenter"/>) to an Avalonia <c>Image</c>: the thumbnails of
/// the Avalonia editor are Avalonia bitmaps (<see cref="Stride.Core.Assets.Editor.Avalonia.Services.ThumbnailDataFactory"/>), so this only filters out what
/// isn't one.
/// </summary>
public sealed class ThumbnailToImage : OneWayValueConverter<ThumbnailToImage>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IImage image ? image : AvaloniaProperty.UnsetValue;
    }
}
