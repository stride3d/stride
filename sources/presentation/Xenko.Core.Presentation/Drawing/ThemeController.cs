using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Xenko.Core.Presentation.Drawing
{
    /// <summary>
    /// This class controls luminosity of a <see cref="DrawingImage"/>.
    /// </summary>
    /// <example>&gt;DrawingImage ThemeController.TransformLuminosity = "True" /&lt;</example>
    public sealed class ThemeController : DependencyObject
    {
        public static readonly DependencyProperty TransformLuminosityProperty =
            DependencyProperty.RegisterAttached("TransformLuminosity", typeof(bool), typeof(ThemeController),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None, LuminosityChanged));

        public static bool GetTransformLuminosity(DependencyObject obj)
            => (bool)obj.GetValue(TransformLuminosityProperty);

        public static void SetTransformLuminosity(DependencyObject obj, bool value)
        {
            obj.SetValue(TransformLuminosityProperty, value);
            ChangeTheme(obj, value);
        }

        private static readonly double DarkLuminosity = Color.FromRgb(16, 16, 17).ToHslColor().Luminosity;

        private static void LuminosityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
            => (sender as DrawingImage).Changed += (sender2, e) => ChangeTheme(sender, (bool)args.NewValue);

        private static void ChangeTheme(DependencyObject obj, bool transformLuminosity)
        {
            if (transformLuminosity && obj is DrawingImage d)
            {
                TransformGeometry(d.Drawing);
            }
        }

        private static double TransformLuminosity(HslColor hsl, double backgroundLuminosity)
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

        private static void TransformGeometry(System.Windows.Media.Drawing d)
        {
            if (d is GeometryDrawing gd)
            {

                if (gd.Brush is SolidColorBrush s)
                {

                    var oldbutgold = s.Color;
                    var hsl = s.Color.ToHslColor();

                    var newL = TransformLuminosity(hsl, DarkLuminosity);
                    var newColor = new HslColor(hsl.Hue, hsl.Saturation, newL, hsl.Alpha).ToColor();
                    s.Color = newColor;
                }
            }
            else if (d is DrawingGroup dg)
            {
                foreach (System.Windows.Media.Drawing dr in dg.Children)
                {
                    if (dr is DrawingGroup || dr is GeometryDrawing) TransformGeometry(dr);
                }
            }
        }
    }
}
