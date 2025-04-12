// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Tracking;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.IO;
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

        // This task will listen to the AssetSourceTracker for source file change notifications and collect them
        Dispatcher.InvokeTask(() => PullSourceFileChangesAsync(cts.Token), cts.Token);
    }

    public SessionViewModel Session { get; }

    /// <inheritdoc/>
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

        // FIXME xplat-editor
        //UpdateAllAssetsWithModifiedSourceCommand.IsEnabled = assetsToUpdate.Count > 0;
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
