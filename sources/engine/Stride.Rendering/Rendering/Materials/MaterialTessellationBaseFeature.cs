// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Tessellation;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// The displacement map for a surface material feature.
    /// </summary>
    [DataContract("MaterialTesselationFeature")]
    public abstract class MaterialTessellationBaseFeature : MaterialFeature, IMaterialTessellationFeature
    {
        private static readonly PropertyKey<bool> HasFinalCallback = new PropertyKey<bool>("MaterialTessellationBaseFeature.HasFinalCallback", typeof(MaterialTessellationBaseFeature));

        protected MaterialTessellationBaseFeature()
        {
            TriangleSize = 12f;
        }

        /// <summary>
        /// Gets or sets the desired triangle size.
        /// </summary>
        /// <userdoc>
        /// The desired triangles' size in pixels. This drives the tessellation factor.
        /// </userdoc>
        [DataMember(10)]
        [DataMemberRange(1, 100, 1, 5, 2)]
        [Display("Triangle Size")]
        public float TriangleSize { get; set; }

        /// <summary>
        /// Gets or sets the adjacent edges average activation state.
        /// </summary>
        /// <userdoc>
        /// Indicate if average should be performed on adjacent edges to prevent tessellation cracks.
        /// </userdoc>
        [DataMember(20)]
        [Display("Adjacent Edges Average")]
        public bool AdjacentEdgeAverage { get; set; }

        protected bool hasAlreadyTessellationFeature;

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            // determine if an tessellation material have already been added in another layer
            hasAlreadyTessellationFeature = context.GetStreamFinalModifier<MaterialTessellationBaseFeature>(MaterialShaderStage.Domain) != null;

            // Notify problem on multiple tessellation techniques and return
            if (hasAlreadyTessellationFeature)
            {
                context.Log.Warning("A material cannot have more than one layer performing tessellation. The first tessellation method found, will be used.");
                return;
            }

            // reset the tessellation stream at the beginning of the stage
            context.AddStreamInitializer(MaterialShaderStage.Domain, "MaterialTessellationStream");

            // set the desired triangle size desired for this material
            context.Parameters.Set(TessellationKeys.DesiredTriangleSize, TriangleSize);

            // set the tessellation method and callback to add Displacement/Normal average shaders.
            if (AdjacentEdgeAverage && !context.Tags.Get(HasFinalCallback))
            {
                context.Tags.Set(HasFinalCallback, true);
                context.MaterialPass.TessellationMethod = StrideTessellationMethod.AdjacentEdgeAverage;
                context.AddFinalCallback(MaterialShaderStage.Domain, AddAdjacentEdgeAverageMacros);
                context.AddFinalCallback(MaterialShaderStage.Domain, AddAdjacentEdgeAverageShaders);
            }
        }

        public void AddAdjacentEdgeAverageShaders(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            var tessellationShader = context.Parameters.Get(MaterialKeys.TessellationShader) as ShaderMixinSource;
            if (tessellationShader == null)
                return;

            if (context.GetStreamFinalModifier<MaterialDisplacementMapFeature>(MaterialShaderStage.Domain) != null)
            {
                tessellationShader.Mixins.Add(new ShaderClassSource("TessellationAE2", "TexCoord")); // this suppose Displacement from Texture -> TODO make it more flexible so that it works with any kind of displacement.
                tessellationShader.Mixins.Add(new ShaderClassSource("TessellationAE3", "normalWS"));
            }
        }

        public void AddAdjacentEdgeAverageMacros(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            var tessellationShader = context.Parameters.Get(MaterialKeys.TessellationShader) as ShaderMixinSource;
            if (tessellationShader == null)
                return;

            tessellationShader.Macros.Add(new ShaderMacro("InputControlPointCount", 12));
        }
    }
}
