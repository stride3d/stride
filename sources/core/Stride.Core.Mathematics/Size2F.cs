// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Core.Mathematics;

/// <summary>
/// Defines a 2D rectangular size (width,height).
/// </summary>
[DataContract("Size2F")]
[DataStyle(DataStyle.Compact)]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Size2F : IEquatable<Size2F>
{
    /// <summary>
    /// A zero size with (width, height) = (0,0)
    /// </summary>
    public static readonly Size2F Zero = new(0, 0);

    /// <summary>
    /// A zero size with (width, height) = (0,0)
    /// </summary>
    public static readonly Size2F Empty = Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="Size2F"/> struct.
    /// </summary>
    /// <param name="width">The x.</param>
    /// <param name="height">The y.</param>
    public Size2F(float width, float height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Size2F"/> struct.
    /// </summary>
    /// <param name="uniform">The width and height of the <see cref="Size2F"/>.</param>
    public Size2F(float uniform)
    {
        Width = uniform;
        Height = uniform;
    }

    /// <summary>
    /// Width.
    /// </summary>
    [DataMember(0)]
    public float Width;

    /// <summary>
    /// Height.
    /// </summary>
    [DataMember(1)]
    public float Height;

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the Width or Height component, depending on the index.</value>
    /// <param name="index">The index of the component to access. Use 0 for the Width component and 1 for the Height component.</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 1].</exception>
    public float this[int index]
    {
        readonly get
        {
            return index switch
            {
                0 => Width,
                1 => Height,
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Indices for Size2F run from 0 to 1, inclusive."),
            };
        }
        set
        {
            switch (index)
            {
                case 0: Width = value; break;
                case 1: Height = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index), "Indices for Size2F run from 0 to 1, inclusive.");
            }
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="other">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>
    ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public readonly bool Equals(Size2F other)
    {
        return other.Width == Width && other.Height == Height;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Size2F size && Equals(size);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(Size2F left, Size2F right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(Size2F left, Size2F right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Implements the operator <c>/</c>, component wise.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Size2F operator /(Size2F left, Size2F right)
    {
        return new Size2F(left.Width / right.Width, left.Height / right.Height);
    }

    /// <summary>
    /// Implements the operator <c>/</c>, component wise.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Size2F operator /(Size2F left, float right)
    {
        return new Size2F(left.Width / right, left.Height / right);
    }

    /// <summary>
    /// Implements the operator <c>*</c>, component wise.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Size2F operator *(Size2F left, Size2F right)
    {
        return new Size2F(left.Width * right.Width, left.Height * right.Height);
    }

    /// <summary>
    /// Implements the operator <c>*</c>, component wise.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Size2F operator *(Size2F left, float right)
    {
        return new Size2F(left.Width * right, left.Height * right);
    }

    /// <summary>
    /// Implements the operator <c>+</c>, component wise.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Size2F operator +(Size2F left, Size2F right)
    {
        return new Size2F(left.Width + right.Width, left.Height + right.Height);
    }

    /// <summary>
    /// Implements the operator <c>-</c>, component wise.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static Size2F operator -(Size2F left, Size2F right)
    {
        return new Size2F(left.Width + right.Width, left.Height + right.Height);
    }

    /// <summary>
    /// Returns a size containing the largest components of the specified sizes.
    /// </summary>
    /// <param name="left">The first source size.</param>
    /// <param name="right">The second source size.</param>
    /// <returns>A size containing the largest components of the source size.</returns>
    public static Size2F Max(Size2F left, Size2F right)
    {
        return new Size2F(Math.Max(left.Width, right.Width), Math.Max(left.Height, right.Height));
    }

    /// <summary>
    /// Returns a size containing the smallest components of the specified sizes.
    /// </summary>
    /// <param name="left">The first source size.</param>
    /// <param name="right">The second source size.</param>
    /// <returns>A size containing the smallest components of the source size.</returns>
    public static Size2F Min(Size2F left, Size2F right)
    {
        return new Size2F(Math.Min(left.Width, right.Width), Math.Min(left.Height, right.Height));
    }

    /// <summary>
    /// Casts from <see cref="Vector2"/> to <see cref="Size2F"/>.
    /// </summary>
    /// <param name="vector">Value to cast.</param>
    public static explicit operator Size2F(Vector2 vector)
    {
        return Unsafe.BitCast<Vector2, Size2F>(vector);
    }

    /// <summary>
    /// Casts from <see cref="Size2F"/> to <see cref="Vector2"/>.
    /// </summary>
    /// <param name="size">Value to cast.</param>
    public static explicit operator Vector2(Size2F size)
    {
        return Unsafe.BitCast<Size2F, Vector2>(size);
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return string.Format("({0},{1})", Width, Height);
    }

    /// <summary>
    /// Deconstructs the vector's components into named variables.
    /// </summary>
    /// <param name="width">The Width component</param>
    /// <param name="height">The Height component</param>
    public readonly void Deconstruct(out float width, out float height)
    {
        width = Width;
        height = Height;
    }
}
