// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Templates;
using Xenko.Core.IO;
using Xenko.Core.Reflection;
using Xenko.Assets.Models;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.Templates;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(ModelAsset))]
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
                Icon = new Image { Source = new BitmapImage(new Uri("/Xenko.Assets.Presentation;component/Resources/Icons/create_skeleton-16.png", UriKind.RelativeOrAbsolute))},
            }));
        }

        public ICommandBase CreateSkeletonCommand { get; }

        protected override IAssetImporter GetImporter()
        {
            return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
        }

        protected override void UpdateAssetFromSource(ModelAsset assetToMerge)
        {
            // Create a dictionary containing all new and old materials, favoring old ones to maintain existing references
            var dictionary = assetToMerge.Materials.ToDictionary(x => x.Name, x => x);
            Asset.Materials.ForEach(x => dictionary[x.Name] = x);

            // Create a dictionary mapping existing materials to their item id, to attempt to maintain existing ids and avoid unnecessary changes.
            var ids = CollectionItemIdHelper.GetCollectionItemIds(Asset.Materials).ToDictionary(x => Asset.Materials[(int)x.Key].Name, x => x.Value);

            // Remove currently existing materials, one by one because Quantum does not provide a Clear method.
            var materialsNode = AssetRootNode[nameof(ModelAsset.Materials)].Target;
            while (Asset.Materials.Count > 0)
            {
                materialsNode.Remove(Asset.Materials[0], new NodeIndex(0));
            }

            // Repopulate the list of materials
            for (var i = 0; i < assetToMerge.Materials.Count; ++i)
            {
                // Retrieve or create an id for the material
                ItemId id;
                if (!ids.TryGetValue(assetToMerge.Materials[i].Name, out id))
                    id = ItemId.New();

                // Use Restore to allow to set manually the id.
                materialsNode.Restore(dictionary[assetToMerge.Materials[i].Name], new NodeIndex(i), id);
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
