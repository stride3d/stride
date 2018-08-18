// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
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

using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Defines various types of pixel formats.
    /// </summary>
    [DataContract]
    public enum PixelFormat
    {
        /// <summary>
        /// <dd> <p>The format is not known.</p> </dd>
        /// </summary>
        None = unchecked((int)0),

        /// <summary>
        /// <dd> <p>A four-component, 128-bit typeless format that supports 32 bits per channel including alpha. 1</p> </dd>
        /// </summary>
        R32G32B32A32_Typeless = unchecked((int)1),

        /// <summary>
        /// <dd> <p>A four-component, 128-bit floating-point format that supports 32 bits per channel including alpha. 1</p> </dd>
        /// </summary>
        R32G32B32A32_Float = unchecked((int)2),

        /// <summary>
        /// <dd> <p>A four-component, 128-bit unsigned-integer format that supports 32 bits per channel including alpha. 1</p> </dd>
        /// </summary>
        R32G32B32A32_UInt = unchecked((int)3),

        /// <summary>
        /// <dd> <p>A four-component, 128-bit signed-integer format that supports 32 bits per channel including alpha. 1</p> </dd>
        /// </summary>
        R32G32B32A32_SInt = unchecked((int)4),

        /// <summary>
        /// <dd> <p>A three-component, 96-bit typeless format that supports 32 bits per color channel.</p> </dd>
        /// </summary>
        R32G32B32_Typeless = unchecked((int)5),

        /// <summary>
        /// <dd> <p>A three-component, 96-bit floating-point format that supports 32 bits per color channel.</p> </dd>
        /// </summary>
        R32G32B32_Float = unchecked((int)6),

        /// <summary>
        /// <dd> <p>A three-component, 96-bit unsigned-integer format that supports 32 bits per color channel.</p> </dd>
        /// </summary>
        R32G32B32_UInt = unchecked((int)7),

        /// <summary>
        /// <dd> <p>A three-component, 96-bit signed-integer format that supports 32 bits per color channel.</p> </dd>
        /// </summary>
        R32G32B32_SInt = unchecked((int)8),

        /// <summary>
        /// <dd> <p>A four-component, 64-bit typeless format that supports 16 bits per channel including alpha.</p> </dd>
        /// </summary>
        R16G16B16A16_Typeless = unchecked((int)9),

        /// <summary>
        /// <dd> <p>A four-component, 64-bit floating-point format that supports 16 bits per channel including alpha.</p> </dd>
        /// </summary>
        R16G16B16A16_Float = unchecked((int)10),

        /// <summary>
        /// <dd> <p>A four-component, 64-bit unsigned-normalized-integer format that supports 16 bits per channel including alpha.</p> </dd>
        /// </summary>
        R16G16B16A16_UNorm = unchecked((int)11),

        /// <summary>
        /// <dd> <p>A four-component, 64-bit unsigned-integer format that supports 16 bits per channel including alpha.</p> </dd>
        /// </summary>
        R16G16B16A16_UInt = unchecked((int)12),

        /// <summary>
        /// <dd> <p>A four-component, 64-bit signed-normalized-integer format that supports 16 bits per channel including alpha.</p> </dd>
        /// </summary>
        R16G16B16A16_SNorm = unchecked((int)13),

        /// <summary>
        /// <dd> <p>A four-component, 64-bit signed-integer format that supports 16 bits per channel including alpha.</p> </dd>
        /// </summary>
        R16G16B16A16_SInt = unchecked((int)14),

        /// <summary>
        /// <dd> <p>A two-component, 64-bit typeless format that supports 32 bits for the red channel and 32 bits for the green channel.</p> </dd>
        /// </summary>
        R32G32_Typeless = unchecked((int)15),

        /// <summary>
        /// <dd> <p>A two-component, 64-bit floating-point format that supports 32 bits for the red channel and 32 bits for the green channel.</p> </dd>
        /// </summary>
        R32G32_Float = unchecked((int)16),

        /// <summary>
        /// <dd> <p>A two-component, 64-bit unsigned-integer format that supports 32 bits for the red channel and 32 bits for the green channel.</p> </dd>
        /// </summary>
        R32G32_UInt = unchecked((int)17),

        /// <summary>
        /// <dd> <p>A two-component, 64-bit signed-integer format that supports 32 bits for the red channel and 32 bits for the green channel.</p> </dd>
        /// </summary>
        R32G32_SInt = unchecked((int)18),

        /// <summary>
        /// <dd> <p>A two-component, 64-bit typeless format that supports 32 bits for the red channel, 8 bits for the green channel, and 24 bits are unused.</p> </dd>
        /// </summary>
        R32G8X24_Typeless = unchecked((int)19),

        /// <summary>
        /// <dd> <p>A 32-bit floating-point component, and two unsigned-integer components (with an additional 32 bits). This format supports 32-bit depth, 8-bit stencil, and 24 bits are unused.</p> </dd>
        /// </summary>
        D32_Float_S8X24_UInt = unchecked((int)20),

        /// <summary>
        /// <dd> <p>A 32-bit floating-point component, and two typeless components (with an additional 32 bits). This format supports 32-bit red channel, 8 bits are unused, and 24 bits are unused.</p> </dd>
        /// </summary>
        R32_Float_X8X24_Typeless = unchecked((int)21),

        /// <summary>
        /// <dd> <p>A 32-bit typeless component, and two unsigned-integer components (with an additional 32 bits). This format has 32 bits unused, 8 bits for green channel, and 24 bits are unused.</p> </dd>
        /// </summary>
        X32_Typeless_G8X24_UInt = unchecked((int)22),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit typeless format that supports 10 bits for each color and 2 bits for alpha.</p> </dd>
        /// </summary>
        R10G10B10A2_Typeless = unchecked((int)23),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized-integer format that supports 10 bits for each color and 2 bits for alpha.</p> </dd>
        /// </summary>
        R10G10B10A2_UNorm = unchecked((int)24),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-integer format that supports 10 bits for each color and 2 bits for alpha.</p> </dd>
        /// </summary>
        R10G10B10A2_UInt = unchecked((int)25),

        /// <summary>
        /// <dd> <p>Three partial-precision floating-point numbers encoded into a single 32-bit value (a variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).  There are no sign bits, and there is a 5-bit biased (15) exponent for each channel, 6-bit mantissa  for R and G, and a 5-bit mantissa for B, as shown in the following illustration.</p> <p></p> </dd>
        /// </summary>
        R11G11B10_Float = unchecked((int)26),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit typeless format that supports 8 bits per channel including alpha.</p> </dd>
        /// </summary>
        R8G8B8A8_Typeless = unchecked((int)27),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits per channel including alpha.</p> </dd>
        /// </summary>
        R8G8B8A8_UNorm = unchecked((int)28),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized integer sRGB format that supports 8 bits per channel including alpha.</p> </dd>
        /// </summary>
        R8G8B8A8_UNorm_SRgb = unchecked((int)29),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-integer format that supports 8 bits per channel including alpha.</p> </dd>
        /// </summary>
        R8G8B8A8_UInt = unchecked((int)30),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit signed-normalized-integer format that supports 8 bits per channel including alpha.</p> </dd>
        /// </summary>
        R8G8B8A8_SNorm = unchecked((int)31),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit signed-integer format that supports 8 bits per channel including alpha.</p> </dd>
        /// </summary>
        R8G8B8A8_SInt = unchecked((int)32),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit typeless format that supports 16 bits for the red channel and 16 bits for the green channel.</p> </dd>
        /// </summary>
        R16G16_Typeless = unchecked((int)33),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit floating-point format that supports 16 bits for the red channel and 16 bits for the green channel.</p> </dd>
        /// </summary>
        R16G16_Float = unchecked((int)34),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit unsigned-normalized-integer format that supports 16 bits each for the green and red channels.</p> </dd>
        /// </summary>
        R16G16_UNorm = unchecked((int)35),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit unsigned-integer format that supports 16 bits for the red channel and 16 bits for the green channel.</p> </dd>
        /// </summary>
        R16G16_UInt = unchecked((int)36),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit signed-normalized-integer format that supports 16 bits for the red channel and 16 bits for the green channel.</p> </dd>
        /// </summary>
        R16G16_SNorm = unchecked((int)37),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit signed-integer format that supports 16 bits for the red channel and 16 bits for the green channel.</p> </dd>
        /// </summary>
        R16G16_SInt = unchecked((int)38),

        /// <summary>
        /// <dd> <p>A single-component, 32-bit typeless format that supports 32 bits for the red channel.</p> </dd>
        /// </summary>
        R32_Typeless = unchecked((int)39),

        /// <summary>
        /// <dd> <p>A single-component, 32-bit floating-point format that supports 32 bits for depth.</p> </dd>
        /// </summary>
        D32_Float = unchecked((int)40),

        /// <summary>
        /// <dd> <p>A single-component, 32-bit floating-point format that supports 32 bits for the red channel.</p> </dd>
        /// </summary>
        R32_Float = unchecked((int)41),

        /// <summary>
        /// <dd> <p>A single-component, 32-bit unsigned-integer format that supports 32 bits for the red channel.</p> </dd>
        /// </summary>
        R32_UInt = unchecked((int)42),

        /// <summary>
        /// <dd> <p>A single-component, 32-bit signed-integer format that supports 32 bits for the red channel.</p> </dd>
        /// </summary>
        R32_SInt = unchecked((int)43),

        /// <summary>
        /// <dd> <p>A two-component, 32-bit typeless format that supports 24 bits for the red channel and 8 bits for the green channel.</p> </dd>
        /// </summary>
        R24G8_Typeless = unchecked((int)44),

        /// <summary>
        /// <dd> <p>A 32-bit z-buffer format that supports 24 bits for depth and 8 bits for stencil.</p> </dd>
        /// </summary>
        D24_UNorm_S8_UInt = unchecked((int)45),

        /// <summary>
        /// <dd> <p>A 32-bit format, that contains a 24 bit, single-component, unsigned-normalized integer, with an additional typeless 8 bits. This format has 24 bits red channel and 8 bits unused.</p> </dd>
        /// </summary>
        R24_UNorm_X8_Typeless = unchecked((int)46),

        /// <summary>
        /// <dd> <p>A 32-bit format, that contains a 24 bit, single-component, typeless format,  with an additional 8 bit unsigned integer component. This format has 24 bits unused and 8 bits green channel.</p> </dd>
        /// </summary>
        X24_Typeless_G8_UInt = unchecked((int)47),

        /// <summary>
        /// <dd> <p>A two-component, 16-bit typeless format that supports 8 bits for the red channel and 8 bits for the green channel.</p> </dd>
        /// </summary>
        R8G8_Typeless = unchecked((int)48),

        /// <summary>
        /// <dd> <p>A two-component, 16-bit unsigned-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel.</p> </dd>
        /// </summary>
        R8G8_UNorm = unchecked((int)49),

        /// <summary>
        /// <dd> <p>A two-component, 16-bit unsigned-integer format that supports 8 bits for the red channel and 8 bits for the green channel.</p> </dd>
        /// </summary>
        R8G8_UInt = unchecked((int)50),

        /// <summary>
        /// <dd> <p>A two-component, 16-bit signed-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel.</p> </dd>
        /// </summary>
        R8G8_SNorm = unchecked((int)51),

        /// <summary>
        /// <dd> <p>A two-component, 16-bit signed-integer format that supports 8 bits for the red channel and 8 bits for the green channel.</p> </dd>
        /// </summary>
        R8G8_SInt = unchecked((int)52),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit typeless format that supports 16 bits for the red channel.</p> </dd>
        /// </summary>
        R16_Typeless = unchecked((int)53),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit floating-point format that supports 16 bits for the red channel.</p> </dd>
        /// </summary>
        R16_Float = unchecked((int)54),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for depth.</p> </dd>
        /// </summary>
        D16_UNorm = unchecked((int)55),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for the red channel.</p> </dd>
        /// </summary>
        R16_UNorm = unchecked((int)56),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit unsigned-integer format that supports 16 bits for the red channel.</p> </dd>
        /// </summary>
        R16_UInt = unchecked((int)57),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit signed-normalized-integer format that supports 16 bits for the red channel.</p> </dd>
        /// </summary>
        R16_SNorm = unchecked((int)58),

        /// <summary>
        /// <dd> <p>A single-component, 16-bit signed-integer format that supports 16 bits for the red channel.</p> </dd>
        /// </summary>
        R16_SInt = unchecked((int)59),

        /// <summary>
        /// <dd> <p>A single-component, 8-bit typeless format that supports 8 bits for the red channel.</p> </dd>
        /// </summary>
        R8_Typeless = unchecked((int)60),

        /// <summary>
        /// <dd> <p>A single-component, 8-bit unsigned-normalized-integer format that supports 8 bits for the red channel.</p> </dd>
        /// </summary>
        R8_UNorm = unchecked((int)61),

        /// <summary>
        /// <dd> <p>A single-component, 8-bit unsigned-integer format that supports 8 bits for the red channel.</p> </dd>
        /// </summary>
        R8_UInt = unchecked((int)62),

        /// <summary>
        /// <dd> <p>A single-component, 8-bit signed-normalized-integer format that supports 8 bits for the red channel.</p> </dd>
        /// </summary>
        R8_SNorm = unchecked((int)63),

        /// <summary>
        /// <dd> <p>A single-component, 8-bit signed-integer format that supports 8 bits for the red channel.</p> </dd>
        /// </summary>
        R8_SInt = unchecked((int)64),

        /// <summary>
        /// <dd> <p>A single-component, 8-bit unsigned-normalized-integer format for alpha only.</p> </dd>
        /// </summary>
        A8_UNorm = unchecked((int)65),

        /// <summary>
        /// <dd> <p>A single-component, 1-bit unsigned-normalized integer format that supports 1 bit for the red channel. 2.</p> </dd>
        /// </summary>
        R1_UNorm = unchecked((int)66),

        /// <summary>
        /// <dd> <p>Three partial-precision floating-point numbers encoded into a single 32-bit value all sharing the same 5-bit exponent (variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).  There is no sign bit, and there is a shared 5-bit biased (15) exponent and a 9-bit mantissa for each channel, as shown in the following illustration. 2.</p> <p></p> </dd>
        /// </summary>
        R9G9B9E5_Sharedexp = unchecked((int)67),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized-integer format. This packed RGB format is analogous to the UYVY format. Each 32-bit block describes a pair of pixels: (R8, G8, B8) and (R8, G8, B8) where the R8/B8 values are repeated, and the G8 values are unique to each pixel. 3</p> </dd>
        /// </summary>
        R8G8_B8G8_UNorm = unchecked((int)68),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized-integer format. This packed RGB format is analogous to the YUY2 format. Each 32-bit block describes a pair of pixels: (R8, G8, B8) and (R8, G8, B8) where the R8/B8 values are repeated, and the G8 values are unique to each pixel. 3</p> </dd>
        /// </summary>
        G8R8_G8B8_UNorm = unchecked((int)69),

        /// <summary>
        /// <dd> <p>Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC1_Typeless = unchecked((int)70),

        /// <summary>
        /// <dd> <p>Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC1_UNorm = unchecked((int)71),

        /// <summary>
        /// <dd> <p>Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC1_UNorm_SRgb = unchecked((int)72),

        /// <summary>
        /// <dd> <p>Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC2_Typeless = unchecked((int)73),

        /// <summary>
        /// <dd> <p>Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC2_UNorm = unchecked((int)74),

        /// <summary>
        /// <dd> <p>Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC2_UNorm_SRgb = unchecked((int)75),

        /// <summary>
        /// <dd> <p>Four-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC3_Typeless = unchecked((int)76),

        /// <summary>
        /// <dd> <p>Four-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC3_UNorm = unchecked((int)77),

        /// <summary>
        /// <dd> <p>Four-component block-compression format for sRGB data. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC3_UNorm_SRgb = unchecked((int)78),

        /// <summary>
        /// <dd> <p>One-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC4_Typeless = unchecked((int)79),

        /// <summary>
        /// <dd> <p>One-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC4_UNorm = unchecked((int)80),

        /// <summary>
        /// <dd> <p>One-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC4_SNorm = unchecked((int)81),

        /// <summary>
        /// <dd> <p>Two-component typeless block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC5_Typeless = unchecked((int)82),

        /// <summary>
        /// <dd> <p>Two-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC5_UNorm = unchecked((int)83),

        /// <summary>
        /// <dd> <p>Two-component block-compression format. For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC5_SNorm = unchecked((int)84),

        /// <summary>
        /// <dd> <p>A three-component, 16-bit unsigned-normalized-integer format that supports 5 bits for blue, 6 bits for green, and 5 bits for red.</p> </dd>
        /// </summary>
        B5G6R5_UNorm = unchecked((int)85),

        /// <summary>
        /// <dd> <p>A four-component, 16-bit unsigned-normalized-integer format that supports 5 bits for each color channel and 1-bit alpha.</p> </dd>
        /// </summary>
        B5G5R5A1_UNorm = unchecked((int)86),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8-bit alpha.</p> </dd>
        /// </summary>
        B8G8R8A8_UNorm = unchecked((int)87),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8 bits unused.</p> </dd>
        /// </summary>
        B8G8R8X8_UNorm = unchecked((int)88),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit 2.8-biased fixed-point format that supports 10 bits for each color channel and 2-bit alpha.</p> </dd>
        /// </summary>
        R10G10B10_Xr_Bias_A2_UNorm = unchecked((int)89),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit typeless format that supports 8 bits for each channel including alpha. 4</p> </dd>
        /// </summary>
        B8G8R8A8_Typeless = unchecked((int)90),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each channel including alpha. 4</p> </dd>
        /// </summary>
        B8G8R8A8_UNorm_SRgb = unchecked((int)91),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit typeless format that supports 8 bits for each color channel, and 8 bits are unused. 4</p> </dd>
        /// </summary>
        B8G8R8X8_Typeless = unchecked((int)92),

        /// <summary>
        /// <dd> <p>A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each color channel, and 8 bits are unused. 4</p> </dd>
        /// </summary>
        B8G8R8X8_UNorm_SRgb = unchecked((int)93),

        /// <summary>
        /// <dd> <p>A typeless block-compression format. 4 For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC6H_Typeless = unchecked((int)94),

        /// <summary>
        /// <dd> <p>A block-compression format. 4 For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC6H_Uf16 = unchecked((int)95),

        /// <summary>
        /// <dd> <p>A block-compression format. 4 For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC6H_Sf16 = unchecked((int)96),

        /// <summary>
        /// <dd> <p>A typeless block-compression format. 4 For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC7_Typeless = unchecked((int)97),

        /// <summary>
        /// <dd> <p>A block-compression format. 4 For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC7_UNorm = unchecked((int)98),

        /// <summary>
        /// <dd> <p>A block-compression format. 4 For information about block-compression formats, see Texture Block Compression in Direct3D 11.</p> </dd>
        /// </summary>
        BC7_UNorm_SRgb = unchecked((int)99),

        /// <summary>
        /// <dd> <p>A block-compression format. For information about block-compression formats, see Texture Block Compression in PowerVC Texture Compression.</p> </dd>
        /// </summary>

        PVRTC_2bpp_RGB = unchecked((int)1024),
        PVRTC_2bpp_RGBA = unchecked((int)1025),
        PVRTC_4bpp_RGB = unchecked((int)1026),
        PVRTC_4bpp_RGBA = unchecked((int)1027),
        PVRTC_II_2bpp = unchecked((int)1028),
        PVRTC_II_4bpp = unchecked((int)1029),

        PVRTC_2bpp_RGB_SRgb = unchecked((int)1030),
        PVRTC_2bpp_RGBA_SRgb = unchecked((int)1031),
        PVRTC_4bpp_RGB_SRgb = unchecked((int)1032),
        PVRTC_4bpp_RGBA_SRgb = unchecked((int)1033),

        ETC1 = unchecked((int)1088),
        ETC2_RGB = unchecked((int)1089),
        ETC2_RGBA = unchecked((int)1090),
        ETC2_RGB_A1 = unchecked((int)1091),
        EAC_R11_Unsigned = unchecked((int)1092),
        EAC_R11_Signed = unchecked((int)1093),
        EAC_RG11_Unsigned = unchecked((int)1094),
        EAC_RG11_Signed = unchecked((int)1095),
        ETC2_RGBA_SRgb = unchecked((int)1096),
        ETC2_RGB_SRgb = unchecked((int)1097),

        ATC_RGB = unchecked((int)1120),
        ATC_RGBA_Explicit = unchecked((int)1121),
        ATC_RGBA_Interpolated = unchecked((int)1122),
    }
}
