// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class AngleSingleToDegrees : ValueConverterBase<AngleSingleToDegrees>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return targetType == typeof(double) ? ConverterHelper.ConvertToAngleSingle(value).Degrees : ConverterHelper.TryConvertToAngleSingle(value)?.Degrees;
    }

    /// <inheritdoc/>
    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var doubleValue = targetType == typeof(AngleSingle) ? ConverterHelper.ConvertToDouble(value, culture) : ConverterHelper.TryConvertToDouble(value, culture);
        return doubleValue is not null ? new AngleSingle((float)doubleValue.Value, AngleType.Degree) : null;
    }
}
