// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Images.SphericalHarmonics
{
    public class SphericalHarmonicsRendererEffect : ImageEffectShader
    {
        /// <summary>
        /// Gets or sets the harmonic order to use during the filtering.
        /// </summary>
        public Core.Mathematics.SphericalHarmonics InputSH { get; set; }

        public SphericalHarmonicsRendererEffect()
        {
            EffectName = "SphericalHarmonicsRendererEffect";
        }

        protected override void UpdateParameters()
        {
            base.UpdateParameters();

            if (InputSH != null)
            {
                Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, InputSH.Order);
                Parameters.Set(SphericalHarmonicsRendererKeys.SHCoefficients, InputSH.Coefficients);
            }
            else
            {
                Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, 1);
                Parameters.Set(SphericalHarmonicsRendererKeys.SHCoefficients, new[] { new Color3() });
            }
        }
    }
}
