// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Markup;
using System.Windows.Media;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.MarkupExtensions
{

    using Xenko.Core.Presentation.Extensions;
    using static Xenko.Core.Presentation.Extensions.IconThemeSelector;

    [MarkupExtensionReturnType(typeof(ImageSource))]
    public class ThemedSourceExtension : MarkupExtension
    {
        public ThemedSourceExtension() { }

        public ThemedSourceExtension(ImageSource source, KnownThemes theme)
        {
            this.Source = source;
            this.Theme = theme.GetIconTheme();
        }

        [ConstructorArgument("source")]
        private ImageSource Source { get; }

        [ConstructorArgument("theme")]
        private IconTheme Theme { get; }

        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Source is DrawingImage) return new DrawingImage
            {
                Drawing = ImageThemingUtilities.TransformDrawing((Source as DrawingImage)?.Drawing, Theme)
            };
            else return Source;
        }
    }
}
