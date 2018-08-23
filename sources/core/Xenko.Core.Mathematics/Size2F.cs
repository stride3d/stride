// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using System;
using System.Runtime.InteropServices;

namespace Xenko.Core.Mathematics
{
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
        public static readonly Size2F Zero = new Size2F(0, 0);

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
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Size2F other)
        {
            return other.Width == Width && other.Height == Height;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Size2F)) return false;
            return Equals((Size2F)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Width.GetHashCode() * 397) ^ Height.GetHashCode();
            }
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("({0},{1})", Width, Height);
        }
    }
}
