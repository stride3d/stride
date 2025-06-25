// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

[DataContract("TextureFilter")]
public enum TextureFilter
{
    /// <summary>
    /// Filtering options during texture sampling.
    /// </summary>
    /// <remarks>
    /// During texture sampling, one or more texels are read and combined (this is calling filtering) to produce a single value. Point sampling reads a single texel while linear sampling reads two texels (endpoints) and linearly interpolates a third value between the endpoints. HLSL texture-sampling functions also support comparison filtering during texture sampling. Comparison filtering compares each sampled texel against a comparison value. The boolean result is blended the same way that normal texture filtering is blended. You can use HLSL intrinsic texture-sampling functions that implement texture filtering only or companion functions that use texture filtering with comparison filtering.  Texture Sampling FunctionTexture Sampling Function with Comparison Filtering samplesamplecmp or samplecmplevelzero  ? Comparison filters only work with textures that have the following DXGI formats: R32_FLOAT_X8X24_TYPELESS, R32_FLOAT, R24_UNORM_X8_TYPELESS, R16_UNORM. 
    /// </remarks>
        /// <summary>
        /// Use point sampling for minification, magnification, and mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification and magnification; use linear interpolation for mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification; use linear interpolation for magnification; use point sampling for mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification; use linear interpolation for magnification and mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification; use point sampling for magnification and mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification; use point sampling for magnification; use linear interpolation for mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification and magnification; use point sampling for mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification, magnification, and mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use anisotropic interpolation for minification, magnification, and mip-level sampling. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification, magnification, and mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification and magnification; use linear interpolation for mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification; use linear interpolation for magnification; use point sampling for mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use point sampling for minification; use linear interpolation for magnification and mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification; use point sampling for magnification and mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification; use point sampling for magnification; use linear interpolation for mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification and magnification; use point sampling for mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use linear interpolation for minification, magnification, and mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
        /// <summary>
        /// Use anisotropic interpolation for minification, magnification, and mip-level sampling. Compare the result to the comparison value. 
        /// </summary>
    Point = 0,

    MinMagPointMipLinear = 1,

    MinPointMagLinearMipPoint = 4,

    MinPointMagMipLinear = 5,

    MinLinearMagMipPoint = 16,

    MinLinearMagPointMipLinear = 17,

    MinMagLinearMipPoint = 20,

    Linear = 21,

    Anisotropic = 85,

    ComparisonPoint = 128,

    ComparisonMinMagPointMipLinear = 129,

    ComparisonMinPointMagLinearMipPoint = 132,

    ComparisonMinPointMagMipLinear = 133,

    ComparisonMinLinearMagMipPoint = 144,

    ComparisonMinLinearMagPointMipLinear = 145,

    ComparisonMinMagLinearMipPoint = 148,

    ComparisonLinear = 149,

    ComparisonAnisotropic = 213
}
