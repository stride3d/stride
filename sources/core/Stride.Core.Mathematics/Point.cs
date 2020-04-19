// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Stride.Core.Serialization;

namespace Stride.Core.Mathematics
{
    /// <summary>
    /// A 2D point.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Point : IEquatable<Point>
    {
        /// <summary>
        /// A point with (0,0) coordinates.
        /// </summary>
        public static readonly Point Zero = new Point(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> struct.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Left coordinate.
        /// </summary>
        [DataMember(0)]
        public int X;

        /// <summary>
        /// Top coordinate.
        /// </summary>
        [DataMember(1)]
        public int Y;

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Point other)
        {
            return other.X == X && other.Y == Y;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Point)) return false;
            return Equals((Point)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
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
        public static bool operator ==(Point left, Point right)
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
        public static bool operator !=(Point left, Point right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("({0},{1})", X, Y);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Vector2"/> to <see cref="Point"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Point(Vector2 value)
        {
            return new Point((int)value.X, (int)value.Y);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Point"/> to <see cref="Vector2"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Vector2(Point value)
        {
            return new Vector2(value.X, value.Y);
        }
   }
}
