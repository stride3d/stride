// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.Styling;
using Stride.Core;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// Converts a <see cref="PlatformType"/> to its icon (the <c>ImagePlatform*</c> resources of the core editor views).
/// </summary>
public sealed class PlatformIconConverter : IValueConverter
{
    private static readonly Lazy<IResourceProvider> Images = new(() => new ResourceInclude(new Uri("avares://Stride.Assets.Editor.Avalonia/"))
    {
        Source = new Uri("avares://Stride.Core.Assets.Editor.Avalonia/Views/ImageResources.axaml"),
    });

    public static PlatformIconConverter Instance { get; } = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PlatformType platform)
            return null;
        try
        {
            return Images.Value.TryGetResource($"ImagePlatform{platform}", null, out var image) ? image : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
