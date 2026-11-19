// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Translation;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The command that groups the selection under a new parent, as one undoable operation.
/// </summary>
public abstract partial class EntityHierarchyEditorViewModel
{
    /// <summary>
    /// Gets the command that creates an empty entity at the center of the selected entities, makes it their parent without
    /// moving them in the world, and selects it.
    /// </summary>
    public ICommandBase GroupUnderNewParentCommand => field ??= new AnonymousCommand(ServiceProvider, () => GroupSelectionUnderNewParent(), () => TryGetGroupParent(GetSelectedRoots(), out _, out _));

    /// <summary>
    /// Creates an empty entity at the center of the selected entities, under their common parent (the scene or prefab when
    /// they have none), makes it the parent of the selected entities without moving them in the world, and selects it, as
    /// one undoable operation.
    /// </summary>
    /// <returns>The new parent, or <c>null</c> when the selection can't be grouped.</returns>
    internal EntityViewModel? GroupSelectionUnderNewParent()
    {
        var roots = GetSelectedRoots();
        if (!TryGetGroupParent(roots, out var parent, out var index))
            return null;

        // In the order of the tree, so that the children keep their order under the new parent
        var order = HierarchyRoot.Children.DepthFirst(x => x.Children).OfType<EntityViewModel>().Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i);
        var children = roots.OrderBy(x => order.GetValueOrDefault(x, int.MaxValue)).Cast<object>().ToList();
        var entity = new Entity { Name = EntityFactory.ComputeNewName(parent, "Group") };
        entity.Transform.Position = ToLocalPoint(parent.Owner, GetCenter(roots));
        AssetCollectionItemIdHelper.GenerateMissingItemIds(entity);

        var asset = (EntityHierarchyViewModel)parent.Asset;
        var collection = new AssetPartCollection<EntityDesign, Entity> { new EntityDesign(entity, (parent as EntityFolderViewModel)?.Path ?? "") };
        var undoRedo = Session.UndoRedoService;
        EntityViewModel group;
        using (var transaction = undoRedo.CreateTransaction())
        {
            asset.AssetHierarchyPropertyGraph.AddPartToAsset(collection, collection.Single().Value, (parent.Owner as EntityViewModel)?.AssetSideEntity, index);
            group = (EntityViewModel)FindPartViewModel(new AbsoluteId(asset.Id, entity.Id))!;
            // Alt: the moved entities keep their place in the world
            ((IAddChildViewModel)group).AddChildren(children, AddChildModifiers.Alt);
            undoRedo.SetName(transaction, Tr._p("Undo", "Group under a new parent"));
        }

        ClearSelection();
        SelectedContent.Add(group);
        group.IsExpanded = true;
        return group;
    }

    /// <summary>
    /// Finds where the new parent of the given entities goes: the item that holds them all (a folder, an entity or the root),
    /// else their deepest common ancestor; and its index among the entities of that item.
    /// </summary>
    /// <returns><c>false</c> when there are no entities, or when they are in different scenes.</returns>
    internal static bool TryGetGroupParent(IReadOnlyList<EntityViewModel> roots, [NotNullWhen(true)] out EntityHierarchyItemViewModel? parent, out int index)
    {
        parent = null;
        index = 0;
        if (roots.Count == 0 || roots.Any(x => x.Parent is null) || roots.Select(GetRoot).Distinct().Count() > 1)
            return false;

        if (roots.Select(x => x.Parent).Distinct().Count() == 1)
        {
            parent = roots[0].Parent!;
        }
        else
        {
            // The deepest element that is an ancestor of each of them
            var chains = roots.Select(x => Ancestors(x).ToList()).ToList();
            parent = chains[0].First(x => chains.All(chain => chain.Contains(x)));
        }

        var owner = parent.Owner;
        var indices = roots.Select(x => owner.IndexOfEntity(x)).Where(x => x >= 0).ToList();
        index = indices.Count > 0 ? indices.Min() : owner.EntityCount;
        return true;

        static IEnumerable<EntityHierarchyElementViewModel> Ancestors(EntityViewModel entity)
        {
            for (var element = entity.TransformParent; element is not null; element = element.TransformParent)
                yield return element;
        }
    }

    /// <summary>
    /// Gets a point of the world in the space of the children of an element: an entity, or the root of a scene with its offset.
    /// </summary>
    private static Vector3 ToLocalPoint(EntityHierarchyElementViewModel element, Vector3 worldPoint)
    {
        var point = worldPoint - GetSceneOffset(element);
        if (element is not EntityViewModel entity)
            return point;

        var transform = entity.AssetSideEntity.Transform;
        transform.UpdateWorldMatrix();
        return Vector3.TransformCoordinate(point, Matrix.Invert(transform.WorldMatrix));
    }
}
