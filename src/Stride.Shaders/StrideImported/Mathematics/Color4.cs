// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Core.Mathematics;

/// <summary>
///   A RGBA color value with 32-bit floating-point precision per channel.
/// </summary>
[DataContract("Color4")]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Color4 : IEquatable<Color4>, ISpanFormattable
{
    /// <summary>
    /// The Black color (0, 0, 0, 1).
    /// </summary>
    public static readonly Color4 Black = new(red: 0, green: 0, blue: 0);

    /// <summary>
    /// The White color (1, 1, 1, 1).
    /// </summary>
    public static readonly Color4 White = new(red: 1, green: 1, blue: 1);

    /// <summary>
    /// The transparent black color (0, 0, 0, 0).
    /// </summary>
    public static readonly Color4 TransparentBlack = default;

    /// <summary>
    /// The red component of the color.
    /// </summary>
    public float R;

    /// <summary>
    /// The green component of the color.
    /// </summary>
    public float G;

    /// <summary>
    /// The blue component of the color.
    /// </summary>
    public float B;

    /// <summary>
    /// The alpha component of the color.
    /// </summary>
    public float A;

    /// <summary>
    /// Initializes a new instance of the <see cref="Color4"/> struct.
    /// </summary>
    /// <param name="value">The value that will be assigned to all components.</param>
    public Color4(float value)
    {
        R = value;
        G = value;
        B = value;
        A = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color4"/> struct.
    /// </summary>
    /// <param name="red">The red component of the color.</param>
    /// <param name="green">The green component of the color.</param>
    /// <param name="blue">The blue component of the color.</param>
    /// <param name="alpha">The alpha component of the color.</param>
    public Color4(float red, float green, float blue, float alpha = 1f)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color4"/> struct.
    /// </summary>
    /// <param name="rgba">A packed integer containing all four color components in RGBA order.</param>
    public Color4(uint rgba)
    {
        A = ((rgba >> 24) & 255) / 255.0f;
        B = ((rgba >> 16) & 255) / 255.0f;
        G = ((rgba >> 8) & 255) / 255.0f;
        R = (rgba & 255) / 255.0f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color4"/> struct.
    /// </summary>
    /// <param name="rgba">A packed integer containing all four color components in RGBA order.</param>
    public Color4(int rgba)
    {
        A = ((rgba >> 24) & 255) / 255.0f;
        B = ((rgba >> 16) & 255) / 255.0f;
        G = ((rgba >> 8) & 255) / 255.0f;
        R = (rgba & 255) / 255.0f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color4"/> struct.
    /// </summary>
    /// <param name="values">The values to assign to the red, green, blue, and alpha components of the color. This must be an array with four elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
    public Color4(float[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length is not 3 and not 4)
            throw new ArgumentOutOfRangeException(nameof(values), "There must be 3 or 4 float[] values for Color4.");

        R = values[0];
        G = values[1];
        B = values[2];
        A = values.Length >= 4 ? values[3] : 1f;
    }

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the red, green, blue, and alpha components, depending on the index.</value>
    /// <param name="index">The index of the component to access. Use 0 for the alpha component, 1 for the red component, 2 for the green component, and 3 for the blue component.</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 3].</exception>
    public float this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => R,
                1 => G,
                2 => B,
                3 => A,
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Indices for Color4 run from 0 to 3, inclusive."),
            };
        }

        set
        {
            switch (index)
            {
                case 0: R = value; break;
                case 1: G = value; break;
                case 2: B = value; break;
                case 3: A = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), "Indices for Color4 run from 0 to 3, inclusive.");
            }
        }
    }

    /// <summary>
    /// Converts the color into a packed integer.
    /// </summary>
    /// <returns>A packed integer containing all four color components.</returns>
    public readonly int ToBgra()
    {
        uint a = (uint)(A * 255.0f) & 255;
        uint r = (uint)(R * 255.0f) & 255;
        uint g = (uint)(G * 255.0f) & 255;
        uint b = (uint)(B * 255.0f) & 255;

        uint value = b;
        value |= g << 8;
        value |= r << 16;
        value |= a << 24;

        return (int)value;
    }

    /// <summary>
    /// Converts the color into a packed integer.
    /// </summary>
    public readonly void ToBgra(out byte r, out byte g, out byte b, out byte a)
    {
        a = (byte)(A * 255.0f);
        r = (byte)(R * 255.0f);
        g = (byte)(G * 255.0f);
        b = (byte)(B * 255.0f);
    }

    /// <summary>
    /// Converts the color into a packed integer.
    /// </summary>
    /// <returns>A packed integer containing all four color components.</returns>
    public readonly int ToRgba()
    {
        uint a = (uint)(A * 255.0f) & 255;
        uint r = (uint)(R * 255.0f) & 255;
        uint g = (uint)(G * 255.0f) & 255;
        uint b = (uint)(B * 255.0f) & 255;

        uint value = r;
        value |= g << 8;
        value |= b << 16;
        value |= a << 24;

        return (int)value;
    }

    /// <summary>
    /// Creates an array containing the elements of the color.
    /// </summary>
    /// <returns>A four-element array containing the components of the color.</returns>
    public readonly float[] ToArray()
    {
        return [R, G, B, A];
    }

    /// <summary>
    /// Adds two colors.
    /// </summary>
    /// <param name="left">The first color to add.</param>
    /// <param name="right">The second color to add.</param>
    /// <param name="result">When the method completes, completes the sum of the two colors.</param>
    public static void Add(ref readonly Color4 left, ref readonly Color4 right, out Color4 result)
    {
        result.A = left.A + right.A;
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
    public static Color4 Add(Color4 left, Color4 right)
    {
        return new Color4(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
    }

    /// <summary>
    /// Subtracts two colors.
    /// </summary>
    /// <param name="left">The first color to subtract.</param>
    /// <param name="right">The second color to subtract.</param>
    /// <param name="result">WHen the method completes, contains the difference of the two colors.</param>
    public static void Subtract(ref readonly Color4 left, ref readonly Color4 right, out Color4 result)
    {
        result.A = left.A - right.A;
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
    public static Color4 Subtract(Color4 left, Color4 right)
    {
        return new Color4(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);
    }

    /// <summary>
    /// Modulates two colors.
    /// </summary>
    /// <param name="left">The first color to modulate.</param>
    /// <param name="right">The second color to modulate.</param>
    /// <param name="result">When the method completes, contains the modulated color.</param>
    public static void Modulate(ref readonly Color4 left, ref readonly Color4 right, out Color4 result)
    {
        result.A = left.A * right.A;
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
    public static Color4 Modulate(Color4 left, Color4 right)
    {
        return new Color4(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
    }

    /// <summary>
    /// Scales a color.
    /// </summary>
    /// <param name="value">The color to scale.</param>
    /// <param name="scale">The amount by which to scale.</param>
    /// <param name="result">When the method completes, contains the scaled color.</param>
    public static void Scale(ref readonly Color4 value, float scale, out Color4 result)
    {
        result.A = value.A * scale;
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
    public static Color4 Scale(Color4 value, float scale)
    {
        return new Color4(value.R * scale, value.G * scale, value.B * scale, value.A * scale);
    }

    /// <summary>
    /// Negates a color.
    /// </summary>
    /// <param name="value">The color to negate.</param>
    /// <param name="result">When the method completes, contains the negated color.</param>
    public static void Negate(ref readonly Color4 value, out Color4 result)
    {
        result.A = 1.0f - value.A;
        result.R = 1.0f - value.R;
        result.G = 1.0f - value.G;
        result.B = 1.0f - value.B;
    }

    /// <summary>
    /// Negates a color.
    /// </summary>
    /// <param name="value">The color to negate.</param>
    /// <returns>The negated color.</returns>
    public static Color4 Negate(Color4 value)
    {
        return new Color4(1.0f - value.R, 1.0f - value.G, 1.0f - value.B, 1.0f - value.A);
    }

    /// <summary>
    /// Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="result">When the method completes, contains the clamped value.</param>
    public static void Clamp(ref readonly Color4 value, ref readonly Color4 min, ref readonly Color4 max, out Color4 result)
    {
        float alpha = value.A;
        alpha = (alpha > max.A) ? max.A : alpha;
        alpha = (alpha < min.A) ? min.A : alpha;

        float red = value.R;
        red = (red > max.R) ? max.R : red;
        red = (red < min.R) ? min.R : red;

        float green = value.G;
        green = (green > max.G) ? max.G : green;
        green = (green < min.G) ? min.G : green;

        float blue = value.B;
        blue = (blue > max.B) ? max.B : blue;
        blue = (blue < min.B) ? min.B : blue;

        result = new Color4(red, green, blue, alpha);
    }

    /// <summary>
    /// Restricts a value to be within a specified range.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The clamped value.</returns>
    public static Color4 Clamp(Color4 value, Color4 min, Color4 max)
    {
        Clamp(ref value, ref min, ref max, out var result);
        return result;
    }

    /// <summary>
    /// Premultiplies the color components by the alpha value.
    /// </summary>
    /// <param name="value">The color to premultiply.</param>
    /// <returns>A color with premultiplied alpha.</returns>
    public static Color4 PremultiplyAlpha(Color4 value)
    {
        return new Color4(value.R * value.A, value.G * value.A, value.B * value.A, value.A);
    }

    /// <summary>
    /// Adds two colors.
    /// </summary>
    /// <param name="left">The first color to add.</param>
    /// <param name="right">The second color to add.</param>
    /// <returns>The sum of the two colors.</returns>
    public static Color4 operator +(Color4 left, Color4 right)
    {
        return new Color4(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
    }

    /// <summary>
    /// Assert a color (return it unchanged).
    /// </summary>
    /// <param name="value">The color to assert (unchanged).</param>
    /// <returns>The asserted (unchanged) color.</returns>
    public static Color4 operator +(Color4 value)
    {
        return value;
    }

    /// <summary>
    /// Subtracts two colors.
    /// </summary>
    /// <param name="left">The first color to subtract.</param>
    /// <param name="right">The second color to subtract.</param>
    /// <returns>The difference of the two colors.</returns>
    public static Color4 operator -(Color4 left, Color4 right)
    {
        return new Color4(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);
    }

    /// <summary>
    /// Negates a color.
    /// </summary>
    /// <param name="value">The color to negate.</param>
    /// <returns>A negated color.</returns>
    public static Color4 operator -(Color4 value)
    {
        return new Color4(-value.R, -value.G, -value.B, -value.A);
    }

    /// <summary>
    /// Scales a color.
    /// </summary>
    /// <param name="scale">The factor by which to scale the color.</param>
    /// <param name="value">The color to scale.</param>
    /// <returns>The scaled color.</returns>
    public static Color4 operator *(float scale, Color4 value)
    {
        return new Color4(value.R * scale, value.G * scale, value.B * scale, value.A * scale);
    }

    /// <summary>
    /// Scales a color.
    /// </summary>
    /// <param name="value">The factor by which to scale the color.</param>
    /// <param name="scale">The color to scale.</param>
    /// <returns>The scaled color.</returns>
    public static Color4 operator *(Color4 value, float scale)
    {
        return new Color4(value.R * scale, value.G * scale, value.B * scale, value.A * scale);
    }

    /// <summary>
    /// Modulates two colors.
    /// </summary>
    /// <param name="left">The first color to modulate.</param>
    /// <param name="right">The second color to modulate.</param>
    /// <returns>The modulated color.</returns>
    public static Color4 operator *(Color4 left, Color4 right)
    {
        return new Color4(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
    }

    /// <summary>
    /// Tests for equality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has the same value as <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Color4 left, Color4 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Tests for inequality between two objects.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> has a different value than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Color4 left, Color4 right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="Color4"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator int(Color4 value)
    {
        return value.ToRgba();
    }

    /// <summary>
    /// Performs an explicit conversion from <see cref="int"/> to <see cref="Color4"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator Color4(int value)
    {
        return new Color4(value);
    }

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public override readonly string ToString() => $"{this}";
    
    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        var handler = new DefaultInterpolatedStringHandler(11, 4, formatProvider);
        handler.AppendLiteral("A:");
        handler.AppendFormatted(A, format);
        handler.AppendLiteral(" R:");
        handler.AppendFormatted(R, format);
        handler.AppendLiteral(" G:");
        handler.AppendFormatted(G, format);
        handler.AppendLiteral(" B:");
        handler.AppendFormatted(B, format);
        return handler.ToStringAndClear();
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var format1 = format.Length > 0 ? format.ToString() : null;
        var handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(11, 4, destination, provider, out _);
        handler.AppendLiteral("A:");
        handler.AppendFormatted(A, format1);
        handler.AppendLiteral(" R:");
        handler.AppendFormatted(R, format1);
        handler.AppendLiteral(" G:");
        handler.AppendFormatted(G, format1);
        handler.AppendLiteral(" B:");
        handler.AppendFormatted(B, format1);
        return destination.TryWrite(ref handler, out charsWritten);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(A, R, G, B);
    }

    /// <summary>
    /// Determines whether the specified <see cref="Color4"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="Color4"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="Color4"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool Equals(Color4 other)
    {
        return A == other.A && R == other.R && G == other.G && B == other.B;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="value">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override readonly bool Equals(object? value)
    {
        return value is Color4 color && Equals(color);
    }

    /// <summary>
    /// Deconstructs the vector's components into named variables.
    /// </summary>
    /// <param name="r">The R component</param>
    /// <param name="g">The G component</param>
    /// <param name="b">The B component</param>
    /// <param name="a">The A component</param>
    public readonly void Deconstruct(out float r, out float g, out float b, out float a)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    }
}
