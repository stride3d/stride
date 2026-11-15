// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter will convert a <see cref="char"/> value to a string containing only this char.
/// </summary>
public sealed class CharToString : ValueConverterBase<CharToString>
{
    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is char c ? c.ToString() : string.Empty;
    }

    /// <inheritdoc/>
    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value as string;
        if (!string.IsNullOrEmpty(str))
            return str[0];

        return targetType == typeof(char) ? '\0' : null;
    }
}
