// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Defines the color space used for Textures, Materials, lighting calculations, etc.
/// </summary>
[DataContract("ColorSpace")]
public enum ColorSpace
{
    /// <summary>
    ///   Use a <strong>linear color space</strong>, i.e. treat color values as linear values, without
    ///   applying any gamma correction.
    /// </summary>
    /// <remarks>
    ///   The linear color space is useful when the output of the rendering (or the input
    ///   Textures) represent values that can be transformed in a post-processing step
    ///   (such as tone-mapping, color-correction, etc.) or if they represent non-final
    ///   color values (like intermediate buffers) or non-color values (like heights,
    ///   roughness, etc.)
    /// </remarks>
    Linear,

    /// <summary>
    ///   Use a <strong>gamma color space</strong>.
    /// </summary>
    /// <remarks>
    ///   A gamma color space is a color space in which colors are applied a gamma curve
    ///   (like sRGB) so they are perceptually linear. This is useful when the output of
    ///   the rendering (or the input Textures) represent final color values that will
    ///   be presented to a non-HDR screen, or if they represent color values that won't
    ///   be transformed in a post-processing step.
    /// </remarks>
    Gamma
}
