// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// Converts an internal editor executable name (e.g. <c>Stride.GameStudio.Avalonia.Desktop</c>)
/// to a user-friendly display name.
/// </summary>
public sealed class EditorNameConverter : OneWayValueConverter<EditorNameConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (string?)value switch
        {
            GameStudioNames.StrideAvalonia => "Game Studio (Avalonia)",
            GameStudioNames.Stride => "Game Studio (WPF)",
            GameStudioNames.Xenko => "Xenko Game Studio",
            var name => name ?? string.Empty,
        };
    }
}
