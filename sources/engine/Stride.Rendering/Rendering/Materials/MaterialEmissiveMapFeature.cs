// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    [DataContract("MaterialEmissiveMapFeature")]
    [Display("Emissive Map")]
    public class MaterialEmissiveMapFeature : MaterialFeature, IMaterialEmissiveFeature, IMaterialStreamProvider
    {
        private static readonly MaterialStreamDescriptor EmissiveStream = new MaterialStreamDescriptor("Emissive", "matEmissive", MaterialKeys.EmissiveValue.PropertyType);

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEmissiveMapFeature"/> class.
        /// </summary>
        public MaterialEmissiveMapFeature() : this(new ComputeTextureColor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEmissiveMapFeature"/> class.
        /// </summary>
        /// <param name="emissiveMap">The emissive map.</param>
        /// <exception cref="System.ArgumentNullException">emissiveMap</exception>
        public MaterialEmissiveMapFeature(IComputeColor emissiveMap)
        {
            if (emissiveMap == null) throw new ArgumentNullException("emissiveMap");
            EmissiveMap = emissiveMap;
            Intensity = new ComputeFloat(1.0f);
            UseAlpha = false;
        }

        /// <summary>
        /// Gets or sets the diffuse map.
        /// </summary>
        /// <value>The diffuse map.</value>
        /// <userdoc>The map specifying the color emitted by the material.</userdoc>
        [Display("Emissive Map")]
        [NotNull]
        [DataMember(10)]
        public IComputeColor EmissiveMap { get; set; }

        /// <summary>
        /// Gets or sets the intensity.
        /// </summary>
        /// <value>The intensity.</value>
        /// <userdoc>The map specifying the intensity of the light emitted by the material. This scales the color value specified by emissive map.</userdoc>
        [Display("Intensity")]
        [NotNull]
        [DataMember(20)]
        public IComputeScalar Intensity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the alpha component of the emissive map as main alpha color for the material.
        /// </summary>
        /// <value><c>true</c> if [use alpha]; otherwise, <c>false</c>.</value>
        /// <userdoc>Use the emissive map alpha component as main alpha color for the material. Otherwise, use the diffuse alpha color.</userdoc>
        [DataMember(30)]
        [DefaultValue(false)]
        public bool UseAlpha { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            Vector4 emissiveMin = Vector4.Zero;
            Vector4 emissiveMax = new Vector4(float.MaxValue);
            EmissiveMap.ClampFloat4(ref emissiveMin, ref emissiveMax);
            Intensity.ClampFloat(0, float.MaxValue);

            context.SetStream(EmissiveStream.Stream, EmissiveMap, MaterialKeys.EmissiveMap, MaterialKeys.EmissiveValue);
            context.SetStream("matEmissiveIntensity", Intensity, MaterialKeys.EmissiveIntensityMap, MaterialKeys.EmissiveIntensity);

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.ShaderSources.Add(new ShaderClassSource("MaterialSurfaceEmissiveShading", UseAlpha));
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return other is MaterialEmissiveMapFeature;
        }

        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return EmissiveStream;
        }
    }
}
