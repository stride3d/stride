// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts a log message to the URL of the asset it is about (Asset column of the asset errors), or an empty string.
/// </summary>
public sealed class LogMessageToAssetUrl : OneWayValueConverter<LogMessageToAssetUrl>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AssetLogViewModel.GetAssetUrl(value as ILogMessage)?.ToString() ?? string.Empty;
    }
}
