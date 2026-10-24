// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Storage;

namespace Stride.Editor.Thumbnails;

/// <summary>
/// Creates the <see cref="ThumbnailData"/> whose presenter is a <see cref="Stride.Graphics.Image"/>: the resource thumbnails come
/// from images embedded in <see cref="ResourceAssembly"/>. The application registers it as the <see cref="IThumbnailDataFactory"/>.
/// </summary>
public sealed class ThumbnailDataFactory : IThumbnailDataFactory
{
    /// <param name="resourceAssembly">The assembly that embeds the resource images (the editor's shell); <c>null</c> uses <see cref="Resources.EmbeddedResourceReader.DefaultSource"/>.</param>
    public ThumbnailDataFactory(Assembly? resourceAssembly = null)
    {
        ResourceAssembly = resourceAssembly;
    }

    /// <summary>
    /// Gets the assembly whose embedded images the resource thumbnails show.
    /// </summary>
    public Assembly? ResourceAssembly { get; }

    /// <inheritdoc/>
    public ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey) => new ResourceThumbnailData(thumbnailId, resourceKey, ResourceAssembly);
}
