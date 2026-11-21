// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Gives the rest of the Guid template ("00000000-0000-0000-0000-000000000000") after the text typed so far, so that the Guid
/// editor can show which characters and dashes are still missing. Empty once the text has the length of a Guid.
/// </summary>
public sealed class GuidTemplateRemainder : OneWayValueConverter<GuidTemplateRemainder>
{
    private const string Template = "00000000-0000-0000-0000-000000000000";

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        return text.Length < Template.Length ? Template[text.Length..] : string.Empty;
    }
}
