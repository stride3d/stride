// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Defines various types of pixel formats.
/// </summary>
/// <remarks>
///   <para>
///     Pixel formats describe the layout and type of data stored in a pixel, including the number of channels, bits per channel,
///     and whether the data is normalized or typeless.
///   </para>
///   The suffixes in the names indicate the type of data:
///   <list type="table">
///     <item>
///       <term>UNorm (Unsigned Normalized)</term>
///       <description>
///         Values are stored as <strong>unsigned integers</strong>, but <strong>interpreted as floating-point values between <c>0.0</c> and <c>1.0</c></strong>.
///         For example, <see cref="R8G8B8A8_UNorm"/> stores 8-bit values per channel, where 0 maps to 0.0 and 255 maps to 1.0.
///         <br/>
///         Common for color Textures and Render Targets.
///       </description>
///     </item>
///     <item>
///       <term>SNorm (Signed Normalized)</term>
///       <description>
///         Values are stored as <strong>signed integers</strong>, but <strong>interpreted as floating-point values between <c>-1.0</c> and <c>1.0</c></strong>.
///         For example, <see cref="R8G8_SNorm"/> stores 8-bit values per channel, where -128 to -1.0 and 127 to ~1.0.
///         <br/>
///         Useful for storing normals or vectors.
///       </description>
///     </item>
///     <item>
///       <term>Typeless</term>
///       <description>
///         The format is not fully defined —it’s <strong>just a memory layout</strong>.
///         <br/>
///         Multiple Views (e.g., Shader Resource Views, Render Target Views) can be created over a Graphics Resource with a typeless format
///         with different interpretations. For example, <see cref="R8G8B8A8_Typeless"/> can be viewed as <c>UNorm</c>, <c>UNorm_SRgb</c>,
///         or <c>UInt</c>, depending on how the resource is bound.
///         <br/>
///         Great for flexibility, like rendering in linear space and sampling in sRGB.
///       </description>
///     </item>
///     <item>
///       <term>UInt / SInt</term>
///       <description>
///         <strong>Unsigned (<c>UInt</c>) or signed (<c>SInt</c>) integer values. No normalization</strong> —values are used as-is.
///         <br/>
///         Often used for IDs, masks, or counters.
///       </description>
///     </item>
///     <item>
///       <term>Float</term>
///       <description>
///         Stores values as <strong>IEEE floating-point numbers</strong>.
///         <br/>
///         Used when precision and range are important, like Depth Buffers or HDR color.
///       </description>
///     </item>
///     <item>
///       <term>SRgb</term>
///       <description>
///         Stores <strong>color data</strong> in the <strong>sRGB color space</strong>.
///         When sampling, the GPU automatically converts it to linear space, and when writing the data, it converts it back to sRGB
///         (applies <em>gamma correction</em>).
///         <br/>
///         It is typically used for textures that represent colors.
///       </description>
///     </item>
///   </list>
/// </remarks>
[DataContract]
public enum PixelFormat
{
    /// <summary>
    ///   The format is not known.
    /// </summary>
    None = 0,


    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit format that supports <strong>32 bits per channel</strong> including alpha.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R32G32B32A32_Typeless = 1,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>floating-point format</strong> that supports <strong>32 bits per channel</strong> including alpha.
    /// </summary>
    R32G32B32A32_Float = 2,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>unsigned integer format</strong> that supports <strong>32 bits per channel</strong> including alpha.
    /// </summary>
    R32G32B32A32_UInt = 3,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>signed integer format</strong> that supports <strong>32 bits per channel</strong> including alpha.
    /// </summary>
    R32G32B32A32_SInt = 4,


    /// <summary>
    ///   A <strong>three-component</strong>, 96-bit format that supports <strong>32 bits per channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R32G32B32_Typeless = 5,

    /// <summary>
    ///   A <strong>three-component</strong>, 96-bit <strong>floating-point format</strong> that supports <strong>32 bits per channel</strong>.
    /// </summary>
    R32G32B32_Float = 6,

    /// <summary>
    ///   A <strong>three-component</strong>, 96-bit <strong>unsigned integer format</strong> that supports <strong>32 bits per channel</strong>.
    /// </summary>
    R32G32B32_UInt = 7,

