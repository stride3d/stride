// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// The controller of a game that has no graphics: a thread of its own, the game-side parts the test gives it, and a
/// content loader that loads nothing. Enough for the change propagators.
/// </summary>
internal sealed class FakeEditorGameController : IEditorGameController, IDisposable
{
    private readonly ThreadDispatcherService gameThread = new("Fake game thread");
    private readonly Dictionary<AbsoluteId, object> parts = [];

    public FakeEditorGameController(ISessionViewModel session)
    {
        GameSideNodeContainer = new SessionNodeContainer(session) { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        Loader = new FakeContentLoader(this);
    }

    public IEditorContentLoader Loader { get; }

    public Logger Logger { get; } = GlobalLogger.GetLogger("FakeEditorGame");

    public Task GameContentLoaded => Task.CompletedTask;

    public SessionNodeContainer GameSideNodeContainer { get; }

    /// <summary>
    /// Gets how many times the propagators asked the game to evaluate its render stages again, once per propagated change.
    /// </summary>
    public int RenderStageReevaluationCount { get; private set; }

    /// <summary>
    /// Puts a game-side part in the game (game thread).
    /// </summary>
    public void AddGameSidePart(AbsoluteId id, object part)
    {
        gameThread.EnsureAccess();
        parts[id] = part;
    }

    public object? FindGameSidePart(AbsoluteId partId)
    {
        gameThread.EnsureAccess();
        return parts.GetValueOrDefault(partId);
    }

    public void TriggerActiveRenderStageReevaluation()
    {
        gameThread.EnsureAccess();
        RenderStageReevaluationCount++;
    }

    public Task<bool> StartGame() => Task.FromResult(true);

    public Task<bool> CreateScene() => Task.FromResult(true);

    public void OnHideGame() { }

    public void OnShowGame() { }

    public T GetService<T>() where T : IEditorGameViewModelService => default!;

    public IReadOnlyList<T> GetServices<T>() where T : IEditorGameViewModelService => [];

    public void Destroy() => Dispose();

    public void Dispose() => gameThread.Dispose();

    public void Invoke(Action callback) => gameThread.Invoke(callback);

    public TResult Invoke<TResult>(Func<TResult> callback) => gameThread.Invoke(callback);

    public Task InvokeAsync(Action callback, CancellationToken token = default) => gameThread.InvokeAsync(callback, token);

    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) => gameThread.LowPriorityInvokeAsync(callback, token);

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => gameThread.InvokeAsync(callback, token);

    public Task InvokeTask(Func<Task> task, CancellationToken token = default) => gameThread.InvokeTask(task, token);

    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => gameThread.InvokeTask(task, token);

    public bool CheckAccess() => gameThread.CheckAccess();

    public void EnsureAccess(bool inDispatcherThread = true) => gameThread.EnsureAccess(inDispatcherThread);

    private sealed class FakeContentLoader : IEditorContentLoader
    {
        public FakeContentLoader(IDispatcherService gameDispatcher)
        {
            Manager = new LoaderReferenceManager(gameDispatcher, this);
        }

        public LoaderReferenceManager Manager { get; }

        public event EventHandler<ContentLoadEventArgs>? AssetLoading { add { } remove { } }

        public event EventHandler<ContentLoadEventArgs>? AssetLoaded { add { } remove { } }

        public void BuildAndReloadAsset(AssetId assetId) { }

        public Task UnloadAsset(AssetId id) => Task.CompletedTask;

        public Task<ISyncLockable> ReserveDatabaseSyncLock() => throw new NotSupportedException();

        public Task<IDisposable> LockDatabaseAsynchronously() => Task.FromResult<IDisposable>(new NoLock());

        public T GetRuntimeObject<T>(AssetItem assetItem) where T : class => null!;

        public void Dispose() { }
    }

    private sealed class NoLock : IDisposable
    {
        public void Dispose() { }
    }
}
