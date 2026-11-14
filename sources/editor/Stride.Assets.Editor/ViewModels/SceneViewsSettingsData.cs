// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Settings;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The scene views key of the package user settings: for each scene or prefab shown in several views, how its views are
/// laid out and where their cameras are.
/// </summary>
/// <remarks>
/// It is a key of its own, next to <see cref="PackageSceneSettings.SceneSettings"/>, whose members both editors share and
/// which keeps the camera of the primary view: an editor that shows one view keeps this key as it is, and one whose scene
/// settings have no member for the views (any earlier version) still reads its own.
/// </remarks>
public static class PackageSceneViews
{
    public static readonly SettingsKey<Dictionary<AssetId, SceneViewsSettingsData>> Views = new("Package/SceneViews",
        PackageUserSettings.SettingsContainer, () => new Dictionary<AssetId, SceneViewsSettingsData>());
}

/// <summary>
/// The views of a scene or prefab editor that shows several views of the scene.
/// </summary>
[DataContract(nameof(SceneViewsSettingsData))]
public sealed class SceneViewsSettingsData
{
    /// <summary>
    /// How the views are laid out.
    /// </summary>
    public EditorViewLayout Layout = EditorViewLayout.Single;

    /// <summary>
    /// The views of the layout other than the primary one, in the order of the panes. The primary view's camera is the
    /// camera of the scene settings.
    /// </summary>
    public List<SceneViewSettingsData> Views = [];

    /// <summary>
    /// The index of the pane that fills the whole view, in the panes of the layout, or -1 when every pane shows.
    /// </summary>
    public int MaximizedView = -1;
}

/// <summary>
/// A view of a scene editor that shows several views of the scene, other than the primary one: the camera it started from,
/// and where its camera is.
/// </summary>
[DataContract(nameof(SceneViewSettingsData))]
public sealed class SceneViewSettingsData
{
    /// <summary>
    /// The camera the view started from, which its title shows.
    /// </summary>
    public EditorViewCamera Camera = EditorViewCamera.Perspective;

    /// <summary>
    /// Whether the members below say where the camera of the view is; otherwise the view starts from <see cref="Camera"/>.
    /// </summary>
    public bool HasCameraState = false;

    public Vector3 CamPosition;
    public Vector2 CamPitchYaw;
    public CameraProjectionMode CamProjection = CameraProjectionMode.Perspective;
    public float CamOrthographicSize = CameraComponent.DefaultOrthographicSize;

    /// <summary>
    /// The point the camera of the view orbits around.
    /// </summary>
    public Vector3 CamTarget;
}
