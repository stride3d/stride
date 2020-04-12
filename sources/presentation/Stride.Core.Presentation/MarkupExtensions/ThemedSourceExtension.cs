// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Markup;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Themes;

namespace Stride.Core.Presentation.MarkupExtensions
{
    using static Stride.Core.Presentation.Themes.IconThemeSelector;

    [MarkupExtensionReturnType(typeof(ImageSource))]
    public class ThemedSourceExtension : MarkupExtension
    {
        public ThemedSourceExtension() { }

        public ThemedSourceExtension(ImageSource source, ThemeBase theme)
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
