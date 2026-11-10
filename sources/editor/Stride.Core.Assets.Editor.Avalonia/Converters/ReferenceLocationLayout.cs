// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Whether the dimmed location (and its slash) of a reference field is shown: only when the location isn't empty and
/// enough width remains after the asset name, so that the name keeps priority and the location goes whole rather than
/// leaving a lone slash.
/// </summary>
/// <remarks>Values: the width of the display, the width of the name, the location. Parameter: the minimum width left.</remarks>
public sealed class ReferenceLocationVisibility : MultiValueConverterBase<ReferenceLocationVisibility>
{
    public override object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3 || values[2] is not string location || string.IsNullOrEmpty(location))
            return false;

        var threshold = parameter is not null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 0.0;
        return ReferenceLocationMaxWidth.Remaining(values) >= threshold;
    }
}

/// <summary>
/// The maximum width of the dimmed location of a reference field: the width of the display less the width of the name.
/// </summary>
/// <remarks>Values: the width of the display, the width of the name. Parameter: the width kept for the slash.</remarks>
public sealed class ReferenceLocationMaxWidth : MultiValueConverterBase<ReferenceLocationMaxWidth>
{
    public override object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var slash = parameter is not null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 0.0;
        return Math.Max(0.0, Remaining(values) - slash);
    }

    internal static double Remaining(IList<object?> values)
    {
        if (values.Count < 2 || values[0] is not double width || values[1] is not double nameWidth)
            return 0.0;

        return width - nameWidth;
    }
}
