// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Yaml;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Storage;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// Watches the asset files of the editable packages of a session, and brings into the session the changes another program makes to them:
    /// a changed asset is reloaded from its file (once its unsaved changes, if any, are confirmed to be discarded), and a new asset file adds its asset.
    /// </summary>
    /// <remarks>
    /// A file whose content is the one the session would save for its asset, as after a save of the session, is not a change.
    /// </remarks>
    internal sealed class ExternalAssetChangeTracker : IDisposable
    {
        // A program writing a file raises several events: the files are processed once they have been quiet for this long
        private const int QuietDelay = 500;

        private static readonly Logger Log = GlobalLogger.GetLogger("ExternalAssetChanges");

        private readonly SessionViewModel session;
        private readonly DirectoryWatcher watcher = new();
        private readonly HashSet<string> trackedFolders = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pendingFiles = new(StringComparer.OrdinalIgnoreCase);
        // The content of the files whose changes the user chose not to reload, so that the same content is not proposed again
        private readonly Dictionary<string, ObjectId> declinedContents = new(StringComparer.OrdinalIgnoreCase);
        private long lastEventTime;
        private bool processing;
        private bool disposed;

        public ExternalAssetChangeTracker([NotNull] SessionViewModel session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            watcher.Modified += FileModified;
            session.LocalPackages.CollectionChanged += LocalPackagesChanged;
            TrackPackages();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            disposed = true;
            session.LocalPackages.CollectionChanged -= LocalPackagesChanged;
            watcher.Modified -= FileModified;
            watcher.Dispose();
        }

        private void LocalPackagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TrackPackages();
        }

        private void TrackPackages()
        {
            foreach (var package in session.LocalPackages.Where(x => x.IsEditable))
            {
                if (package.Package.RootDirectory == null)
                    continue;

                foreach (var folder in package.Package.GetDistinctAssetFolderPaths())
                {
                    var path = folder.ToOSPath();
                    if (Directory.Exists(path) && trackedFolders.Add(path))
                        watcher.Track(path);
                }
            }
        }

        private static bool IsAssetFile(string path)
        {
            var extension = Path.GetExtension(path);
            return AssetRegistry.IsAssetFileExtension(extension)
                   // Code and shaders follow their project, and packages are not assets
                   && !AssetRegistry.IsProjectAssetFileExtension(extension)
                   && !string.Equals(extension, Package.PackageFileExtension, StringComparison.OrdinalIgnoreCase);
        }

        private void FileModified(object sender, FileEvent e)
        {
            var renamedFrom = (e as FileRenameEvent)?.OldFullPath;
            var isAssetFile = IsAssetFile(e.FullPath);
            var wasAssetFile = renamedFrom != null && IsAssetFile(renamedFrom);
            if (!isAssetFile && !wasAssetFile)
                return;

            lock (pendingFiles)
            {
                // A file written to a temporary file then renamed over the asset file is a change of the asset file
                if (isAssetFile)
                    pendingFiles.Add(e.FullPath);
                if (wasAssetFile)
                    pendingFiles.Add(renamedFrom);
                lastEventTime = Environment.TickCount64;
                if (processing)
                    return;
                processing = true;
            }
            ProcessPendingFiles().Forget();
        }

        private async Task ProcessPendingFiles()
        {
            try
            {
                while (true)
                {
                    lock (pendingFiles)
                    {
                        if (disposed || pendingFiles.Count == 0)
                        {
                            processing = false;
                            return;
                        }
                    }

                    await Task.Delay(QuietDelay);
                    lock (pendingFiles)
                    {
                        if (Environment.TickCount64 - lastEventTime < QuietDelay)
                            continue;
                    }

                    await session.Dispatcher.InvokeTask(async () =>
                    {
                        // The session saves its own changes, or is being changed: the files stay pending until it is done
                        if (disposed || session.IsSaving || session.UndoRedoService.TransactionInProgress || session.UndoRedoService.UndoRedoInProgress)
                            return;

                        List<string> files;
                        lock (pendingFiles)
                        {
                            files = pendingFiles.ToList();
                            pendingFiles.Clear();
                        }
                        var assetsByPath = new Dictionary<string, AssetViewModel>(StringComparer.OrdinalIgnoreCase);
                        foreach (var asset in session.LocalPackages.Where(x => x.IsEditable).SelectMany(x => x.Assets))
                        {
                            assetsByPath.TryAdd(asset.AssetItem.FullPath.ToOSPath(), asset);
                        }
                        // The files that exist first: a file renamed or moved is then known as such when its previous path is processed
                        var movedAssets = new HashSet<AssetViewModel>();
                        foreach (var file in files.OrderBy(x => !File.Exists(x)))
                        {
                            try
                            {
                                await ProcessFile(file, assetsByPath, movedAssets);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"The change of [{file}] outside of Game Studio could not be applied.", ex);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                lock (pendingFiles)
                {
                    processing = false;
                }
                Log.Error("The changes made to asset files outside of Game Studio could not be processed.", ex);
            }
        }

        private async Task ProcessFile(string path, IDictionary<string, AssetViewModel> assetsByPath, ISet<AssetViewModel> movedAssets)
        {
            var asset = FindAsset(assetsByPath, path);
            var content = await ReadFile(path);
            if (content == null)
            {
                // A file deleted outside of Game Studio is not deleted from the session: the deletion of an asset can need its references to be fixed
                if (asset != null && !movedAssets.Contains(asset))
                    Log.Warning($"[{asset.Url}] has been deleted outside of Game Studio. It stays in the session: delete it in Game Studio, or save to write it again.");
                declinedContents.Remove(path);
                return;
            }

            // Look again, the session might have changed while the file was read
            asset = FindAsset(assetsByPath, path);
            if (asset == null)
            {
                await AddAsset(path, assetsByPath, movedAssets);
            }
            else if (asset.Asset is not SourceCodeAsset)
            {
                // Code files are reloaded by their own editor
                await ReloadAsset(asset, path, content);
            }
        }

        [ItemCanBeNull]
        private static async Task<byte[]> ReadFile(string path)
        {
            // The program that writes the file might still have it open
            for (var attempt = 0; ; ++attempt)
            {
                try
                {
                    return await File.ReadAllBytesAsync(path);
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    return null;
                }
                catch (IOException) when (attempt < 10)
                {
                    await Task.Delay(100);
                }
            }
        }

        [CanBeNull]
        private AssetViewModel FindAsset(IDictionary<string, AssetViewModel> assetsByPath, string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (!assetsByPath.TryGetValue(fullPath, out var asset))
                return null;

            // Check that the assset is still the one at this path, as it might have been moved or renamed in the session while the file was read
            if (session.GetAssetById(asset.Id) == asset && string.Equals(asset.AssetItem.FullPath.ToOSPath(), fullPath, StringComparison.OrdinalIgnoreCase))
                return asset;

            // Remove invalid assets from the dictionary, so that they are not found again for the same path
            assetsByPath.Remove(fullPath);
            return null;
        }

        private async Task ReloadAsset([NotNull] AssetViewModel asset, string path, [NotNull] byte[] content)
        {
            // The file holds what the session would save for this asset: it has just been saved, or touched without being changed
            var logger = new LoggerResult();
            asset.PrepareSave(logger);
            if (SerializeAsSaved(asset.AssetItem, logger).AsSpan().SequenceEqual(content))
            {
                declinedContents.Remove(path);
                return;
            }

            var contentId = ObjectId.FromBytes(content);
            if (declinedContents.TryGetValue(path, out var declinedId) && declinedId == contentId)
                return;

            var package = asset.AssetItem.Package;
            var assetItem = await Task.Run(() => package.LoadAssetFromFile(asset.AssetItem.FullPath, Log));
            if (assetItem == null || session.GetAssetById(asset.Id) != asset)
                return;

            if (assetItem.Asset.Id != asset.Id || assetItem.Asset.GetType() != asset.AssetType)
            {
                Log.Warning($"[{asset.Url}] has been changed outside of Game Studio into another asset (identifier or type). Close and open the project again to load it.");
                return;
            }

            if (asset.IsDirty)
            {
                var message = string.Format(Tr._p("Message", "{0} has been changed outside of Game Studio, and has unsaved changes here.\r\n\r\nDo you want to reload it from the disk and lose these changes? If you keep them, saving overwrites the changes made outside of Game Studio."), asset.Url);
                var buttons = DialogHelper.CreateButtons([Tr._p("Button", "Reload"), Tr._p("Button", "Keep my changes")], 1, 2);
                var result = await session.Dialogs.MessageBoxAsync(message, buttons, MessageBoxImage.Warning);
                if (result != 1)
                {
                    declinedContents[path] = contentId;
                    return;
                }

                // The asset might have changed or been removed while the question was asked
                if (session.GetAssetById(asset.Id) != asset)
                    return;
            }

            declinedContents.Remove(path);

            // The previous asset comes back on undo with its own Yaml metadata, as it would be saved
            asset.PrepareSave(logger);
            var previousAsset = asset.Asset;
            var previousYamlMetadata = new AttachedYamlAssetMetadata();
            asset.AssetItem.YamlMetadata.CopyInto(previousYamlMetadata);

            var undoRedoService = session.UndoRedoService;
            using (var transaction = undoRedoService.CreateTransaction())
            {
                using (session.CreateAssetFixupContext())
                {
                    var changedDerivedAssets = ReplaceAsset(asset, assetItem.Asset, assetItem.YamlMetadata);
                    undoRedoService.PushOperation(new ReloadAssetOperation(this, asset, previousAsset, previousYamlMetadata, assetItem.Asset, assetItem.YamlMetadata));
                    // The assets deriving from it differ from their files now, as when it is edited in Game Studio
                    if (changedDerivedAssets.Count > 0)
                        undoRedoService.PushOperation(new EmptyDirtyingOperation(changedDerivedAssets.SelectMany(x => x.Dirtiables).Distinct()));
                }
                undoRedoService.SetName(transaction, $"Reload {asset.Url} changed outside of Game Studio");
            }
            // The reloaded asset is what the file holds: it is clean, and undoing the reload makes it dirty
            undoRedoService.NotifySave(asset);
            Log.Info($"[{asset.Url}] has been reloaded: it has been changed outside of Game Studio.");
        }

        // The content the session writes when it saves the asset: its paths are absolute in memory, relative in its file
        [NotNull]
        private static byte[] SerializeAsSaved([NotNull] AssetItem assetItem, ILogger logger)
        {
            AssetAnalysis.ConvertUPaths(assetItem, UPathType.Relative);
            try
            {
                using var stream = new MemoryStream();
                AssetFileSerializer.Save(stream, assetItem.Asset, assetItem.YamlMetadata, logger, assetItem.Package?.Container?.AssetNamespace);
                return stream.ToArray();
            }
            finally
            {
                AssetAnalysis.ConvertUPaths(assetItem, UPathType.Absolute);
            }
        }

        // Returns the assets deriving from the asset whose content has changed with it
        private IReadOnlyCollection<AssetViewModel> ReplaceAsset([NotNull] AssetViewModel asset, [NotNull] Asset newAsset, AttachedYamlAssetMetadata yamlMetadata)
        {
            IReadOnlyCollection<AssetViewModel> changedDerivedAssets = [];
            // An open editor works on the asset being replaced: it is opened again on the new one, in its tab
            var editorsManager = session.ServiceProvider.TryGet<IAssetEditorsManager>();
            if (editorsManager != null)
                editorsManager.ReopenAssetEditorWindow(asset, () => changedDerivedAssets = ReplaceAssetInSession(asset, newAsset, yamlMetadata)).Forget();
            else
                changedDerivedAssets = ReplaceAssetInSession(asset, newAsset, yamlMetadata);
            return changedDerivedAssets;
        }

        private IReadOnlyCollection<AssetViewModel> ReplaceAssetInSession([NotNull] AssetViewModel asset, [NotNull] Asset newAsset, AttachedYamlAssetMetadata yamlMetadata)
        {
            var logger = new LoggerResult();
            asset.UpdateAsset(newAsset, yamlMetadata, logger);
            logger.Messages.Where(x => x.IsAtLeast(LogMessageType.Warning)).ForEach(x => Log.Log(x));

            // The assets deriving from this one link to its nodes. Changes are not propagated from a base in the fixup context:
            // each derived asset that changes is a base whose own derived assets are updated in turn (nested prefabs)
            var changedDerivedAssets = new HashSet<AssetViewModel>();
            var changedAssets = new Queue<AssetViewModel>([asset]);
            while (changedAssets.TryDequeue(out var changedAsset))
            {
                var referencers = session.DependencyManager.ComputeDependencies(changedAsset.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference)?.LinksIn ?? [];
                foreach (var referencer in referencers.Select(x => session.GetAssetById(x.Item.Id)).NotNull().Where(x => x != asset))
                {
                    if (referencer.PropertyGraph != null && (referencer.Asset.Archetype?.Id == changedAsset.Id || referencer.Asset is AssetComposite)
                        && UpdateFromBases(referencer) && changedDerivedAssets.Add(referencer))
                    {
                        changedAssets.Enqueue(referencer);
                    }
                }
            }

            session.NotifyAssetPropertiesChanged([asset]);
            session.ActiveProperties.RefreshSelectedPropertiesAsync().Forget();
            return changedDerivedAssets;
        }

        // Links the asset to the nodes of its bases again and takes their values. Returns whether its content has changed.
        private static bool UpdateFromBases([NotNull] AssetViewModel asset)
        {
            var changed = false;
            void OnChanged(object sender, EventArgs e) => changed = true;

            asset.PropertyGraph.Changed += OnChanged;
            asset.PropertyGraph.ItemChanged += OnChanged;
            try
            {
                asset.PropertyGraph.RefreshBase();
                asset.PropertyGraph.ReconcileWithBase();
            }
            finally
            {
                asset.PropertyGraph.Changed -= OnChanged;
                asset.PropertyGraph.ItemChanged -= OnChanged;
            }
            return changed;
        }

        private async Task AddAsset(string path, IDictionary<string, AssetViewModel> assetsByPath, ISet<AssetViewModel> movedAssets)
        {
            var package = session.LocalPackages.Where(x => x.IsEditable)
                .FirstOrDefault(x => x.Package.RootDirectory != null && x.Package.GetDistinctAssetFolderPaths().Any(f => f.Contains(new UFile(path))));
            if (package == null)
                return;

            var assetFolder = package.Package.GetDistinctAssetFolderPaths().First(x => x.Contains(new UFile(path)));
            var filePath = new UFile(GetActualCase(assetFolder.ToOSPath(), path));
            var assetItem = await Task.Run(() => package.Package.LoadAssetFromFile(filePath, Log));
            if (assetItem == null || FindAsset(assetsByPath, path) != null)
                return;

            var existing = session.GetAssetById(assetItem.Id);
            if (existing != null && !File.Exists(existing.AssetItem.FullPath.ToOSPath()))
            {
                // Its file has been renamed or moved, by a switch of branch for instance. Not followed in the session (yet): it keeps
                // the asset at its location, where a save writes it again, giving two files with the same identifier
                movedAssets.Add(existing);
                Log.Warning($"[{existing.Url}] has been renamed or moved to [{filePath}] outside of Game Studio. Close and open the project again to take it into account: until then, saving changes to it writes it again at its previous location.");
                return;
            }
            if (existing != null)
            {
                Log.Error($"[{filePath}] has been added outside of Game Studio, but it is not added to the session: it has the same identifier as [{existing.Url}].");
                return;
            }
            existing = package.Assets.FirstOrDefault(x => string.Equals(x.Url, assetItem.Location, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Log.Error($"[{filePath}] has been added outside of Game Studio, but it is not added to the session: [{existing.Url}] has the same location.");
                return;
            }

            // The asset is what the file holds: it is not an undoable creation
            var directory = package.GetOrCreateAssetDirectory(GetDisplayDirectory(package.Package, assetItem.Location), false);
            var logger = new LoggerResult();
            var asset = package.CreateAsset(directory, assetItem, false, logger);
            assetsByPath[asset.AssetItem.FullPath.ToOSPath()] = asset;

            // Builds its thumbnail, as for an asset created in Game Studio
            session.NotifyAssetPropertiesChanged([asset]);
            logger.Messages.Where(x => x.IsAtLeast(LogMessageType.Warning)).ForEach(x => Log.Log(x));
            Log.Info($"[{assetItem.Location}] has been added: its file has been created outside of Game Studio.");
        }

        private static string GetDisplayDirectory(Package package, UFile location)
        {
            if (package.Container is { } container)
                location = container.Unqualify(location);
            return location.GetFullDirectory() ?? "";
        }

        // The directory watcher reports lower-cased paths: the file system gives back the actual case of each part below the asset folder
        private static string GetActualCase(string assetFolder, string path)
        {
            var result = assetFolder;
            foreach (var part in Path.GetRelativePath(assetFolder, path).Split(Path.DirectorySeparatorChar))
            {
                result = Directory.EnumerateFileSystemEntries(result, part).FirstOrDefault() ?? Path.Combine(result, part);
            }
            return result;
        }

        /// <summary>
        /// The reload of an asset from its file. It replaces the asset as a whole, so that undoing it brings back the previous asset.
        /// </summary>
        private sealed class ReloadAssetOperation : DirtyingOperation
        {
            private ExternalAssetChangeTracker tracker;
            private AssetViewModel asset;
            private Asset previousAsset;
            private AttachedYamlAssetMetadata previousYamlMetadata;
            private Asset newAsset;
            private AttachedYamlAssetMetadata newYamlMetadata;

            public ReloadAssetOperation(ExternalAssetChangeTracker tracker, AssetViewModel asset, Asset previousAsset, AttachedYamlAssetMetadata previousYamlMetadata, Asset newAsset, AttachedYamlAssetMetadata newYamlMetadata)
                : base([asset])
            {
                this.tracker = tracker;
                this.asset = asset;
                this.previousAsset = previousAsset;
                this.previousYamlMetadata = previousYamlMetadata;
                this.newAsset = newAsset;
                this.newYamlMetadata = newYamlMetadata;
            }

            protected override void FreezeContent()
            {
                tracker = null;
                asset = null;
                previousAsset = null;
                previousYamlMetadata = null;
                newAsset = null;
                newYamlMetadata = null;
            }

            protected override void Undo()
            {
                tracker.ReplaceAsset(asset, previousAsset, previousYamlMetadata);
            }

            protected override void Redo()
            {
                tracker.ReplaceAsset(asset, newAsset, newYamlMetadata);
            }
        }
    }
}
