// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// Operands of the MaterialNode.
    /// </summary>
    [DataContract("BinaryOperator")]
    public enum BinaryOperator
    {
        /// <summary>
        /// Add of the two textures.
        /// </summary>
        Add,

        /// <summary>
        /// Average of the two textures.
        /// </summary>
        Average,

        /// <summary>
        /// Color effect from the two textures.
        /// </summary>
        Color,

        /// <summary>
        /// Color burn effect from the two textures.
        /// </summary>
        ColorBurn,

        /// <summary>
        /// Color dodge effect from the two textures.
        /// </summary>
        ColorDodge,

        /// <summary>
        /// Darken effect from the two textures.
        /// </summary>
        Darken,

        /// <summary>
        /// Desaturate effect from the two textures.
        /// </summary>
        Desaturate,

        /// <summary>
        /// Difference of the two textures.
        /// </summary>
        Difference,

        /// <summary>
        /// Divide first texture with the second one.
        /// </summary>
        Divide,

        /// <summary>
        /// Exclusion effect from the two textures.
        /// </summary>
        Exclusion,

        /// <summary>
        /// Hard light effect from the two textures.
        /// </summary>
        HardLight,

        /// <summary>
        /// hard mix effect from the two textures.
        /// </summary>
        HardMix,

        /// <summary>
        /// Hue effect from the two textures.
        /// </summary>
        Hue,

        /// <summary>
        /// Illuminate effect from the two textures.
        /// </summary>
        Illuminate,

        /// <summary>
        /// In effect from the two textures.
        /// </summary>
        In,

        /// <summary>
        /// Lighten effect from the two textures.
        /// </summary>
        Lighten,

        /// <summary>
        /// Linear burn effect from the two textures.
        /// </summary>
        LinearBurn,

        /// <summary>
        /// Linear dodge effect from the two textures.
        /// </summary>
        LinearDodge,

        /// <summary>
        /// Apply mask from second texture to the first one.
        /// </summary>
        Mask,

        /// <summary>
        /// Multiply the two textures.
        /// </summary>
        Multiply,
        
        /// <summary>
        /// Out effect from the two textures.
        /// </summary>
        Out,

        /// <summary>
        /// Over effect from the two textures.
        /// </summary>
        Over,

        /// <summary>
        /// Overlay effect from the two textures.
        /// </summary>
        Overlay,

        /// <summary>
        /// Pin light effect from the two textures.
        /// </summary>
        PinLight,

        /// <summary>
        /// Saturate effect from the two textures.
        /// </summary>
        Saturate,

        /// <summary>
        /// Saturation effect from the two textures.
        /// </summary>
        Saturation,

        /// <summary>
        /// Screen effect from the two textures.
        /// </summary>
        Screen,

        /// <summary>
        /// Soft light effect from the two textures.
        /// </summary>
        SoftLight,

        /// <summary>
        /// Subtract the two textures.
        /// </summary>
        Subtract,

        /// <summary>
        /// Take color for the first texture but alpha from the second
        /// </summary>
        SubstituteAlpha,

        /// <summary>
        /// Threshold, resulting in a black-white texture for grayscale against a set threshold
        /// </summary>
        Threshold,

        //TODO: lerp, clamp ?
    }
}
