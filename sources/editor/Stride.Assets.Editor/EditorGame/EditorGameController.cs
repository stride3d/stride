// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Assets.Editor.EditorGame.ContentLoader;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Editor;
using Stride.Editor.Build;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Editor.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Creates the game of an editor.
/// </summary>
/// <param name="gameContentLoadedTaskSource">The source the game completes once its content is loaded.</param>
/// <param name="effectCompiler">The effect compiler of the session.</param>
/// <param name="effectLogPath">The file the game records the effects it uses into, or <c>null</c>.</param>
public delegate TEditorGame EditorGameFactory<out TEditorGame>(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string? effectLogPath)
    where TEditorGame : EditorServiceGame;

/// <summary>
/// Runs the game of an editor on its own thread, rendering into a viewport of the UI (<see cref="IEditorGameViewport"/>),
/// and gives the editor access to it: the services of the game, and the work posted to the game thread.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameController</c>, without its game form: the game has no
/// window, the viewport shows its frames. Mouse cursor changes and drag and drop are not supported yet.
/// </remarks>
/// <typeparam name="TEditorGame">The type of the game.</typeparam>
public abstract class EditorGameController<TEditorGame> : IEditorGameController
    where TEditorGame : EditorServiceGame
{
    private readonly Thread gameThread;
    private readonly IEditorDebugService? debugService;
    private readonly IDebugPage? debugPage;
    private readonly TaskCompletionSource<bool> gameStartedTaskSource = new();
    private readonly TaskCompletionSource<bool> gameContentLoadedTaskSource = new();
    private readonly CancellationTokenSource gameEndedCancellation = new();
    private EditorGameServiceRegistry? serviceRegistry;
    private volatile bool gameEnded;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGameController{TEditorGame}"/> class (UI thread).
    /// </summary>
    /// <param name="editor">The editor of the asset, whose view shows the game.</param>
    /// <param name="viewportService">The service that creates the viewport the game renders into.</param>
    /// <param name="gameFactory">The factory that creates the game.</param>
    protected EditorGameController(AssetEditorViewModel editor, IEditorGameViewportService viewportService, EditorGameFactory<TEditorGame> gameFactory)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(viewportService);
        ArgumentNullException.ThrowIfNull(gameFactory);

        Editor = editor;
        Asset = editor.Asset;
        GameSideNodeContainer = new SessionNodeContainer(Asset.Session) { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

        var builderService = Asset.ServiceProvider.Get<GameStudioBuilderService>();
        Logger = new LoggerResult();
        debugService = Asset.ServiceProvider.TryGet<IEditorDebugService>();
        debugPage = debugService?.CreateLogDebugPage(Logger, "Scene");
        Viewport = viewportService.CreateViewport(Logger);

        // Create the game
        Game = gameFactory(gameContentLoadedTaskSource, builderService.EffectCompiler, builderService.EffectLogPath);
        Game.PackageSettings = Asset.ServiceProvider.Get<GameSettingsProviderService>();
        gameThread = new Thread(SafeAction.Wrap(GameRunThread)) { IsBackground = true, Name = $"EditorGameThread ({Asset.Url})" };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            gameThread.SetApartmentState(ApartmentState.STA);

        Loader = new EditorContentLoader(this, Logger, editor, Game);
    }

    /// <inheritdoc/>
    public IEditorContentLoader Loader { get; }

    /// <inheritdoc/>
    public Logger Logger { get; }

    /// <inheritdoc/>
    public SessionNodeContainer GameSideNodeContainer { get; }

    /// <inheritdoc/>
    public Task GameContentLoaded => gameContentLoadedTaskSource.Task;

    /// <summary>
    /// Gets the viewport the game renders into.
    /// </summary>
    public IEditorGameViewport Viewport { get; }

    /// <summary>
    /// Gets the control of the UI toolkit that shows the game.
    /// </summary>
    public object View => Viewport.View;

    /// <summary>
    /// Gets the asset edited by the editor.
    /// </summary>
    protected AssetViewModel Asset { get; }

    /// <summary>
    /// Gets the editor of the asset.
    /// </summary>
    protected AssetEditorViewModel Editor { get; }

    /// <summary>
    /// Gets the game.
    /// </summary>
    protected TEditorGame Game { get; }

    /// <summary>
    /// Indicates whether this controller has been destroyed.
    /// </summary>
    /// <remarks>
    /// Once this property is <c>true</c>, attempting to call a method on the controller will throw a <see cref="ObjectDisposedException"/>.
    /// </remarks>
    /// <seealso cref="IsDestroying"/>
    protected bool IsDestroyed { get; private set; }

    /// <summary>
    /// Indicates whether this controller is currently being destroyed.
    /// </summary>
    /// <remarks>
    /// While this property is <c>true</c>, attempting to call a method on the controller will not throw but might
    /// either return a default value (e.g. <see cref="GetService{T}"/>) or a cancelled task (e.g. <see cref="InvokeAsync"/>.
    /// </remarks>
    /// <seealso cref="IsDestroyed"/>
    protected bool IsDestroying { get; private set; }

    /// <inheritdoc/>
    /// <remarks>
    /// Derived class should override this method, implement specific cleanup and then call the base implementation.
    /// </remarks>
    public virtual void Destroy()
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return;

        Loader.Dispose();
        debugService?.UnregisterDebugPage(debugPage);
        IsDestroying = true;

        // Clean after everything
        PostTask(async _ =>
        {
            if (serviceRegistry != null)
                await serviceRegistry.DisposeAsync();
            Game.Exit();
        }, int.MaxValue)
        // Ensure the properties are correctly set, even in case of a failure (default continuation)
        .ContinueWith(_ =>
        {
            Asset.Dispatcher.Invoke(() =>
            {
                IsDestroyed = true;
                IsDestroying = false;
            });
        });
        GameSideNodeContainer.Clear();
    }

    /// <inheritdoc/>
    public abstract Task<bool> CreateScene();

    /// <inheritdoc/>
    public object? FindGameSidePart(AbsoluteId partId)
    {
        EnsureNotDestroyed();
        EnsureGameAccess();
        return FindPart(partId);
    }

    /// <summary>
    /// Finds the game-side object of a part of the asset (game thread).
    /// </summary>
    protected abstract object? FindPart(AbsoluteId partId);

    /// <inheritdoc/>
    public T? GetService<T>() where T : IEditorGameViewModelService
    {
        EnsureNotDestroyed();
        EnsureAssetAccess();
        if (IsDestroying || serviceRegistry == null)
            return default;
        return serviceRegistry.Get<T>();
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetServices<T>() where T : IEditorGameViewModelService
    {
        EnsureNotDestroyed();
        EnsureAssetAccess();
        if (IsDestroying || serviceRegistry == null)
            return [];
        return serviceRegistry.Services.OfType<T>().ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> StartGame()
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            throw new InvalidOperationException("This controller is being destroyed.");
        // Run the game in its own thread
        gameThread.Start();
        // Wait for the game to start
        return await gameStartedTaskSource.Task;
    }

    /// <inheritdoc/>
    public void OnHideGame()
    {
        Game.IsEditorHidden = true;
    }

    /// <inheritdoc/>
    public void OnShowGame()
    {
        Game.IsEditorHidden = false;
    }

    /// <inheritdoc/>
    public void TriggerActiveRenderStageReevaluation()
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return;
        Game.TriggerActiveRenderStageReevaluation();
    }

    /// <inheritdoc/>
    public Task InvokeAsync(Action callback, CancellationToken token = default)
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return Task.FromCanceled(new CancellationToken(true));
        return PostActionAsync(callback, 0);
    }

    /// <inheritdoc/>
    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default)
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return Task.FromCanceled(new CancellationToken(true));
        return PostActionAsync(callback, int.MaxValue);
    }

    /// <inheritdoc/>
    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default)
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return Task.FromCanceled<TResult>(new CancellationToken(true));
        return PostActionAsync(callback, 0);
    }

    /// <inheritdoc/>
    public Task InvokeTask(Func<Task> task, CancellationToken token = default)
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return Task.FromCanceled(new CancellationToken(true));
        return PostTask(_ => task(), 0);
    }

    /// <inheritdoc/>
    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default)
    {
        EnsureNotDestroyed();
        if (IsDestroying)
            return Task.FromCanceled<TResult>(new CancellationToken(true));
        return PostTask(_ => task(), 0);
    }

    /// <summary>
    /// Verifies that the current thread is the UI thread.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckAssetAccess()
    {
        return Asset.Dispatcher.CheckAccess();
    }

    /// <summary>
    /// Ensures that the current thread is the UI thread. This method will throw an exception if it is not the case.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureAssetAccess()
    {
        Asset.Dispatcher.EnsureAccess();
    }

    /// <summary>
    /// Verifies that the current thread is the game thread.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CheckGameAccess()
    {
        return Thread.CurrentThread == gameThread;
    }

    /// <summary>
    /// Ensures that the current thread is the game thread (or not, when <paramref name="inGameThread"/> is <c>false</c>).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureGameAccess(bool inGameThread = true)
    {
        if (inGameThread && Thread.CurrentThread != gameThread)
            throw new InvalidOperationException("This code must be executed in the game thread.");
        if (!inGameThread && Thread.CurrentThread == gameThread)
            throw new InvalidOperationException("This code must not be executed in the game thread.");
    }

    /// <inheritdoc/>
    bool IDispatcherService.CheckAccess() => CheckGameAccess();

    /// <inheritdoc/>
    void IDispatcherService.EnsureAccess(bool inDispatcherThread) => EnsureGameAccess(inDispatcherThread);

    /// <inheritdoc/>
    void IDispatcherService.Invoke(Action callback)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    TResult IDispatcherService.Invoke<TResult>(Func<TResult> callback)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Checks whether this controller has been destroyed, and throws an <see cref="ObjectDisposedException"/> if it is the case.
    /// </summary>
    /// <param name="name">The name to supply to the <see cref="ObjectDisposedException"/>.</param>
    protected void EnsureNotDestroyed(string? name = null)
    {
        if (IsDestroyed)
            throw new ObjectDisposedException(name ?? nameof(EditorGameController<TEditorGame>), "This controller has already been disposed.");
    }

    /// <summary>
    /// Creates the services declared for this controller by <see cref="EditorGameServiceAttribute"/> in every asset
    /// assembly through its source-generated scan index (game thread, before the game runs).
    /// </summary>
    protected virtual void InitializeServices(EditorGameServiceRegistry services)
    {
        var serviceTypes = new List<(Type Type, int Order)>();
        foreach (var serviceType in AssetRegistry.AssetAssemblies.SelectMany(x => AssemblyRegistry.GetScanTypes(x, typeof(EditorGameServiceAttribute))).Distinct())
        {
            if (!typeof(IEditorGameService).IsAssignableFrom(serviceType) || serviceType.IsAbstract || serviceType.IsGenericTypeDefinition)
                continue;

            var declaration = serviceType.GetCustomAttributes<EditorGameServiceAttribute>(inherit: false).FirstOrDefault(x => x.ControllerType.IsInstanceOfType(this));
            if (declaration != null)
                serviceTypes.Add((serviceType, declaration.Order));
        }

        foreach (var (serviceType, _) in serviceTypes.OrderBy(x => x.Order).ThenBy(x => x.Type.FullName, StringComparer.Ordinal))
        {
            try
            {
                services.Add(CreateService(serviceType));
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to create the editor game service [{serviceType.Name}]", e);
            }
        }
    }

    private IEditorGameService CreateService(Type serviceType)
    {
        foreach (var constructor in serviceType.GetConstructors())
        {
            var parameters = constructor.GetParameters();
            var arguments = new object[parameters.Length];
            var matched = true;
            for (var i = 0; i < parameters.Length && matched; ++i)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsInstanceOfType(this))
                    arguments[i] = this;
                else if (parameterType.IsInstanceOfType(Editor))
                    arguments[i] = Editor;
                else
                    matched = false;
            }

            if (matched)
                return (IEditorGameService)constructor.Invoke(arguments);
        }

        throw new InvalidOperationException($"[{serviceType.Name}] has no constructor taking only the controller and/or the editor view model.");
    }

    private void GameRunThread()
    {
        GameContext context;
        try
        {
            // The game renders offscreen into the viewport
            context = Viewport.Attach(Game);

            // Wait for shaders to be loaded
            Asset.ServiceProvider.Get<GameStudioBuilderService>().WaitForShaders();

            // Create and register services
            serviceRegistry = new EditorGameServiceRegistry();
            InitializeServices(serviceRegistry);
            Game.RegisterServices(serviceRegistry);
        }
        catch (Exception e)
        {
            gameEnded = true;
            gameEndedCancellation.Cancel();
            try
            {
                Game.Dispose();
                Viewport.Dispose();
            }
            finally
            {
                gameStartedTaskSource.TrySetException(e);
                gameContentLoadedTaskSource.TrySetException(e);
            }
            return;
        }

        // Notify game start
        gameStartedTaskSource.SetResult(true);
        try
        {
            Game.Run(context);
        }
        catch (GraphicsDeviceException ex) when (ex.Status != GraphicsDeviceStatus.Normal)
        {
            // The loss ends the run; every device of the process went with it, so the studio restarts
            GraphicsDeviceLoss.Report(Game, ex);
        }
        finally
        {
            gameEnded = true;
            gameEndedCancellation.Cancel();
            // An editor waiting for a game that never loaded its content (the run failed) doesn't wait forever
            gameContentLoadedTaskSource.TrySetCanceled();
            Game.Dispose();
            Viewport.Dispose();
        }
    }

    /// <summary>
    /// Work posted to a game whose loop has ended (a lost device, the exit) is cancelled, like while destroying.
    /// </summary>
    private static Task<T> GameEndedTask<T>()
    {
        return Task.FromCanceled<T>(new CancellationToken(true));
    }

    /// <summary>
    /// Creates the source of work posted to the game. What is still queued when the game loop ends never runs:
    /// its source is then cancelled.
    /// </summary>
    private TaskCompletionSource<T> CreatePostedTaskSource<T>()
    {
        var tcs = new TaskCompletionSource<T>();
        var registration = gameEndedCancellation.Token.Register(() => tcs.TrySetCanceled());
        tcs.Task.ContinueWith(_ => registration.Unregister(), TaskContinuationOptions.ExecuteSynchronously);
        return tcs;
    }

    private Task PostTask(Func<ScriptSystem, Task> task, int priority)
    {
        if (gameEnded)
            return GameEndedTask<int>();

        var tcs = CreatePostedTaskSource<int>();
        Game.Script.AddTask(async () =>
        {
            try
            {
                await task(Game.Script);
                tcs.TrySetResult(0);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }, priority);
        return tcs.Task;
    }

    private Task<T> PostTask<T>(Func<ScriptSystem, Task<T>> task, int priority)
    {
        if (gameEnded)
            return GameEndedTask<T>();

        var tcs = CreatePostedTaskSource<T>();
        Game.Script.AddTask(async () =>
        {
            try
            {
                var result = await task(Game.Script);
                tcs.TrySetResult(result);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }, priority);
        return tcs.Task;
    }

    private Task PostActionAsync(Action action, int priority)
    {
        if (CheckGameAccess())
        {
            action();
            return Task.CompletedTask;
        }

        if (gameEnded)
            return GameEndedTask<int>();

        var tcs = CreatePostedTaskSource<int>();
        Game.Script.AddTask(() =>
        {
            try
            {
                action();
                tcs.TrySetResult(0);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
            return tcs.Task;
        }, priority);
        return tcs.Task;
    }

    private Task<T> PostActionAsync<T>(Func<T> action, int priority)
    {
        if (CheckGameAccess())
            return Task.FromResult(action());

        if (gameEnded)
            return GameEndedTask<T>();

        var tcs = CreatePostedTaskSource<T>();
        Game.Script.AddTask(() =>
        {
            try
            {
                tcs.TrySetResult(action());
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
            return tcs.Task;
        }, priority);
        return tcs.Task;
    }
}
