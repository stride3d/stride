// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Templates;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The part of the asset view that adds assets: from a template (the Add asset button and dialog) or from files (the import
/// button, a drop of files). The assets are created in the folder selected in the solution explorer.
/// </summary>
public sealed partial class AssetCollectionViewModel : IAddChildViewModel
{
    private readonly Lazy<AddAssetTemplateCollectionViewModel> addAssetTemplateCollection;

    /// <summary>
    /// The asset templates of the session, grouped for the Add asset list. Built on first use: a view that never shows the list (an
    /// asset picker) does not follow the session's templates.
    /// </summary>
    public AddAssetTemplateCollectionViewModel AddAssetTemplateCollection => addAssetTemplateCollection.Value;

    /// <summary>
    /// Creates an asset from the template given as parameter (an <see cref="ITemplateDescriptionViewModel"/>) in the selected
    /// folder.
    /// </summary>
    public ICommandBase RunAssetTemplateCommand { get; }

    /// <summary>
    /// Asks for files to import and creates assets from them in the selected folder.
    /// </summary>
    public ICommandBase SelectFilesToCreateAssetCommand { get; }

    /// <summary>
    /// Shows the Add asset dialog and creates an asset from the picked template in the selected folder.
    /// </summary>
    public ICommandBase ShowAddAssetDialogCommand { get; }

    private IEditorDialogService EditorDialogs => ServiceProvider.Get<IEditorDialogService>();

    /// <summary>
    /// Creates an asset from a template in the selected folder (or in the first folder accepting the asset type), and selects
    /// the new assets.
    /// </summary>
    /// <param name="template">The template to run.</param>
    /// <param name="files">The source files to import, or <c>null</c> when the template asks for them itself.</param>
    /// <param name="customParameters">Additional parameters of the generator, or <c>null</c>.</param>
    /// <returns>The new assets; none when nothing was created.</returns>
    public async Task<List<AssetViewModel>> RunAssetTemplateAsync(ITemplateDescriptionViewModel? template, IList<UFile>? files, PropertyContainer? customParameters = null)
    {
        if (template is null)
            return [];

        var loggerResult = new LoggerResult();

        var directory = await GetAssetCreationTargetFolderAsync();
        if (directory is null)
            return [];

        if (template.GetTemplate() is not TemplateAssetDescription templateDescription)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Unable to use the selected template because it is not an asset template."), MessageBoxButton.OK, MessageBoxImage.Warning);
            return [];
        }
        var assetType = templateDescription.GetAssetType();
        // If the mount point of the current folder does not support this type of asset, try to select the first mount point that support it.
        directory = AssetViewModel.FindValidCreationLocation(assetType, directory, Session.CurrentProject);
        if (directory is null)
            return [];

        var name = string.Empty;
        if (templateDescription.RequireName)
        {
            name = templateDescription.DefaultOutputName ?? templateDescription.AssetTypeName;
        }

