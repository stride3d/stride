// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class AssetSourceTrackerViewModel : DispatcherViewModel, IAssetSourceTrackerViewModel
{
    private readonly CancellationTokenSource cts = new();
    private readonly ObservableSet<AssetViewModel> assetsToUpdate = [];

    public AssetSourceTrackerViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
        UpdateSelectedAssetsFromSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateSelectedAssetsFromSourceAsync);
        UpdateAllAssetsWithModifiedSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateAllAssetsWithModifiedSourceAsync) { IsEnabled = false };

        // This task will listen to the AssetSourceTracker for source file change notifications and collect them
        Dispatcher.InvokeTask(() => PullSourceFileChangesAsync(cts.Token), cts.Token);
    }

    public SessionViewModel Session { get; }

    public ICommandBase UpdateSelectedAssetsFromSourceCommand { get; }

    public ICommandBase UpdateAllAssetsWithModifiedSourceCommand { get; }

    /// <summary>
    /// Gets the assets whose source files changed since they were last imported.
    /// </summary>
    public IReadOnlyObservableCollection<AssetViewModel> AssetsToUpdate => assetsToUpdate;

    public BroadcastBlock<IReadOnlyList<SourceFileChangedData>> SourceFileChanged => Session.PackageSession.SourceTracker.SourceFileChanged;

    public override void Destroy()
    {
        base.Destroy();
        cts.Cancel();
    }

    public ObjectId GetCurrentHash(UFile file)
    {
        return Session.PackageSession.SourceTracker.GetCurrentHash(file);
    }

    public void UpdateAssetStatus(AssetViewModel asset)
    {
        if (asset.Sources.NeedUpdateFromSource && !asset.IsDeleted)
            assetsToUpdate.Add(asset);
        else
            assetsToUpdate.Remove(asset);

        UpdateAllAssetsWithModifiedSourceCommand.IsEnabled = assetsToUpdate.Count > 0;
    }

    private Task UpdateSelectedAssetsFromSourceAsync()
    {
        return UpdateAssetsFromSourceAsync(Session.AssetCollection.SelectedAssets);
    }

    private Task UpdateAllAssetsWithModifiedSourceAsync()
    {
        return UpdateAssetsFromSourceAsync(Session.AllAssets.Where(x => x.Sources.NeedUpdateFromSource));
    }

    private async Task UpdateAssetsFromSourceAsync(IEnumerable<AssetViewModel> assets)
    {
        var logger = new LoggerResult();
        var workProgress = new WorkProgressViewModel(ServiceProvider, logger)
        {
            Title = "Update assets from source",
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false,
        };
        workProgress.RegisterProgressStatus(logger, true);

        workProgress.ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress);
        var undoRedo = ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedo.CreateTransaction())
        {
            var tasks = new List<Task>();
            foreach (var asset in assets)
            {
                logger.Verbose($"Updating {asset.Url}...");
                var task = asset.Sources.UpdateAssetFromSource(logger);
                // Continuation might swallow exceptions, careful to keep the original task like this
                task.ContinueWith(_ => logger.Verbose($"{asset.Url} updated")).Forget();
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            logger.Info("Update completed...");
            undoRedo.SetName(transaction, $"Update {tasks.Count} asset(s) from their source(s)");
        }

        await workProgress.NotifyWorkFinished(false, logger.HasErrors);
    }

    private async Task PullSourceFileChangesAsync(CancellationToken token)
    {
        var sourceTracker = Session.PackageSession.SourceTracker;
        var bufferBlock = new BufferBlock<IReadOnlyList<SourceFileChangedData>>();
        var changedAssets = new HashSet<AssetViewModel>();
        var sourceFileChanges = new List<SourceFileChangedData>();

        using (sourceTracker.SourceFileChanged.LinkTo(bufferBlock))
        {
            sourceTracker.EnableTracking = true;
            while (!IsDestroyed)
            {
                // Await for source file changes
                await bufferBlock.OutputAvailableAsync(token);
                if (token.IsCancellationRequested)
                    return;

                // Try as much as possible to process all changes at once
                if (!bufferBlock.TryReceiveAll(out var changes))
                    continue;

                sourceFileChanges.AddRange(changes.SelectMany(x => x));
                if (sourceFileChanges.Count == 0)
                    continue;

                for (var i = 0; i < sourceFileChanges.Count; i++)
                {
                    var sourceFileChange = sourceFileChanges[i];
                    // Look first in the assets of the session, then in the list of deleted assets.
                    var asset = Session.GetAssetById(sourceFileChange.AssetId)
                                ?? Session.AllPackages.SelectMany(x => x.DeletedAssets).FirstOrDefault(x => x.Id == sourceFileChange.AssetId);
                    if (asset is null)
                        continue;

                    // We care only about changes that needs to update
                    if (sourceFileChange.NeedUpdate)
                    {
                        switch (sourceFileChange.Type)
                        {
                            case SourceFileChangeType.Asset:
                                // The asset itself has changed and now references a new source
                                asset.Sources.UpdateUsedHashes(sourceFileChange.Files);
                                break;
                            case SourceFileChangeType.SourceFile:
                                // A source file referenced by the asset has changed
                                asset.Sources.ComputeNeedUpdateFromSource();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    // The asset has been updated, remove the change data from the list
                    sourceFileChanges.RemoveAt(i--);

                    if (!asset.IsDeleted)
                    {
                        // We will notify only for undeleted assets
                        changedAssets.Add(asset);
                    }
                }

                // Notify all changed assets at once.
                if (changedAssets.Count > 0)
                {
                    Session.NotifyAssetPropertiesChangedAsync(changedAssets.ToList()).Forget();
                    changedAssets.Clear();
                }
            }
        }
    }
}
