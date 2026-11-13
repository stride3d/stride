// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Media;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts path data (the path markup of WPF and Avalonia) to a geometry, for the icons of the tools a plugin declares.
/// </summary>
/// <remarks>
/// Path data that can't be parsed gives no geometry, and is logged once.
/// </remarks>
public sealed class PathDataToGeometry : OneWayValueConverter<PathDataToGeometry>
{
    private static readonly Logger Log = GlobalLogger.GetLogger("EditorView");
    private static readonly HashSet<string> InvalidPathData = [];

    /// <inheritdoc />
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string { Length: > 0 } pathData)
            return null;

        try
        {
            return StreamGeometry.Parse(pathData);
        }
        catch (Exception e) when (e is FormatException or InvalidDataException or ArgumentException or IndexOutOfRangeException)
        {
            lock (InvalidPathData)
            {
                if (InvalidPathData.Add(pathData))
                    Log.Warning($"The icon '{pathData}' is not valid path data.", e);
            }
            return null;
        }
    }
}
