// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Stride.Video
{
    /// <summary>
    /// Represents an image extracted from a video.
    /// </summary>
    public sealed class VideoImage : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoImage"/> class.
        /// </summary>
        public VideoImage(int width, int height, int bufferSize)
        {
            Buffer = Marshal.AllocHGlobal(bufferSize);
            BufferSize = bufferSize;
            Height = height;
            Width = width;
        }

        /// <summary>
        /// Buffer to the image data.
        /// </summary>
        public IntPtr Buffer { get; }

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        public int BufferSize { get; }

        /// <summary>
        /// Image height.
        /// </summary>
        public int Height { get; }

        public int Linesize { get; set; }

        public long Timestamp { get; set; }

        /// <summary>
        /// Image width.
        /// </summary>
        public int Width { get; }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Buffer);
        }
    }
}