    /// <summary>
    ///   A <strong>three-component</strong>, 96-bit <strong>signed integer format</strong> that supports <strong>32 bits per channel</strong>.
    /// </summary>
    R32G32B32_SInt = 8,


    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit format that supports <strong>16 bits per channel</strong> including alpha.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R16G16B16A16_Typeless = 9,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>floating-point format</strong> that supports <strong>16 bits per channel</strong> including alpha.
    /// </summary>
    R16G16B16A16_Float = 10,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>unsigned normalized integer</strong> format that supports <strong>16 bits per channel</strong> including alpha.
    /// </summary>
    R16G16B16A16_UNorm = 11,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>unsigned integer format</strong> that supports <strong>16 bits per channel</strong> including alpha.
    /// </summary>
    R16G16B16A16_UInt = 12,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>signed normalized integer</strong> format that supports <strong>16 bits per channel</strong> including alpha.
    /// </summary>
    R16G16B16A16_SNorm = 13,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>signed integer format</strong> that supports <strong>16 bits per channel</strong> including alpha.
    /// </summary>
    R16G16B16A16_SInt = 14,


    /// <summary>
    ///   A <strong>two-component</strong>, 64-bit format that supports <strong>32 bits for the red channel and 32 bits for the green channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R32G32_Typeless = 15,

    /// <summary>
    ///   A <strong>two-component</strong>, 64-bit <strong>floating-point format</strong> that supports <strong>32 bits for the red channel and 32 bits for the green channel</strong>.
    /// </summary>
    R32G32_Float = 16,

    /// <summary>
    ///   A <strong>two-component</strong>, 64-bit <strong>unsigned integer format</strong> that supports <strong>32 bits for the red channel and 32 bits for the green channel</strong>.
    /// </summary>
    R32G32_UInt = 17,

    /// <summary>
    ///   A <strong>two-component</strong>, 64-bit <strong>signed integer format</strong> that supports <strong>32 bits for the red channel and 32 bits for the green channel</strong>.
    /// </summary>
    R32G32_SInt = 18,


    /// <summary>
    ///   A <strong>two-component</strong>, 64-bit format that supports <strong>32 bits for the red channel, 8 bits for the green channel, and 24 bits are unused</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R32G8X24_Typeless = 19,

    /// <summary>
    ///   A composite format consisting of a <strong>32-bit floating-point component</strong> intended for <strong>depth</strong>, plus a <strong>8-bit unsigned-integer component</strong>
    ///   intended for <strong>stencil values</strong> in an additional 32-bit part where the last 24 bits are unused (for padding).
    /// </summary>
    D32_Float_S8X24_UInt = 20,

    /// <summary>
    ///   A composite format consisting of a <strong>32-bit floating-point component for the red channel</strong>, and <strong>two typeless components (8-bit and 24-bit respectively)</strong>
    ///   in an additional 32-bit part.
    ///   <br/>
    ///   This is commonly used to create Views for Depth-Stencil Buffers where a shader needs to access the depth.
    ///   <br/>
    ///   This format has <strong>typeless</strong> components, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R32_Float_X8X24_Typeless = 21,

    /// <summary>
    ///   A composite format consisting of an <strong>unused 32-bit typeless component</strong>, plus an <strong>8-bit unsigned-integer component</strong>
    ///   in an additional 32-bit part where the last 24 bits are unused (for padding).
    ///   <br/>
    ///   This is commonly used to create Views for Depth-Stencil Buffers where a shader needs to access the stencil values.
    ///   <br/>
    ///   This format has <strong>typeless</strong> components, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    X32_Typeless_G8X24_UInt = 22,


    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit format that supports <strong>10 bits for each color and 2 bits for alpha</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R10G10B10A2_Typeless = 23,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>10 bits for each color and 2 bits for alpha</strong>.
    /// </summary>
    R10G10B10A2_UNorm = 24,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned integer format</strong> that supports <strong>10 bits for each color and 2 bits for alpha</strong>.
    /// </summary>
    R10G10B10A2_UInt = 25,

