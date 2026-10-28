// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Assets;
using Stride.Editor.Build;
using Stride.Editor.Resources;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Editor.Thumbnails
{
    public sealed class GameStudioThumbnailService : IThumbnailService
    {
        private readonly object hashLock = new object();
        // Keyed by AssetId, not AssetItem: a single asset can have several AssetItem instances over time (reload, rename).
        private readonly Dictionary<AssetId, PriorityQueueNode<AssetBuildUnit>> thumbnailQueueHash = new Dictionary<AssetId, PriorityQueueNode<AssetBuildUnit>>();

        // Note: KVP.Value is usually null, and is only set if a new request for the same thumbnail has been done and we need to start it when the current one finish (avoid running it twice at the same time)
        private readonly Dictionary<AssetId, ThumbnailContinuation?> thumbnailsInProgressAndContinuation = new Dictionary<AssetId, ThumbnailContinuation?>();
        private readonly SessionViewModel session;
        private readonly GameStudioBuilderService assetBuilderService;
        private readonly ThumbnailListCompiler? thumbnailCompiler;
        private readonly Func<AssetItem, GameSettingsAsset, bool, ListBuildStep>? headlessCompile;
        private readonly AssetCompilerRegistry compilerRegistry;
        private readonly List<ThumbnailPriorityItem> assetsToIncreasePriority = new List<ThumbnailPriorityItem>();
        private readonly ThumbnailGenerator? generator;
        private bool thumbnailThreadShouldTerminate;
        private RenderingMode renderingMode;
        private ColorSpace colorSpace;
        private GraphicsProfile graphicsProfile;
        private int firstPriority, lastPriority;

        private int currentJobToken = -1;

        private GameSettingsAsset currentGameSettings = null!;

        private readonly GameSettingsProviderService gameSettingsProviderService;

        public GameStudioThumbnailService(SessionViewModel session, GameSettingsProviderService settingsProvider, GameStudioBuilderService assetBuilderService)
            : this(session, settingsProvider, assetBuilderService, new ThumbnailGenerator((EffectCompilerBase)assetBuilderService.EffectCompiler), null)
        {
        }

        /// <summary>
        /// Creates a service that builds no thumbnail image: <paramref name="headlessCompile"/> provides the build steps of each
        /// asset (asset, game settings, whether its thumbnail is static) in place of the <see cref="ThumbnailListCompiler"/>, so
        /// the queue runs without a graphics device. Tests use it; see <see cref="NotifyThumbnailBuilt"/> for the completion side.
        /// </summary>
        internal GameStudioThumbnailService(SessionViewModel session, GameSettingsProviderService settingsProvider, GameStudioBuilderService assetBuilderService, Func<AssetItem, GameSettingsAsset, bool, ListBuildStep> headlessCompile)
            : this(session, settingsProvider, assetBuilderService, null, headlessCompile)
        {
        }

        private GameStudioThumbnailService(SessionViewModel session, GameSettingsProviderService settingsProvider, GameStudioBuilderService assetBuilderService, ThumbnailGenerator? generator, Func<AssetItem, GameSettingsAsset, bool, ListBuildStep>? headlessCompile)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.assetBuilderService = assetBuilderService ?? throw new ArgumentNullException(nameof(assetBuilderService));
            this.generator = generator;
            this.headlessCompile = headlessCompile;

            compilerRegistry = new AssetCompilerRegistry { DefaultCompiler = new CustomAssetThumbnailCompiler() };
            compilerRegistry.RegisterAttributeCompilers<StaticThumbnailAttribute>(typeof(ThumbnailCompilationContext), CreateStaticThumbnailCompiler);
            if (generator != null)
                thumbnailCompiler = new ThumbnailListCompiler(generator, ThumbnailBuilt, compilerRegistry);

            gameSettingsProviderService = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            gameSettingsProviderService.GameSettingsChanged += GameSettingsChanged;
            UpdateGameSettings(settingsProvider.CurrentGameSettings);
            StartPushNotificationsTask();
        }

        /// <inheritdoc/>
        public Task StopAsync()
        {
            RemoveQueuedBuildUnits();
            return generator?.StopAsync() ?? Task.CompletedTask;
        }

        public void Dispose()
        {
            RemoveQueuedBuildUnits();
            generator?.Dispose();
            gameSettingsProviderService.GameSettingsChanged -= GameSettingsChanged;
        }

        private void RemoveQueuedBuildUnits()
        {
            // Terminate thumbnail control thread
            lock (hashLock)
            {
                foreach (var item in thumbnailQueueHash)
                {
                    assetBuilderService.RemoveBuildUnit(item.Value);
                }
                thumbnailQueueHash.Clear();
            }
            thumbnailThreadShouldTerminate = true;
        }

        private void GameSettingsChanged(object? sender, GameSettingsChangedEventArgs e)
        {
            UpdateGameSettings(e.GameSettings);
        }

        private void UpdateGameSettings(GameSettingsAsset gameSettings)
        {
            currentGameSettings = AssetCloner.Clone(gameSettings, AssetClonerFlags.RemoveUnloadableObjects);

            bool shouldRefreshAllThumbnails = false;
            if (renderingMode != gameSettings.GetOrCreate<EditorSettings>().RenderingMode)
            {
                renderingMode = gameSettings.GetOrCreate<EditorSettings>().RenderingMode;
                shouldRefreshAllThumbnails = true;
            }
            if (colorSpace != gameSettings.GetOrCreate<RenderingSettings>().ColorSpace)
            {
                colorSpace = gameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
                shouldRefreshAllThumbnails = true;
            }
            if (graphicsProfile != gameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile)
            {
                graphicsProfile = gameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile;
                shouldRefreshAllThumbnails = true;
            }
            if (shouldRefreshAllThumbnails)
            {
                var allAssets = session.AllAssets.Select(x => x.AssetItem).ToList();
                Task.Run(() => AddThumbnailAssetItems(allAssets, QueuePosition.First));
            }
        }

        public static byte[] HandleBrokenThumbnail()
        {
            // Load broken asset thumbnail
            var assetBrokenThumbnail = Image.Load(DefaultThumbnails.AssetBrokenThumbnail);

            // Apply thumbnail status in corner
            ThumbnailBuildHelper.ApplyThumbnailStatus(assetBrokenThumbnail, LogMessageType.Error);
            var memoryStream = new MemoryStream();
            assetBrokenThumbnail.Save(memoryStream, ImageFileType.Png);

            return memoryStream.ToArray();
        }

        public event EventHandler<ThumbnailCompletedArgs>? ThumbnailCompleted;

        /// <inheritdoc/>
        public int PendingThumbnailCount { get { lock (hashLock) return thumbnailQueueHash.Count; } }

        /// <inheritdoc/>
        private static (Type AssetType, IAssetCompiler Compiler) CreateStaticThumbnailCompiler(StaticThumbnailAttribute attribute, Assembly assembly)
        {
            var image = DeclaredResources.Read(assembly, attribute.ResourceName)
                ?? throw new FileNotFoundException($"Embedded resource [{attribute.ResourceName}] not found in [{assembly.GetName().Name}]");
            var compiler = (IAssetCompiler)Activator.CreateInstance(typeof(StaticThumbnailCompiler<>).MakeGenericType(attribute.AssetType), image)!;
            return (attribute.AssetType, compiler);
        }

        public bool HasStaticThumbnail(Type assetType)
        {
            var compiler = (IThumbnailCompiler)compilerRegistry.GetCompiler(assetType, typeof(ThumbnailCompilationContext));
            return compiler?.IsStatic ?? true;
        }

        public ListBuildStep Compile(AssetItem asset, GameSettingsAsset gameSettings)
        {
            // Mark thumbnail as being compiled
            lock (hashLock)
            {
                thumbnailQueueHash.Remove(asset.Id);
                thumbnailsInProgressAndContinuation[asset.Id] = null;
            }

            var staticThumbnail = HasStaticThumbnail(asset.Asset.GetType());
            return thumbnailCompiler != null
                ? thumbnailCompiler.Compile(asset, gameSettings, staticThumbnail)
                : headlessCompile!(asset, gameSettings, staticThumbnail);
        }

        /// <summary>
        /// Completes a thumbnail the way the <see cref="ThumbnailListCompiler"/> does once its build step ran: the headless
        /// service of the tests has no compiler to raise it.
        /// </summary>
        internal void NotifyThumbnailBuilt(ThumbnailBuiltEventArgs e)
        {
            ThumbnailBuilt(this, e);
        }

        public void AddThumbnailAssetItems(IEnumerable<AssetItem> assetItems, QueuePosition position)
        {
            if (!thumbnailThreadShouldTerminate)
            {
                assetBuilderService.WaitForShaders();

                lock (hashLock)
                {
                    foreach (var asset in assetItems)
                    {
                        if (thumbnailsInProgressAndContinuation.ContainsKey(asset.Id))
                        {
                            // Thumbnail is already being generated, set this one as continuation
                            thumbnailsInProgressAndContinuation[asset.Id] = new ThumbnailContinuation(asset, position);
                        }
                        else if (position == QueuePosition.First)
                        {
                            PriorityQueueNode<AssetBuildUnit> node;
                            if (thumbnailQueueHash.TryGetValue(asset.Id, out node))
                            {
                                assetBuilderService.RemoveBuildUnit(node);
                                thumbnailQueueHash.Remove(asset.Id);
                            }

                            node = assetBuilderService.PushBuildUnit(new ThumbnailAssetBuildUnit(asset, currentGameSettings, this, firstPriority--));
                            thumbnailQueueHash.Add(asset.Id, node);
                        }
                        else
                        {
                            if (!thumbnailQueueHash.ContainsKey(asset.Id))
                            {
                                var node = assetBuilderService.PushBuildUnit(new ThumbnailAssetBuildUnit(asset, currentGameSettings, this, lastPriority++));
                                thumbnailQueueHash.Add(asset.Id, node);
                            }
                        }
                    }
                }
            }
        }

        public void IncreaseThumbnailPriority(IEnumerable<AssetItem> assetItems)
        {
            if (!thumbnailThreadShouldTerminate)
            {
                lock (hashLock)
                {
                    // Batch assets whose priority needs to be updated
                    foreach (var assetItem in assetItems)
                    {
                        PriorityQueueNode<AssetBuildUnit> node;
                        if (thumbnailQueueHash.TryGetValue(assetItem.Id, out node))
                        {
                            var compiler = (IThumbnailCompiler)compilerRegistry.GetCompiler(assetItem.Asset.GetType(), typeof(ThumbnailCompilationContext));
                            var priority = compiler.Priority;
                            assetsToIncreasePriority.Add(new ThumbnailPriorityItem(assetItem, node, priority));
                        }
                    }

                    // Sort by thumbnail priority
                    assetsToIncreasePriority.Sort(ThumbnailPriorityComparer.Default);

                    // Readd at beginning of the queue (reverse so that firstPriority-- matches assetsToIncreasePriority order
                    for (int index = assetsToIncreasePriority.Count - 1; index >= 0; index--)
                    {
                        var thumbnailPriorityItem = assetsToIncreasePriority[index];
                        var node = thumbnailPriorityItem.Node;
                        var asset = thumbnailPriorityItem.Asset;

                        assetBuilderService.RemoveBuildUnit(node);
                        thumbnailQueueHash.Remove(asset.Id);
                        node = assetBuilderService.PushBuildUnit(new ThumbnailAssetBuildUnit(asset, currentGameSettings, this, firstPriority--));
                        thumbnailQueueHash.Add(asset.Id, node);
                    }

                    assetsToIncreasePriority.Clear();
                }
            }
        }

        private void ThumbnailBuilt(object? sender, ThumbnailBuiltEventArgs e)
        {
            ThumbnailData? thumbnailData = null;
            if (e.ThumbnailStream != null)
            {
                var stream = new MemoryStream();
                e.ThumbnailStream.CopyTo(stream);
                thumbnailData = new BitmapThumbnailData(e.ThumbnailId, stream);
            }

            if (e.Result != ThumbnailBuildResult.Cancelled)
            {
                ThumbnailCompleted?.Invoke(this, new ThumbnailCompletedArgs(e.AssetId, thumbnailData));

                // Show why a thumbnail is broken (e.g. a font that fails to load) in the asset log
                var hasProblems = e.BuildLog?.Messages.Any(x => x.IsAtLeast(LogMessageType.Warning)) ?? false;
                session.AssetLog.SetAssetLog(e.AssetId, AssetLogViewModel.ThumbnailLogName, hasProblems ? e.BuildLog : null);
            }

            lock (hashLock)
            {
                thumbnailsInProgressAndContinuation.TryGetValue(e.AssetId, out var thumbnailContinuation);
                thumbnailsInProgressAndContinuation.Remove(e.AssetId);

                // Check if same asset has been requested again while it was compiling
                if (thumbnailContinuation != null)
                {
                    var priority = thumbnailContinuation.Position == QueuePosition.First ? firstPriority-- : lastPriority++;
                    var node = assetBuilderService.PushBuildUnit(new ThumbnailAssetBuildUnit(thumbnailContinuation.UpdatedAssetToRecompile, currentGameSettings, this, priority));
                    thumbnailQueueHash.Add(thumbnailContinuation.UpdatedAssetToRecompile.Id, node);
                }
            }
        }

        private void StartPushNotificationsTask()
        {
            Task.Run(async () =>
            {
                while (!thumbnailThreadShouldTerminate)
                {
                    await Task.Delay(500);
                    if (currentJobToken >= 0)
                    {
                        if (thumbnailQueueHash.Count > 0)
                        {
                            session.Main.Status.NotifyBackgroundJobProgress(currentJobToken, thumbnailQueueHash.Count, true);
                        }
                        else
                        {
                            session.Main.Status.NotifyBackgroundJobFinished(currentJobToken);
                            currentJobToken = -1;
                        }
                    }
                    else if (thumbnailQueueHash.Count > 0)
                    {
                        currentJobToken = session.Main.Status.NotifyBackgroundJobStarted("Building thumbnails… ({0} in queue)", JobPriority.Background);
                    }
                }
            });
        }

        private readonly struct ThumbnailPriorityItem
        {
            public readonly AssetItem Asset;
            public readonly PriorityQueueNode<AssetBuildUnit> Node;
            public readonly int Priority;

            public ThumbnailPriorityItem(AssetItem asset, PriorityQueueNode<AssetBuildUnit> node, int priority) : this()
            {
                Asset = asset;
                Node = node;
                Priority = priority;
            }
        }

        private sealed class ThumbnailPriorityComparer : Comparer<ThumbnailPriorityItem>
        {
            public static new readonly ThumbnailPriorityComparer Default = new ThumbnailPriorityComparer();

            public override int Compare(ThumbnailPriorityItem x, ThumbnailPriorityItem y)
            {
                return x.Priority - y.Priority;
            }
        }

        private sealed class ThumbnailContinuation
        {
            public readonly AssetItem UpdatedAssetToRecompile;
            public readonly QueuePosition Position;

            public ThumbnailContinuation(AssetItem updatedAssetToRecompile, QueuePosition position)
            {
                UpdatedAssetToRecompile = updatedAssetToRecompile;
                Position = position;
            }
        }
    }
}
