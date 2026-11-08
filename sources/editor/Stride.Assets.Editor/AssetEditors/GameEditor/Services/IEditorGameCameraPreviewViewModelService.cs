// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Editor.EditorGame.ViewModels;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// Interface allowing a <see cref="ViewModels.GameEditorViewModel"/> to safely access a camera preview.
    /// </summary>
    public interface IEditorGameCameraPreviewViewModelService : IEditorGameViewModelService
    {
        bool IsActive { get; set; }
    }
}
