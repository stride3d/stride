// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization.Contents;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    /// <summary>
    /// A service that provides access to debug information of an editor game.
    /// </summary>
    public interface IEditorGameDebugViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Gets the stats of the game asset manager.
        /// </summary>
        ContentManagerStats ContentManagerStats { get; }
    }
}
