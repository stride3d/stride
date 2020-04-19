// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// A Specular map for the specular material feature.
    /// </summary>
    [DataContract("MaterialSpecularMapFeature")]
    [Display("Specular Map")]
    public class MaterialSpecularMapFeature : MaterialFeature, IMaterialSpecularFeature, IMaterialStreamProvider
    {
        private static readonly MaterialStreamDescriptor SpecularStream = new MaterialStreamDescriptor("Specular", "matSpecular", MaterialKeys.SpecularValue.PropertyType);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialSpecularMapFeature"/> class.
        /// </summary>
        public MaterialSpecularMapFeature()
        {
            SpecularMap = new ComputeTextureColor();
            Intensity = new ComputeFloat(1.0f);
            IsEnergyConservative = true;
        }

        /// <summary>
        /// Gets or sets the specular map.
        /// </summary>
        /// <value>The specular map.</value>
        /// <userdoc>The map specifying the color of the specular reflection.</userdoc>
        [DataMember(10)]
        [Display("Specular Map")]
        [NotNull]
        public IComputeColor SpecularMap { get; set; }

        /// <summary>
        /// Gets or sets the specular intensity.
        /// </summary>
        /// <value>The map specifying the intensity of the specular reflection. An intensity of 0 means no reflection. An intensity of 1 means full reflection.</value>
        [DataMember(20)]
        [NotNull]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public IComputeScalar Intensity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is energy conservative.
        /// </summary>
        /// <value><c>true</c> if this instance is energy conservative; otherwise, <c>false</c>.</value>
        /// <value>Conserve energy between the diffuse and specular colors</value>
        [DataMember(30)]
        [DefaultValue(true)]
        [Display("Energy conservative")]
        public bool IsEnergyConservative { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            Intensity.ClampFloat(0, 1);

            context.SetStream(SpecularStream.Stream, SpecularMap, MaterialKeys.SpecularMap, MaterialKeys.SpecularValue);
            context.SetStream("matSpecularIntensity", Intensity, null, MaterialKeys.SpecularIntensityValue);
        }

        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return SpecularStream;
        }
    }
}
