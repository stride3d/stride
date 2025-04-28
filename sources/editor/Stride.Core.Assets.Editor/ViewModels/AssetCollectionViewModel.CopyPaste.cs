// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

partial class AssetCollectionViewModel
{
    private IClipboardService? ClipboardService => ServiceProvider.TryGet<IClipboardService>();
    private ICopyPasteService? CopyPasteService => ServiceProvider.TryGet<ICopyPasteService>();
    private IDialogService DialogService => ServiceProvider.Get<IDialogService>();

    public ICommandBase CopyAssetsRecursivelyCommand { get; }

    public ICommandBase CopyAssetUrlCommand { get; }

    public ICommandBase CopyContentCommand { get; }

    public ICommandBase CopyLocationsCommand { get; }

    public ICommandBase CutContentCommand { get; }

    public ICommandBase CutLocationsCommand { get; }

    public ICommandBase PasteCommand { get; }

    private bool CanCopy()
    {
        return CopyPasteService is not null && ClipboardService is not null;
    }

    private bool CanPaste()
    {
        if (CopyPasteService is { } copyPaste && ClipboardService is { } clipboard)
        {
            var text = clipboard.GetTextAsync().Result;
            return copyPaste.CanPaste(text, typeof(List<AssetItem>), typeof(List<AssetItem>), typeof(List<AssetItem>));
        }

        return false;
    }

    private async Task CopyAssetUrl()
    {
        if (SingleSelectedAsset is null)
            return;

        try
        {
            await ClipboardService!.SetTextAsync(SingleSelectedAsset.Url);
        }
        catch (SystemException e)
        {
            // We don't provide feedback when copying fails.
            e.Ignore();
        }
    }

    private async Task CopySelectedAssetsRecursively()
    {
        var assetsToCopy = new ObservableSet<AssetViewModel>();
        foreach (var asset in SelectedAssets)
        {
            assetsToCopy.Add(asset);
            assetsToCopy.AddRange(asset.Dependencies.RecursiveReferencedAssets.Where(a => a.IsEditable));
        }

        await CopySelection(null, assetsToCopy);
        UpdateCommands();
    }

    private async Task CopySelectedContent()
    {
        var directories = SelectedContent.OfType<DirectoryBaseViewModel>().ToList();
        await CopySelection(directories, SelectedAssets);
        UpdateCommands();
    }

    private async Task CopySelectedLocations()
    {
        var directories = GetSelectedDirectories(false);
        await CopySelection(directories, null);
        UpdateCommands();
    }

    private async Task CopySelection(IReadOnlyCollection<DirectoryBaseViewModel>? directories, IEnumerable<AssetViewModel>? assetsToCopy)
    {
        var assetsToWrite = await GetCopyCollection(directories, assetsToCopy);
        if (assetsToWrite?.Count > 0)
        {
            await WriteToClipboardAsync(assetsToWrite);
        }
    }

    private async Task CutSelectedContent()
    {
        var directories = SelectedContent.OfType<DirectoryBaseViewModel>().ToList();
        await CutSelection(directories, SelectedAssets);
        UpdateCommands();
    }

    private async Task CutSelectedLocations()
    {
        var directories = GetSelectedDirectories(false);
        await CutSelection(directories, null);
        UpdateCommands();
    }

