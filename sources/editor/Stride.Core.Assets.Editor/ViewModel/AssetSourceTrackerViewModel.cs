// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class AssetSourceTrackerViewModel : DispatcherViewModel
    {
        private readonly PackageSession session;
        private readonly SessionViewModel sessionViewModel;
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly ObservableSet<AssetViewModel> assetsToUpdate = new ObservableSet<AssetViewModel>();

        public AssetSourceTrackerViewModel(IViewModelServiceProvider serviceProvider, PackageSession packageSession, SessionViewModel session)
            : base(serviceProvider)
        {
            this.session = packageSession;
            sessionViewModel = session;
            UpdateSelectedAssetsFromSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateSelectedAssetsFromSource);
            UpdateAllAssetsWithModifiedSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateAllAssetsWithModifiedSource) { IsEnabled = false };
            // This task will listen to the AssetSourceTracker for source file change notifications and collect them
            Dispatcher.InvokeTask(PullSourceFileChanges);
        }

        /// <summary>
        /// Gets the command that will reimport the pending assets in the <see cref="System.Collections.IEnumerable"/> passed as parameter.
        /// </summary>
        public ICommandBase UpdateSelectedAssetsFromSourceCommand { get; }

        /// <summary>
        /// Gets the command that will reimport all pending assets.
        /// </summary>
        public ICommandBase UpdateAllAssetsWithModifiedSourceCommand { get; }

        /// <summary>
        /// Gets the list of assets that need to be updated.
        /// </summary>
        public IReadOnlyObservableCollection<AssetViewModel> AssetsToUpdate => assetsToUpdate;

        /// <summary>
        /// Gets a broadcast block from which asset source file events are sent.
        /// </summary>
        public BroadcastBlock<IReadOnlyList<SourceFileChangedData>> SourceFileChanged => session.SourceTracker.SourceFileChanged;

        /// <inheritdoc/>
        public override void Destroy()
        {
            base.Destroy();
            cancel.Cancel();
        }

        public ObjectId GetCurrentHash(UFile file)
        {
            return session.SourceTracker.GetCurrentHash(file);
        }

        internal void UpdateAssetStatus(AssetViewModel asset)
        {
            if (asset.Sources.NeedUpdateFromSource && !asset.IsDeleted)
                assetsToUpdate.Add(asset);
            else
                assetsToUpdate.Remove(asset);

            UpdateAllAssetsWithModifiedSourceCommand.IsEnabled = assetsToUpdate.Count > 0;
        }

        private Task UpdateSelectedAssetsFromSource()
        {
            return UpdateAssetsFromSource(sessionViewModel.ActiveAssetView.SelectedAssets);
        }

        private Task UpdateAllAssetsWithModifiedSource()
        {
            return UpdateAssetsFromSource(sessionViewModel.AllAssets.Where(x => x.Sources.NeedUpdateFromSource));
        }

        private async Task UpdateAssetsFromSource(IEnumerable<AssetViewModel> assets)
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

            workProgress.ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress, 500);
            var undoRedo = ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedo.CreateTransaction())
            {
                var tasks = new List<Task>();
                foreach (var asset in assets)
                {
                    logger.Verbose($"Updating {asset.Url}...");
                    var task = asset.Sources.UpdateAssetFromSource(logger);
                    // Continuation might swallow exceptions, careful to keep the original task like this
                    task.ContinueWith(x => logger.Verbose($"{asset.Url} updated")).Forget();
                    tasks.Add(task);
                }
                await Task.WhenAll(tasks);
                logger.Info("Update completed...");
                undoRedo.SetName(transaction, $"Update {tasks.Count} asset(s) from their source(s)");
            }

            await workProgress.NotifyWorkFinished(false, logger.HasErrors);
        }

        private async Task PullSourceFileChanges()
        {
            var sourceTracker = session.SourceTracker;
            var bufferBlock = new BufferBlock<IReadOnlyList<SourceFileChangedData>>();
            var changedAssets = new HashSet<AssetViewModel>();
            var sourceFileChanges = new List<SourceFileChangedData>();

            using (sourceTracker.SourceFileChanged.LinkTo(bufferBlock))
            {
                sourceTracker.EnableTracking = true;
                while (!IsDestroyed)
                {
                    // Await for source file changes
                    await bufferBlock.OutputAvailableAsync(cancel.Token);
                    if (cancel.IsCancellationRequested)
                        return;

                    // Try as much as possible to process all changes at once
                    IList<IReadOnlyList<SourceFileChangedData>> changes;
                    bufferBlock.TryReceiveAll(out changes);
                    sourceFileChanges.AddRange(changes.SelectMany(x => x));

                    if (sourceFileChanges.Count == 0)
                        continue;

                    for (var i = 0; i < sourceFileChanges.Count; i++)
                    {
                        var sourceFileChange = sourceFileChanges[i];
                        // Look first in the assets of the session, then in the list of deleted assets.
                        var asset = sessionViewModel.GetAssetById(sourceFileChange.AssetId)
                                    ?? sessionViewModel.AllPackages.SelectMany(x => x.DeletedAssets).FirstOrDefault(x => x.Id == sourceFileChange.AssetId);

                        if (asset == null)
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
                        sessionViewModel.NotifyAssetPropertiesChanged(changedAssets.ToList());
                        changedAssets.Clear();
                    }
                }
            }
        }
    }
}
