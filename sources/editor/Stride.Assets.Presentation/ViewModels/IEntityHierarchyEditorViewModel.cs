// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// The editor of a scene or a prefab, as the items of its hierarchy see it: the selection, the game it loads the hierarchy
/// into, and the settings it keeps for the scenes.
/// </summary>
/// <remarks>
/// The root of the hierarchy holds it (<see cref="EntityHierarchyRootViewModel.Editor"/>), and every item reaches it through
/// its asset (<see cref="EntityHierarchyViewModel.HierarchyRoot"/>), even after it left the hierarchy. A hierarchy that
/// has no editor (<c>null</c>) loads nothing and selects nothing.
/// </remarks>
public interface IEntityHierarchyEditorViewModel
{
    /// <summary>
    /// Gets the selected items of the hierarchy (entities, folders and roots).
    /// </summary>
    ObservableSet<object> SelectedContent { get; }

    /// <summary>
    /// Gets or sets the root that gets the entities created "in the root": the edited scene, or one of its child scenes.
    /// </summary>
    EntityHierarchyRootViewModel ActiveRoot { get; set; }

    /// <summary>
    /// Gets the policies that turn an asset dropped on the hierarchy into components or entities.
    /// </summary>
    IReadOnlyList<IAddAssetPolicy> AddAssetPolicies { get; }

    /// <summary>
    /// Finds the view model of the part with the given id, in any root that the editor shows, or <c>null</c>.
    /// </summary>
    IAssetPartViewModel? FindPartViewModel(AbsoluteId id);

    /// <summary>
    /// Clears the selection.
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Selects again the items of <paramref name="previousSelection"/> that are in <paramref name="newSelection"/>, after
    /// they moved: an entity moved from another asset has a new view model, found by its id.
    /// </summary>
    void FixupAndRestoreSelection(IEnumerable<object> previousSelection, IReadOnlyCollection<object> newSelection);

    /// <summary>
    /// Gathers the assets the given entity is based on (its prefabs), recursively, and with its children's when
    /// <paramref name="includeChildren"/> is <c>true</c>.
    /// </summary>
    ISet<AssetId> GatherAllBasePartAssets(EntityViewModel entity, bool includeChildren);

    /// <summary>
    /// Loads the given roots into the game: a scene with its entities, or the entities of a prefab. The editor waits for
    /// its game first. The roots set their state themselves (<see cref="EntityHierarchyElementViewModel.IsLoaded"/>).
    /// </summary>
    Task LoadRootsAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots);

    /// <summary>
    /// Unloads the given roots from the game, in this order.
    /// </summary>
    Task UnloadRootsAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots);

    /// <summary>
    /// Moves the given scene in the game to its offset (<see cref="SceneRootViewModel.Offset"/>) from its parent scene.
    /// </summary>
    Task UpdateSceneOffsetAsync(SceneRootViewModel root);

    /// <summary>
    /// Gets whether the given child scene was loaded when its editor was last closed. The editor waits for its game first.
    /// </summary>
    Task<bool> IsSceneLoadedOnOpenAsync(SceneRootViewModel root);

    /// <summary>
    /// Remembers whether the given scene is loaded (<see cref="EntityHierarchyElementViewModel.IsLoaded"/>), for the next
    /// time its editor opens.
    /// </summary>
    void SaveSceneLoaded(SceneRootViewModel root);

    /// <summary>
    /// Moves the camera of the game to the given entity.
    /// </summary>
    Task FocusOnEntityAsync(EntityViewModel entity);
}
