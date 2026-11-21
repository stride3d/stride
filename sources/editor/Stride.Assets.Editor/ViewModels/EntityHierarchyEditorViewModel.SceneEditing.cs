// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Translation;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The scene editing commands that place the selection from the view: move it to the view, align it with the view, and align
/// the view to it; and what the other scene editing commands share. Each change of the scene is one undoable operation.
/// </summary>
public abstract partial class EntityHierarchyEditorViewModel
{
    /// <summary>
    /// The distance in front of the camera where <see cref="MoveToViewCommand"/> puts the selection when the camera has no
    /// point to turn around yet.
    /// </summary>
    private const float DefaultViewDistance = 5.0f;

    /// <summary>
    /// Gets the command that moves the selected entities in front of the camera of the view, at the distance of the point the
    /// camera turns around, keeping their places relative to each other.
    /// </summary>
    public ICommandBase MoveToViewCommand => field ??= new AnonymousTaskCommand(ServiceProvider, MoveSelectionToViewAsync, CanTransformSelectionFromView);

    /// <summary>
    /// Gets the command that gives each selected entity the position and the rotation of the camera of the view.
    /// </summary>
    public ICommandBase AlignWithViewCommand => field ??= new AnonymousTaskCommand(ServiceProvider, AlignSelectionWithViewAsync, CanTransformSelectionFromView);

    /// <summary>
    /// Gets the command that moves the camera of the view to the position of the selected entity that has the transformation
    /// gizmo, looking where it looks.
    /// </summary>
    public ICommandBase AlignViewToSelectionCommand => field ??= new AnonymousTaskCommand(ServiceProvider, AlignViewToSelectionAsync, () => isGameLoaded && EntityWithGizmo is not null);

    /// <summary>
    /// Gets the selected entities whose parents are not selected: moving one moves its selected descendants.
    /// </summary>
    internal IReadOnlyList<EntityViewModel> GetSelectedRoots()
    {
        var selected = SelectedItems.ToHashSet();
        return SelectedItems.Where(x => !Ancestors(x).Any(selected.Contains)).Distinct().ToList();

        static IEnumerable<EntityViewModel> Ancestors(EntityViewModel entity)
        {
            for (var parent = entity.TransformParent as EntityViewModel; parent is not null; parent = parent.TransformParent as EntityViewModel)
                yield return parent;
        }
    }

    /// <summary>
    /// Moves the selected entities so that their center is in front of the camera, at the given distance, keeping their
    /// places relative to each other, as one undoable operation.
    /// </summary>
    internal void MoveSelectionToView(Vector3 cameraPosition, Quaternion cameraRotation, float distance)
    {
        var roots = GetSelectedRoots();
        if (roots.Count == 0)
            return;

        var center = GetCenter(roots);
        var target = InFrontOf(cameraPosition, cameraRotation, distance);
        ApplyTransformations(roots.Select(x => (x, Translate(x, target - center))), Tr._p("Undo", "Move to view"));
    }

    /// <summary>
    /// Gives each selected entity the position and the rotation of the camera, keeping its scale, as one undoable operation.
    /// </summary>
    internal void AlignSelectionWith(Vector3 cameraPosition, Quaternion cameraRotation)
    {
        var roots = GetSelectedRoots();
        ApplyTransformations(roots.Select(x => (x, ToLocal(x, cameraPosition, cameraRotation))), Tr._p("Undo", "Align with view"));
    }

    private bool CanTransformSelectionFromView() => isGameLoaded && SelectedItems.Count > 0;

    private async Task MoveSelectionToViewAsync()
    {
        if (await GetViewAsync() is { } view)
            await Dispatcher.InvokeAsync(() => MoveSelectionToView(view.Position, view.Rotation, view.Distance > MathUtil.ZeroTolerance ? view.Distance : DefaultViewDistance));
    }

    private async Task AlignSelectionWithViewAsync()
    {
        if (await GetViewAsync() is { } view)
            await Dispatcher.InvokeAsync(() => AlignSelectionWith(view.Position, view.Rotation));
    }

