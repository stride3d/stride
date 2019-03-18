// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.MicroThreading;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;
using Xenko.Core.Presentation.Services;
using Xenko.Assets;
using Xenko.Assets.Entities;
using Xenko.Assets.Materials;
using Xenko.Assets.Navigation;
using Xenko.Assets.Textures;
using Xenko.Editor.Build;
using Xenko.Editor.EditorGame.Game;
using Xenko.Graphics;
using Xenko.Navigation;

namespace Xenko.Editor.EditorGame.ContentLoader
{
    /// <summary>
    /// A class that handles loading/unloading referenced resources for a game used in an editor.
    /// </summary>
    public sealed class EditorContentLoader : IEditorContentLoader
    {
        private readonly ILogger logger;
        private readonly GameStudioDatabase database;
        private readonly GameSettingsProviderService settingsProvider;
        private RenderingMode currentRenderingMode;
        private ColorSpace currentColorSpace;
        private ObjectId currentNavigationGroupsHash;
        private int loadingAssetCount;
#if DEBUG
        private ContentManagerStats debugStats;
        private bool enableReferenceLogging = true;
#endif

        /// <summary>
        /// RW lock for <see cref="assetsToReloadQueue"/> and <see cref="assetsToReloadMapping"/>.
        /// </summary>
        private readonly object assetsToReloadLock = new object();

        /// <summary>
        /// The assets currently waiting for a reload to be done.
        /// </summary>
        private readonly Queue<ReloadingAsset> assetsToReloadQueue = new Queue<ReloadingAsset>();

        /// <summary>
        /// Fast lookup to know what is in <see cref="assetsToReloadQueue"/>.
        /// </summary>
        private readonly Dictionary<AssetItem, ReloadingAsset> assetsToReloadMapping = new Dictionary<AssetItem, ReloadingAsset>();


        /// <summary>
        /// Initializes a new instance of the <see cref="EditorContentLoader"/> class
        /// </summary>
        /// <param name="gameDispatcher">The dispatcher to the game thread.</param>
        /// <param name="logger">The logger to use to log operations.</param>
        /// <param name="asset">The asset associated with this instance.</param>
        /// <param name="game">The editor game associated with this instance.</param>
        public EditorContentLoader(IDispatcherService gameDispatcher, ILogger logger, AssetViewModel asset, EditorServiceGame game)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            GameDispatcher = gameDispatcher ?? throw new ArgumentNullException(nameof(gameDispatcher));
            Session = asset.Session;
            Session.AssetPropertiesChanged += AssetPropertiesChanged;
            Game = game ?? throw new ArgumentNullException(nameof(game));
            Asset = asset;
            Manager = new LoaderReferenceManager(GameDispatcher, this);
            this.logger = logger;
            database = asset.ServiceProvider.Get<GameStudioDatabase>();
            settingsProvider = asset.ServiceProvider.Get<GameSettingsProviderService>();
            settingsProvider.GameSettingsChanged += GameSettingsChanged;
            currentRenderingMode = settingsProvider.CurrentGameSettings.GetOrCreate<EditorSettings>().RenderingMode;
            currentColorSpace = settingsProvider.CurrentGameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
            currentNavigationGroupsHash = settingsProvider.CurrentGameSettings.GetOrCreate<NavigationSettings>().ComputeGroupsHash();
        }

        public LoaderReferenceManager Manager { get; }
        /// <summary>
        /// The asset view model associated to this instance.
        /// </summary>
        private AssetViewModel Asset { get; }

        /// <summary>
        /// A dictionary storing the urls used to load an asset, to use the same at unload, in case the asset has been renamed.
        /// </summary>
        private Dictionary<AssetId, string> AssetLoadingTimeUrls { get; } = new Dictionary<AssetId, string>();

        /// <summary>
        /// A dispatcher to the game thread.
        /// </summary>
        private IDispatcherService GameDispatcher { get; }

