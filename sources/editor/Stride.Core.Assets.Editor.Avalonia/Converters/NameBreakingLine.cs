// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Text.RegularExpressions;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Inserts zero-width spaces where a name is better wrapped: after a non-alphanumeric character and between the words of a
/// camel-cased name (aA, a0, A0), as the asset tiles of WPF wrap the names.
/// </summary>
public sealed partial class NameBreakingLine : OneWayValueConverter<NameBreakingLine>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string name ? BreakPoints().Replace(name, Evaluate) : value;
    }

    private static string Evaluate(Match match)
    {
        return match.Groups["BEFORE"].Value + "\u200B" + match.Groups["AFTER"].Value;
    }

    [GeneratedRegex("(?<BEFORE>[^a-zA-Z0-9])|(?<BEFORE>[a-z])(?<AFTER>[A-Z0-9])|(?<BEFORE>[A-Z])(?<AFTER>[0-9])")]
    private static partial Regex BreakPoints();
}
