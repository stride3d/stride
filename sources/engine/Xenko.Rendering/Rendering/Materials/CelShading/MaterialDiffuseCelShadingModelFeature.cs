// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// The diffuse Cel Shading for the diffuse material model attribute.
    /// </summary>
    [DataContract("MaterialDiffuseCelShadingModelFeature")]
    [Display("Cel Shading")]
    public class MaterialDiffuseCelShadingModelFeature : MaterialFeature, IMaterialDiffuseModelFeature, IEquatable<MaterialDiffuseCelShadingModelFeature>, IEnergyConservativeDiffuseModelFeature
    {
        [DataMemberIgnore]
        bool IEnergyConservativeDiffuseModelFeature.IsEnergyConservative { get; set; }

        private bool IsEnergyConservative => ((IEnergyConservativeDiffuseModelFeature)this).IsEnergyConservative;

        /// <summary>
        /// When positive, the dot product between N and L will be modified to account for light intensity with the specified value as a factor
        /// </summary>
        [DataMember(5)]
        [Display("Modify N.L factor")]
        public float FakeNDotL { get; set; } = 0;

        [DataMember(10)]
        [Display("Ramp Function")]
        [NotNull]
        public IMaterialCelShadingLightFunction RampFunction { get; set; } = new MaterialCelShadingLightDefault();

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var shaderSource = new ShaderMixinSource();
            shaderSource.Mixins.Add(new ShaderClassSource("MaterialSurfaceShadingDiffuseCelShading", IsEnergyConservative, FakeNDotL));
            if (RampFunction != null)
            {
                shaderSource.AddComposition("celLightFunction", RampFunction.Generate(context));
            }

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.LightDependentSurface = shaderSource;
        }

        public bool Equals(MaterialDiffuseCelShadingModelFeature other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsEnergyConservative.Equals(other.IsEnergyConservative);
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return Equals(other as MaterialDiffuseCelShadingModelFeature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as MaterialDiffuseCelShadingModelFeature);
        }

        public override int GetHashCode()
        {
            var hashCode = RampFunction?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (IsEnergyConservative.GetHashCode());
            return hashCode;
        }
    }
}