    private async Task CutSelection(IReadOnlyCollection<DirectoryBaseViewModel>? directories, IEnumerable<AssetViewModel>? assetsToCut)
    {
        // Ensure all directories can be cut
        if (directories?.Any(d => !d.IsEditable) == true)
        {
            await DialogService.MessageBoxAsync(Tr._p("Message", "Read-only folders can't be cut."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var assetsToWrite = await GetCopyCollection(directories, assetsToCut);
        if (assetsToWrite is null || assetsToWrite.Count == 0)
            return;

        //// Flatten to a list
        //var assetList = assetsToWrite.SelectMany(x => x).ToList();
        //foreach (var asset in assetList)
        //{
        //    if (!asset.CanDelete(out string error))
        //    {
        //        error = string.Format(Tr._p("Message", "The asset {0} can't be deleted. {1}{2}"), asset.Url, Environment.NewLine, error);
        //        await DialogService.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }
        //}

        // Copy
        if (!await WriteToClipboardAsync(assetsToWrite))
        {
            return;
        }

        using var transaction = Session.ActionService?.CreateTransaction();

        // Clear the selection at first to reduce view updates in the following actions
        ClearSelection();
        //// Add an action item that will fix back the references in the referencers of the assets being cut, in case the
        //var assetsToFix = PackageViewModel.GetReferencers(dependencyManager, Session, assetList.Select(x => x.AssetItem));
        //var fixReferencesOperation = new FixAssetReferenceOperation(assetsToFix, true, false);
        //Session.ActionService.PushOperation(fixReferencesOperation);
        //// Delete the assets
        //DeleteAssets(assetList);
        //if (directories is not null)
        //{
        //    // Delete the directories
        //    foreach (var directory in directories)
        //    {
        //        // Last-chance check (note that we already checked that the directories are not read-only)
        //        if (!directory.CanDelete(out string error))
        //        {
        //            error = string.Format(Tr._p("Message", "{0} can't be deleted. {1}{2}"), directory.Name, Environment.NewLine, error);
        //            await DialogService.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }
        //        directory.Delete();
        //    }
        //}

        Session.ActionService?.SetName(transaction!, "Cut selection");
    }

    /// <summary>
    /// Gets the whole collection of assets to be copied.
    /// </summary>
    /// <param name="directories">The collection of separate directories of assets.</param>
    /// <param name="assetsToCopy">The collection of assets in the current directory.</param>
    /// <remarks>Directories cannot be in the same hierarchy of one another.</remarks>
    /// <returns>The collection of assets to be copied, or null if the selection cannot be copied.</returns>
    private async Task<ICollection<IGrouping<string, AssetViewModel>>?> GetCopyCollection(IReadOnlyCollection<DirectoryBaseViewModel>? directories, IEnumerable<AssetViewModel>? assetsToCopy)
    {
        var collection = new List<IGrouping<string, AssetViewModel>>();
        // First level assets will be copied as is
        if (assetsToCopy is not null)
        {
            collection.AddRange(assetsToCopy.GroupBy(_ => string.Empty));
        }

        if (directories is not null)
        {
            // Check directory structure
            foreach (var directory in directories)
            {
                var parent = directory.Parent;
                while (parent is not MountPointViewModel)
                {
                    if (directories.Contains(parent))
                    {
                        await DialogService.MessageBoxAsync(Tr._p("Message", "Unable to cut or copy a selection that contains a folder and one of its subfolders."), MessageBoxButton.OK, MessageBoxImage.Information);
                        return null;
                    }
                    parent = parent.Parent;
                }
            }
            // Get all assets from directories
            foreach (var directory in directories)
            {
                var hierarchy = directory.GetDirectoryHierarchy();
                foreach (var folder in hierarchy)
                {
                    EnsureDirectoryHierarchy(folder.Assets, folder);
                    // Add assets grouped by relative path
                    collection.AddRange(folder.Assets.GroupBy(_ => folder.Path.Remove(0, directory.Parent.Path.Length)));
                }
            }
        }

        return collection;

        static void EnsureDirectoryHierarchy(IEnumerable<AssetViewModel> assets, DirectoryBaseViewModel directory)
        {
            if (assets.Any(asset => asset.AssetItem.Location.HasDirectory && !asset.Url.StartsWith(directory.Parent.Path, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("One of the asset does not match the directory hierarchy.");
            }
        }
    }

    private async Task Paste()
    {
        var directories = GetSelectedDirectories(false);
        if (directories.Count != 1)
        {
            await DialogService.MessageBoxAsync(Tr._p("Message", "Select a valid asset folder to paste the selection to."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // If the selection is already a directory, paste into it
        var directory = SingleSelectedContent as DirectoryBaseViewModel ?? directories.First();
        var package = directory.Package;
        if (!package.IsEditable)
        {
            await DialogService.MessageBoxAsync(Tr._p("Message", "This package or directory can't be modified."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var text = await ClipboardService!.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var pastedAssets = new List<AssetItem>();
        pastedAssets = CopyPasteService!.DeserializeCopiedData(text, pastedAssets, typeof(List<AssetItem>)).Items.FirstOrDefault()?.Data as List<AssetItem>;
        if (pastedAssets is null)
            return;

        var updatedAssets = new List<AssetItem>();
        var root = directory.Root;
        var project = (root as ProjectCodeViewModel)?.Project;
        foreach (var assetItem in pastedAssets)
        {
            // Perform allowed asset types validation
            if (!root.AcceptAssetType(assetItem.Asset.GetType()))
            {
                // Skip invalid assets
                continue;
            }

            var location = UPath.Combine(directory.Path, assetItem.Location);

            // Check if we are pasting to package or a project (with a source code)
            if (project is not null)
            {
                // Link source project
                assetItem.SourceFolder = project.Package.RootDirectory;
            }

            // Resolve folders to paste collisions with those existing in a directory
            var assetLocationDir = assetItem.Location.FullPath;
            {
                // Split path into two parts
                int firstSeparator = assetLocationDir.IndexOf(DirectoryBaseViewModel.Separator, StringComparison.Ordinal);
                if (firstSeparator > 0)
                {
                    // Left: (folder)
                    // /
                    // Right: (..folders..) / (file.ext)
                    UDirectory leftPart = assetLocationDir.Remove(firstSeparator);
                    UFile rightPart = assetLocationDir[(firstSeparator + 1)..];

                    // Find valid left part location (if already in use)
                    leftPart = NamingHelper.ComputeNewName(leftPart, e => directory.GetDirectory(e) is not null, "{0} ({1})");

                    // Fix location: (paste directory) / left/ right
                    location = UPath.Combine(Path.Combine(directory.Path, leftPart), rightPart);
                }
            }

            var updatedAsset = assetItem.Clone(true, location, assetItem.Asset);
            updatedAssets.Add(updatedAsset);
        }

        if (updatedAssets.Count == 0)
            return;

        var viewModels = PasteAssetsIntoPackage(package, updatedAssets, project);

        var referencerViewModels = AssetViewModel.ComputeRecursiveReferencerAssets(viewModels);
        viewModels.AddRange(referencerViewModels);
        await Session.NotifyAssetPropertiesChangedAsync(viewModels);
        UpdateCommands();
    }
    
    public static List<AssetViewModel> PasteAssetsIntoPackage(PackageViewModel package, List<AssetItem> assets, ProjectViewModel? project)
    {
        var viewModels = new List<AssetViewModel>();

        // Don't touch the action stack in this case.
        if (assets.Count == 0)
            return viewModels;

        var fixedAssets = new List<AssetItem>();

        using var transaction = package.UndoRedoService.CreateTransaction();
        // Clean collision by renaming pasted asset if an asset with the same name already exists in that location.
        AssetCollision.Clean(null, assets, fixedAssets, AssetResolver.FromPackage(package.Package), false, false);

        // Temporarily add the new asset to the package
        fixedAssets.ForEach(x => package.Package.Assets.Add(x));

        // Find which assets are referencing the pasted assets in order to fix the reference link.
        var assetsToFix = GetReferencers(package.Session.DependencyManager, package.Session, fixedAssets);

        // Remove temporarily added assets - they will be properly re-added with the correct action stack entry when creating the view model
        fixedAssets.ForEach(x => package.Package.Assets.Remove(x));

        // Create directories and view models, actually add assets to package.
        foreach (var asset in fixedAssets)
        {
            var location = asset.Location.GetFullDirectory();
            var assetDirectory = project == null ?
                package.GetOrCreateAssetDirectory(location) :
                project.GetOrCreateProjectDirectory(location);
            var assetViewModel = package.CreateAsset(asset, assetDirectory);
            viewModels.Add(assetViewModel);
        }

        // Fix references in the assets that references what we pasted.
        // We wrap this operation in an action item so the action stack can properly re-execute it.
        var fixReferencesAction = new FixAssetReferenceOperation(assetsToFix, false, true);
        fixReferencesAction.FixAssetReferences();
        package.UndoRedoService.PushOperation(fixReferencesAction);

        package.UndoRedoService.SetName(transaction, "Paste assets");
        return viewModels;
    }
    
    private static List<AssetViewModel> GetReferencers(IAssetDependencyManager dependencyManager, ISessionViewModel session, IEnumerable<AssetItem> assets)
    {
        var result = new List<AssetViewModel>();

        // Find which assets are referencing the pasted assets in order to fix the reference link.
        foreach (var asset in assets)
        {
            if (dependencyManager.ComputeDependencies(asset.Id, AssetDependencySearchOptions.In) is not { } referencers)
                continue;

            foreach (var referencerLink in referencers.LinksIn)
            {
                if (session.GetAssetById(referencerLink.Item.Id) is not { } assetViewModel)
                    continue;

                if (!result.Contains(assetViewModel))
                    result.Add(assetViewModel);
            }
        }
        return result;
    }

    private void UpdateCommands()
    {
        var atLeastOneAsset = SelectedAssets.Count > 0;
        var atLeastOneContent = SelectedContent.Count > 0;

        CopyAssetsRecursivelyCommand.IsEnabled = atLeastOneAsset;
        CopyAssetUrlCommand.IsEnabled = SingleSelectedAsset is not null;
        CopyContentCommand.IsEnabled = atLeastOneContent;
        // Can copy from asset mount point
        CopyLocationsCommand.IsEnabled = SelectedLocations.All(x => x is DirectoryBaseViewModel or PackageViewModel);
        CutContentCommand.IsEnabled = atLeastOneContent;
        // TODO: Allow to cut asset mount point - but do not remove the mount point
        CutLocationsCommand.IsEnabled = SelectedLocations.All(x => x is DirectoryViewModel or PackageViewModel);
        PasteCommand.IsEnabled = SelectedLocations.Count == 1 && SelectedLocations.All(x => x is DirectoryBaseViewModel or PackageViewModel);
    }

    /// <summary>
    /// Actually writes the assets to the clipboard.
    /// </summary>
    /// <param name="assetsToWrite"></param>
    /// <returns></returns>
    private async Task<bool> WriteToClipboardAsync(IEnumerable<IGrouping<string, AssetViewModel>> assetsToWrite)
    {
        var assetCollection = new List<AssetItem>();
        assetCollection.AddRange(assetsToWrite.SelectMany(
            grp => grp.Select(a => new AssetItem(UPath.Combine<UFile>(grp.Key, a.AssetItem.Location.GetFileNameWithoutExtension()!), a.AssetItem.Asset))));
        try
        {
            var text = CopyPasteService!.CopyMultipleAssets(assetCollection);
            if (string.IsNullOrEmpty(text))
                return false;

            await ClipboardService!.SetTextAsync(text);
            return true;
        }
        catch (SystemException)
        {
            // We don't provide feedback when copying fails.
            return false;
        }
    }
}
