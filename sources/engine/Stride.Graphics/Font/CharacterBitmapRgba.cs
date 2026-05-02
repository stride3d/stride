// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Data;
using Stride.Core;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// An RGBA bitmap representing a glyph (4 bytes per pixel).
    /// Intended for runtime MSDF font (stored in RGB, alpha optional).
    /// </summary>
    internal sealed class CharacterBitmapRgba : IDisposable
    {
        private readonly int width;
        private readonly int rows;
        private readonly int pitch;
        private readonly IntPtr buffer;

        private bool disposed;

        /// <summary>
        /// Initializes a null bitmap.
        /// </summary>
        public CharacterBitmapRgba()
        {
        }

        /// <summary>
        /// Allocates an RGBA bitmap (uninitialized).
        /// </summary>
        public CharacterBitmapRgba(int width, int rows)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (rows < 0) throw new ArgumentOutOfRangeException(nameof(rows));

            this.width = width;
            this.rows = rows;
            pitch = checked(width * 4);

            if (width != 0 && rows != 0)
            {
                buffer = MemoryUtilities.Allocate(checked(pitch * rows), 1);
            }
        }

        /// <summary>
        /// Allocates an RGBA bitmap and copies data from a source buffer with the given pitch.
        /// </summary>
        public unsafe CharacterBitmapRgba(IntPtr srcRgba, int width, int rows, int srcPitchBytes)
            : this(width, rows)
        {
            if (srcRgba == IntPtr.Zero && (width != 0 || rows != 0))
                throw new ArgumentNullException(nameof(srcRgba));
            if (srcPitchBytes < 0) throw new ArgumentOutOfRangeException(nameof(srcPitchBytes));

            if (buffer == IntPtr.Zero)
                return;

            var src = (byte*)srcRgba;
            var dst = (byte*)buffer;

            // Copy row-by-row to handle pitch differences.
            var copyBytesPerRow = Math.Min(srcPitchBytes, pitch);
            for (int y = 0; y < rows; y++)
            {
                var srcRow = src + y * srcPitchBytes;
                var dstRow = dst + y * pitch;

                MemoryUtilities.CopyWithAlignmentFallback(dstRow, srcRow, (uint)copyBytesPerRow);

                if (copyBytesPerRow < pitch)
                {
                    MemoryUtilities.Clear(dstRow + copyBytesPerRow, (uint)(pitch - copyBytesPerRow));
                }
            }
        }

        public bool IsDisposed => disposed;

        public int Width
        {
            get
            {
                ThrowIfDisposed();
                return width;
            }
        }

        public int Rows
        {
            get
            {
                ThrowIfDisposed();
                return rows;
            }
        }

        public int Pitch
        {
            get
            {
                ThrowIfDisposed();
                return pitch;
            }
        }

        public IntPtr Buffer
        {
            get
            {
                ThrowIfDisposed();
                return buffer;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (buffer != IntPtr.Zero)
                MemoryUtilities.Free(buffer);

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CharacterBitmapRgba), "Cannot access a disposed object.");
        }
    }
}
