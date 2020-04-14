// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public interface IEditorGameMaterialHighlightService : IEditorGameService
    {
        /// <summary>
        /// The last entity which has one if its material highlighted (null when nothing is highlighted) 
        /// </summary>/// 
        Entity HighlightedEntity { get; }

        /// <summary>
        /// The index of the material in the last entity which has one if its material highlighted (-1 when nothing is highlighted) 
        /// </summary>
        int HighlightedMaterialIndex { get; }

        /// <summary>
        /// The index of the mesh node in the last entity which has one if its material highlighted (-1 when nothing is highlighted) 
        /// </summary>
        int HighlightedMeshNodeIndex { get; }
    }
}
