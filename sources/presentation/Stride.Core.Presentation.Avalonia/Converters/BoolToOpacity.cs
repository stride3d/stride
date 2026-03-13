// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// Returns full opacity (1.0) when the value is <c>true</c>, otherwise half opacity (0.5).
/// </summary>
public sealed class BoolToOpacity : OneWayValueConverter<BoolToOpacity>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.5;
    }
}