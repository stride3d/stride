// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <seealso cref="AndMulti"/>
/// <seealso cref="OrMulti"/>
public sealed class XOrMulti : MultiValueConverterBase<XOrMulti>
{
    public override object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2) return AvaloniaProperty.UnsetValue;

        var seed = values[0] is bool b && b;
        return values.Skip(1).Aggregate(seed, (current, value) => current ^ (value is bool b && b)).Box();
    }
}
