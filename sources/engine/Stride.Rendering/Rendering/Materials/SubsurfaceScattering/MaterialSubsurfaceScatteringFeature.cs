// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    [DataContract("MaterialSubsurfaceScatteringFeature")]
    [Display("Subsurface Scattering")]
    public class MaterialSubsurfaceScatteringFeature : MaterialFeature, IMaterialSubsurfaceScatteringFeature, IMaterialStreamProvider
    {
        private static readonly MaterialStreamDescriptor ScatteringStrengthStream = new MaterialStreamDescriptor("ScatteringStrength", "matScatteringStrength", MaterialKeys.ScatteringStrengthValue.PropertyType);

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        //public IServiceRegistry Services { get; set; }

        public bool IsLightDependent => true;

        /// <summary>
        /// Width of the scattering kernel in world space.
        /// </summary>
        /// <userdoc>
        /// This parameter controls how far the light should scatter (in meters).
        /// </userdoc>
        [DataMember(5)]
        [Display("Scattering width")]
        [DefaultValue(0.015f)]
        [DataMemberRange(0.001, 0.5, 0.001, 0.01, 4)]
        public float ScatteringWidth { get; set; } = 0.015f;

        /// <summary>
        /// Translucency of the material.
        /// </summary>
        /// <userdoc>
        /// This parameter controls how translucent the material is.
        /// "0.0" stands for no translucency and "1.0" stands for maximum translucency.
        /// </userdoc>
        [DataMember(10)]
        [Display("Translucency")]
        [DataMemberRange(0.0, 1.0, 0.001, 0.01, 3)]
        public float Translucency { get; set; } = 0.83f;

        /// <summary>
        /// Controls the thickness of the object. This value gets multiplied with the "Translucency" parameter.
        /// </summary>
        /// <userdoc>
        /// This grayscale map controls how translucent different regions of the model are.
        /// A brighter value results in more, and a darker one in less scattering.
        /// For example the ears of a person should scatter more than the top of the head, because they are thinner and therefore light passes through them more easily.
        /// This texture is multiplied with the "Translucency" parameter.
        /// </userdoc>
        [NotNull]
        [DataMember(20)]
        [Display("Translucency map")]
        public IComputeScalar TranslucencyMap { get; set; } = new ComputeTextureScalar();   // IComputeScalar instead of IMaterialSubsurfaceScatteringStrengthMap, because this way the editor displays it more nicely.

        /// <summary>
        /// The profile mixin to use for the scattering calculations.
        /// </summary>
        /// <userdoc>
        /// The scattering profile to use during the forward render pass.
        /// </userdoc>
        [NotNull]
        [DataMember(25)]
        [Display("Scattering profile")]
        public IMaterialSubsurfaceScatteringScatteringProfile ProfileFunction { get; set; } = new MaterialSubsurfaceScatteringScatteringProfileSkin();

        /// <summary>
        /// Generates the scattering kernel.
        /// </summary>
        /// <userdoc>
        /// The scattering kernel to use in the SSS post-process.
        /// </userdoc>
        [NotNull]
        [DataMember(30)]
        [Display("Scattering kernel")]
        public IMaterialSubsurfaceScatteringScatteringKernel KernelFunction { get; set; } = new MaterialSubsurfaceScatteringScatteringKernelSkin();

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var shaderSource = new ShaderMixinSource();
            shaderSource.Mixins.Add(new ShaderClassSource("MaterialSurfaceSubsurfaceScatteringShading"));

            TranslucencyMap.ClampFloat(0.0f, 1.0f);
            context.SetStream(ScatteringStrengthStream.Stream, TranslucencyMap, MaterialKeys.ScatteringStrengthMap, MaterialKeys.ScatteringStrengthValue);

            // TODO: Generate a hash with the scattering kernel and scattering width!

            var parameters = context.MaterialPass.Parameters;
            parameters.Set(Stride.Rendering.Materials.MaterialSurfaceSubsurfaceScatteringShadingKeys.Translucency, Translucency);
            parameters.Set(Stride.Rendering.Materials.MaterialSurfaceSubsurfaceScatteringShadingKeys.ScatteringWidth, ScatteringWidth);

            // Generate and set the scattering profile:
            shaderSource.AddComposition("scatteringProfileFunction", ProfileFunction.Generate(context));

            // Generate and set the scattering kernel:
            Vector4[] scatteringKernel = KernelFunction.Generate();
            parameters.Set(Stride.Rendering.Materials.MaterialSurfaceSubsurfaceScatteringShadingKeys.ScatteringKernel, scatteringKernel);

            /*
            {
                Game game = Services.GetSafeServiceAs<Game>();
                SubsurfaceScatteringSettings settings = game.Settings.Configurations.Get<SubsurfaceScatteringSettings>();
                //SubsurfaceScatteringSettings settings = services.GetSafeServiceAs<SubsurfaceScatteringSettings>();  // TODO: Query the settings like this once the system is ready.
            }
            */

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.LightDependentSurface = shaderSource;
        }

        protected bool Equals(MaterialSubsurfaceScatteringFeature other)
        {
            return ScatteringWidth.Equals(other.ScatteringWidth) && Translucency.Equals(other.Translucency) && TranslucencyMap.Equals(other.TranslucencyMap) && ProfileFunction.Equals(other.ProfileFunction) && KernelFunction.Equals(other.KernelFunction);
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return Equals(other as MaterialSubsurfaceScatteringFeature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MaterialSubsurfaceScatteringFeature)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ScatteringWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ Translucency.GetHashCode();
                hashCode = (hashCode * 397) ^ TranslucencyMap.GetHashCode();
                hashCode = (hashCode * 397) ^ ProfileFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ KernelFunction.GetHashCode();
                return hashCode;
            }
        }
        
        public IEnumerable<MaterialStreamDescriptor> GetStreams()
        {
            yield return ScatteringStrengthStream;
        }
    }
}