    /// <summary>
    ///   A <strong>three-component</strong>, 32-bit <strong>partial-precision floating-point format</strong> that supports
    ///   <strong>11 bits for the red and green channels and 10 bits for the blue channel</strong>.
    ///   <br/>
    ///   It uses a variant of <c>s10e5</c> (sign bit, 10-bit mantissa, and 5-bit biased (15) exponent), but there are no sign bits, and there is a 5-bit biased (15) exponent
    ///   for each channel, 6-bit mantissa for R and G, and a 5-bit mantissa for B.
    /// </summary>
    R11G11B10_Float = 26,


    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit format that supports <strong>8 bits per channel including alpha</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R8G8B8A8_Typeless = 27,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits per channel including alpha</strong>.
    /// </summary>
    R8G8B8A8_UNorm = 28,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits per channel including alpha</strong>,
    ///   where the data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    /// </summary>
    R8G8B8A8_UNorm_SRgb = 29,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned integer format</strong> that supports <strong>8 bits per channel including alpha</strong>.
    /// </summary>
    R8G8B8A8_UInt = 30,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>signed normalized integer</strong> format that supports <strong>8 bits per channel including alpha</strong>.
    /// </summary>
    R8G8B8A8_SNorm = 31,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>signed integer format</strong> that supports <strong>8 bits per channel including alpha</strong>.
    /// </summary>
    R8G8B8A8_SInt = 32,


    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit format that supports <strong>16 bits for the red channel and 16 bits for the green channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R16G16_Typeless = 33,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit <strong>floating-point format</strong> that supports <strong>16 bits for the red channel and 16 bits for the green channel</strong>.
    /// </summary>
    R16G16_Float = 34,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>16 bits for the red channel and 16 bits for the green channel</strong>.
    /// </summary>
    R16G16_UNorm = 35,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit <strong>unsigned integer format</strong> that supports <strong>16 bits for the red channel and 16 bits for the green channel</strong>.
    /// </summary>
    R16G16_UInt = 36,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit <strong>signed normalized integer</strong> format that supports <strong>16 bits for the red channel and 16 bits for the green channel</strong>.
    /// </summary>
    R16G16_SNorm = 37,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit <strong>signed integer format</strong> that supports <strong>16 bits for the red channel and 16 bits for the green channel</strong>.
    /// </summary>
    R16G16_SInt = 38,


    /// <summary>
    ///   A <strong>single-component</strong>, 32-bit format that supports <strong>32 bits for the red channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R32_Typeless = 39,

    /// <summary>
    ///   A <strong>single-component</strong>, 32-bit <strong>floating-point format</strong> that supports <strong>32 bits for depth</strong>.
    /// </summary>
    D32_Float = 40,

    /// <summary>
    ///   A <strong>single-component</strong>, 32-bit <strong>floating-point format</strong> that supports <strong>32 bits for the red channel</strong>.
    /// </summary>
    R32_Float = 41,

    /// <summary>
    ///   A <strong>single-component</strong>, 32-bit <strong>unsigned integer format</strong> that supports <strong>32 bits for the red channel</strong>.
    /// </summary>
    R32_UInt = 42,

    /// <summary>
    ///   A <strong>single-component</strong>, 32-bit <strong>signed integer format</strong> that supports <strong>32 bits for the red channel</strong>.
    /// </summary>
    R32_SInt = 43,


    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit format that supports <strong>24 bits for the red channel and 8 bits for the green channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R24G8_Typeless = 44,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit format consisting of a <strong>24-bit unsigned normalized integer component for depth</strong>
    ///   and a <strong>8-bit unsigned integer component for stencil</strong>.
    /// </summary>
    D24_UNorm_S8_UInt = 45,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit format consisting of a <strong>24-bit unsigned normalized integer red component</strong>
    ///   and a <strong>8-bit typeless component</strong>.
    ///   <br/>
    ///   This format has a <strong>typeless</strong> component, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R24_UNorm_X8_Typeless = 46,

    /// <summary>
    ///   A <strong>two-component</strong>, 32-bit format consisting of a <strong>24-bit typeless component</strong>
    ///   and a <strong>8-bit unsigned integer green component</strong>.
    ///   <br/>
    ///   This format has a <strong>typeless</strong> component, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    X24_Typeless_G8_UInt = 47,


