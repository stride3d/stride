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

    using Media = System.Windows.Media;

    public enum KnowThemes
    {
        Light, Dark
    }

    [MarkupExtensionReturnType(typeof(ImageSource))]
    public class ThemedSourceExtension : MarkupExtension
    {
        private static readonly double DarkLuminosity = Color.FromRgb(16, 16, 17).ToHslColor().Luminosity;
        private static readonly double LightLuminosity = Color.FromRgb(245, 245, 245).ToHslColor().Luminosity;

        private readonly ImageSource source;
        private readonly KnowThemes theme;

        public ThemedSourceExtension(ImageSource source, KnowThemes theme)
        {
            this.source = source;
            this.theme = theme;
        }

        private void TransformGeometry(Media.Drawing d)
        {
            if (d is GeometryDrawing gd && gd.Brush is SolidColorBrush s)
            {
                var hsl = s.Color.ToHslColor();
                var newL = TransformLuminosity(hsl, (theme == KnowThemes.Light) ? LightLuminosity : DarkLuminosity);
                var newColor = new HslColor(hsl.Hue, hsl.Saturation, newL, hsl.Alpha).ToColor();
                s.Color = newColor;
            }
            else if (d is DrawingGroup dg)
            {
                foreach (Media.Drawing dr in dg.Children)
                {
                    if (dr is DrawingGroup || dr is GeometryDrawing)
                    {
                        TransformGeometry(dr);

                    }
                }
            }
        }

        private double TransformLuminosity(HslColor hsl, double backgroundLuminosity)
        {
            var hue = hsl.Hue;
            var saturation = hsl.Saturation;
            var luminosity = hsl.Luminosity;
            if (backgroundLuminosity < 0.5)
            {
                if (luminosity >= 82.0 / 85.0)
                    return backgroundLuminosity * (luminosity - 1.0) / (-3.0 / 85.0);
                double val2 = saturation >= 0.2 ? (saturation <= 0.3 ? 1.0 - (saturation - 0.2) / (1.0 / 10.0) : 0.0) : 1.0;
                double num1 = Math.Max(Math.Min(1.0, Math.Abs(hue - 37.0) / 20.0), val2);
                double num2 = ((backgroundLuminosity - 1.0) * 0.66 / (82.0 / 85.0) + 1.0) * num1 + 0.66 * (1.0 - num1);
                if (luminosity < 0.66)
                    return (num2 - 1.0) / 0.66 * luminosity + 1.0;
                return (num2 - backgroundLuminosity) / (-259.0 / 850.0) * (luminosity - 82.0 / 85.0) + backgroundLuminosity;
            }
            if (luminosity < 82.0 / 85.0)
                return luminosity * backgroundLuminosity / (82.0 / 85.0);
            return (1.0 - backgroundLuminosity) * (luminosity - 1.0) / (3.0 / 85.0) + 1.0;
        }

        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (source == null) return source;
            var newSource = source.CloneCurrentValue();
            TransformGeometry((newSource as DrawingImage)?.Drawing);
            return newSource;
        }
    }
}
