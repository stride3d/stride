// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// The microfacet specular shading model.
    /// </summary>
    [DataContract("MaterialSpecularMicrofacetModelFeature")]
    [Display("Microfacet")]
    public class MaterialSpecularMicrofacetModelFeature : MaterialFeature, IMaterialSpecularModelFeature, IEquatable<MaterialSpecularMicrofacetModelFeature>
    {
        /// <userdoc>Specify the function to use to calculate the Fresnel component of the micro-facet lighting equation. 
        /// This defines the amount of the incoming light that is reflected.</userdoc>
        [DataMember(10)]
        [Display("Fresnel")]
        [NotNull]
        public IMaterialSpecularMicrofacetFresnelFunction Fresnel { get; set; } = new MaterialSpecularMicrofacetFresnelSchlick();

        /// <userdoc>Specify the function to use to calculate the visibility component of the micro-facet lighting equation.</userdoc>
        [DataMember(20)]
        [Display("Visibility")]
        [NotNull]
        public IMaterialSpecularMicrofacetVisibilityFunction Visibility { get; set; } = new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX();

        /// <userdoc>Specify the function to use to calculate the normal distribution in the micro-facet lighting equation. 
        /// This defines how the normal is distributed.</userdoc>
        [DataMember(30)]
        [Display("Normal Distribution")]
        [NotNull]
        public IMaterialSpecularMicrofacetNormalDistributionFunction NormalDistribution { get; set; } = new MaterialSpecularMicrofacetNormalDistributionGGX();

        /// <userdoc>Specify the function to use to calculate the environment DFG term in the micro-facet lighting equation. 
        /// This defines how the material reflects specular cubemaps.</userdoc>
        [DataMember(40)]
        [Display("Environment (DFG)")]
        [NotNull]
        public IMaterialSpecularMicrofacetEnvironmentFunction Environment { get; set; } = new MaterialSpecularMicrofacetEnvironmentGGXLUT();

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var shaderSource = new ShaderMixinSource();
            shaderSource.Mixins.Add(new ShaderClassSource("MaterialSurfaceShadingSpecularMicrofacet"));

            GenerateShaderCompositions(context, shaderSource);

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.LightDependentSurface = shaderSource;
        }

        protected virtual void GenerateShaderCompositions(MaterialGeneratorContext context, ShaderMixinSource shaderSource)
        {
            if (Fresnel != null)
            {
                shaderSource.AddComposition("fresnelFunction", Fresnel.Generate(context));
            }

            if (Visibility != null)
            {
                shaderSource.AddComposition("geometricShadowingFunction", Visibility.Generate(context));
            }

            if (NormalDistribution != null)
            {
                shaderSource.AddComposition("normalDistributionFunction", NormalDistribution.Generate(context));
            }

            if (Environment != null)
            {
                shaderSource.AddComposition("environmentFunction", Environment.Generate(context));
            }
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return Equals((object)other);
        }

        public bool Equals(MaterialSpecularMicrofacetModelFeature other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Fresnel, other.Fresnel) && Equals(Visibility, other.Visibility) && Equals(NormalDistribution, other.NormalDistribution) && Equals(Environment, other.Environment);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals(obj as MaterialSpecularMicrofacetModelFeature);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Fresnel != null ? Fresnel.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Visibility != null ? Visibility.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NormalDistribution != null ? NormalDistribution.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Environment != null ? Environment.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
