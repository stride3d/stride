// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;
using Stride.Core.Assets.Editor.ViewModels.Progress;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Tracks the source files of the assets of a session, and updates the assets whose source files changed.
/// </summary>
public sealed class AssetSourceTrackerViewModel : DispatcherViewModel, IAssetSourceTrackerViewModel
{
    /// <summary>
    /// The delay before the progress window of an update from source opens, in milliseconds: a quick update shows no window.
    /// </summary>
    public const int ProgressWindowDelay = 500;

    private readonly ISessionViewModel session;
    private readonly PackageSession packageSession;
    private readonly CancellationTokenSource cts = new();
    private readonly ObservableSet<AssetViewModel> assetsToUpdate = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetSourceTrackerViewModel"/> class.
    /// </summary>
    /// <param name="session">The session view model.</param>
    /// <param name="packageSession">The package session, whose source tracker this view model listens to.</param>
    public AssetSourceTrackerViewModel(ISessionViewModel session, PackageSession packageSession)
        : base(session.SafeArgument().ServiceProvider)
    {
        this.session = session;
        this.packageSession = packageSession.SafeArgument();
        UpdateSelectedAssetsFromSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateSelectedAssetsFromSourceAsync);
        UpdateAllAssetsWithModifiedSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateAllAssetsWithModifiedSourceAsync) { IsEnabled = false };

        // This task will listen to the AssetSourceTracker for source file change notifications and collect them, until the session closes
        session.TrackBackgroundWork(PullSourceFileChangesUntilDestroyedAsync());
    }

    /// <summary>
    /// Gets the command that will reimport the selected assets from their source files.
    /// </summary>
    public ICommandBase UpdateSelectedAssetsFromSourceCommand { get; }

    /// <summary>
    /// Gets the command that will reimport all pending assets.
    /// </summary>
    public ICommandBase UpdateAllAssetsWithModifiedSourceCommand { get; }

    /// <summary>
    /// Gets the assets whose source files changed since they were last imported.
    /// </summary>
    public IReadOnlyObservableCollection<AssetViewModel> AssetsToUpdate => assetsToUpdate;

    /// <summary>
    /// Gets a broadcast block from which asset source file events are sent.
    /// </summary>
    public BroadcastBlock<IReadOnlyList<SourceFileChangedData>> SourceFileChanged => packageSession.SourceTracker.SourceFileChanged;

    public override void Destroy()
    {
        base.Destroy();
        cts.Cancel();
    }

    public ObjectId GetCurrentHash(UFile file)
    {
        return packageSession.SourceTracker.GetCurrentHash(file);
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
        return UpdateAssetsFromSourceAsync(session.ActiveAssetView.SelectedAssets);
    }

    private Task UpdateAllAssetsWithModifiedSourceAsync()
    {
        return UpdateAssetsFromSourceAsync(session.AllAssets.Where(x => x.Sources.NeedUpdateFromSource));
    }

    /// <inheritdoc />
    public Task UpdateAssetsFromSourceAsync(IReadOnlyCollection<AssetViewModel> assets)
    {
        return UpdateAssetsFromSourceAsync((IEnumerable<AssetViewModel>)assets);
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

        workProgress.ServiceProvider.Get<IProgressWindowService>().ShowProgressWindow(workProgress, ProgressWindowDelay);
        var undoRedo = ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedo.CreateTransaction())
        {
            var tasks = new List<Task>();
            var updated = new List<AssetViewModel>();
            foreach (var asset in assets)
            {
                updated.Add(asset);
                logger.Verbose($"Updating {asset.Url}...");
                var task = asset.Sources.UpdateAssetFromSource(logger);
                // Continuation might swallow exceptions, careful to keep the original task like this
                task.ContinueWith(_ => logger.Verbose($"{asset.Url} updated")).Forget();
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            logger.Info("Update completed...");
            // One asset (its "Update from source" button) keeps the name it had when the asset ran the update itself
            undoRedo.SetName(transaction, updated.Count == 1 ? $"Update [{updated[0].Url}] from its source(s)" : $"Update {tasks.Count} asset(s) from their source(s)");
        }

        await workProgress.NotifyWorkFinished(false, logger.HasErrors);
    }

    /// <summary>
    /// Runs <see cref="PullSourceFileChangesAsync"/> in the dispatcher until <see cref="Destroy"/> cancels it.
    /// </summary>
    private async Task PullSourceFileChangesUntilDestroyedAsync()
    {
        try
        {
            await Dispatcher.InvokeTask(() => PullSourceFileChangesAsync(cts.Token), cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Destroyed
        }
    }

    private async Task PullSourceFileChangesAsync(CancellationToken token)
    {
        var sourceTracker = packageSession.SourceTracker;
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
                    var asset = session.GetAssetById(sourceFileChange.AssetId)
                                ?? session.AllPackages.SelectMany(x => x.DeletedAssets).FirstOrDefault(x => x.Id == sourceFileChange.AssetId);
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
                    session.NotifyAssetPropertiesChangedAsync(changedAssets.ToList()).Forget();
                    changedAssets.Clear();
                }
            }
        }
    }
}
