// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class DescribeViewportTool
{
    private const float DefaultAspectRatio = 16f / 9f;

    [McpServerTool(Name = "describe_viewport"), Description("Returns a list of entities visible in the current editor viewport with their projected screen coordinates (normalized 0-1). This helps understand what is rendered in the viewport even when materials or lighting make the image hard to interpret. Use this alongside capture_viewport to correlate visual output with entity positions. The scene must be open in the editor (use open_scene first).")]
    public static async Task<string> DescribeViewport(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene whose viewport to describe")] string sceneId,
        [Description("Maximum number of entities to return (default 100)")] int maxEntities = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(sceneId, out var assetId))
                return new { error = "Invalid scene ID format. Expected a GUID.", result = (object?)null };

            var assetVm = session.GetAssetById(assetId);
            if (assetVm is not SceneViewModel sceneVm)
                return new { error = $"Scene not found: {sceneId}", result = (object?)null };

            var editorsManager = session.ServiceProvider.Get<IAssetEditorsManager>();
            if (!editorsManager.TryGetAssetEditor<GameEditorViewModel>(sceneVm, out var editor))
                return new { error = $"Scene is not open in the editor. Use open_scene first: {sceneId}", result = (object?)null };

            if (!editor.SceneInitialized)
                return new { error = "Editor is still initializing. Please wait and try again.", result = (object?)null };

            var cameraVmService = editor.GetEditorGameService<IEditorGameCameraViewModelService>();
            if (cameraVmService == null)
                return new { error = "Camera service is not available for this editor.", result = (object?)null };

            // Get camera state via SaveSettings snapshot
            var snapshot = new Stride.Assets.Presentation.SceneEditor.SceneSettingsData();
            cameraVmService.SaveSettings(snapshot);

            var camPosition = snapshot.CamPosition;
            var pitch = snapshot.CamPitchYaw.X;
            var yaw = snapshot.CamPitchYaw.Y;

            // Get projection settings from the EditorCameraViewModel
            var entityCameraService = editor.GetEditorGameService<IEditorGameEntityCameraViewModelService>();
            bool isOrthographic = false;
            float fov = 45f;
            float nearPlane = 0.1f;
            float farPlane = 1000f;
            float orthographicSize = 10f;
            float aspectRatio = DefaultAspectRatio;

            if (entityCameraService != null)
            {
                var cam = entityCameraService.Camera;
                isOrthographic = cam.OrthographicProjection;
                fov = cam.FieldOfView;
                nearPlane = cam.NearPlane;
                farPlane = cam.FarPlane;
                orthographicSize = cam.OrthographicSize;
            }

            // Try to get aspect ratio from the camera service (same object implements both interfaces)
            if (cameraVmService is Stride.Assets.Presentation.AssetEditors.GameEditor.Game.IEditorGameCameraService gameCameraService)
            {
                aspectRatio = gameCameraService.AspectRatio;
            }

            // Reconstruct view matrix (same formula as EditorGameCameraService.UpdateViewMatrix)
            var rotation = Quaternion.Invert(Quaternion.RotationYawPitchRoll(yaw, pitch, 0));
            var viewMatrix = Matrix.Translation(-camPosition) * Matrix.RotationQuaternion(rotation);

            // Reconstruct projection matrix
            Matrix projectionMatrix;
            if (isOrthographic)
            {
                var orthoWidth = orthographicSize * aspectRatio;
                var orthoHeight = orthographicSize;
                projectionMatrix = Matrix.OrthoRH(orthoWidth, orthoHeight, nearPlane, farPlane);
            }
            else
            {
                projectionMatrix = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(fov), aspectRatio, nearPlane, farPlane);
            }

            // Use a 1x1 viewport for normalized coordinates (0-1)
            var viewport = new Viewport(0, 0, 1, 1);

            // Walk all entities in the scene asset hierarchy and project them
            var sceneAsset = sceneVm.Asset;
            var rootEntities = sceneAsset.Hierarchy.RootParts;
            var visibleEntities = new List<object>();

            foreach (var rootEntity in rootEntities)
            {
                CollectVisibleEntities(rootEntity, Matrix.Identity, viewport, viewMatrix, projectionMatrix, camPosition, farPlane, visibleEntities);
            }

            // Sort by distance to camera (nearest first), then cap
            var sortedEntities = visibleEntities.Take(maxEntities).ToList();

            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    sceneId = sceneVm.Id.ToString(),
                    sceneName = sceneVm.Name,
                    camera = new
                    {
                        position = new { x = camPosition.X, y = camPosition.Y, z = camPosition.Z },
                        yawDegrees = MathUtil.RadiansToDegrees(yaw),
                        pitchDegrees = MathUtil.RadiansToDegrees(pitch),
                        projection = isOrthographic ? "orthographic" : "perspective",
                        fieldOfViewDegrees = fov,
                        nearPlane,
                        farPlane,
                    },
                    totalEntitiesInScene = sceneAsset.Hierarchy.Parts.Count,
                    visibleEntityCount = sortedEntities.Count,
                    entities = sortedEntities,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static void CollectVisibleEntities(
        Entity entity,
        Matrix parentWorldMatrix,
        Viewport viewport,
        Matrix viewMatrix,
        Matrix projectionMatrix,
        Vector3 camPosition,
        float farPlane,
        List<object> results)
    {
        // Build this entity's local matrix from its Transform component
        var transform = entity.Transform;
        var localMatrix = Matrix.Scaling(transform.Scale)
            * Matrix.RotationQuaternion(transform.Rotation)
            * Matrix.Translation(transform.Position);
        var worldMatrix = localMatrix * parentWorldMatrix;

        // Extract world position
        var worldPos = worldMatrix.TranslationVector;

        // Compute distance to camera
        var distanceToCamera = Vector3.Distance(worldPos, camPosition);

        // Project to screen space
        var screenPos = viewport.Project(worldPos, projectionMatrix, viewMatrix, Matrix.Identity);

        // Check if entity is in front of camera and within screen bounds
        bool isInFrustum = screenPos.Z >= 0 && screenPos.Z <= 1
            && screenPos.X >= -0.5f && screenPos.X <= 1.5f
            && screenPos.Y >= -0.5f && screenPos.Y <= 1.5f;

        var componentNames = entity.Components
            .Select(c => c.GetType().Name)
            .ToList();

        results.Add(new
        {
            id = entity.Id.ToString(),
            name = entity.Name,
            worldPosition = new { x = Math.Round(worldPos.X, 2), y = Math.Round(worldPos.Y, 2), z = Math.Round(worldPos.Z, 2) },
            screenPosition = isInFrustum
                ? new { x = Math.Round(screenPos.X, 3), y = Math.Round(screenPos.Y, 3) }
                : null,
            distanceToCamera = Math.Round(distanceToCamera, 2),
            isVisible = isInFrustum,
            components = componentNames,
        });

        // Recurse into children
        if (transform != null)
        {
            foreach (var childTransform in transform.Children)
            {
                if (childTransform.Entity != null)
                {
                    CollectVisibleEntities(childTransform.Entity, worldMatrix, viewport, viewMatrix, projectionMatrix, camPosition, farPlane, results);
                }
            }
        }
    }
}
