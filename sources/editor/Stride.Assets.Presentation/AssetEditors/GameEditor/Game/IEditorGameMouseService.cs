// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    /// <summary>
    /// An interface representing a service that can control the mouse.
    /// </summary>
    public interface IEditorGameMouseService : IEditorGameService
    {
        /// <summary>
        /// Gets whether this instance is currently controlling the mouse.
        /// </summary>
        bool IsControllingMouse { get; }

        /// <summary>
        /// Gets whether the mouse is available to be be controlled.
        /// </summary>
        bool IsMouseAvailable { get; }
    }
}
