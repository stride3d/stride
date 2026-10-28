// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <seealso cref="MultiToValueTuple"/>
public sealed class MultiToTuple : MultiValueConverterBase<MultiToTuple>
{
    public override object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.Count switch
        {
            1 => Tuple.Create(values[0]),
            2 => Tuple.Create(values[0], values[1]),
            3 => Tuple.Create(values[0], values[1], values[2]),
            4 => Tuple.Create(values[0], values[1], values[2], values[3]),
            5 => Tuple.Create(values[0], values[1], values[2], values[3], values[4]),
            6 => Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5]),
            7 => Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6]),
            8 => Tuple.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]),
            _ => throw new ArgumentException("This converter supports only between one and eight elements")
        };
    }
}
