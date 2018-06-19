// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Editor.EditorGame.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// React to <see cref="Editor.EditorGame.Game.EditorServiceGame"/> exceptions.
    /// </summary>
    public interface IEditorGameRecoveryViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Resume a faulted <see cref="Editor.EditorGame.Game.EditorServiceGame"/>.
        /// </summary>
        void Resume();
    }
}
