// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stride.Core.IO;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    /// <summary>
    /// Converts the path of a template image (the icon or a screenshot of a template) to an image. A template without an icon, or
    /// whose icon cannot be loaded, shows the default template icon; pass <c>false</c> as parameter to show nothing instead.
    /// </summary>
    [ValueConversion(typeof(UFile), typeof(ImageSource))]
    public sealed class TemplateImageToImageSource : OneWayValueConverter<TemplateImageToImageSource>
    {
        private static readonly Uri DefaultIconUri = new($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Images/default-template-icon.png");
        // The lists of templates create their items again on each selection
        private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.Ordinal);
        private static ImageSource defaultIcon;

        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var image = value is UFile path ? LoadImage(path.ToOSPath()) : null;
            if (image is null && parameter as bool? != false)
            {
                defaultIcon ??= LoadImage(DefaultIconUri);
                return defaultIcon;
            }
            return image;
        }

        private static ImageSource LoadImage(string path)
        {
            if (!Cache.TryGetValue(path, out var image))
            {
                image = File.Exists(path) ? LoadImage(new Uri(path)) : null;
                Cache[path] = image;
            }
            return image;
        }

        private static ImageSource LoadImage(Uri uri)
        {
            try
            {
                return new BitmapImage(uri);
            }
            catch (Exception)
            {
                // A broken image shows as no image
                return null;
            }
        }
    }
}
