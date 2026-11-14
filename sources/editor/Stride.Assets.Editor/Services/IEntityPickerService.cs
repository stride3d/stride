// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// Shows the entity picker of a scene or prefab editor: the user picks an entity, or one of the components of an entity, from
/// its hierarchy. Each editor (WPF, Avalonia) implements it with its own window.
/// </summary>
/// <remarks>
/// The Pick buttons of the entity and component properties (<see cref="Quantum.NodePresenters.Commands.PickupEntityCommand"/>
/// and <see cref="Quantum.NodePresenters.Commands.PickupEntityComponentCommand"/>) look it up in the session's services, and
/// pick nothing in an editor that has none.
/// </remarks>
[Experimental("STREXP007")]
public interface IEntityPickerService
{
    /// <summary>
    /// Shows the picker and waits for the user to pick.
    /// </summary>
    /// <param name="editor">The editor whose hierarchy is shown.</param>
    /// <param name="componentType">The type of the component to pick, or <c>null</c> to pick an entity.</param>
    /// <param name="filter">The items of the hierarchy that are shown, with their children.</param>
    /// <returns>The picked entity, or <c>null</c> when the user cancelled.</returns>
    Task<IPickedEntity?> PickEntityAsync(EntityHierarchyEditorViewModel editor, Type? componentType, Func<EntityHierarchyItemViewModel, bool> filter);
}
