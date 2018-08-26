// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.TextureConverter;
using Xenko.Graphics;

namespace Xenko.Assets.Presentation.AssetEditors.SpriteEditor.Services
{
    public class SpriteEditorImageCache : IDisposable
    {
        private const int CacheSize = 30;

        private readonly List<Tuple<UFile, DateTime, TexImage, BitmapImage>> cache = new List<Tuple<UFile, DateTime, TexImage, BitmapImage>>(CacheSize);

        public void Dispose()
        {
            foreach (var image in cache)
            {
                image.Item3.Dispose();
                image.Item4.StreamSource.Dispose();
            }
        }

        public Rectangle? FindSpriteRegion(UFile source, Vector2 initialPoint, bool useTransparency, Color maskColor)
        {
            var image = cache.FirstOrDefault(x => x.Item1 == source)?.Item3;
            if (image == null)
                return null;

            var pixelPoint = new Int2((int)initialPoint.X, (int)initialPoint.Y);

            using (var texTool = new TextureTool())
            {
                return useTransparency ? texTool.FindSpriteRegion(image, pixelPoint) : texTool.FindSpriteRegion(image, pixelPoint, maskColor, 0x00ffffff);
            }
        }

        public Size2? GetPixelSize(UFile source)
        {
            var bitmap = RetrieveImage(source);
            if (bitmap == null)
                return null;

            return new Size2(bitmap.PixelWidth, bitmap.PixelHeight);
        }

        public Color? PickPixelColor(UFile source, Vector2 initialPoint)
        {
            var image = cache.FirstOrDefault(x => x.Item1 == source)?.Item3;
            if (image == null)
                return null;

            var pixelPoint = new Int2((int)initialPoint.X, (int)initialPoint.Y);

            using (var texTool = new TextureTool())
            {
                return texTool.PickColor(image, pixelPoint);
            }
        }

        public BitmapImage RetrieveImage(UFile filePath)
        {
            if (filePath == null)
                return null;

            if (!File.Exists(filePath))
                return null;

            var lastWrite = File.GetLastWriteTime(filePath);
            var entry = cache.FirstOrDefault(x => x.Item1 == filePath);
            if (entry != null)
            {
                if (lastWrite == entry.Item2)
                {
                    return entry.Item4;
                }
                // Clear from cache
                cache.Remove(entry);
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    TexImage texImage;
                    using (var texTool = new TextureTool())
                    {
                        texImage = texTool.Load(filePath, false);
                        texTool.Decompress(texImage, texImage.Format.IsSRgb());
                        if (texImage.Format == PixelFormat.R16G16B16A16_UNorm)
                            texTool.Convert(texImage, PixelFormat.R8G8B8A8_UNorm);
                        var image = texTool.ConvertToXenkoImage(texImage);
                        image.Save(stream, ImageFileType.Png);
                    }

                    stream.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    //bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    // This flag is only used when loaded via UriSource, and make it crash when loaded via StreamSource
                    //bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    // Update the cache
                    cache.Add(Tuple.Create(filePath, lastWrite, texImage, bitmap));
                    if (cache.Count == CacheSize)
                        cache.RemoveAt(0);

                    return bitmap;
                }
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (TextureToolsException)
            {
                return null;
            }
        }
    }
}
