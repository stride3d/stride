// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Rendering.Materials.ComputeColors;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// The normal map for a surface material feature.
    /// </summary>
    [DataContract("MaterialNormalMapFeature")]
    [Display("Normal Map")]
    public class MaterialNormalMapFeature : MaterialFeature, IMaterialSurfaceFeature, IMaterialStreamProvider
    {
        private static readonly MaterialStreamDescriptor NormalStream = new MaterialStreamDescriptor("Normal (tangent)", "matNormal", MaterialKeys.NormalValue.PropertyType, true);
        private static readonly MaterialStreamDescriptor NormalStreamWorld = new MaterialStreamDescriptor("Normal (world)", "NormalStream.normalWS", new ShaderClassSource("MaterialSurfaceNormalStreamShading"));

        public static readonly Color DefaultNormalColor = new Color(0x80, 0x80, 0xFF, 0xFF);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialNormalMapFeature"/> class.
        /// </summary>
        public MaterialNormalMapFeature() : this(new ComputeTextureColor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialNormalMapFeature"/> class.
        /// </summary>
        /// <param name="normalMap">The normal map.</param>
        public MaterialNormalMapFeature(IComputeColor normalMap)
        {
            ScaleAndBias = true;
            NormalMap = normalMap;
        }

        /// <summary>
        /// Gets or sets the normal map.
        /// </summary>
        /// <value>The normal map.</value>
        /// <userdoc>
        /// The normal map.
        /// </userdoc>
        [DataMember(10)]
        [Display("Normal Map")]
        [NotNull]
        public IComputeColor NormalMap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to scale by (2,2) and offset by (-1,-1) the normal map.
        /// </summary>
        /// <value><c>true</c> if scale and offset this normal map; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Scale the XY by (2,2) and offset by (-1,-1). Required to unpack unsigned values of [0..1] to signed coordinates of [-1..+1].
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(true)]
        [Display("Scale & Offset")]
        public bool ScaleAndBias { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the normal is only stored in XY components and Z is assumed to be sqrt(1 - x*x - y*y).
        /// </summary>
        /// <value><c>true</c> if this instance is xy normal; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// If there's no Z component in the texture, reconstruct it from the X and Y components. This assumes that Z = sqrt(1 - x*x - y*y) and that Z is always positive, so no normal vector can point to the back side of the surface. We recommend you enable this option, as Xenko might remove the Z component when you compress normal maps.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(false)]
        [Display("Reconstruct Z")]
        public bool IsXYNormal { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            if (NormalMap != null)
            {
                // Inform the context that we are using matNormal (from the MaterialSurfaceNormalMap shader)
                context.UseStreamWithCustomBlend(MaterialShaderStage.Pixel, NormalStream.Stream, new ShaderClassSource("MaterialStreamNormalBlend"));
                context.Parameters.Set(MaterialKeys.HasNormalMap, true);

                var normalMap = NormalMap;
                // Workaround to make sure that normal map are setup 
                var computeTextureColor = normalMap as ComputeTextureColor;
                if (computeTextureColor != null)
                {
                    if (computeTextureColor.FallbackValue.Value == Color.White)
                    {
                        computeTextureColor.FallbackValue.Value = DefaultNormalColor;
                    }
                }
                else
                {
                    var computeColor = normalMap as ComputeColor;
                    if (computeColor != null)
                    {
                        if (computeColor.Value == Color.Black || computeColor.Value == Color.White)
                        {
                            computeColor.Value = DefaultNormalColor;
                        }
                    }
                    else
                    {
                        var computeFloat4 = normalMap as ComputeFloat4;
                        if (computeFloat4 != null)
                        {
                            if (computeFloat4.Value == Vector4.Zero)
                            {
                                computeFloat4.Value = DefaultNormalColor.ToVector4();
                            }
                        }
                    }
                }

                var computeColorSource = NormalMap.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.NormalMap, MaterialKeys.NormalValue, DefaultNormalColor, false));
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource("MaterialSurfaceNormalMap", IsXYNormal, ScaleAndBias));
                mixin.AddComposition("normalMap", computeColorSource);
                context.AddShaderSource(MaterialShaderStage.Pixel, mixin);
            }
        }

        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return NormalStream;
            yield return NormalStreamWorld;
        }
    }
}
