// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// Interface allowing a <see cref="ViewModels.GameEditorViewModel"/> to safely access a camera preview.
    /// </summary>
    public interface IEditorGameCameraPreviewViewModelService : IEditorGameViewModelService
    {
        bool IsActive { get; set; }
    }
}
