// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public class AssetSourcesViewModel : DispatcherViewModel
    {
        private readonly AssetViewModel asset;
        private IReadOnlyDictionary<UFile, ObjectId> updatedHashes;
        private readonly HashSet<UFile> currentSourceFiles = new HashSet<UFile>();
        private bool needUpdateFromSource;
        private bool updatingFromSource;

        public AssetSourcesViewModel(AssetViewModel asset) : base(asset.SafeArgument(nameof(asset)).ServiceProvider)
        {
            this.asset = asset;
            UpdateFromSourceCommand = new AnonymousTaskCommand(ServiceProvider, UpdateAssetFromSource);
            updatedHashes = SourceHashesHelper.GetAllHashes(asset.Asset);
            currentSourceFiles.AddRange(updatedHashes.Keys);
        }

        /// <summary>
        /// Gets or sets whether the asset related to this view model needs to be updated from its source files.
        /// </summary>
        public bool NeedUpdateFromSource { get { return needUpdateFromSource; } private set { SetValue(ref needUpdateFromSource, value); } }

        public ICommandBase UpdateFromSourceCommand { get; }

        internal void UpdateUsedHashes(IReadOnlyList<UFile> files)
        {
            currentSourceFiles.Clear();
            currentSourceFiles.AddRange(files);
            ComputeNeedUpdateFromSource();
        }

        internal void ComputeNeedUpdateFromSource()
        {
            var result = false;
            foreach (var file in currentSourceFiles)
            {
                ObjectId hash;
                if (!updatedHashes.TryGetValue(file, out hash))
                {
                    result = true;
                    break;
                }

                var latestHash = asset.Session.SourceTracker.GetCurrentHash(file);
                if (hash != latestHash)
                {
                    result = true;
                    break;
                }
            }

            var notifyTracker = NeedUpdateFromSource != result;

            NeedUpdateFromSource = result;

            if (notifyTracker)
                asset.Session.SourceTracker?.UpdateAssetStatus(asset);
        }

        public async Task UpdateAssetFromSource(LoggerResult logger)
        {
            await asset.UpdateAssetFromSource(logger);

            var oldHashes = new Dictionary<UFile, ObjectId>((Dictionary<UFile, ObjectId>)updatedHashes);
            var newHashes = new Dictionary<UFile, ObjectId>();
            foreach (var file in currentSourceFiles)
            {
                var hash = asset.Session.SourceTracker.GetCurrentHash(file);
                newHashes[file] = hash;
            }
            // Add an operation to update hashes in the SourceHashHelper
            var operation = new AnonymousDirtyingOperation(asset.Dirtiables,
                () =>
                {
                    SourceHashesHelper.UpdateHashes(asset.Asset, oldHashes);
                    updatedHashes = oldHashes;
                    ComputeNeedUpdateFromSource();
                },
                () =>
                {
                    SourceHashesHelper.UpdateHashes(asset.Asset, newHashes);
                    updatedHashes = newHashes;
                    ComputeNeedUpdateFromSource();
                });
            asset.UndoRedoService.PushOperation(operation);

            updatedHashes = newHashes;
            SourceHashesHelper.UpdateHashes(asset.Asset, updatedHashes);

            NeedUpdateFromSource = false;
            asset.Session.SourceTracker?.UpdateAssetStatus(asset);
        }

        private async Task UpdateAssetFromSource()
        {
            if (updatingFromSource)
                return;

            updatingFromSource = true;
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

            using (var transaction = asset.UndoRedoService.CreateTransaction())
            {
                await UpdateAssetFromSource(logger);
                asset.UndoRedoService.SetName(transaction, $"Update [{asset.Url}] from its source(s)");
            }

            await workProgress.NotifyWorkFinished(false, logger.HasErrors);
            updatingFromSource = false;
        }
    }
}
