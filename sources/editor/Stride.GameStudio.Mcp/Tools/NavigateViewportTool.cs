// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Presentation.AssetEditors;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Mathematics;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class NavigateViewportTool
{
    [McpServerTool(Name = "navigate_viewport"), Description("Controls the editor viewport camera. Actions: 'set_orientation' changes camera to a preset view (Front, Back, Top, Bottom, Left, Right). 'set_position' moves the camera to specific coordinates with optional yaw/pitch angles. 'set_projection' toggles between perspective and orthographic modes. 'set_field_of_view' changes the FOV (perspective) or orthographic size. 'get_camera_state' returns the current camera position, orientation, and projection settings. The scene must be open in the editor (use open_scene first).")]
    public static async Task<string> NavigateViewport(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the scene whose viewport camera to control")] string sceneId,
        [Description("The action to perform: 'set_orientation', 'set_position', 'set_projection', 'set_field_of_view', 'get_camera_state'")] string action,
        [Description("For 'set_orientation': one of 'Front', 'Back', 'Top', 'Bottom', 'Left', 'Right'")] string? orientation = null,
        [Description("For 'set_position': X coordinate")] float? x = null,
        [Description("For 'set_position': Y coordinate")] float? y = null,
        [Description("For 'set_position': Z coordinate")] float? z = null,
        [Description("For 'set_position': yaw angle in degrees (horizontal rotation, default ~45°)")] float? yaw = null,
        [Description("For 'set_position': pitch angle in degrees (vertical rotation, default ~-15°)")] float? pitch = null,
        [Description("For 'set_projection': true for orthographic, false for perspective")] bool? orthographic = null,
        [Description("For 'set_field_of_view': FOV in degrees (perspective) or orthographic size")] float? value = null,
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

            var entityCameraService = editor.GetEditorGameService<IEditorGameEntityCameraViewModelService>();

            switch (action.ToLowerInvariant())
            {
                case "set_orientation":
                    return SetOrientation(cameraVmService, orientation);
                case "set_position":
                    return SetPosition(cameraVmService, x, y, z, yaw, pitch);
                case "set_projection":
                    return SetProjection(cameraVmService, orthographic);
                case "set_field_of_view":
                    return SetFieldOfView(cameraVmService, entityCameraService, value);
                case "get_camera_state":
                    return GetCameraState(cameraVmService, entityCameraService);
                default:
                    return new { error = $"Unknown action: '{action}'. Expected 'set_orientation', 'set_position', 'set_projection', 'set_field_of_view', or 'get_camera_state'.", result = (object?)null };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object SetOrientation(IEditorGameCameraViewModelService cameraService, string? orientation)
    {
        if (string.IsNullOrEmpty(orientation))
            return new { error = "orientation parameter is required for 'set_orientation' action.", result = (object?)null };

        if (!Enum.TryParse<CameraOrientation>(orientation, ignoreCase: true, out var cameraOrientation))
            return new { error = $"Invalid orientation: '{orientation}'. Expected one of: Front, Back, Top, Bottom, Left, Right.", result = (object?)null };

        cameraService.ResetCameraOrientation(cameraOrientation);

        return new
        {
            error = (string?)null,
            result = (object)new
            {
                action = "set_orientation",
                orientation = cameraOrientation.ToString(),
            },
        };
    }

    private static object SetPosition(IEditorGameCameraViewModelService cameraService, float? x, float? y, float? z, float? yawDegrees, float? pitchDegrees)
    {
        if (x == null || y == null || z == null)
            return new { error = "x, y, and z parameters are required for 'set_position' action.", result = (object?)null };

        var snapshot = new Stride.Assets.Presentation.SceneEditor.SceneSettingsData();
        cameraService.SaveSettings(snapshot);

        snapshot.CamPosition = new Vector3(x.Value, y.Value, z.Value);

        if (yawDegrees.HasValue)
            snapshot.CamPitchYaw = new Vector2(snapshot.CamPitchYaw.X, MathUtil.DegreesToRadians(yawDegrees.Value));

        if (pitchDegrees.HasValue)
            snapshot.CamPitchYaw = new Vector2(MathUtil.DegreesToRadians(pitchDegrees.Value), snapshot.CamPitchYaw.Y);

        cameraService.LoadSettings(snapshot);

        return new
        {
            error = (string?)null,
            result = (object)new
            {
                action = "set_position",
                position = new { x = x.Value, y = y.Value, z = z.Value },
                yawDegrees = MathUtil.RadiansToDegrees(snapshot.CamPitchYaw.Y),
                pitchDegrees = MathUtil.RadiansToDegrees(snapshot.CamPitchYaw.X),
            },
        };
    }

    private static object SetProjection(IEditorGameCameraViewModelService cameraService, bool? orthographic)
    {
        if (orthographic == null)
            return new { error = "orthographic parameter is required for 'set_projection' action.", result = (object?)null };

        cameraService.SetOrthographicProjection(orthographic.Value);

        return new
        {
            error = (string?)null,
            result = (object)new
            {
                action = "set_projection",
                projection = orthographic.Value ? "orthographic" : "perspective",
            },
        };
    }

    private static object SetFieldOfView(IEditorGameCameraViewModelService cameraService, IEditorGameEntityCameraViewModelService? entityCameraService, float? value)
    {
        if (value == null)
            return new { error = "value parameter is required for 'set_field_of_view' action.", result = (object?)null };

        bool isOrthographic = entityCameraService?.Camera?.OrthographicProjection ?? false;

        if (isOrthographic)
        {
            cameraService.SetOrthographicSize(value.Value);
            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    action = "set_field_of_view",
                    projection = "orthographic",
                    orthographicSize = value.Value,
                },
            };
        }
        else
        {
            cameraService.SetFieldOfView(value.Value);
            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    action = "set_field_of_view",
                    projection = "perspective",
                    fieldOfViewDegrees = value.Value,
                },
            };
        }
    }

    private static object GetCameraState(IEditorGameCameraViewModelService cameraService, IEditorGameEntityCameraViewModelService? entityCameraService)
    {
        var snapshot = new Stride.Assets.Presentation.SceneEditor.SceneSettingsData();
        cameraService.SaveSettings(snapshot);

        bool isOrthographic = false;
        float fov = 45f;
        float nearPlane = 0.1f;
        float farPlane = 1000f;
        float orthographicSize = 10f;
        float moveSpeed = EditorGameCameraService.DefaultMoveSpeed;

        if (entityCameraService != null)
        {
            var cam = entityCameraService.Camera;
            isOrthographic = cam.OrthographicProjection;
            fov = cam.FieldOfView;
            nearPlane = cam.NearPlane;
            farPlane = cam.FarPlane;
            orthographicSize = cam.OrthographicSize;
            moveSpeed = cam.MoveSpeed;
        }

        float aspectRatio = 16f / 9f;
        if (cameraService is IEditorGameCameraService gameCameraService)
        {
            aspectRatio = gameCameraService.AspectRatio;
        }

        return new
        {
            error = (string?)null,
            result = (object)new
            {
                action = "get_camera_state",
                position = new { x = snapshot.CamPosition.X, y = snapshot.CamPosition.Y, z = snapshot.CamPosition.Z },
                yawDegrees = MathUtil.RadiansToDegrees(snapshot.CamPitchYaw.Y),
                pitchDegrees = MathUtil.RadiansToDegrees(snapshot.CamPitchYaw.X),
                projection = isOrthographic ? "orthographic" : "perspective",
                fieldOfViewDegrees = fov,
                orthographicSize,
                nearPlane,
                farPlane,
                aspectRatio,
                moveSpeed,
            },
        };
    }
}
