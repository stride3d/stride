using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Drawing;

namespace Xenko.Core.Presentation.MarkupExtensions
{

    [MarkupExtensionReturnType(typeof(ImageSource))]
    public class ThemedSourceExtension : MarkupExtension
    {

        private readonly ImageSource source;
        private readonly KnownThemes theme;

        public ThemedSourceExtension(ImageSource source, KnownThemes theme)
        {
            this.source = source;
            this.theme = theme;
        }

        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (source is DrawingImage) return new DrawingImage
            {
                Drawing = ImageThemingUtilities.TransformDrawing((source as DrawingImage)?.Drawing, theme)
            };
            else return source;
        }
    }
}
