// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter convert a <see cref="Nullable"/> type to its underlying type. If the type is not nullable, it returns the type itself.
/// </summary>
public sealed class UnderlyingType : OneWayValueConverter<UnderlyingType>
{
    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Type type)
        {
            throw new ArgumentException("The object passed to this value converter is not a type.");
        }
        return Nullable.GetUnderlyingType(type) ?? value;
    }
}
