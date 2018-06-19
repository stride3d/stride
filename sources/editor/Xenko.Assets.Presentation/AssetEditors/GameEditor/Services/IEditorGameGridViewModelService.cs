// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;
using Xenko.Editor.EditorGame.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// A service that provides access to the grid of an editor game.
    /// </summary>
    public interface IEditorGameGridViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Gets or sets whether the grid is currently visible.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the color to apply to the grid.
        /// </summary>
        Color3 Color { get; set; }

        /// <summary>
        /// Gets or sets the alpha level of the grid.
        /// </summary>
        float Alpha { get; set; }
    }
}
