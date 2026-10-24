// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Storage;
using Stride.Editor.Resources;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails;

/// <summary>
/// A thumbnail that shows an image embedded in an assembly (a folder icon...), decoded into an <see cref="Image"/>.
/// </summary>
public sealed class ResourceThumbnailData : ThumbnailData
{
    private readonly Assembly? source;
    private string? resourceName;

    /// <param name="thumbnailId">The identifier of the thumbnail.</param>
    /// <param name="resourceKey">The name of the embedded resource (its <see cref="string"/> representation is used).</param>
    /// <param name="source">The assembly that embeds the resource; <c>null</c> looks in <see cref="EmbeddedResourceReader.DefaultSource"/>.</param>
    public ResourceThumbnailData(ObjectId thumbnailId, object resourceKey, Assembly? source = null)
        : base(thumbnailId)
    {
        resourceName = resourceKey?.ToString();
        this.source = source;
    }

    /// <inheritdoc />
    protected override object? BuildImageSource()
    {
        if (resourceName == null)
            return null;

        try
        {
            return EmbeddedResourceReader.TryGetImage(resourceName, source);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override void FreeBuildingResources()
    {
        resourceName = null;
    }
}
