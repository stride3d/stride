// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// An occlusion map for the occlusion material feature.
    /// </summary>
    [DataContract("MaterialOcclusionMapFeature")]
    [Display("Occlusion Map")]
    public class MaterialOcclusionMapFeature : MaterialFeature, IMaterialOcclusionFeature, IMaterialStreamProvider
    {
        private static readonly MaterialStreamDescriptor OcclusionStream = new MaterialStreamDescriptor("Occlusion", "matAmbientOcclusion", MaterialKeys.AmbientOcclusionValue.PropertyType);
        private static readonly MaterialStreamDescriptor CavityStream = new MaterialStreamDescriptor("Cavity", "matCavity", MaterialKeys.CavityValue.PropertyType);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialOcclusionMapFeature"/> class.
        /// </summary>
        public MaterialOcclusionMapFeature()
        {
            AmbientOcclusionMap = new ComputeTextureScalar();
            DirectLightingFactor = new ComputeFloat(0.0f);
            CavityMap = new ComputeTextureScalar();
            DiffuseCavity = new ComputeFloat(1.0f);
            SpecularCavity = new ComputeFloat(1.0f);
        }

        /// <summary>
        /// Gets or sets the occlusion map.
        /// </summary>
        /// <value>The occlusion map.</value>
        /// <userdoc>The map specifying the ambient occlusion of the material. This modulates the amount of incoming ambient light to the material (0 => no ambient, 1 => full ambient).
        /// Ambient occlusions are generally used to produce coarse occlusions on the material (shadows, etc...). It is geometry related and thus ignores possible UV scale overrides.</userdoc>
        [Display("Occlusion Map")]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar AmbientOcclusionMap { get; set; }

        /// <summary>
        /// Gets or sets how much the occlusion map can influence direct lighting (default: 0).
        /// </summary>
        /// <value>The direct lighting factor.</value>
        /// <userdoc>Specify how much the occlusion map should influence the direct lighting (non ambient lightings). 
        /// Usually the occlusion maps are used only to affect ambient lighting, but using this parameter one can also have it partially affecting the direct lighting.</userdoc>
        [Display("Direct Lighting Influence")]
        [DataMember(15)]
        [NotNull]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar DirectLightingFactor { get; set; }

        /// <summary>
        /// Gets or sets the cavity map.
        /// </summary>
        /// <value>The cavity map.</value>
        /// <userdoc>The map specifying the cavity occlusions of the material. This modulates the amount of incoming direct (non-ambient) light to the material (0 => no light, 1 => full light).
        /// Cavity occlusions are generally used to produce fine grained artifacts on the material.</userdoc>
        [Display("Cavity Map")]
        [DataMember(20)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar CavityMap { get; set; }

        /// <summary>
        /// Gets or sets the diffuse cavity influence.
        /// </summary>
        /// <value>The diffuse cavity.</value>
        /// <userdoc>Specify the influence of the cavity map on the diffuse lighting (0 => no influence, 1 => full influence).</userdoc>
        [Display("Diffuse Cavity")]
        [DataMember(30)]
        [NotNull]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar DiffuseCavity { get; set; }

        /// <summary>
        /// Gets or sets the specular cavity.
        /// </summary>
        /// <value>The specular cavity.</value>
        /// <userdoc>Specify the influence of the cavity map on the specular lighting (0 => no influence, 1 => full influence).</userdoc>
        [Display("Specular Cavity")]
        [DataMember(40)]
        [NotNull]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar SpecularCavity { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            // Exclude ambient occlusion from uv-scale overrides
            var revertOverrides = new MaterialOverrides();
            revertOverrides.UVScale = 1.0f / context.CurrentOverrides.UVScale;

            context.PushOverrides(revertOverrides);
            context.SetStream(OcclusionStream.Stream, AmbientOcclusionMap, MaterialKeys.AmbientOcclusionMap, MaterialKeys.AmbientOcclusionValue, Color.White);
            context.PopOverrides();

            context.SetStream("matAmbientOcclusionDirectLightingFactor", DirectLightingFactor, null, MaterialKeys.AmbientOcclusionDirectLightingFactorValue);

            if (CavityMap != null)
            {
                context.SetStream(CavityStream.Stream, CavityMap, MaterialKeys.CavityMap, MaterialKeys.CavityValue, Color.White);
                context.SetStream("matCavityDiffuse", DiffuseCavity, null, MaterialKeys.CavityDiffuseValue);
                context.SetStream("matCavitySpecular", SpecularCavity, null, MaterialKeys.CavitySpecularValue);
            }
        }

        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return OcclusionStream;
            yield return CavityStream;
        }
    }
}
