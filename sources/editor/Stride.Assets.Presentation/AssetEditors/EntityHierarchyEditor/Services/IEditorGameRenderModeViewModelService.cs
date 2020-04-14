// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    public interface IEditorGameRenderModeViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Gets or sets the material filter stream.
        /// </summary>
        /// <value>The material filter stream.</value>
        EditorRenderMode RenderMode { get; set; }
    }
}