        /// <summary>
        /// The editor associated to this instance.
        /// </summary>
        private SessionViewModel Session { get; }

        /// <summary>
        /// Types that support fast reloading (ie. updating existing object instead of loading a new one and updating references).
        /// </summary>
        // TODO: add an Attribute on Assets to specify if they are fast-reloadable (plugin approach)
        private static ICollection<Type> FastReloadTypes => new[] { typeof(MaterialAsset), typeof(TextureAsset) };

        /// <summary>
        /// The <see cref="EditorServiceGame"/> associated with this instance.
        /// </summary>
        private EditorServiceGame Game { get; }

        /// <summary>
        /// Raised when an asset start to be compiled and loaded.
        /// </summary>
        public event EventHandler<ContentLoadEventArgs> AssetLoading;

        /// <summary>
        /// Raised when an asset has been loaded.
        /// </summary>
        public event EventHandler<ContentLoadEventArgs> AssetLoaded;

        public void BuildAndReloadAsset(AssetId assetId)
        {
            var assetToRebuild = Session.GetAssetById(assetId)?.AssetItem;
            if (assetToRebuild == null)
                return;

            if (assetToRebuild.Asset is SceneAsset)
            {
                // We never build the SceneAsset directly. Its content is build separately.
                // Note that the only case where this could happen is if a script or component references a scene directly, which is a bad design (should use scene streaming or prefabs instead).
                return;
            }

            Session.Dispatcher.InvokeAsync(() => BuildAndReloadAssets(assetToRebuild.Yield()));
        }

        public T GetRuntimeObject<T>(AssetItem assetItem)
        {
            if (assetItem == null) throw new ArgumentNullException(nameof(assetItem));

            var url = GetLoadingTimeUrl(assetItem);
            return !string.IsNullOrEmpty(url) ? Game.Content.Get<T>(url) : default(T);
        }

        public Task<ISyncLockable> ReserveDatabaseSyncLock()
        {
            return database.ReserveSyncLock();
        }

        public Task<IDisposable> LockDatabaseAsynchronously()
        {
            return database.LockAsync();
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            settingsProvider.GameSettingsChanged -= GameSettingsChanged;
            Session.AssetPropertiesChanged -= AssetPropertiesChanged;
        }

        private async Task<Dictionary<AssetItem, object>> BuildAndReloadAssets(IEnumerable<AssetItem> assetsToRebuild)
        {
            var assetList = assetsToRebuild.ToList();
            if (assetList.Count == 0)
                return null;

            logger?.Debug($"Starting BuildAndReloadAssets for assets {string.Join(", ", assetList.Select(x => x.Location))}");
            var value = Interlocked.Increment(ref loadingAssetCount);
            AssetLoading?.Invoke(this, new ContentLoadEventArgs(value));
            try
            {
                // Rebuild the assets
                await Task.WhenAll(assetList.Select(x => database.Build(x)));

                logger?.Debug("Assets have been built");
                // Unload the previous versions of assets and (re)load the newly build ones.
                var reloadedObjects = await UnloadAndReloadAssets(assetList);

                Game.TriggerActiveRenderStageReevaluation();
                return reloadedObjects;
            }
            finally
            {
                value = Interlocked.Decrement(ref loadingAssetCount);
                AssetLoaded?.Invoke(this, new ContentLoadEventArgs(value));
                logger?.Debug($"Completed BuildAndReloadAssets for assets {string.Join(", ", assetList.Select(x => x.Location))}");
            }
        }

