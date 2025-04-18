// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Extensions;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class Yield : OneWayValueConverter<Yield>
{
    /// <inheritdoc />
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == AvaloniaProperty.UnsetValue ? AvaloniaProperty.UnsetValue : value.Yield();
    }
}
