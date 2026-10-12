// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.TemplateProviders;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Rendering;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModel
{
    public class ModelComponentViewModel : DispatcherViewModel
    {
        private readonly EntityViewModel entity;
        // Latest dragged weights by target name, waiting for the next game frame.
        private readonly Dictionary<string, float> previewWeights = new(StringComparer.Ordinal);
        private bool previewPosted;
        private IMemberNode modelContent;

        public ModelComponentViewModel(IViewModelServiceProvider serviceProvider, EntityViewModel entity)
            : base(serviceProvider)
        {
            this.entity = entity;
        }

        public void Initialize()
        {
            var assetNode = entity.Editor.NodeContainer.GetOrCreateNode(entity.AssetSideEntity);
            var componentNode = assetNode[nameof(Entity.Components)].Target;
            componentNode.ItemChanged += ComponentListChanged;
            RegisterModelChanged();
        }

        public override void Destroy()
        {
            var assetNode = entity.Editor.NodeContainer.GetOrCreateNode(entity.AssetSideEntity);
            var componentNode = assetNode[nameof(Entity.Components)].Target;
            componentNode.ItemChanged -= ComponentListChanged;
            UnregisterModelChanged();

            base.Destroy();
        }

        internal void UpdateNodePresenter(INodePresenter node)
        {
            if (node.Value is ModelComponent component && node.Parent?.Value is EntityComponentCollection)
            {
                // Make sure the materials get refreshed if we change the model.
                var materials = node[nameof(ModelComponent.Materials)];
                var model = node[nameof(ModelComponent.Model)];
                materials.AddDependency(model, false);

                // Morph settings follow the model, and are only shown when it has morph targets.
                var morphs = node[nameof(ModelComponent.Morphs)];
                morphs.AddDependency(model, true);
                morphs.IsVisible = GetMorphTargetNames(GetModelAsset(component.Model)).Length != 0;
            }

            if (node.Value is ModelMorphSettings settings)
            {
                // The weights are edited through the named sliders below, not the raw dictionary.
                node[nameof(ModelMorphSettings.Weights)].IsVisible = false;

                // The settings of this component, not the entity's first ModelComponent.
                var morphComponent = entity.AssetSideEntity.Components.OfType<ModelComponent>().First(candidate => candidate.Morphs == settings);
                var names = GetMorphTargetNames(GetModelAsset(morphComponent.Model));
                if (names.Length == 0)
                    return;

                // Sliders live in their own category: editing one weight rebuilds the dictionary presenter, not the other rows.
                var weights = node.CreateCategory("Weights", 30, ExpandRule.Once);
                var weightsNode = (IAssetObjectNode)entity.Editor.NodeContainer.GetNode(settings.Weights);
                weights.Commands.Clear();
                var componentId = morphComponent.Id;
                weights.Commands.Add(new SyncAnonymousNodePresenterCommand("ResetAllMorphWeights", (_, _) => SetMorphWeights(weightsNode, componentId, names, 0f, "Reset all morph weights")));
                weights.Commands.Add(new SyncAnonymousNodePresenterCommand("MaxAllMorphWeights", (_, _) => SetMorphWeights(weightsNode, componentId, names, 1f, "Set all morph weights to 1")));
                weights.Commands.Add(new SyncAnonymousNodePresenterCommand("MinAllMorphWeights", (_, _) => SetMorphWeights(weightsNode, componentId, names, -1f, "Set all morph weights to -1")));

                var factory = ((IAssetNodePresenter)node).Factory;
                for (int i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    var index = new NodeIndex(name);
                    var slider = factory.CreateVirtualNodePresenter(weights, name + MorphWeightTemplateProvider.NodeNameSuffix, typeof(float), i,
                        () => weightsNode.Indices.Contains(index) ? weightsNode.Retrieve(index) : 0f,
                        value => SetMorphWeight(weightsNode, index, (float)value),
                        () => weightsNode.BaseNode != null,
                        () => weightsNode.Indices.Contains(index) && weightsNode.IsItemInherited(index),
                        () => weightsNode.Indices.Contains(index) && weightsNode.IsItemOverridden(index));
                    slider.DisplayName = name;
                    slider.RegisterAssociatedNode(new NodeAccessor(weightsNode, index));
                    slider.AttachedProperties.Set(NumericData.MinimumKey, -1f);
                    slider.AttachedProperties.Set(NumericData.MaximumKey, 1f);
                    slider.AttachedProperties.Set(NumericData.SmallStepKey, 0.01);
                    slider.AttachedProperties.Set(NumericData.LargeStepKey, 0.1);
                    slider.AttachedProperties.Set(NumericData.DecimalPlacesKey, 3);
                    slider.AttachedProperties.Set(MorphWeightTemplateProvider.PreviewKey, value => PreviewMorphWeight(componentId, name, (float)value));
                }
            }

            if (node.Value is IndexingDictionary<Material> && node.Parent?.Value is ModelComponent)
            {
                var materialsNode = (IAssetObjectNode)entity.Editor.NodeContainer.GetNode(node.Value);
                var materials = node;
                var model = GetReferencedModel();
                if (model != null)
                {
                    int i = 0;
                    foreach (var child in materials.Children.ToList())
                    {
                        child.IsVisible = false;
                    }
                    var factory = ((IAssetNodePresenter)node).Factory;
                    foreach (var material in model.Materials.ToList())
                    {
                        var modelMaterial = model.Materials.Count > i ? model.Materials[i] : null;
                        var materialName = modelMaterial?.Name ?? $"(Material {i + 1})";
                        var index = new NodeIndex(i);
                        var virtualMaterial = factory.CreateVirtualNodePresenter(node, material.Name + "___Virtual", typeof(Material), i,
                                                   () => GetMaterial(materialsNode, index),
                                                   x => SetMaterial(materialsNode, index, (Material)x),
                                                   () => materialsNode.BaseNode != null,
                                                   () => materialsNode.IsItemInherited(index),
                                                   () => materialsNode.IsItemOverridden(index));

                        // Do not put the FetchAssetCommand, we need a custom implementation for this one.
                        // Do not put the CreateNewInstanceCommand neither, otherwise it will display the "Clear reference" button which doesn't make sense here (null => disabled)
                        virtualMaterial.Commands.RemoveWhere(x => x.Name == FetchAssetCommand.CommandName || x.Name == CreateNewInstanceCommand.CommandName);

                        // Override the FetchAsset command to be able to fetch the model material when it is null in the component
                        var fetchAsset = new AnonymousNodePresenterCommand(FetchAssetCommand.CommandName, (x, param) => FetchMaterial(materialsNode, index));
                        virtualMaterial.Commands.Add(fetchAsset);
                        virtualMaterial.DisplayName = materialName;
                        virtualMaterial.RegisterAssociatedNode(new NodeAccessor(materialsNode, index));

                        var enabledNode = factory.CreateVirtualNodePresenter(virtualMaterial, "Enabled", typeof(bool), 0,
                               () => IsMaterialEnabled(materialsNode, index),
                               x => SetMaterialEnabled(materialsNode, index, (bool)x),
                               () => materialsNode.BaseNode != null,
                               () => materialsNode.IsItemInherited(index),
                               () => materialsNode.IsItemOverridden(index));
                        enabledNode.RegisterAssociatedNode(new NodeAccessor(materialsNode, index));
                        enabledNode.IsVisible = false;
                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Shows a dragged weight in the scene on the next game frame, without editing the asset: the editor sends asset
        /// changes to the game one after another, which lags behind a drag. Only the latest weight of each target is sent.
        /// </summary>
        private void PreviewMorphWeight(Guid componentId, string name, float value)
        {
            if (!float.IsFinite(value))
                return;
            lock (previewWeights)
            {
                previewWeights[name] = value;
                if (previewPosted)
                    return;
                previewPosted = true;
            }
            entity.Editor.Controller.InvokeAsync(() => ApplyPreviewWeights(componentId));
        }

        private void ApplyPreviewWeights(Guid componentId)
        {
            KeyValuePair<string, float>[] weights;
            lock (previewWeights)
            {
                weights = previewWeights.ToArray();
                previewWeights.Clear();
                previewPosted = false;
            }
            var gameSideEntity = entity.Editor.Controller.FindGameSidePart(entity.Id) as Entity;
            if (gameSideEntity?.Components.FirstOrDefault(component => component.Id == componentId) is not ModelComponent gameSideComponent)
                return;
            // Runtime weights only: the authored weights replace them once the asset change reaches the scene.
            foreach (var (name, weight) in weights)
            {
                int slot = gameSideComponent.FindMorphTarget(name);
                if (slot >= 0)
                    gameSideComponent.SetMorphWeight(slot, weight);
            }
        }

        private static void SetMorphWeight(IObjectNode node, NodeIndex index, float value)
        {
            // The slider range is -1 to 1, but typed values may go beyond it, like imported or animated weights.
            if (!float.IsFinite(value))
                return;
            if (node.Indices.Contains(index))
                node.Update(value, index);
            else
                node.Add(value, index);
        }

        private void SetMorphWeights(IObjectNode weightsNode, Guid componentId, string[] names, float value, string actionName)
        {
            // Show the new weights at once: the asset changes reach the scene one after another.
            foreach (var name in names)
                PreviewMorphWeight(componentId, name, value);
            // Item changes reach the scene game like slider edits; the named transaction makes them a single undo step.
            var undoRedoService = ServiceProvider.TryGet<IUndoRedoService>();
            using var transaction = undoRedoService?.CreateTransaction();
            foreach (var name in names)
            {
                var index = new NodeIndex(name);
                if (!weightsNode.Indices.Contains(index) || (float)weightsNode.Retrieve(index) != value)
                    SetMorphWeight(weightsNode, index, value);
            }
            if (transaction != null)
                undoRedoService.SetName(transaction, actionName);
        }

        /// <summary>Distinct morph target names of a model asset, empty when it has none or doesn't import them.</summary>
        private static string[] GetMorphTargetNames(IModelAsset model)
        {
            return model is ModelAsset { ImportMorphTargets: true } asset
                ? asset.MorphTargetNames.Distinct(StringComparer.Ordinal).ToArray()
                : [];
        }

        private Task FetchMaterial(IObjectNode materialsNode, NodeIndex index)
        {
            var material = GetMaterial(materialsNode, index);
            return FetchAssetCommand.Fetch(entity.Editor.Session, material);
        }

        private static void SetMaterial(IObjectNode materialNode, NodeIndex index, Material value)
        {
            if (materialNode.Indices.Contains(index))
            {
                materialNode.Update(value, index);
            }
            else
            {
                materialNode.Add(value, index);
            }
        }

        private object GetMaterial(IObjectNode materialNode, NodeIndex index)
        {
            if (materialNode.Indices.Contains(index))
            {
                return materialNode.Retrieve(index);
            }

            var model = GetReferencedModel();
            if (model == null)
                return null;

            // During specific operations such as changes of the referenced model, this getter can be used while we're not currently in sync with the collection
            // of the model. In this case, return null.
            if (model.Materials.Count <= index.Int)
                return null;

            return model?.Materials[index.Int].MaterialInstance.Material;
        }

        private void SetMaterialEnabled(IObjectNode materialNode, NodeIndex index, bool value)
        {
            if (value)
            {
                var material = GetMaterial(materialNode, index);
                materialNode.Add(material, index);
            }
            else
            {
                var material = materialNode.Retrieve(index);
                materialNode.Remove(material, index);
            }
        }

        private static object IsMaterialEnabled(IObjectNode materialNode, NodeIndex index)
        {
            return materialNode.Indices.Contains(index);
        }

        private static void ClearMaterialList(IObjectNode materials)
        {
            var indices = materials.Indices.ToList();
            foreach (var index in indices)
            {
                var item = materials.Retrieve(index);
                materials.Remove(item, index);
            }
        }

        private IObjectNode GetMaterialsNode()
        {
            var modelComponent = entity.AssetSideEntity.Get<ModelComponent>();
            return modelComponent != null ? entity.Editor.NodeContainer.GetNode(modelComponent)[nameof(ModelComponent.Materials)].Target : null;
        }

        private IModelAsset GetReferencedModel() => GetModelAsset(entity.AssetSideEntity.Get<ModelComponent>()?.Model);

        private IModelAsset GetModelAsset(object modelReference)
        {
            if (modelReference == null)
                return null;

            var modelUrl = AttachedReferenceManager.GetUrl(modelReference);
            return entity.Editor.Session.AllAssets.FirstOrDefault(x => x.Url == modelUrl)?.Asset as IModelAsset;
        }

        private void ComponentListChanged(object sender, ItemChangeEventArgs e)
        {
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                if (e.NewValue is ModelComponent)
                {
                    RegisterModelChanged();
                }
            }
            if (e.ChangeType == ContentChangeType.CollectionRemove)
            {
                if (e.OldValue is ModelComponent)
                {
                    UnregisterModelChanged();
                }
            }
        }

        private void RegisterModelChanged()
        {
            UnregisterModelChanged();
            var modelComponent = entity.AssetSideEntity.Get<ModelComponent>();
            if (modelComponent != null)
            {
                var modelNode = entity.Editor.NodeContainer.GetNode(modelComponent);
                modelContent = modelNode[nameof(ModelComponent.Model)];
                modelContent.ValueChanging += ModelChanging;
            }
        }

        private void UnregisterModelChanged()
        {
            if (modelContent != null)
            {
                modelContent.ValueChanging -= ModelChanging;
                modelContent = null;
            }
        }

        private void ModelChanging(object sender, MemberNodeChangeEventArgs e)
        {
            if (e.NewValue != e.OldValue && !entity.Editor.UndoRedoService.UndoRedoInProgress)
            {
                var materials = GetMaterialsNode();
                ClearMaterialList(materials);

                // Keep the weights of targets the new model also has, so swapping between variants of a model keeps the pose.
                var component = entity.AssetSideEntity.Get<ModelComponent>();
                var kept = GetMorphTargetNames(GetModelAsset(e.NewValue)).ToHashSet(StringComparer.Ordinal);
                var weights = entity.Editor.NodeContainer.GetNode(component.Morphs.Weights);
                foreach (var index in weights.Indices.ToList())
                {
                    if (!kept.Contains((string)index.Value))
                        weights.Remove(weights.Retrieve(index), index);
                }
            }
        }
    }
}
