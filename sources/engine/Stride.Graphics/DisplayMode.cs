// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Globalization;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes the display mode.
    /// </summary>
    public partial class DisplayMode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayMode"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="refreshRate">The refresh rate.</param>
        public DisplayMode(PixelFormat format, int width, int height, Rational refreshRate)
        {
            Format = format;
            Width = width;
            Height = height;
            RefreshRate = refreshRate;
        }

        /// <summary>
        /// Gets the aspect ratio used by the graphics device.
        /// </summary>
        public float AspectRatio
        {
            get
            {
                if ((Height != 0) && (Width != 0))
                {
                    return ((float)Width) / Height;
                }
                return 0f;
            }
        }

        /// <summary>
        /// Gets a value indicating the surface format of the display mode.
        /// </summary>
        public readonly PixelFormat Format;

        /// <summary>
        /// Gets a value indicating the screen width, in pixels.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Gets a value indicating the screen height, in pixels.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Gets a value indicating the refresh rate
        /// </summary>
        public readonly Rational RefreshRate;

        /// <summary>
        /// Retrieves a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "[Width:{0} Height:{1} Format:{2} AspectRatio:{3}]",  Width, Height, Format, AspectRatio);
        }
    }
}
