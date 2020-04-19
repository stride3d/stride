// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public interface IEditorGameEntitySelectionService : IEditorGameService
    {
        /// <summary>
        /// Gets whether a selection mask should be displayed in the editor.
        /// </summary>
        bool DisplaySelectionMask { get; }

        /// <summary>
        /// Gets the number of currently selected entities.
        /// </summary>
        int SelectedIdCount { get; }

        /// <summary>
        /// Gets the number of currently selected root entities.
        /// </summary>
        /// <remarks>
        /// An entity is selected as a root if it is currently selected and none of its parent is currently selected.
        /// </remarks>
        int SelectedRootIdCount { get; }

        /// <summary>
        /// Raised in the scene game thread after the selection has been updated.
        /// </summary>
        event EventHandler<EntitySelectionEventArgs> SelectionUpdated;

        /// <summary>
        /// Gets a copy of the set of <see cref="Guid"/> corresponding to the currently selected entities.
        /// </summary>
        IReadOnlyCollection<AbsoluteId> GetSelectedIds();

        /// <summary>
        /// Gets a copy of the set of <see cref="Guid"/> corresponding to the currently selected root entities.
        /// </summary>
        /// <remarks>
        /// An entity is selected as a root if it is currently selected and none of its parent is currently selected.
        /// </remarks>
        IReadOnlyCollection<AbsoluteId> GetSelectedRootIds();

        /// <summary>
        /// Gets the current picking information.
        /// </summary>
        /// <returns>An instance of <see cref="EntityPickingResult"/>.</returns>
        EntityPickingResult Pick();

        /// <summary>
        /// Duplicates the currently selected entities. Note that this will trigger the selection of the duplicated entities.
        /// </summary>
        Task<bool> DuplicateSelection();
    }
}
