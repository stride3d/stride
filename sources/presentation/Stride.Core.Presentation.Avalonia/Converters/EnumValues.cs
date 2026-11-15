// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Stride.Core.Extensions;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class EnumValues : OneWayValueConverter<EnumValues>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Type enumType)
            return null;

        if (!enumType.IsEnum)
        {
            enumType = Nullable.GetUnderlyingType(enumType);
            if (enumType is not { IsEnum: true })
                return null;
        }

        if (enumType.GetCustomAttribute<FlagsAttribute>(false) != null)
        {
            var query = EnumExtensions.GetIndividualFlags(enumType);
            return query;
        }
        else
        {
            var query = Enum.GetValues(enumType).Cast<object>().Distinct().ToList();
            return query;
        }
    }
}
