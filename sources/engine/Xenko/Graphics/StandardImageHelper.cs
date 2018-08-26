// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Graphics
{
    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// </summary>
    partial class StandardImageHelper
    {
        private static unsafe void CopyMemoryBGRA(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
        {
            if (sizeInBytesToCopy % 4 != 0)
                throw new ArgumentException("Should be a multiple of 4.", "sizeInBytesToCopy");

            var bufferSize = sizeInBytesToCopy / 4;
            var srcPtr = (uint*)src;
            var destPtr = (uint*)dest;
            for (int i = 0; i < bufferSize; ++i)
            {
                var value = *srcPtr++;
                // BGRA => RGBA
                value = (value & 0xFF000000) | ((value & 0xFF0000) >> 16) | (value & 0x00FF00) | ((value & 0x0000FF) << 16);
                *destPtr++ = value;
            }
        }

        private static unsafe void CopyMemoryRRR1(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
        {
            var bufferSize = sizeInBytesToCopy;
            var srcPtr = (byte*)src;
            var destPtr = (uint*)dest;
            for (int i = 0; i < bufferSize; ++i)
            {
                uint value = *srcPtr++;
                // R => RGBA
                value = (0xFF000000) | ((value) << 8) | (value) | ((value) << 16);
                *destPtr++ = value;
            }
        }
    }
}
