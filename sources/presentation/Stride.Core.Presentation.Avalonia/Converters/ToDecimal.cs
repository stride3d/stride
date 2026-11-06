// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This value converter will convert any numeric value to decimal. <see cref="ConvertBack"/> is supported and
/// will convert the value to the target if it is numeric, otherwise it returns the value as-is.
/// </summary>
public sealed class ToDecimal : ValueConverterBase<ToDecimal>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return targetType == typeof(decimal) ? ConverterHelper.ConvertToDecimal(value, culture) : ConverterHelper.TryConvertToDecimal(value, culture);
    }

    /// <inheritdoc/>
    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return targetType.IsValueType && !targetType.IsNullable() ? ConverterHelper.ChangeType(value, targetType, culture) : ConverterHelper.TryChangeType(value, targetType, culture);
    }
}