    /// <summary>
    ///   A <strong>two-component</strong>, 16-bit format that supports <strong>8 bits for the red channel and 8 bits for the green channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R8G8_Typeless = 48,

    /// <summary>
    ///   A <strong>two-component</strong>, 16-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits for the red channel and 8 bits for the green channel</strong>.
    /// </summary>
    R8G8_UNorm = 49,

    /// <summary>
    ///   A <strong>two-component</strong>, 16-bit <strong>unsigned integer format</strong> that supports <strong>8 bits for the red channel and 8 bits for the green channel</strong>.
    /// </summary>
    R8G8_UInt = 50,

    /// <summary>
    ///   A <strong>two-component</strong>, 16-bit <strong>signed normalized integer</strong> format that supports <strong>8 bits for the red channel and 8 bits for the green channel</strong>.
    /// </summary>
    R8G8_SNorm = 51,

    /// <summary>
    ///   A <strong>two-component</strong>, 16-bit <strong>signed integer format</strong> that supports <strong>8 bits for the red channel and 8 bits for the green channel</strong>.
    /// </summary>
    R8G8_SInt = 52,


    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit format that supports <strong>16 bits for the red channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R16_Typeless = 53,

    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit <strong>floating-point format</strong> that supports <strong>16 bits for the red channel</strong>.
    /// </summary>
    R16_Float = 54,

    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit <strong>unsigned normalized integer</strong> format that supports <strong>16 bits for depth</strong>.
    /// </summary>
    D16_UNorm = 55,

    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit <strong>unsigned normalized integer</strong> format that supports <strong>16 bits for the red channel</strong>.
    /// </summary>
    R16_UNorm = 56,

    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit <strong>unsigned integer format</strong> that supports <strong>16 bits for the red channel</strong>.
    /// </summary>
    R16_UInt = 57,

    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit <strong>signed normalized integer</strong> format that supports <strong>16 bits for the red channel</strong>.
    /// </summary>
    R16_SNorm = 58,

    /// <summary>
    ///   A <strong>single-component</strong>, 16-bit <strong>signed integer format</strong> that supports <strong>16 bits for the red channel</strong>.
    /// </summary>
    R16_SInt = 59,


    /// <summary>
    ///   A <strong>single-component</strong>, 8-bit format that supports <strong>8 bits for the red channel</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    R8_Typeless = 60,

    /// <summary>
    ///   A <strong>single-component</strong>, 8-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits for the red channel</strong>.
    /// </summary>
    R8_UNorm = 61,

    /// <summary>
    ///   A <strong>single-component</strong>, 8-bit <strong>unsigned integer format</strong> that supports <strong>8 bits for the red channel</strong>.
    /// </summary>
    R8_UInt = 62,

    /// <summary>
    ///   A <strong>single-component</strong>, 8-bit <strong>signed normalized integer</strong> format that supports <strong>8 bits for the red channel</strong>.
    /// </summary>
    R8_SNorm = 63,

    /// <summary>
    ///   A <strong>single-component</strong>, 8-bit <strong>signed integer format</strong> that supports <strong>8 bits for the red channel</strong>.
    /// </summary>
    R8_SInt = 64,

    /// <summary>
    ///   A <strong>single-component</strong>, 8-bit <strong>unsigned normalized integer</strong> format <strong>for alpha only</strong>.
    /// </summary>
    A8_UNorm = 65,


    /// <summary>
    ///   A <strong>single-component</strong>, 1-bit <strong>unsigned normalized integer</strong> format that supports <strong>1 bit for the red channel</strong>.
    /// </summary>
    R1_UNorm = 66,