    private Task AlignViewToSelectionAsync()
    {
        if (!isGameLoaded || EntityWithGizmo is not { } entity || Controller?.GetService<EditorGameEntityCameraService>() is not { } camera)
            return Task.CompletedTask;

        var (position, rotation, _) = GetWorldTransformation(entity);
        var (yaw, pitch) = EditorGameCameraService.ToYawPitch(Vector3.Transform(-Vector3.UnitZ, rotation));
        camera.LoadSettings(position, new Vector2(pitch, yaw));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the camera of the view (game thread): its position, its rotation, and the distance to the point it turns around.
    /// </summary>
    private async Task<(Vector3 Position, Quaternion Rotation, float Distance)?> GetViewAsync()
    {
        if (!isGameLoaded || Controller is not { } controller || controller.GetService<EditorGameEntityCameraService>() is not { } camera)
            return null;

        return await controller.InvokeAsync(() => (camera.Position, Quaternion.RotationYawPitchRoll(camera.Yaw, camera.Pitch, 0), camera.OrbitDistance));
    }

    private void ApplyTransformations(IEnumerable<(EntityViewModel Entity, TransformationTRS Transformation)> changes, string name)
    {
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        var undoRedo = Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        foreach (var (entity, transformation) in list)
            UpdateTransformations(entity, transformation);
        undoRedo.SetName(transaction, name);
    }

    /// <summary>
    /// Gets the transformation of an entity moved by an offset in the world: its rotation and scale don't change.
    /// </summary>
    private static TransformationTRS Translate(EntityViewModel entity, Vector3 worldOffset)
    {
        var transform = entity.AssetSideEntity.Transform;
        var localOffset = worldOffset;
        if (entity.TransformParent is EntityViewModel parent)
        {
            var parentTransform = parent.AssetSideEntity.Transform;
            parentTransform.UpdateWorldMatrix();
            localOffset = Vector3.TransformNormal(worldOffset, Matrix.Invert(parentTransform.WorldMatrix));
        }
        return new TransformationTRS { Position = transform.Position + localOffset, Rotation = transform.Rotation, Scale = transform.Scale };
    }

    /// <summary>
    /// Gets the transformation that puts an entity at a position and a rotation of the world, keeping its scale.
    /// </summary>
    private static TransformationTRS ToLocal(EntityViewModel entity, Vector3 worldPosition, Quaternion worldRotation)
    {
        var transform = entity.AssetSideEntity.Transform;
        var position = worldPosition - GetSceneOffset(entity);
        var rotation = worldRotation;
        if (entity.TransformParent is EntityViewModel parent)
        {
            var parentTransform = parent.AssetSideEntity.Transform;
            parentTransform.UpdateWorldMatrix();
            var scale = Vector3.One;
            parentTransform.WorldToLocal(ref position, ref rotation, ref scale);
        }
        return new TransformationTRS { Position = position, Rotation = rotation, Scale = transform.Scale };
    }

    /// <summary>
    /// Gets the position, the rotation and the scale of an entity in the world of the game, where a child scene has its
    /// offset.
    /// </summary>
    private static (Vector3 Position, Quaternion Rotation, Vector3 Scale) GetWorldTransformation(EntityViewModel entity)
    {
        var transform = entity.AssetSideEntity.Transform;
        transform.UpdateWorldMatrix();
        transform.GetWorldTransformation(out var position, out var rotation, out var scale);
        return (position + GetSceneOffset(entity), rotation, scale);
    }

    /// <summary>
    /// Gets the point at a distance in front of a camera, which looks along -Z rotated by its rotation.
    /// </summary>
    internal static Vector3 InFrontOf(Vector3 cameraPosition, Quaternion cameraRotation, float distance)
    {
        return cameraPosition + Vector3.Transform(-Vector3.UnitZ, cameraRotation) * distance;
    }

    private static Vector3 GetCenter(IReadOnlyList<EntityViewModel> entities)
    {
        var sum = Vector3.Zero;
        foreach (var entity in entities)
            sum += GetWorldTransformation(entity).Position;
        return sum / entities.Count;
    }

    /// <summary>
    /// Gets the offset of the scene of an element and of its parent scenes: the game shows a child scene at its offset.
    /// </summary>
    private static Vector3 GetSceneOffset(EntityHierarchyElementViewModel element)
    {
        var root = element as EntityHierarchyRootViewModel ?? GetRoot((EntityViewModel)element);
        var offset = Vector3.Zero;
        for (var scene = root as SceneRootViewModel; scene is not null; scene = scene.ParentScene)
            offset += scene.Offset;
        return offset;
    }
}
