// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Identifies the filtering mode to use during Texture sampling.
/// </summary>
/// <remarks>
///   During texture sampling, one or more texels are read and combined (this is called filtering)
///   to produce a single value.
/// </remarks>
[DataContract("TextureFilter")]
public enum TextureFilter
{
    /// <summary>
    ///   Use point sampling for minification, magnification, and mip-level sampling.
    /// </summary>
    /// <remarks>
    ///   Point sampling is the fastest texture filtering method. It is also the lowest quality,
    ///   because it just reads a single value and does not blend between texels.
    /// </remarks>
    Point = 0,

    /// <summary>
    ///   Use point sampling for minification and magnification, and linear interpolation for mip-level sampling.
    /// </summary>
    /// <seealso cref="Point"/>
    /// <seealso cref="Linear"/>
    MinMagPointMipLinear = 1,

    /// <summary>
    ///   Use point sampling for minification, linear interpolation for magnification, and point sampling for mip-level sampling.
    /// </summary>
    /// <seealso cref="Point"/>
    /// <seealso cref="Linear"/>
    MinPointMagLinearMipPoint = 4,

    /// <summary>
    ///   Use point sampling for minification, linear interpolation for magnification and mip-level sampling.
    /// </summary>
    /// <seealso cref="Point"/>
    /// <seealso cref="Linear"/>
    MinPointMagMipLinear = 5,

    /// <summary>
    ///   Use linear interpolation for minification, point sampling for magnification and mip-level sampling.
    /// </summary>
    /// <seealso cref="Point"/>
    /// <seealso cref="Linear"/>
    MinLinearMagMipPoint = 16,

    /// <summary>
    ///   Use linear interpolation for minification, point sampling for magnification, and linear interpolation for mip-level sampling.
    /// </summary>
    /// <seealso cref="Point"/>
    /// <seealso cref="Linear"/>
    MinLinearMagPointMipLinear = 17,

    /// <summary>
    ///   Use linear interpolation for minification and magnification, and point sampling for mip-level sampling.
    /// </summary>
    MinMagLinearMipPoint = 20,

    /// <summary>
    ///   Use linear interpolation for minification, magnification, and mip-level sampling.
    /// </summary>
    /// <remarks>
    ///   Linear interpolation is slower than point sampling, but produces higher quality results.
    ///   Two samples are taken across the sampling direction, and a linearly interpolated value
    ///   is generated between those by blending them.
    /// </remarks>
    Linear = 21,

    /// <summary>
    ///   Use anisotropic interpolation for minification, magnification, and mip-level sampling.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     When viewing a surface at a shallow angle, the Texture is stretched according to the perspective.
    ///     Point or linear interpolation sample in a circular area independent of the viewing angle, producing a
    ///     blurry or smeared appearance.
    ///   </para>
    ///   <para>
    ///     Anisotropic filtering addresses this by sampling Textures differently depending on the angle of the
    ///     surface relative to the viewer.
    ///     Instead of assuming a circular sampling footprint (as in isotropic methods like bilinear filtering),
    ///     it stretches the sampling region into an ellipse or more complex shapes that better fit the distorted
    ///     projection of the Texture, and takes more samples along that direction, depending on the anisotropy level.
    ///   </para>
    ///   <para>
    ///     It also interacts with mipmapping, selecting and interpolating from the appropriate mipmap levels,
    ///     to ensure that the correct Texture resolution is used, even when viewed at extreme angles.
    ///   </para>
    /// </remarks>
    Anisotropic = 85,

    /// <summary>
    ///   Use point sampling for minification, magnification, and mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Comparison filtering compares each sampled texel against a comparison value.
    ///     The boolean result is blended the same way that normal texture filtering is blended.
    ///   </para>
    ///   <para>
    ///     Comparison filters only work with textures that have the following formats:
    ///     <see cref="PixelFormat.R32_Float_X8X24_Typeless"/>, <see cref="PixelFormat.R32_Float"/>,
    ///     <see cref="PixelFormat.R24_UNorm_X8_Typeless"/>, and <see cref="PixelFormat.R16_UNorm"/>.
    ///   </para>
    /// </remarks>
    ComparisonPoint = 128,

    /// <summary>
    ///   Use point sampling for minification and magnification, and linear interpolation for mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonMinMagPointMipLinear = 129,

    /// <summary>
    ///   Use point sampling for minification, linear interpolation for magnification, and point sampling for mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonMinPointMagLinearMipPoint = 132,

    /// <summary>
    ///   Use point sampling for minification, and linear interpolation for magnification and mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonMinPointMagMipLinear = 133,

    /// <summary>
    ///   Use linear interpolation for minification, and point sampling for magnification and mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonMinLinearMagMipPoint = 144,

    /// <summary>
    ///   Use linear interpolation for minification, point sampling for magnification, and linear interpolation for mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonMinLinearMagPointMipLinear = 145,

    /// <summary>
    ///   Use linear interpolation for minification and magnification, and point sampling for mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonMinMagLinearMipPoint = 148,

    /// <summary>
    ///   Use linear interpolation for minification, magnification, and mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonLinear = 149,

    /// <summary>
    ///   Use anisotropic interpolation for minification, magnification, and mip-level sampling.
    ///   Compare the result to the comparison value.
    /// </summary>
    /// <inheritdoc cref="ComparisonPoint" path="/remarks"/>
    ComparisonAnisotropic = 213
}
