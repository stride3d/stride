// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using SharpFont;

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics.Font
{
    /// <summary>
    /// A bitmap representing a given character
    /// </summary>
    internal sealed class CharacterBitmap : IDisposable
    {
        private readonly int width;
        private readonly int rows;
        private readonly int pitch;
        private readonly int grayLevels;

        private readonly PixelMode pixelMode;

        private readonly IntPtr buffer;
        
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterBitmap"/> representing a null bitmap.
        /// </summary>
        public CharacterBitmap()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterBitmap"/> class from a data array.
        /// </summary>
        /// <param name="pixelMode">The data format of the bitmap data</param>
        /// <param name="data">The bitmap data</param>
        /// <param name="borderSize">The size of the border around the image</param>
        /// <param name="width">The width of the bitmap </param>
        /// <param name="rows">The height of the bitmap</param>
        /// <param name="pitch">The pitch of the bitmap</param>
        /// <param name="grayLevels">The number of gray levels of the bitmap</param>
        public CharacterBitmap(IntPtr data, ref Int2 borderSize, int width, int rows, int pitch, int grayLevels, PixelMode pixelMode)
        {
            // add one empty border to each side of the bitmap
            width += 2 * borderSize.X;
            rows += 2 * borderSize.Y;

            buffer = Utilities.AllocateMemory(width * rows, 1);

            if (pixelMode == PixelMode.Mono)
                CopyAndAddBordersFromMono(data, buffer, ref borderSize, width, rows, pitch);
            else
                CopyAndAddBordersFromGrays(data, buffer, ref borderSize, width, rows);

            this.width = width;
            this.rows = rows;
            this.pitch = width;
            this.grayLevels = grayLevels;
            this.pixelMode = pixelMode;
        }

        private static unsafe void CopyAndAddBordersFromGrays(IntPtr data, IntPtr dataBytes, ref Int2 borderSize, int width, int rows)
        {
            var widthLessBorders = width - (borderSize.X << 1);
            var rowsLessBorders = rows - (borderSize.Y << 1);

            var resetBorderLineSize = width * borderSize.Y;
            Utilities.ClearMemory(dataBytes, 0, resetBorderLineSize);
            Utilities.ClearMemory(dataBytes + width * rows - resetBorderLineSize, 0, resetBorderLineSize); // set last border lines to null
            
            var src = (byte*)data;
            var dst = (byte*)dataBytes + resetBorderLineSize;

            // set the middle of the image
            for (int row = 0; row < rowsLessBorders; row++)
            {
                for (int c = 0; c < borderSize.X; c++)
                {
                    *dst = 0;
                    ++dst;
                }

                for (int c = 0; c < widthLessBorders; c++)
                {
                    *dst = *src;

                    ++dst;
                    ++src;
                }

                for (int c = 0; c < borderSize.X; c++)
                {
                    *dst = 0;
                    ++dst;
                }
            }
        }

        private static unsafe void CopyAndAddBordersFromMono(IntPtr data, IntPtr dataBytes, ref Int2 borderSize, int width, int rows, int srcPitch)
        {
            var rowsLessBorders = rows - (borderSize.Y << 1);

            var resetBorderLineSize = width * borderSize.Y;
            Utilities.ClearMemory(dataBytes, 0, resetBorderLineSize); // set first border lines to null 
            Utilities.ClearMemory(dataBytes + rows * width - resetBorderLineSize, 0, resetBorderLineSize); // set last border lines to null

            var rowSrc = (byte*)data;
            var dst = (byte*)dataBytes + resetBorderLineSize;

            // copy each row one by one
            for (int row = 0; row < rowsLessBorders; row++)
            {
                var src = rowSrc;
                var col = 0;

                for (int c = 0; c < borderSize.X; c++)
                    dst[col++] = 0;

                while (true)
                {
                    byte mask = 0x80;
                    for (int k = 0; k < 8; k++)
                    {
                        dst[col] = (*src & mask) != 0 ? byte.MaxValue : (byte)0;
                            
                        mask >>= 1;
                        ++col;

                        if (col >= width - borderSize.X)
                            goto EndRow;
                    }
                    ++src;
                }

            EndRow:
                for (int c = 0; c < borderSize.X; ++c)
                    dst[col++] = 0;

                rowSrc += srcPitch;
                dst += width;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="CharacterBitmap"/> has been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return disposed;
            }
        }

        /// <summary>
        /// Gets the number of bitmap rows.
        /// </summary>
        public int Rows
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("CharacterBitmap", "Cannot access a disposed object.");

                return rows;
            }
        }

        /// <summary>
        /// Gets the number of pixels in bitmap row.
        /// </summary>
        public int Width
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("CharacterBitmap", "Cannot access a disposed object.");

                return width;
            }
        }

        /// <summary><para>
        /// Gets the pitch's absolute value is the number of bytes taken by one bitmap row, including padding. However,
        /// the pitch is positive when the bitmap has a ‘down’ flow, and negative when it has an ‘up’ flow. In all
        /// cases, the pitch is an offset to add to a bitmap pointer in order to go down one row.
        /// </para><para>
        /// Note that ‘padding’ means the alignment of a bitmap to a byte border, and FreeType functions normally align
        /// to the smallest possible integer value.
        /// </para><para>
        /// For the B/W rasterizer, ‘pitch’ is always an even number.
        /// </para></summary>
        public int Pitch
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("CharacterBitmap", "Cannot access a disposed object.");

                return pitch;
            }
        }

        /// <summary>
        /// Gets a typeless pointer to the bitmap buffer. This value should be aligned on 32-bit boundaries in most
        /// cases.
        /// </summary>
        public IntPtr Buffer
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("CharacterBitmap", "Cannot access a disposed object.");

                return buffer;
            }
        }

        /// <summary>
        /// Gets the number of gray levels used in the bitmap. This field is only used with
        /// <see cref="SharpFont.PixelMode.Gray"/>.
        /// </summary>
        public int GrayLevels
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("CharacterBitmap", "Cannot access a disposed object.");

                return grayLevels;
            }
        }

        /// <summary>
        /// Gets the pixel mode, i.e., how pixel bits are stored.
        /// </summary>
        public PixelMode PixelMode
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("CharacterBitmap", "Cannot access a disposed object.");

                return pixelMode;
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            Utilities.FreeMemory(buffer);

            disposed = true;
        }
    }
}
