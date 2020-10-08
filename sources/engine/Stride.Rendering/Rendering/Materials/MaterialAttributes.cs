// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Graphics;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Common material attributes.
    /// </summary>
    [DataContract("MaterialAttributes")]
    [Display("Material attributes")]
    [CategoryOrder(5, "Geometry")]
    [CategoryOrder(10, "Shading")]
    [CategoryOrder(15, "Misc")]
    public class MaterialAttributes : IMaterialAttributes
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialAttributes"/> class.
        /// </summary>
        public MaterialAttributes()
        {
            CullMode = CullMode.Back;
            Overrides = new MaterialOverrides();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MaterialAttributes"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [DataMember(-20)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the tessellation.
        /// </summary>
        /// <value>The tessellation.</value>
        /// <userdoc>The method used for tessellation (subdividing model poligons to increase realism)</userdoc>
        [Display("Tessellation", "Geometry")]
        [DefaultValue(null)]
        [DataMember(10)]
        public IMaterialTessellationFeature Tessellation { get; set; }

        /// <summary>
        /// Gets or sets the displacement.
        /// </summary>
        /// <value>The displacement.</value>
        /// <userdoc>The method used for displacement (altering vertex positions by adding offsets)</userdoc>
        [Display("Displacement", "Geometry")]
        [DefaultValue(null)]
        [DataMember(20)]
        public IMaterialDisplacementFeature Displacement { get; set; }

        /// <summary>
        /// Gets or sets the surface.
        /// </summary>
        /// <value>The surface.</value>
        /// <userdoc>The method used to alter macrosurface aspects (eg perturbing the normals of the model)</userdoc>
        [Display("Surface", "Geometry")]
        [DefaultValue(null)]
        [DataMember(30)]
        public IMaterialSurfaceFeature Surface { get; set; }

        /// <summary>
        /// Gets or sets the micro surface.
        /// </summary>
        /// <value>The micro surface.</value>
        /// <userdoc>The method used to alter the material microsurface</userdoc>
        [Display("MicroSurface", "Geometry", Expand = ExpandRule.Once)]
        [DefaultValue(null)]
        [DataMember(40)]
        public IMaterialMicroSurfaceFeature MicroSurface { get; set; }

        /// <summary>
        /// Gets or sets the diffuse.
        /// </summary>
        /// <value>The diffuse.</value>
        /// <userdoc>The method used to determine the diffuse color of the material. 
        /// The diffuse color is the essential (pure) color of the object without reflections.</userdoc>
        [Display("Diffuse", "Shading", Expand = ExpandRule.Once)]
        [DefaultValue(null)]
        [DataMember(50)]
        public IMaterialDiffuseFeature Diffuse { get; set; }

        /// <summary>
        /// Gets or sets the diffuse model.
        /// </summary>
        /// <value>The diffuse model.</value>
        /// <userdoc>The shading model used to render the diffuse color.</userdoc>
        [Display("Diffuse Model", "Shading")]
        [DefaultValue(null)]
        [DataMember(60)]
        public IMaterialDiffuseModelFeature DiffuseModel { get; set; }

        /// <summary>
        /// Gets or sets the specular.
        /// </summary>
        /// <value>The specular.</value>
        /// <userdoc>The method used to determine the specular color. 
        /// This is the color produced by the reflection of a white light on the object.</userdoc>
        [Display("Specular", "Shading", Expand = ExpandRule.Once)]
        [DefaultValue(null)]
        [DataMember(70)]
        public IMaterialSpecularFeature Specular { get; set; }

        /// <summary>
        /// Gets or sets the specular model.
        /// </summary>
        /// <value>The specular model.</value>
        /// <userdoc>The shading model used to render the material specular color</userdoc>
        [Display("Specular model", "Shading", Expand = ExpandRule.Once)]
        [DefaultValue(null)]
        [DataMember(80)]
        public IMaterialSpecularModelFeature SpecularModel { get; set; }

        /// <summary>
        /// Gets or sets the occlusion.
        /// </summary>
        /// <value>The occlusion.</value>
        /// <userdoc>The occlusion method. Occlusions modulate the ambient and direct lighting of the material to simulate shadows or cavity artifacts.
        /// </userdoc>
        [Display("Occlusion", "Misc")]
        [DefaultValue(null)]
        [DataMember(90)]
        public IMaterialOcclusionFeature Occlusion { get; set; }

        /// <summary>
        /// Gets or sets the emissive.
        /// </summary>
        /// <value>The emissive.</value>
        /// <userdoc>The method used to determine the emissive color (the color emitted by the object)
        /// </userdoc>
        [Display("Emissive", "Shading")]
        [DefaultValue(null)]
        [DataMember(100)]
        public IMaterialEmissiveFeature Emissive { get; set; }
        
        [Display("Subsurface Scattering", "Shading")]
        [DefaultValue(null)]
        [DataMember(105)]
        public IMaterialSubsurfaceScatteringFeature SubsurfaceScattering { get; set; }
        
        /// <summary>
        /// Gets or sets the transparency.
        /// </summary>
        /// <value>The transparency.</value>
        /// <userdoc>The method used to determine the transparency</userdoc>
        [Display("Transparency", "Misc")]
        [DefaultValue(null)]
        [DataMember(110)]
        public IMaterialTransparencyFeature Transparency { get; set; }

        /// <summary>
        /// Gets or sets the overrides.
        /// </summary>
        /// <value>The overrides.</value>
        /// <userdoc>Override properties of the current material</userdoc>
        [Display("Overrides", "Misc")]
        [DataMember(120)]
        public MaterialOverrides Overrides { get; private set; }

        /// <summary>
        /// Gets or sets the cull mode used for the material.
        /// </summary>
        /// <userdoc>Cull some faces of the model depending on orientation</userdoc>
        [Display("Cull Mode", "Misc")]
        [DataMember(130)]
        [DefaultValue(CullMode.Back)]
        public CullMode CullMode { get; set; }

        /// <summary>
        /// Gets or sets the clear coat shading features for the material.
        /// </summary>
        /// <userdoc>Use clear-coat shading to simulate vehicle paint</userdoc>
        [Display("Clear Coat", "Misc")]
        [DefaultValue(null)]
        [DataMember(140)]
        public IMaterialClearCoatFeature ClearCoat { get; set; }

        public void Visit(MaterialGeneratorContext context)
        {
            if (!Enabled)
                return;

            // Push overrides of this attributes
            context.PushOverrides(Overrides);

            // Order is important, as some features are dependent on other
            // (For example, Specular can depend on Diffuse in case of Metalness)
            // We may be able to describe a dependency system here, but for now, assume 
            // that it won't change much so it is hardcoded

            // If Specular has energy conservative, copy this to the diffuse lambertian model
            // TODO: Should we apply it to any Diffuse Model?
            var isEnergyConservative = (Specular as MaterialSpecularMapFeature)?.IsEnergyConservative ?? false;

            var lambert = DiffuseModel as IEnergyConservativeDiffuseModelFeature;
            if (lambert != null)
            {
                lambert.IsEnergyConservative = isEnergyConservative;
            }

            // Diffuse - these 2 features are always used as a pair
            context.Visit(Diffuse);
            if (Diffuse != null)
                context.Visit(DiffuseModel);

            // Surface Geometry
            context.Visit(Tessellation);
            context.Visit(Displacement);
            context.Visit(Surface);
            context.Visit(MicroSurface);

            // Specular - these 2 features are always used as a pair
            context.Visit(Specular);
            if (Specular != null)
                context.Visit(SpecularModel);

            // Misc
            context.Visit(Occlusion);
            context.Visit(Emissive);
            context.Visit(SubsurfaceScattering);

            // If hair shading is enabled, ignore the transparency feature to avoid errors during shader compilation.
            // Allowing the transparency feature while hair shading is on makes no sense anyway.
            if (!(SpecularModel is MaterialSpecularHairModelFeature) &&
                !(DiffuseModel is MaterialDiffuseHairModelFeature))
            {
                context.Visit(Transparency);
            }

            context.Visit(ClearCoat);

            // Pop overrides
            context.PopOverrides();

            // Only set the cullmode to something 
            if (context.Step == MaterialGeneratorStep.GenerateShader && CullMode != CullMode.Back)
            {
                if (context.MaterialPass.CullMode == null)
                {
                    context.MaterialPass.CullMode = CullMode;
                }
            }
        }
    }
}
