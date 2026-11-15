// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Media.Imaging;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Storage;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Services;

/// <summary>
/// A thumbnail that shows an image embedded in an assembly (the folder icon...), decoded into an Avalonia <see cref="Bitmap"/>.
/// </summary>
public sealed class ResourceThumbnailData : ThumbnailData
{
    private readonly Assembly source;
    private string? resourceName;

    /// <param name="thumbnailId">The identifier of the thumbnail.</param>
    /// <param name="resourceKey">The name of the embedded image: its manifest resource name, or the end of it, with or without
    /// the <c>.png</c> extension (its <see cref="string"/> representation is used).</param>
    /// <param name="source">The assembly that embeds the image.</param>
    public ResourceThumbnailData(ObjectId thumbnailId, object resourceKey, Assembly source)
        : base(thumbnailId)
    {
        resourceName = resourceKey?.ToString();
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <inheritdoc />
    protected override object? BuildImageSource()
    {
        if (resourceName == null)
            return null;

        var bytes = Annotations.DeclaredResources.Read(source, resourceName) ?? Annotations.DeclaredResources.Read(source, resourceName + ".png");
        if (bytes == null)
            return null;

        using var stream = new MemoryStream(bytes);
        return ThumbnailBitmaps.TryDecode(stream);
    }

    /// <inheritdoc />
    protected override void FreeBuildingResources()
    {
        resourceName = null;
    }
}
