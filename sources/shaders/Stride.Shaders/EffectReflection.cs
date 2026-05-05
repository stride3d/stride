// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Serialization;
using Stride.Graphics;

namespace Stride.Shaders
{
    /// <summary>
    /// The reflection data describing the parameters of a shader.
    /// </summary>
    [DataContract]
    public class EffectReflection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EffectReflection"/> class.
        /// </summary>
        public EffectReflection()
        {
            SamplerStates = [];
            ResourceBindings = [];
            ConstantBuffers = [];
            ShaderStreamOutputDeclarations = [];
            InputAttributes = [];
        }

        /// <summary>
        /// Resource groups organized by descriptor set (e.g. "PerFrame", "PerView", "PerDraw", "PerMaterial").
        /// Each group contains its entries pre-ordered (cbuffer first) and its constant buffer description.
        /// </summary>
        public List<EffectResourceGroupDescription> ResourceGroups { get; set; } = [];

        /// <summary>
        /// Gets or sets the sampler states.
        /// </summary>
        /// <value>The sampler states.</value>
        public List<EffectSamplerStateBinding> SamplerStates { get; set; }

        /// <summary>
        /// Gets the parameter binding descriptions.
        /// </summary>
        /// <value>The resource bindings.</value>
        public List<EffectResourceBindingDescription> ResourceBindings { get; set; }

        /// <summary>
        /// Gets the constant buffer descriptions (if any).
        /// </summary>
        /// <value>The constant buffers.</value>
        public List<EffectConstantBufferDescription> ConstantBuffers { get; set; }

        /// <summary>
        /// Gets or sets the stream output declarations.
        /// </summary>
        /// <value>The stream output declarations.</value>
        public List<ShaderStreamOutputDeclarationEntry> ShaderStreamOutputDeclarations { get; set; }

        /// <summary>
        /// Gets or sets the stream output strides.
        /// </summary>
        /// <value>The stream output strides.</value>
        public int[] StreamOutputStrides { get; set; }

        /// <summary>
        /// Gets or sets the stream output rasterized stream.
        /// </summary>
        /// <value>The stream output rasterized stream.</value>
        public int StreamOutputRasterizedStream { get; set; }

        public List<ShaderInputAttributeDescription> InputAttributes { get; set; }

        /// <summary>
        /// Finds a resource group by name, with fallback for the default set slot ("Globals").
        /// </summary>
        public EffectResourceGroupDescription FindResourceGroup(string name, string defaultSetSlot = null)
        {
            foreach (var group in ResourceGroups)
            {
                if (group.Name == name ||
                    (defaultSetSlot != null && name == defaultSetSlot && group.Name is null or "Globals"))
                    return group;
            }
            return null;
        }

        /// <summary>
        /// Finds or creates a resource group with the given name.
        /// </summary>
        public EffectResourceGroupDescription GetOrCreateGroup(string name)
        {
            foreach (var group in ResourceGroups)
            {
                if (group.Name == name)
                    return group;
            }
            var newGroup = new EffectResourceGroupDescription { Name = name };
            ResourceGroups.Add(newGroup);
            return newGroup;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var groups = string.Join(", ", ResourceGroups.Select(g => $"{g.Name}({g.Entries.Count})"));
            return $"EffectReflection [{groups}]";
        }
    }
}
