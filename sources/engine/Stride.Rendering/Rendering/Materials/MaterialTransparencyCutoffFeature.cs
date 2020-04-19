// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// A transparent cutoff material.
    /// </summary>
    [DataContract("MaterialTransparencyCutoffFeature")]
    [Display("Cutoff")]
    public class MaterialTransparencyCutoffFeature : MaterialFeature, IMaterialTransparencyFeature
    {
        private const float DefaultAlpha = 0.5f;

        private static readonly MaterialStreamDescriptor AlphaDiscardStream = new MaterialStreamDescriptor("Alpha Discard", "matAlphaDiscard", MaterialKeys.AlphaDiscardValue.PropertyType);

        private static readonly PropertyKey<bool> HasFinalCallback = new PropertyKey<bool>("MaterialTransparencyCutoffFeature.HasFinalCallback", typeof(MaterialTransparencyCutoffFeature));

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransparencyCutoffFeature"/> class.
        /// </summary>
        public MaterialTransparencyCutoffFeature()
        {
            Alpha = new ComputeFloat(DefaultAlpha);
        }

        /// <summary>
        /// Gets or sets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>The alpha threshold of the cutoff. All alpha values above this threshold are considered as fully transparent.
        /// All alpha values under this threshold are considered as fully opaque.</userdoc>
        [NotNull]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar Alpha { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var alpha = Alpha ?? new ComputeFloat(DefaultAlpha);
            alpha.ClampFloat(0, 1);
            context.SetStream(AlphaDiscardStream.Stream, alpha, MaterialKeys.AlphaDiscardMap, MaterialKeys.AlphaDiscardValue, new Color(DefaultAlpha));

            context.MaterialPass.Parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true);

            if (!context.Tags.Get(HasFinalCallback))
            {
                context.Tags.Set(HasFinalCallback, true);
                context.AddFinalCallback(MaterialShaderStage.Pixel, AddDiscardFromLuminance);
            }
        }

        private void AddDiscardFromLuminance(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceTransparentAlphaDiscard"));
        }
    }
}
