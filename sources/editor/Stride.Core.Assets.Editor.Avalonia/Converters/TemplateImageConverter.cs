// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts the path of a template image (<see cref="UFile"/> or string) to a bitmap; a null path, or an image that cannot be
/// loaded, gives the default template icon. The bitmaps are cached: the lists of templates rebuild their items on each selection.
/// </summary>
public sealed class TemplateImageConverter : IValueConverter
{
    private static readonly Uri DefaultIconUri = new("avares://Stride.Core.Assets.Editor.Avalonia/Assets/default-template-icon.png");
    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.Ordinal);
    private static readonly object CacheLock = new();
    private static Bitmap? defaultIcon;

    public static TemplateImageConverter Instance { get; } = new();

    /// <summary>
    /// The icon shown for a template without one.
    /// </summary>
    public static Bitmap? DefaultIcon
    {
        get
        {
            lock (CacheLock)
            {
                if (defaultIcon is null)
                {
                    try
                    {
                        using var stream = AssetLoader.Open(DefaultIconUri);
                        defaultIcon = new Bitmap(stream);
                    }
                    catch (Exception)
                    {
                        // No default icon (a headless host without a bitmap implementation)
                    }
                }
                return defaultIcon;
            }
        }
    }

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value switch
        {
            UFile file => file.ToOSPath(),
            string s => s,
            _ => null,
        };
        if (string.IsNullOrEmpty(path))
            return DefaultIcon;

        lock (CacheLock)
        {
            if (!Cache.TryGetValue(path, out var bitmap))
            {
                try
                {
                    bitmap = File.Exists(path) ? new Bitmap(path) : null;
                }
                catch (Exception)
                {
                    bitmap = null;
                }
                Cache[path] = bitmap;
            }
            return bitmap ?? DefaultIcon;
        }
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
