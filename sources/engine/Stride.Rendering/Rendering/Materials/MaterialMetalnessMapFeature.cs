// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// A Metalness map for the specular material feature.
    /// </summary>
    [DataContract("MaterialMetalnessMapFeature")]
    [Display("Metalness Map")]
    public class MaterialMetalnessMapFeature : MaterialFeature, IMaterialSpecularFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialMetalnessMapFeature"/> class.
        /// </summary>
        public MaterialMetalnessMapFeature()
        {
            MetalnessMap = new ComputeTextureScalar();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialMetalnessMapFeature"/> class.
        /// </summary>
        /// <param name="metalnessMap">The metalness map.</param>
        public MaterialMetalnessMapFeature(IComputeScalar metalnessMap)
        {
            MetalnessMap = metalnessMap;
        }

        /// <summary>
        /// Gets or sets the metalness map.
        /// </summary>
        /// <value>The metalness map.</value>
        /// <userdoc>The map specifying the metalness of the material.</userdoc>
        [Display("Metalness Map")]
        [NotNull]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar MetalnessMap { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            if (MetalnessMap != null)
            {
                MetalnessMap.ClampFloat(0, 1);

                var computeColorSource = MetalnessMap.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.MetalnessMap, MaterialKeys.MetalnessValue));
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource("MaterialSurfaceMetalness"));
                mixin.AddComposition("metalnessMap", computeColorSource);
                context.UseStream(MaterialShaderStage.Pixel, "matSpecular");
                context.AddShaderSource(MaterialShaderStage.Pixel, mixin);
            }
        }
    }
}
