// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Stride.Assets.Models;
using Stride.Assets.Presentation.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Rendering;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel<ModelAsset>]
    public class ModelViewModel : ImportedAssetViewModel<ModelAsset>
    {
        public ModelViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
            CreateSkeletonCommand = new AnonymousCommand(ServiceProvider, CreateSkeleton);

            // FIXME: tooltip, icons, etc. should not be created on the view model side (see PDX-2952)
            Dispatcher.Invoke(() => assetCommands.Add(new MenuCommandInfo(ServiceProvider, CreateSkeletonCommand)
            {
                DisplayName = "Create Skeleton",
                Tooltip = "Create a skeleton asset",
                Icon = new Image { Source = new BitmapImage(new Uri("/Stride.Assets.Presentation;component/Resources/Icons/create_skeleton-16.png", UriKind.RelativeOrAbsolute))},
            }));
        }

        public ICommandBase CreateSkeletonCommand { get; }

        protected override IAssetImporter GetImporter()
        {
            return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
        }

        protected override void PrepareImporterInputParametersForUpdateFromSource(PropertyCollection importerInputParameters, ModelAsset asset)
        {
            // This setting will be ignored if it's the FBX importer
            importerInputParameters.Set(ModelAssetImporter.DeduplicateMaterialsKey, asset.DeduplicateMaterials);
        }

        protected override void UpdateAssetFromSource(ModelAsset assetToMerge)
        {

            if (Asset.KepMeshIndex > -1)
            {
                var importer = GetImporter() as ModelAssetImporter;
                var importParams = new AssetImporterParameters();
                importParams.InputParameters.Set(ModelAssetImporter.SplitHierarchyKey, true);
                var entityInfo = importer.GetEntityInfo(Asset.MainSource, null, importParams);
                var updatedEntityModel = entityInfo.Models.Where(c=>c.MeshStartIndex==Asset.KepMeshIndex).First();

                var needed = new HashSet<int>();

                if (entityInfo.NodeNameToMaterialIndices != null && entityInfo.Nodes != null)
                {
                    var nodeName = updatedEntityModel.NodeName;
                    if (!string.IsNullOrEmpty(nodeName) &&
                        entityInfo.NodeNameToMaterialIndices.TryGetValue(nodeName, out var idxList) &&
                        idxList != null)
                    {
                        foreach (var idx in idxList) needed.Add(idx);
                    }
                }

                if (needed.Count == 0 && updatedEntityModel.MaterialIndices != null)
                {
                    foreach (var idx in updatedEntityModel.MaterialIndices) needed.Add(idx);
                }

                if (needed.Count == 0)
                    return;

                var globalNames =
                    (entityInfo.MaterialOrder != null && entityInfo.MaterialOrder.Count > 0)
                        ? entityInfo.MaterialOrder.ToList()
                        : (entityInfo.Materials != null
                                ? entityInfo.Materials.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList()
                                : new List<string>());

                var subMeshMaterialsNode = AssetRootNode[nameof(ModelAsset.Materials)].Target;
      
                int insert = 0;
                foreach (var idx in needed.OrderBy(i => i))
                {
                    var slotName = (idx < globalNames.Count) ? globalNames[idx] : $"Material {idx + 1}";

                    if (!Asset.Materials.Any(c => c.Name == slotName))
                    {
                        var slot = new ModelMaterial
                        {
                            Name = slotName,
                            MaterialInstance = new MaterialInstance()
                        };

                        subMeshMaterialsNode.Restore(slot, new NodeIndex(insert++), ItemId.New());
                    }
                }

            }
            else
            {

                var dictionary = assetToMerge.Materials.ToDictionary(x => x.Name, x => x);
                Asset.Materials.ForEach(x => dictionary[x.Name] = x);

                var ids = CollectionItemIdHelper.GetCollectionItemIds(Asset.Materials).ToDictionary(x => Asset.Materials[(int)x.Key].Name, x => x.Value);

                var materialsNode = AssetRootNode[nameof(ModelAsset.Materials)].Target;
                while (Asset.Materials.Count > 0)
                {
                    materialsNode.Remove(Asset.Materials[0], new NodeIndex(0));
                }

                for (var i = 0; i < assetToMerge.Materials.Count; ++i)
                {
                    ItemId id;
                    if (!ids.TryGetValue(assetToMerge.Materials[i].Name, out id))
                        id = ItemId.New();

                    materialsNode.Restore(dictionary[assetToMerge.Materials[i].Name], new NodeIndex(i), id);
                }
            }
        }

        private async void CreateSkeleton()
        {
            var source = Asset.Source;
            if (UPath.IsNullOrEmpty(source))
                return;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var template = Session.FindTemplates(TemplateScope.Asset).SingleOrDefault(x => x.Id == SkeletonFromFileTemplateGenerator.Id);
                if (template != null)
                {
                    var viewModel = new TemplateDescriptionViewModel(ServiceProvider, template);
                    var skeleton = (await Session.ActiveAssetView.RunAssetTemplate(viewModel, new[] { source })).SingleOrDefault();
                    if (skeleton == null)
                        return;

                    var skeletonNode = AssetRootNode[nameof(ModelAsset.Skeleton)];
                    var reference = ContentReferenceHelper.CreateReference<Skeleton>(skeleton);
                    skeletonNode.Update(reference);
                }
                UndoRedoService.SetName(transaction, "Create Skeleton");
            }
        }
    }
}
