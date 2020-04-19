// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Rendering.Images
{
    [DataContract("FilmGrain")]
    public sealed class FilmGrain : ColorTransform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilmGrain"/> class.
        /// </summary>
        public FilmGrain() : this("FilmGrainShader")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilmGrain"/> class.
        /// </summary>
        public FilmGrain(string effect) : base(effect)
        {
            Amount = 0.25f;
            GrainSize = 1.6f;
            Animate = false;
            LuminanceFactor = 1f;
        }

        /// <summary>
        /// Amount of grain.
        /// </summary>
        /// <userdoc>The strength of the effect</userdoc>
        [DataMember(10)]
        [DefaultValue(0.25f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f, 3)]
        public float Amount { get; set; }

        /// <summary>
        /// Grain size.
        /// </summary>
        /// <userdoc>The size of the grains (in pixels)</userdoc>
        [DataMember(20)]
        [DefaultValue(1.6f)]
        [DataMemberRange(0f, 10f, 0.1f, 1.0f, 3)]
        public float GrainSize { get; set; }

        /// <summary>
        /// Animates the film grain.
        /// </summary>
        /// <userdoc>When checked, the procedural pattern changes at each frame.</userdoc>
        [DataMember(30)]
        [DefaultValue(false)]
        public bool Animate { get; set; }

        /// <summary>
        /// How the luminance influences the amount of grain.
        /// </summary>
        /// <userdoc>Specifies how strongly the original pixel luminance is affected by the grain pattern.</userdoc>
        [DataMember(40)]
        [DefaultValue(1.0)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f, 3)]
        public float LuminanceFactor { get; set; }

        private float time = 1f;

        public override void UpdateParameters(ColorTransformContext context)
        {
            Parameters.Set(FilmGrainShaderKeys.Amount, Amount * 4 * 0.02f);
            Parameters.Set(FilmGrainShaderKeys.GrainSize, GrainSize);
            Parameters.Set(FilmGrainShaderKeys.LuminanceFactor, LuminanceFactor);
            if (Animate)
            {
                time += 0.1f;
                if (time > 100f) time = 1f;
            }
            Parameters.Set(FilmGrainShaderKeys.Time, time);

            // Copy parameters to parent
            base.UpdateParameters(context);
        }
    }
}
