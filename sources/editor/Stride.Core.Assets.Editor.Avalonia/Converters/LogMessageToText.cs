// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts a log message to the text the Message column of the asset errors shows: its module, its type and its text, as in WPF.
/// </summary>
public sealed class LogMessageToText : OneWayValueConverter<LogMessageToText>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as ILogMessage)?.ToString() ?? string.Empty;
    }
}
