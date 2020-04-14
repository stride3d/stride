// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Media;

namespace Stride.Core.Presentation.Drawing
{
    public struct HslColor
    {
        private const double MinAlpha = 0.0;
        private const double MaxAlpha = 1.0;
        private const double MinHue = 0.0;
        private const double MaxHue = 360.0;
        private const double MinSaturation = 0.0;
        private const double MaxSaturation = 1.0;
        private const double MinLuminosity = 0.0;
        private const double MaxLuminosity = 1.0;
        private double _hue;
        private double _saturation;
        private double _luminosity;
        private double _alpha;

        /// <summary>Initializes a new instance of the HslColor class with the specified hue, saturation, and luminosity.</summary>
        /// <param name="hue">The hue.</param>
        /// <param name="saturation">Ths saturation.</param>
        /// <param name="luminosity">The luminosity.</param>
        public HslColor(double hue, double saturation, double luminosity)
        {
            this = new HslColor(hue, saturation, luminosity, 1.0);
        }

        /// <summary>Initializes a new instance of the HslColor class with the specified hue, saturation, luminosity, and alpha.</summary>
        /// <param name="hue">The hue.</param>
        /// <param name="saturation">The saturation.</param>
        /// <param name="luminosity">The luminosity.</param>
        /// <param name="alpha">The alpha.</param>
        public HslColor(double hue, double saturation, double luminosity, double alpha)
        {
            _hue = LimitRange(hue, 0.0, 360.0);
            _saturation = LimitRange(saturation, 0.0, 1.0);
            _luminosity = LimitRange(luminosity, 0.0, 1.0);
            _alpha = LimitRange(alpha, 0.0, 1.0);
        }

        /// <summary>Gets or sets the HslColor's Hue component</summary>
        /// <returns>The HslColor's Hue component.</returns>
        public double Hue
        {
            get => _hue;
            set => _hue = LimitRange(value, 0.0, 360.0);
        }

        /// <summary>Gets or sets the HslColor's Saturation component.</summary>
        /// <returns>The HslColor's Saturation component.</returns>
        public double Saturation
        {
            get => _saturation;
            set => _saturation = LimitRange(value, 0.0, 1.0);
        }

        /// <summary>Gets or sets the HslColor's Luminosity component</summary>
        /// <returns>The HslColor's Luminosity component.</returns>
        public double Luminosity
        {
            get => _luminosity;
            set => _luminosity = LimitRange(value, 0.0, 1.0);
        }

        /// <summary>Gets or sets the HslColor's Alpha component.</summary>
        /// <returns>The HslColor's Alpha component.</returns>
        public double Alpha
        {
            get => _alpha;
            set => _alpha = LimitRange(value, 0.0, 1.0);
        }

        /// <summary>Converts a Color value to an HslColor. The algorithm is based on pseudocode available on HSL and HSV.</summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The converted color.</returns>
        public static HslColor FromColor(Color color)
        {
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;
            byte num1 = Math.Max(r, Math.Max(g, b));
            byte num2 = Math.Min(r, Math.Min(g, b));
            double num3 = num1 - num2;
            double num4 = num1 / (double)byte.MaxValue;
            double num5 = num2 / (double)byte.MaxValue;
            double hue = num1 != num2 ? num1 != r ? num1 != g ? 60.0 * (r - g) / num3 + 240.0 : 60.0 * (b - r) / num3 + 120.0 : (int)(60.0 * (g - b) / num3 + 360.0) % 360 : 0.0;
            double alpha = color.A / (double)byte.MaxValue;
            double luminosity = 0.5 * (num4 + num5);
            double saturation = num1 != num2 ? (luminosity > 0.5 ? (num4 - num5) / (2.0 - 2.0 * luminosity) : (num4 - num5) / (2.0 * luminosity)) : 0.0;
            return new HslColor(hue, saturation, luminosity, alpha);
        }

        /// <summary>Converts an HslColor value to a Color. The algorithm is based on pseudocode available on HSL and HSV.</summary>
        /// <returns>The converted color.</returns>
        public Color ToColor()
        {
            double q = Luminosity < 0.5 ? Luminosity * (1.0 + Saturation) : Luminosity + Saturation - Luminosity * Saturation;
            double p = 2.0 * Luminosity - q;
            double num = Hue / 360.0;
            double tC1 = ModOne(num + 1.0 / 3.0);
            double tC2 = num;
            double tC3 = ModOne(num - 1.0 / 3.0);
            return Color.FromArgb((byte)(Alpha * byte.MaxValue), (byte)(ComputeRGBComponent(p, q, tC1) * byte.MaxValue), (byte)(ComputeRGBComponent(p, q, tC2) * byte.MaxValue), (byte)(ComputeRGBComponent(p, q, tC3) * byte.MaxValue));
        }

        private static double ModOne(double value)
        {
            if (value < 0.0)
                return value + 1.0;
            if (value > 1.0)
                return value - 1.0;
            return value;
        }

        private static double ComputeRGBComponent(double p, double q, double tC)
        {
            if (tC < 1.0 / 6.0)
                return p + (q - p) * 6.0 * tC;
            if (tC < 0.5)
                return q;
            if (tC < 2.0 / 3.0)
                return p + (q - p) * 6.0 * (2.0 / 3.0 - tC);
            return p;
        }

        private static double LimitRange(double value, double min, double max)
        {
            value = Math.Max(min, value);
            value = Math.Min(value, max);
            return value;
        }
    }

    public static class HslExtensions
    {
        public static HslColor ToHslColor(this Color color) => HslColor.FromColor(color);
    }
}
