// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Interop;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    /// <summary>
    /// Base class for the view model of asset editors that runs an instance of a Game.
    /// </summary>
    // TODO: this base class cannot do much yet. In the future, we can make some EditorGameServices compatible with it and use it to edit assets other than EntityHierarchy, such as Model, Material, etc.
    public abstract class GameEditorViewModel : AssetEditorViewModel
    {
        private Exception lastException;
        private bool sceneInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        protected GameEditorViewModel([NotNull] AssetViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset)
        {
            Controller = controllerFactory(this);
            CopyErrorToClipboardCommand = new AnonymousCommand(ServiceProvider, CopyErrorToClipboard);
            ResumeCommand = new AnonymousCommand(ServiceProvider, ResumeFromError);
        }

        /// <summary>
        /// The last exception from the underlying Game.
        /// </summary>
        /// <seealso cref="EditorGameRecoveryService"/>
        public Exception LastException { get { return lastException; } set { SetValue(ref lastException, value); } }

        /// <summary>
        /// Gets or sets whether the scene of the game has been fully initialized.
        /// </summary>
        /// <remarks>When the scene is initialized, its content might still not be fully loaded.</remarks>
        public bool SceneInitialized { get { return sceneInitialized; } private set { SetValue(ref sceneInitialized, value); } }

        /// <summary>
        /// The <see cref="SessionNodeContainer"/> to use to create Quantum graphs for asset side elements.
        /// </summary>
        [NotNull]
        internal SessionNodeContainer NodeContainer => Session.AssetNodeContainer;

        [NotNull]
        protected internal IEditorGameController Controller { get; }

        [NotNull]
        public ICommandBase CopyErrorToClipboardCommand { get; }

        [NotNull]
        public ICommandBase ResumeCommand { get; }

        /// <inheritdoc/>
        public sealed override async Task<bool> Initialize()
        {
            SceneInitialized = await InitializeEditor();
            return SceneInitialized;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(GameEditorViewModel));

            Controller.Destroy();
            SceneInitialized = false;

            base.Destroy();
        }

        public void HideGame()
        {
            Controller.OnHideGame();
        }

        public void ShowGame()
        {
            Controller.OnShowGame();
        }

        protected virtual async Task<bool> InitializeEditor()
        {
            Dispatcher.EnsureAccess();

            if (!await Controller.StartGame())
                return false;

            // Wait for the game to be fully initialized (including services).
            // Note: in the case of a game editor, LoadContent() is not supposed to load anything so this should be almost instant.
            await Controller.GameContentLoaded;

            if (!await Controller.CreateScene())
                return false;

            await OnGameContentLoaded();

            return true;
        }

        /// <summary>
        /// Processes the initialization that should occurs once the game content has been loaded.
        /// </summary>
        protected virtual Task OnGameContentLoaded()
        {
            // Do nothing by default
            return Task.CompletedTask;
        }

        private void CopyErrorToClipboard()
        {
            var log = LastException?.ToString();
            if (!string.IsNullOrEmpty(log))
                SafeClipboard.SetText(log);
        }

        private void ResumeFromError()
        {
            Controller.GetService<IEditorGameRecoveryViewModelService>()?.Resume();
        }
    }
}
