// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// Operands of the MaterialNode.
    /// </summary>
    [DataContract("BinaryOperator")]
    public enum BinaryOperator
    {
        /// <summary>
        /// Add '3ds' of the two textures.
        /// </summary>
        Add = 0,

        /// <summary>
        /// Add of the two textures.
        /// </summary>
        AddMath = 31,

        /// <summary>
        /// Average of the two textures.
        /// </summary>
        Average = 1,

        /// <summary>
        /// Color effect from the two textures.
        /// </summary>
        Color = 2,

        /// <summary>
        /// Color burn effect from the two textures.
        /// </summary>
        ColorBurn = 3,

        /// <summary>
        /// Color dodge effect from the two textures.
        /// </summary>
        ColorDodge = 4,

        /// <summary>
        /// Darken effect from the two textures.
        /// </summary>
        Darken = 5,

        /// <summary>
        /// Desaturate effect from the two textures.
        /// </summary>
        Desaturate = 6,

        /// <summary>
        /// Difference of the two textures.
        /// </summary>
        Difference = 7,

        /// <summary>
        /// Divide first texture with the second one.
        /// </summary>
        Divide = 8,

        /// <summary>
        /// Exclusion effect from the two textures.
        /// </summary>
        Exclusion = 9,

        /// <summary>
        /// Hard light effect from the two textures.
        /// </summary>
        HardLight = 10,

        /// <summary>
        /// hard mix effect from the two textures.
        /// </summary>
        HardMix = 11,

        /// <summary>
        /// Hue effect from the two textures.
        /// </summary>
        Hue = 12,

        /// <summary>
        /// Illuminate effect from the two textures.
        /// </summary>
        Illuminate = 13,

        /// <summary>
        /// In effect from the two textures.
        /// </summary>
        In = 14,

        /// <summary>
        /// Lighten effect from the two textures.
        /// </summary>
        Lighten = 15,

        /// <summary>
        /// Linear burn effect from the two textures.
        /// </summary>
        LinearBurn = 16,

        /// <summary>
        /// Linear dodge effect from the two textures.
        /// </summary>
        LinearDodge = 17,

        /// <summary>
        /// Apply mask from second texture to the first one.
        /// </summary>
        Mask = 18,

        /// <summary>
        /// Multiply the two textures.
        /// </summary>
        Multiply = 19,
        
        /// <summary>
        /// Out effect from the two textures.
        /// </summary>
        Out = 20,

        /// <summary>
        /// Over effect from the two textures.
        /// </summary>
        Over = 21,

        /// <summary>
        /// Overlay effect from the two textures.
        /// </summary>
        Overlay = 22,

        /// <summary>
        /// Pin light effect from the two textures.
        /// </summary>
        PinLight = 23,

        /// <summary>
        /// Saturate effect from the two textures.
        /// </summary>
        Saturate = 24,

        /// <summary>
        /// Saturation effect from the two textures.
        /// </summary>
        Saturation = 25,

        /// <summary>
        /// Screen effect from the two textures.
        /// </summary>
        Screen = 26,

        /// <summary>
        /// Soft light effect from the two textures.
        /// </summary>
        SoftLight = 27,

        /// <summary>
        /// Subtract the two textures.
        /// </summary>
        Subtract = 28,

        /// <summary>
        /// Take color for the first texture but alpha from the second
        /// </summary>
        SubstituteAlpha = 29,

        /// <summary>
        /// Threshold, resulting in a black-white texture for grayscale against a set threshold
        /// </summary>
        Threshold = 30,
        
        // N.B. #31 is allocated to 'AddMath'

        //TODO: lerp, clamp ?
    }
}
