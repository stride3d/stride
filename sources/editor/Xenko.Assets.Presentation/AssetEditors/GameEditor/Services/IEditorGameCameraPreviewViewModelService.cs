// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Editor.EditorGame.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// Interface allowing a <see cref="ViewModels.GameEditorViewModel"/> to safely access a camera preview.
    /// </summary>
    public interface IEditorGameCameraPreviewViewModelService : IEditorGameViewModelService
    {
        bool IsActive { get; set; }
    }
}
