// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// The displacement map for a surface material feature.
    /// </summary>
    [DataContract("MaterialDisplacementMapFeature")]
    [Display("Displacement Map")]
    public class MaterialDisplacementMapFeature : MaterialFeature, IMaterialDisplacementFeature
    {
        public const string DisplacementStream = "matDisplacement";
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialDisplacementMapFeature"/> class.
        /// </summary>
        public MaterialDisplacementMapFeature() : this(new ComputeTextureScalar())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialDisplacementMapFeature"/> class.
        /// </summary>
        /// <param name="displacementMap">The displacement map.</param>
        public MaterialDisplacementMapFeature(IComputeScalar displacementMap)
        {
            ScaleAndBias = true;
            DisplacementMap = displacementMap;
            Intensity = new ComputeFloat(1.0f);
            Stage = DisplacementMapStage.Vertex;
        }

        /// <summary>
        /// Gets or sets the displacement map.
        /// </summary>
        /// <value>The displacement map.</value>
        /// <userdoc>
        /// The map containing the displacement offsets to apply onto the model vertex positions.
        /// </userdoc>
        [DataMember(10)]
        [Display("Displacement Map")]
        [NotNull]
        public IComputeScalar DisplacementMap { get; set; }

        /// <summary>
        /// Gets or sets the displacement map.
        /// </summary>
        /// <value>The displacement map.</value>
        /// <userdoc>
        /// The intensity factor of the displacement. This scales the offset introduced by displacement map.
        /// </userdoc>
        [DataMember(20)]
        [Display("Intensity")]
        [NotNull]
        public IComputeScalar Intensity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to scale by (2,2,2) and bias by (-1,-1,-1) the displacement map.
        /// </summary>
        /// <value><c>true</c> if scale and bias this displacement map; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Scale by (2,2,2) and bias by (-1,-1,-1) this displacement map.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(true)]
        [Display("Scale & Bias")]
        public bool ScaleAndBias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating in which stage the displacement should occur.
        /// </summary>
        /// <userdoc>
        /// The value indicating in which stage the displacement will occur.
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(true)]
        [Display("Shader Stage")]
        public DisplacementMapStage Stage { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            if (DisplacementMap == null)
                return;

            var materialStage = (MaterialShaderStage)Stage;

            // reset the displacement streams at the beginning of the stage
            context.AddStreamInitializer(materialStage, "MaterialDisplacementStream");

            // set the blending mode of displacement map to additive (and not default linear blending)
            context.UseStreamWithCustomBlend(materialStage, DisplacementStream, new ShaderClassSource("MaterialStreamAdditiveBlend", DisplacementStream));

            // build the displacement computer
            var displacement = DisplacementMap;
            if (ScaleAndBias) // scale and bias should be done by layer
            {
                displacement = new ComputeBinaryScalar(displacement, new ComputeFloat(2f), BinaryOperator.Multiply);
                displacement = new ComputeBinaryScalar(displacement, new ComputeFloat(1f), BinaryOperator.Subtract);
            }
            displacement = new ComputeBinaryScalar(displacement, Intensity, BinaryOperator.Multiply);

            // Workaround to inform compute colors that sampling is occurring from a vertex shader
            context.IsNotPixelStage = materialStage != MaterialShaderStage.Pixel;
            context.SetStream(materialStage, DisplacementStream, displacement, MaterialKeys.DisplacementMap, MaterialKeys.DisplacementValue);
            context.IsNotPixelStage = false;

            var scaleNormal = materialStage != MaterialShaderStage.Vertex;
            var positionMember = materialStage == MaterialShaderStage.Vertex ? "Position" : "PositionWS";
            var normalMember = materialStage == MaterialShaderStage.Vertex ? "meshNormal" : "normalWS";
            context.SetStreamFinalModifier<MaterialDisplacementMapFeature>(materialStage, new ShaderClassSource("MaterialSurfaceDisplacement", positionMember, normalMember, scaleNormal));
        }
    }
}
