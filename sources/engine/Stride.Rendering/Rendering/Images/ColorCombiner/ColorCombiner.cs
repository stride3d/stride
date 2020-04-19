// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
namespace Stride.Rendering.Images
{
    /// <summary>
    /// A texture combiner allows to mix up to 10 textures with different weights.
    /// </summary>
    /// <remarks> This effects takes from 1 to 10 textures in input and combine them to a single output.
    /// Before using this class, it is recommended to clear the inputs by using <see cref="ImageEffect.Reset"/>.
    /// </remarks>
    public class ColorCombiner : ImageEffectShader
    {
        internal static readonly PermutationParameterKey<int> FactorCount = ParameterKeys.NewPermutation(0);

        private readonly float[] factors;

        private readonly Color3[] modulateRGB;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorCombiner"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="colorCombinerShaderName">Name of the color combiner shader.</param>
        public ColorCombiner(string colorCombinerShaderName = "ColorCombinerEffect")
        {
            EffectName = colorCombinerShaderName;
            factors = new float[TexturingKeys.DefaultTextures.Count];
            modulateRGB = new Color3[TexturingKeys.DefaultTextures.Count];
            for (int i = 0; i < TexturingKeys.DefaultTextures.Count; i++)
            {
                factors[i] = 1f;
                modulateRGB[i] = new Color3(1f, 1f, 1f);
            }
        }

        /// <summary>
        /// Gets the factors used to multiply the colors.
        /// </summary>
        /// <value>The factors.</value>
        public float[] Factors
        {
            get
            {
                return factors;
            }
        }

        /// <summary>
        /// Gets the RGB modulation of each texture.
        /// </summary>
        public Color3[] ModulateRGB
        {
            get
            {
                return modulateRGB;
            }
        }

        protected override void UpdateParameters()
        {
            base.UpdateParameters();
            int value = InputCount;
            Parameters.Set(FactorCount, value);
            Parameters.Set(ColorCombinerShaderKeys.Factors, factors);
            Parameters.Set(ColorCombinerShaderKeys.ModulateRGB, ModulateRGB);
        }
    }
}
