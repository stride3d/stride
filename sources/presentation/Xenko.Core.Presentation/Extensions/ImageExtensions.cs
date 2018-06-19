// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Extensions
{
    public static class ImageExtensions
    {
        public static void SetSource([NotNull] this Image image, [NotNull] Uri uri)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            image.Source = ImageSourceFromFile(uri);
        }

        public static void SetSource([NotNull] this Image image, [NotNull] string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Invalid 'uri' argument.");

            SetSource(image, new Uri(uri));
        }

        [NotNull]
        public static ImageSource ImageSourceFromFile([NotNull] Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var source = new BitmapImage();
            source.BeginInit();
            source.UriSource = uri;
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();

            return source;
        }

        [NotNull]
        public static ImageSource ImageSourceFromFile([NotNull] string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Invalid 'uri' argument.");

            return ImageSourceFromFile(new Uri(uri));
        }
    }
}