    /// <summary>
    ///   A <strong>three-component</strong>, 32-bit <strong>partial-precision floating-point format</strong> that supports
    ///   <strong>9 bits per channel</strong> with the same <strong>5-bit shared exponent</strong>.
    ///   <br/>
    ///   It uses a variant of <c>s10e5</c> (sign bit, 10-bit mantissa, and 5-bit biased (15) exponent), but there are no sign bits, and the exponent is shared.
    /// </summary>
    R9G9B9E5_Sharedexp = 67,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format packed in a form <strong>analogous to the UYVY format</strong>.
    ///   Each 32-bit block describes a pair of pixels (<c>RGB</c>, <c>RGB</c>) with 8-bit RGB components where the R/B values are repeated,
    ///   and the G values are unique to each pixel.
    /// </summary>
    R8G8_B8G8_UNorm = 68,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format packed in a form <strong>analogous to the YUY2 format</strong>.
    ///   Each 32-bit block describes a pair of pixels (<c>RGB</c>, <c>RGB</c>) with 8-bit RGB components where the R/B values are repeated,
    ///   and the G values are unique to each pixel.
    /// </summary>
    G8R8_G8B8_UNorm = 69,


    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit block-compression format using the <strong>BC1 encoding</strong>, where
    ///   the alpha channel is optionally encoded in 1 bit (either fully opaque or fully transparent), and the RGB color is encoded
    ///   in 2 bits per pixel to select from a 4-color (or 3-color + transparent) color palette.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC1 format is tipically used for diffuse maps that do not require alpha transparency or where the alpha channel is not important.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC1_Typeless = 70,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>unsigned normalized integer</strong> block-compression format
    ///   using the <strong>BC1 encoding</strong>, where the alpha channel is optionally encoded in 1 bit (either fully opaque or fully transparent),
    ///   and the RGB color is encoded in 2 bits per pixel to select from a 4-color (or 3-color + transparent) color palette.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC1 format is tipically used for diffuse maps that do not require alpha transparency or where the alpha channel is not important.
    /// </summary>
    BC1_UNorm = 71,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit <strong>unsigned normalized integer</strong> block-compression format <strong>for sRGB data</strong>
    ///   using the <strong>BC1 encoding</strong>, where the alpha channel is optionally encoded in 1 bit (either fully opaque or fully transparent),
    ///   and the RGB color is encoded in 2 bits per pixel to select from a 4-color (or 3-color + transparent) color palette.
    ///   <br/>
    ///   The data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC1 format is tipically used for diffuse maps that do not require alpha transparency or where the alpha channel is not important.
    /// </summary>
    BC1_UNorm_SRgb = 72,


    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit block-compression format using the <strong>BC2 encoding</strong>,
    ///   where the alpha channel is encoded in 4 bits (for 16 levels of transparency, quantized, not interpolated),
    ///   and the RGB color is encoded in 2 bits per pixel to select from a 4-color color palette.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC2 format is tipically used for UI, decals, or sharp masks, although it is now considered somewhat outdated.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC2_Typeless = 73,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format
    ///   using the <strong>BC2 encoding</strong>, where the alpha channel is encoded in 4 bits (for 16 levels of transparency, quantized, not interpolated),
    ///   and the RGB color is encoded in 2 bits per pixel to select from a 4-color color palette.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC2 format is tipically used for UI, decals, or sharp masks, although it is now considered somewhat outdated.
    /// </summary>
    BC2_UNorm = 74,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format <strong>for sRGB data</strong>
    ///   using the <strong>BC2 encoding</strong>, where the alpha channel is encoded in 4 bits (for 16 levels of transparency, quantized, not interpolated),
    ///   and the RGB color is encoded in 2 bits per pixel to select from a 4-color color palette.
    ///   <br/>
    ///   The data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC2 format is tipically used for UI, decals, or sharp masks, although it is now considered somewhat outdated.
    /// </summary>
    BC2_UNorm_SRgb = 75,


    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit block-compression format using the <strong>BC3 encoding</strong>,
    ///   where the alpha channel is encoded as two 8-bit alpha endpoints and 3 bits per pixel to select
    ///   from 6 interpolated alpha values, and the RGB color is encoded in 2 bits per pixel to select from a 4-color color palette.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC3 format is tipically used for smooth and soft transparency, like foliage, shadows, hair, or smoke.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC3_Typeless = 76,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format
    ///   using the <strong>BC3 encoding</strong>, where the alpha channel is encoded as two 8-bit alpha endpoints and 3 bits per pixel to select
    ///   from 6 interpolated alpha values, and the RGB color is encoded in 2 bits per pixel to select from a 4-color color palette.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC3 format is tipically used for smooth and soft transparency, like foliage, shadows, hair, or smoke.
    /// </summary>
    BC3_UNorm = 77,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format <strong>for sRGB data</strong>
    ///   using the <strong>BC3 encoding</strong>, where the alpha channel is encoded as two 8-bit alpha endpoints and 3 bits per pixel to select
    ///   from 6 interpolated alpha values, and the RGB color is encoded in 2 bits per pixel to select from a 4-color color palette.
    ///   <br/>
    ///   The data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC3 format is tipically used for smooth and soft transparency, like foliage, shadows, hair, or smoke.
    /// </summary>
    BC3_UNorm_SRgb = 78,


