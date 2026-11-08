// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Editor.EditorGame.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// React to <see cref="EditorServiceGame"/> exceptions.
    /// </summary>
    [EditorGameService(typeof(IEditorGameController), Order = 20)]
    public class EditorGameRecoveryService : EditorGameServiceBase, IEditorGameRecoveryViewModelService
    {
        private readonly EditorGameHost host;

        public EditorGameRecoveryService([NotNull] IGameEditorViewModel editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            host = editor.GameHost;
        }

        /// <inheritdoc />
        public override bool IsActive { get; set; } = true;

        protected EditorServiceGame Game { get; private set; }

        void IEditorGameRecoveryViewModelService.Resume()
        {
            host.Dispatcher.EnsureAccess();

            if (!IsActive)
                return;

            // Remove faulted flag
            Game.Faulted = false;
            host.LastException = null;
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            Game = editorGame;
            Game.ExceptionThrown += GameOnExceptionThrown;
            return Task.FromResult(true);
        }

        private void GameOnExceptionThrown(object sender, ExceptionThrownEventArgs e)
        {
            if (!IsActive)
                return;

            e.Handled = true;
            host.Dispatcher.InvokeAsync(() => host.LastException = e.Exception).Forget();
        }
    }
}
