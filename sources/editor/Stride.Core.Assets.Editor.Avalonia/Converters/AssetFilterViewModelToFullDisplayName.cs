// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts an asset filter to a display name by prefixing the category to the name.
/// </summary>
public sealed class AssetFilterViewModelToFullDisplayName : OneWayValueConverter<AssetFilterViewModelToFullDisplayName>
{
    /// <summary>
    /// Converts an asset filter to a display name by prefixing the category to the name.
    /// </summary>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AssetFilterViewModel filter)
            return null;

        var category = filter.Category switch
        {
            FilterCategory.AssetName => "name",
            FilterCategory.AssetTag  => "tag",
            FilterCategory.AssetType => "type",
            _ => filter.Category.ToString().ToLowerInvariant(),
        };
        return $"{category}: {filter.DisplayName}";
    }
}
