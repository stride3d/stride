// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Xenko.Core.IO;
using Xenko.Core.Presentation.ValueConverters;
using Xenko.Assets.Presentation.AssetEditors.SpriteEditor.Services;

namespace Xenko.Assets.Presentation.ValueConverters
{
    public class UFileToBitmapImage : OneWayMultiValueConverter<UFileToBitmapImage>
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return null;

            var filePath = values[0] as UFile;
            var spriteCache = values[1] as SpriteEditorImageCache;
            if (filePath == null || spriteCache == null)
                return null;

            return spriteCache.RetrieveImage(filePath);
        }
    }
}
