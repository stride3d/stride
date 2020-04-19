// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Extensions
{
    using WindowsPoint = System.Windows.Point;
    using WindowsRect = System.Windows.Rect;
    using WindowsThickness = System.Windows.Thickness;
    using WindowsVector = System.Windows.Vector;

    public static class MathExtensions
    {
        /// <summary>
        /// Returns the location of the center of the rectangle.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static WindowsPoint GetCenterLocation(this WindowsRect r)
        {
            return new WindowsPoint(r.X + r.Width*0.5f, r.Y + r.Height*0.5f);
        }

        /// <summary>
        /// Returns a rectangle that is shrunk by the specified thickness, in all directions.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="t">The thickness to apply to the rectangle.</param>
        /// <returns>The deflated rectangle.</returns>
        public static WindowsRect Deflate(this WindowsRect r, WindowsThickness t)
        {
            return new WindowsRect(r.Left + t.Left, r.Top + t.Top, r.Width - t.Left - t.Right, r.Height - t.Top - t.Bottom);
        }

        /// <summary>
        /// Returns a rectangle that is expanded by the specified thickness, in all directions.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="t">The thickness to apply to the rectangle.</param>
        /// <returns>The inflated rectangle.</returns>
        public static WindowsRect Inflate(this WindowsRect r, WindowsThickness t)
        {
            return new WindowsRect(r.Left - t.Left, r.Top - t.Top, r.Width + t.Left + t.Right, r.Height + t.Top + t.Bottom);
        }

        /// <summary>
        /// Converts a <see cref="WindowsPoint"/> to a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this WindowsPoint point)
        {
            return new Vector2((float)point.X, (float)point.Y);
        }

        /// <summary>
        /// Converts a <see cref="WindowsVector"/> to a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this WindowsVector vector)
        {
            return new Vector2((float)vector.X, (float)vector.Y);
        }

        /// <summary>
        /// Converts a <see cref="Vector2"/> to a <see cref="WindowsPoint"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WindowsPoint ToWindowsPoint(this Vector2 point)
        {
            return new WindowsPoint(point.X, point.Y);
        }

        /// <summary>
        /// Converts a <see cref="Vector2"/> to a <see cref="WindowsVector"/>.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WindowsVector ToWindowsVector(this Vector2 vector)
        {
            return new WindowsVector(vector.X, vector.Y);
        }
    }
}
