// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The entities of a scene editor that can be selected in its game, and the ones selected, shared by the UI thread and the
/// game thread.
/// </summary>
/// <remarks>
/// An entity is selected as a root when none of its parents is selected: the transformation gizmos move the roots.
/// </remarks>
internal sealed class EntitySelection
{
    private readonly object lockObject = new();
    private readonly HashSet<AbsoluteId> selectableIds = [];
    private readonly HashSet<AbsoluteId> selectedIds = [];
    private readonly HashSet<AbsoluteId> selectedRootIds = [];

    /// <summary>
    /// Gets the number of selected entities.
    /// </summary>
    public int Count { get { lock (lockObject) return selectedIds.Count; } }

    /// <summary>
    /// Gets the number of entities selected as roots.
    /// </summary>
    public int RootCount { get { lock (lockObject) return selectedRootIds.Count; } }

    /// <summary>
    /// Lets an entity be selected: its clone is in the game.
    /// </summary>
    public void AddSelectable(AbsoluteId id)
    {
        lock (lockObject)
            selectableIds.Add(id);
    }

    /// <summary>
    /// Stops an entity from being selected: its clone left the game.
    /// </summary>
    public void RemoveSelectable(AbsoluteId id)
    {
        lock (lockObject)
            selectableIds.Remove(id);
    }

    public bool IsSelectable(AbsoluteId id)
    {
        lock (lockObject)
            return selectableIds.Contains(id);
    }

    public bool Contains(AbsoluteId id)
    {
        lock (lockObject)
            return selectedIds.Contains(id);
    }

    /// <summary>
    /// Gets a copy of the selected entities.
    /// </summary>
    public IReadOnlyCollection<AbsoluteId> GetSelectedIds()
    {
        lock (lockObject)
            return selectedIds.ToList();
    }

    /// <summary>
    /// Gets a copy of the entities selected as roots.
    /// </summary>
    public IReadOnlyCollection<AbsoluteId> GetSelectedRootIds()
    {
        lock (lockObject)
            return selectedRootIds.ToList();
    }

    /// <summary>
    /// Adds a selectable entity to the selection (UI thread).
    /// </summary>
    /// <returns><c>false</c> when the entity can't be selected or was already selected.</returns>
    public bool Add(EntityViewModel entity)
    {
        lock (lockObject)
        {
            if (!selectableIds.Contains(entity.Id) || !selectedIds.Add(entity.Id))
                return false;

            // A selected parent stays the root
            if (Parents(entity).Any(x => selectedIds.Contains(x.Id)))
                return true;

            selectedRootIds.Add(entity.Id);
            // The selected children are no longer roots
            foreach (var child in Descendants(entity))
            {
                selectedRootIds.Remove(child.Id);
            }
            return true;
        }
    }

    /// <summary>
    /// Removes an entity from the selection (UI thread).
    /// </summary>
    /// <returns><c>false</c> when the entity was not selected.</returns>
    public bool Remove(EntityViewModel entity)
    {
        lock (lockObject)
        {
            if (!selectedIds.Remove(entity.Id))
                return false;

            if (!selectedRootIds.Remove(entity.Id))
                return true;

            // The selected children with no other selected parent below the entity become roots
            foreach (var child in Descendants(entity))
            {
                if (selectedIds.Contains(child.Id) && Parents(child).TakeWhile(x => x != entity).All(x => !selectedIds.Contains(x.Id)))
                    selectedRootIds.Add(child.Id);
            }
            return true;
        }
    }

    /// <summary>
    /// Empties the selection.
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            selectedIds.Clear();
            selectedRootIds.Clear();
        }
    }

    private static IEnumerable<EntityViewModel> Parents(EntityViewModel entity)
    {
        for (var parent = entity.Parent as EntityViewModel; parent is not null; parent = parent.Parent as EntityViewModel)
        {
            yield return parent;
        }
    }

    private static IEnumerable<EntityViewModel> Descendants(EntityViewModel entity)
    {
        var queue = new Queue<EntityViewModel>(entity.Children.OfType<EntityViewModel>());
        while (queue.TryDequeue(out var child))
        {
            yield return child;
            foreach (var grandChild in child.Children.OfType<EntityViewModel>())
            {
                queue.Enqueue(grandChild);
            }
        }
    }
}
