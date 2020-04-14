// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2011 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System.Runtime.InteropServices;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// Helper class to perform Half/Float conversion.
    /// Code extract from paper : www.fox-toolkit.org/ftp/fasthalffloatconversion.pdf by Jeroen van der Zijp
    /// </summary>
    internal class HalfUtils
    {
        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        private struct FloatToUint
        {
            [FieldOffset(0)]
            public uint UIntValue;
            [FieldOffset(0)]
            public float FloatValue;
        }

        /// <summary>
        /// Unpacks the specified h.
        /// </summary>
        /// <param name="h">The packed value.</param>
        /// <returns>The float representation of the packed value.</returns>
        public static float Unpack(ushort h)
        {
            var conv = new FloatToUint();
            conv.UIntValue = HalfToFloatMantissaTable[HalfToFloatOffsetTable[h >> 10] + (((uint)h) & 0x3ff)] + HalfToFloatExponentTable[h >> 10];
            return conv.FloatValue;
        }

        /// <summary>
        /// Packs the specified f.
        /// </summary>
        /// <param name="f">The float value.</param>
        /// <returns>The packed representation of the float value.</returns>
        public static ushort Pack(float f)
        {
            FloatToUint conv = new FloatToUint();
            conv.FloatValue = f;
            return (ushort)(FloatToHalfBaseTable[(conv.UIntValue >> 23) & 0x1ff] + ((conv.UIntValue & 0x007fffff) >> FloatToHalfShiftTable[(conv.UIntValue >> 23) & 0x1ff]));
        }

        private static readonly uint[] HalfToFloatMantissaTable = new uint[2048];
        private static readonly uint[] HalfToFloatExponentTable = new uint[64];
        private static readonly uint[] HalfToFloatOffsetTable = new uint[64];
        private static readonly ushort[] FloatToHalfBaseTable = new ushort[512];
        private static readonly byte[] FloatToHalfShiftTable = new byte[512];

        static HalfUtils()
        {
            int i;

            // -------------------------------------------------------------------
            // Half to Float tables
            // -------------------------------------------------------------------

            // Mantissa table

            // 0 => 0
            HalfToFloatMantissaTable[0] = 0;

            // Transform subnormal to normalized
            for (i = 1; i < 1024; i++)
            {
                uint m = ((uint)i) << 13;
                uint e = 0;

                while ((m & 0x00800000) == 0)
                {
                    e -= 0x00800000;
                    m <<= 1;
                }
                m &= ~0x00800000U;
                e += 0x38800000;
                HalfToFloatMantissaTable[i] = m | e;
            }

            // Normal case
            for (i = 1024; i < 2048; i++)
                HalfToFloatMantissaTable[i] = 0x38000000 + (((uint)(i - 1024)) << 13);

            // Exponent table

            // 0 => 0
            HalfToFloatExponentTable[0] = 0;

            for (i = 1; i < 63; i++)
            {
                if (i < 31) // Positive Numbers
                    HalfToFloatExponentTable[i] = ((uint)i) << 23;
                else // Negative Numbers
                    HalfToFloatExponentTable[i] = 0x80000000 + (((uint)(i - 32)) << 23);
            }
            HalfToFloatExponentTable[31] = 0x47800000;
            HalfToFloatExponentTable[32] = 0x80000000;
            HalfToFloatExponentTable[63] = 0xC7800000;

            // Offset table
            HalfToFloatOffsetTable[0] = 0;
            for (i = 1; i < 64; i++)
                HalfToFloatOffsetTable[i] = 1024;
            HalfToFloatOffsetTable[32] = 0;

            // -------------------------------------------------------------------
            // Float to Half tables
            // -------------------------------------------------------------------
       
            for (i = 0; i < 256; i++)
            {
                int e = i - 127;
                if (e < -24)
                { // Very small numbers map to zero
                    FloatToHalfBaseTable[i | 0x000] = 0x0000;
                    FloatToHalfBaseTable[i | 0x100] = 0x8000;
                    FloatToHalfShiftTable[i | 0x000] = 24;
                    FloatToHalfShiftTable[i | 0x100] = 24;
                }
                else if (e < -14)
                { // Small numbers map to denorms
                    FloatToHalfBaseTable[i | 0x000] = (ushort)((0x0400 >> (-e - 14)));
                    FloatToHalfBaseTable[i | 0x100] = (ushort)((0x0400 >> (-e - 14)) | 0x8000);
                    FloatToHalfShiftTable[i | 0x000] = (byte)(-e - 1);
                    FloatToHalfShiftTable[i | 0x100] = (byte)(-e - 1);
                }
                else if (e <= 15)
                { // Normal numbers just lose precision
                    FloatToHalfBaseTable[i | 0x000] = (ushort)(((e + 15) << 10));
                    FloatToHalfBaseTable[i | 0x100] = (ushort)(((e + 15) << 10) | 0x8000);
                    FloatToHalfShiftTable[i | 0x000] = 13;
                    FloatToHalfShiftTable[i | 0x100] = 13;
                }
                else if (e < 128)
                { // Large numbers map to Infinity
                    FloatToHalfBaseTable[i | 0x000] = 0x7C00;
                    FloatToHalfBaseTable[i | 0x100] = 0xFC00;
                    FloatToHalfShiftTable[i | 0x000] = 24;
                    FloatToHalfShiftTable[i | 0x100] = 24;
                }
                else
                { // Infinity and NaN's stay Infinity and NaN's
                    FloatToHalfBaseTable[i | 0x000] = 0x7C00;
                    FloatToHalfBaseTable[i | 0x100] = 0xFC00;
                    FloatToHalfShiftTable[i | 0x000] = 13;
                    FloatToHalfShiftTable[i | 0x100] = 13;
                }
            }
        }
    }
}
