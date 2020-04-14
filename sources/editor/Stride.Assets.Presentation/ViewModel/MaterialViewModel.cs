// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Assets.Materials;
using Xenko.Rendering.Materials;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Quantum;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Assets.Effect;
using Xenko.Rendering.Materials.ComputeColors;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(MaterialAsset))]
    public class MaterialViewModel : AssetViewModel<MaterialAsset>
    {
        public MaterialViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        public static Type RootNodeType => typeof(MaterialAsset);

        [Obsolete]
        protected override void OnAssetPropertyChanged(string propertyName, IGraphNode node, NodeIndex index, object oldValue, object newValue)
        {
            base.OnAssetPropertyChanged(propertyName, node, index, oldValue, newValue);
            if (!PropertyGraph.UpdatingPropertyFromBase)
            {
                if (propertyName == nameof(MaterialAttributes.Diffuse) && Equals(Asset.Attributes.Diffuse, newValue))
                {
                    var diffuseModelNode = AssetRootNode[nameof(MaterialAsset.Attributes)].Target[nameof(MaterialAttributes.DiffuseModel)];
                    var currentDiffuseModel = diffuseModelNode.Retrieve();
                    if (newValue != null && currentDiffuseModel == null)
                        diffuseModelNode.Update(new MaterialDiffuseLambertModelFeature());
                    if (newValue == null && currentDiffuseModel != null)
                        diffuseModelNode.Update(null);
                }
                if (propertyName == nameof(MaterialAttributes.Specular) && Equals(Asset.Attributes.Specular, newValue))
                {
                    var specularModelNode = AssetRootNode[nameof(MaterialAsset.Attributes)].Target[nameof(MaterialAttributes.SpecularModel)];
                    var currentSpecularModel = specularModelNode.Retrieve();
                    if (newValue != null && currentSpecularModel == null)
                        specularModelNode.Update(new MaterialSpecularMicrofacetModelFeature());
                    if (newValue == null && currentSpecularModel != null)
                        specularModelNode.Update(null);
                }
                if (!UndoRedoService.UndoRedoInProgress && propertyName == nameof(ComputeShaderClassBase<IComputeNode>.MixinReference))
                {
                    var memberNode = node as IMemberNode;
                    if (memberNode != null)
                    {
                        var ownerNode = memberNode.Parent;
                        var value = ownerNode.Retrieve();
                        var colorNode = value as ComputeShaderClassColor;
                        if (colorNode != null)
                        {
                            UpdateNode(colorNode, ownerNode);
                        }

                        var scalarNode = value as ComputeShaderClassScalar;
                        if (scalarNode != null)
                        {
                            UpdateNode(scalarNode, ownerNode);
                        }
                    }
                }
            }
        }


        private void UpdateNode<T>(ComputeShaderClassBase<T> node, IObjectNode ownerNode) where T : class, IComputeNode
        {
            var projectShaders = new Dictionary<string, string>();
            foreach (var asset in Directory.Package.AllAssets.Where(x => x.Asset is EffectShaderAsset))
            {
                // TODO: we don't detect collision of name here
                projectShaders[asset.Name] = ((EffectShaderAsset)asset.Asset).Text;
            }

            var shader = node.ParseReferencedShader(projectShaders);

            // Process generics
            UpdateGenerics(shader, node, ownerNode);

            // Process composition nodes
            UpdateCompositionNodes(shader, node, ownerNode);
        }

        private void UpdateGenerics<T>(ShaderClassType shader, ComputeShaderClassBase<T> node, IObjectNode ownerNode)
            where T : class, IComputeNode
        {
            var genericsNode = ownerNode[nameof(ComputeShaderClassBase<T>.Generics)].Target;
            var keysToRemove = new List<object>(node.Generics.Keys);
            if (shader != null)
            {
                foreach (var generic in shader.ShaderGenerics)
                {
                    var parameterType = ComputeShaderClassHelper.GetComputeColorParameterType(generic.Type.Name.Text);
                    if (parameterType == null)
                        continue;

                    var index = new NodeIndex(generic.Name.Text);
                    if (genericsNode.Indices.Any(x => Equals(x, index)))
                    {
                        var value = genericsNode.Retrieve(index);
                        if (parameterType.IsInstanceOfType(value))
                        {
                            // This generic already exists and has the correct type, keep it in the list
                            keysToRemove.Remove(generic.Name);
                        }
                        else
                        {
                            // This generic already exists but has a different type. Just update the value.
                            var parameter = Activator.CreateInstance(parameterType);
                            genericsNode.Add(parameter, index);
                        }
                    }
                    else
                    {
                        // This is a new generic, add it
                        var parameter = Activator.CreateInstance(parameterType);
                        genericsNode.Add(parameter, index);
                    }
                }
            }

            // Remove all generics that we don't have anymore
            keysToRemove.Select(x => new NodeIndex(x)).ForEach(x => genericsNode.Remove(genericsNode.Retrieve(x), x));
        }

        private void UpdateCompositionNodes<T>(ShaderClassType shader, ComputeShaderClassBase<T> node, IObjectNode ownerNode)
            where T : class, IComputeNode
        {
            var keysToRemove = new List<object>(node.CompositionNodes.Keys);
            var compositionNodesNode = ownerNode[nameof(ComputeShaderClassBase<T>.CompositionNodes)].Target;
            if (shader != null)
            {
                // TODO: is it enough detect compositions?
                foreach (var member in shader.Members.OfType<Variable>().Where(x => x.Type is TypeName && x.Type.TypeInference?.TargetType == null))
                {
                    // ComputeColor only
                    if (member.Type.Name.Text == "ComputeColor")
                    {
                        var index = new NodeIndex(member.Name.Text);
                        if (compositionNodesNode.Indices.Any(x => Equals(x, index)))
                        {
                            // This composition node already exists, keep it in the list
                            keysToRemove.Remove(member.Name.Text);
                        }
                        else
                        {
                            // This is a new composition node, add it
                            compositionNodesNode.Add(null, index);
                        }
                    }
                }
            }

            // Remove all composition nodes that we don't have anymore
            keysToRemove.Select(x => new NodeIndex(x)).ForEach(x => compositionNodesNode.Remove(compositionNodesNode.Retrieve(x), x));
        }
    }
}

