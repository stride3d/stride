// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// Poor man version of a priority binding, which returns the first value not evaluating to <see cref="AvaloniaProperty.UnsetValue"/>.
/// </summary>
public sealed class PriorityMulti : MultiValueConverterBase<PriorityMulti>
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool atLeastOneValueSet = false;
        bool allowNull = parameter is true;
        foreach (var value in values)
        {
            if (value == AvaloniaProperty.UnsetValue)
                continue;

            atLeastOneValueSet = true;

            if (allowNull || value is not null)
                return value;
        }

        // If all values are unset we propagate for consistency
        return (atLeastOneValueSet && allowNull) ? null : AvaloniaProperty.UnsetValue;
    }
}
