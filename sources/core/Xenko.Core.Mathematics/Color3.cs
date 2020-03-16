// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// Represents a color in the form of rgb.
    /// </summary>
    [DataContract("Color3")]
    [DataStyle(DataStyle.Compact)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Color3 : IEquatable<Color3>, IFormattable
    {
        private const string ToStringFormat = "R:{0} G:{1} B:{2}";

        /// <summary>
        /// The red component of the color.
        /// </summary>
        [DataMember(0)]
        public float R;

        /// <summary>
        /// The green component of the color.
        /// </summary>
        [DataMember(1)]
        public float G;

        /// <summary>
        /// The blue component of the color.
        /// </summary>
        [DataMember(2)]
        public float B;

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Color3(float value)
        {
            R = value;
            G = value;
            B = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3"/> struct.
        /// </summary>
        /// <param name="red">The red component of the color.</param>
        /// <param name="green">The green component of the color.</param>
        /// <param name="blue">The blue component of the color.</param>
        public Color3(float red, float green, float blue)
        {
            R = red;
            G = green;
            B = blue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3"/> struct.
        /// </summary>
        /// <param name="value">The red, green, and blue components of the color.</param>
        public Color3(Vector3 value)
        {
            R = value.X;
            G = value.Y;
            B = value.Z;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3"/> struct.
        /// </summary>
        /// <param name="rgb">A packed integer containing all three color components.
        /// The alpha component is ignored.</param>
        public Color3(int rgb)
        {
            B = ((rgb >> 16) & 255) / 255.0f;
            G = ((rgb >> 8) & 255) / 255.0f;
            R = (rgb & 255) / 255.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3"/> struct.
        /// </summary>
        /// <param name="rgb">A packed unsigned integer containing all three color components.
        /// The alpha component is ignored.</param>
        public Color3(uint rgb)
        {
            B = ((rgb >> 16) & 255) / 255.0f;
            G = ((rgb >> 8) & 255) / 255.0f;
            R = (rgb & 255) / 255.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color3"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the red, green, and blue components of the color. This must be an array with three elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public Color3(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != 3)
                throw new ArgumentOutOfRangeException(nameof(values), "There must be three and only three input values for Color3.");

            R = values[0];
            G = values[1];
            B = values[2];
        }

        /// <summary>
        /// Gets or sets the component at the specified index.
        /// </summary>
        /// <value>The value of the red, green, or blue component, depending on the index.</value>
        /// <param name="index">The index of the component to access. Use 0 for the red component, 1 for the green component, and 2 for the blue component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 2].</exception>
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return R;
                    case 1: return G;
                    case 2: return B;
                }

                throw new ArgumentOutOfRangeException(nameof(index), "Indices for Color3 run from 0 to 2, inclusive.");
            }

            set
            {
                switch (index)
                {
                    case 0: R = value; break;
                    case 1: G = value; break;
                    case 2: B = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index), "Indices for Color3 run from 0 to 2, inclusive.");
                }
            }
        }

        /// <summary>
        /// Converts the color into a packed integer.
        /// </summary>
        /// <returns>A packed integer containing all three color components.
        /// The alpha channel is set to 255.</returns>
        public int ToRgb()
        {
            uint a = 255;
            uint r = (uint)(R * 255.0f);
            uint g = (uint)(G * 255.0f);
            uint b = (uint)(B * 255.0f);

            uint value = r;
            value += g << 8;
            value += b << 16;
            value += a << 24;

            return (int)value;
        }

        /// <summary>
        /// Raises the exponent for each components.
        /// </summary>
        /// <param name="exponent">The exponent.</param>
        public void Pow(float exponent)
        {
            R = (float)Math.Pow(R, exponent);
            G = (float)Math.Pow(G, exponent);
            B = (float)Math.Pow(B, exponent);
        }

        /// <summary>
        /// Converts the color into a three component vector.
        /// </summary>
        /// <returns>A three component vector containing the red, green, and blue components of the color.</returns>
        public Vector3 ToVector3()
        {
            return new Vector3(R, G, B);
        }

        /// <summary>
        /// Creates an array containing the elements of the color.
        /// </summary>
        /// <returns>A three-element array containing the components of the color.</returns>
        public float[] ToArray()
        {
            return new[] { R, G, B };
        }

        /// <summary>
        /// Adds two colors.
        /// </summary>
        /// <param name="left">The first color to add.</param>
        /// <param name="right">The second color to add.</param>
        /// <param name="result">When the method completes, completes the sum of the two colors.</param>
        public static void Add(ref Color3 left, ref Color3 right, out Color3 result)
        {
            result.R = left.R + right.R;
            result.G = left.G + right.G;
            result.B = left.B + right.B;
        }

        /// <summary>
        /// Adds two colors.
        /// </summary>
        /// <param name="left">The first color to add.</param>
        /// <param name="right">The second color to add.</param>
        /// <returns>The sum of the two colors.</returns>
        public static Color3 Add(Color3 left, Color3 right)
        {
            return new Color3(left.R + right.R, left.G + right.G, left.B + right.B);
        }

        /// <summary>
        /// Subtracts two colors.
        /// </summary>
        /// <param name="left">The first color to subtract.</param>
        /// <param name="right">The second color to subtract.</param>
        /// <param name="result">WHen the method completes, contains the difference of the two colors.</param>
        public static void Subtract(ref Color3 left, ref Color3 right, out Color3 result)
        {
            result.R = left.R - right.R;
            result.G = left.G - right.G;
            result.B = left.B - right.B;
        }

        /// <summary>
        /// Subtracts two colors.
        /// </summary>
        /// <param name="left">The first color to subtract.</param>
        /// <param name="right">The second color to subtract</param>
        /// <returns>The difference of the two colors.</returns>
        public static Color3 Subtract(Color3 left, Color3 right)
        {
            return new Color3(left.R - right.R, left.G - right.G, left.B - right.B);
        }

        /// <summary>
        /// Modulates two colors.
        /// </summary>
        /// <param name="left">The first color to modulate.</param>
        /// <param name="right">The second color to modulate.</param>
        /// <param name="result">When the method completes, contains the modulated color.</param>
        public static void Modulate(ref Color3 left, ref Color3 right, out Color3 result)
        {
            result.R = left.R * right.R;
            result.G = left.G * right.G;
            result.B = left.B * right.B;
        }

        /// <summary>
        /// Modulates two colors.
        /// </summary>
        /// <param name="left">The first color to modulate.</param>
        /// <param name="right">The second color to modulate.</param>
        /// <returns>The modulated color.</returns>
        public static Color3 Modulate(Color3 left, Color3 right)
        {
            return new Color3(left.R * right.R, left.G * right.G, left.B * right.B);
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="value">The color to scale.</param>
        /// <param name="scale">The amount by which to scale.</param>
        /// <param name="result">When the method completes, contains the scaled color.</param>
        public static void Scale(ref Color3 value, float scale, out Color3 result)
        {
            result.R = value.R * scale;
            result.G = value.G * scale;
            result.B = value.B * scale;
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="value">The color to scale.</param>
        /// <param name="scale">The amount by which to scale.</param>
        /// <returns>The scaled color.</returns>
        public static Color3 Scale(Color3 value, float scale)
        {
            return new Color3(value.R * scale, value.G * scale, value.B * scale);
        }

        /// <summary>
        /// Negates a color.
        /// </summary>
        /// <param name="value">The color to negate.</param>
        /// <param name="result">When the method completes, contains the negated color.</param>
        public static void Negate(ref Color3 value, out Color3 result)
        {
            result.R = 1.0f - value.R;
            result.G = 1.0f - value.G;
            result.B = 1.0f - value.B;
        }

        /// <summary>
        /// Negates a color.
        /// </summary>
        /// <param name="value">The color to negate.</param>
        /// <returns>The negated color.</returns>
        public static Color3 Negate(Color3 value)
        {
            return new Color3(1.0f - value.R, 1.0f - value.G, 1.0f - value.B);
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="result">When the method completes, contains the clamped value.</param>
        public static void Clamp(ref Color3 value, ref Color3 min, ref Color3 max, out Color3 result)
        {
            float red = value.R;
            red = (red > max.R) ? max.R : red;
            red = (red < min.R) ? min.R : red;

            float green = value.G;
            green = (green > max.G) ? max.G : green;
            green = (green < min.G) ? min.G : green;

            float blue = value.B;
            blue = (blue > max.B) ? max.B : blue;
            blue = (blue < min.B) ? min.B : blue;

            result = new Color3(red, green, blue);
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static Color3 Clamp(Color3 value, Color3 min, Color3 max)
        {
            Color3 result;
            Clamp(ref value, ref min, ref max, out result);
            return result;
        }

        /// <summary>
        /// Performs a linear interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the linear interpolation of the two colors.</param>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static void Lerp(ref Color3 start, ref Color3 end, float amount, out Color3 result)
        {
            result.R = start.R + amount * (end.R - start.R);
            result.G = start.G + amount * (end.G - start.G);
            result.B = start.B + amount * (end.B - start.B);
        }

        /// <summary>
        /// Performs a linear interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The linear interpolation of the two colors.</returns>
        /// <remarks>
        /// This method performs the linear interpolation based on the following formula.
        /// <code>start + (end - start) * amount</code>
        /// Passing <paramref name="amount"/> a value of 0 will cause <paramref name="start"/> to be returned; a value of 1 will cause <paramref name="end"/> to be returned. 
        /// </remarks>
        public static Color3 Lerp(Color3 start, Color3 end, float amount)
        {
            return new Color3(
                start.R + amount * (end.R - start.R),
                start.G + amount * (end.G - start.G),
                start.B + amount * (end.B - start.B));
        }

        /// <summary>
        /// Performs a cubic interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <param name="result">When the method completes, contains the cubic interpolation of the two colors.</param>
        public static void SmoothStep(ref Color3 start, ref Color3 end, float amount, out Color3 result)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            result.R = start.R + ((end.R - start.R) * amount);
            result.G = start.G + ((end.G - start.G) * amount);
            result.B = start.B + ((end.B - start.B) * amount);
        }

        /// <summary>
        /// Performs a cubic interpolation between two colors.
        /// </summary>
        /// <param name="start">Start color.</param>
        /// <param name="end">End color.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of <paramref name="end"/>.</param>
        /// <returns>The cubic interpolation of the two colors.</returns>
        public static Color3 SmoothStep(Color3 start, Color3 end, float amount)
        {
            amount = (amount > 1.0f) ? 1.0f : ((amount < 0.0f) ? 0.0f : amount);
            amount = (amount * amount) * (3.0f - (2.0f * amount));

            return new Color3(
                start.R + ((end.R - start.R) * amount),
                start.G + ((end.G - start.G) * amount),
                start.B + ((end.B - start.B) * amount));
        }

        /// <summary>
        /// Returns a color containing the smallest components of the specified colorss.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <param name="result">When the method completes, contains an new color composed of the largest components of the source colorss.</param>
        public static void Max(ref Color3 left, ref Color3 right, out Color3 result)
        {
            result.R = (left.R > right.R) ? left.R : right.R;
            result.G = (left.G > right.G) ? left.G : right.G;
            result.B = (left.B > right.B) ? left.B : right.B;
        }

        /// <summary>
        /// Returns a color containing the largest components of the specified colorss.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <returns>A color containing the largest components of the source colors.</returns>
        public static Color3 Max(Color3 left, Color3 right)
        {
            Color3 result;
            Max(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Returns a color containing the smallest components of the specified colors.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <param name="result">When the method completes, contains an new color composed of the smallest components of the source colors.</param>
        public static void Min(ref Color3 left, ref Color3 right, out Color3 result)
        {
            result.R = (left.R < right.R) ? left.R : right.R;
            result.G = (left.G < right.G) ? left.G : right.G;
            result.B = (left.B < right.B) ? left.B : right.B;
        }

        /// <summary>
        /// Returns a color containing the smallest components of the specified colors.
        /// </summary>
        /// <param name="left">The first source color.</param>
        /// <param name="right">The second source color.</param>
        /// <returns>A color containing the smallest components of the source colors.</returns>
        public static Color3 Min(Color3 left, Color3 right)
        {
            Color3 result;
            Min(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Adjusts the contrast of a color.
        /// </summary>
        /// <param name="value">The color whose contrast is to be adjusted.</param>
        /// <param name="contrast">The amount by which to adjust the contrast.</param>
        /// <param name="result">When the method completes, contains the adjusted color.</param>
        public static void AdjustContrast(ref Color3 value, float contrast, out Color3 result)
        {
            result.R = 0.5f + contrast * (value.R - 0.5f);
            result.G = 0.5f + contrast * (value.G - 0.5f);
            result.B = 0.5f + contrast * (value.B - 0.5f);
        }

        /// <summary>
        /// Adjusts the contrast of a color.
        /// </summary>
        /// <param name="value">The color whose contrast is to be adjusted.</param>
        /// <param name="contrast">The amount by which to adjust the contrast.</param>
        /// <returns>The adjusted color.</returns>
        public static Color3 AdjustContrast(Color3 value, float contrast)
        {
            return new Color3(
                0.5f + contrast * (value.R - 0.5f),
                0.5f + contrast * (value.G - 0.5f),
                0.5f + contrast * (value.B - 0.5f));
        }

        /// <summary>
        /// Adjusts the saturation of a color.
        /// </summary>
        /// <param name="value">The color whose saturation is to be adjusted.</param>
        /// <param name="saturation">The amount by which to adjust the saturation.</param>
        /// <param name="result">When the method completes, contains the adjusted color.</param>
        public static void AdjustSaturation(ref Color3 value, float saturation, out Color3 result)
        {
            float grey = value.R * 0.2125f + value.G * 0.7154f + value.B * 0.0721f;

            result.R = grey + saturation * (value.R - grey);
            result.G = grey + saturation * (value.G - grey);
            result.B = grey + saturation * (value.B - grey);
        }

        /// <summary>
        /// Adjusts the saturation of a color.
        /// </summary>
        /// <param name="value">The color whose saturation is to be adjusted.</param>
        /// <param name="saturation">The amount by which to adjust the saturation.</param>
        /// <returns>The adjusted color.</returns>
        public static Color3 AdjustSaturation(Color3 value, float saturation)
        {
            float grey = value.R * 0.2125f + value.G * 0.7154f + value.B * 0.0721f;

            return new Color3(
                grey + saturation * (value.R - grey),
                grey + saturation * (value.G - grey),
                grey + saturation * (value.B - grey));
        }

        /// <summary>
        /// Converts this color from linear space to sRGB space.
        /// </summary>
        /// <returns>A color3 in sRGB space.</returns>
        public Color3 ToSRgb()
        {
            return new Color3(MathUtil.LinearToSRgb(R), MathUtil.LinearToSRgb(G), MathUtil.LinearToSRgb(B));
        }

        /// <summary>
        /// Converts this color from sRGB space to linear space.
        /// </summary>
        /// <returns>Color3.</returns>
        public Color3 ToLinear()
        {
            return new Color3(MathUtil.SRgbToLinear(R), MathUtil.SRgbToLinear(G), MathUtil.SRgbToLinear(B));
        }

        /// <summary>
        /// Adds two colors.
        /// </summary>
        /// <param name="left">The first color to add.</param>
        /// <param name="right">The second color to add.</param>
        /// <returns>The sum of the two colors.</returns>
        public static Color3 operator +(Color3 left, Color3 right)
        {
            return new Color3(left.R + right.R, left.G + right.G, left.B + right.B);
        }

        /// <summary>
        /// Assert a color (return it unchanged).
        /// </summary>
        /// <param name="value">The color to assert (unchange).</param>
        /// <returns>The asserted (unchanged) color.</returns>
        public static Color3 operator +(Color3 value)
        {
            return value;
        }

        /// <summary>
        /// Subtracts two colors.
        /// </summary>
        /// <param name="left">The first color to subtract.</param>
        /// <param name="right">The second color to subtract.</param>
        /// <returns>The difference of the two colors.</returns>
        public static Color3 operator -(Color3 left, Color3 right)
        {
            return new Color3(left.R - right.R, left.G - right.G, left.B - right.B);
        }

        /// <summary>
        /// Negates a color.
        /// </summary>
        /// <param name="value">The color to negate.</param>
        /// <returns>A negated color.</returns>
        public static Color3 operator -(Color3 value)
        {
            return new Color3(-value.R, -value.G, -value.B);
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="scale">The factor by which to scale the color.</param>
        /// <param name="value">The color to scale.</param>
        /// <returns>The scaled color.</returns>
        public static Color3 operator *(float scale, Color3 value)
        {
            return new Color3(value.R * scale, value.G * scale, value.B * scale);
        }

        /// <summary>
        /// Scales a color.
        /// </summary>
        /// <param name="value">The factor by which to scale the color.</param>
        /// <param name="scale">The color to scale.</param>
        /// <returns>The scaled color.</returns>
        public static Color3 operator *(Color3 value, float scale)
        {
            return new Color3(value.R * scale, value.G * scale, value.B * scale);
        }

        /// <summary>
        /// Modulates two colors.
        /// </summary>
        /// <param name="left">The first color to modulate.</param>
        /// <param name="right">The second color to modulate.</param>
        /// <returns>The modulated color.</returns>
        public static Color3 operator *(Color3 left, Color3 right)
        {
            return new Color3(left.R * right.R, left.G * right.G, left.B * right.B);
        }

        /// <summary>
        /// Tests for equality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Color3 left, Color3 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests for inequality between two objects.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Color3 left, Color3 right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Color3"/> to <see cref="Color4"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Color4(Color3 value)
        {
            return new Color4(value.R, value.G, value.B, 1.0f);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Color3"/> to <see cref="Xenko.Core.Mathematics.Vector3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Vector3(Color3 value)
        {
            return new Vector3(value.R, value.G, value.B);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Xenko.Core.Mathematics.Vector3"/> to <see cref="Color3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Color3(Vector3 value)
        {
            return new Color3(value.X, value.Y, value.Z);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="int"/> to <see cref="Color3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Color3(int value)
        {
            return new Color3(value);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Convert this color to an equivalent <see cref="Color4"/> with an opaque alpha.
        /// </summary>
        /// <returns>An equivalent <see cref="Color4"/> with an opaque alpha.</returns>
        public Color4 ToColor4()
        {
            return new Color4(R, G, B, 1.0f);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, ToStringFormat, R, G, B);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return ToString(formatProvider);

            return string.Format(formatProvider, ToStringFormat,
                                 R.ToString(format, formatProvider),
                                 G.ToString(format, formatProvider),
                                 B.ToString(format, formatProvider));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return R.GetHashCode() + G.GetHashCode() + B.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Color3"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Color3"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Color3"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Color3 other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object value)
        {
            if (value == null)
                return false;

            if (value.GetType() != GetType())
                return false;

            return Equals((Color3)value);
        }

#if SlimDX1xInterop
        /// <summary>
        /// Performs an implicit conversion from <see cref="Xenko.Core.Mathematics.Color3"/> to <see cref="SlimDX.Color3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator SlimDX.Color3(Color3 value)
        {
            return new SlimDX.Color3(value.Red, value.Green, value.Blue);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SlimDX.Color3"/> to <see cref="Xenko.Core.Mathematics.Color3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Color3(SlimDX.Color3 value)
        {
            return new Color3(value.Red, value.Green, value.Blue);
        }
#endif

#if WPFInterop
        /// <summary>
        /// Performs an explicit conversion from <see cref="Xenko.Core.Mathematics.Color3"/> to <see cref="System.Windows.Media.Color"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator System.Windows.Media.Color(Color3 value)
        {
            return new System.Windows.Media.Color()
            {
                A = 255,
                R = (byte)(255f * value.Red),
                G = (byte)(255f * value.Green),
                B = (byte)(255f * value.Blue)
            };
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Windows.Media.Color"/> to <see cref="Xenko.Core.Mathematics.Color3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Color3(System.Windows.Media.Color value)
        {
            return new Color3()
            {
                Red = (float)value.R / 255f,
                Green = (float)value.G / 255f,
                Blue = (float)value.B / 255f
            };
        }
#endif

#if WinFormsInterop
        /// <summary>
        /// Performs an explicit conversion from <see cref="Xenko.Core.Mathematics.Color3"/> to <see cref="System.Drawing.Color"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator System.Drawing.Color(Color3 value)
        {
            return System.Drawing.Color.FromArgb(
                (byte)(255f * value.Red),
                (byte)(255f * value.Green),
                (byte)(255f * value.Blue));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Drawing.Color"/> to <see cref="Xenko.Core.Mathematics.Color3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Color3(System.Drawing.Color value)
        {
            return new Color3()
            {
                Red = (float)value.R / 255f,
                Green = (float)value.G / 255f,
                Blue = (float)value.B / 255f
            };
        }
#endif
    }
}
