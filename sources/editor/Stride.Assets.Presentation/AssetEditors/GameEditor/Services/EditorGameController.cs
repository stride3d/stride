// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Editor.Build;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Editor.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Shaders.Compiler;
using Point = System.Windows.Point;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// Represents a factory to create the <see cref="EditorServiceGame"/>.
    /// </summary>
    /// <typeparam name="TEditorGame"></typeparam>
    /// <param name="gameContentLoadedTaskSource"></param>
    /// <param name="effectCompiler"></param>
    /// <param name="effectLogPath"></param>
    /// <returns></returns>
    public delegate TEditorGame EditorGameFactory<out TEditorGame>(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
        where TEditorGame : EditorServiceGame;

    public abstract partial class EditorGameController<TEditorGame> : IEditorGameController
        where TEditorGame : EditorServiceGame
    {
        /// <summary>
        /// The scene asset view model associated to this scene service.
        /// </summary>
        protected readonly AssetViewModel Asset;
        /// <summary>
        /// Gets the game associated with the scene editor.
        /// </summary>
        protected readonly TEditorGame Game;
        /// <summary>
        /// The scene game thread.
        /// </summary>
        private readonly Thread sceneGameThread;
        /// <summary>
        /// The service registry.
        /// </summary>
        private EditorGameServiceRegistry serviceRegistry;
        /// <summary>
        /// The debug page that displays the scene log.
        /// </summary>
        private readonly IDebugPage debugPage;
        /// <summary>
        /// The handle of the game form.
        /// </summary>
        private IntPtr windowHandle;
        /// <summary>
        /// The last click position.
        /// </summary>
        private System.Drawing.Point lastClickPosition;
        /// <summary>
        /// Task completion source that sets the result when the game is started.
        /// </summary>
        private readonly TaskCompletionSource<bool> gameStartedTaskSource = new TaskCompletionSource<bool>();
        /// <summary>
        /// Task completion source that sets the result when the game content is loaded.
        /// </summary>
        private readonly TaskCompletionSource<bool> gameContentLoadedTaskSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorGameController{TEditorGame}"/> class.
        /// </summary>
        /// <param name="asset">The asset associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="gameFactory">The factory to create the editor game.</param>
        protected EditorGameController([NotNull] AssetViewModel asset, [NotNull] GameEditorViewModel editor, [NotNull] EditorGameFactory<TEditorGame> gameFactory)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            Asset = asset;
            Editor = editor;
            GameSideNodeContainer = new SessionNodeContainer(asset.Session) { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

            //Logger = GlobalLogger.GetLogger("Scene");
            Logger = new LoggerResult();
            debugPage = EditorDebugTools.CreateLogDebugPage(Logger, "Scene");

            // Create the game
            var builderService = asset.ServiceProvider.Get<GameStudioBuilderService>();
            Game = gameFactory(gameContentLoadedTaskSource, builderService.EffectCompiler, builderService.EffectLogPath);
            Game.PackageSettings = asset.ServiceProvider.Get<GameSettingsProviderService>();
            sceneGameThread = new Thread(SafeAction.Wrap(SceneGameRunThread)) { IsBackground = true, Name = $"EditorGameThread ({asset.Url})" };
            sceneGameThread.SetApartmentState(ApartmentState.STA);

            Debug = new EditorGameDebugService();
            Loader = new EditorContentLoader(this, Logger, asset, Game);
        }

        /// <inheritdoc/>
        public IEditorContentLoader Loader { get; }

        /// <inheritdoc/>
        public Logger Logger { get; }

        public EditorGameDebugService Debug { get; }

        /// <inheritdoc/>
        public SessionNodeContainer GameSideNodeContainer { get; }

        public GameEngineHost EditorHost => GameForm.Host;

        /// <inheritdoc/>
        public Task GameContentLoaded => gameContentLoadedTaskSource.Task;

        protected GameEditorViewModel Editor { get; }

        /// <summary>
        /// The game form hosting the scene view.
        /// </summary>
        protected EmbeddedGameForm GameForm { get; private set; }

        protected EditorGameRecoveryService RecoveryService { get; private set; }

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
            EditorDebugTools.UnregisterDebugPage(debugPage);
            UnregisterFromDragDropEvents();
            GameForm?.Host?.Dispose();
            IsDestroying = true;

            // Clean after everything
            PostTask(async x =>
            {
                if (serviceRegistry != null)
                    await serviceRegistry.DisposeAsync();
                Game.Exit();
                GameForm?.Dispose();
            }, int.MaxValue)
            // Ensure the properties are correctly set, even in case of a failure (default continuation)
            .ContinueWith(t =>
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
        public object FindGameSidePart(AbsoluteId partId)
        {
            EnsureNotDestroyed();
            EnsureGameAccess();
            return FindPart(partId);
        }

        [CanBeNull]
        protected abstract object FindPart(AbsoluteId partId);

        public T GetService<T>() where T : IEditorGameViewModelService
        {
            EnsureNotDestroyed();
            EnsureAssetAccess();
            if (IsDestroying || serviceRegistry == null)
                return default(T);
            return serviceRegistry.Get<T>();
        }

        /// <inheritdoc/>
        public async Task<bool> StartGame()
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                throw new InvalidOperationException("This controller is beeing disposed.");
            // Run the game in a separate thread (create Form and run)
            sceneGameThread.Start();
            // Wait for the game to start
            await gameStartedTaskSource.Task;
            GameForm.MouseDown += (sender, e) => lastClickPosition = Control.MousePosition;
            // Initialize the WPF GameEngineHwndHost on this thread
            GameForm.Host = new GameEngineHost(windowHandle);

            // TODO: we could check if the game fails to create.
            return true;
        }

        public void OnHideGame()
        {
            Game.IsEditorHidden = true;
        }

        public void OnShowGame()
        {
            Game.IsEditorHidden = false;
        }

        public Vector3 GetMousePositionInScene(bool lastRightClick)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Vector3.Zero;
            var mousePosition = lastRightClick ? lastClickPosition : Control.MousePosition;
            var localPosition = GameForm.Host.PointFromScreen(new Point(mousePosition.X, mousePosition.Y));
            var relativePos = new Vector2((float)(localPosition.X / GameForm.Host.ActualWidth), (float)(localPosition.Y / GameForm.Host.ActualHeight));
            return Game.GetPositionInScene(relativePos);
        }

        public void TriggerActiveRenderStageReevaluation()
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return;
            Game.TriggerActiveRenderStageReevaluation();
        }

        public void ChangeCursor(Cursor cursor)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return;
            GameForm.Cursor = cursor;
        }

        /// <inheritdoc/>
        public Task InvokeAsync(Action callback)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Task.FromCanceled(new CancellationToken(true));
            return PostActionAsync(callback, 0);
        }

        /// <inheritdoc/>
        public Task LowPriorityInvokeAsync(Action callback)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Task.FromCanceled(new CancellationToken(true));
            return PostActionAsync(callback, int.MaxValue);
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Task.FromCanceled<TResult>(new CancellationToken(true));
            return PostActionAsync(callback, 0);
        }

        /// <inheritdoc/>
        public Task InvokeTask(Func<Task> task)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Task.FromCanceled(new CancellationToken(true));
            return PostTask(x => task(), 0);
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task)
        {
            EnsureNotDestroyed();
            if (IsDestroying)
                return Task.FromCanceled<TResult>(new CancellationToken(true));
            return PostTask(x => task(), 0);
        }

        /// <summary>
        /// Verifies that the current thread is the main thread.
        /// </summary>
        /// <returns><c>True</c> if the current thread is the main thread, <c>False</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckAssetAccess()
        {
            return Asset.Dispatcher.CheckAccess();
        }

        /// <summary>
        /// Ensures that the current thread is the main thread. This method will throw an exception if it is not the case.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureAssetAccess()
        {
            Asset.Dispatcher.EnsureAccess();
        }

        /// <summary>
        /// Verifies that the current thread is the game thread.
        /// </summary>
        /// <returns><c>True</c> if the current thread is the game thread, <c>False</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckGameAccess()
        {
            return Thread.CurrentThread == sceneGameThread;
        }

        /// <summary>
        /// Ensures that the current thread is the game thread. This method will throw an exception if it is not the case.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureGameAccess(bool inGameThread = true)
        {
            if (inGameThread && Thread.CurrentThread != sceneGameThread)
                throw new InvalidOperationException("This code must be executed in the game thread.");
            if (!inGameThread && Thread.CurrentThread == sceneGameThread)
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
        /// Checks whether this controller has been disposed, and throws an <see cref="ObjectDisposedException"/> if it is the case.
        /// </summary>
        /// <param name="name">The name to supply to the <see cref="ObjectDisposedException"/>.</param>
        protected void EnsureNotDestroyed(string name = null)
        {
            if (IsDestroyed)
            {
                throw new ObjectDisposedException(name ?? nameof(EditorGameController<TEditorGame>), "This controller has already been disposed.");
            }
        }

        protected virtual void InitializeServices([NotNull] EditorGameServiceRegistry services)
        {
            services.Add(new EditorGameDebugService());
            services.Add(RecoveryService = new EditorGameRecoveryService(Editor) { IsActive = false });
        }

        private void SceneGameRunThread()
        {
            // Create the form from this thread
            GameForm = new EmbeddedGameForm
            {
                TopLevel = false,
                Visible = false,
            };
            windowHandle = GameForm.Handle;
            var context = new GameContextWinforms(GameForm) { InitializeDatabase = false };
            RegisterToDragDropEvents();

            // Wait for shaders to be loaded
            Asset.ServiceProvider.Get<GameStudioBuilderService>().WaitForShaders();

            // Create and register services
            serviceRegistry = new EditorGameServiceRegistry();
            InitializeServices(serviceRegistry);
            Game.RegisterServices(serviceRegistry);

            // Notify game start
            gameStartedTaskSource.SetResult(true);
            Game.Run(context);
            Game.Dispose();
        }

        partial void RegisterToDragDropEvents();

        partial void UnregisterFromDragDropEvents();

        private Task PostTask(Func<ScriptSystem, Task> task, int priority)
        {
            var tcs = new TaskCompletionSource<int>();
            Game.Script.AddTask(async () => { await task(Game.Script); tcs.SetResult(0); }, priority);
            return tcs.Task;
        }

        private Task<T> PostTask<T>(Func<ScriptSystem, Task<T>> task, int priority)
        {
            var tcs = new TaskCompletionSource<T>();
            Game.Script.AddTask(async () => { var result = await task(Game.Script); tcs.SetResult(result); }, priority);
            return tcs.Task;
        }

        private Task PostActionAsync(Action action, int priority)
        {
            if (CheckGameAccess())
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<int>();
            Game.Script.AddTask(() => { action(); tcs.SetResult(0); return tcs.Task; }, priority);
            return tcs.Task;
        }

        private Task<T> PostActionAsync<T>(Func<T> action, int priority)
        {
            if (CheckGameAccess())
            {
                var result = action();
                return Task.FromResult(result);
            }

            var tcs = new TaskCompletionSource<T>();
            Game.Script.AddTask(() => { var result = action(); tcs.SetResult(result); return tcs.Task; }, priority);
            return tcs.Task;
        }
    }
}
