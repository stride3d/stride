// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Assets.Textures.Packing
{
    /// <summary>
    /// RotableRectangle adds a rotating status to Rectangle struct type indicating that this rectangle is rotated by 90 degree and that width and height is swapped.
    /// </summary>
    public struct RotableRectangle
    {
        /// <summary>
        /// The starting position of the rectangle along X.
        /// </summary>
        public int X;

        /// <summary>
        /// The starting position of the rectangle along Y.
        /// </summary>
        public int Y;

        /// <summary>
        /// The width of the rectangle.
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the rectangle
        /// </summary>
        public int Height;

        /// <summary>
        /// Gets or sets a rotation flag to indicate that this rectangle is rotated by 90 degree
        /// </summary>
        public bool IsRotated;

        /// <summary>
        /// Initializes a new instance of RotableRectangle with top-left position: x, y, width and height of the rectangle with an optional key 
        /// </summary>
        /// <param name="x">Left value in X axis</param>
        /// <param name="y">Top value in Y axis</param>
        /// <param name="width">Width of a rectangle</param>
        /// <param name="height">Height of a rectangle</param>
        /// <param name="isRotated">Indicate if the rectangle is rotated or not</param>
        public RotableRectangle(int x, int y, int width, int height, bool isRotated = false)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            IsRotated = isRotated;
        }

        /// <summary>
        /// Initializes a new instance of RotableRectangle from an rectangle
        /// </summary>
        /// <param name="rectangle">Reference rectangle</param>
        /// <param name="isRotated">Indicate if the rectangle is rotated or not</param>
        public RotableRectangle(Rectangle rectangle, bool isRotated = false)
        {
            X = rectangle.X;
            Y = rectangle.Y;
            Width = rectangle.Width;
            Height = rectangle.Height;
            IsRotated = isRotated;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "X:{0} Y:{1} Width:{2} Height:{3} Rotated: {4}", X, Y, Width, Height, IsRotated);
        }

        /// <summary>
        /// Specify if the rectangle is empty. That is, if it has a null area.
        /// </summary>
        /// <returns><value>True</value> if empty</returns>
        public bool IsEmpty()
        {
            return Width <= 0 || Height <= 0;
        }

        /// <summary>
        /// Gets the position of the right border of the triangle
        /// </summary>
        public int Right { get { return X + Width; } }

        /// <summary>
        /// Gets the position of the bottom border of the triangle
        /// </summary>
        public int Bottom { get { return Y + Height; } }
    }
}
