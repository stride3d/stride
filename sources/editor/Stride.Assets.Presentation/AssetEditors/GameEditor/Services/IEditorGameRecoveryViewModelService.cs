// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
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