        return await InvokeAddAssetTemplateAsync(loggerResult, name, directory, templateDescription, files, customParameters);
    }

    /// <summary>
    /// Creates assets from files dropped on the view or picked by the user: the templates importing their extensions are
    /// searched, and the user picks one when several apply.
    /// </summary>
    /// <param name="files">The files, or folders whose files are imported.</param>
    public async Task AddFilesAsync(IReadOnlyCollection<UFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var fileList = new List<UFile>();
        foreach (var file in files)
        {
            ConstructFileList(file, fileList);
        }
        if (fileList.Count == 0)
            return;

        // Compute the list of compatible templates per file extension
        var extensionsList = new Dictionary<string, int>();
        foreach (var file in fileList)
        {
            var extension = file.GetFileExtension() ?? "";
            extensionsList[extension] = extensionsList.TryGetValue(extension, out var count) ? count + 1 : 1;
        }
        var extensions = new Dictionary<string, List<TemplateAssetDescription>>();
        foreach (var extension in extensionsList.Keys)
        {
            var assetTemplates = Session.FindTemplates(TemplateScope.Asset).OfType<TemplateAssetDescription>();
            var matchingTemplates = assetTemplates.Where(x => x.ImportSource && x.FindAssetType() is not null && x.GetSupportedExtensions().Contains(extension));
            extensions[extension] = matchingTemplates.OrderBy(x => x.Order).ToList();
        }

        // Ensure that all template collections matches. This allows to have different extension of the same type of asset (such as fbx/obj or png/tga/...) to still work, but not fbx/png.
        var firstList = extensions.First();
        var templateMismatch = extensions.Any(extension => !ArrayExtensions.ArraysReferenceEqual(firstList.Value, extension.Value));

        Dictionary<TemplateAssetDescription, int>? templatesDictionary = null;
        if (templateMismatch)
        {
            // If we have a mismatch, we'll use a slightly different path. In this case we build a dictionary indicating the number of matching files per template
            // NOTE: This will break the order, but it's not a problem as long as AssetTemplatesViewModel rebuild the proper order.
            templatesDictionary = [];
            foreach (var extension in extensionsList)
            {
                foreach (var template in extensions[extension.Key])
                {
                    templatesDictionary[template] = templatesDictionary.TryGetValue(template, out var count) ? count + extension.Value : extension.Value;
                }
            }
        }

        var templates = firstList.Value;
        // Note: this should never happen since we have raw assets, litteraly everything can be imported.
        if (templates.Count == 0)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "These files aren't supported."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Don't display the template selection window if there is no choice to make
        if (templates.Count == 1)
        {
            await RunAssetTemplateAsync(new TemplateDescriptionViewModel(ServiceProvider, templates[0]), fileList);
            return;
        }

        if (!templateMismatch)
        {
            // No mismatch, the easy way
            var selectedTemplate = await ShowAssetTemplatesDialogAsync(new AssetTemplatesViewModel(ServiceProvider, templates));
            if (selectedTemplate is not null)
            {
                await RunAssetTemplateAsync(selectedTemplate, fileList);
            }
        }
        else
        {
            // Mismatch, we pass additional information because we want to display some numbers
            var selectedTemplate = await ShowAssetTemplatesDialogAsync(new AssetTemplatesViewModel(ServiceProvider, fileList.Count, templatesDictionary!));
            if (selectedTemplate is not null)
            {
                // Rebuild a list of files that matches the selected template
                fileList = fileList.Where(x => extensions[x.GetFileExtension() ?? ""].Contains((TemplateAssetDescription)selectedTemplate.GetTemplate())).ToList();
                await RunAssetTemplateAsync(selectedTemplate, fileList);
            }
        }
    }

    /// <inheritdoc/>
    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        var directories = GetSelectedDirectories(false);
        if (directories.Count != 1)
        {
            message = Tr._p("Message", "This location is invalid. Please select a package folder in the Solution explorer.");
            return false;
        }

        if (children.All(x => x is UFile))
        {
            message = Tr._p("Message", "Drop files");
            return true;
        }
        message = DragDropBehavior.InvalidDropAreaMessage;
        return false;
    }

    /// <inheritdoc/>
    async void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        await AddFilesAsync(children.OfType<UFile>().ToList());
    }

    private AddAssetTemplateCollectionViewModel CreateAddAssetTemplateCollection()
    {
        // Only a view showing the Add asset list follows the session (the asset picker's views never build it)
        Session.AssetTypesChanged += AssetTypesChanged;
        return new AddAssetTemplateCollectionViewModel(Session);
    }

    private void DestroyAddAssetTemplateCollection()
    {
        if (addAssetTemplateCollection.IsValueCreated)
        {
            Session.AssetTypesChanged -= AssetTypesChanged;
            addAssetTemplateCollection.Value.Destroy();
        }
    }

    private void AssetTypesChanged(object? sender, EventArgs e)
    {
        addAssetTemplateCollection.Value.Refresh();
    }

    private async Task ShowAddAssetDialogAsync()
    {
        var directory = await GetAssetCreationTargetFolderAsync();
        if (directory is null)
            return;

        var templates = new AddAssetTemplateCollectionViewModel(Session) { CurrentDirectory = directory };
        try
        {
            var selectedTemplate = await EditorDialogs.ShowAddAssetDialogAsync(templates);
            if (selectedTemplate is not null)
            {
                await RunAssetTemplateAsync(selectedTemplate, null);
            }
        }
        finally
        {
            templates.Destroy();
        }
    }

    private async Task<TemplateDescriptionViewModel?> ShowAssetTemplatesDialogAsync(AssetTemplatesViewModel templates)
    {
        try
        {
            return await EditorDialogs.ShowAssetTemplatesDialogAsync(templates);
        }
        finally
        {
            templates.Destroy();
        }
    }

    private async Task SelectFilesToCreateAssetAsync()
    {
        var lastDirectory = InternalSettings.FileDialogLastImportDirectory.GetValue();
        var files = await Dialogs.OpenMultipleFilesPickerAsync(string.IsNullOrEmpty(lastDirectory) ? null : new UDirectory(lastDirectory));
        if (files.Count > 0)
        {
            InternalSettings.FileDialogLastImportDirectory.SetValue(files[0].GetFullDirectory());
            InternalSettings.SaveProfile();
            await AddFilesAsync(files);
        }
    }

    private static string ComputeNamespace(DirectoryBaseViewModel directory)
    {
        return directory switch
        {
            ProjectCodeViewModel projectCode => projectCode.Project.RootNamespace,
            { Parent: { } parent } => $"{ComputeNamespace(parent)}.{directory.Name}",
            _ => directory.Name,
        };
    }

    private async Task<string> GetAssetCopyDirectoryAsync(DirectoryBaseViewModel directory, UFile file)
    {
        var path = directory.Path;
        var message = Tr._p("Message", "Do you want to place the resource in the default location ?");
        var finalPath = Path.GetFullPath(Path.Combine(directory.Package.Package.ResourceFolders[0], path, file.GetFileName()));
        var pathResult = await Dialogs.MessageBoxAsync(message, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (pathResult == MessageBoxResult.No)
        {
            while (true)
            {
                var filePath = await Dialogs.SaveFilePickerAsync(
                    Path.GetFullPath(directory.Package.Package.ResourceFolders[0].FullPath),
                    [new FilePickerFilter("") { Patterns = [file.GetFileExtension()] }],
                    defaultFileName: file.GetFileName());

                // If the user closes the dialog, assume that they want to use the default directory
                if (filePath is null)
                {
                    return finalPath;
                }

                var fullPath = Path.GetFullPath(filePath);

                var inResource = directory.Package.Package.ResourceFolders.Any(x => fullPath.StartsWith(Path.GetFullPath(x.FullPath), StringComparison.Ordinal));
                if (inResource)
                {
                    return fullPath;
                }

                message = Tr._p("Message", "The selected directory is not a subdirectory of the resources folder!");
                await Dialogs.MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        return finalPath;
    }

    private async Task<List<AssetViewModel>> InvokeAddAssetTemplateAsync(LoggerResult logger, string name, DirectoryBaseViewModel directory, TemplateAssetDescription templateDescription, IList<UFile>? files, PropertyContainer? customParameters)
    {
        const int DialogClosed = 0;
        const int DialogYes = 1;
        const int DialogNo = 2;
        const int DialogYesToAll = 3;
        const int DialogNoToAll = 4;

        var newAssets = new List<AssetViewModel>();
        var copyPromptWithToAllButtons = DialogButtons.Create(
        [
            Tr._p("Button", "Yes"),
            Tr._p("Button", "No"),
            Tr._p("Button", "Yes to all"),
            Tr._p("Button", "No to all")
        ], 1, 2);

        var overwritePromptWithToAllButtons = DialogButtons.Create(
        [
            Tr._p("Button", "Yes"),
            Tr._p("Button", "No"),
            Tr._p("Button", "Yes to all")
        ], 1, 2);

        var dialogDefaultButtons = DialogButtons.Create(
        [
            Tr._p("Button", "Yes"),
            Tr._p("Button", "No")
        ], 1, 2);

        var yesToAll = false;
        var overwriteAll = false;
        var finalPath = string.Empty;

        if (files is not null)
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var inResourceFolder = directory.Package.Package.ResourceFolders.Any(x => file.FullPath.StartsWith(x.FullPath, StringComparison.Ordinal));

                if (inResourceFolder)
                {
                    continue;
                }

                if (!yesToAll)
                {
                    var message = Tr._p("Message", "Source file '{0}' is not inside of your project's resource folders, do you want to copy it?").ToFormat(file.FullPath);

                    var copyResult = await Dialogs.MessageBoxAsync(message, i != files.Count - 1 ? copyPromptWithToAllButtons : dialogDefaultButtons, MessageBoxImage.Warning);

                    if (copyResult is DialogClosed or DialogNo)
                    {
                        continue;
                    }

                    if (copyResult is DialogNoToAll)
                    {
                        break;
                    }

                    if (copyResult is DialogYesToAll)
                    {
                        yesToAll = true;
                    }

                    if (copyResult is DialogYes or DialogYesToAll)
                    {
                        finalPath = await GetAssetCopyDirectoryAsync(directory, file);
                    }
                }
                else
                {
                    // If "Yes to all" we're going to assume they want to use the same directory as the initial file.
                    finalPath = Path.Combine(Path.GetDirectoryName(finalPath)!, file.GetFileName());
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
                    if (File.Exists(finalPath))
                    {
                        if (!overwriteAll)
                        {
                            var message = Tr._p("Message", "The file '{0}' already exists, it will get overwritten if you continue, do you really want to proceed?").ToFormat(finalPath);

                            var copyResult = await Dialogs.MessageBoxAsync(message, i != files.Count - 1 ? overwritePromptWithToAllButtons : dialogDefaultButtons, MessageBoxImage.Warning);

                            overwriteAll = copyResult is DialogYesToAll;

                            if (copyResult is DialogClosed)
                            {
                                return newAssets;
                            }

                            if (copyResult is DialogNo)
                            {
                                continue;
                            }
                        }
                        File.Copy(file.FullPath, finalPath, true);
                    }
                    else
                    {
                        File.Copy(file.FullPath, finalPath);
                    }

                    files[i] = new UFile(finalPath);
                }
                catch (Exception ex)
                {
                    var message = Tr._p("Message", "An error occurred while copying the asset to the resources folder: {0}").ToFormat(ex.Message);
                    await Dialogs.MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
                    return newAssets;
                }
            }
        }

        var parameters = new AssetTemplateGeneratorParameters(directory.Path, files)
        {
            Name = name,
            Description = templateDescription,
            Package = directory.Package.Package,
            Logger = logger,
            Namespace = ComputeNamespace(directory),
        };

        if (customParameters.HasValue)
        {
            foreach (var tag in customParameters.Value)
            {
                parameters.Tags[tag.Key] = tag.Value;
            }
        }

        var generator = TemplateManager.FindTemplateGenerator(parameters);
        if (generator is null)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
            return newAssets;
        }

        var workProgress = new WorkProgressViewModel(ServiceProvider, logger)
        {
            Title = Tr._p("Title", "Add asset…"),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false, // The process is not cancellable at the beginning.
        };
        workProgress.RegisterProgressStatus(logger, true);

        try
        {
            var bufferBlock = new BufferBlock<IReadOnlyList<SourceFileChangedData>>();
            var cancel = new CancellationTokenSource();
            List<AssetItem> newAssetItems;

            // The source tracker tells when the hashes of the source files of the new assets are known
            using (Session.SourceTracker?.SourceFileChanged.LinkTo(bufferBlock))
            {
                // Run the template generator first. This method also takes care of showing the progress window.
                await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress);

                var previousAssetItems = new HashSet<AssetItem>(directory.Package.Assets.Select(x => x.AssetItem));
                newAssetItems = new List<AssetItem>(directory.Package.Package.Assets.Where(x => !previousAssetItems.Contains(x)));

                // No asset to create, early exit.
                if (newAssetItems.Count == 0)
                {
                    logger.Info("Nothing to import.");
                    return newAssets;
                }

                // Collect all the source files that affect the assets, we want their hashes before creating view models.
                var sources = new Dictionary<AssetId, HashSet<UFile>>();
                var collector = new SourceFilesCollector();
                foreach (var newAsset in newAssetItems)
                {
                    var assetSources = collector.GetSourceFiles(newAsset.Asset).Where(x => x.Value).Select(x => x.Key).ToList();
                    if (assetSources.Count > 0)
                    {
                        sources.Add(newAsset.Id, new HashSet<UFile>(assetSources));
                    }
                }

                if (Session.SourceTracker is { } sourceTracker && sources.Count > 0)
                {
                    logger.Info($"Computing hashes of {sources.Sum(x => x.Value.Count)} source files...");
                    // From now the import become cancellable. If for some reason, the hashes of some source files is never computed,
                    // this process will wait forever until the user cancels.
                    workProgress.IsCancellable = true;
                    workProgress.CancelCommand = new AnonymousCommand(ServiceProvider, () => cancel.Cancel());

                    while (sources.Count > 0)
                    {
                        IReadOnlyList<SourceFileChangedData> changes;
                        try
                        {
                            changes = await bufferBlock.ReceiveAsync(cancel.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        if (cancel.IsCancellationRequested)
                        {
                            break;
                        }

                        foreach (var change in changes)
                        {
                            // Filter out changes unrelated to our new assets.
                            if (!sources.TryGetValue(change.AssetId, out var assetSources))
                                continue;

                            var assetItem = newAssetItems.First(x => x.Id == change.AssetId);

                            // We care only about source files changes
                            if (change.Type == SourceFileChangeType.SourceFile)
                            {
                                // Retrieve the currently stored hashes for this asset.
                                var assetHashes = SourceHashesHelper.GetAllHashes(assetItem.Asset);
                                foreach (var file in change.Files)
                                {
                                    // Update it with newly computed hashes
                                    var hash = sourceTracker.GetCurrentHash(file);
                                    assetHashes[file] = hash;
                                    // Remove hashes that have been registered.
                                    assetSources.Remove(file);
                                    logger.Verbose($"Computed hash of {file} for asset {assetItem.Location}. {sources.Sum(x => x.Value.Count)} files remaining...");
                                }

                                // Push the changes we did to the stored hashes.
                                SourceHashesHelper.UpdateHashes(assetItem.Asset, assetHashes);

                                // Remove this asset from the list if we got all its hashes
                                if (assetSources.Count == 0)
                                    sources.Remove(assetItem.Id);
                            }
                        }
                    }

                    if (cancel.IsCancellationRequested)
                    {
                        logger.Info("Operation cancelled");
                    }
                }
            }

            // If user cancelled, stops here and return an empty list.
            if (cancel.IsCancellationRequested)
                return newAssets;

            // Actually create the transaction and the view models now.
            var undoRedoService = Session.UndoRedoService;
            using (var transaction = undoRedoService?.CreateTransaction())
            {
                newAssets = newAssetItems.Select(assetItem => directory.Package.CreateAsset(directory, assetItem, true, logger)).ToList();
                if (transaction is not null)
                    undoRedoService!.SetName(transaction, newAssets.Count == 1 ? $"Create asset '{newAssets[0].Url}'" : $"Create {newAssets.Count} assets");
            }

            Session.CheckConsistency();
            if (parameters.RequestSessionSave)
            {
                await Session.SaveSessionAsync();
            }

            SelectAssets(newAssets);
            return newAssets;
        }
        catch (Exception e)
        {
            logger.Error("There was a problem generating the asset", e);
            return newAssets;
        }
        finally
        {
            await workProgress.NotifyWorkFinished(false, logger.HasErrors);
        }
    }

    private async Task<DirectoryBaseViewModel?> GetAssetCreationTargetFolderAsync()
    {
        var directories = GetSelectedDirectories(false);
        var directoryCount = directories.Count;
        if (directoryCount > 1)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Game Studio can't create assets in multiple locations. In the solution explorer, select a single directory or package to create the asset in."), MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        if (directoryCount == 0)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Game Studio can't create an asset here. In the solution explorer, select a directory or package to create the asset in."), MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var directory = directories.First();
        if (!directory.Package.IsEditable)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "Game Studio can't create an asset here because the selected directory or package can't be edited. In the solution explorer, select a directory or package to create the asset in."), MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
        return directory;
    }

    private static void ConstructFileList(UFile file, List<UFile> fileList)
    {
        try
        {
            if (Directory.Exists(file))
            {
                foreach (var subFile in Directory.GetFiles(file))
                {
                    ConstructFileList(subFile, fileList);
                }
                foreach (var subDir in Directory.GetDirectories(file))
                {
                    ConstructFileList(subDir, fileList);
                }
            }
            else
            {
                fileList.Add(file);
            }
        }
        catch (Exception e)
        {
            e.Ignore();
        }
    }
}
