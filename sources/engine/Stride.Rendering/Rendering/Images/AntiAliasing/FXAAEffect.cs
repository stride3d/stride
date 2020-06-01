// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// A FXAA anti-aliasing pass.
    /// </summary>
    [DataContract("FXAAEffect")]
    public class FXAAEffect : ImageEffectShader, IScreenSpaceAntiAliasingEffect
    {
        private const int DefaultQuality = 3;
        internal static readonly PermutationParameterKey<int> GreenAsLumaKey = ParameterKeys.NewPermutation(0);
        internal static readonly PermutationParameterKey<int> QualityKey = ParameterKeys.NewPermutation(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="FXAAEffect"/> class.
        /// </summary>
        public FXAAEffect() : this("FXAAShaderEffect")
        {
            InputLuminanceInAlpha = true;
        }

        public bool NeedRangeDecompress => true;

        public bool RequiresDepthBuffer => false;

        public bool RequiresVelocityBuffer => false;

        /// <summary>
        /// The dithering type used (directly related to rendering style).
        /// </summary>
        /// <userdoc>The dithering type used (directly related to rendering style)</userdoc>
        [DataMember(10)]
        [DefaultValue(DitherType.Low)]
        public DitherType Dither { get; set; } = DitherType.Low;

        /// <summary>
        /// The quality of the FXAA (directly related to performance). From 0 to 5 with <see cref="DitherType.Medium"/>, from 0 to 9 with <see cref="DitherType.Low"/> and unavailable (should be 9) with <see cref="DitherType.None"/>.
        /// </summary>
        /// <userdoc>The quality of the FXAA (directly related to performance). From 0 to 5 with Medium dither, from 0 to 9 with Low dither and unavailable (should be 9) with no dither.</userdoc>
        [DataMember(20)]
        [DefaultValue(DefaultQuality)]
        [DataMemberRange(0, 9, 1, 2, 0)]
        public int Quality { get; set; } = DefaultQuality;

        /// <summary>
        /// Gets or sets a value indicating whether the luminance will be retrieved from the alpha channel of the input color. Otherwise, the green component of the input color is used as a luminance.
        /// </summary>
        /// <value><c>true</c> the luminance will be retrieved from the alpha channel of the input color. Otherwise, the green component of the input color is used as a luminance.</value>
        /// <userdoc>Retrieve the luminance from the alpha channel of the input color. Otherwise, use the green component of the input color as an approximation of the luminance.</userdoc>
        [DataMember(30)]
        [DefaultValue(true)]
        [Display("Input luminance from alpha")]
        public bool InputLuminanceInAlpha { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FXAAEffect"/> class.
        /// </summary>
        /// <param name="antialiasShaderName">Name of the antialias shader.</param>
        /// <exception cref="System.ArgumentNullException">antialiasShaderName</exception>
        public FXAAEffect(string antialiasShaderName) : base(antialiasShaderName)
        {
            if (antialiasShaderName == null) throw new ArgumentNullException("antialiasShaderName");
        }

        public static (int, int) GetQualityRange(DitherType dither)
        {
            // Returns valid ranges for FXAA_QUALITY__PRESET (as in FXAAShader.sdsl)
            switch (dither)
            {
                case DitherType.Medium:
                    return (0, 5);
                case DitherType.Low:
                    return (0, 9);
                case DitherType.None:
                    return (9, 9);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Dither));
            }
        }

        protected override void UpdateParameters()
        {
            base.UpdateParameters();
            Parameters.Set(GreenAsLumaKey, InputLuminanceInAlpha ? 0 : 1);
            var (minQuality, maxQuality) = GetQualityRange(Dither);
            if (Quality < minQuality || Quality > maxQuality)
                throw new ArgumentOutOfRangeException(nameof(Quality), $"Quality should be between {minQuality} and {maxQuality} for dither level {Dither}");
            Parameters.Set(QualityKey, (int)Dither + Quality);
        }

        /// <summary>
        /// The dithering level used by FXAA.
        /// </summary>
        public enum DitherType
        {
            [Display("Medium (Fastest)")]
            Medium = 10,

            [Display("Low (Normal)")]
            Low = 20,

            [Display("None (Slowest)")]
            None = 30,
        }
    }
}