    /// <summary>
    ///   A <strong>single-component</strong>, 64-bit  block-compression format using the <strong>BC4 encoding</strong>,
    ///   where the data is encoded as two 8-bit endpoints and 3 bits per pixel to select from 6 interpolated values.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC4 format is tipically used for grayscale Textures, smooth masks, or heightmaps.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC4_Typeless = 79,

    /// <summary>
    ///   A <strong>single-component</strong>, 64-bit <strong>unsigned normalized integer</strong> block-compression format
    ///   using the <strong>BC4 encoding</strong>, where the data is encoded as two 8-bit endpoints and 3 bits per pixel to select
    ///   from 6 interpolated values.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC4 format is tipically used for grayscale Textures, smooth masks, or heightmaps.
    /// </summary>
    BC4_UNorm = 80,

    /// <summary>
    ///   A <strong>single-component</strong>, 64-bit <strong>signed normalized integer</strong> block-compression format
    ///   using the <strong>BC4 encoding</strong>, where the data is encoded as two 8-bit endpoints and 3 bits per pixel to select
    ///   from 6 interpolated values.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC4 format is tipically used for grayscale Textures, smooth masks, or heightmaps.
    /// </summary>
    BC4_SNorm = 81,


    /// <summary>
    ///   A <strong>two-component</strong>, 128-bit block-compression format using the <strong>BC5 encoding</strong>,
    ///   where for each channel the data is encoded as two 8-bit endpoints and 3 bits per pixel to select from 6 interpolated values.
    ///   This is essentially a BC4 format for each channel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC5 format is tipically used for dual-channel data like normal maps (X and Y) or vector fields.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC5_Typeless = 82,

    /// <summary>
    ///   A <strong>two-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format
    ///   using the <strong>BC5 encoding</strong>, where for each channel the data is encoded as two 8-bit endpoints and 3 bits per pixel
    ///   to select from 6 interpolated values. This is essentially a BC4 format for each channel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC5 format is tipically used for dual-channel data like normal maps (X and Y) or vector fields.
    /// </summary>
    BC5_UNorm = 83,

    /// <summary>
    ///   A <strong>two-component</strong>, 128-bit <strong>signed normalized integer</strong> block-compression format
    ///   using the <strong>BC5 encoding</strong>, where for each channel the data is encoded as two 8-bit endpoints and 3 bits per pixel
    ///   to select from 6 interpolated values. This is essentially a BC4 format for each channel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC5 format is tipically used for dual-channel data like normal maps (X and Y) or vector fields.
    /// </summary>
    BC5_SNorm = 84,


    /// <summary>
    ///   A <strong>three-component</strong>, 16-bit <strong>unsigned normalized integer</strong> format that supports <strong>5 bits for blue, 6 bits for green, and 5 bits for red</strong>.
    /// </summary>
    B5G6R5_UNorm = 85,

