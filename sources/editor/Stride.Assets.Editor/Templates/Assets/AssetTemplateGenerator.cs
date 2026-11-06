// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.IO;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization.Contents;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The base of the generators creating assets from an asset template: it adds the assets the derived class creates to the package
/// with unique names, fixes the references between them and lets the derived class complete them. The dialogs (asset picker, file
/// pickers) go through the services of the current <see cref="SessionViewModel"/>, whatever the toolkit.
/// </summary>
public abstract class AssetTemplateGenerator : TemplateGeneratorBase<AssetTemplateGeneratorParameters>
{
    /// <inheritdoc/>
    public sealed override Task<bool> PrepareForRun(AssetTemplateGeneratorParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();
        return PrepareAssetCreation(parameters);
    }

    /// <inheritdoc/>
    public sealed override bool Run(AssetTemplateGeneratorParameters parameters)
    {
        var assets = CreateAssets(parameters)?.ToList();
        if (assets is null)
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
            // Overrides marked by the generator (items a derived asset owns)
            asset.YamlMetadata.CopyInto(item.YamlMetadata);

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
                if (assetReferenceLink.Reference is not IReference refToUpdate)
                    continue;

                // Look for the referenced asset in the new assets
                if (newAssets.TryGetValue(refToUpdate.Id, out var realItem))
                {
                    assetReferenceLink.UpdateReference(realItem.Id, realItem.Location);
                }
                else
                {
                    // If not found, try on the already existing assets
                    realItem = parameters.Package.Session?.FindAsset(refToUpdate.Id);
                    assetReferenceLink.UpdateReference(realItem?.Id ?? AssetId.Empty, realItem?.Location!);
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

    /// <summary>
    /// Creates the assets of the template, each with a location (see <see cref="GenerateLocation"/>).
    /// </summary>
    protected abstract IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters);

    /// <summary>
    /// Asks the user what the template needs before the assets are created; <c>false</c> cancels the creation.
    /// </summary>
    protected virtual Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Completes an asset once it is in the package and its references are fixed.
    /// </summary>
    protected virtual void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
    {
    }

    /// <summary>
    /// The location of a new asset named after the parameters, unique in the package.
    /// </summary>
    protected static UFile GenerateLocation(AssetTemplateGeneratorParameters parameters)
    {
        return GenerateLocation(parameters.Name, parameters);
    }

    private static UFile GenerateLocation(string assetName, AssetTemplateGeneratorParameters parameters)
    {
        var location = assetName.StartsWith(parameters.TargetLocation, StringComparison.Ordinal)
            ? new UFile(assetName)
            : UPath.Combine(parameters.TargetLocation, new UFile(assetName));

        return NamingHelper.ComputeNewName(location, x => parameters.Package.Assets.Find(x) != null, "{0}_{1}");
    }

    /// <summary>
    /// The session the template runs in: the one open in the editor.
    /// </summary>
    protected static SessionViewModel Session => SessionViewModel.Instance;

    /// <summary>
    /// Shows the asset picker and waits for the user to pick an asset of the given types.
    /// </summary>
    /// <param name="package">The package the asset is created in, whose folders the picker opens on.</param>
    /// <param name="acceptedTypes">The asset types the picker lists.</param>
    /// <param name="initialDirectory">The folder of the package the picker opens on, or <c>null</c>.</param>
    /// <param name="message">What the asset is picked for.</param>
    /// <returns>The picked asset, or <c>null</c> when the user cancelled.</returns>
    protected static async Task<AssetViewModel?> BrowseForAssetAsync(Package package, IEnumerable<Type> acceptedTypes, string? initialDirectory, string? message)
    {
        var session = Session;
        var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
        return await session.Dispatcher.InvokeTask(async () =>
        {
            var packageViewModel = session.LocalPackages.FirstOrDefault(x => x.Match(package));
            var directory = packageViewModel?.AssetMountPoint.GetDirectory(initialDirectory ?? "");
            return await dialogService.OpenAssetPickerAsync(session, acceptedTypes, initialLocation: directory, message: message);
        });
    }

    /// <summary>
    /// Shows the file picker and waits for the user to pick a file.
    /// </summary>
    /// <returns>The picked file, or <c>null</c> when the user cancelled.</returns>
    protected static async Task<UFile?> BrowseForFileAsync(FileExtensionCollection extensions, bool allowAllFiles, string? initialDirectory)
    {
        return (await BrowseForFilesAsync(extensions, allowAllFiles, false, initialDirectory))?.Single();
    }

    /// <summary>
    /// Shows the file picker and waits for the user to pick one or several files.
    /// </summary>
    /// <returns>The picked files, or <c>null</c> when the user cancelled.</returns>
    protected static async Task<IEnumerable<UFile>?> BrowseForFilesAsync(FileExtensionCollection extensions, bool allowAllFiles, bool allowMultiSelection, string? initialDirectory)
    {
        List<FilePickerFilter> filters =
        [
            new(extensions.Description) { Patterns = extensions.SingleExtensions.ToList() }
        ];
        if (allowAllFiles)
        {
            filters.Add(new("All files") { Patterns = ["*.*"] });
        }

        var dialogs = Session.ServiceProvider.Get<IDialogService>();
        var initialPath = string.IsNullOrEmpty(initialDirectory) ? null : new UDirectory(initialDirectory);
        if (allowMultiSelection)
        {
            var files = await dialogs.OpenMultipleFilesPickerAsync(initialPath, filters);
            return files.Count > 0 ? files : null;
        }

        var file = await dialogs.OpenFilePickerAsync(initialPath, filters);
        return file is null ? null : [file];
    }
}
