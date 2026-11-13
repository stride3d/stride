// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.IO;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter will convert an <see cref="UDirectory"/> object to its string representation. <see cref="ConvertBack"/> is supported.
/// </summary>
/// <seealso cref="UFileToString"/>
public sealed class UDirectoryToString : ValueConverterBase<UDirectoryToString>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.Replace('/', Path.DirectorySeparatorChar);
    }

    /// <inheritdoc/>
    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return null;
        try
        {
            return new UDirectory((string)value);
        }
        catch
        {
            return new UDirectory("");
        }
    }
}
