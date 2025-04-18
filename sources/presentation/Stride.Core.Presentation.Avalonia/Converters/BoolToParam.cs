// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter will convert a boolean to the object given in parameter if its true,
/// and to <see cref="AvaloniaProperty.UnsetValue"/> if it's false.
/// <see cref="ConvertBack"/> is supported and will return whether the given object is different from
/// <see cref="AvaloniaProperty.UnsetValue"/>.
/// </summary>
public sealed class BoolToParam : ValueConverterBase<BoolToParam>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = ConverterHelper.ConvertToBoolean(value, culture);
        return result ? parameter : AvaloniaProperty.UnsetValue;
    }

    /// <inheritdoc/>
    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value != AvaloniaProperty.UnsetValue;
        return result.Box();
    }
}
