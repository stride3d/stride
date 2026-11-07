// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Core.IO;
using Stride.Core.Presentation.ValueConverters;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ValueConverters
{
    /// <summary>
    /// Gives the bitmap of an image of the <see cref="SpriteEditorImageCache"/> of the sprite editor: the first value is the
    /// image file, the second one the cache.
    /// </summary>
    public sealed class UFileToBitmapImage : OneWayMultiValueConverter<UFileToBitmapImage>
    {
        // A bitmap per image of the cache: the cache keeps the image until its file changes
        private static readonly ConditionalWeakTable<Image, BitmapImage> Bitmaps = new();

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null;

            if (values[0] is not UFile filePath || values[1] is not SpriteEditorImageCache spriteCache)
                return null;

            var image = spriteCache.RetrieveImage(filePath);
            if (image == null)
                return null;

            return Bitmaps.GetValue(image, ToBitmap);
        }

        private static BitmapImage ToBitmap(Image image)
        {
            // As the WPF cache of the sprite editor did, through a PNG
            using var stream = new MemoryStream();
            image.Save(stream, ImageFileType.Png);
            stream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
