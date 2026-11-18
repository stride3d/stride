// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Media;
using Avalonia.Media.Imaging;
using MarkView.Avalonia.Extensions;
using MarkView.Avalonia.Rendering;

namespace Stride.Launcher.Views;

/// <summary>
/// Shows the images of a Markdown text that are local files (<c>file:</c> URLs), which MarkView's own loader doesn't read.
/// </summary>
internal sealed class LocalImages : IMarkViewExtension, IImageLoader
{
    public void Register(AvaloniaRenderer renderer) => renderer.ImageLoaders.Add(this);

    public bool CanLoad(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile;

    public async Task<IImage?> LoadAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = new MemoryStream(await File.ReadAllBytesAsync(new Uri(url).LocalPath, cancellationToken));
            return new Bitmap(stream);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
