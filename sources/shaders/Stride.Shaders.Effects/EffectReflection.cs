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
        /// The constant buffers of all <see cref="ResourceGroups"/>. A group owns its constant buffer, so this is a
        /// view and not a second serialized list.
        /// </summary>
        [DataMemberIgnore]
        public IEnumerable<EffectConstantBufferDescription> ConstantBuffers
        {
            get
            {
                foreach (var group in ResourceGroups)
                {
                    if (group.ConstantBuffer != null)
                        yield return group.ConstantBuffer;
                }
            }
        }

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
