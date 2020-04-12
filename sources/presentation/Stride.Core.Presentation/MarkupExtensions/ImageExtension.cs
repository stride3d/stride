// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Themes;

namespace Stride.Core.Presentation.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(Image))]
    public class ImageExtension : MarkupExtension
    {
        private readonly ImageSource source;
        private readonly int width;
        private readonly int height;
        private readonly BitmapScalingMode scalingMode;

        public ImageExtension(ImageSource source)
        {
            this.source = source;
            width = -1;
            height = -1;
        }

        public ImageExtension(ImageSource source, int width, int height)
            : this(source, width, height, BitmapScalingMode.Unspecified)
        {
        }

        public ImageExtension(ImageSource source, int width, int height, BitmapScalingMode scalingMode)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            this.source = source;
            this.width = width;
            this.height = height;
            this.scalingMode = scalingMode;
        }

        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var image = new Image { Source = source };
            if (source is DrawingImage drawingImage)
            {
                image.Source = new DrawingImage()
                {
                    Drawing = ImageThemingUtilities.TransformDrawing((source as DrawingImage)?.Drawing, ThemeController.CurrentTheme.GetThemeBase().GetIconTheme())
                };
            }

            RenderOptions.SetBitmapScalingMode(image, scalingMode);
            if (width >= 0 && height >= 0)
            {
                image.Width = width;
                image.Height = height;
            }
            return image;
        }
    }
}
