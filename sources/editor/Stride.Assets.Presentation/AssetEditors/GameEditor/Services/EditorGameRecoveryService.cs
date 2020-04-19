// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// React to <see cref="EditorServiceGame"/> exceptions.
    /// </summary>
    public class EditorGameRecoveryService : EditorGameServiceBase, IEditorGameRecoveryViewModelService
    {
        private readonly GameEditorViewModel editor;

        public EditorGameRecoveryService([NotNull] GameEditorViewModel editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            this.editor = editor;
        }

        /// <inheritdoc />
        public override bool IsActive { get; set; } = true;

        protected EditorServiceGame Game { get; private set; }

        void IEditorGameRecoveryViewModelService.Resume()
        {
            editor.Dispatcher.EnsureAccess();

            if (!IsActive)
                return;

            // Remove faulted flag
            Game.Faulted = false;
            editor.LastException = null;
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
            editor.Dispatcher.InvokeAsync(() => editor.LastException = e.Exception).Forget();
        }
    }
}
