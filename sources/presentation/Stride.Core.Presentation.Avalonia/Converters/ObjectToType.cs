// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter convert any object to its type. It accepts null and will return null in this case.
/// </summary>
public sealed class ObjectToType : OneWayValueConverter<ObjectToType>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.GetType();
    }
}