    /// <summary>
    ///   A <strong>four-component</strong>, 16-bit <strong>unsigned normalized integer</strong> format that supports <strong>5 bits for each color channel and 1-bit alpha</strong>.
    /// </summary>
    B5G5R5A1_UNorm = 86,


    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits for each color channel and 8-bit alpha</strong>.
    /// </summary>
    B8G8R8A8_UNorm = 87,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits for each color channel</strong> and a remaining unused 8-bit part.
    /// </summary>
    B8G8R8X8_UNorm = 88,


    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit format consisting of <strong>10 bits for each of the red, green, and blue channels</strong> encoded as
    ///   2.8-biased fixed-point numbers and <strong>2-bit alpha channel</strong>.
    ///   The <strong>XR bias</strong> applies a fixed scale and offset to the red channel to better represent HDR luminance. This is useful when
    ///   tone mapping has already been applied and the image is ready for display.
    ///   <br/>
    ///   This format is intended for use as a <strong>presentation format</strong>, particularly for High Dynamic Range (HDR) content. It
    ///   is read-only for shaders and not renderable directly. Instead, it is typically converted from a compatible format (like <see cref="R10G10B10A2_UNorm"/>).
    /// </summary>
    R10G10B10_Xr_Bias_A2_UNorm = 89,


    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit format that supports <strong>8 bits for each color channel and 8-bit alpha</strong>.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    B8G8R8A8_Typeless = 90,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit <strong>unsigned normalized integer</strong> format that supports <strong>8 bits for each color channel and 8-bit alpha</strong>,
    ///   where the data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    /// </summary>
    B8G8R8A8_UNorm_SRgb = 91,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit format that supports 8 bits for each color channel, and 8 bits are unused.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    B8G8R8X8_Typeless = 92,

    /// <summary>
    ///   A <strong>four-component</strong>, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each color channel, and 8 bits are unused.
    ///   ,
    ///   where the data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    /// </summary>
    B8G8R8X8_UNorm_SRgb = 93,


    /// <summary>
    ///   A <strong>three-component</strong>, 128-bit block-compression format <strong>for HDR data</strong>
    ///   using the <strong>BC6H encoding</strong>, where the color channels are encoded as 16-bit <see cref="System.Half"/> values and
    ///   selected and interpolated according to the BC6H algorithm.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC6H format is best used for HDR environment maps, lightprobes, or any Texture with values beyond the [0,1] range.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC6H_Typeless = 94,

    /// <summary>
    ///   A <strong>three-component</strong>, 128-bit <strong>unsigned floating-point</strong> block-compression format <strong>for positive-only HDR data</strong>
    ///   using the <strong>BC6H encoding</strong>, where the color channels are encoded as 16-bit <see cref="System.Half"/> values and
    ///   selected and interpolated according to the BC6H algorithm.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC6H format is best used for HDR environment maps, lightprobes, or any Texture with values beyond the [0,1] range.
    /// </summary>
    BC6H_Uf16 = 95,

    /// <summary>
    ///   A <strong>three-component</strong>, 128-bit <strong>signed floating-point</strong> block-compression format <strong>for full-range HDR data</strong>
    ///   using the <strong>BC6H encoding</strong>, where the color channels are encoded as 16-bit <see cref="System.Half"/> values and
    ///   selected and interpolated according to the BC6H algorithm.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC6H format is best used for HDR environment maps, lightprobes, or any Texture with values beyond the [0,1] range.
    /// </summary>
    BC6H_Sf16 = 96,


    /// <summary>
    ///   A <strong>three- or four-component</strong>, 128-bit block-compression format using the <strong>BC7 encoding</strong>,
    ///   where it can encode the RGB channels and an optional 1-8-bit alpha channel according to the BC7 algorithm.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC7 format is best for high-quality color Textures, like albedo maps, UI, or detailed color surfaces.
    ///   <br/>
    ///   This format is <strong>typeless</strong>, i.e. it just specifies the memory layout, but can be used for different types of data,
    ///   such as floating-point, unsigned-integer, or signed-integer.
    /// </summary>
    BC7_Typeless = 97,

    /// <summary>
    ///   A <strong>three- or four-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format
    ///   using the <strong>BC7 encoding</strong>, where it can encode the RGB channels and an optional 1-8-bit alpha channel according to the BC7 algorithm.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC7 format is best for high-quality color Textures, like albedo maps, UI, or detailed color surfaces.
    /// </summary>
    BC7_UNorm = 98,

    /// <summary>
    ///   A <strong>three- or four-component</strong>, 128-bit <strong>unsigned normalized integer</strong> block-compression format <strong>for sRGB data</strong>
    ///   using the <strong>BC7 encoding</strong>, where it can encode the RGB channels and an optional 1-8-bit alpha channel according to the BC7 algorithm.
    ///   <br/>
    ///   The data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   The BC7 format is best for high-quality color Textures, like albedo maps, UI, or detailed color surfaces.
    /// </summary>
    BC7_UNorm_SRgb = 99,


