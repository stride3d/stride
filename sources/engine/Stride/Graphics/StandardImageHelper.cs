// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Buffers.Binary;
using System.Numerics;

namespace Stride.Graphics
{
    /// <summary>
    /// This class is responsible to provide image loader for png, gif, bmp.
    /// </summary>
    partial class StandardImageHelper
    {
        private static unsafe void CopyMemoryBGRA(IntPtr dest, IntPtr src, int sizeInBytesToCopy)
        {
            if ((sizeInBytesToCopy & 3) != 0)
                throw new ArgumentException("Should be a multiple of 4.", "sizeInBytesToCopy");

            var bufferSize = sizeInBytesToCopy / 4;
            var srcPtr = (uint*)src;
            var destPtr = (uint*)dest;
            for (int i = 0; i < bufferSize; ++i)
            {
                var value = *srcPtr++;
                // value: 0xAARRGGBB or in reverse 0xAABBGGRR
                value = BinaryPrimitives.ReverseEndianness(value);
                // value: 0xBBGGRRAA or in reverse 0xRRGGBBAA
                value = BitOperations.RotateRight(value, 8);
                // value: 0xAABBGGRR or in reverse 0xAARRGGBB
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
                value = 0xFF000000u | (value * 0x010101u);
                *destPtr++ = value;
            }
        }
    }
}
