// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Translation;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The command that snaps the selection to the floor, as one undoable operation.
/// </summary>
public abstract partial class EntityHierarchyEditorViewModel
{
    /// <summary>
    /// Gets the command that drops each selected entity until its bounds touch the first surface below it, or the ground
    /// plane.
    /// </summary>
    public ICommandBase SnapToFloorCommand => field ??= new AnonymousTaskCommand(ServiceProvider, SnapSelectionToFloorAsync, CanTransformSelectionFromView);

    /// <summary>
    /// Moves the selected entities up or down by the given world offsets, as one undoable operation.
    /// </summary>
    /// <param name="offsets">The vertical offset of each entity, by identifier.</param>
    internal void SnapSelectionToFloor(IReadOnlyDictionary<AbsoluteId, float> offsets)
    {
        var moves = offsets
            .Where(x => MathF.Abs(x.Value) > MathUtil.ZeroTolerance)
            .Select(x => (Entity: FindPartViewModel(x.Key) as EntityViewModel, Offset: x.Value))
            .Where(x => x.Entity is not null)
            .Select(x => (x.Entity!, Translate(x.Entity!, new Vector3(0, x.Offset, 0))));
        ApplyTransformations(moves, Tr._p("Undo", "Snap to floor"));
    }

    private async Task SnapSelectionToFloorAsync()
    {
        if (!isGameLoaded || Controller is not { } controller || controller.GetService<EditorGameEntitySelectionService>() is not { } selection)
            return;

        var ids = GetSelectedRoots().Select(x => x.Id).ToList();
        var offsets = await controller.InvokeAsync(() =>
        {
            var entities = ids.Select(id => (Id: id, Entity: controller.FindGameSidePart(id) as Entity)).Where(x => x.Entity is not null).ToList();
            var moved = entities.SelectMany(x => SceneEditingGeometry.SelfAndDescendants(x.Entity!)).ToHashSet();
            // The other models are the surfaces: the selected entities don't land on each other
            var surfaces = selection.ContentEntities()
                .Where(x => !moved.Contains(x))
                .SelectMany(x => x.Components.OfType<ModelComponent>())
                .Select(x => SceneEditingGeometry.TryGetBounds(x, out var bounds) ? bounds : (BoundingBox?)null)
                .OfType<BoundingBox>()
                .ToList();
            var result = new Dictionary<AbsoluteId, float>();
            foreach (var (id, entity) in entities)
            {
                if (SceneEditingGeometry.GetFloorOffset(SceneEditingGeometry.GetSnapBounds(entity!), surfaces) is { } offset)
                    result[id] = offset;
            }
            return result;
        });
        await Dispatcher.InvokeAsync(() => SnapSelectionToFloor(offsets));
    }
}