    /// <summary>
    ///   A <strong>three-component</strong>, 64-bit block-compression format using the <strong>ETC1 encoding</strong>,
    ///   where the RGB color is encoded in 4 bits per pixel. Does not support alpha.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Commonly used for opaque textures on mobile devices.
    /// </summary>
    ETC1 = 1088,

    /// <summary>
    ///   A <strong>three-component</strong>, 64-bit block-compression format using the <strong>ETC2 RGB encoding</strong>,
    ///   where the RGB color is encoded in 4 bits per pixel. Does not support alpha.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Widely supported on modern mobile and embedded GPUs for general color data.
    /// </summary>
    ETC2_RGB = 1089,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit block-compression format using the <strong>ETC2 RGBA encoding</strong>,
    ///   where the RGB color is encoded in 4 bits per pixel and the alpha channel is encoded in 4 bits per pixel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Used for Textures requiring full alpha support on mobile and embedded devices.
    /// </summary>
    ETC2_RGBA = 1090,

    /// <summary>
    ///   A <strong>four-component</strong>, 64-bit block-compression format using the <strong>ETC2 RGB+A1 encoding</strong>,
    ///   where the RGB color is encoded in 4 bits per pixel and the alpha channel is encoded as a single bit (1-bit alpha mask).
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Suitable for Textures with simple transparency (fully opaque or fully transparent pixels).
    /// </summary>
    ETC2_RGB_A1 = 1091,

    /// <summary>
    ///   A <strong>single-component</strong>, 64-bit block-compression format using the <strong>EAC R11 unsigned encoding</strong>,
    ///   where the red channel is encoded as an unsigned 11-bit value per pixel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Typically used for single-channel data such as heightmaps or masks.
    /// </summary>
    EAC_R11_Unsigned = 1092,

    /// <summary>
    ///   A <strong>single-component</strong>, 64-bit block-compression format using the <strong>EAC R11 signed encoding</strong>,
    ///   where the red channel is encoded as a signed 11-bit value per pixel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Typically used for single-channel signed data such as normal maps or vector fields (reconstructing the second component).
    /// </summary>
    EAC_R11_Signed = 1093,

    /// <summary>
    ///   A <strong>two-component</strong>, 128-bit block-compression format using the <strong>EAC RG11 unsigned encoding</strong>,
    ///   where the red and green channels are encoded as unsigned 11-bit values per pixel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Used for dual-channel data such as normal maps or vector fields.
    /// </summary>
    EAC_RG11_Unsigned = 1094,

    /// <summary>
    ///   A <strong>two-component</strong>, 128-bit block-compression format using the <strong>EAC RG11 signed encoding</strong>,
    ///   where the red and green channels are encoded as signed 11-bit values per pixel.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Used for dual-channel signed data such as normal maps or vector fields.
    /// </summary>
    EAC_RG11_Signed = 1095,

    /// <summary>
    ///   A <strong>four-component</strong>, 128-bit block-compression format using the <strong>ETC2 RGBA encoding</strong> for <strong>sRGB data</strong>,
    ///   where the RGB color is encoded in 4 bits per pixel and the alpha channel is encoded in 4 bits per pixel.
    ///   <br/>
    ///   The data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Tipically used for color textures with transparency on mobile and embedded devices.
    /// </summary>
    ETC2_RGBA_SRgb = 1096,

    /// <summary>
    ///   A <strong>three-component</strong>, 64-bit block-compression format using the <strong>ETC2 RGB encoding</strong> for <strong>sRGB data</strong>,
    ///   where the RGB color is encoded in 4 bits per pixel.
    ///   <br/>
    ///   The data is stored in <strong>sRGB color space</strong>, and the GPU will automatically convert it to linear space when sampling in a shader.
    ///   <br/>
    ///   The block-compression formats operate only on 4x4 blocks, so Textures using this format <strong>must have dimensions that are multiples of 4</strong>.
    ///   <br/>
    ///   Tipically used for color textures without alpha on mobile and embedded devices.
    /// </summary>
    ETC2_RGB_SRgb = 1097
}
