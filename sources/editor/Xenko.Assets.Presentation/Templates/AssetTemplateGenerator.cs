// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.IO;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Presentation.Services;

namespace Xenko.Assets.Presentation.Templates
{
    public abstract class AssetTemplateGenerator : TemplateGeneratorBase<AssetTemplateGeneratorParameters>
    {
        public sealed override Task<bool> PrepareForRun(AssetTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();
            return PrepareAssetCreation(parameters);
        }

        public sealed override bool Run(AssetTemplateGeneratorParameters parameters)
        {
            var assets = CreateAssets(parameters)?.ToList();
            if (assets == null)
            {
                parameters.Logger.Error("No asset created by the asset factory.");
                return false;
            }

            // Step one: add assets to package with proper unique name
            var newAssets = new Dictionary<AssetId, AssetItem>();
            foreach (var asset in assets)
            {
                if (string.IsNullOrEmpty(asset.Location))
                    throw new InvalidOperationException($"Asset returned by {nameof(CreateAssets)} has no location. Use {nameof(GenerateLocation)} to generate the location for each asset.");

                // Ensure unicity of names amongst package
                var name = NamingHelper.ComputeNewName(asset.Location, x => parameters.Package.Assets.Find(x) != null, "{0}_{1}");
                var item = new AssetItem(name, asset.Asset);

                try
                {
                    parameters.Package.Assets.Add(item);
                    // Ensure the dirty flag is properly set to refresh the dependency manager
                    item.IsDirty = true;
                }
                catch (Exception ex)
                {
                    parameters.Logger.Error("Failed to create new asset from template.", ex);
                    return false;
                }
                newAssets.Add(item.Id, item);
            }

            // Step two: fix references in the newly added assets
            foreach (var asset in newAssets)
            {
                var referencesToFix = AssetReferenceAnalysis.Visit(asset.Value);
                foreach (var assetReferenceLink in referencesToFix)
                {
                    var refToUpdate = assetReferenceLink.Reference as IReference;
                    if (refToUpdate == null)
                        continue;

                    AssetItem realItem;
                    // Look for the referenced asset in the new assets
                    if (newAssets.TryGetValue(refToUpdate.Id, out realItem))
                    {
                        assetReferenceLink.UpdateReference(realItem.Id, realItem.Location);
                    }
                    else
                    {
                        // If not found, try on the already existing assets
                        realItem = parameters.Package.Session.FindAsset(refToUpdate.Id);
                        assetReferenceLink.UpdateReference(realItem?.Id ?? AssetId.Empty, realItem?.Location);
                    }
                }
            }

            // Step three: complete creation and mark them as dirty
            foreach (var asset in newAssets.Values)
            {
                try
                {
                    PostAssetCreation(parameters, asset);
                    // Ensure the dirty flag is properly set to refresh the dependency manager
                    asset.IsDirty = true;
                }
                catch (Exception ex)
                {
                    parameters.Logger.Error("Failed to create new asset from template.", ex);
                    return false;
                }
            }
            return true;
        }

        protected abstract IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters);

        protected virtual Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            return Task.FromResult(true);
        }

        protected virtual void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
        }

        protected UFile GenerateLocation(AssetTemplateGeneratorParameters parameters)
        {
            return GenerateLocation(parameters.Name, parameters);
        }

        private static UFile GenerateLocation(string assetName, AssetTemplateGeneratorParameters parameters)
        {
            var location = assetName.StartsWith(parameters.TargetLocation) ? new UFile(assetName)
                                    : UPath.Combine(parameters.TargetLocation, new UFile(assetName));

            return NamingHelper.ComputeNewName(location, x => parameters.Package.Assets.Find(x) != null, "{0}_{1}");
        }

        protected async Task<AssetViewModel> BrowseForAsset(Package package, IEnumerable<Type> acceptedTypes, string initialDirectory, string message)
        {
            var session = SessionViewModel.Instance;
            var dialogService = SessionViewModel.Instance.ServiceProvider.Get<IEditorDialogService>();
            return await session.Dispatcher.InvokeTask(async () =>
            {
                var dialog = dialogService.CreateAssetPickerDialog(session);
                var packageViewModel = session.LocalPackages.FirstOrDefault(x => x.Match(package));
                var directory = packageViewModel?.AssetMountPoint.GetDirectory(initialDirectory ?? "");
                if (directory != null)
                    dialog.InitialLocation = directory;
                dialog.Message = message;
                dialog.AcceptedTypes.AddRange(acceptedTypes);
                await dialog.ShowModal();
                return dialog.SelectedAssets.FirstOrDefault();
            });
        }

        protected async Task<UFile> BrowseForFile(FileExtensionCollection extensions, bool allowAllFiles, string initialDirectory)
        {
            return (await BrowseForFiles(extensions, allowAllFiles, false, initialDirectory))?.Single();
        }

        protected async Task<IEnumerable<UFile>> BrowseForFiles(FileExtensionCollection extensions, bool allowAllFiles, bool allowMultiSelection, string initialDirectory)
        {
            var fileDialog = SessionViewModel.Instance.ServiceProvider.Get<IEditorDialogService>().CreateFileOpenModalDialog();
            fileDialog.Filters.Insert(0, new FileDialogFilter(extensions.Description, extensions.ConcatenatedExtensions));
            if (allowAllFiles)
            {
                fileDialog.Filters.Add(new FileDialogFilter("All files", "*.*"));
            }
            fileDialog.AllowMultiSelection = allowMultiSelection;
            fileDialog.InitialDirectory = initialDirectory;

            var result = await fileDialog.ShowModal();
            return result == DialogResult.Ok && fileDialog.FilePaths.Count > 0 ? fileDialog.FilePaths.Select(x => new UFile(x)) : null;
        }
    }
}
