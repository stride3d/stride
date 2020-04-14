// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// A smoothness map for the micro-surface material feature.
    /// </summary>
    [DataContract("MaterialGlossinessMapFeature")]
    [Display("Gloss map")]
    public class MaterialGlossinessMapFeature : MaterialFeature, IMaterialMicroSurfaceFeature, IMaterialStreamProvider
    {
        private static readonly MaterialStreamDescriptor GlossinessStream = new MaterialStreamDescriptor("Glossiness", "matGlossiness", MaterialKeys.GlossinessValue.PropertyType);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialGlossinessMapFeature"/> class.
        /// </summary>
        public MaterialGlossinessMapFeature()
        {
            GlossinessMap = new ComputeTextureScalar();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialGlossinessMapFeature"/> class.
        /// </summary>
        /// <param name="glossinessMap">The glossiness map.</param>
        public MaterialGlossinessMapFeature(IComputeScalar glossinessMap)
        {
            GlossinessMap = glossinessMap;
        }

        /// <summary>
        /// Gets or sets the smoothness map.
        /// </summary>
        /// <value>The smoothness map.</value>
        [Display("Gloss map")]
        [NotNull]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar GlossinessMap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MaterialGlossinessMapFeature"/> is invert.
        /// </summary>
        /// <value><c>true</c> if invert; otherwise, <c>false</c>.</value>
        /// <userdoc>Consider the map as a roughness map instead of a gloss map. 
        /// A roughness value of 1.0 corresponds to a gloss value of 0.0 and vice-versa.</userdoc>
        [Display("Invert")]
        [DefaultValue(false)]
        public bool Invert { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            if (GlossinessMap != null)
            {
                GlossinessMap.ClampFloat(0, 1);

                context.UseStream(MaterialShaderStage.Pixel, GlossinessStream.Stream);
                var computeColorSource = GlossinessMap.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.GlossinessMap, MaterialKeys.GlossinessValue));
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource("MaterialSurfaceGlossinessMap", Invert));
                mixin.AddComposition("glossinessMap", computeColorSource);
                context.AddShaderSource(MaterialShaderStage.Pixel, mixin);
            }
        }

        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return GlossinessStream;
        }
    }
}
