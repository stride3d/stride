// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Editor.EditorGame;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Settings;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Engine.Processors;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The settings of the view of a scene or prefab editor, saved per asset in the user settings of its package.
/// </summary>
/// <remarks>
/// The agnostic copy of the WPF <c>SceneSettingsData</c>, member for member and with the same data contract, so both
/// editors read and write the same <see cref="PackageSceneSettings.SceneSettings"/> entries: the Avalonia editor keeps the
/// members it does not use yet as it read them. Until the Phase 3 consolidation, a member added to one copy must be added
/// to the other.
/// </remarks>
[DataContract(nameof(SceneSettingsData))]
public sealed class SceneSettingsData
{
    // Camera view parameters
    public Vector3 CamPosition = EditorGameCameraService.DefaultPosition;
    public Vector2 CamPitchYaw = new(EditorGameCameraService.DefaultPitch, EditorGameCameraService.DefaultYaw);

    // Camera projection parameters
    public CameraProjectionMode CamProjection = CameraProjectionMode.Perspective;
    public float CamVerticalFieldOfView = CameraComponent.DefaultVerticalFieldOfView;
    public float CamOrthographicSize = CameraComponent.DefaultOrthographicSize;
    public float CamNearClipPlane = CameraComponent.DefaultNearClipPlane;
    public float CamFarClipPlane = CameraComponent.DefaultFarClipPlane;
    public float CamAspectRatio = CameraComponent.DefaultAspectRatio;
    public float CamMoveSpeed = EditorGameCameraService.DefaultMoveSpeed;

    // Snapping
    public bool TranslationSnapActive = false;
    public float TranslationSnapValue = 1.0f;
    public bool RotationSnapActive = false;
    public float RotationSnapValue = 22.5f;
    public bool ScaleSnapActive = false;
    public float ScaleSnapValue = 1.1f;
    public float SceneUnit = 1.0f;

    // Gizmos
    public bool SelectionMaskVisible = true;
    public bool GridVisible = true;
    public Color3 GridColor = (Color3)new Color(180, 180, 180);
    public float GridOpacity = 0.35f;
    // The WPF ViewportGridAxis.Y
    public int GridAxisIndex = 2;
    public bool CameraPreviewVisible = true;
    public SceneRenderMode RenderMode = SceneRenderMode.SingleStream;
    public string ActiveStream = string.Empty;
    public double TransformationGizmoSize = 1.0;
    public double ComponentGizmoSize = 1.0;
    public bool FixedSizeGizmos = false;
    public List<string> HiddenGizmos = [];
    /// <summary>
    /// The keys of the overlays (navigation meshes, debug shapes, ...) shown in this scene.
    /// </summary>
    public List<string> VisibleOverlays = [];
    /// <summary>
    /// Read from scenes saved before <see cref="VisibleOverlays"/> existed, never written.
    /// </summary>
    public List<Guid> VisibleNavigationGroups = [];
    public bool LightProbeWireframe = false;
    public int LightProbeBounces = 1;

    // Scene
    /// <summary>
    /// Indicates whether the scene will be automatically loaded when opened in the editor.
    /// </summary>
    /// <remarks>
    /// This settings only make sense in the context of a scene hierarchy. A single scene is always loaded.
    /// </remarks>
    public bool SceneLoaded = false;

    /// <summary>
    /// Creates a new instance of the default scene settings.
    /// </summary>
    public static SceneSettingsData CreateDefault()
    {
        // The transform gizmo is the fallback shown on entities without any other gizmo; the rest is what the gizmo classes declare
        var hiddenGizmos = new List<string> { DisplayAttribute.GetDisplayName(typeof(TransformComponent)) };
        foreach (var gizmo in StrideEditorPlugin.GizmoTypeDictionary)
        {
            if (gizmo.Value.GetCustomAttribute<GizmoComponentAttribute>(true) is { HiddenByDefault: true })
                hiddenGizmos.Add(DisplayAttribute.GetDisplayName(gizmo.Key));
        }

        return new SceneSettingsData { HiddenGizmos = hiddenGizmos };
    }
}

/// <summary>
/// The rendering mode of the view of a scene editor, saved by name in <see cref="SceneSettingsData"/> (the WPF <c>RenderMode</c>).
/// </summary>
public enum SceneRenderMode
{
    /// <summary>
    /// Renders a single stream of the material.
    /// </summary>
    SingleStream,
    /// <summary>
    /// Renders the material with lighting.
    /// </summary>
    Lighting,
}

/// <summary>
/// The scene settings key of the package user settings, shared with the WPF editor.
/// </summary>
public static class PackageSceneSettings
{
    public static readonly SettingsKey<Dictionary<AssetId, SceneSettingsData>> SceneSettings = new("Package/SceneSettings",
        PackageUserSettings.SettingsContainer, () => new Dictionary<AssetId, SceneSettingsData>());
}
