// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Stride.Core;

namespace Stride.Shaders.Compiler.Direct3D
{
    // Source: https://github.com/microsoft/hlsl-specs/blob/main/proposals/infra/INF-0004-validator-hashing.md
    internal static class DxilHash
    {
        const byte S11 = 7;
        const byte S12 = 12;
        const byte S13 = 17;
        const byte S14 = 22;
        const byte S21 = 5;
        const byte S22 = 9;
        const byte S23 = 14;
        const byte S24 = 20;
        const byte S31 = 4;
        const byte S32 = 11;
        const byte S33 = 16;
        const byte S34 = 23;
        const byte S41 = 6;
        const byte S42 = 10;
        const byte S43 = 15;
        const byte S44 = 21;

        static void FF(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
        {
            a += ((b & c) | (~b & d)) + x + ac;
            a = ((a << s) | (a >> (32 - s))) + b;
        }

        static void GG(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
        {
            a += ((b & d) | (c & ~d)) + x + ac;
            a = ((a << s) | (a >> (32 - s))) + b;
        }

        static void HH(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
        {
            a += (b ^ c ^ d) + x + ac;
            a = ((a << s) | (a >> (32 - s))) + b;
        }

        static void II(ref uint a, uint b, uint c, uint d, uint x, byte s, uint ac)
        {
            a += (c ^ (b | ~d)) + x + ac;
            a = ((a << s) | (a >> (32 - s))) + b;
        }

        public static unsafe void ComputeHashRetail(byte* pData, uint byteCount, byte* pOutHash)
        {
            var padding = stackalloc byte[] {
                0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            uint leftOver = byteCount & 0x3f;
            uint padAmount;
            bool bTwoRowsPadding = false;
            if (leftOver < 56)
            {
                padAmount = 56 - leftOver;
            }
            else
            {
                padAmount = 120 - leftOver;
                bTwoRowsPadding = true;
            }
            uint padAmountPlusSize = padAmount + 8;
            var state = stackalloc uint[] { 0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476 };
            uint N = (byteCount + padAmountPlusSize) >> 6;
            uint offset = 0;
            uint NextEndState = bTwoRowsPadding ? N - 2 : N - 1;
            byte* pCurrData = pData;
            var x = stackalloc uint[16];
            for (uint i = 0; i < N; i++, offset += 64, pCurrData += 64)
            {
                uint* pX;
                if (i == NextEndState)
                {
                    if (!bTwoRowsPadding && i == N - 1)
                    {
                        uint remainder = byteCount - offset;
                        x[0] = byteCount << 3;

                        Debug.Assert(byteCount - offset <= byteCount); // check for underflow
                        Debug.Assert(pCurrData + remainder == pData + byteCount);
                        Buffer.MemoryCopy(pCurrData, (byte*)x + 4, remainder, remainder); // could copy nothing
                        Buffer.MemoryCopy(padding, (byte*)x + 4 + remainder, padAmount, padAmount);
                        x[15] = 1 | (byteCount << 1);
                    }
                    else if (bTwoRowsPadding)
                    {
                        if (i == N - 2)
                        {
                            uint remainder = byteCount - offset;

                            Debug.Assert(byteCount - offset <= byteCount); // check for underflow
                            Debug.Assert(pCurrData + remainder == pData + byteCount);
                            Buffer.MemoryCopy(pCurrData, x, remainder, remainder);
                            Buffer.MemoryCopy(padding, (byte*)x + remainder, padAmount - 56, padAmount - 56);
                            NextEndState = N - 1;
                        }
                        else if (i == N - 1)
                        {
                            x[0] = byteCount << 3;
                            Buffer.MemoryCopy(padding + padAmount - 56, (byte*)x + 4, 56, 56);
                            x[15] = 1 | (byteCount << 1);
                        }
                    }
                    pX = x;
                }
                else
                {
                    Debug.Assert(pCurrData + 64 <= pData + byteCount);
                    pX = (uint*)pCurrData;
                }

                uint a = state[0];
                uint b = state[1];
                uint c = state[2];
                uint d = state[3];

                /* Round 1 */
                FF(ref a, b, c, d, pX[0], S11, 0xd76aa478); /* 1 */
                FF(ref d, a, b, c, pX[1], S12, 0xe8c7b756); /* 2 */
                FF(ref c, d, a, b, pX[2], S13, 0x242070db); /* 3 */
                FF(ref b, c, d, a, pX[3], S14, 0xc1bdceee); /* 4 */
                FF(ref a, b, c, d, pX[4], S11, 0xf57c0faf); /* 5 */
                FF(ref d, a, b, c, pX[5], S12, 0x4787c62a); /* 6 */
                FF(ref c, d, a, b, pX[6], S13, 0xa8304613); /* 7 */
                FF(ref b, c, d, a, pX[7], S14, 0xfd469501); /* 8 */
                FF(ref a, b, c, d, pX[8], S11, 0x698098d8); /* 9 */
                FF(ref d, a, b, c, pX[9], S12, 0x8b44f7af); /* 10 */
                FF(ref c, d, a, b, pX[10], S13, 0xffff5bb1); /* 11 */
                FF(ref b, c, d, a, pX[11], S14, 0x895cd7be); /* 12 */
                FF(ref a, b, c, d, pX[12], S11, 0x6b901122); /* 13 */
                FF(ref d, a, b, c, pX[13], S12, 0xfd987193); /* 14 */
                FF(ref c, d, a, b, pX[14], S13, 0xa679438e); /* 15 */
                FF(ref b, c, d, a, pX[15], S14, 0x49b40821); /* 16 */

                /* Round 2 */
                GG(ref a, b, c, d, pX[1], S21, 0xf61e2562); /* 17 */
                GG(ref d, a, b, c, pX[6], S22, 0xc040b340); /* 18 */
                GG(ref c, d, a, b, pX[11], S23, 0x265e5a51); /* 19 */
                GG(ref b, c, d, a, pX[0], S24, 0xe9b6c7aa); /* 20 */
                GG(ref a, b, c, d, pX[5], S21, 0xd62f105d); /* 21 */
                GG(ref d, a, b, c, pX[10], S22, 0x2441453); /* 22 */
                GG(ref c, d, a, b, pX[15], S23, 0xd8a1e681); /* 23 */
                GG(ref b, c, d, a, pX[4], S24, 0xe7d3fbc8); /* 24 */
                GG(ref a, b, c, d, pX[9], S21, 0x21e1cde6); /* 25 */
                GG(ref d, a, b, c, pX[14], S22, 0xc33707d6); /* 26 */
                GG(ref c, d, a, b, pX[3], S23, 0xf4d50d87); /* 27 */
                GG(ref b, c, d, a, pX[8], S24, 0x455a14ed); /* 28 */
                GG(ref a, b, c, d, pX[13], S21, 0xa9e3e905); /* 29 */
                GG(ref d, a, b, c, pX[2], S22, 0xfcefa3f8); /* 30 */
                GG(ref c, d, a, b, pX[7], S23, 0x676f02d9); /* 31 */
                GG(ref b, c, d, a, pX[12], S24, 0x8d2a4c8a); /* 32 */

                /* Round 3 */
                HH(ref a, b, c, d, pX[5], S31, 0xfffa3942); /* 33 */
                HH(ref d, a, b, c, pX[8], S32, 0x8771f681); /* 34 */
                HH(ref c, d, a, b, pX[11], S33, 0x6d9d6122); /* 35 */
                HH(ref b, c, d, a, pX[14], S34, 0xfde5380c); /* 36 */
                HH(ref a, b, c, d, pX[1], S31, 0xa4beea44); /* 37 */
                HH(ref d, a, b, c, pX[4], S32, 0x4bdecfa9); /* 38 */
                HH(ref c, d, a, b, pX[7], S33, 0xf6bb4b60); /* 39 */
                HH(ref b, c, d, a, pX[10], S34, 0xbebfbc70); /* 40 */
                HH(ref a, b, c, d, pX[13], S31, 0x289b7ec6); /* 41 */
                HH(ref d, a, b, c, pX[0], S32, 0xeaa127fa); /* 42 */
                HH(ref c, d, a, b, pX[3], S33, 0xd4ef3085); /* 43 */
                HH(ref b, c, d, a, pX[6], S34, 0x4881d05); /* 44 */
                HH(ref a, b, c, d, pX[9], S31, 0xd9d4d039); /* 45 */
                HH(ref d, a, b, c, pX[12], S32, 0xe6db99e5); /* 46 */
                HH(ref c, d, a, b, pX[15], S33, 0x1fa27cf8); /* 47 */
                HH(ref b, c, d, a, pX[2], S34, 0xc4ac5665); /* 48 */

                /* Round 4 */
                II(ref a, b, c, d, pX[0], S41, 0xf4292244); /* 49 */
                II(ref d, a, b, c, pX[7], S42, 0x432aff97); /* 50 */
                II(ref c, d, a, b, pX[14], S43, 0xab9423a7); /* 51 */
                II(ref b, c, d, a, pX[5], S44, 0xfc93a039); /* 52 */
                II(ref a, b, c, d, pX[12], S41, 0x655b59c3); /* 53 */
                II(ref d, a, b, c, pX[3], S42, 0x8f0ccc92); /* 54 */
                II(ref c, d, a, b, pX[10], S43, 0xffeff47d); /* 55 */
                II(ref b, c, d, a, pX[1], S44, 0x85845dd1); /* 56 */
                II(ref a, b, c, d, pX[8], S41, 0x6fa87e4f); /* 57 */
                II(ref d, a, b, c, pX[15], S42, 0xfe2ce6e0); /* 58 */
                II(ref c, d, a, b, pX[6], S43, 0xa3014314); /* 59 */
                II(ref b, c, d, a, pX[13], S44, 0x4e0811a1); /* 60 */
                II(ref a, b, c, d, pX[4], S41, 0xf7537e82); /* 61 */
                II(ref d, a, b, c, pX[11], S42, 0xbd3af235); /* 62 */
                II(ref c, d, a, b, pX[2], S43, 0x2ad7d2bb); /* 63 */
                II(ref b, c, d, a, pX[9], S44, 0xeb86d391); /* 64 */

                state[0] += a;
                state[1] += b;
                state[2] += c;
                state[3] += d;
            }

            Buffer.MemoryCopy(&state[0], pOutHash, 16, 16);
        }
    }
}
