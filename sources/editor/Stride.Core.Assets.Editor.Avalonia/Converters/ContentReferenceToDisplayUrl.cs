// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// The asset name part of a reference URL, for the unfocused display of a reference field (never truncated).
/// </summary>
public sealed class ContentReferenceToDisplayName : OneWayValueConverter<ContentReferenceToDisplayName>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ContentReferenceDisplayUrl.Split(value, SessionViewModel.Instance, Tr._("(Different values)")).Name;
    }
}

/// <summary>
/// The location part of a reference URL without the trailing slash, for the unfocused display of a reference field.
/// The separating slash is shown apart so that it stays when the location is trimmed.
/// </summary>
public sealed class ContentReferenceToDisplayLocation : OneWayValueConverter<ContentReferenceToDisplayLocation>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ContentReferenceDisplayUrl.Split(value, SessionViewModel.Instance, Tr._("(Different values)")).Location;
    }
}
