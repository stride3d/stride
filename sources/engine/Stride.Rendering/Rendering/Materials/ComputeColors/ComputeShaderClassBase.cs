// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// Base class for shader class node.
    /// </summary>
    /// <typeparam name="T">Type of the node (scalar or color)</typeparam>
    [DataContract(Inherited = true)]
    [Display("Shader")]
    public abstract class ComputeShaderClassBase<T> : ComputeNode where T : class, IComputeNode
    {
        protected ComputeShaderClassBase()
        {
            Generics = new ComputeColorParameters();
            CompositionNodes = new Dictionary<string, T>();
        }

        /// <summary>
        /// The shader.
        /// </summary>
        /// <userdoc>
        /// The shader used in this node. It should be a ComputeColor.
        /// </userdoc>
        //TODO: use typed AssetReferences
        [DataMember(10)]
        [InlineProperty]
        public string MixinReference { get; set; }

        /// <summary>
        /// The generics of this class.
        /// </summary>
        /// <userdoc>
        /// The generics of the shader. There is no need to edit the list, it is automatically filled when the shader is loaded.
        /// </userdoc>
        [DataMember(30)]
        [MemberCollection(ReadOnly = true)]
        public ComputeColorParameters Generics { get; set; }
        
        /// <summary>
        /// The compositions of this class.
        /// </summary>
        /// <userdoc>
        /// The compositions of the shader where material nodes can be attached. There is no need to edit the list, it is automatically filled when the shader is loaded.
        /// </userdoc>
        [DataMember(40)]
        [MemberCollection(ReadOnly = true)]
        public Dictionary<string, T> CompositionNodes { get; set; }

        /// <summary>
        /// The members of this class.
        /// </summary>
        /// <userdoc>
        /// The editables values of this shader. There is no need to edit the list, it is automatically filled when the shader is loaded.
        /// </userdoc>
        [DataMemberIgnore]
        [MemberCollection(ReadOnly = true)]
        public Dictionary<ParameterKey, object> Members { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            foreach (var composition in CompositionNodes)
            {
                if (composition.Value != null)
                {
                    yield return composition.Value;
                }
            }

            var materialContext = context as MaterialGeneratorContext;
            if (materialContext != null)
            {
                foreach (var gen in Generics)
                {
                    if (gen.Value is ComputeColorParameterTexture)
                    {
                        var foundNode = ((ComputeColorParameterTexture)gen.Value).Texture;
                        if (foundNode != null) 
                            yield return foundNode;
                    }
                }
            }
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            if (string.IsNullOrEmpty(MixinReference))
                return new ShaderClassSource("ComputeColor");
            var mixinName = MixinReference;

            object[] generics = null;
            if (Generics.Count > 0)
            {
                // TODO: correct generic order
                var mixinGenerics = new List<object>();
                foreach (var genericKey in Generics.Keys)
                {
                    var generic = Generics[genericKey];
                    if (generic is ComputeColorParameterTexture)
                    {
                        var textureParameter = (ComputeColorParameterTexture)generic;
                        var textureKey = context.GetTextureKey(textureParameter.Texture, baseKeys);
                        mixinGenerics.Add(textureKey.ToString());
                    }
                    else if (generic is ComputeColorParameterSampler)
                    {
                        var pk = context.GetSamplerKey((ComputeColorParameterSampler)generic);
                        mixinGenerics.Add(pk.ToString());
                    }
                    else if (generic is ComputeColorParameterFloat)
                    {
                        mixinGenerics.Add(((ComputeColorParameterFloat)generic).Value.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (generic is ComputeColorParameterInt)
                    {
                        mixinGenerics.Add(((ComputeColorParameterInt)generic).Value.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (generic is ComputeColorParameterFloat2)
                    {
                        mixinGenerics.Add(MaterialUtility.GetAsShaderString(((ComputeColorParameterFloat2)generic).Value));
                    }
                    else if (generic is ComputeColorParameterFloat3)
                    {
                        mixinGenerics.Add(MaterialUtility.GetAsShaderString(((ComputeColorParameterFloat3)generic).Value));
                    }
                    else if (generic is ComputeColorParameterFloat4)
                    {
                        mixinGenerics.Add(MaterialUtility.GetAsShaderString(((ComputeColorParameterFloat4)generic).Value));
                    }
                    else if (generic is ComputeColorStringParameter)
                    {
                        mixinGenerics.Add(((ComputeColorStringParameter)generic).Value);
                    }
                    else
                    {
                        throw new Exception("[Material] Unknown node type: " + generic.GetType());
                    }
                }
                generics = mixinGenerics.ToArray();
            }

            var shaderClassSource = new ShaderClassSource(mixinName, generics);

            if (CompositionNodes.Count == 0)
                return shaderClassSource;

            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(shaderClassSource);

            foreach (var comp in CompositionNodes)
            {
                var compShader = comp.Value?.GenerateShaderSource(context, baseKeys);
                if (compShader != null)
                    mixin.Compositions.Add(comp.Key, compShader);
            }

            return mixin;
        }

        /// <inheritdoc/>
        public override string ToString() => "Shader";
    }
}
