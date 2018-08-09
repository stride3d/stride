// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Markup;
using System.Windows.Media;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Themes;

namespace Xenko.Core.Presentation.MarkupExtensions
{
    using static Xenko.Core.Presentation.Themes.IconThemeSelector;

    [MarkupExtensionReturnType(typeof(ImageSource))]
    public class ThemedSourceExtension : MarkupExtension
    {
        public ThemedSourceExtension() { }

        public ThemedSourceExtension(ImageSource source, KnownThemes theme)
        {
            Source = source;
            Theme = theme.GetIconTheme();
        }

        [ConstructorArgument("source")]
        private ImageSource Source { get; }

        [ConstructorArgument("theme")]
        private IconTheme Theme { get; }

        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Source is DrawingImage drawingImage)
            {
                return new DrawingImage
                {
                    Drawing = ImageThemingUtilities.TransformDrawing(drawingImage.Drawing, Theme)
                };
            }
            else
            {
                return Source;
            }
        }
    }
}
