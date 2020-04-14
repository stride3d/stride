// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.Themes
{
    using Media = System.Windows.Media;

    public static class ImageThemingUtilities
    {
        /// <summary>
        /// This method transforms colors of a geometry-based drawing to a desired theme.
        /// </summary>
        /// <param name="drawing">The input drawing</param>
        /// <param name="theme">The desired theme</param>
        /// <param name="checkLuminosity">If check uminosity is on, a dark drawing only can be converted to a light one, this is specially used
        /// when you don't want your dark icon reveted to light when called twice</param>
        /// <returns>A new drawing with converted colors</returns>
        public static Media.Drawing TransformDrawing(Media.Drawing drawing, IconTheme theme, bool checkLuminosity = true)
        {
            var isDark = ThemeController.GetIsDark(drawing);
            if (checkLuminosity)
            {
                if (isDark == IsDark(theme.BackgroundLuminosity))
                    return drawing;
            }
            var newDrawing = drawing.CloneCurrentValue();
            TransformParts(newDrawing, theme);
            ThemeController.SetIsDark(newDrawing, !isDark);
            return newDrawing;
        }

        /// <summary>
        /// Transforms every single part of a geomtry-based drawing.
        /// </summary>
        /// <param name="drawing"></param>
        /// <param name="theme"></param>
        private static void TransformParts(Media.Drawing drawing, IconTheme theme)
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

        /// <summary>
        /// Transforms luminosity of an HSL color based on background
        /// </summary>
        /// <param name="hsl"></param>
        /// <param name="backgroundLuminosity"></param>
        /// <returns></returns>
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
