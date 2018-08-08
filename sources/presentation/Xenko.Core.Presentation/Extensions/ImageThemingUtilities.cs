// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xenko.Core.Presentation.Drawing;

namespace Xenko.Core.Presentation.Extensions
{
    using Media = System.Windows.Media;

    public class ThemeController : DependencyObject
    {
        public static bool GetIsDark(DependencyObject obj)
         => (bool)obj.GetValue(IsDarkProperty);

        public static void SetIsDark(DependencyObject obj, bool value)
         => obj.SetValue(IsDarkProperty, value);

        public static readonly DependencyProperty IsDarkProperty =
            DependencyProperty.RegisterAttached("IsDark", typeof(bool), typeof(ThemeController), new PropertyMetadata(false));

    }

    public static class IconThemeSelector
    {
        public enum KnownThemes
        {
            Light,
            Dark
        }

        public static IconTheme GetIconTheme(this KnownThemes theme)
        {
            switch (theme)
            {
                case KnownThemes.Dark: return new IconTheme("Dark", Color.FromRgb(16, 16, 17));
                case KnownThemes.Light: return new IconTheme("Light", Color.FromRgb(245, 245, 245));
                default:return default(IconTheme);
            }
        }

    }

    public struct IconTheme
    {
        public IconTheme(string name, Color backgroundColor)
        {
            this.Name = name;
            this.BackgroundColor = backgroundColor;
        }
        public string Name { get; }
        public Color BackgroundColor { get; }
        public double BackgroundLuminosity => BackgroundColor.ToHslColor().Luminosity;

    }

    public static class ImageThemingUtilities
    {

        public static Media.Drawing TransformDrawing(Media.Drawing drawing, IconTheme theme, bool checkLuminosity = true)
        {
            var isDark = ThemeController.GetIsDark(drawing);
            if (checkLuminosity)
            {
                if (isDark == IsDark(theme.BackgroundLuminosity)) return drawing;
            }
            var newDrawing = drawing.CloneCurrentValue();
            newDrawing.TransformParts(theme);
            ThemeController.SetIsDark(newDrawing, !isDark);
            return newDrawing;
        }

        private static void TransformParts(this Media.Drawing drawing, IconTheme theme)
        {
            if (drawing is GeometryDrawing gd && gd.Brush is SolidColorBrush s)
            {
                var hsl = s.Color.ToHslColor();
                var newL = TransformLuminosity(hsl, theme.BackgroundLuminosity);
                var newColor = new HslColor(hsl.Hue, hsl.Saturation, newL, hsl.Alpha).ToColor();
                s.Color = newColor;
            }
            else if (drawing is DrawingGroup dg)
            {
                foreach (Media.Drawing dr in dg.Children)
                {
                    if (dr is DrawingGroup || dr is GeometryDrawing)
                    {
                        TransformParts(dr, theme);
                    }
                }
            }
        }

        private static bool IsDark(double luminosity) => luminosity < 0.5;

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

    }
}
