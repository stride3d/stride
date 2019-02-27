// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Rendering.Materials.ComputeColors;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// A material blend layer
    /// </summary>
    [DataContract("MaterialBlendLayer")]
    [Display("Material Layer")]
    public class MaterialBlendLayer : IMaterialShaderGenerator
    {
        internal const string BlendStream = "matBlend";

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialBlendLayer"/> class.
        /// </summary>
        public MaterialBlendLayer()
        {
            Enabled = true;
            BlendMap = new ComputeTextureScalar();
            Overrides = new MaterialOverrides();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MaterialBlendLayer"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        /// <userdoc>Take the layer into account; otherwise ignore it</userdoc>
        [DefaultValue(true)]
        [DataMember(10)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the name of this blend layer.
        /// </summary>
        /// <value>The name.</value>
        /// <userdoc>The name of the material layer</userdoc>
        [DefaultValue(null)]
        [DataMember(20)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the material.
        /// </summary>
        /// <value>The material.</value>
        /// <userdoc>The reference to the material asset to layer.</userdoc>
        [DefaultValue(null)]
        [DataMember(30)]
        [InlineProperty]
        public Material Material { get; set; }

        /// <summary>
        /// Gets or sets the blend map.
        /// </summary>
        /// <value>The blend map.</value>
        /// <userdoc>The blend map specifying how to blend the material with the previous layer.</userdoc>
        [Display("Blend Map")]
        [DefaultValue(null)]
        [DataMember(40)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar BlendMap { get; set; }

        /// <summary>
        /// Gets or sets the material overrides.
        /// </summary>
        /// <value>The overrides.</value>
        /// <userdoc>Can be used to override properties of the referenced material.</userdoc>
        [DataMember(50)]
        [Display("Overrides")]
        public MaterialOverrides Overrides { get; private set; }

        public virtual void Visit(MaterialGeneratorContext context)
        {
            // If not enabled, or Material or BlendMap are null, skip this layer
            if (!Enabled || Material == null || BlendMap == null || context.FindAsset == null)
            {
                return;
            }

            // Find the material from the reference
            var material = context.FindAsset(Material) as IMaterialDescriptor;
            if (material == null)
            {
                context.Log.Error($"Unable to find material [{Material}]");
                return;
            }

            // Check that material is valid
            var materialName = context.GetAssetFriendlyName(Material);
            if (!context.PushMaterial(material, materialName))
            {
                return;
            }

            try
            {
                // TODO: Because we are not fully supporting Streams declaration in shaders, we have to workaround this limitation by using a dynamic shader (inline)
                // Push a layer for the sub-material
                context.PushOverrides(Overrides);
                context.PushLayer(BlendMap);

                // Generate the material shaders into the current context
                material.Visit(context);
            }
            finally
            {
                // Pop the stack
                context.PopLayer();
                context.PopOverrides();
                context.PopMaterial();
            }
        }
    }
}
