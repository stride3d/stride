// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.TextureConverter;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// The source images of the sprites of a sprite sheet editor, loaded with the <see cref="TextureTool"/>: their size in pixels,
/// the region of the sprite under a pixel (magic wand) and the color of a pixel (color key picker). The images are given to the
/// view as Stride <see cref="Image"/>s in an 8-bit RGBA format, which each UI toolkit turns into its own bitmap.
/// </summary>
/// <remarks>
/// As the cache of the WPF sprite editor, it keeps the last 30 images and reloads an image whose file changed.
/// </remarks>
public sealed class SpriteEditorImageCache : IDisposable
{
    private const int CacheSize = 30;

    private readonly List<Entry> cache = new(CacheSize);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var entry in cache)
        {
            entry.Dispose();
        }
        cache.Clear();
    }

    /// <summary>
    /// Finds the region of the sprite that contains the given pixel of the image, delimited by transparent pixels or by the
    /// given mask color.
    /// </summary>
    /// <param name="source">The image file.</param>
    /// <param name="initialPoint">The pixel, in pixels of the image.</param>
    /// <param name="useTransparency"><c>true</c> to delimit the sprite with the transparent pixels; <c>false</c> to delimit it with <paramref name="maskColor"/>.</param>
    /// <param name="maskColor">The color that delimits the sprites when <paramref name="useTransparency"/> is <c>false</c>.</param>
    /// <returns>The region, or <c>null</c> when the image cannot be loaded.</returns>
    public Rectangle? FindSpriteRegion(UFile? source, Vector2 initialPoint, bool useTransparency, Color maskColor)
    {
        var image = RetrieveEntry(source)?.TexImage;
        if (image is null)
            return null;

        var pixelPoint = new Int2((int)initialPoint.X, (int)initialPoint.Y);
        using var texTool = new TextureTool();
        return useTransparency ? texTool.FindSpriteRegion(image, pixelPoint) : texTool.FindSpriteRegion(image, pixelPoint, maskColor, 0x00ffffff);
    }

    /// <summary>
    /// Gets the size of the given image, in pixels.
    /// </summary>
    /// <returns>The size, or <c>null</c> when the image cannot be loaded.</returns>
    public Size2? GetPixelSize(UFile? source)
    {
        var image = RetrieveEntry(source)?.Image;
        return image is not null ? new Size2(image.Description.Width, image.Description.Height) : null;
    }

    /// <summary>
    /// Gets the color of the given pixel of the image.
    /// </summary>
    /// <param name="source">The image file.</param>
    /// <param name="initialPoint">The pixel, in pixels of the image.</param>
    /// <returns>The color, or <c>null</c> when the image cannot be loaded or the pixel is outside of it.</returns>
    public Color? PickPixelColor(UFile? source, Vector2 initialPoint)
    {
        var image = RetrieveEntry(source)?.TexImage;
        if (image is null)
            return null;

        var pixelPoint = new Int2((int)initialPoint.X, (int)initialPoint.Y);
        if (pixelPoint.X < 0 || pixelPoint.Y < 0 || pixelPoint.X >= image.Width || pixelPoint.Y >= image.Height)
            return null;

        using var texTool = new TextureTool();
        return texTool.PickColor(image, pixelPoint);
    }

    /// <summary>
    /// Gets the given image, loaded again when its file changed since the last call.
    /// </summary>
    /// <param name="filePath">The image file.</param>
    /// <returns>The image in an 8-bit RGBA or BGRA format, or <c>null</c> when it cannot be loaded. The cache owns it.</returns>
    public Image? RetrieveImage(UFile? filePath)
    {
        return RetrieveEntry(filePath)?.Image;
    }

    private Entry? RetrieveEntry(UFile? filePath)
    {
        if (filePath is null || !File.Exists(filePath))
            return null;

        var lastWrite = File.GetLastWriteTime(filePath);
        var entry = cache.FirstOrDefault(x => x.Path == filePath);
        if (entry is not null)
        {
            if (lastWrite == entry.LastWrite)
                return entry;

            // Clear from cache
            cache.Remove(entry);
            entry.Dispose();
        }

        TexImage? texImage = null;
        try
        {
            using var texTool = new TextureTool();
            texImage = texTool.Load(filePath, false);
            texTool.Decompress(texImage, texImage.Format.IsSRgb);
            // The region finding, the color picking and the bitmaps of the views need 8-bit RGBA or BGRA pixels
            if (texImage.Format is not (PixelFormat.R8G8B8A8_UNorm or PixelFormat.R8G8B8A8_UNorm_SRgb or PixelFormat.B8G8R8A8_UNorm or PixelFormat.B8G8R8A8_UNorm_SRgb))
                texTool.Convert(texImage, PixelFormat.R8G8B8A8_UNorm);
            var image = texTool.ConvertToStrideImage(texImage);

            // Update the cache
            entry = new Entry(filePath, lastWrite, texImage, image);
            cache.Add(entry);
            if (cache.Count > CacheSize)
            {
                cache[0].Dispose();
                cache.RemoveAt(0);
            }
            return entry;
        }
        catch (Exception e) when (e is InvalidOperationException or TextureToolsException or NotImplementedException)
        {
            texImage?.Dispose();
            return null;
        }
    }

    private sealed record Entry(UFile Path, DateTime LastWrite, TexImage TexImage, Image Image) : IDisposable
    {
        public void Dispose()
        {
            TexImage.Dispose();
            Image.Dispose();
        }
    }
}
