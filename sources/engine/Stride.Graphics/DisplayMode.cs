// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics
{
    /// <summary>
    ///   Describes a display mode.
    /// </summary>
    /// <param name="Format">The pixel format of this display mode.</param>
    /// <param name="Width">The screen width, in pixels.</param>
    /// <param name="Height">The screen height, in pixels.</param>
    /// <param name="RefreshRate">The refresh rate, in Hz.</param>
    public readonly partial record struct DisplayMode(PixelFormat Format, int Width, int Height, Rational RefreshRate)
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="DisplayMode"/> record.
        /// </summary>
        /// <param name="format">The pixel format of this display mode.</param>
        /// <param name="width">The screen width, in pixels.</param>
        /// <param name="height">The screen height, in pixels.</param>
        /// <param name="refreshRate">The refresh rate, in Hz.</param>
        public DisplayMode(PixelFormat format, int width, int height, uint refreshRate)
            : this(format, width, height, new Rational((int) refreshRate, 1)) { }


        /// <summary>
        ///   Gets the aspect ratio of this display mode.
        /// </summary>
        /// <remarks>
        ///   The aspect ratio is the ratio of the display mode's <see cref="Width"/> in relation to the <see cref="Height"/>,
        ///   i.e. <c>Width / Height</c>.
        /// </remarks>
        public readonly float AspectRatio => (Height != 0) && (Width != 0) ? (float) Width / Height : 0;
    }
}
