// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.Assets.Effect;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Editor.Annotations;
using Stride.Editor.Build;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Editor.Preview;

/// <summary>
/// The preview service of a session, shared by both editors: it runs the preview game on its own thread, finds the preview
/// of the selected asset, and builds and updates it.
/// </summary>
/// <remarks>
/// A toolkit only provides the view the game renders in: <see cref="CreateGameContext"/> gives the game its context on the
/// game thread, and <see cref="DisposeView"/> releases the view once the game thread has ended. A derived constructor calls
/// <see cref="StartPreviewGame"/> once it is ready for <see cref="CreateGameContext"/> to be called.
/// </remarks>
public abstract class GameStudioPreviewServiceBase : IAssetPreviewService, IPreviewBuilder
{
    /// <summary>
    /// Whether the preview game is not run (<c>/DisablePreview</c>).
    /// </summary>
    public static bool DisablePreview = false;

    // Set when the game loop has ended (a lost device, the exit): nothing can be posted to the game anymore
    private volatile bool previewGameEnded;

    // Set by Dispose: a game the thread has not created yet never starts
    private bool previewStopped;

    // The last visibility of the preview panel, applied to the game when the thread creates it
    private bool isPreviewHidden;

    private readonly ISessionViewModel session;
    private readonly IEditorDebugService? debugService;
    private readonly IDebugPage? loggerDebugPage;
    private IAssetPreview? currentPreview;
    private object? previewView;

    private AssetViewModel? previewBuildQueue;
    private readonly SemaphoreSlim previewChangeLock = new(1, 1);
    /// <summary>
    /// A lock used to access the <see cref="currentPreview"/>, <see cref="previewBuildQueue"/>, <see cref="previewStopped"/>,
    /// <see cref="isPreviewHidden"/> fields and the <see cref="PreviewGame"/> property safely.
    /// </summary>
    private readonly object previewLock = new();
    private readonly Thread previewGameThread;
    private readonly Dictionary<Type, AssetPreviewFactory> assetPreviewFactories = new();

    // The preview type each factory creates, so unloading its assembly can drop the factory
    private readonly Dictionary<Type, Type> assetPreviewTypes = new();

    private readonly AssetCompilerContext previewCompileContext = new() { Platform = PlatformType.Windows };
    private readonly AssetDependenciesCompiler previewCompiler = new(typeof(PreviewCompilationContext));

    private readonly GameSettingsAsset previewGameSettings;
    private readonly GameSettingsProviderService gameSettingsProvider;

    protected GameStudioPreviewServiceBase(ISessionViewModel session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        Dispatcher = session.Dispatcher;
        AssetBuilderService = session.ServiceProvider.Get<GameStudioBuilderService>();
        gameSettingsProvider = session.ServiceProvider.Get<GameSettingsProviderService>();

        Logger = GlobalLogger.GetLogger("Preview");
        debugService = session.ServiceProvider.TryGet<IEditorDebugService>();
        loggerDebugPage = debugService?.CreateLogDebugPage(Logger, "Preview");

        previewGameSettings = GameSettingsFactory.Create();
        previewGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0;
        UpdateGameSettings(gameSettingsProvider.CurrentGameSettings);

        foreach (var assembly in AssemblyRegistry.Find(AssemblyCommonCategories.Assets))
            RegisterAssetPreviews(assembly);
        AssemblyRegistry.AssemblyRegistered += AssemblyRegistered;
        AssemblyRegistry.AssemblyUnregistered += AssemblyUnregistered;
        previewCompileContext.SetGameSettingsAsset(previewGameSettings);
        previewCompileContext.CompilationContext = typeof(PreviewCompilationContext);
        previewCompileContext.Properties.Set(ShaderSourceLocator.Key, AssetBuilderService.ShaderSources);

        previewGameThread = new Thread(SafeAction.Wrap(PreviewGameThread)) { IsBackground = true, Name = "PreviewGame Thread" };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            previewGameThread.SetApartmentState(ApartmentState.STA);
    }

    /// <summary>
    /// Gets whether this instance of <see cref="GameStudioPreviewServiceBase"/> has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    public GameStudioBuilderService AssetBuilderService { get; }

    /// <inheritdoc />
    AssetBuilderService IPreviewBuilder.AssetBuilderService => AssetBuilderService;

    public IDispatcherService Dispatcher { get; }

    public Logger Logger { get; }

    public PreviewGame PreviewGame { get; private set; } = null!;

    public event EventHandler<EventArgs>? PreviewAssetUpdated;

    /// <summary>
    /// Starts the preview game thread and follows the session. A derived constructor calls it once.
    /// </summary>
    protected void StartPreviewGame()
    {
        previewGameThread.Start();

        session.AssetPropertiesChanged += OnAssetPropertyChanged;
        gameSettingsProvider.GameSettingsChanged += OnGameSettingsChanged;
    }

    /// <summary>
    /// Prepares what the view needs, first thing on the game thread, before the preview game is created.
    /// </summary>
    /// <remarks>
    /// A constructor that waits for its view (a window handle) is released here, even when the game can't be created.
    /// </remarks>
    protected virtual void PrepareView()
    {
    }

    /// <summary>
    /// Creates the view the preview game renders in and the game context that runs the game in it. Called on the game thread,
    /// before the game runs.
    /// </summary>
    /// <param name="game">The preview game.</param>
    /// <returns>The context to run the game with.</returns>
    protected abstract GameContext CreateGameContext(PreviewGame game);

    /// <summary>
    /// Releases the view the preview game rendered in. Called by <see cref="Dispose"/> once the game thread has ended.
    /// </summary>
    protected abstract void DisposeView();

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

