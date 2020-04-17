// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Settings;
using Xenko.Assets.Presentation.AssetEditors.GameEditor;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Engine;
using Xenko.Engine.Processors;

namespace Xenko.Assets.Presentation.SceneEditor
{
    [DataContract(nameof(SceneSettingsData))]
    public sealed class SceneSettingsData
    {
        // Camera view parameters
        public Vector3 CamPosition = EditorGameCameraService.DefaultPosition;
        public Vector2 CamPitchYaw = new Vector2(EditorGameCameraService.DefaultPitch, EditorGameCameraService.DefaultYaw);

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
        public float ScaleSnapValue = 1.0f;
        public float SceneUnit = 1.0f;

        // Gizmos
        public bool SelectionMaskVisible = true;
        public bool GridVisible = true;
        public Color3 GridColor = (Color3)new Color(180, 180, 180);
        public bool CameraPreviewVisible = true;
        public RenderMode RenderMode = RenderMode.SingleStream;
        public string ActiveStream = string.Empty;
        public double TransformationGizmoSize = 1.0;
        public double ComponentGizmoSize = 1.0;
        public bool FixedSizeGizmos = false;
        public List<string> HiddenGizmos = new List<string>();
        public List<Guid> VisibleNavigationGroups = new List<Guid>();
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
        [NotNull]
        public static SceneSettingsData CreateDefault()
        {
            return new SceneSettingsData
            {
                HiddenGizmos = new List<string> { DisplayAttribute.GetDisplayName(typeof(TransformComponent)), DisplayAttribute.GetDisplayName(typeof(PhysicsComponent)) }
            };
        }
    }

    public static class PackageSceneSettings
    {
        public static SettingsKey<Dictionary<AssetId, SceneSettingsData>> SceneSettings = new SettingsKey<Dictionary<AssetId, SceneSettingsData>>("Package/SceneSettings",
            PackageUserSettings.SettingsContainer, () => new Dictionary<AssetId, SceneSettingsData>());
    }
}
