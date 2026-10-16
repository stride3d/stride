// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Assets;
using Stride.Editor.Annotations;
using Stride.Editor.Build;
using Stride.Editor.Engine;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Editor.Preview
{
    public class GameStudioPreviewService : IAssetPreviewService, IPreviewBuilder
    {
        public static bool DisablePreview = false;

        // Set when the game loop has ended (a lost device, the exit): nothing can be posted to the game anymore
        private volatile bool previewGameEnded;

        private readonly SessionViewModel session;

        private readonly AutoResetEvent initializationSignal = new AutoResetEvent(false);
        private readonly GameEngineHost host;
        private readonly IDebugPage loggerDebugPage;
        private IAssetPreview currentPreview;
        private IntPtr windowHandle;
        private EmbeddedGameForm gameForm;
        private object previewView;

        private AssetViewModel previewBuildQueue;
        private readonly SemaphoreSlim previewChangeLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// A lock used to access the <see cref="currentPreview"/> and <see cref="previewBuildQueue"/> fields safely.
        /// </summary>
        private readonly object previewLock = new object();
        private readonly Thread previewGameThread;
        private readonly Dictionary<Type, AssetPreviewFactory> assetPreviewFactories = new Dictionary<Type, AssetPreviewFactory>();

        // The preview type each factory creates, so unloading its assembly can drop the factory
        private readonly Dictionary<Type, Type> assetPreviewTypes = new Dictionary<Type, Type>();

        private readonly AssetCompilerContext previewCompileContext = new AssetCompilerContext { Platform = PlatformType.Windows };
        private readonly AssetDependenciesCompiler previewCompiler = new AssetDependenciesCompiler(typeof(PreviewCompilationContext));

        private readonly GameSettingsAsset previewGameSettings;
        private readonly GameSettingsProviderService gameSettingsProvider;

        public GameStudioPreviewService(SessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            this.session = session;
            Dispatcher = session.Dispatcher;
            AssetBuilderService = session.ServiceProvider.Get<GameStudioBuilderService>();
            gameSettingsProvider = session.ServiceProvider.Get<GameSettingsProviderService>();

            Logger = GlobalLogger.GetLogger("Preview");
            loggerDebugPage = EditorDebugTools.CreateLogDebugPage(Logger, "Preview");

            previewGameSettings = GameSettingsFactory.Create();
            previewGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0;
            UpdateGameSettings(gameSettingsProvider.CurrentGameSettings);

            foreach (var assembly in AssemblyRegistry.Find(AssemblyCommonCategories.Assets))
                RegisterAssetPreviews(assembly);
            AssemblyRegistry.AssemblyRegistered += AssemblyRegistered;
            AssemblyRegistry.AssemblyUnregistered += AssemblyUnregistered;
            previewCompileContext.SetGameSettingsAsset(previewGameSettings);
            previewCompileContext.CompilationContext = typeof(PreviewCompilationContext);

            previewGameThread = new Thread(SafeAction.Wrap(StrideUIThread)) { IsBackground = true, Name = "PreviewGame Thread" };
            previewGameThread.SetApartmentState(ApartmentState.STA);
            previewGameThread.Start();

            // Wait for the window handle to be generated on the proper thread
            initializationSignal.WaitOne();
            host = new GameEngineHost(windowHandle);

            session.AssetPropertiesChanged += OnAssetPropertyChanged;
            gameSettingsProvider.GameSettingsChanged += OnGameSettingsChanged;
        }

        /// <summary>
        /// Gets whether this instance of <see cref="GameStudioPreviewService"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        public GameStudioBuilderService AssetBuilderService { get; }

        public IDispatcherService Dispatcher { get; }

        public Logger Logger { get; }

        public PreviewGame PreviewGame { get; private set; }

        public event EventHandler<EventArgs> PreviewAssetUpdated;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                // Terminate preview control thread
                previewBuildQueue = null;

                session.AssetPropertiesChanged -= OnAssetPropertyChanged;
                gameSettingsProvider.GameSettingsChanged -= OnGameSettingsChanged;
                AssemblyRegistry.AssemblyRegistered -= AssemblyRegistered;
                AssemblyRegistry.AssemblyUnregistered -= AssemblyUnregistered;

                if (PreviewGame.IsRunning)
                {
                    PreviewGame.Exit();
                }

                // Wait for the game thread to terminate
                previewGameThread.Join();


                //Game = null;
                host.Dispose();
                //host = null;
                //gameForm = null;
                //windowHandle = IntPtr.Zero;
                previewCompileContext?.Dispose();

                EditorDebugTools.UnregisterDebugPage(loggerDebugPage);

                IsDisposed = true;
            }
        }

        private void StrideUIThread()
        {
            gameForm = new EmbeddedGameForm { TopLevel = false, Visible = false };
            windowHandle = gameForm.Handle;

            initializationSignal.Set();

            PreviewGame = new PreviewGame(AssetBuilderService.EffectCompiler);
            var context = new GameContextWinforms(gameForm) { InitializeDatabase = false };

            // Wait for shaders to be loaded
            AssetBuilderService.WaitForShaders();

            if (!DisablePreview)
            {
                PreviewGame.GraphicsDeviceManager.DeviceCreated += GraphicsDeviceManagerDeviceCreated;
                try
                {
                    PreviewGame.Run(context);
                }
                catch (GraphicsDeviceException ex) when (ex.Status != GraphicsDeviceStatus.Normal)
                {
                    // The loss ends the run; every device of the process went with it, so the studio restarts
                    GraphicsDeviceLoss.Report(PreviewGame, ex);
                }
                finally
                {
                    previewGameEnded = true;
                    PreviewGame.Dispose();
                }
            }
        }

        private void OnGameSettingsChanged(object sender, GameSettingsChangedEventArgs e)
        {
            UpdateGameSettings(e.GameSettings);
        }

        private void UpdateGameSettings(GameSettingsAsset currentGameSettings)
        {
            previewGameSettings.GetOrCreate<EditorSettings>().RenderingMode = currentGameSettings.GetOrCreate<EditorSettings>().RenderingMode;
            previewGameSettings.GetOrCreate<RenderingSettings>().ColorSpace = currentGameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
        }

        private void OnAssetPropertyChanged(object sender, AssetChangedEventArgs e)
        {
            lock (previewLock)
            {
                var allAssets = AssetViewModel.ComputeRecursiveReferencerAssets(e.Assets);
                allAssets.AddRange(e.Assets);
                if (currentPreview != null && allAssets.Contains(currentPreview.AssetViewModel) && !previewGameEnded)
                {
                    PreviewGame.Script.AddTask(UpdatePreviewAsset);
                }
            }
        }

        private void GraphicsDeviceManagerDeviceCreated(object sender, EventArgs e)
        {
            // Transmit actual GraphicsProfile to preview and thumbnail builder context
            var graphicsProfile = PreviewGame.GraphicsDeviceManager.GraphicsDevice.Features.CurrentProfile;
            //ThumbnailService.ThumbnailBuilderContext.GetGameSettingsAsset().Get<RenderingSettings>().DefaultGraphicsProfile = graphicsProfile;
            previewCompileContext.GetGameSettingsAsset().GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = graphicsProfile;
        }

        public void SetAssetToPreview(AssetViewModel asset)
        {
            lock (previewLock)
            {
                previewBuildQueue = asset;
            }
            if (!previewGameEnded)
                PreviewGame.Script.AddTask(ChangePreviewAsset);
        }

        public AssetCompilerResult Compile(AssetItem asset)
        {
            return previewCompiler.Prepare(previewCompileContext, asset);
        }

        public FrameworkElement GetStrideView()
        {
            return !IsDisposed ? host : null;
        }

        private async Task UpdatePreviewAsset()
        {
            if (!previewChangeLock.Wait(0))
                return;

            try
            {
                // copy currentPreview to a local variable because it might be modified in a different thread.
                IAssetPreview localPreview = currentPreview;
                if (localPreview != null)
                {
                    await localPreview.Update();
                }
                Logger.Info("Preview updated following to a a property change.");
            }
            catch (Exception e)
            {
                Logger.Error("Unable to update the preview after a property change.", e);
            }
            finally
            {
                previewChangeLock.Release();
            }
        }

        private async Task ChangePreviewAsset()
        {
            if (!previewChangeLock.Wait(0))
                return;

            AssetViewModel asset = null;
            IAssetPreview nextPreview = null;
            try
            {
                if (currentPreview != null)
                {
                    IAssetPreview previousPreview;
                    // Ensure that the current preview won't be disposed twice with a lock
                    lock (previewLock)
                    {
                        previousPreview = currentPreview;
                        currentPreview = null;
                    }
                    await previousPreview.IsInitialized();
                    await previousPreview.Dispose();
                    Logger.Info($"Unloaded previous preview of {previousPreview.AssetViewModel.Url}.");
                }

                lock (previewLock)
                {
                    if (previewBuildQueue != null)
                    {
                        asset = previewBuildQueue;
                        nextPreview = GetPreviewForAsset(previewBuildQueue);
                        previewBuildQueue = null;
                    }
                    currentPreview = nextPreview;
                }

                if (asset != null && nextPreview != null)
                {
                    previewView = await nextPreview.Initialize(asset, this);
                    if (previewView != null)
                        Logger.Info($"Initialized preview of {nextPreview.AssetViewModel.Url}.");
                }
                else
                {
                    previewView = null;
                }
            }
            catch (Exception e)
            {
                lock (previewLock)
                {
                    currentPreview = null;
                }
                previewView = null;
                Logger.Error("An exception occurred while changing the previewed asset", e);
            }
            finally
            {
                previewChangeLock.Release();
            }

            // Notify that the previewed asset has changed, so the editor view can update its visual tree.
            PreviewAssetUpdated?.Invoke(this, EventArgs.Empty);
        }

        private IAssetPreview GetPreviewForAsset(AssetViewModel asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            var assetType = asset.Asset.GetType();
            while (assetType != null)
            {
                AssetPreviewFactory factory;
                bool found;
                lock (assetPreviewFactories)
                    found = assetPreviewFactories.TryGetValue(assetType, out factory);
                if (found)
                {
                    var assetPreview = factory(this, PreviewGame, asset.AssetItem);
                    return assetPreview;
                }
                assetType = assetType.BaseType;
            }
            return null;
        }

        public object GetCurrentPreviewView()
        {
            return previewView;
        }

        public void RegisterAssetPreviewFactories(IReadOnlyDictionary<Type, AssetPreviewFactory> factories)
        {
            lock (assetPreviewFactories)
                factories.ForEach(x => assetPreviewFactories[x.Key] = x.Value);
        }

        private void AssemblyRegistered(object sender, AssemblyRegisteredEventArgs e)
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                RegisterAssetPreviews(e.Assembly);
        }

        private void AssemblyUnregistered(object sender, AssemblyRegisteredEventArgs e)
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                UnregisterAssetPreviews(e.Assembly);
        }

        /// <summary>
        /// Previews tagged <see cref="AssetPreviewAttribute"/> in <paramref name="assembly"/> (from the assembly processor's
        /// scan index). An assembly can register on any thread (a package loaded by a background task).
        /// </summary>
        private void RegisterAssetPreviews(Assembly assembly)
        {
            foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(IAssetPreview)))
            {
                if (!typeof(IAssetPreview).IsAssignableFrom(type) || type.GetCustomAttribute<AssetPreviewAttribute>() is not { } previewAttribute)
                    continue;
                var localType = type;
                lock (assetPreviewFactories)
                {
                    assetPreviewFactories[previewAttribute.AssetType] = (builder, game, asset) => (IAssetPreview)Activator.CreateInstance(localType);
                    assetPreviewTypes[previewAttribute.AssetType] = localType;
                }
            }
        }

        /// <summary>Drops the previews of an unloaded assembly; a reload registers the new types.</summary>
        private void UnregisterAssetPreviews(Assembly assembly)
        {
            lock (assetPreviewFactories)
            {
                var assetTypes = new List<Type>();
                foreach (var entry in assetPreviewTypes)
                {
                    if (entry.Value.Assembly == assembly)
                        assetTypes.Add(entry.Key);
                }
                foreach (var assetType in assetTypes)
                {
                    assetPreviewTypes.Remove(assetType);
                    assetPreviewFactories.Remove(assetType);
                }
            }
        }

        public void OnShowPreview()
        {
            PreviewGame.IsEditorHidden = false;
        }

        public void OnHidePreview()
        {
            PreviewGame.IsEditorHidden = true;
        }
    }
}
