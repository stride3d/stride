// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Engine;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class SetTransformTool
{
    [McpServerTool(Name = "set_transform"), Description("Sets the transform (position, rotation, scale) of an entity. The scene must be open in the editor (use open_scene first). Only the provided components are changed; omitted ones keep their current values. Rotation uses Euler angles in degrees. This operation supports undo/redo in the editor.")]
    public static async Task<string> SetTransform(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene containing the entity")] string sceneId,
        [Description("The entity ID to modify")] string entityId,
        [Description("Position X coordinate")] float? positionX = null,
        [Description("Position Y coordinate")] float? positionY = null,
        [Description("Position Z coordinate")] float? positionZ = null,
        [Description("Rotation around X axis in degrees (Euler)")] float? rotationX = null,
        [Description("Rotation around Y axis in degrees (Euler)")] float? rotationY = null,
        [Description("Rotation around Z axis in degrees (Euler)")] float? rotationZ = null,
        [Description("Scale X factor")] float? scaleX = null,
        [Description("Scale Y factor")] float? scaleY = null,
        [Description("Scale Z factor")] float? scaleZ = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
            {
                return new { error = "Invalid scene ID format. Expected a GUID.", transform = (object?)null };
            }

            if (!Guid.TryParse(entityId, out var entityGuid))
            {
                return new { error = "Invalid entity ID format. Expected a GUID.", transform = (object?)null };
            }

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
            {
                return new { error = $"Scene not found: {sceneId}", transform = (object?)null };
            }

            // Get the editor for this scene
            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<EntityHierarchyEditorViewModel>(sceneVm, out var editor))
            {
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", transform = (object?)null };
            }

            // Find the entity
            var absoluteId = new AbsoluteId(assetId, entityGuid);
            var partVm = editor.FindPartViewModel(absoluteId);
            if (partVm is not EntityViewModel entityVm)
            {
                return new { error = $"Entity not found in scene: {entityId}", transform = (object?)null };
            }

            var entity = entityVm.AssetSideEntity;
            var transformNode = session.AssetNodeContainer.GetOrCreateNode(entity.Transform);
            if (transformNode == null)
            {
                return new { error = "Failed to access transform node for entity.", transform = (object?)null };
            }

            // Read current values
            var currentPosition = (Vector3)transformNode[nameof(TransformComponent.Position)].Retrieve();
            var currentRotation = (Quaternion)transformNode[nameof(TransformComponent.Rotation)].Retrieve();
            var currentScale = (Vector3)transformNode[nameof(TransformComponent.Scale)].Retrieve();

            // Get current Euler angles (radians) via TransformComponent's built-in conversion
            var currentEulerRad = entity.Transform.RotationEulerXYZ;
            var currentEulerDeg = new Vector3(
                MathUtil.RadiansToDegrees(currentEulerRad.X),
                MathUtil.RadiansToDegrees(currentEulerRad.Y),
                MathUtil.RadiansToDegrees(currentEulerRad.Z));

            // Build new values, keeping current where not specified
            var newPosition = new Vector3(
                positionX ?? currentPosition.X,
                positionY ?? currentPosition.Y,
                positionZ ?? currentPosition.Z);

            var newEulerDeg = new Vector3(
                rotationX ?? currentEulerDeg.X,
                rotationY ?? currentEulerDeg.Y,
                rotationZ ?? currentEulerDeg.Z);
            // Convert Euler XYZ degrees → Quaternion (same formula as TransformComponent.RotationEulerXYZ setter)
            var newEulerRad = new Vector3(
                MathUtil.DegreesToRadians(newEulerDeg.X),
                MathUtil.DegreesToRadians(newEulerDeg.Y),
                MathUtil.DegreesToRadians(newEulerDeg.Z));
            var newRotation = Quaternion.RotationX(newEulerRad.X)
                            * Quaternion.RotationY(newEulerRad.Y)
                            * Quaternion.RotationZ(newEulerRad.Z);

            var newScale = new Vector3(
                scaleX ?? currentScale.X,
                scaleY ?? currentScale.Y,
                scaleZ ?? currentScale.Z);

            // Apply changes through the property graph with undo/redo
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                if (currentPosition != newPosition)
                    transformNode[nameof(TransformComponent.Position)].Update(newPosition);
                if (currentRotation != newRotation)
                    transformNode[nameof(TransformComponent.Rotation)].Update(newRotation);
                if (currentScale != newScale)
                    transformNode[nameof(TransformComponent.Scale)].Update(newScale);

                undoRedoService.SetName(transaction, $"Set transform '{entityVm.Name}'");
            }

            return new
            {
                error = (string?)null,
                transform = (object)new
                {
                    id = entityId,
                    name = entityVm.Name,
                    position = new { x = newPosition.X, y = newPosition.Y, z = newPosition.Z },
                    rotation = new { x = newEulerDeg.X, y = newEulerDeg.Y, z = newEulerDeg.Z },
                    scale = new { x = newScale.X, y = newScale.Y, z = newScale.Z },
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
