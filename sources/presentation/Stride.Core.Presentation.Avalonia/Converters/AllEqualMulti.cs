// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class AllEqualMulti : MultiValueConverterBase<AllEqualMulti>
{
    public override object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return AvaloniaProperty.UnsetValue;

        var fallbackValue = parameter is true;
        var first = values[0];
        var result = values.All(x => x == AvaloniaProperty.UnsetValue ? fallbackValue : Equals(x, first));
        return result.Box();
    }
}