        /// <summary>
        /// Generates the list of assets that are referenced directly and indirectly by entities
        /// </summary>
        [Pure]
        private async Task<Dictionary<AssetId, HashSet<AssetId>>> ComputeReferences()
        {
            var dependencyManager = Session.DependencyManager;
            var references = new Dictionary<AssetId, HashSet<AssetId>>();
            var ids = (await Manager.ComputeReferencedAssets()).ToList();
            foreach (var id in ids)
            {
                var referencedAsset = Session.GetAssetById(id);
                if (referencedAsset == null)
                    continue;

                var dependencies = dependencyManager.ComputeDependencies(referencedAsset.AssetItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
                if (dependencies != null)
                {
                    var entry = references.GetOrCreateValue(referencedAsset.Id);
                    entry.Add(referencedAsset.Id);
                    foreach (var dependency in dependencies.LinksOut)
                    {
                        entry = references.GetOrCreateValue(dependency.Item.Id);
                        entry.Add(referencedAsset.Id);
                    }
                }
            }

            return references;
        }

        private string GetLoadingTimeUrl(AssetItem assetItem)
        {
            return GetLoadingTimeUrl(assetItem.Id) ?? assetItem.Location;
        }

        private string GetLoadingTimeUrl(AssetId assetId)
        {
            string url;
            AssetLoadingTimeUrls.TryGetValue(assetId, out url);
            return url;
        }

        private bool IsCurrentlyLoaded(AssetId assetId, bool loadedManuallyOnly = false)
        {
            string url;
            return AssetLoadingTimeUrls.TryGetValue(assetId, out url) && Game.Content.IsLoaded(url, loadedManuallyOnly);
        }

        private Task<Dictionary<AssetItem, object>> UnloadAndReloadAssets(ICollection<AssetItem> assets)
        {
            var reloadingAssets = new List<ReloadingAsset>();

            // Add assets to assetsToReloadQueue
            lock (assetsToReloadLock)
            {
                foreach (var asset in assets)
                {
                    ReloadingAsset reloadingAsset;

                    // Make sure it is not already in the queue (otherwise reuse it)
                    if (!assetsToReloadMapping.TryGetValue(asset, out reloadingAsset))
                    {
                        assetsToReloadQueue.Enqueue(reloadingAsset = new ReloadingAsset(asset));
                        assetsToReloadMapping.Add(asset, reloadingAsset);
                    }

                    reloadingAssets.Add(reloadingAsset);
                }
            }

            // Ask Game thread to check our collection
            // Note: if there was many requests during same frame, they will be grouped and only first invocation of this method will process all of them in a batch
            CheckAssetsToReload().Forget();

            // Wait for all of the currently requested assets to be processed
            return Task.WhenAll(reloadingAssets.Select(x => x.Result.Task))
                .ContinueWith(task =>
                {
                    // Convert to expected output format
                    return reloadingAssets.Where(x => x.Result.Task.Result != null).ToDictionary(x => x.AssetItem, x => x.Result.Task.Result);
                });
        }

        private Task CheckAssetsToReload()
        {
            return GameDispatcher.InvokeTask(async () =>
            {
                List<ReloadingAsset> assets;

                // Get assets to reload from queue
                lock (assetsToReloadLock)
                {
                    // Nothing left, early exit
                    if (assetsToReloadQueue.Count == 0)
                        return;

                    // Copy locally and clear queue
                    assets = assetsToReloadQueue.ToList();
                    assetsToReloadQueue.Clear();
                    assetsToReloadMapping.Clear();
                }

                // Update the colorspace
                Game.UpdateColorSpace(currentColorSpace);

                var objToFastReload = new Dictionary<string, object>();

                using (await database.MountInCurrentMicroThread())
                {
                    // First, unload assets
                    foreach (var assetToUnload in assets)
                    {
                        if (FastReloadTypes.Contains(assetToUnload.AssetItem.Asset.GetType()) && IsCurrentlyLoaded(assetToUnload.AssetItem.Asset.Id))
                        {
                            // If this type supports fast reload, retrieve the current (old) value via a load
                            var type = AssetRegistry.GetContentType(assetToUnload.AssetItem.Asset.GetType());
                            string url = GetLoadingTimeUrl(assetToUnload.AssetItem);
                            var oldValue = Game.Content.Get(type, url);
                            if (oldValue != null)
                            {
                                logger?.Debug($"Preparing fast-reload of {assetToUnload.AssetItem.Location}");
                                objToFastReload.Add(url, oldValue);
                            }
                        }
                        else if (IsCurrentlyLoaded(assetToUnload.AssetItem.Asset.Id, true))
                        {
                            // Unload this object if it has already been loaded.
                            logger?.Debug($"Unloading {assetToUnload.AssetItem.Location}");
                            await UnloadAsset(assetToUnload.AssetItem.Asset.Id);
                        }
                    }

                    // Process fast-reload objects
                    var nonFastReloadAssets = new List<ReloadingAsset>();
                    foreach (var assetToLoad in assets)
                    {
                        object oldValue;
                        string url = GetLoadingTimeUrl(assetToLoad.AssetItem);
                        if (FastReloadTypes.Contains(assetToLoad.AssetItem.Asset.GetType()) && objToFastReload.TryGetValue(url, out oldValue))
                        {
                            // Fill oldValue with the values from the database without reloading the object.
                            // As a result, no reference needs to be updated.
                            logger?.Debug($"Fast-reloading {assetToLoad.AssetItem.Location}");
                            ReloadContent(oldValue, assetToLoad.AssetItem);
                            var loadedObject = oldValue;

                            // This fast-reloaded content might have been already loaded through private reference, but if we're reloading it here,
                            // it means that we expect a public reference (eg. it has just been referenced publicly). Reload() won't increase public reference count
                            // so we have to do it manually.
                            if (!IsCurrentlyLoaded(assetToLoad.AssetItem.Id, true))
                            {
                                var type = AssetRegistry.GetContentType(assetToLoad.AssetItem.Asset.GetType());
                                LoadContent(type, url);
                            }

                            await Manager.ReplaceContent(assetToLoad.AssetItem.Asset.Id, loadedObject);

                            assetToLoad.Result.SetResult(loadedObject);
                        }
                        else
                        {
                            nonFastReloadAssets.Add(assetToLoad);
                        }
                    }

                    // Load all async object in a separate task
                    // We avoid Game.Content.LoadAsync, which would wait next frame between every loaded asset
                    var microThread = Scheduler.CurrentMicroThread;
                    var bufferBlock = new BufferBlock<KeyValuePair<ReloadingAsset, object>>();
                    var task = Task.Run(() =>
                    {
                        var initialContext = SynchronizationContext.Current;
                        // This synchronization context gives access to any MicroThreadLocal values. The database to use might actually be micro thread local.
                        SynchronizationContext.SetSynchronizationContext(new MicrothreadProxySynchronizationContext(microThread));

                        foreach (var assetToLoad in nonFastReloadAssets)
                        {
                            var type = AssetRegistry.GetContentType(assetToLoad.AssetItem.Asset.GetType());
                            string url = GetLoadingTimeUrl(assetToLoad.AssetItem);

                            object loadedObject = null;
                            try
                            {
                                loadedObject = LoadContent(type, url);
                            }
                            catch (Exception e)
                            {
                                logger?.Error($"Unable to load asset [{assetToLoad.AssetItem.Location}].", e);
                            }

                            // Post it in BufferBlock so that the game-side loop can process results incrementally
                            bufferBlock.Post(new KeyValuePair<ReloadingAsset, object>(assetToLoad, loadedObject));
                        }

                        bufferBlock.Complete();

                        SynchronizationContext.SetSynchronizationContext(initialContext);
                    });

                    while (await bufferBlock.OutputAvailableAsync())
                    {
                        var item = await bufferBlock.ReceiveAsync();

                        var assetToLoad = item.Key;
                        var loadedObject = item.Value;

                        if (loadedObject != null)
                        {
                            // If it's the first load of this asset, keep its loading url
                            if (!AssetLoadingTimeUrls.ContainsKey(assetToLoad.AssetItem.Asset.Id))
                                AssetLoadingTimeUrls.Add(assetToLoad.AssetItem.Asset.Id, assetToLoad.AssetItem.Location);

                            // Add assets that were not previously loaded to the assetLoadingTimeUrls map.
                            var dependencyManager = Asset.AssetItem.Package.Session.DependencyManager;
                            var dependencies = dependencyManager.ComputeDependencies(Asset.AssetItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
                            if (dependencies != null)
                            {
                                foreach (var dependency in dependencies.LinksOut)
                                {
                                    if (!AssetLoadingTimeUrls.ContainsKey(dependency.Item.Id))
                                        AssetLoadingTimeUrls.Add(dependency.Item.Id, dependency.Item.Location);
                                }
                            }

                            // Remove assets that were previously loaded but are not anymore from the assetLoadingTimeUrls map.
                            foreach (var loadedUrls in AssetLoadingTimeUrls.Where(x => !Game.Content.IsLoaded(x.Value)).ToList())
                            {
                                AssetLoadingTimeUrls.Remove(loadedUrls.Key);
                            }
                        }

                        await Manager.ReplaceContent(assetToLoad.AssetItem.Asset.Id, loadedObject);

                        assetToLoad.Result.SetResult(loadedObject);
                    }

                    // Make sure everything is complete before we return
                    await task;
                }
            });
        }

        public async Task UnloadAsset(AssetId id)
        {
            GameDispatcher.EnsureAccess();

            // Unload this object if it has already been loaded.
            using (await database.LockAsync())
            {
                string url;
                if (AssetLoadingTimeUrls.TryGetValue(id, out url))
                {
                    UnloadContent(url);
                    // Remove assets that were previously loaded but are not anymore from the assetLoadingTimeUrls map.
                    foreach (var loadedUrls in AssetLoadingTimeUrls.Where(x => !Game.Content.IsLoaded(x.Value)).ToList())
                    {
                        AssetLoadingTimeUrls.Remove(loadedUrls.Key);
                    }
                }
            }
        }

        private async void AssetPropertiesChanged(object sender, AssetChangedEventArgs e)
        {
            // Get the list of assets directly referenced by entities, that reference one of the modified asset. (eg. get models when a material is changed)
            var allAssetsToRebuild = new HashSet<AssetViewModel>();

            // Don't propagate property changes until we're fully initialized.
            await Asset.EditorInitialized;

            // If GameSettingsAssets.ColorSpace was changed, rebuild the whole scene
            var assets = e.Assets.ToList();

            var references = await ComputeReferences();
            var assetsToProcess = new Queue<AssetViewModel>(assets);
            var processedAssets = new HashSet<AssetViewModel>(assets);

            // Recurse through assets that depend on this one (recursively)
            while (assetsToProcess.Count > 0)
            {
                var assetToProcess = assetsToProcess.Dequeue();
                HashSet<AssetId> modifiedAssetReferencers;

                // Check if the asset is referenced in the scene.
                if (!references.TryGetValue(assetToProcess.Id, out modifiedAssetReferencers))
                    continue;

                // We wait for a lock of the database. The lock we retrieve is synchronous, do not await in this using block!
                using ((await database.ReserveSyncLock()).Lock())
                {
                    // There is two patterns:
                    // - Object is a fast-reloadable & already loaded object: we can replace its content internally without loading a new object and recreating any of its referencers
                    //   Note that we still need to process referencers in case it is used as a compile-time dependency (i.e. Material layer)
                    // - Object is not a fast-reloadable object: we need to find its referencers (recursively) until we find node directly referenced by the scene (part of modifiedAssetReferencers) and reload this one
                    var isFastReloadCurrentlyLoaded = FastReloadTypes.Contains(assetToProcess.AssetType) && IsCurrentlyLoaded(assetToProcess.Id);
                    if (modifiedAssetReferencers.Contains(assetToProcess.Id) || isFastReloadCurrentlyLoaded)
                    {
                        allAssetsToRebuild.Add(assetToProcess);
                    }

                    // Find dependent assets
                    foreach (var referencer in assetToProcess.Dependencies.ReferencerAssets)
                    {
                        var node = database.AssetDependenciesCompiler.BuildDependencyManager.FindOrCreateNode(referencer.AssetItem, typeof(AssetCompilationContext));
                        node.Analyze(database.CompilerContext);
                        foreach (var reference in node.References)
                        {
                            // Check if this reference is actually a compile-time dependency
                            // Or if it's not a fast reloadable type (in which case we also need to process its references)
                            if (reference.Target.AssetItem.Id == assetToProcess.Id && (reference.HasOne(BuildDependencyType.CompileContent | BuildDependencyType.CompileAsset) || !isFastReloadCurrentlyLoaded))
                            {
                                // If yes, process this asset later
                                if (processedAssets.Add(referencer))
                                {
                                    assetsToProcess.Enqueue(referencer);
                                }
                            }
                        }
                    }
                }
            }

            await BuildAndReloadAssets(allAssetsToRebuild.Select(x => x.AssetItem));
        }

        private async void GameSettingsChanged(object sender, GameSettingsChangedEventArgs e)
        {
            // Remark: we assume that GameStudioDatabase has already updated the compiler game settings,
            // which is the case because this service is registered before the creation of this EditorContentLoader
            if (e.GameSettings.GetOrCreate<RenderingSettings>().ColorSpace != currentColorSpace || e.GameSettings.GetOrCreate<EditorSettings>().RenderingMode != currentRenderingMode)
            {
                currentRenderingMode = e.GameSettings.GetOrCreate<EditorSettings>().RenderingMode;
                currentColorSpace = e.GameSettings.GetOrCreate<RenderingSettings>().ColorSpace;

                await BuildAndReloadAssets(Asset.Dependencies.ReferencedAssets.Select(x => x.AssetItem));
            }

            // Update navigation meshes that are previewed inside the current scene when the game settings's group settings for navigation meshes change
            var navigationGroupsHash = e.GameSettings.GetOrCreate<NavigationSettings>().ComputeGroupsHash();
            if (navigationGroupsHash != currentNavigationGroupsHash)
            {
                currentNavigationGroupsHash = navigationGroupsHash;

                await BuildAndReloadAssets(Session.AllAssets.Where(x => x.AssetType == typeof(NavigationMeshAsset)).Select(x=>x.AssetItem));
            }
        }

        private object LoadContent(Type type, string url)
        {
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = debugStats ?? Game.Content.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Loading {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            var result = Game.Content.Load(type, url);
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = Game.Content.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Loaded {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            return result;
        }

        private void UnloadContent(string url)
        {
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = debugStats ?? Game.Content.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Unloading {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            Game.Content.Unload(url);
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = Game.Content.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Unloaded {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
        }

        private void ReloadContent(object obj, AssetItem assetItem)
        {
            var url = assetItem.Location;
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = debugStats ?? Game.Content.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Reloading {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            Game.Content.Reload(obj, url);
            AssetLoadingTimeUrls[assetItem.Id] = url;

#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = Game.Content.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Reloaded {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
        }

        /// <summary>
        /// Represents an asset being reloaded asynchronously.
        /// </summary>
        class ReloadingAsset
        {
            public ReloadingAsset(AssetItem assetItem)
            {
                AssetItem = assetItem;
            }

            /// <summary>
            /// The asset being reloaded.
            /// </summary>
            public AssetItem AssetItem { get; }

            /// <summary>
            /// The task containg the runtime value of the reloaded asset.
            /// </summary>
            public TaskCompletionSource<object> Result { get; } = new TaskCompletionSource<object>();
        }
    }
}
