// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// The diffuse Lambertian for the diffuse material model attribute.
    /// </summary>
    [DataContract("MaterialDiffuseLambertModelFeature")]
    [Display("Lambert")]
    public class MaterialDiffuseLambertModelFeature : MaterialFeature, IMaterialDiffuseModelFeature, IEnergyConservativeDiffuseModelFeature
    {
        [DataMemberIgnore]
        bool IEnergyConservativeDiffuseModelFeature.IsEnergyConservative { get; set; }

        private bool IsEnergyConservative => ((IEnergyConservativeDiffuseModelFeature)this).IsEnergyConservative;

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var shaderBuilder = context.AddShading(this);
            shaderBuilder.LightDependentSurface = new ShaderClassSource("MaterialSurfaceShadingDiffuseLambert", IsEnergyConservative);
        }

        public bool Equals(MaterialDiffuseLambertModelFeature other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsEnergyConservative.Equals(other.IsEnergyConservative);
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return Equals(other as MaterialDiffuseLambertModelFeature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as MaterialDiffuseLambertModelFeature);
        }

        public override int GetHashCode()
        {
            return IsEnergyConservative.GetHashCode();
        }
    }
}