            PreviewGame? previewGame;
            lock (previewLock)
            {
                previewStopped = true;
                previewGame = PreviewGame;
            }
            // The game may not exist yet: the thread then ends without starting it
            if (previewGame is not null && !previewGameEnded)
            {
                if (previewGame.IsRunning)
                {
                    previewGame.Exit();
                }
                else
                {
                    // A game still starting (device, shaders) cannot exit yet: its first update does
                    previewGame.Script.AddTask(() =>
                    {
                        previewGame.Exit();
                        return Task.CompletedTask;
                    });
                }
            }

            // Wait for the game thread to terminate
            if (previewGameThread.IsAlive)
                previewGameThread.Join();

            DisposeView();
            previewCompileContext?.Dispose();

            debugService?.UnregisterDebugPage(loggerDebugPage);

            IsDisposed = true;
        }
    }

    private void PreviewGameThread()
    {
        PrepareView();
        var previewGame = new PreviewGame(AssetBuilderService.EffectCompiler);
        lock (previewLock)
        {
            if (previewStopped)
            {
                previewGameEnded = true;
                previewGame.Dispose();
                return;
            }

            PreviewGame = previewGame;
            // The preview panel may have been hidden before the game existed
            if (isPreviewHidden)
                PreviewGame.IsEditorHidden = true;
            // An asset selected before the game existed (the asset of the editors that open with the session) shows first
            if (previewBuildQueue != null)
                PreviewGame.Script.AddTask(ChangePreviewAsset);
        }
        var context = CreateGameContext(PreviewGame);

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

    private void OnGameSettingsChanged(object? sender, GameSettingsChangedEventArgs e)
    {
        UpdateGameSettings(e.GameSettings);
    }

    private void UpdateGameSettings(GameSettingsAsset currentGameSettings)
    {
        previewGameSettings.GetOrCreate<EditorSettings>().RenderingMode = currentGameSettings.GetOrCreate<EditorSettings>().RenderingMode;
        previewGameSettings.GetOrCreate<RenderingSettings>().ColorSpace = currentGameSettings.GetOrCreate<RenderingSettings>().ColorSpace;
    }

    private void OnAssetPropertyChanged(object? sender, AssetChangedEventArgs e)
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

    private void GraphicsDeviceManagerDeviceCreated(object? sender, EventArgs e)
    {
        // Transmit actual GraphicsProfile to preview and thumbnail builder context
        var graphicsProfile = PreviewGame.GraphicsDeviceManager.GraphicsDevice.Features.CurrentProfile;
        previewCompileContext.GetGameSettingsAsset().GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = graphicsProfile;
    }

    public void SetAssetToPreview(AssetViewModel asset)
    {
        lock (previewLock)
        {
            previewBuildQueue = asset;
            // The game thread has not created the game yet: it takes the asset when it does
            if (PreviewGame is null)
                return;
        }
        if (!previewGameEnded)
            PreviewGame.Script.AddTask(ChangePreviewAsset);
    }

    public AssetCompilerResult Compile(AssetItem asset)
    {
        return previewCompiler.Prepare(previewCompileContext, asset);
    }

    private async Task UpdatePreviewAsset()
    {
        if (!previewChangeLock.Wait(0))
            return;

        try
        {
            // copy currentPreview to a local variable because it might be modified in a different thread.
            var localPreview = currentPreview;
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

        AssetViewModel? asset = null;
        IAssetPreview? nextPreview = null;
        try
        {
            if (currentPreview != null)
            {
                IAssetPreview? previousPreview;
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

    private IAssetPreview? GetPreviewForAsset(AssetViewModel asset)
    {
        if (asset == null) throw new ArgumentNullException(nameof(asset));

        var assetType = asset.Asset.GetType();
        while (assetType != null)
        {
            AssetPreviewFactory? factory;
            bool found;
            lock (assetPreviewFactories)
                found = assetPreviewFactories.TryGetValue(assetType, out factory);
            if (found)
            {
                var assetPreview = factory!(this, PreviewGame, asset.AssetItem);
                return assetPreview;
            }
            assetType = assetType.BaseType;
        }
        return null;
    }

    public object? GetCurrentPreviewView()
    {
        return previewView;
    }

    public void RegisterAssetPreviewFactories(IReadOnlyDictionary<Type, AssetPreviewFactory> factories)
    {
        lock (assetPreviewFactories)
            factories.ForEach(x => assetPreviewFactories[x.Key] = x.Value);
    }

    private void AssemblyRegistered(object? sender, AssemblyRegisteredEventArgs e)
    {
        if (e.Categories.Contains(AssemblyCommonCategories.Assets))
            RegisterAssetPreviews(e.Assembly);
    }

    private void AssemblyUnregistered(object? sender, AssemblyRegisteredEventArgs e)
    {
        if (e.Categories.Contains(AssemblyCommonCategories.Assets))
            UnregisterAssetPreviews(e.Assembly);
    }

    /// <summary>
    /// Registers previews tagged <see cref="AssetPreviewAttribute"/> in <paramref name="assembly"/> using its
    /// source-generated scan index. An assembly can register on any thread (a package loaded by a background task).
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
                // A preview's constructor can ask for the services of the session
                assetPreviewFactories[previewAttribute.AssetType] = (builder, game, asset) => (IAssetPreview)session.ServiceProvider.CreateInstance(localType);
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
        SetPreviewHidden(false);
    }

    public void OnHidePreview()
    {
        SetPreviewHidden(true);
    }

    private void SetPreviewHidden(bool hidden)
    {
        lock (previewLock)
        {
            isPreviewHidden = hidden;
            // The game thread has not created the game yet: it applies the visibility when it does
            if (PreviewGame is not null)
                PreviewGame.IsEditorHidden = hidden;
        }
    }
}
