// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// The game service that pauses the game of an editor after an exception (<see cref="Stride.Assets.Editor.EditorGame.EditorGameHost.LastException"/>).
    /// </summary>
    public interface IEditorGameRecoveryViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Lets the game run again after an exception.
        /// </summary>
        void Resume();
    }
}
