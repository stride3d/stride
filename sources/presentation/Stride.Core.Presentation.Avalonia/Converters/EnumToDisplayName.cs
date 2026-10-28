// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class EnumToDisplayName : OneWayValueConverter<EnumToDisplayName>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return "(None)";

        var stringValue = value.ToString()!;
        var type = value.GetType();
        var memberInfo = type.GetMember(stringValue).FirstOrDefault();
        if (memberInfo is null)
            return stringValue;

        return memberInfo.GetCustomAttribute(typeof(DisplayAttribute), false) is DisplayAttribute attribute
            ? attribute.Name
            : stringValue;
    }
}
