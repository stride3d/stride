// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Storage;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Services;

/// <summary>
/// Creates the <see cref="ThumbnailData"/> of the Avalonia editor: their <see cref="ThumbnailData.Presenter"/> is an Avalonia
/// bitmap, which the views show without a converter. The application registers it as the <see cref="IThumbnailDataFactory"/>.
/// </summary>
public sealed class ThumbnailDataFactory : IThumbnailDataFactory
{
    /// <param name="resourceAssembly">The assembly that embeds the images of the resource thumbnails (the editor's shell).</param>
    public ThumbnailDataFactory(Assembly resourceAssembly)
    {
        ResourceAssembly = resourceAssembly ?? throw new ArgumentNullException(nameof(resourceAssembly));
    }

    /// <summary>
    /// Gets the assembly whose embedded images the resource thumbnails show.
    /// </summary>
    public Assembly ResourceAssembly { get; }

    /// <inheritdoc/>
    public ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey) => new ResourceThumbnailData(thumbnailId, resourceKey, ResourceAssembly);

    /// <inheritdoc/>
    public ThumbnailData CreateFromStream(ObjectId thumbnailId, Stream imageStream) => new BitmapThumbnailData(thumbnailId, imageStream);
}
