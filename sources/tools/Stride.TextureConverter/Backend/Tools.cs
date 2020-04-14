// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Graphics;

namespace Stride.TextureConverter
{
    /// <summary>
    /// Provides general methods used by the libraries.
    /// </summary>
    internal class Tools
    {
        /// <summary>
        /// Computes the pitch.
        /// </summary>
        /// <param name="fmt">The format.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="rowPitch">output row pitch.</param>
        /// <param name="slicePitch">output slice pitch.</param>
        public static void ComputePitch(PixelFormat fmt, int width, int height, out int rowPitch, out int slicePitch)
        {
            int widthCount = width;
            int heightCount = height;

            int bpp = fmt.SizeInBits();

            if (fmt.IsCompressed())
            {
                widthCount = Math.Max(1, (width + 3) / 4);
                heightCount = Math.Max(1, (height + 3) / 4);
                rowPitch = widthCount * bpp;

                slicePitch = rowPitch * heightCount;
            }
            else if (fmt.IsPacked())
            {
                rowPitch = ((width + 1) >> 1) * 4;

                slicePitch = rowPitch * height;
            }
            else
            {
                if (bpp == 0)
                    bpp = fmt.SizeInBits();

                rowPitch = (width * bpp + 7) / 8;
                slicePitch = rowPitch * height;
            }
        }

        /// <summary>
        /// Determines whether two different formats are in same channel order.
        /// </summary>
        /// <param name="format1">The format1.</param>
        /// <param name="format2">The format2.</param>
        /// <returns>
        ///   <c>true</c> if the formats are in the same channel order; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInSameChannelOrder(PixelFormat format1, PixelFormat format2)
        {
            return format1.IsBGRAOrder() && format2.IsBGRAOrder() || format1.IsRGBAOrder() && format2.IsRGBAOrder();
        }
    }
}
