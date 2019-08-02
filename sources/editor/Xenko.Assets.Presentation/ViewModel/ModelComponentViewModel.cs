// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Extensions;
using Xenko.Core.Serialization;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Core.Collections;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.ViewModel
{
    public class ModelComponentViewModel : DispatcherViewModel
    {
        private readonly EntityViewModel entity;
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
            if (node.Value is ModelComponent && node.Parent?.Value is EntityComponentCollection)
            {
                // Make sure the materials get refreshed if we change the model.
                var materials = node[nameof(ModelComponent.Materials)];
                var model = node[nameof(ModelComponent.Model)];
                materials.AddDependency(model, false);
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

        private IModelAsset GetReferencedModel()
        {
            var modelReference = entity.AssetSideEntity.Get<ModelComponent>()?.Model;
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
            }
        }
    }
}
